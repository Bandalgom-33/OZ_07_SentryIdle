using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Runtime;
using UnityEngine;
using Random = UnityEngine.Random;

namespace EndlessGuard.TestBattle
{
    /// <summary>
    /// 테스트용 맵 격자 및 적 이동 경로를 동적으로 생성하고,
    /// Unit 전투 이동 시스템(EnemyMove)에 필요한 PathNode[] 데이터 변환을 지원하는 맵 생성기 클래스
    /// </summary>
    public class TestMapGenerator : MonoBehaviour
    {
        #region 인스펙터 직렬화 필드

        [Header("--- 격자(Grid) 크기 설정 ---")]
        [Tooltip("맵의 가로 타일 수")]
        [SerializeField, Min(4)] private int width = 12;

        [Tooltip("맵의 세로 타일 수")]
        [SerializeField, Min(4)] private int height = 8;

        [Header("--- 맵 렌더러 참조 ---")]
        [Tooltip("타일 오브젝트 인스턴스화 및 그리드-월드 좌표 변환을 담당하는 렌더러")]
        [SerializeField] private GridMapRenderer mapRenderer;

        [Header("--- 지형 생성 비율 ---")]
        [Tooltip("고지대(원거리 배치용) 타일 생성 확률 (기본 25%)")]
        [Range(0.1f, 0.5f)]
        [SerializeField] private float highGroundRatio = 0.25f;

        [Tooltip("경로 인근 고지대 타일 최소 요구 수량 (부족 시 재추첨)")]
        [SerializeField] private int minHighGroundNearPath = 4;

        #endregion

        #region 내부 런타임 데이터 및 프로퍼티

        // 논리 타일 격자 2차원 배열
        private TileNode[,] _grid;

        // 첫 번째 스폰 지점(Spawn A) 경로 좌표 목록
        private readonly List<Vector2Int> _pathPositionA = new List<Vector2Int>();

        // 두 번째 스폰 지점(Spawn B) 경로 좌표 목록
        private readonly List<Vector2Int> _pathPositionB = new List<Vector2Int>();

        // Unit 시스템 EnemyMove에 전달할 PathNode 캐시 배열
        private PathNode[] _cachedPathNodesA;
        private PathNode[] _cachedPathNodesB;

        // 맵 생성 완료 여부 플래그
        public bool IsMapGenerated { get; private set; }

        // 맵 생성 완료 이벤트 (아군 소환 및 웨이브 매니저 동기화용)
        public event Action OnMapGenerated;

        public TileNode[,] Grid => _grid;
        public int Width => width;
        public int Height => height;
        public GridMapRenderer MapRenderer => mapRenderer;
        public IReadOnlyList<Vector2Int> PathPositionA => _pathPositionA;
        public IReadOnlyList<Vector2Int> PathPositionB => _pathPositionB;
        public PathNode[] PathNodesA => _cachedPathNodesA;
        public PathNode[] PathNodesB => _cachedPathNodesB;

        #endregion

        #region 라이프사이클

        private void Awake()
        {
            // 인스펙터 미할당 시 씬 내 GridMapRenderer 자동 탐색
            if (mapRenderer == null)
            {
                mapRenderer = FindFirstObjectByType<GridMapRenderer>();
            }
        }

        #endregion

        #region 맵 생성 핵심 로직

        /// <summary>
        /// 맵 격자를 초기화하고, 2방향 랜덤 경로를 계산한 뒤 지형을 렌더링합니다.
        /// </summary>
        public void GenerateMap()
        {
            IsMapGenerated = false;

            // 1. 2차원 그리드 노드 배열 초기화
            InitializeGrid();

            // 2. 2개 스폰 지점에서 Goal로 합류하는 랜덤 경로 생성
            GenerateRandomDualPath();

            // 3. 원거리 캐릭터 배치를 위한 고지대 타일이 경로 주변에 충분한지 검증 (부족 시 재추첨)
            if (!HasEnoughHighGroundNearPath(minHighGroundNearPath))
            {
                GenerateMap();
                return;
            }

            // 4. GridMapRenderer를 통한 타일 오브젝트 씬 렌더링
            if (mapRenderer != null)
            {
                mapRenderer.ClearMap();
                mapRenderer.RenderMap(_grid);
            }

            // 5. EnemyMove 컴포넌트에 주입할 PathNode[] 배열 변환 및 캐싱
            _cachedPathNodesA = ConvertToPathNodes(_pathPositionA);
            _cachedPathNodesB = ConvertToPathNodes(_pathPositionB);

            // 6. 맵 생성 완료 상태 설정 및 이벤트 발행
            IsMapGenerated = true;
            OnMapGenerated?.Invoke();

            Debug.Log($"[TestMapGenerator] 맵 생성 완료: {width}x{height} 그리드, 경로A 노드 수: {_pathPositionA.Count}, 경로B 노드 수: {_pathPositionB.Count}");
        }

