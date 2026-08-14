using System;
using System.Collections.Generic;

namespace EndlessGuard.Unit.Raid.Runtime
{
    public sealed class RaidRoutePlan
    {
        private readonly int[] edgeIndices;

        public int EntryNodeId { get; }
        public int GoalNodeId { get; }
        public int StepCount { get; }
        public IReadOnlyList<int> EdgeIndices => edgeIndices;

        public RaidRoutePlan(int entryNodeId, int goalNodeId, IReadOnlyList<int> edgeIndices, int stepCount)
        {
            if (entryNodeId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(entryNodeId));
            }

            if (goalNodeId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(goalNodeId));
            }

            if (edgeIndices == null)
            {
                throw new ArgumentNullException(nameof(edgeIndices));
            }

            if (edgeIndices.Count == 0)
            {
                throw new ArgumentException("Route Plan에는 최소 하나의 Edge가 필요합니다.", nameof(edgeIndices));
            }

            if (stepCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(stepCount), stepCount, "Route Plan 이동 길이는 1 이상이어야 합니다.");
            }

            EntryNodeId = entryNodeId;
            GoalNodeId = goalNodeId;
            StepCount = stepCount;
            this.edgeIndices = new int[edgeIndices.Count];

            for (int i = 0; i < edgeIndices.Count; i++)
            {
                this.edgeIndices[i] = edgeIndices[i];
            }
        }
    }
}