using System;
using System.Collections;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Runtime;
using UnityEngine;
using Random = UnityEngine.Random;

namespace EndlessGuard.TestBattle
{
    // EnemyCatalog에서 보스를 제외한 일반 몬스터를 무작위로 추출하여 스폰하고,
    // 맵 생성 후 초기 텀(초기 지연 시간) 적용, 세이브 데이터(StageProgressManager) 연동, 마석(WaveStone) 보상 지급,
    // 라운드 스펙 스케일링, 공중 몬스터 직통 비행 경로, 덱 변경 시 현재 웨이브 유지 재시작 및 게임오버 복구를 총괄하는 웨이브 매니저
    public class CatalogWaveManager : MonoBehaviour
    {
        #region 인스펙터 직렬화 필드

        [Header("--- 맵 및 스테이지 참조 ---")]
        [Tooltip("경로 좌표 및 맵 재생성을 지원받을 MapGenerator 컴포넌트")]
        [SerializeField] private MapGenerator mapGenerator;

        [Tooltip("타일-월드 좌표 변환을 지원받을 GridMapRenderer 컴포넌트")]
        [SerializeField] private GridMapRenderer mapRenderer;

        [Tooltip("스테이지 및 웨이브 수치를 관리하는 StageManager 컴포넌트")]
        [SerializeField] private StageManager stageManager;

        [Header("--- 몬스터 카탈로그 및 보스 설정 ---")]
        [Tooltip("게임 내 전체 몬스터 데이터가 등록된 EnemyCatalog ScriptableObject")]
        [SerializeField] private EnemyCatalog enemyCatalog;

        [Tooltip("카탈로그에 보스 몬스터가 없을 경우 사용할 기본 예비 보스 프리팹")]
        [SerializeField] private EnemyRuntimeState fallbackBossPrefab;

        [Header("--- 스폰 및 웨이브 진행 수치 ---")]
        [Tooltip("맵 생성 완료 후 첫 몬스터 스폰이 시작되기 전 대기 시간 (초 단위 텀)")]
        [SerializeField, Min(0f)] private float initialSpawnDelay = 3.0f;

        [Tooltip("웨이브당 스폰할 적 몬스터 수 (각 경로당)")]
        [SerializeField] private int enemyCountPerWave = 3;

        [Tooltip("적 몬스터 간의 스폰 간격(초)")]
        [SerializeField] private float spawnInterval = 1.0f;

        [Tooltip("웨이브와 웨이브 사이의 대기 시간(초)")]
        [SerializeField] private float waveInterval = 3.0f;

        [Tooltip("지상 적 몬스터가 이동할 기본 높이(Y축) 오프셋")]
        [SerializeField] private float enemyHeight = 1.0f;

        [Tooltip("공중 몬스터의 직통 비행 높이(Y축) 오프셋")]
        [SerializeField] private float airMonsterHeight = 2.0f;

        [Header("--- 몬스터 스펙 스케일링 설정 ---")]
        [Tooltip("스테이지당 몬스터 최대 체력 증가율 (0.25 = +25%)")]
        [SerializeField, Range(0f, 2f)] private float stageHpMultiplier = 0.25f;

        [Tooltip("웨이브당 몬스터 최대 체력 추가 증가율 (0.05 = +5%)")]
        [SerializeField, Range(0f, 0.5f)] private float waveHpMultiplier = 0.05f;

        [Tooltip("스테이지당 몬스터 공격력 증가율 (0.15 = +15%)")]
        [SerializeField, Range(0f, 1f)] private float stageAttackMultiplier = 0.15f;

        [Tooltip("웨이브당 몬스터 공격력 추가 증가율 (0.03 = +3%)")]
        [SerializeField, Range(0f, 0.3f)] private float waveAttackMultiplier = 0.03f;

        [Tooltip("스테이지당 몬스터 방어력 증가율 (0.10 = +10%)")]
        [SerializeField, Range(0f, 1f)] private float stageDefenseMultiplier = 0.10f;

        [Header("--- 클리어 마석 보상 설정 ---")]
        [Tooltip("웨이브 클리어 시 지급할 기본 웨이브 마석 수량")]
        [SerializeField] private int waveClearWaveStone = 1;

        [Tooltip("스테이지 클리어 시 지급할 추가 웨이브 마석 수량")]
        [SerializeField] private int stageClearWaveStone = 5;

        #endregion

        #region 내부 런타임 데이터

        // 보스를 제외한 일반/엘리트 몬스터 풀
        private readonly List<EnemyDataSO> _normalEnemyPool = new List<EnemyDataSO>();