        /// <summary>
        /// 2차원 격자 배열을 생성하고 모든 타일을 Empty 상태로 초기화합니다.
        /// </summary>
        private void InitializeGrid()
        {
            _grid = new TileNode[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    _grid[x, y] = new TileNode(new Vector2Int(x, y));
                }
            }
        }

        /// <summary>
        /// 스폰 A와 스폰 B에서 시작하여 병합점(Merge Point)을 거쳐 골 지점으로 향하는 2방향 랜덤 경로를 생성합니다.
        /// </summary>
        private void GenerateRandomDualPath()
        {
            _pathPositionA.Clear();
            _pathPositionB.Clear();

            // 시작 지점 Y좌표 무작위 선별 (서로 다른 라인 보장)
            int spawnAY = Random.Range(0, height);
            int spawnBY = Random.Range(0, height);
            while (spawnBY == spawnAY && height > 1)
            {
                spawnBY = Random.Range(0, height);
            }

            // 병합 지점(MergePoint) 좌표 선정 (X: 1~3 구간, Y: 무작위)
            int mergeX = Random.Range(1, Mathf.Min(4, width - 2));
            int mergeY = Random.Range(0, height);

            // 1차 경유지(Waypoint1: X 4~5), 2차 경유지(Waypoint2: X 중간~골 이전), 골 지점(Goal: X=width-1)
            int way1X = Random.Range(Mathf.Min(4, width - 3), Mathf.Min(6, width - 2));
            int way1Y = Random.Range(0, height);

            int way2X = Random.Range(width / 2 + 1, width - 1);
            int way2Y = Random.Range(0, height);

            int goalY = Random.Range(0, height);

            Vector2Int spawnPosA = new Vector2Int(0, spawnAY);
            Vector2Int spawnPosB = new Vector2Int(0, spawnBY);
            Vector2Int mergePoint = new Vector2Int(mergeX, mergeY);
            Vector2Int wayPoint1 = new Vector2Int(way1X, way1Y);
            Vector2Int wayPoint2 = new Vector2Int(way2X, way2Y);
            Vector2Int goalPos = new Vector2Int(width - 1, goalY);

            // 1. SpawnA -> MergePoint 경로 생성
            AddHorizontalPath(spawnPosA.x, mergePoint.x, spawnPosA.y, _pathPositionA);
            AddVerticalPath(spawnPosA.y, mergePoint.y, mergePoint.x, _pathPositionA);

            // 2. SpawnB -> MergePoint 경로 생성
            AddHorizontalPath(spawnPosB.x, mergePoint.x, spawnPosB.y, _pathPositionB);
            AddVerticalPath(spawnPosB.y, mergePoint.y, mergePoint.x, _pathPositionB);

            // 3. MergePoint -> WayPoint1 -> WayPoint2 -> Goal 공통 경로 생성 (경로 A에 구축)
            ConnectPoints(mergePoint, wayPoint1, _pathPositionA);
            ConnectPoints(wayPoint1, wayPoint2, _pathPositionA);
            ConnectPoints(wayPoint2, goalPos, _pathPositionA);

            // 4. 병합 지점 이후의 공통 경로를 경로 B에도 복사
            int mergeIndex = _pathPositionA.IndexOf(mergePoint);
            if (mergeIndex >= 0)
            {
                for (int i = mergeIndex + 1; i < _pathPositionA.Count; i++)
                {
                    AddPathPosition(_pathPositionA[i], _pathPositionB);
                }
            }

            // 5. 생성된 경로 타일의 Type 지정 (Spawn, Path, Goal)
            SetPathTileTypes(_pathPositionA);
            SetPathTileTypes(_pathPositionB);

            // 6. 경로 외 나머지 Empty 타일에 고지대(HighGround) 및 지상(Ground) 지형 롤링
            GenerateTerrain();
        }

