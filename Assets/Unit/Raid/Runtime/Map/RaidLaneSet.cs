using System;

namespace EndlessGuard.Unit.Raid.Runtime
{
    public readonly struct RaidLane
    {
        public int EdgeIndex { get; }
        public int LaneIndex { get; }
        public int LaneCount { get; }
        public float OffsetTiles { get; }

        public RaidLane(int edgeIndex, int laneIndex, int laneCount, float offsetTiles)
        {
            if (edgeIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(edgeIndex));
            }

            if (laneCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(laneCount), laneCount, "Lane Count는 1 이상이어야 합니다.");
            }

            if (laneIndex < 0 || laneIndex >= laneCount)
            {
                throw new ArgumentOutOfRangeException(nameof(laneIndex), laneIndex, "Lane Index가 Lane 범위를 벗어났습니다.");
            }

            EdgeIndex = edgeIndex;
            LaneIndex = laneIndex;
            LaneCount = laneCount;
            OffsetTiles = offsetTiles;
        }
    }

    public sealed class RaidLaneSet
    {
        private readonly RaidLane[] lanes;
        private readonly int[] edgeStarts;

        public int EdgeCount { get; }
        public int LaneCount => lanes.Length;

        public RaidLaneSet(RaidRouteGraph graph)
        {
            if (graph == null)
            {
                throw new ArgumentNullException(nameof(graph));
            }

            EdgeCount = graph.EdgeCount;
            edgeStarts = new int[EdgeCount + 1];

            int totalLaneCount = 0;

            for (int edgeIndex = 0; edgeIndex < EdgeCount; edgeIndex++)
            {
                edgeStarts[edgeIndex] = totalLaneCount;
                totalLaneCount += graph.Edges[edgeIndex].Width;
            }

            edgeStarts[EdgeCount] = totalLaneCount;
            lanes = new RaidLane[totalLaneCount];

            for (int edgeIndex = 0; edgeIndex < EdgeCount; edgeIndex++)
            {
                int laneCount = graph.Edges[edgeIndex].Width;
                int minOffset = -(laneCount / 2);
                int start = edgeStarts[edgeIndex];

                for (int laneIndex = 0; laneIndex < laneCount; laneIndex++)
                {
                    float offsetTiles = minOffset + laneIndex;
                    lanes[start + laneIndex] = new RaidLane(edgeIndex, laneIndex, laneCount, offsetTiles);
                }
            }
        }

        public int GetLaneCount(int edgeIndex)
        {
            ValidateEdge(edgeIndex);
            return edgeStarts[edgeIndex + 1] - edgeStarts[edgeIndex];
        }

        public RaidLane GetLane(int edgeIndex, int laneIndex)
        {
            ValidateEdge(edgeIndex);

            int laneCount = GetLaneCount(edgeIndex);

            if (laneIndex < 0 || laneIndex >= laneCount)
            {
                throw new ArgumentOutOfRangeException(nameof(laneIndex), laneIndex, "Lane Index가 Edge의 Lane 범위를 벗어났습니다.");
            }

            return lanes[edgeStarts[edgeIndex] + laneIndex];
        }

        private void ValidateEdge(int edgeIndex)
        {
            if (edgeIndex < 0 || edgeIndex >= EdgeCount)
            {
                throw new ArgumentOutOfRangeException(nameof(edgeIndex), edgeIndex, "존재하지 않는 Route Edge입니다.");
            }
        }
    }
}