using System.Collections;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Runtime;
using UnityEngine;

public class MapGenerator : MonoBehaviour, ISummonTileProvider
{
    private const int CritSummonNeighborRadius = 1;

    [Header("Grid 크기")]
    [SerializeField, Min(1)] private int width = 12;
    [SerializeField, Min(1)] private int height = 8;

    [Header("맵")]
    [SerializeField] private GridMapRenderer mapRenderer;

    [Header("정식 몬스터 테스트")]
    [Tooltip("MAP 연동 테스트에서 생성할 정식 EnemyDataSO입니다.")]
    [SerializeField] private EnemyDataSO enemyData;

    [Tooltip("공중 몬스터의 월드 Y 높이입니다.")]
    [Min(0f)]
    [SerializeField] private float airHeight = 2f;

    [Header("자동 배치 테스트")]
    [Tooltip("정식 Ground 캐릭터 Prefab을 연결합니다.")]
    [SerializeField] private GameObject meleeUnitPrefab;

    [Tooltip("정식 HighGround 캐릭터 Prefab을 연결합니다.")]
    [SerializeField] private GameObject rangedUnitPrefab;

    [Tooltip("Ground 캐릭터 Root의 추가 Y 높이입니다.")]
    [SerializeField] private float meleeUnitHeight;

    [Tooltip("HighGround 캐릭터 Root의 추가 Y 높이입니다. 맵의 실제 언덕 높이에 맞춰 설정합니다.")]
    [SerializeField] private float rangedUnitHeight = 0.25f;

    [Header("MAP 테스트 웨이브")]
    [Min(1)]
    [SerializeField] private int enemyCountPerPath = 3;

    [Min(0.1f)]
    [SerializeField] private float enemySpawnInterval = 1f;

    private TileNode[,] grid;
    private readonly List<Vector2Int> pathPosition = new List<Vector2Int>();
    private readonly List<Vector2Int> pathPositionB = new List<Vector2Int>();
    private readonly List<Vector2Int> summonTileCandidates = new List<Vector2Int>();

    private CombatLoop combatLoop;

    public TileNode[,] Grid => grid;
    public IReadOnlyList<Vector2Int> PathPosition => pathPosition;
    public IReadOnlyList<Vector2Int> PathPositionB => pathPositionB;
    public int Width => width;
    public int Height => height;

    // 외부 소환 매니저(MapUnitSummonManager)에서 월드 좌표 변환 시 참조하기 위한 프로퍼티
    public GridMapRenderer MapRenderer => mapRenderer;

    private void Awake()
    {
        combatLoop = FindFirstObjectByType<CombatLoop>();

        if (combatLoop == null)
        {
            Debug.LogError("씬에 CombatLoop가 없습니다. 빈 GameObject 하나를 만들고 CombatLoop 컴포넌트를 1개 추가하세요.", this);
        }
    }

    private void OnEnable()
    {
        SummonTileService.Register(this);
    }

    private void OnDisable()
    {
        SummonTileService.Unregister(this);
    }

    private void Start()
    {
        GenerateMap();
    }

    // 맵 생성 완료 여부 및 이벤트 (외부 MapUnitSummonManager 연동용)
    public bool IsMapGenerated { get; private set; }
    public event System.Action OnMapGenerated;

    public void GenerateMap()
    {
        InitializeGrid();
        GenerateRandomPath();

        if (mapRenderer == null)
        {
            Debug.LogError("MapGenerator에 GridMapRenderer가 연결되지 않았습니다.", this);
            return;
        }

        mapRenderer.RenderMap(grid);

        // 맵 생성 완료 상태 처리 및 이벤트 알림
        IsMapGenerated = true;
        OnMapGenerated?.Invoke();

        if (combatLoop == null)
        {
            Debug.LogError("CombatLoop가 없어서 정식 몬스터 전투 테스트를 시작하지 않습니다.", this);
            return;
        }

        StartCoroutine(SpawnWave(pathPosition, enemyCountPerPath, enemySpawnInterval));
        StartCoroutine(SpawnWave(pathPositionB, enemyCountPerPath, enemySpawnInterval));
    }

    public void MobSpawnWave()
    {
        StartCoroutine(SpawnWave(pathPosition, enemyCountPerPath, enemySpawnInterval));
        StartCoroutine(SpawnWave(pathPositionB, enemyCountPerPath, enemySpawnInterval));
    }