        // 보스 몬스터 풀
        private readonly List<EnemyDataSO> _bossEnemyPool = new List<EnemyDataSO>();

        // 현재 필드에 스폰되어 생존 중인 적 유닛 목록
        private readonly List<EnemyRuntimeState> _spawnedEnemies = new List<EnemyRuntimeState>();

        // 현재 진행 중인 스테이지 및 웨이브 번호
        private int _currentStage = 1;
        private int _currentWave = 1;
        private int _aliveEnemyCount = 0;

        // 실행 중인 메인 웨이브 코루틴 참조
        private Coroutine _waveSystemRoutine;

        #endregion

        #region 프로퍼티 및 이벤트

        public int CurrentStage => _currentStage;
        public int CurrentWave => _currentWave;
        public int AliveEnemyCount => _aliveEnemyCount;

        public event Action<int> OnStageCleared;

        #endregion

        #region 라이프사이클

        private void Awake()
        {
            if (mapGenerator == null) mapGenerator = FindFirstObjectByType<MapGenerator>();
            if (mapRenderer == null) mapRenderer = FindFirstObjectByType<GridMapRenderer>();
            if (stageManager == null) stageManager = FindFirstObjectByType<StageManager>();

            InitializeEnemyPoolFromCatalog();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<NormalDeckChangedEvent>(OnNormalDeckChanged);
            EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);

            CombatEvents.OnEnemyDied += HandleEnemyDied;
            CombatEvents.OnEnemyReachedGoal += HandleEnemyReachedGoal;

            if (mapGenerator != null)
            {
                mapGenerator.OnMapGenerated += HandleMapRegenerated;
            }
        }

        private void Start()
        {
            LoadProgressFromStageProgressManager();

            if (mapGenerator != null && mapGenerator.IsMapGenerated)
            {
                HandleMapRegenerated();
            }
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<NormalDeckChangedEvent>(OnNormalDeckChanged);
            EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);

            CombatEvents.OnEnemyDied -= HandleEnemyDied;
            CombatEvents.OnEnemyReachedGoal -= HandleEnemyReachedGoal;

            if (mapGenerator != null)
            {
                mapGenerator.OnMapGenerated -= HandleMapRegenerated;
            }

