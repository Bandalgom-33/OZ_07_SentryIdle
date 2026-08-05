using UnityEngine;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour
{
    [Header("Grid 크기")]
    [SerializeField, Min(1)] private int width = 12;
    [SerializeField, Min(1)] private int height = 8;

    [Header("맵 크기")]
    [SerializeField] private GridMapRenderer mapRenderer;

    [Header("적 생성 설정")]
    [SerializeField] private EnemyMover enemyPrefab;

    private TileNode[,] grid;
    private List<Vector2Int> pathPosition = new List<Vector2Int>();

    public TileNode[,] Grid => grid;
    public IReadOnlyList<Vector2Int> PathPosition => pathPosition;
    public int Width => width;
    public int Height => height;

    void Start()
    {
        GenerateMap();
    }


    public void GenerateMap()
    {
        InitializedGrid();
        GenerateFixedPath();

        if (mapRenderer == null) return;
        mapRenderer.RenderMap(grid);

        SpawnEnemy();
    }


    public void InitializedGrid()
    {
        grid = new TileNode[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int gridPosition = new Vector2Int(x, y);
                grid[x, y] = new TileNode(gridPosition);
            }
        }
    }


    private void GenerateFixedPath()
    {
        pathPosition.Clear();

        AddHorizontalPath(0, 4, 3);
        AddVerticalPath(3, 5, 4);
        AddHorizontalPath(4, 8, 5);
        AddVerticalPath(5, 2, 8);
        AddHorizontalPath(8, 11, 2);

        SetPathTileTypes();
    }

    private void AddHorizontalPath(int startX,int endX, int y)
    {
        //이동 방향이 왼쪽 오른쪽 모두 가능하기 때문에 방향이 필요함
        //시작 X가 끝X 보다 작거나 같으면? -> 1씩 증가
        //시작X가 끝 X보다 크면? -> -1씩 감소
        int direction = startX <= endX ? 1 : -1;

        for(int x=  startX; x < endX + direction;x += direction)
        {
            AddPathPosition(new Vector2Int(x, y));
        }
    }

    private void AddVerticalPath (int startY, int endY, int x)
    {
        int direction = startY <= endY ? 1 : -1;

        for(int y= startY; y != endY + direction; y += direction)
        {
            AddPathPosition(new Vector2Int(x, y));
        }
    }

    private void AddPathPosition(Vector2Int position)
    {
        //만약 리스트에 마지막 좌표와 새로 넣으려는 좌표가 같으면 추가 ㄴㄴ
        if (pathPosition.Count > 0 && pathPosition[pathPosition.Count - 1] == position) return;

        pathPosition.Add(position);
    }
  

   private void SetPathTileTypes()
    {
        if (pathPosition.Count < 2) return;

        for(int i = 0; i < pathPosition.Count; i++)
        {
            Vector2Int position = pathPosition[i];

            if (!IsInsideGrid(position)) continue;

            if (i == 0)
            {
                //첫 번째 좌표는 Start
                grid[position.x, position.y].SetTileType(TileType.Spawn);
            }
            else if (i == pathPosition.Count - 1)
            {
                //마지막 좌표는 Goal
                grid[position.x, position.y].SetTileType(TileType.Goal);
            }
            else
            {
                //나머지 좌표는 path
                grid[position.x,position.y].SetTileType(TileType.Path);
            }
        }
    }

    //Grid 범위 확인 메서드
    //우리가 만들 12x8 맵이면 
    //x -> 0~11 / y -> 0~7 구간을 확인
    private bool IsInsideGrid(Vector2Int position)
    {
        return position.x >= 0&&
            position.x < width &&
            position.y >= 0 &&
            position.y < height;
    }

    
    private void SpawnEnemy()
    {
        if (enemyPrefab == null) return;
        if(pathPosition == null || pathPosition.Count == 0) return;

        //스폰 포지션 좌표 받기
        Vector2Int spawnGridPosition = pathPosition[0];
        //World 위치로 변환
        Vector3 spawnWorldPosition =   mapRenderer.GridToWorld(spawnGridPosition);
        spawnWorldPosition.y = 1.0f;
        //생성 시키기
        EnemyMover spawnEnemy = Instantiate(enemyPrefab,spawnWorldPosition,Quaternion.identity);
        spawnEnemy.Initialize(pathPosition, mapRenderer);
    }




}