        #endregion

        #region 경로 연결 헬퍼 로직

        // 가로 방향 경로 연결
        private void AddHorizontalPath(int startX, int endX, int y, List<Vector2Int> targetPath)
        {
            int direction = startX <= endX ? 1 : -1;
            for (int x = startX; x != endX + direction; x += direction)
            {
                AddPathPosition(new Vector2Int(x, y), targetPath);
            }
        }

        // 세로 방향 경로 연결
        private void AddVerticalPath(int startY, int endY, int x, List<Vector2Int> targetPath)
        {
            int direction = startY <= endY ? 1 : -1;
            for (int y = startY; y != endY + direction; y += direction)
            {
                AddPathPosition(new Vector2Int(x, y), targetPath);
            }
        }

        // 두 점을 가로/세로 우선순위를 무작위로 선택하여 꺾인 경로로 연결
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

        // 중복 좌표 추가 방지 헬퍼
        private void AddPathPosition(Vector2Int position, List<Vector2Int> targetPath)
        {
            if (targetPath.Count > 0 && targetPath[targetPath.Count - 1] == position)
            {
                return;
            }
            targetPath.Add(position);
        }

        // 경로상의 격자 노드에 TileType(Spawn, Path, Goal) 부여
        private void SetPathTileTypes(IReadOnlyList<Vector2Int> path)
        {
            if (path == null || path.Count < 2) return;

            for (int i = 0; i < path.Count; i++)
            {
                Vector2Int pos = path[i];
                if (!IsInsideGrid(pos)) continue;

                if (i == 0)
                {
                    _grid[pos.x, pos.y].SetTileType(TileType.Spawn);
                }
                else if (i == path.Count - 1)
                {
                    _grid[pos.x, pos.y].SetTileType(TileType.Goal);
                }
                else
                {
                    // 기존에 Spawn이나 Goal로 지정된 타일이 아니라면 Path로 설정
                    if (_grid[pos.x, pos.y].TileType != TileType.Spawn && _grid[pos.x, pos.y].TileType != TileType.Goal)
                    {
                        _grid[pos.x, pos.y].SetTileType(TileType.Path);
                    }
                }
            }
        }

        // 경로 외 빈 타일에 고지대(HighGround, 25%) 및 일반 지상(Ground, 75%) 무작위 배치
        private void GenerateTerrain()
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    TileNode node = _grid[x, y];
                    if (node.TileType != TileType.Empty) continue;

