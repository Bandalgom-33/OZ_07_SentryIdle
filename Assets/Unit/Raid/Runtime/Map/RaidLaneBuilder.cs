using System;
using System.Collections.Generic;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    internal sealed class RaidLaneBuilder
    {
        private const float ParallelThreshold = 0.999f;
        private const float MiterLimit = 2f;

        private readonly RaidBoard board;
        private readonly RaidLaneSet laneSet;
        private readonly List<Vector3> pointBuffer;
        private readonly List<RaidLanePath> pathBuffer;
        private readonly Vector3 worldTileX;
        private readonly Vector3 worldTileY;

        public RaidLaneBuilder(RaidBoard board, RaidLaneSet laneSet)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (laneSet == null)
            {
                throw new ArgumentNullException(nameof(laneSet));
            }

            this.board = board;
            this.laneSet = laneSet;

            pointBuffer = new List<Vector3>(board.Count);
            pathBuffer = new List<RaidLanePath>(laneSet.LaneCount);

            Vector3 origin = board.TileToWorld(Vector2Int.zero);
            worldTileX = board.TileToWorld(Vector2Int.right) - origin;
            worldTileY = board.TileToWorld(Vector2Int.up) - origin;
        }

        public RaidLanePath[] Build(RaidRouteGraph graph)
        {
            if (graph == null)
            {
                throw new ArgumentNullException(nameof(graph));
            }

            if (graph.EdgeCount != laneSet.EdgeCount)
            {
                throw new InvalidOperationException("Route Graph와 Lane Set의 Edge 수가 일치하지 않습니다.");
            }

            pathBuffer.Clear();

            for (int edgeIndex = 0; edgeIndex < graph.EdgeCount; edgeIndex++)
            {
                RaidRouteEdge edge = graph.Edges[edgeIndex];
                int laneCount = laneSet.GetLaneCount(edgeIndex);

                for (int laneIndex = 0; laneIndex < laneCount; laneIndex++)
                {
                    RaidLane lane = laneSet.GetLane(edgeIndex, laneIndex);
                    BuildLane(edge.CenterLine, lane.OffsetTiles);

                    if (pointBuffer.Count < 2)
                    {
                        throw new InvalidOperationException($"Lane Waypoint가 부족합니다. Edge: {edgeIndex}, Lane: {laneIndex}");
                    }

                    pathBuffer.Add(new RaidLanePath(edgeIndex, laneIndex, laneCount, pointBuffer));
                }
            }

            return pathBuffer.ToArray();
        }

        private void BuildLane(IReadOnlyList<Vector2Int> centerLine, float offsetTiles)
        {
            if (centerLine == null)
            {
                throw new ArgumentNullException(nameof(centerLine));
            }

            if (centerLine.Count < 2)
            {
                throw new InvalidOperationException("Route Edge 중심선에는 최소 두 좌표가 필요합니다.");
            }

            pointBuffer.Clear();

            for (int i = 0; i < centerLine.Count; i++)
            {
                Vector2 offset = GetOffset(centerLine, i, offsetTiles);
                Vector3 centerWorld = board.TileToWorld(centerLine[i]);
                Vector3 offsetWorld = worldTileX * offset.x + worldTileY * offset.y;
                pointBuffer.Add(centerWorld + offsetWorld);
            }
        }

        private static Vector2 GetOffset(IReadOnlyList<Vector2Int> centerLine, int index, float offsetTiles)
        {
            if (Mathf.Approximately(offsetTiles, 0f))
            {
                return Vector2.zero;
            }

            Vector2 previousDirection = GetPreviousDirection(centerLine, index);
            Vector2 nextDirection = GetNextDirection(centerLine, index);

            Vector2 previousNormal = GetLeftNormal(previousDirection);
            Vector2 nextNormal = GetLeftNormal(nextDirection);

            if (Vector2.Dot(previousDirection, nextDirection) >= ParallelThreshold)
            {
                return previousNormal * offsetTiles;
            }

            Vector2 normalSum = previousNormal + nextNormal;

            if (normalSum.sqrMagnitude < 0.0001f)
            {
                return previousNormal * offsetTiles;
            }

            Vector2 miter = normalSum.normalized;
            float denominator = Vector2.Dot(miter, previousNormal);

            if (Mathf.Abs(denominator) < 0.0001f)
            {
                return previousNormal * offsetTiles;
            }

            float length = offsetTiles / denominator;
            float maxLength = Mathf.Abs(offsetTiles) * MiterLimit;
            length = Mathf.Clamp(length, -maxLength, maxLength);

            return miter * length;
        }

        private static Vector2 GetPreviousDirection(IReadOnlyList<Vector2Int> centerLine, int index)
        {
            if (index == 0)
            {
                return ToDirection(centerLine[1] - centerLine[0]);
            }

            return ToDirection(centerLine[index] - centerLine[index - 1]);
        }

        private static Vector2 GetNextDirection(IReadOnlyList<Vector2Int> centerLine, int index)
        {
            if (index == centerLine.Count - 1)
            {
                return ToDirection(centerLine[index] - centerLine[index - 1]);
            }

            return ToDirection(centerLine[index + 1] - centerLine[index]);
        }

        private static Vector2 ToDirection(Vector2Int delta)
        {
            if (delta == Vector2Int.right)
            {
                return Vector2.right;
            }

            if (delta == Vector2Int.left)
            {
                return Vector2.left;
            }

            if (delta == Vector2Int.up)
            {
                return Vector2.up;
            }

            if (delta == Vector2Int.down)
            {
                return Vector2.down;
            }

            throw new InvalidOperationException($"Lane 중심선에 인접하지 않은 좌표가 있습니다. Delta: {delta}");
        }

        private static Vector2 GetLeftNormal(Vector2 direction)
        {
            return new Vector2(-direction.y, direction.x);
        }
    }
}