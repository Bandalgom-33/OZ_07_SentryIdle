using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Runtime;
using Random = UnityEngine.Random;

public class WaveManager : MonoBehaviour
{
    [Header("맵 참조하기")] 
    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private GridMapRenderer mapRenderer;

    [Header("Stage 참조하기")]
    [SerializeField] private StageManager stageManager;

    [Header("Wave 설정")]
    private int currentWave = 0;
    
    [Header("적 데이터 카탈로그")]
    [SerializeField] private EnemyCatalog enemyCatalog;
    
    [Header("웨이브 적 강화")]
    [SerializeField] private float hpBonusPerWavePercent = 10f;
    [SerializeField] private float attackBonusPerWavePercent = 5f;
    [SerializeField] private float defenseBonusPerWavePercent = 5f;
    
    [Header("웨이브 클리어 보상")]
    [SerializeField] private long waveClearReward = 1;
    

    //정식 EnemyMove의 SetPath()가 PathNode의 월드 좌표를 사용하기 때문에
    //적이 이동할 높이도 PathNode 생성 시 같이 적용
    [SerializeField] private float enemyHeight = 1.0f;

    //한 웨이브당 적 소환 갯수
    [SerializeField] private int enemyCountPerWave = 3;
    //적 한 마리와 다음 적 소환 간격
    [SerializeField] private float spawnInterval = 1.0f;
    //웨이브와 웨이브 사이 대기 시간
    [SerializeField] private float waveInterval = 5.0f;
    
    //적 출구 도착 알림이
    public event Action<EnemyReachedGoalInfo> OnEnemyReachedGoal;
    //적이 살아있는지 확인
    private int aliveEnemyCount = 0;

    //적 리스트 직접 관리하기
    private List<EnemyRuntimeState> spawnedEnemies = new List<EnemyRuntimeState>();

    public List<Vector3> path; 
    int CurrentWave => currentWave;

    //MapGenerator 에 있는 SpawnEnemy를 WaveManager로 가져오기
    //기존 EnemyMover 대신 정식 EnemyRuntimeState / EnemyMove 시스템 사용
    private void SpawnEnemy(IReadOnlyList<Vector2Int> path)
    {
        if (mapRenderer == null) return;
        if (path == null || path.Count == 0) return;

        //우리 MapGenerator의 Vector2Int 경로를
        //정식 EnemyMove가 사용하는 PathNode[] 형태로 변경
        PathNode[] pathNodes = BuildPathNodes(path);

        if (pathNodes == null || pathNodes.Length == 0) return;

        EnemyCategory selectedCategory = GetCurrentEnemyCategory();

        EnemyDataSO selectedEnemyData = GetRandomEnemyByCategory(selectedCategory);

        if (selectedEnemyData == null) return;
        if (selectedEnemyData.EnemyPrefab == null) return;

        GameObject enemyObject = Instantiate( selectedEnemyData.EnemyPrefab, pathNodes[0].Position, Quaternion.identity );

        EnemyRuntimeState spawnEnemy = enemyObject.GetComponent<EnemyRuntimeState>();

        if (spawnEnemy == null)
        {
            Destroy(enemyObject);
            return;
        }
        
        ApplyWaveScaling(spawnEnemy);

        //정식 EnemyRuntimeState 안에 EnemyMove가 정상적으로 연결됐는지 확인
        if (spawnEnemy.Move == null)
        {
            Destroy(spawnEnemy.gameObject);
            return;
        }

        //정식 EnemyMove에 이동 경로 전달
        bool pathSet = spawnEnemy.Move.SetPath(pathNodes);

        if (!pathSet)
        {
            Destroy(spawnEnemy.gameObject);
            return;
        }

        //현재 WaveManager가 생성한 적 리스트에 등록
        spawnedEnemies.Add(spawnEnemy);

        
        //정식 SpawnedEnemyManager에도 등록
        //적이 전투로 사망했을 때 EnemyDiedEvent를 받아 제거할 수 있게 하기 위함
        SpawnedEnemyManager.Instance.RegisterEnemy(spawnEnemy);

        aliveEnemyCount++;
    }

