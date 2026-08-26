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

    [Header("보스 설정")]
    [SerializeField] private EnemyRuntimeState bossPrefab;
    
    //인스펙터에서 웨이브풀 보기
    [SerializeField] private WaveEnemyPool[] waveEnemyPools;

    //정식 EnemyMove의 SetPath()가 PathNode의 월드 좌표를 사용하기 때문에
    //적이 이동할 높이도 PathNode 생성 시 같이 적용
    [SerializeField] private float enemyHeight = 1.0f;

    //한 웨이브당 적 소환 갯수
    [SerializeField] private int enemyCountPerWave = 3;
    //적 한 마리와 다음 적 소환 간격
    [SerializeField] private float spawnInterval = 1.0f;
    //웨이브와 웨이브 사이 대기 시간
    [SerializeField] private float waveInterval = 5.0f;

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

        //첫 번째 PathNode의 위치에서 정식 적 프리팹 생성
        EnemyRuntimeState[] currentPool = GetCurrentWaveEnemyPool();

        if (currentPool == null || currentPool.Length == 0) return;
        EnemyRuntimeState selectedEnemy = currentPool[Random.Range(0, currentPool.Length)];

        if (selectedEnemy == null) return;
        
        EnemyRuntimeState spawnEnemy = Instantiate(selectedEnemy, pathNodes[0].Position, Quaternion.identity);

        //정식 EnemyRuntimeState 안에 EnemyMove가 정상적으로 연결됐는지 확인
        if (spawnEnemy.Move == null)
        {
            Debug.LogError("생성된 Enemy에 EnemyMove가 없습니다.", spawnEnemy);
            Destroy(spawnEnemy.gameObject);
            return;
        }

        //정식 EnemyMove에 이동 경로 전달
        bool pathSet = spawnEnemy.Move.SetPath(pathNodes);

        if (!pathSet)
        {
            Debug.LogError("Enemy 경로 설정에 실패했습니다.", spawnEnemy);
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
        if(mapGenerator == null) return;
        if(stageManager == null) return;

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

            if (waveIndex < stageManager.WavesPerStage - 1)
            {
                yield return new WaitForSeconds(waveInterval);
            }
        }

        Debug.Log("모든 웨이브 종료");
        //스테이지 클리어
        stageManager.ClearStage();
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
        if(bossPrefab == null) return;
        if(mapRenderer == null) return;
        if(path == null || path.Count == 0) return;

        //우리 MapGenerator의 경로를 정식 PathNode[]로 변경
        PathNode[] pathNodes = BuildPathNodes(path);

        if (pathNodes == null || pathNodes.Length == 0) return;

        //첫 번째 PathNode 위치에서 보스 생성
        EnemyRuntimeState spawnBoss =
            Instantiate(bossPrefab, pathNodes[0].Position, Quaternion.identity);

        //보스 프리팹에 EnemyMove가 정상 연결되어 있는지 확인
        if (spawnBoss.Move == null)
        {
            Debug.LogError("생성된 Boss에 EnemyMove가 없습니다.", spawnBoss);
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

    //적 삭제 해버리기
    private void ClearAllEnemies()
    {
        for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
        {
            if (spawnedEnemies[i] != null)
            {
                //정식 SpawnedEnemyManager에서도 먼저 등록 해제
                SpawnedEnemyManager.Instance.UnregisterEnemy(spawnedEnemies[i]);
                Destroy(spawnedEnemies[i].gameObject);
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
    
    //현재 웨이브 적 목록 가져오기
    private EnemyRuntimeState[] GetCurrentWaveEnemyPool()
    {
        int waveIndex = currentWave - 1;

        if (waveEnemyPools == null) return null;
        if (waveIndex < 0 || waveIndex >= waveEnemyPools.Length) return null;

        WaveEnemyPool pool = waveEnemyPools[waveIndex];

        if (pool == null) return null;

        return pool.enemyPrefabs;
    }
}