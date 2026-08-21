using System;
using System.Collections;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Runtime;
using UnityEngine;
using Random = UnityEngine.Random;

namespace EndlessGuard.TestBattle
{
    /// <summary>
    /// EnemyCatalog의 몬스터 데이터를 바탕으로 스테이지 및 웨이브별 적을 소환하고,
    /// EnemyMove 경로 주입, 처치 보상(EventBus) 발행 및 스테이지 진행을 관리하는 웨이브 매니저 클래스
    /// </summary>
    public class TestWaveManager : MonoBehaviour
    {
        #region 인스펙터 직렬화 필드

        [Header("--- 맵 생성기 참조 ---")]
        [Tooltip("경로 데이터(PathNodes)를 제공받을 TestMapGenerator")]
        [SerializeField] private TestMapGenerator mapGenerator;

        [Header("--- 적 카탈로그 데이터 ---")]
        [Tooltip("웨이브에 등장할 적 데이터 ScriptableObject 카탈로그")]
        [SerializeField] private EnemyCatalog enemyCatalog;

        [Header("--- 보스 적 프리팹 (선택) ---")]
        [Tooltip("보스 웨이브에 강제 지정할 보스 EnemyDataSO (미지정 시 카탈로그에서 자동 선별)")]
        [SerializeField] private EnemyDataSO bossEnemyData;

        [Header("--- 웨이브 및 스테이지 설정 ---")]
        [Tooltip("현재 진행 중인 스테이지 번호")]
        [SerializeField, Min(1)] private int currentStage = 1;

        [Tooltip("스테이지당 웨이브 수 (기본 5)")]
        [SerializeField, Min(1)] private int wavesPerStage = 5;

        [Tooltip("웨이브당 스폰할 적 수 (기본 3~4마리)")]
        [SerializeField, Min(1)] private int enemiesPerWave = 3;

        [Tooltip("적 생성 간격 (초)")]
        [SerializeField, Min(0.1f)] private float spawnInterval = 1.0f;

        [Tooltip("웨이브 간 휴식 대기 시간 (초)")]
        [SerializeField, Min(0.5f)] private float waveInterval = 3.0f;

        [Header("--- 몬스터 스테이지/웨이브 스케일링 설정 ---")]
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

        [Header("--- 공중 몬스터 비행 설정 ---")]
        [Tooltip("공중 몬스터의 비행 높이 (Y축 오프셋)")]
        [SerializeField] private float airMonsterHeight = 2.0f;

        [Header("--- 클리어 마석 보상 설정 ---")]
        [Tooltip("웨이브 클리어 시 지급할 기본 웨이브 마석 수량")]
        [SerializeField] private int waveClearWaveStone = 1;

        [Tooltip("스테이지 클리어 시 지급할 추가 웨이브 마석 수량")]
        [SerializeField] private int stageClearWaveStone = 5;

        #endregion

        #region 내부 런타임 추적 데이터

        private int _currentWave = 0;
        private int _aliveEnemyCount = 0;
        private Coroutine _waveSystemCoroutine;

        // 필드에 스폰된 적 런타임 목록
        private readonly List<EnemyRuntimeState> _spawnedEnemies = new List<EnemyRuntimeState>();

        public int CurrentStage => currentStage;
        public int CurrentWave => _currentWave;
        public int WavesPerStage => wavesPerStage;
        public int AliveEnemyCount => _aliveEnemyCount;
        public bool IsWaveRunning => _waveSystemCoroutine != null;

        // 스테이지 완료 시 발행되는 이벤트 (전투 코디네이터 연동)
        public event Action<int> OnStageCleared;

        #endregion

        #region 라이프사이클

        private void Awake()
        {
            if (mapGenerator == null)
            {
                mapGenerator = FindFirstObjectByType<TestMapGenerator>();
            }

            if (enemyCatalog == null)
            {
                enemyCatalog = Resources.Load<EnemyCatalog>("Catalogs/EnemyCatalog");
            }
        }

        private void OnEnable()
        {
            // Unit 전투 엔진 전역 사망 및 골인 이벤트 구독
            CombatEvents.OnEnemyDied += HandleEnemyDied;
            CombatEvents.OnEnemyReachedGoal += HandleEnemyReachedGoal;
        }

        private void OnDisable()
        {
            CombatEvents.OnEnemyDied -= HandleEnemyDied;
            CombatEvents.OnEnemyReachedGoal -= HandleEnemyReachedGoal;
            StopWaveSystem();
            ClearAllEnemies();
        }

        #endregion

        #region 웨이브 시스템 제어