            StopWaveSystem();
            ClearAllActiveEnemies();
        }

        #endregion

        #region 세이브 데이터 동기화 및 풀 초기화

        private void LoadProgressFromStageProgressManager()
        {
            if (StageProgressManager.Instance != null)
            {
                _currentStage = StageProgressManager.Instance.CurrentStage;
                _currentWave = StageProgressManager.Instance.CurrentWave;
            }
            else if (stageManager != null)
            {
                _currentStage = stageManager.CurrentStage;
            }
        }

        private void InitializeEnemyPoolFromCatalog()
        {
            _normalEnemyPool.Clear();
            _bossEnemyPool.Clear();

            if (enemyCatalog == null || enemyCatalog.Enemies == null)
            {
                Debug.LogWarning("[CatalogWaveManager] EnemyCatalog가 연결되지 않았습니다.", this);
                return;
            }

            foreach (EnemyDataSO enemy in enemyCatalog.Enemies)
            {
                if (enemy == null || enemy.EnemyPrefab == null) continue;

                if (enemy.Category == EnemyCategory.Boss)
                {
                    _bossEnemyPool.Add(enemy);
                }
                else
                {
                    _normalEnemyPool.Add(enemy);
                }
            }

            Debug.Log($"[CatalogWaveManager] 적 카탈로그 풀 구성 완료 - 일반 몬스터: {_normalEnemyPool.Count}종, 보스: {_bossEnemyPool.Count}종");
        }

        #endregion

        #region 웨이브 진행 제어 로직

        private void HandleMapRegenerated()
        {
            ClearAllActiveEnemies();
            StartWaveSystem();
        }

        public void StartWaveSystem()
        {
            StopWaveSystem();
            _waveSystemRoutine = StartCoroutine(RunWaveSystemLoop());
        }

        public void StopWaveSystem()
        {
            if (_waveSystemRoutine != null)
            {
                StopCoroutine(_waveSystemRoutine);
                _waveSystemRoutine = null;
            }
        }

        private void OnNormalDeckChanged(NormalDeckChangedEvent evt)
        {
            RestartCurrentWave();
        }

        public void RestartCurrentWave()
        {
            StopWaveSystem();
            ClearAllActiveEnemies();

            Debug.Log($"[CatalogWaveManager] 덱 변경으로 인한 현재 웨이브 재시작: Stage {_currentStage} - Wave {_currentWave}");

            _waveSystemRoutine = StartCoroutine(RunWaveSystemLoop());
        }

        // 현재 웨이브부터 보스 웨이브까지 순차 실행하는 메인 코루틴
        private IEnumerator RunWaveSystemLoop()
        {
            int totalWaves = (stageManager != null) ? stageManager.WavesPerStage : 5;

            // 기술적 근거: 맵 생성 직후 아군 유닛이 배치되고 플레이어가 전황을 파악할 수 있도록 몬스터 스폰 전 초기 텀(대기 시간) 부여
            if (initialSpawnDelay > 0f)
            {
                Debug.Log($"[CatalogWaveManager] 맵 생성 완료: 첫 웨이브 스폰까지 {initialSpawnDelay}초 대기");
                yield return new WaitForSeconds(initialSpawnDelay);
            }

            while (_currentWave <= totalWaves)
            {
                if (StageProgressManager.Instance != null)
                {
                    StageProgressManager.Instance.SetCurrentWave(_currentWave);
                }

                Debug.Log($"[CatalogWaveManager] Stage {_currentStage} - Wave {_currentWave} / {totalWaves} 시작");
                float waveStartTime = Time.time;

                if (_currentWave == totalWaves)
                {
                    yield return StartCoroutine(RunBossWave());
                }
                else
                {
                    yield return StartCoroutine(RunNormalWave());
                }

                float waveDuration = Time.time - waveStartTime;
                HandleWaveCleared(waveDuration);

                _currentWave++;

                if (_currentWave <= totalWaves)
                {
                    yield return new WaitForSeconds(waveInterval);
                }
            }

            Debug.Log($"[CatalogWaveManager] Stage {_currentStage} 내 모든 웨이브 완료! 스테이지 클리어 처리 및 맵 재생성");

            HandleStageCleared();
        }

        private IEnumerator RunNormalWave()
        {
            for (int i = 0; i < enemyCountPerWave; i++)
            {
                SpawnRandomNormalEnemy(mapGenerator.PathPosition);
                SpawnRandomNormalEnemy(mapGenerator.PathPositionB);

                yield return new WaitForSeconds(spawnInterval);
            }

            while (_aliveEnemyCount > 0)
            {
                yield return null;
            }
        }

        private IEnumerator RunBossWave()
        {
            Debug.Log("[CatalogWaveManager] [BOSS WAVE] 강력한 보스 몬스터 등장!");

            SpawnBossEnemy(mapGenerator.PathPosition);

            while (_aliveEnemyCount > 0)
            {
                yield return null;
            }
        }

        #endregion

        #region 마석 보상 및 클리어 처리

        private void HandleWaveCleared(float duration)
        {
            if (StageProgressManager.Instance != null)
            {
                StageProgressManager.Instance.RecordWaveClearDuration(duration);
            }

            if (CurrencyManager.Instance != null && waveClearWaveStone > 0)
            {
                CurrencyManager.Instance.AddCurrency(CurrencyType.WaveStone, waveClearWaveStone, applyModifiers: false);
            }

            EventBus.Publish(new WaveClearedEvent(_currentStage, _currentWave, waveClearWaveStone));
            Debug.Log($"[CatalogWaveManager] Stage {_currentStage} - Wave {_currentWave} 클리어! (웨이브 마석 +{waveClearWaveStone}, 소요시간: {duration:F1}초)");
        }

        private void HandleStageCleared()
        {
            int clearedStage = _currentStage;

            if (CurrencyManager.Instance != null && stageClearWaveStone > 0)
            {
                CurrencyManager.Instance.AddCurrency(CurrencyType.WaveStone, stageClearWaveStone, applyModifiers: false);
            }

            EventBus.Publish(new StageClearedEvent(clearedStage, stageClearWaveStone));
            OnStageCleared?.Invoke(clearedStage);

            if (StageProgressManager.Instance != null)
            {
                StageProgressManager.Instance.AdvanceToNextStage();
                _currentStage = StageProgressManager.Instance.CurrentStage;
            }
            else
            {
                _currentStage++;
            }

            if (stageManager != null)
            {
                stageManager.ClearStage();
            }

            _currentWave = 1;

            if (mapGenerator != null)
            {
                mapGenerator.RegenerateMap();
            }
        }

        #endregion

        #region 적 몬스터 스폰 및 능력치 스케일링 로직

        private void SpawnRandomNormalEnemy(IReadOnlyList<Vector2Int> path)
        {
            if (path == null || path.Count == 0 || mapRenderer == null) return;

            EnemyDataSO selectedData = null;
            if (_normalEnemyPool.Count > 0)
            {
                selectedData = _normalEnemyPool[Random.Range(0, _normalEnemyPool.Count)];
            }

            if (selectedData == null || selectedData.EnemyPrefab == null) return;

            SpawnEnemyInstance(selectedData, path);
        }

        private void SpawnBossEnemy(IReadOnlyList<Vector2Int> path)
        {
            if (path == null || path.Count == 0 || mapRenderer == null) return;

            EnemyDataSO selectedBossData = null;
            if (_bossEnemyPool.Count > 0)
            {
                selectedBossData = _bossEnemyPool[Random.Range(0, _bossEnemyPool.Count)];
            }

            if (selectedBossData != null && selectedBossData.EnemyPrefab != null)
            {
                SpawnEnemyInstance(selectedBossData, path);
            }
            else if (fallbackBossPrefab != null)
            {
                SpawnFallbackBoss(path);
            }
        }

        private void SpawnEnemyInstance(EnemyDataSO enemyData, IReadOnlyList<Vector2Int> gridPath)
        {
            bool isAir = enemyData.MovementType == EnemyMovementType.Air;
            PathNode[] pathNodes = isAir ? BuildAirPathNodes(gridPath) : BuildPathNodes(gridPath);

            if (pathNodes == null || pathNodes.Length == 0) return;

            GameObject instance = Instantiate(enemyData.EnemyPrefab, pathNodes[0].Position, Quaternion.identity);
            instance.name = $"Enemy_{enemyData.DisplayName}_{_aliveEnemyCount + 1}";

            EnemyRuntimeState enemyState = instance.GetComponent<EnemyRuntimeState>();
            if (enemyState == null || enemyState.Move == null)
            {
                Debug.LogError("[CatalogWaveManager] 생성된 적 프리팹에 EnemyRuntimeState 컴포넌트가 없습니다.", instance);
                Destroy(instance);
                return;
            }

            enemyState.InitializeRuntime();
            ApplyStatScaling(enemyState, enemyData);
            enemyState.Move.SetPath(pathNodes);

            _spawnedEnemies.Add(enemyState);
            SpawnedEnemyManager.Instance.RegisterEnemy(enemyState);
            _aliveEnemyCount++;
        }

        private void SpawnFallbackBoss(IReadOnlyList<Vector2Int> gridPath)
        {
            PathNode[] pathNodes = BuildPathNodes(gridPath);
            if (pathNodes == null || pathNodes.Length == 0) return;

            EnemyRuntimeState bossInstance = Instantiate(fallbackBossPrefab, pathNodes[0].Position, Quaternion.identity);
            bossInstance.Move.SetPath(pathNodes);

            _spawnedEnemies.Add(bossInstance);
            SpawnedEnemyManager.Instance.RegisterEnemy(bossInstance);
            _aliveEnemyCount++;
        }

        private void ApplyStatScaling(EnemyRuntimeState runtimeState, EnemyDataSO enemyData)
        {
            if (runtimeState == null || enemyData == null || enemyData.BaseStats == null || runtimeState.Stats == null)
            {
                return;
            }

            float hpScale = 1.0f + (_currentStage - 1) * stageHpMultiplier + (_currentWave - 1) * waveHpMultiplier;
            float atkScale = 1.0f + (_currentStage - 1) * stageAttackMultiplier + (_currentWave - 1) * waveAttackMultiplier;
            float defScale = 1.0f + (_currentStage - 1) * stageDefenseMultiplier;

            float baseMaxHp = enemyData.BaseStats.MaxHp;
            float newMaxHp = Mathf.Max(1f, baseMaxHp * hpScale);
            runtimeState.Stats.SetMaxHp(newMaxHp);
            if (runtimeState.Health != null)
            {
                runtimeState.Health.SetMaxHp(newMaxHp);
            }

            float basePAtk = enemyData.BaseStats.PhysicalAttack;
            float baseMAtk = enemyData.BaseStats.MagicalAttack;
            runtimeState.Stats.SetPhysicalAttack(basePAtk * atkScale);
            runtimeState.Stats.SetMagicalAttack(baseMAtk * atkScale);

            float basePDef = enemyData.BaseStats.PhysicalDefense;
            float baseMDef = enemyData.BaseStats.MagicalDefense;
            runtimeState.Stats.SetPhysicalDefense(basePDef * defScale);
            runtimeState.Stats.SetMagicalDefense(baseMDef * defScale);
        }

        private PathNode[] BuildPathNodes(IReadOnlyList<Vector2Int> gridPath)
        {
            if (gridPath == null || gridPath.Count == 0 || mapRenderer == null) return null;

            PathNode[] pathNodes = new PathNode[gridPath.Count];

            for (int i = 0; i < gridPath.Count; i++)
            {
                Vector2Int gridPosition = gridPath[i];
                Vector3 worldPosition = mapRenderer.GridToWorld(gridPosition);
                worldPosition.y = enemyHeight;

                GridFacingDirection facing = ResolveFacingDirection(gridPath, i);
                pathNodes[i] = new PathNode(worldPosition, gridPosition, facing);
            }

            return pathNodes;
        }

        private PathNode[] BuildAirPathNodes(IReadOnlyList<Vector2Int> gridPath)
        {
            if (gridPath == null || gridPath.Count < 2 || mapRenderer == null) return null;

            Vector2Int startCoord = gridPath[0];
            Vector2Int goalCoord = gridPath[gridPath.Count - 1];

            Vector3 startPos = mapRenderer.GridToWorld(startCoord);
            startPos.y = airMonsterHeight;

            Vector3 goalPos = mapRenderer.GridToWorld(goalCoord);
            goalPos.y = airMonsterHeight;

            Vector2Int delta = goalCoord - startCoord;
            GridFacingDirection facing = ResolveFacingFromDelta(delta);

            return new PathNode[]
            {
                new PathNode(startPos, startCoord, facing),
                new PathNode(goalPos, goalCoord, facing)
            };
        }

        private GridFacingDirection ResolveFacingDirection(IReadOnlyList<Vector2Int> path, int index)
        {
            if (path == null || path.Count <= 1) return GridFacingDirection.East;

            Vector2Int direction = (index < path.Count - 1)
                ? (path[index + 1] - path[index])
                : (path[index] - path[index - 1]);

            return ResolveFacingFromDelta(direction);
        }

        private GridFacingDirection ResolveFacingFromDelta(Vector2Int delta)
        {
            if (delta.x > 0) return GridFacingDirection.East;
            if (delta.x < 0) return GridFacingDirection.West;
            if (delta.y > 0) return GridFacingDirection.North;
            if (delta.y < 0) return GridFacingDirection.South;

            return GridFacingDirection.East;
        }

        #endregion

        #region 게임오버(패배) 및 전투 이벤트 수신

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            if (evt.newState == GameState.GameOver)
            {
                Debug.LogWarning($"[CatalogWaveManager] [GAME OVER] 라운드 패배 감지! Stage {_currentStage} - Wave 1부터 재도전합니다.");

                _currentWave = 1;
                StopWaveSystem();
                ClearAllActiveEnemies();

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.ResetLife();
                    GameManager.Instance.ChangeState(GameState.InGame);
                }

                StartWaveSystem();
            }
        }

        private void HandleEnemyDied(EnemyDiedInfo info)
        {
            EnemyRuntimeState enemy = FindSpawnedEnemy(info.RuntimeId);
            if (enemy == null) return;

            RemoveEnemyFromWave(enemy);
        }

        private void HandleEnemyReachedGoal(EnemyReachedGoalInfo info)
        {
            EnemyRuntimeState enemy = FindSpawnedEnemy(info.RuntimeId);
            if (enemy == null) return;

            RemoveEnemyFromWave(enemy);
            Destroy(enemy.gameObject);
        }

        private EnemyRuntimeState FindSpawnedEnemy(int runtimeId)
        {
            for (int i = 0; i < _spawnedEnemies.Count; i++)
            {
                if (_spawnedEnemies[i] != null && _spawnedEnemies[i].RuntimeId == runtimeId)
                {
                    return _spawnedEnemies[i];
                }
            }
            return null;
        }

        private void RemoveEnemyFromWave(EnemyRuntimeState enemy)
        {
            if (enemy == null) return;

            if (_spawnedEnemies.Remove(enemy))
            {
                SpawnedEnemyManager.Instance.UnregisterEnemy(enemy);
                _aliveEnemyCount = Mathf.Max(0, _aliveEnemyCount - 1);
            }
        }

        public void ClearAllActiveEnemies()
        {
            for (int i = _spawnedEnemies.Count - 1; i >= 0; i--)
            {
                if (_spawnedEnemies[i] != null)
                {
                    SpawnedEnemyManager.Instance.UnregisterEnemy(_spawnedEnemies[i]);
                    Destroy(_spawnedEnemies[i].gameObject);
                }
            }

            _spawnedEnemies.Clear();
            _aliveEnemyCount = 0;
        }

        #endregion
    }
}