    //웨이브 시작 메서드
    public void StartWave()
    {
        if (mapGenerator == null || stageManager == null) return;

        StopAllCoroutines();
        ClearAllEnemies();
        currentWave = 0;

        StartCoroutine(RunWaveSystem());
    }

    //코루틴으로 Wave마다 적 생성하기 -> 1웨이브를 담당
    private IEnumerator RunWave()
    {
        for (int i = 0; i < enemyCountPerWave; i++)
        {
            SpawnEnemy(mapGenerator.PathPosition);
            SpawnEnemy(mapGenerator.PathPositionB);

            yield return new WaitForSeconds(spawnInterval);
        }

        while (aliveEnemyCount > 0)
        {
            //프레임 마다 한 번씩 적생존여부 확인하기
            yield return null;
        }
    }

    //웨이브 자체를 여러번 반복시키는 웨이브 코루틴
    private IEnumerator RunWaveSystem()
    {
        for (int waveIndex = 0; waveIndex < stageManager.WavesPerStage; waveIndex++)
        {
            currentWave = waveIndex + 1;
            
            //웨이브 시작 사운드 재생
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayWaveStartSound();
            }

            // 스테이지 및 웨이브 변경 이벤트 발행과 전역 진행도 매니저 동기화
            if (StageProgressManager.Instance != null)
            {
                StageProgressManager.Instance.SetCurrentWave(currentWave);
            }
            EventBus.Publish(new StageWaveChangedEvent(stageManager.CurrentStage, currentWave));

            Debug.Log($"Stage {stageManager.CurrentStage} - Wave {currentWave} 시작");

            if (currentWave == stageManager.WavesPerStage)
            {
                Debug.Log("Boss Wave 시작");
                yield return StartCoroutine(RunBossWave());
            }
            else
            {
                yield return StartCoroutine(RunWave());
            }
            //웨이브 클리어 소리 재생
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayWaveClearSound();
            }
            
            GiveWaveClearReward();
            