        /// <summary>
        /// 1웨이브부터 스테이지 웨이브 코루틴을 시작합니다.
        /// </summary>
        public void StartStageWaves()
        {
            if (StageProgressManager.Instance != null)
            {
                currentStage = StageProgressManager.Instance.CurrentStage;
            }

            StopWaveSystem();
            ClearAllEnemies();
            _waveSystemCoroutine = StartCoroutine(RunStageWaveSystem());
        }

        /// <summary>
        /// 실행 중인 모든 웨이브 코루틴을 정지합니다.
        /// </summary>
        public void StopWaveSystem()
        {
            if (_waveSystemCoroutine != null)
            {
                StopCoroutine(_waveSystemCoroutine);
                _waveSystemCoroutine = null;
            }
        }

        /// <summary>
        /// 현재 스테이지의 진행 상태를 초기화하고 1웨이브부터 재시작합니다.
        /// </summary>
        public void RestartCurrentStage()
        {
            if (StageProgressManager.Instance != null)
            {
                currentStage = StageProgressManager.Instance.CurrentStage;
            }

            Debug.Log($"[TestWaveManager] Stage {currentStage} 웨이브 재시작");
            StartStageWaves();
        }

        // 전체 스테이지 웨이브 루프 코루틴
        private IEnumerator RunStageWaveSystem()
        {
            for (int waveIdx = 0; waveIdx < wavesPerStage; waveIdx++)
            {
                _currentWave = waveIdx + 1;

                if (StageProgressManager.Instance != null)
                {
                    StageProgressManager.Instance.SetCurrentWave(_currentWave);
                }

                // 1. EventBus로 스테이지/웨이브 변경 이벤트 발행 -> InGameUI 등에 반영
                EventBus.Publish(new StageWaveChangedEvent(currentStage, _currentWave));
                Debug.Log($"[TestWaveManager] >>> Stage {currentStage} - Wave {_currentWave}/{wavesPerStage} 시작 <<<");

                // 2. 마지막 웨이브는 보스 웨이브, 그 외는 일반 웨이브 실행
                if (_currentWave == wavesPerStage)
                {
                    yield return StartCoroutine(RunBossWaveRoutine());
                }
                else
                {
                    yield return StartCoroutine(RunNormalWaveRoutine());
                }

                // 3. 필드의 모든 적이 전멸할 때까지 대기
                while (_aliveEnemyCount > 0)
                {
                    yield return null;
                }

                // 4. 웨이브 클리어 보상 (웨이브 마석 지급 및 이벤트 발행)
                if (CurrencyManager.Instance != null && waveClearWaveStone > 0)
                {
                    CurrencyManager.Instance.AddCurrency(CurrencyType.WaveStone, waveClearWaveStone, applyModifiers: false);
                }
                EventBus.Publish(new WaveClearedEvent(currentStage, _currentWave, waveClearWaveStone));
                Debug.Log($"[TestWaveManager] Stage {currentStage} - Wave {_currentWave} 클리어! (보상: 웨이브 마석 +{waveClearWaveStone})");

                // 5. 다음 웨이브 사이 대기
                if (waveIdx < wavesPerStage - 1)
                {
                    yield return new WaitForSeconds(waveInterval);
                }
            }

            Debug.Log($"[TestWaveManager] Stage {currentStage} 모든 웨이브 클리어!");

            int clearedStage = currentStage;

            // 스테이지 클리어 보상 (추가 웨이브 마석 지급 및 이벤트 발행)
            if (CurrencyManager.Instance != null && stageClearWaveStone > 0)
            {
                CurrencyManager.Instance.AddCurrency(CurrencyType.WaveStone, stageClearWaveStone, applyModifiers: false);
            }
            EventBus.Publish(new StageClearedEvent(clearedStage, stageClearWaveStone));

            // StageProgressManager에 다음 스테이지 진입 및 자동 저장 요청
            if (StageProgressManager.Instance != null)
            {
                StageProgressManager.Instance.AdvanceToNextStage();
                currentStage = StageProgressManager.Instance.CurrentStage;
            }
            else
            {
                currentStage++;
            }

            OnStageCleared?.Invoke(clearedStage);
        }

        // 일반 웨이브 스폰 코루틴
        private IEnumerator RunNormalWaveRoutine()
        {
            for (int i = 0; i < enemiesPerWave; i++)
            {
                // 경로 A와 경로 B에 각각 1마리씩 분산 스폰
                SpawnEnemyOnPath(mapGenerator.PathNodesA);

                if (mapGenerator.PathNodesB != null && mapGenerator.PathNodesB.Length > 0)
                {
                    SpawnEnemyOnPath(mapGenerator.PathNodesB);
                }

                yield return new WaitForSeconds(spawnInterval);
            }
        }

