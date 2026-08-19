using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WaveManager : MonoBehaviour
{
    [Header("맵 참조하기")] 
    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private GridMapRenderer mapRenderer;
    
    [Header("Stage 참조하기")]
    [SerializeField] private StageManager stageManager;
     
    [Header("Wave 설정")]
    private int currentWave = 0;

    [Header("보스 설정")] [SerializeField] private EnemyMover bossPrefab;
   
    //한 웨이브당 적 소환 갯수
    [SerializeField] private int enemyCountPerWave = 3;
    //적 한 마리와 다음 적 소환 간격
    [SerializeField] private float spawnInterval = 1.0f;
    //웨이브와 웨이브 사이 대기 시간
    [SerializeField] private float waveInterval = 5.0f;

    [Header("적 생성 설정")] 
    [SerializeField] private EnemyMover enemyPrefab;

    //적이 살아있는지 확인
    private int aliveEnemyCount = 0;

    //적 리스트 직접 관리하기
    private List<EnemyMover> spawnedEnemies = new List<EnemyMover>();
    
    public List<Vector3> path; int CurrentWave => currentWave;

    

    //MapGenerator 에 있는 SpawnEnemy를 WaveManager로 가져오기
    private void SpawnEnemy(IReadOnlyList<Vector2Int> path)
    {
        if (enemyPrefab == null) return;
        if (mapRenderer == null) return;
        if (path == null || path.Count == 0) return;
        
        Vector2Int spawnGridPosition = path[0];
        
        Vector3 spawnWorldPOsition = mapRenderer.GridToWorld(spawnGridPosition);

        spawnWorldPOsition.y = 1.0f;
        
        EnemyMover spawnEnemy = Instantiate(enemyPrefab,spawnWorldPOsition, Quaternion.identity);

        spawnedEnemies.Add(spawnEnemy);
        
        aliveEnemyCount++;
       
        spawnEnemy.Initialize(path,mapRenderer,this);
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

        while ( aliveEnemyCount > 0)
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
    public void EnemyRemoved(EnemyMover enemy)
    {
        if (spawnedEnemies.Contains(enemy))
        {
            spawnedEnemies.Remove(enemy);
        }
        
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
        if (path == null||path.Count == 0) return;
        //좌표 받아오기
        Vector2Int spawnGridPosition = path[0];
        //받아온 좌표 월드 위치로 변환
        Vector3 spawnWorldPOsition = mapRenderer.GridToWorld(spawnGridPosition);
        //높이 조절
        spawnWorldPOsition.y = 1.0f;
        
        EnemyMover spawnBoss = Instantiate(bossPrefab,spawnWorldPOsition, Quaternion.identity);

        
        spawnedEnemies.Add(spawnBoss);    
        aliveEnemyCount++;
        spawnBoss.Initialize(path,mapRenderer,this);
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
                Destroy(spawnedEnemies[i].gameObject);
            }
        }
        spawnedEnemies.Clear();
        aliveEnemyCount = 0;
    }
}