                    if (Random.value < highGroundRatio)
                    {
                        node.SetTileType(TileType.HighGround);
                    }
                    else
                    {
                        node.SetTileType(TileType.Ground);
                    }
                }
            }
        }

        #endregion

        #region PathNode 변환 유틸리티 (Unit 전투 이동 시스템 연동)

        /// <summary>
        /// Vector2Int 격자 경로 리스트를 Unit 시스템의 EnemyMove가 요구하는 PathNode[] 구조체 배열로 변환합니다.
        /// 각 노드 간의 차이 벡터를 계산하여 올바른 진행 방향(GridFacingDirection)을 주입합니다.
        /// </summary>
        public PathNode[] ConvertToPathNodes(IReadOnlyList<Vector2Int> gridPath)
        {
            if (gridPath == null || gridPath.Count == 0 || mapRenderer == null)
            {
                return Array.Empty<PathNode>();
            }

            PathNode[] result = new PathNode[gridPath.Count];

            for (int i = 0; i < gridPath.Count; i++)
            {
                Vector2Int currentTile = gridPath[i];
                Vector3 worldPos = mapRenderer.GridToWorld(currentTile);

                // 진행 방향 결정 (다음 노드가 있으면 다음 노드를 향하고, 마지막 노드는 이전 노드의 방향 유지)
                GridFacingDirection facing = GridFacingDirection.East;
                if (i < gridPath.Count - 1)
                {
                    Vector2Int delta = gridPath[i + 1] - currentTile;
                    facing = CalculateFacingDirection(delta);
                }
                else if (i > 0)
                {
                    Vector2Int delta = currentTile - gridPath[i - 1];
                    facing = CalculateFacingDirection(delta);
                }

                result[i] = new PathNode(worldPos, currentTile, facing);
            }

            return result;
        }

        /// <summary>
        /// 좌표 변화량(Delta)을 바탕으로 동/서/남/북 그리드 방향을 산출합니다.
        /// </summary>
        public static GridFacingDirection CalculateFacingDirection(Vector2Int delta)
        {
            if (delta.x > 0) return GridFacingDirection.East;
            if (delta.x < 0) return GridFacingDirection.West;
            if (delta.y > 0) return GridFacingDirection.North;
            if (delta.y < 0) return GridFacingDirection.South;
            return GridFacingDirection.East;
        }

        #endregion

        #region 타일 검색 및 검증 인터페이스

        /// <summary>
        /// 조건에 맞는(배치 가능, 미점유, 지정 타일 타입) 랜덤 타일을 검색하여 반환합니다.
        /// </summary>
        public TileNode FindRandomDeployableTile(TileType targetTileType)
        {
            if (_grid == null) return null;

            List<TileNode> candidates = new List<TileNode>();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    TileNode node = _grid[x, y];
                    if (node.IsDeployable && !node.IsOccupied && node.TileType == targetTileType)
                    {
                        // 고지대 유닛의 경우 공격 유효 사거리를 위해 적 경로에서 2칸 이내인 타일 우선 필터링
                        if (targetTileType == TileType.HighGround && !IsNearPath(node.GridPosition, 2))
                        {
                            continue;
                        }
                        candidates.Add(node);
                    }
                }
            }

            if (candidates.Count == 0) return null;
            return candidates[Random.Range(0, candidates.Count)];
        }

        /// <summary>
        /// 특정 좌표에서 지정 거리 내에 적 이동 경로(Path)가 존재하는지 검사합니다.
        /// </summary>
        public bool IsNearPath(Vector2Int position, int maxDistance)
        {
            if (_grid == null) return false;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    TileNode node = _grid[x, y];
                    if (node.TileType != TileType.Path) continue;

                    int distance = Mathf.Abs(position.x - x) + Mathf.Abs(position.y - y);
                    if (distance <= maxDistance) return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 특정 좌표가 그리드 유효 범위 내부인지 확인합니다.
        /// </summary>
        public bool IsInsideGrid(Vector2Int position)
        {
            return position.x >= 0 && position.x < width && position.y >= 0 && position.y < height;
        }

        /// <summary>
        /// 경로 주변에 고지대 타일이 최소 요구치 이상 존재하는지 검증합니다.
        /// </summary>
        private bool HasEnoughHighGroundNearPath(int minimumCount)
        {
            int count = 0;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    TileNode node = _grid[x, y];
                    if (node.TileType == TileType.HighGround && IsNearPath(node.GridPosition, 2))
                    {
                        count++;
                    }
                }
            }
            return count >= minimumCount;
        }

        /// <summary>
        /// 공중 몬스터를 위해 시작 지점부터 골 지점까지 공중 높이(airHeight)로 떠서 직통 이동하는 PathNode 배열을 생성합니다.
        /// </summary>
        public PathNode[] BuildAirPath(float airHeight, Vector2Int startTile, Vector2Int goalTile)
        {
            Vector3 startPos = mapRenderer != null ? mapRenderer.GridToWorld(startTile) : new Vector3(startTile.x, 0f, startTile.y);
            startPos.y = airHeight;

            Vector3 goalPos = mapRenderer != null ? mapRenderer.GridToWorld(goalTile) : new Vector3(goalTile.x, 0f, goalTile.y);
            goalPos.y = airHeight;

            GridFacingDirection facing = CalculateFacingDirection(goalTile - startTile);

            return new PathNode[]
            {
                new PathNode(startPos, startTile, facing),
                new PathNode(goalPos, goalTile, facing)
            };
        }

        /// <summary>
        /// 맵 전체를 초기화하고 재생성합니다.
        /// </summary>
        public void RegenerateMap()
        {
            if (mapRenderer != null)
            {
                mapRenderer.ClearMap();
            }
            GenerateMap();
        }

        #endregion
    }
}
