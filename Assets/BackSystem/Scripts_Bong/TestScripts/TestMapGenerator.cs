using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Runtime;
using UnityEngine;
using Random = UnityEngine.Random;

namespace EndlessGuard.TestBattle
{
    // 테스트용 격자 맵 및 적 이동 경로 동적 생성/PathNode 변환 컴포넌트
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

        private TileNode[,] _grid;
        private readonly List<Vector2Int> _pathPositionA = new List<Vector2Int>();
        private readonly List<Vector2Int> _pathPositionB = new List<Vector2Int>();
        private PathNode[] _cachedPathNodesA;
        private PathNode[] _cachedPathNodesB;

        public bool IsMapGenerated { get; private set; }
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

        // 맵 렌더러 컴포넌트 자동 탐색
        private void Awake()
        {
            if (mapRenderer == null)
            {
                mapRenderer = FindFirstObjectByType<GridMapRenderer>();
            }
        }

        #endregion

        #region 맵 생성 핵심 로직

        // 맵 격자 및 랜덤 듀얼 경로 생성
        public void GenerateMap()
        {
            IsMapGenerated = false;

            InitializeGrid();
            GenerateRandomDualPath();

            if (!HasEnoughHighGroundNearPath(minHighGroundNearPath))
            {
                GenerateMap();
                return;
            }

            if (mapRenderer != null)
            {
                mapRenderer.ClearMap();
                mapRenderer.RenderMap(_grid);
            }

            _cachedPathNodesA = ConvertToPathNodes(_pathPositionA);
            _cachedPathNodesB = ConvertToPathNodes(_pathPositionB);

            IsMapGenerated = true;
            OnMapGenerated?.Invoke();

            Debug.Log($"[TestMapGenerator] 맵 생성 완료: {width}x{height} 그리드, 경로A 노드 수: {_pathPositionA.Count}, 경로B 노드 수: {_pathPositionB.Count}");
        }

        // 2차원 격자 배열 초기화
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

        // 2방향 스폰 및 합류 랜덤 경로 생성
        private void GenerateRandomDualPath()
        {
            _pathPositionA.Clear();
            _pathPositionB.Clear();

            int spawnAY = Random.Range(0, height);
            int spawnBY = Random.Range(0, height);
            while (spawnBY == spawnAY && height > 1)
            {
                spawnBY = Random.Range(0, height);
            }

            int mergeX = Random.Range(1, Mathf.Min(4, width - 2));
            int mergeY = Random.Range(0, height);

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

            AddHorizontalPath(spawnPosA.x, mergePoint.x, spawnPosA.y, _pathPositionA);
            AddVerticalPath(spawnPosA.y, mergePoint.y, mergePoint.x, _pathPositionA);

            AddHorizontalPath(spawnPosB.x, mergePoint.x, spawnPosB.y, _pathPositionB);
            AddVerticalPath(spawnPosB.y, mergePoint.y, mergePoint.x, _pathPositionB);

            ConnectPoints(mergePoint, wayPoint1, _pathPositionA);
            ConnectPoints(wayPoint1, wayPoint2, _pathPositionA);
            ConnectPoints(wayPoint2, goalPos, _pathPositionA);

            int mergeIndex = _pathPositionA.IndexOf(mergePoint);
            if (mergeIndex >= 0)
            {
                for (int i = mergeIndex + 1; i < _pathPositionA.Count; i++)
                {
                    AddPathPosition(_pathPositionA[i], _pathPositionB);
                }
            }

            SetPathTileTypes(_pathPositionA);
            SetPathTileTypes(_pathPositionB);

            GenerateTerrain();
        }

        #endregion

        #region 경로 연결 헬퍼 로직

        // 가로 방향 경로 추가
        private void AddHorizontalPath(int startX, int endX, int y, List<Vector2Int> targetPath)
        {
            int direction = startX <= endX ? 1 : -1;
            for (int x = startX; x != endX + direction; x += direction)
            {
                AddPathPosition(new Vector2Int(x, y), targetPath);
            }
        }

        // 세로 방향 경로 추가
        private void AddVerticalPath(int startY, int endY, int x, List<Vector2Int> targetPath)
        {
            int direction = startY <= endY ? 1 : -1;
            for (int y = startY; y != endY + direction; y += direction)
            {
                AddPathPosition(new Vector2Int(x, y), targetPath);
            }
        }

        // 두 좌표 간 꺾인 경로 연결
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

        // 중복 방지 경로 좌표 추가
        private void AddPathPosition(Vector2Int position, List<Vector2Int> targetPath)
        {
            if (targetPath.Count > 0 && targetPath[targetPath.Count - 1] == position)
            {
                return;
            }
            targetPath.Add(position);
        }

        // 경로 좌표 노드에 타일 타입 부여
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
                    if (_grid[pos.x, pos.y].TileType != TileType.Spawn && _grid[pos.x, pos.y].TileType != TileType.Goal)
                    {
                        _grid[pos.x, pos.y].SetTileType(TileType.Path);
                    }
                }
            }
        }

        // 빈 타일 대상 고지대/지상 지형 무작위 생성
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

        // Vector2Int 경로 목록을 PathNode 배열로 변환
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

        // 방향 벡터 기반 그리드 방향 열거형 산출
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

        // 조건에 부합하는 배치 가능 랜덤 타일 검색
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

        // 경로 인접 여부 확인
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

        // 그리드 내부 유효 좌표 여부 검사
        public bool IsInsideGrid(Vector2Int position)
        {
            return position.x >= 0 && position.x < width && position.y >= 0 && position.y < height;
        }

        // 경로 인근 고지대 타일 수량 검증
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

        // 공중 몬스터 직통 비행 PathNode 배열 생성
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

        // 맵 전체 재생성
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