            if (waveIndex < stageManager.WavesPerStage - 1)
            {
                yield return new WaitForSeconds(waveInterval);
            }
        }

        Debug.Log("모든 웨이브 종료");
        
        //스테이지 클리어 BGM 재생
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayStageClearSound();
        }
        
        //스테이지 클리어 및 전역 진행도 갱신
        int clearedStage = stageManager.CurrentStage;
        stageManager.ClearStage();

        if (StageProgressManager.Instance != null)
        {
            StageProgressManager.Instance.AdvanceToNextStage();
        }
        EventBus.Publish(new StageClearedEvent(clearedStage, (int)waveClearReward));
        EventBus.Publish(new StageWaveChangedEvent(stageManager.CurrentStage, 1));

        //맵 재생성
        mapGenerator.RegenerateMap(); 
    }

    
    
    //적이 사라질때 카운트 줄이기
    //기존 EnemyMover 대신 정식 EnemyRuntimeState를 받도록 변경
    public void EnemyRemoved(EnemyRuntimeState enemy)
    {
        if (enemy == null) return;

        //이미 제거된 적이면 중복 처리하지 않기
        if (!spawnedEnemies.Remove(enemy))
        {
            return;
        }

        //정식 SpawnedEnemyManager에서도 등록 해제
        SpawnedEnemyManager.Instance.UnregisterEnemy(enemy);

        aliveEnemyCount--;

        if (aliveEnemyCount < 0)
        {
            aliveEnemyCount = 0;
        }
    }

    private IEnumerator RunBossWave()
    {
        Debug.Log("보스웨이브 실행중");
        SpawnBoss(mapGenerator.PathPosition);

        while (aliveEnemyCount > 0) 
        {
            yield return null;
        }
    }

    private void SpawnBoss(IReadOnlyList<Vector2Int> path)
    {
        if (mapRenderer == null) return;
        if (path == null || path.Count == 0) return;

        //우리 MapGenerator의 경로를 정식 PathNode[]로 변경
        PathNode[] pathNodes = BuildPathNodes(path);

        if (pathNodes == null || pathNodes.Length == 0) return;

        //첫 번째 PathNode 위치에서 보스 생성
        EnemyDataSO selectedBossData = GetRandomEnemyByCategory(EnemyCategory.Boss);

        if (selectedBossData == null) return;
        if (selectedBossData.EnemyPrefab == null) return;

        GameObject bossObject = Instantiate( selectedBossData.EnemyPrefab, pathNodes[0].Position, Quaternion.identity );

        EnemyRuntimeState spawnBoss = bossObject.GetComponent<EnemyRuntimeState>();

        if (spawnBoss == null)
        {
            Destroy(bossObject);
            return;
        }
        
        ApplyWaveScaling(spawnBoss);

        //보스 프리팹에 EnemyMove가 정상 연결되어 있는지 확인
        if (spawnBoss.Move == null)
        {
            Destroy(spawnBoss.gameObject);
            return;
        }

        //정식 EnemyMove에 경로 전달
        bool pathSet = spawnBoss.Move.SetPath(pathNodes);

        if (!pathSet)
        {
            Debug.LogError("Boss 경로 설정에 실패했습니다.", spawnBoss);
            Destroy(spawnBoss.gameObject);
            return;
        }

        spawnedEnemies.Add(spawnBoss);

        //정식 SpawnedEnemyManager에도 등록
        SpawnedEnemyManager.Instance.RegisterEnemy(spawnBoss);

        aliveEnemyCount++;
    }

    public void RestartCurtrentStage()
    {
        //현재 돌아가는 Wave 코루틴 정지
        StopAllCoroutines();
        //현재 적 모두 삭제
        ClearAllEnemies();
        //현재 wave와 살아있는 Enemy 초기화
        currentWave = 0;

        // 전역 진행도 및 상단 UI 1웨이브 초기화 동기화
        int stage = (stageManager != null) ? stageManager.CurrentStage : 1;
        if (StageProgressManager.Instance != null)
        {
            StageProgressManager.Instance.SetCurrentWave(1);
        }
        EventBus.Publish(new StageWaveChangedEvent(stage, 1));

        //코루틴 재시작
        StartCoroutine(RunWaveSystem());
    }

    
    private void OnEnable()
    {
        CombatEvents.OnEnemyDied += HandleEnemyDied;
        CombatEvents.OnEnemyReachedGoal += HandleEnemyReachedGoal;
    }
    
    private void OnDisable()
    {
        CombatEvents.OnEnemyDied -= HandleEnemyDied;
        CombatEvents.OnEnemyReachedGoal -= HandleEnemyReachedGoal;
    }
    
//적이 전투 중 사망했을 때 처리
    private void HandleEnemyDied(EnemyDiedInfo info)
    {
        EnemyRuntimeState enemy = FindSpawnedEnemy(info.RuntimeId);

        if (enemy == null) return;

        EnemyRemoved(enemy);
    }

//적이 Goal에 도착했을 때 처리
    private void HandleEnemyReachedGoal(EnemyReachedGoalInfo info)
    {
        
        OnEnemyReachedGoal?.Invoke(info);
        
        EnemyRuntimeState enemy = FindSpawnedEnemy(info.RuntimeId);

        if (enemy == null) return;

        EnemyRemoved(enemy);

        //Goal 도착은 사망이 아니기 때문에
        //SpawnedEnemyManager가 자동으로 삭제해주지 않음
        Destroy(enemy.gameObject);
    }