    // 필드의 모든 아군 유닛과 적 유닛을 제거하고 타일 점유 해제
    public void ClearAllUnitsAndEnemies()
    {
        // 1. 진행 중인 스폰 코루틴 중단
        StopAllCoroutines();

        // 2. 타일 점유 상태 해제
        if (grid != null)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    grid[x, y]?.SetOccupied(false);
                }
            }
        }

        // 3. 적 유닛 제거
        var activeEnemies = new List<EnemyRuntimeState>(CombatRegistry.Enemies);
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null)
            {
                SpawnedEnemyManager.Instance.UnregisterEnemy(enemy);
                Destroy(enemy.gameObject);
            }
        }

        // 4. 아군 유닛 제거 (MapUnitSummonManager 추적 딕셔너리 및 이벤트 일괄 초기화)
        var summonManager = FindFirstObjectByType<EndlessGuard.Map.MapUnitSummonManager>();
        if (summonManager != null)
        {
            summonManager.ClearAllUnits();
        }

        var activeUnits = new List<UnitRuntimeState>(CombatRegistry.Units);
        foreach (var unit in activeUnits)
        {
            if (unit != null)
            {
                Destroy(unit.gameObject);
            }
        }
    }

    // 필드 유닛 청소 후 맵은 그대로 둔 채 몬스터 웨이브 재시작
    public void RestartWave()
    {
        ClearAllUnitsAndEnemies();
        MobSpawnWave();
    }

    public void InitializeGrid()
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
        pathPosition.Clear();
        pathPositionB.Clear();

        int spawnY = Random.Range(0, height);
        int spawnBY = Random.Range(0, height);
        int mergeY = Random.Range(0, height);
        int mergeX = Random.Range(1, 4);
        int goalY = Random.Range(0, height);
        int wayPoint1X = Random.Range(4, 6);
        int wayPoint1Y = Random.Range(0, height);
        int waypoint2X = Random.Range(width / 2 + 1, width - 2);
        int wayPoint2Y = Random.Range(0, height);

        while (spawnBY == spawnY)
        {
            spawnBY = Random.Range(0, height);
        }

        Vector2Int spawnPosition = new Vector2Int(0, spawnY);
        Vector2Int spawnPositionB = new Vector2Int(0, spawnBY);
        Vector2Int mergePoint = new Vector2Int(mergeX, mergeY);
        Vector2Int goalPosition = new Vector2Int(width - 1, goalY);
        Vector2Int wayPoint1 = new Vector2Int(wayPoint1X, wayPoint1Y);
        Vector2Int wayPoint2 = new Vector2Int(waypoint2X, wayPoint2Y);

        AddHorizontalPath(spawnPosition.x, mergePoint.x, spawnPosition.y, pathPosition);
        AddVerticalPath(spawnPosition.y, mergePoint.y, mergePoint.x, pathPosition);

        AddHorizontalPath(spawnPositionB.x, mergePoint.x, spawnPositionB.y, pathPositionB);
        AddVerticalPath(spawnPositionB.y, mergePoint.y, mergePoint.x, pathPositionB);

        ConnectPoints(mergePoint, wayPoint1, pathPosition);
        ConnectPoints(wayPoint1, wayPoint2, pathPosition);
        ConnectPoints(wayPoint2, goalPosition, pathPosition);

        int mergeIndex = pathPosition.IndexOf(mergePoint);

        for (int i = mergeIndex + 1; i < pathPosition.Count; i++)
        {
            AddPathPosition(pathPosition[i], pathPositionB);
        }

        bool isValidA = ValidatePath(pathPosition, spawnPosition, goalPosition);
        bool isValidB = ValidatePath(pathPositionB, spawnPositionB, goalPosition);

        if (!isValidA || !isValidB)
        {
            Debug.LogError("생성된 MAP 경로가 유효하지 않습니다.", this);
            return;
        }

        SetPathTileTypes();
        GenerateTerrain();
    }

    private void AddHorizontalPath(int startX, int endX, int y, List<Vector2Int> targetPath)
    {
        int direction = startX <= endX ? 1 : -1;

        for (int x = startX; x != endX + direction; x += direction)
        {
            AddPathPosition(new Vector2Int(x, y), targetPath);
        }
    }

    private void AddVerticalPath(int startY, int endY, int x, List<Vector2Int> targetPath)
    {
        int direction = startY <= endY ? 1 : -1;

        for (int y = startY; y != endY + direction; y += direction)
        {
            AddPathPosition(new Vector2Int(x, y), targetPath);
        }
    }

    private void ConnectPoints(Vector2Int start, Vector2Int end, List<Vector2Int> targetPath)
    {
        int connectType = Random.Range(0, 2);

        if (connectType == 0)
        {
            AddHorizontalPath(start.x, end.x, start.y, targetPath);
            AddVerticalPath(start.y, end.y, end.x, targetPath);
        }
        else
        {
            AddVerticalPath(start.y, end.y, start.x, targetPath);
            AddHorizontalPath(start.x, end.x, end.y, targetPath);
        }
    }

    private void AddPathPosition(Vector2Int position, List<Vector2Int> targetPath)
    {
        if (targetPath.Count > 0 && targetPath[targetPath.Count - 1] == position)
        {
            return;
        }

        targetPath.Add(position);
    }

    private void SetPathTileTypes()
    {
        SetSinglePathTileTypes(pathPosition);
        SetSinglePathTileTypes(pathPositionB);
    }

    private void SetSinglePathTileTypes(IReadOnlyList<Vector2Int> path)
    {
        if (path == null || path.Count < 2)
        {
            return;
        }

        for (int i = 0; i < path.Count; i++)
        {
            Vector2Int position = path[i];

            if (!IsInsideGrid(position))
            {
                continue;
            }

            if (i == 0)
            {
                grid[position.x, position.y].SetTileType(TileType.Spawn);
            }
            else if (i == path.Count - 1)
            {
                grid[position.x, position.y].SetTileType(TileType.Goal);
            }
            else
            {
                grid[position.x, position.y].SetTileType(TileType.Path);
            }
        }
    }

    private bool IsInsideGrid(Vector2Int position)
    {
        return position.x >= 0 && position.x < width && position.y >= 0 && position.y < height;
    }

    public bool TryGetTile(SummonTileRequest request, out SummonTile tile)
    {
        tile = default;

        if (grid == null || mapRenderer == null || request.Owner == null || request.SummonData == null || request.Owner.GridPosition == null || !request.Owner.GridPosition.IsInitialized)
        {
            return false;
        }

        summonTileCandidates.Clear();

        if (request.Source is CritSummonSO)
        {
            AddNearbySummonTiles(request.Owner.GridPosition.TileCoordinate, request.SummonData.Placement, CritSummonNeighborRadius);
        }
        else
        {
            AddAllSummonTiles(request.SummonData.Placement);
        }

        if (summonTileCandidates.Count == 0)
        {
            return false;
        }

        Vector2Int selectedCoordinate = summonTileCandidates[Random.Range(0, summonTileCandidates.Count)];
        TileNode selectedNode = grid[selectedCoordinate.x, selectedCoordinate.y];
        Vector3 worldPosition = GetSummonWorldPosition(selectedNode);

        tile = new SummonTile(worldPosition, selectedCoordinate);
        return true;
    }

    private void AddNearbySummonTiles(Vector2Int ownerTile, UnitPlacement placement, int radius)
    {
        int safeRadius = Mathf.Max(1, radius);

        for (int x = -safeRadius; x <= safeRadius; x++)
        {
            for (int y = -safeRadius; y <= safeRadius; y++)
            {
                if (x == 0 && y == 0)
                {
                    continue;
                }

                Vector2Int coordinate = ownerTile + new Vector2Int(x, y);
                TryAddSummonTile(coordinate, placement);
            }
        }
    }

    private void AddAllSummonTiles(UnitPlacement placement)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                TryAddSummonTile(new Vector2Int(x, y), placement);
            }
        }
    }

    private void TryAddSummonTile(Vector2Int coordinate, UnitPlacement placement)
    {
        if (!IsInsideGrid(coordinate))
        {
            return;
        }

        TileNode node = grid[coordinate.x, coordinate.y];

        if (!CanUseSummonTile(node, placement))
        {
            return;
        }

        if (IsCombatTileOccupied(coordinate))
        {
            return;
        }

        summonTileCandidates.Add(coordinate);
    }

    private bool CanUseSummonTile(TileNode node, UnitPlacement placement)
    {
        if (node == null || !node.IsDeployable || node.IsOccupied)
        {
            return false;
        }

        if (node.TileType == TileType.Empty || node.TileType == TileType.Spawn || node.TileType == TileType.Goal)
        {
            return false;
        }

        return IsPlacementAllowed(placement, node.TileType);
    }

    private static bool IsPlacementAllowed(UnitPlacement placement, TileType tileType)
    {
        bool ground = tileType == TileType.Ground || tileType == TileType.Path;
        bool highGround = tileType == TileType.HighGround;

        if (placement == UnitPlacement.Ground)
        {
            return ground;
        }

        if (placement == UnitPlacement.HighGround)
        {
            return highGround;
        }

        if (placement == UnitPlacement.GroundAndHighGround)
        {
            return ground || highGround;
        }

        return false;
    }

    private static bool IsCombatTileOccupied(Vector2Int coordinate)
    {
        foreach (UnitRuntimeState unit in CombatRegistry.Units)
        {
            if (unit == null || !unit.gameObject.activeInHierarchy || !unit.IsInitialized || unit.Health == null || unit.Health.IsDead || unit.GridPosition == null || !unit.GridPosition.IsInitialized)
            {
                continue;
            }

            if (unit.GridPosition.TileCoordinate == coordinate)
            {
                return true;
            }
        }

        foreach (EnemyRuntimeState enemy in CombatRegistry.Enemies)
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy || !enemy.IsInitialized || enemy.Health == null || enemy.Health.IsDead || enemy.GridPosition == null || !enemy.GridPosition.IsInitialized)
            {
                continue;
            }

            if (enemy.GridPosition.TileCoordinate == coordinate)
            {
                return true;
            }
        }

        return false;
    }

    private Vector3 GetSummonWorldPosition(TileNode node)
    {
        Vector3 position = mapRenderer.GridToWorld(node.GridPosition);

        if (node.TileType == TileType.HighGround)
        {
            position.y += rangedUnitHeight;
        }
        else
        {
            position.y += meleeUnitHeight;
        }

        return position;
    }

    private void SpawnEnemy(IReadOnlyList<Vector2Int> mapPath)
    {
        if (enemyData == null)
        {
            Debug.LogError("MapGenerator의 Enemy Data가 비어 있습니다.", this);
            return;
        }

        if (enemyData.EnemyPrefab == null)
        {
            Debug.LogError($"{enemyData.DisplayName} EnemyDataSO에 정식 EnemyPrefab이 연결되지 않았습니다.", enemyData);
            return;
        }

        if (mapRenderer == null || mapPath == null || mapPath.Count < 2)
        {
            return;
        }

        PathNode[] path = BuildPathNodes(mapPath, enemyData.MovementType);

        if (path == null || path.Length < 2)
        {
            Debug.LogError($"{enemyData.DisplayName} PathNode 변환에 실패했습니다.", this);
            return;
        }

        GameObject instance = Instantiate(enemyData.EnemyPrefab, path[0].Position, enemyData.EnemyPrefab.transform.rotation);
        EnemyRuntimeState state = instance.GetComponent<EnemyRuntimeState>();

        if (state == null)
        {
            Debug.LogError($"{enemyData.EnemyPrefab.name}에 EnemyRuntimeState가 없습니다.", instance);
            Destroy(instance);
            return;
        }

        if (!state.IsInitialized || state.DataLink == null || !state.DataLink.HasData)
        {
            Debug.LogError($"{enemyData.EnemyPrefab.name}의 정식 Enemy Runtime 초기화에 실패했습니다.", instance);
            Destroy(instance);
            return;
        }

        if (state.DataLink.EnemyData != enemyData)
        {
            Debug.LogError($"EnemyDataSO와 Prefab의 EnemyDataLink가 다릅니다. 요청={enemyData.DisplayName}, Prefab={state.DataLink.EnemyData.DisplayName}", instance);
            Destroy(instance);
            return;
        }

        if (state.Move == null || !state.Move.SetPath(path))
        {
            Debug.LogError($"{enemyData.DisplayName}의 EnemyMove.SetPath()에 실패했습니다.", instance);
            Destroy(instance);
            return;
        }

        SpawnedEnemyManager.Instance.RegisterEnemy(state);
    }

    private PathNode[] BuildPathNodes(IReadOnlyList<Vector2Int> mapPath, EnemyMovementType movementType)
    {
        if (mapPath == null || mapPath.Count < 2 || mapRenderer == null)
        {
            return null;
        }

        if (movementType == EnemyMovementType.Air)
        {
            Vector2Int startTile = mapPath[0];
            Vector2Int goalTile = mapPath[mapPath.Count - 1];
            GridFacingDirection facing = ResolveFacing(startTile, goalTile);
            Vector3 startPosition = mapRenderer.GridToWorld(startTile) + Vector3.up * airHeight;
            Vector3 goalPosition = mapRenderer.GridToWorld(goalTile) + Vector3.up * airHeight;

            return new[]
            {
                new PathNode(startPosition, startTile, facing),
                new PathNode(goalPosition, goalTile, facing)
            };
        }

        PathNode[] nodes = new PathNode[mapPath.Count];

        for (int i = 0; i < mapPath.Count; i++)
        {
            Vector2Int tile = mapPath[i];
            GridFacingDirection facing;

            if (i < mapPath.Count - 1)
            {
                facing = ResolveFacing(tile, mapPath[i + 1]);
            }
            else
            {
                facing = ResolveFacing(mapPath[i - 1], tile);
            }

            Vector3 worldPosition = mapRenderer.GridToWorld(tile);
            nodes[i] = new PathNode(worldPosition, tile, facing);
        }

        return nodes;
    }

    private static GridFacingDirection ResolveFacing(Vector2Int from, Vector2Int to)
    {
        Vector2Int delta = to - from;

        if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
        {
            return delta.x >= 0 ? GridFacingDirection.East : GridFacingDirection.West;
        }

        return delta.y >= 0 ? GridFacingDirection.North : GridFacingDirection.South;
    }

    private bool ValidatePath(IReadOnlyList<Vector2Int> path, Vector2Int spawnPosition, Vector2Int goalPosition)
    {
        if (path == null || path.Count < 2)
        {
            return false;
        }

        if (path[0] != spawnPosition)
        {
            return false;
        }

        return path[path.Count - 1] == goalPosition;
    }

    private IEnumerator SpawnWave(IReadOnlyList<Vector2Int> path, int enemyCount, float spawnInterval)
    {
        for (int i = 0; i < enemyCount; i++)
        {
            SpawnEnemy(path);
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void GenerateTerrain()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                TileNode node = grid[x, y];

                if (node.TileType != TileType.Empty)
                {
                    continue;
                }

                int terrainType = Random.Range(0, 2);
                node.SetTileType(terrainType == 0 ? TileType.Ground : TileType.HighGround);
            }
        }
    }

    // 외부 소환 매니저에서 배치 가능 타일을 탐색할 수 있도록 public으로 공개
    public TileNode FindRandomDeployableTile(TileType targetTileType)
    {
        List<TileNode> candidates = new List<TileNode>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                TileNode node = grid[x, y];

                if (!node.IsDeployable || node.IsOccupied || node.TileType != targetTileType)
                {
                    continue;
                }

                if (targetTileType == TileType.HighGround && !IsNearPath(node.GridPosition, 2))
                {
                    continue;
                }

                candidates.Add(node);
            }
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        return candidates[Random.Range(0, candidates.Count)];
    }

    private void SpawnMeleeUnit()
    {
        if (meleeUnitPrefab == null || mapRenderer == null)
        {
            return;
        }

        TileNode meleeTile = FindRandomDeployableTile(TileType.Path);

        if (meleeTile == null)
        {
            return;
        }

        Vector3 meleePosition = mapRenderer.GridToWorld(meleeTile.GridPosition);
        meleePosition.y += meleeUnitHeight;

        GameObject instance = Instantiate(meleeUnitPrefab, meleePosition, meleeUnitPrefab.transform.rotation);
        UnitRuntimeState state = instance.GetComponent<UnitRuntimeState>();

        if (state == null || !state.IsInitialized || state.GridPosition == null)
        {
            Debug.LogError($"{meleeUnitPrefab.name}에 정상적인 UnitRuntimeState가 없습니다.", instance);
            Destroy(instance);
            return;
        }

        state.GridPosition.Initialize(meleeTile.GridPosition, GridFacingDirection.East, CombatTargetLayer.Ground);
        meleeTile.SetOccupied(true);
    }

    private void SpawnRangedUnit()
    {
        if (rangedUnitPrefab == null || mapRenderer == null)
        {
            return;
        }

        TileNode rangedTile = FindRandomDeployableTile(TileType.HighGround);

        if (rangedTile == null)
        {
            return;
        }

        Vector3 rangedPosition = mapRenderer.GridToWorld(rangedTile.GridPosition);
        rangedPosition.y += rangedUnitHeight;

        GameObject instance = Instantiate(rangedUnitPrefab, rangedPosition, rangedUnitPrefab.transform.rotation);
        UnitRuntimeState state = instance.GetComponent<UnitRuntimeState>();

        if (state == null || !state.IsInitialized || state.GridPosition == null)
        {
            Debug.LogError($"{rangedUnitPrefab.name}에 정상적인 UnitRuntimeState가 없습니다.", instance);
            Destroy(instance);
            return;
        }

        state.GridPosition.Initialize(rangedTile.GridPosition, GridFacingDirection.East, CombatTargetLayer.Ground);
        rangedTile.SetOccupied(true);
    }

    // 외부 타일 검사 로직에서 이동 경로 인접 여부를 확인할 수 있도록 public으로 공개
    public bool IsNearPath(Vector2Int position, int maxDistance)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                TileNode node = grid[x, y];

                if (node.TileType != TileType.Path)
                {
                    continue;
                }

                int distance = Mathf.Abs(position.x - x) + Mathf.Abs(position.y - y);

                if (distance <= maxDistance)
                {
                    return true;
                }
            }
        }

        return false;
    }
}