        // 보스 웨이브 스폰 코루틴
        private IEnumerator RunBossWaveRoutine()
        {
            Debug.Log("[TestWaveManager] [BOSS WAVE] 강력한 보스 몬스터 등장!");

            // 보스 적 스폰 (경로 A)
            SpawnBossEnemyOnPath(mapGenerator.PathNodesA);

            yield return null;
        }

        #endregion

        #region 적 스폰 및 초기화 로직

        // 경로를 따라 이동하는 일반 적 스폰
        private void SpawnEnemyOnPath(PathNode[] pathNodes)
        {
            if (pathNodes == null || pathNodes.Length == 0 || enemyCatalog == null || enemyCatalog.Enemies.Count == 0)
            {
                return;
            }

            // 카탈로그에서 무작위 일반 적 데이터 선택
            EnemyDataSO randomEnemyData = enemyCatalog.Enemies[Random.Range(0, enemyCatalog.Enemies.Count)];
            if (randomEnemyData == null || randomEnemyData.EnemyPrefab == null)
            {
                return;
            }

            SpawnEnemyInstance(randomEnemyData, pathNodes);
        }

        // 보스 적 스폰
        private void SpawnBossEnemyOnPath(PathNode[] pathNodes)
        {
            if (pathNodes == null || pathNodes.Length == 0) return;

            EnemyDataSO targetBossData = bossEnemyData;
            if (targetBossData == null && enemyCatalog != null && enemyCatalog.Enemies.Count > 0)
            {
                // 보스 데이터 미할당 시 카탈로그의 마지막 유닛을 보스로 활용
                targetBossData = enemyCatalog.Enemies[enemyCatalog.Enemies.Count - 1];
            }

            if (targetBossData != null && targetBossData.EnemyPrefab != null)
            {
                SpawnEnemyInstance(targetBossData, pathNodes);
            }
        }

        // 적 프리팹 인스턴스화, DataLink 주입, EnemyRuntimeState 초기화 및 EnemyMove 경로 설정
        private void SpawnEnemyInstance(EnemyDataSO enemyData, PathNode[] pathNodes)
        {
            // 1. 공중 몬스터 여부에 따른 경로 분기 및 생성
            PathNode[] targetPath = pathNodes;
            bool isAir = enemyData.MovementType == EnemyMovementType.Air;

            if (isAir && mapGenerator != null && mapGenerator.PathPositionA != null && mapGenerator.PathPositionA.Count > 1)
            {
                Vector2Int startCoord = mapGenerator.PathPositionA[0];
                Vector2Int goalCoord = mapGenerator.PathPositionA[mapGenerator.PathPositionA.Count - 1];
                targetPath = mapGenerator.BuildAirPath(airMonsterHeight, startCoord, goalCoord);
            }

            PathNode startNode = targetPath[0];
            Vector3 spawnWorldPos = startNode.Position;
            if (!isAir)
            {
                spawnWorldPos.y = 1.0f; // 지상 적 기본 높이 설정
            }

            // 2. 프리팹 인스턴스화
            GameObject enemyObj = Instantiate(enemyData.EnemyPrefab, spawnWorldPos, Quaternion.identity);
            enemyObj.name = $"Enemy_{enemyData.DisplayName}_{_aliveEnemyCount + 1}";

            // 3. EnemyDataLink 확인
            EnemyDataLink dataLink = enemyObj.GetComponent<EnemyDataLink>();

            // 4. EnemyRuntimeState 초기화
            EnemyRuntimeState runtimeState = enemyObj.GetComponent<EnemyRuntimeState>();
            if (runtimeState != null)
            {
                runtimeState.InitializeRuntime();

                // 5. 라운드(스테이지/웨이브) 스케일링 능력치 보정 주입
                ApplyStatScaling(runtimeState, enemyData);
            }

            // 6. EnemyMove 컴포넌트에 PathNode[] 경로 주입
            EnemyMove mover = enemyObj.GetComponent<EnemyMove>();
            if (mover != null)
            {
                mover.SetPath(targetPath);
            }

            // 7. SpawnedEnemyManager 등록 및 리스트 추적
            if (SpawnedEnemyManager.Instance != null && runtimeState != null)
            {
                SpawnedEnemyManager.Instance.RegisterEnemy(runtimeState);
            }

            if (runtimeState != null)
            {
                _spawnedEnemies.Add(runtimeState);
            }

            _aliveEnemyCount++;
        }