//RuntimeId를 이용해서 WaveManager가 생성한 적 찾기
    private EnemyRuntimeState FindSpawnedEnemy(int runtimeId)
    {
        for (int i = 0; i < spawnedEnemies.Count; i++)
        {
            EnemyRuntimeState enemy = spawnedEnemies[i];

            if (enemy != null &&
                enemy.RuntimeId == runtimeId)
            {
                return enemy;
            }
        }

        return null;
    }
    //테스트용
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartCurtrentStage();
        }
    }

    //적 및 적 소환물 삭제/회수
    private void ClearAllEnemies()
    {
        for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
        {
            if (spawnedEnemies[i] != null)
            {
                var enemySummon = spawnedEnemies[i].GetComponent<EnemySummonRuntime>();
                if (enemySummon != null)
                {
                    SummonService.Release(spawnedEnemies[i].gameObject);
                }
                else
                {
                    //정식 SpawnedEnemyManager에서도 먼저 등록 해제
                    SpawnedEnemyManager.Instance.UnregisterEnemy(spawnedEnemies[i]);
                    Destroy(spawnedEnemies[i].gameObject);
                }
            }
        }

        spawnedEnemies.Clear();
        aliveEnemyCount = 0;
    }

    //우리 MapGenerator에서 만들어진 Vector2Int 경로를
    //정식 EnemyMove가 사용하는 PathNode[] 경로로 변환
    private PathNode[] BuildPathNodes(IReadOnlyList<Vector2Int> gridPath)
    {
        if (gridPath == null || gridPath.Count == 0)
        {
            return null;
        }

        PathNode[] pathNodes = new PathNode[gridPath.Count];

        for (int i = 0; i < gridPath.Count; i++)
        {
            Vector2Int gridPosition = gridPath[i];
            
            TileNode pathTile = mapGenerator.Grid[gridPosition.x, gridPosition.y];

            if (pathTile != null && pathTile.TileType != TileType.Path && pathTile.TileType != TileType.Spawn && pathTile.TileType != TileType.Goal) {
                Debug.LogError(
                    $"[경로 오류] 적 경로 좌표 {gridPosition}의 실제 타입이 " +
                    $"{pathTile.TileType} 입니다."
                );
            }

            //Grid 좌표를 월드 좌표로 변환
            Vector3 worldPosition = mapRenderer.GridToWorld(gridPosition);

            //적 프리팹 높이 적용
            worldPosition.y = enemyHeight;

            //현재 PathNode에서 적이 바라볼 방향 계산
            GridFacingDirection facing = ResolveFacing(gridPath, i);

            pathNodes[i] =
                new PathNode(
                    worldPosition,
                    gridPosition,
                    facing
                );
        }

        return pathNodes;
    }

    //현재 경로와 다음 경로의 차이를 이용해서
    //Enemy가 바라볼 North / East / South / West 방향 계산
    private GridFacingDirection ResolveFacing(
        IReadOnlyList<Vector2Int> path,
        int index)
    {
        if (path == null || path.Count <= 1)
        {
            return GridFacingDirection.East;
        }

        Vector2Int direction;

        //마지막 노드가 아니면 현재 위치 -> 다음 위치 방향 사용
        if (index < path.Count - 1)
        {
            direction = path[index + 1] - path[index];
        }
        //마지막 노드는 이전 위치 -> 현재 위치 방향 사용
        else
        {
            direction = path[index] - path[index - 1];
        }

        if (direction.x > 0)
        {
            return GridFacingDirection.East;
        }

        if (direction.x < 0)
        {
            return GridFacingDirection.West;
        }

        if (direction.y > 0)
        {
            return GridFacingDirection.North;
        }

        if (direction.y < 0)
        {
            return GridFacingDirection.South;
        }

        //같은 좌표가 들어오는 예외 상황에서는 기본 East
        return GridFacingDirection.East;
    }
    
    
    //적 데이터 소스에서 카타로그랑 프리펩 읽어오기
    private EnemyDataSO GetRandomEnemyByCategory(EnemyCategory category)
    {
        if (enemyCatalog == null)
        {
            Debug.LogWarning("[WaveManager] EnemyCatalog이 연결되어 있지 않습니다.");
            return null;
        }

        List<EnemyDataSO> candidates = new List<EnemyDataSO>();

        IReadOnlyList<EnemyDataSO> enemies = enemyCatalog.Enemies;

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyDataSO enemyData = enemies[i];

            if (enemyData == null)
                continue;

            if (enemyData.Category != category)
                continue;

            if (enemyData.EnemyPrefab == null)
                continue;

            candidates.Add(enemyData);
        }

        if (candidates.Count == 0)
        {
            Debug.LogWarning(
                $"[WaveManager] {category} 카테고리에 해당하는 적이 없습니다."
            );

            return null;
        }

        return candidates[Random.Range(0, candidates.Count)];
    }
    
    //적 스텟 누적 계산
    private int GetEnemyScalingLevel()
    {
        if (stageManager == null)
            return 0;

        int normalWavesPerStage = stageManager.WavesPerStage - 1;

        int scalingLevel =
            ((stageManager.CurrentStage - 1) * normalWavesPerStage)
            + (currentWave - 1);

        return Mathf.Max(0, scalingLevel);
    }
    
    //실제 적 런타임 스탯에 강화치 적용 메서드
    private void ApplyWaveScaling(EnemyRuntimeState enemy)
    {
        if (enemy == null) return;
        if (enemy.Stats == null) return;

        int scalingLevel = GetEnemyScalingLevel();

        if (scalingLevel <= 0)
            return;

        float hpBonusPercent =
            scalingLevel * hpBonusPerWavePercent;

        float attackBonusPercent =
            scalingLevel * attackBonusPerWavePercent;

        float defenseBonusPercent =
            scalingLevel * defenseBonusPerWavePercent;

        // HP
        enemy.Stats.AddModifier(
            PassiveStatType.MaxHp,
            0f,
            hpBonusPercent
        );

        // 공격력
        enemy.Stats.AddModifier(
            PassiveStatType.PhysicalAttack,
            0f,
            attackBonusPercent
        );

        enemy.Stats.AddModifier(
            PassiveStatType.MagicalAttack,
            0f,
            attackBonusPercent
        );

        // 방어력
        enemy.Stats.AddModifier(
            PassiveStatType.PhysicalDefense,
            0f,
            defenseBonusPercent
        );

        enemy.Stats.AddModifier(
            PassiveStatType.MagicalDefense,
            0f,
            defenseBonusPercent
        );

        // RuntimeStats의 MaxHp가 증가했으니까 
        // 실제 CombatHealth에도 새로운 최대 HP 반영
        if (enemy.Health != null)
        {
            enemy.Health.SetMaxHp(enemy.Stats.MaxHp);

            // 막 소환된 적이니까 증가한 MaxHp까지 체력을 채움
            enemy.Health.Heal(enemy.Stats.MaxHp);
        }

        Debug.Log(
            $"[Wave Scaling] Stage {stageManager.CurrentStage} / Wave {currentWave}" +
            $" / Level {scalingLevel}" +
            $" / HP +{hpBonusPercent}%" +
            $" / ATK +{attackBonusPercent}%" +
            $" / DEF +{defenseBonusPercent}%"
        );
    }
    
    //웨이브에 맞게 카테고리 정해주는 역할
    private EnemyCategory GetCurrentEnemyCategory()
    {
        if (currentWave < 3)
        {
            return EnemyCategory.Normal;
        }

        //3 웨이브 부터는 엘리트도 포함
        return Random.value < 0.5f
            ? EnemyCategory.Normal
            : EnemyCategory.Elite;
    }
    
   
    private void GiveWaveClearReward()
    {
        if (CurrencyManager.Instance == null)
        {
            Debug.LogWarning("[WaveManager] CurrencyManager.Instance가 없습니다.");
            return;
        }

        if (waveClearReward <= 0)
            return;

        CurrencyManager.Instance.GetWaveStone(waveClearReward);
        
        //웨이브보상 사운드 재생
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayRewardSound();
        }

        Debug.Log($"[WaveManager] Stage {stageManager.CurrentStage} / Wave {currentWave} 클리어 보상 지급: WaveStone +{waveClearReward}");
    }
    
}