using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WaveManager : MonoBehaviour
{
    [Header("맵 참조하기")] 
    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private GridMapRenderer mapRenderer;
    
    
    [Header("Wave 설정")]
    //총 웨이브 수
    [SerializeField] private int waveCount = 3;
    //한 웨이브당 적 소환 갯수
    [SerializeField] private int enemyCountPerWave = 3;
    //적 한 마리와 다음 적 소환 간격
    [SerializeField] private float spawnInterval = 1.0f;
    //웨이브와 웨이브 사이 대기 시간
    [SerializeField] private float waveInterval = 5.0f;

    [Header("적 생성 설정")] 
    [SerializeField] private EnemyMover enemyPrefab;

    

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
        spawnEnemy.Initialize(path,mapRenderer);
    }

    public void StartWave()
    {
        if(mapGenerator == null) return;
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
    }

    //웨이브 자체를 여러번 반복시키는 웨이브 코루틴
    private IEnumerator RunWaveSystem()
    {
        for (int waveIndex = 0; waveIndex < waveCount; waveIndex++)
        {
            yield return StartCoroutine(RunWave());
            if (waveIndex < waveCount - 1)
            {
                yield return new WaitForSeconds(waveInterval);
            }
        }
        Debug.Log("모든 웨이브 종료");
    }

}
