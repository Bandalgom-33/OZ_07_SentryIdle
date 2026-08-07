using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;

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
    //첫 번째 spawn 경로
    private List<Vector2Int> pathPosition = new List<Vector2Int>();
    //두 번째 spawn 경로
    private List<Vector2Int> pathPositionB = new List<Vector2Int>();
   

    public TileNode[,] Grid => grid;
    public IReadOnlyList<Vector2Int> PathPosition => pathPosition;
    public IReadOnlyList<Vector2Int> PathPositionB => pathPositionB;
    public int Width => width;
    public int Height => height;

    void Start()
    {
        GenerateMap();
    }


    public void GenerateMap()
    {
        InitializedGrid();
        //고정 경로 생성은 주석처리
        // GenerateFixedPath();
        GenerateRandomPath();

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


    private void GenerateRandomPath()
    {
        ////좌표 초기화
        pathPosition.Clear();
        pathPositionB.Clear();

        //첫 번째 spawnY 지점
        int spawnY = Random.Range(0, height);
        //두 번째 spawnBY 지점
        int spawnBY = Random.Range(0, height);

        //두 spawn이 만나는 MergePointY 좌표
        int mergeY = Random.Range(0, height);
        //X 좌표  -> 첫 번째 경유지와 겹치지 않게 수정 했음
        int mergeX = Random.Range(1, 4);

        //goalY 지점
        int goalY = Random.Range(0, height);



        //첫 번째 경우지
        //첫 번째 경우지의 X 는 4, 5 로 고정 -> 머지 포인트를 위해 수정 했음 
        int wayPoint1X = Random.Range(4, 6);
        int wayPoint1Y = Random.Range(0, height);

        ////두 번째 경유지
        //// 두번재 경우지는 Goal.x인 11을 피하고 바로 앞인 10도 피해야 하기 때문에
        ////7,8,9 중에 하나
        int waypoint2X = Random.Range(width / 2 + 1, width - 2);
        int wayPoint2Y = Random.Range(0, height);

        while(spawnBY == spawnY)
        {
            spawnBY = Random.Range(0, height);
        }

        ////첫 번째 랜덤 입구 좌표
        Vector2Int spawnPosition = new Vector2Int(0, spawnY);
        //두 번째 랜덤 입구 좌표
        Vector2Int spawnPositionB = new Vector2Int(0, spawnBY);
        //두 입구가 만나는 지점
        Vector2Int mergePoint = new Vector2Int(mergeX, mergeY);
        //랜덤 출구 좌표
        Vector2Int goalPosition = new Vector2Int(width - 1, goalY);
        //첫 번째 경유지 좌표
        Vector2Int wayPoint1 = new Vector2Int(wayPoint1X, wayPoint1Y);
        //두 번째 경우지 자표
        Vector2Int wayPoint2 = new Vector2Int(waypoint2X, wayPoint2Y);


        //// Spawn → Waypoint1 경로 이어주기 
        //AddHorizontalPath(spawnPosition.x,wayPoint1.x,spawnPosition.y );
        //AddVerticalPath( spawnPosition.y, wayPoint1.y, wayPoint1.x);

        //// Waypoint1 → Waypoint2
        //AddHorizontalPath(wayPoint1.x, wayPoint2.x,wayPoint1.y);
        //AddVerticalPath( wayPoint1.y, wayPoint2.y,wayPoint2.x);

        //// Waypoint2 → Goal
        //AddHorizontalPath(wayPoint2.x, goalPosition.x, wayPoint2.y );
        //AddVerticalPath( wayPoint2.y, goalPosition.y, goalPosition.x);

        //SpawnA -> mergePoint
        //이게 머지 포인트를 랜덤으로 잡으니까 경로가 이상해져서 일단 스폰 -> 머지포인트 까진 고정 경로로 해봄
        // ConnectPoints(spawnPosition,mergePoint, pathPosition);
        AddHorizontalPath(spawnPosition.x, mergePoint.x, spawnPosition.y, pathPosition);
         AddVerticalPath(spawnPosition.y, mergePoint.y, mergePoint.x, pathPosition);

        //spawnB -> mergePoint
        //위와 동일
        // ConnectPoints(spawnPositionB, mergePoint, pathPositionB);
        AddHorizontalPath( spawnPositionB.x,mergePoint.x,spawnPositionB.y,pathPositionB);
        AddVerticalPath(spawnPositionB.y, mergePoint.y,mergePoint.x, pathPositionB);


        //mergePoint -> wayPoint1
        ConnectPoints(mergePoint, wayPoint1, pathPosition);
        //WayPoint1 -> 2
        ConnectPoints(wayPoint1,wayPoint2, pathPosition);
        //Waypoint2 -> Goal
        ConnectPoints(wayPoint2, goalPosition, pathPosition);

        //A 경로에서 Merge 위치 찾기
        int mergeIndex = pathPosition.IndexOf(mergePoint);
        //Merge 이후에 공통 경로를 B에 복사하기
        for (int i = mergeIndex + 1; i < pathPosition.Count; i++)
        {
            AddPathPosition(pathPosition[i], pathPositionB);
        }


        bool isValid = ValidatePath(PathPosition,spawnPosition,goalPosition);
        bool isValidB = ValidatePath(PathPositionB, spawnPositionB, goalPosition);
        if (!isValid || !isValidB)return;



        SetPathTileTypes();
    }

    //고정 경로 주석처리
    //private void GenerateFixedPath()
    //{
    //    pathPosition.Clear();

    //    AddHorizontalPath(0, 4, 3);
    //    AddVerticalPath(3, 5, 4);
    //    AddHorizontalPath(4, 8, 5);
    //    AddVerticalPath(5, 2, 8);
    //    AddHorizontalPath(8, 11, 2);

    //    SetPathTileTypes();
    //}

    //가로 연결 메서드
    private void AddHorizontalPath(int startX,int endX, int y, List<Vector2Int> targetPath)
    {
        //이동 방향이 왼쪽 오른쪽 모두 가능하기 때문에 방향이 필요함
        //시작 X가 끝X 보다 작거나 같으면? -> 1씩 증가
        //시작X가 끝 X보다 크면? -> -1씩 감소
        int direction = startX <= endX ? 1 : -1;

        for(int x=  startX; x < endX + direction;x += direction)
        {
            AddPathPosition(new Vector2Int(x, y), targetPath);
        }
    }
    //세로 연결 메서드 
    private void AddVerticalPath (int startY, int endY, int x, List<Vector2Int> targetPath)
    {
        int direction = startY <= endY ? 1 : -1;

        for(int y= startY; y != endY + direction; y += direction)
        {
            AddPathPosition(new Vector2Int(x, y),targetPath);
        }
    }

    //두 점을 연결하는 공통 메서드 만들기
    private void ConnectPoints(Vector2Int start,  Vector2Int end,List<Vector2Int> targetPath)
    {
        //0이 나오면 세로부터 1이 나오면 가로부터
        int connectType = Random.Range(0, 2);
     

        if(connectType == 0 )
        {
            //가로 -> 세로
            AddHorizontalPath(start.x, end.x, start.y, targetPath);
            AddVerticalPath(start.y,end.y, end.x, targetPath);
        }
        else if(connectType == 1 )
        {
            //세로 -> 가로
            AddVerticalPath(start.y,end.y, start.x, targetPath);
            AddHorizontalPath(start.x,end.x, end.y, targetPath);  
        }
    }


    private void AddPathPosition(Vector2Int position, List<Vector2Int> targetPath)
    {
        //만약 리스트에 마지막 좌표와 새로 넣으려는 좌표가 같으면 추가 ㄴㄴ
        if (targetPath.Count > 0 && targetPath[targetPath.Count - 1] == position) return;

        targetPath.Add(position);
    }
  

    private void SetPathTileTypes()
    {
        SetSinglePathTileTypes(pathPosition);
        SetSinglePathTileTypes(pathPositionB);
    }
    //타일을 색으로 구분하기
   private void SetSinglePathTileTypes(IReadOnlyList<Vector2Int> path)
    {
        if (path.Count < 2) return;

        for(int i = 0; i < path.Count; i++)
        {
            Vector2Int position = path[i];

            if (!IsInsideGrid(position)) continue;

            if (i == 0)
            {
                //첫 번째 좌표는 Start
                grid[position.x, position.y].SetTileType(TileType.Spawn);
            }
            else if (i == path.Count - 1)
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

    //pathPosition의 유효성을 검사하는 메서드
    private bool ValidatePath(IReadOnlyList<Vector2Int>path,Vector2Int spawnPosition, Vector2Int goalPosition)
    {
        if (path.Count < 2) return false;
        if (path[0] != spawnPosition) return false;
        if (path[path.Count - 1] != goalPosition) return false;

        return true;

    }


}
