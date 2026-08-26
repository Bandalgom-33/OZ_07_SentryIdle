using System;
using System.Collections.Generic;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    public sealed class RaidTravelPath
    {
        private readonly Vector3[] points;

        public int RoutePlanIndex { get; }
        public int LaneVariantIndex { get; }
        public int EntryNodeId { get; }
        public int GoalNodeId { get; }
        public int PointCount => points.Length;
        public IReadOnlyList<Vector3> Points => points;

        public RaidTravelPath(int routePlanIndex, int laneVariantIndex, int entryNodeId, int goalNodeId, IReadOnlyList<Vector3> points)
        {
            if (routePlanIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(routePlanIndex));
            }

            if (laneVariantIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(laneVariantIndex));
            }

            if (entryNodeId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(entryNodeId));
            }

            if (goalNodeId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(goalNodeId));
            }

            if (points == null)
            {
                throw new ArgumentNullException(nameof(points));
            }

            if (points.Count < 2)
            {
                throw new ArgumentException("Travel Path에는 최소 두 개의 Waypoint가 필요합니다.", nameof(points));
            }

            RoutePlanIndex = routePlanIndex;
            LaneVariantIndex = laneVariantIndex;
            EntryNodeId = entryNodeId;
            GoalNodeId = goalNodeId;
            this.points = new Vector3[points.Count];

            for (int i = 0; i < points.Count; i++)
            {
                this.points[i] = points[i];
            }
        }

        public Vector3 GetPoint(int index)
        {
            if (index < 0 || index >= points.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return points[index];
        }
    }
}