        // 스테이지 및 웨이브 진행도에 따른 몬스터 체력/공격력/방어력 스케일링 계산 및 주입
        private void ApplyStatScaling(EnemyRuntimeState runtimeState, EnemyDataSO enemyData)
        {
            if (runtimeState == null || enemyData == null || enemyData.BaseStats == null || runtimeState.Stats == null)
            {
                return;
            }

            float hpScale = 1.0f + (currentStage - 1) * stageHpMultiplier + (_currentWave - 1) * waveHpMultiplier;
            float atkScale = 1.0f + (currentStage - 1) * stageAttackMultiplier + (_currentWave - 1) * waveAttackMultiplier;
            float defScale = 1.0f + (currentStage - 1) * stageDefenseMultiplier;

            // 1. 최대 체력 스케일링 적용
            float baseMaxHp = enemyData.BaseStats.MaxHp;
            float newMaxHp = Mathf.Max(1f, baseMaxHp * hpScale);
            runtimeState.Stats.SetMaxHp(newMaxHp);
            if (runtimeState.Health != null)
            {
                runtimeState.Health.SetMaxHp(newMaxHp);
            }

            // 2. 공격력 스케일링 적용
            float basePAtk = enemyData.BaseStats.PhysicalAttack;
            float baseMAtk = enemyData.BaseStats.MagicalAttack;
            runtimeState.Stats.SetPhysicalAttack(basePAtk * atkScale);
            runtimeState.Stats.SetMagicalAttack(baseMAtk * atkScale);

            // 3. 방어력 스케일링 적용
            float basePDef = enemyData.BaseStats.PhysicalDefense;
            float baseMDef = enemyData.BaseStats.MagicalDefense;
            runtimeState.Stats.SetPhysicalDefense(basePDef * defScale);
            runtimeState.Stats.SetMagicalDefense(baseMDef * defScale);
        }

        #endregion

        #region 적 사망 / 도착 이벤트 수신 및 보상 처리

        // 적 사망 이벤트 콜백 (Unit 전투 시스템 CombatEvents 연동)
        private void HandleEnemyDied(EnemyDiedInfo info)
        {
            // 사망한 적 탐색
            EnemyRuntimeState deadEnemy = null;
            for (int i = 0; i < _spawnedEnemies.Count; i++)
            {
                if (_spawnedEnemies[i] != null && _spawnedEnemies[i].RuntimeId == info.RuntimeId)
                {
                    deadEnemy = _spawnedEnemies[i];
                    break;
                }
            }

            if (deadEnemy != null)
            {
                _spawnedEnemies.Remove(deadEnemy);

                // 골드 및 경험치 보상 산출 (EnemyDataSO의 RewardGold, RewardExp 추출)
                int rewardGold = 10;
                int rewardExp = 5;
                if (deadEnemy.DataLink != null && deadEnemy.DataLink.HasData && deadEnemy.DataLink.EnemyData != null)
                {
                    rewardGold = Mathf.Max(1, deadEnemy.DataLink.EnemyData.RewardGold);
                    rewardExp = Mathf.Max(1, deadEnemy.DataLink.EnemyData.RewardExp);
                }

                // EventBus로 EnemyDiedEvent 발행 -> CurrencyManager 및 ExperienceManager가 수급
                EventBus.Publish(new EnemyDiedEvent(
                    deadEnemy.gameObject,
                    deadEnemy.EnemyId,
                    rewardGold,
                    rewardExp,
                    deadEnemy.transform.position
                ));

                // 매니저 등록 해제 및 파괴
                if (SpawnedEnemyManager.Instance != null)
                {
                    SpawnedEnemyManager.Instance.UnregisterEnemy(deadEnemy);
                }

                Destroy(deadEnemy.gameObject);
            }

            DecrementAliveEnemyCount();
        }

        // 적이 골 지점에 도달했을 때의 콜백
        private void HandleEnemyReachedGoal(EnemyReachedGoalInfo info)
        {
            for (int i = _spawnedEnemies.Count - 1; i >= 0; i--)
            {
                EnemyRuntimeState enemy = _spawnedEnemies[i];
                if (enemy != null && enemy.RuntimeId == info.RuntimeId)
                {
                    _spawnedEnemies.RemoveAt(i);
                    break;
                }
            }

            DecrementAliveEnemyCount();
        }

        private void DecrementAliveEnemyCount()
        {
            _aliveEnemyCount = Mathf.Max(0, _aliveEnemyCount - 1);
        }

        #endregion

        #region 적 전체 정리

        /// <summary>
        /// 필드의 모든 적 유닛을 파괴하고 카운트를 초기화합니다.
        /// </summary>
        public void ClearAllEnemies()
        {
            for (int i = _spawnedEnemies.Count - 1; i >= 0; i--)
            {
                EnemyRuntimeState enemy = _spawnedEnemies[i];
                if (enemy != null)
                {
                    if (SpawnedEnemyManager.Instance != null)
                    {
                        SpawnedEnemyManager.Instance.UnregisterEnemy(enemy);
                    }
                    Destroy(enemy.gameObject);
                }
            }

            _spawnedEnemies.Clear();
            _aliveEnemyCount = 0;
        }

        #endregion
    }
}
