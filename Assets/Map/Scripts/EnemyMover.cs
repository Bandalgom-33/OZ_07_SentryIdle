using UnityEngine;
using System.Collections.Generic;


public class EnemyMover : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2.0f;
  

    //Enemy가 따라갈 Grid 좌표 목록
    private IReadOnlyList<Vector2Int> path;
    //현재 몇번째 인지 저장
    private int currentPathIndex;
    //Grid 좌표를 World 위치치로 변환하기 위해 사용
    private GridMapRenderer mapRenderer;
    
    private WaveManager waveManager;


    public void Initialize(IReadOnlyList<Vector2Int> newPath, GridMapRenderer newMapRenderer, WaveManager newWaveManager)
    {
        path = newPath;
        mapRenderer = newMapRenderer;
        waveManager = newWaveManager;
        currentPathIndex = 0;
    }

    void Update()
    {
        //경로 없는 상태 체크
        if (path == null || mapRenderer == null || path.Count == 0) return;   
        //인덱스 경로 범위 체크
        if(currentPathIndex >= path.Count) return;

        //현재 인덱스를 이용해서 향할 목표 좌표를 하나 받아서
        Vector2Int targetGridPosition = path[currentPathIndex];
        // 받은 Vector2Int를  World 위치로 변환
        Vector3 targetWorldPosition = mapRenderer.GridToWorld(targetGridPosition);
        //적의 y위치(높이)를 맞춰주자
        targetWorldPosition.y = transform.position.y;
        float moveDistance = moveSpeed * Time.deltaTime;
        //새로운 위치 계산
        Vector3 newPosition = Vector3.MoveTowards(transform.position, targetWorldPosition, moveDistance);
        
        transform.position = newPosition;

        //거리 계산
        float distance = Vector3.Distance(transform.position, targetWorldPosition);
        //도착하면 인덱스 증가
        if(distance < 0.01f)
        {
            currentPathIndex++;
            //현재 인덱스가 경로 개수보다 크면
            if(currentPathIndex >= path.Count)
            {
                EnemyArrivedExit();
            }
        }
    }

    //적 삭제 메서드 이건 추후에 적 도착시 체력 깍이는 로직 추가 예정
    private void EnemyArrivedExit()
    {
        
        if (waveManager != null)
        {
            waveManager.EnemyRemoved(this);
        }
        
        Destroy(gameObject);
    }
  
}
