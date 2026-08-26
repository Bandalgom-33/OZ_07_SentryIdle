using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Runtime;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    public sealed class RaidEnemyPathSet
    {
        private readonly RaidBoard board;
        private readonly PathNode[][] paths;
        private PathNode[][][] cachedAirPaths;
        private float cachedAirHeight = -1f;
        private float cachedAirLateralOffset = -1f;
        private float cachedAirNodeSpacing = -1f;
        private int cachedAirVariantCount = -1;

        public int Count => paths.Length;

        private RaidEnemyPathSet(RaidBoard board, PathNode[][] paths)
        {
            this.board = board ?? throw new ArgumentNullException(nameof(board));
            this.paths = paths ?? throw new ArgumentNullException(nameof(paths));
        }

        internal static bool TryCreate(RaidBoard board, RaidRouteGraph graph, IReadOnlyList<RaidTravelPath> travelPaths, out RaidEnemyPathSet result)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (graph == null)
            {
                throw new ArgumentNullException(nameof(graph));
            }

            if (travelPaths == null)
            {
                throw new ArgumentNullException(nameof(travelPaths));
            }

            if (travelPaths.Count == 0)
            {
                result = null;
                return false;
            }

            PathNode[][] paths = new PathNode[travelPaths.Count][];
            List<Vector2Int> tileBuffer = new List<Vector2Int>(board.Count);

            for (int i = 0; i < travelPaths.Count; i++)
            {
                RaidTravelPath travelPath = travelPaths[i];

                if (travelPath == null)
                {
                    throw new InvalidOperationException($"Travel Path가 null입니다. Index: {i}");
                }

                tileBuffer.Clear();

                if (!TryBuildPath(board, graph, travelPath, tileBuffer, out PathNode[] path))
                {
                    result = null;
                    return false;
                }

                paths[i] = path;
            }

            result = new RaidEnemyPathSet(board, paths);
            return true;
        }

        public int GetNodeCount(int pathIndex)
        {
            ValidateIndex(pathIndex);
            return paths[pathIndex].Length;
        }

        public bool ApplyTo(int pathIndex, EnemyMove enemyMove)
        {
            if (enemyMove == null)
            {
                throw new ArgumentNullException(nameof(enemyMove));
            }

            ValidateIndex(pathIndex);
            return enemyMove.SetPath(paths[pathIndex]);
        }

        public bool ApplyTo(int pathIndex, EnemyMove enemyMove, EnemyMovementType movementType, float airFlightHeight)
        {
            return ApplyTo(pathIndex, enemyMove, movementType, airFlightHeight, 0, 1, 2.4f, 1.25f);
        }

        public bool ApplyTo(int pathIndex, EnemyMove enemyMove, EnemyMovementType movementType, float airFlightHeight, int airVariant, int airVariantCount, float airLateralOffsetTiles, float airNodeSpacingTiles)
        {
            if (enemyMove == null)
            {
                throw new ArgumentNullException(nameof(enemyMove));
            }

            ValidateIndex(pathIndex);

            if (movementType != EnemyMovementType.Air)
            {
                return enemyMove.SetPath(paths[pathIndex]);
            }

            int variantCount = Mathf.Clamp(airVariantCount, 1, 3);
            int normalizedVariant = PositiveModulo(airVariant, variantCount);
            float height = Mathf.Max(0f, airFlightHeight);
            float lateralOffset = Mathf.Max(0f, airLateralOffsetTiles);
            float nodeSpacing = Mathf.Max(0.5f, airNodeSpacingTiles);
            PathNode[] airPath = GetAirPath(pathIndex, height, normalizedVariant, variantCount, lateralOffset, nodeSpacing);
            return airPath != null && airPath.Length >= 2 && enemyMove.SetPath(airPath);
        }

        private PathNode[] GetAirPath(int pathIndex, float height, int variant, int variantCount, float lateralOffset, float nodeSpacing)
        {
            bool cacheInvalid = cachedAirPaths == null ||
                                cachedAirPaths.Length != paths.Length ||
                                !Mathf.Approximately(cachedAirHeight, height) ||
                                !Mathf.Approximately(cachedAirLateralOffset, lateralOffset) ||
                                !Mathf.Approximately(cachedAirNodeSpacing, nodeSpacing) ||
                                cachedAirVariantCount != variantCount;

            if (cacheInvalid)
            {
                cachedAirPaths = new PathNode[paths.Length][][];

                for (int i = 0; i < cachedAirPaths.Length; i++)
                {
                    cachedAirPaths[i] = new PathNode[variantCount][];
                }

                cachedAirHeight = height;
                cachedAirLateralOffset = lateralOffset;
                cachedAirNodeSpacing = nodeSpacing;
                cachedAirVariantCount = variantCount;
            }

            PathNode[] cached = cachedAirPaths[pathIndex][variant];

            if (cached != null)
            {
                return cached;
            }

            cached = BuildAirPath(paths[pathIndex], height, variant, lateralOffset, nodeSpacing);
            cachedAirPaths[pathIndex][variant] = cached;
            return cached;
        }

        private PathNode[] BuildAirPath(PathNode[] source, float height, int variant, float lateralOffset, float nodeSpacing)
        {
            if (source == null || source.Length < 2)
            {
                return source;
            }

            Vector2 start = new Vector2(source[0].Tile.x, source[0].Tile.y);
            Vector2 goal = new Vector2(source[source.Length - 1].Tile.x, source[source.Length - 1].Tile.y);
            Vector2 delta = goal - start;
            float distance = delta.magnitude;

            if (distance <= 0.01f)
            {
                return source;
            }

            Vector2 direction = delta / distance;
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);
            int sideSeed = source[0].Tile.x + source[0].Tile.y + source[source.Length - 1].Tile.x + source[source.Length - 1].Tile.y + variant;
            float side = (sideSeed & 1) == 0 ? 1f : -1f;
            float offset = Mathf.Min(Mathf.Max(0f, lateralOffset), Mathf.Max(0.5f, board.Height * 0.3f));
            List<Vector2> controls = new List<Vector2>(5);
            controls.Add(ClampAirPoint(start));

            if (distance >= 5f && offset > 0.1f)
            {
                switch (variant % 3)
                {
                    case 0:
                        controls.Add(ClampAirPoint(Vector2.Lerp(start, goal, 0.24f) + perpendicular * side * offset));
                        controls.Add(ClampAirPoint(Vector2.Lerp(start, goal, 0.58f) - perpendicular * side * offset * 0.9f));
                        controls.Add(ClampAirPoint(Vector2.Lerp(start, goal, 0.82f) + perpendicular * side * offset * 0.35f));
                        break;
                    case 1:
                        controls.Add(ClampAirPoint(Vector2.Lerp(start, goal, 0.28f) - perpendicular * side * offset * 0.75f));
                        controls.Add(ClampAirPoint(Vector2.Lerp(start, goal, 0.62f) + perpendicular * side * offset * 0.95f));
                        controls.Add(ClampAirPoint(Vector2.Lerp(start, goal, 0.84f) - perpendicular * side * offset * 0.25f));
                        break;
                    default:
                        controls.Add(ClampAirPoint(Vector2.Lerp(start, goal, 0.32f) + perpendicular * side * offset * 0.95f));
                        controls.Add(ClampAirPoint(Vector2.Lerp(start, goal, 0.68f) + perpendicular * side * offset * 0.55f));
                        break;
                }
            }

            controls.Add(ClampAirPoint(goal));

            List<Vector2> samples = new List<Vector2>(24);
            samples.Add(controls[0]);

            for (int controlIndex = 1; controlIndex < controls.Count; controlIndex++)
            {
                Vector2 from = controls[controlIndex - 1];
                Vector2 to = controls[controlIndex];
                float segmentDistance = Vector2.Distance(from, to);
                int steps = Mathf.Max(1, Mathf.CeilToInt(segmentDistance / Mathf.Max(0.5f, nodeSpacing)));

                for (int step = 1; step <= steps; step++)
                {
                    float t = step / (float)steps;
                    Vector2 point = Vector2.Lerp(from, to, t);

                    if ((point - samples[samples.Count - 1]).sqrMagnitude > 0.0001f)
                    {
                        samples.Add(point);
                    }
                }
            }

            if (samples.Count < 2)
            {
                return source;
            }

            Vector3 origin = board.TileToWorld(Vector2Int.zero);
            Vector3 axisX = board.Width > 1 ? board.TileToWorld(Vector2Int.right) - origin : Vector3.right * board.TileSize;
            Vector3 axisY = board.Height > 1 ? board.TileToWorld(Vector2Int.up) - origin : Vector3.forward * board.TileSize;
            PathNode[] result = new PathNode[samples.Count];

            for (int i = 0; i < samples.Count; i++)
            {
                Vector2 point = samples[i];
                int tileX = Mathf.Clamp(Mathf.RoundToInt(point.x), 0, board.Width - 1);
                int tileY = Mathf.Clamp(Mathf.RoundToInt(point.y), 0, board.Height - 1);
                Vector2Int tile = new Vector2Int(tileX, tileY);
                Vector3 position = origin + axisX * point.x + axisY * point.y + Vector3.up * height;
                GridFacingDirection facing = source[Mathf.Min(i, source.Length - 1)].Facing;

                if (i < samples.Count - 1)
                {
                    facing = GetClosestFacing(samples[i + 1] - point, facing);
                }
                else if (i > 0)
                {
                    facing = GetClosestFacing(point - samples[i - 1], facing);
                }

                result[i] = new PathNode(position, tile, facing);
            }

            return result;
        }

        private Vector2 ClampAirPoint(Vector2 point)
        {
            float maxX = Mathf.Max(0f, board.Width - 1f);
            float maxY = Mathf.Max(0f, board.Height - 1f);
            return new Vector2(Mathf.Clamp(point.x, 0f, maxX), Mathf.Clamp(point.y, 0f, maxY));
        }

        private static int PositiveModulo(int value, int modulus)
        {
            if (modulus <= 0)
            {
                return 0;
            }

            int result = value % modulus;
            return result < 0 ? result + modulus : result;
        }

        private static GridFacingDirection GetClosestFacing(Vector2 delta, GridFacingDirection fallback)
        {
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y) && !Mathf.Approximately(delta.x, 0f))
            {
                return delta.x > 0f ? GridFacingDirection.East : GridFacingDirection.West;
            }

            if (!Mathf.Approximately(delta.y, 0f))
            {
                return delta.y > 0f ? GridFacingDirection.North : GridFacingDirection.South;
            }

            return fallback;
        }

        private static bool TryBuildPath(RaidBoard board, RaidRouteGraph graph, RaidTravelPath travelPath, List<Vector2Int> tiles, out PathNode[] result)
        {
            RaidRouteNode entryNode = graph.GetNode(travelPath.EntryNodeId);
            RaidRouteNode goalNode = graph.GetNode(travelPath.GoalNodeId);

            if (entryNode.Type != RaidRouteNodeType.Entry)
            {
                throw new InvalidOperationException("Travel Path의 시작 Node가 Entry가 아닙니다.");
            }

            if (goalNode.Type != RaidRouteNodeType.Goal)
            {
                throw new InvalidOperationException("Travel Path의 도착 Node가 Goal이 아닙니다.");
            }

            AddTile(tiles, entryNode.Coordinate);

            for (int i = 0; i < travelPath.PointCount; i++)
            {
                Vector3 worldPoint = travelPath.GetPoint(i);

                if (!board.TryWorldToTile(worldPoint, out Vector2Int tile))
                {
                    result = null;
                    return false;
                }

                if (!TryAddExpandedTiles(tiles, tile))
                {
                    result = null;
                    return false;
                }
            }

            if (!TryAddExpandedTiles(tiles, goalNode.Coordinate) || tiles.Count < 2)
            {
                result = null;
                return false;
            }

            result = new PathNode[tiles.Count];

            for (int i = 0; i < tiles.Count; i++)
            {
                Vector2Int tile = tiles[i];

                if (!board.GetTile(tile).IsPath)
                {
                    result = null;
                    return false;
                }

                GridFacingDirection facing;

                if (i == 0)
                {
                    if (!TryGetFacing(tile, tiles[1], out facing))
                    {
                        result = null;
                        return false;
                    }
                }
                else if (!TryGetFacing(tiles[i - 1], tile, out facing))
                {
                    result = null;
                    return false;
                }

                result[i] = new PathNode(board.TileToWorld(tile), tile, facing);
            }

            return true;
        }

        private static void AddTile(List<Vector2Int> tiles, Vector2Int tile)
        {
            if (tiles.Count > 0 && tiles[tiles.Count - 1] == tile)
            {
                return;
            }

            tiles.Add(tile);
        }

        private static bool TryAddExpandedTiles(List<Vector2Int> tiles, Vector2Int target)
        {
            if (tiles.Count == 0)
            {
                tiles.Add(target);
                return true;
            }

            Vector2Int current = tiles[tiles.Count - 1];

            if (current == target)
            {
                return true;
            }

            Vector2Int delta = target - current;

            if (delta.x != 0 && delta.y != 0)
            {
                return false;
            }

            Vector2Int direction = new Vector2Int(Math.Sign(delta.x), Math.Sign(delta.y));
            int distance = Mathf.Abs(delta.x) + Mathf.Abs(delta.y);

            for (int i = 0; i < distance; i++)
            {
                current += direction;
                AddTile(tiles, current);
            }

            return true;
        }

        private static bool TryGetFacing(Vector2Int from, Vector2Int to, out GridFacingDirection facing)
        {
            Vector2Int delta = to - from;

            if (delta.x > 0 && delta.y == 0)
            {
                facing = GridFacingDirection.East;
                return true;
            }

            if (delta.x < 0 && delta.y == 0)
            {
                facing = GridFacingDirection.West;
                return true;
            }

            if (delta.y > 0 && delta.x == 0)
            {
                facing = GridFacingDirection.North;
                return true;
            }

            if (delta.y < 0 && delta.x == 0)
            {
                facing = GridFacingDirection.South;
                return true;
            }

            facing = default;
            return false;
        }

        private void ValidateIndex(int pathIndex)
        {
            if (pathIndex < 0 || pathIndex >= paths.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(pathIndex), pathIndex, "존재하지 않는 Enemy Path입니다.");
            }
        }
    }
}
