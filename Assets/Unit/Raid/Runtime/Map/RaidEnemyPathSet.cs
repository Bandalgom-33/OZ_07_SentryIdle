using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Runtime;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    public sealed class RaidEnemyPathSet
    {
        private readonly PathNode[][] paths;

        public int Count => paths.Length;

        private RaidEnemyPathSet(PathNode[][] paths)
        {
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

            result = new RaidEnemyPathSet(paths);
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