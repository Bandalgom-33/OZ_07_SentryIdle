using System;
using System.Collections.Generic;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    public sealed class RaidLanePath
    {
        private readonly Vector3[] points;

        public int EdgeIndex { get; }
        public int LaneIndex { get; }
        public int LaneCount { get; }
        public IReadOnlyList<Vector3> Points => points;
        public int PointCount => points.Length;

        public RaidLanePath(int edgeIndex, int laneIndex, int laneCount, IReadOnlyList<Vector3> points)
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
                throw new ArgumentOutOfRangeException(nameof(laneIndex), laneIndex, "Lane Index가 범위를 벗어났습니다.");
            }

            if (points == null)
            {
                throw new ArgumentNullException(nameof(points));
            }

            if (points.Count < 2)
            {
                throw new ArgumentException("Lane Path에는 최소 두 개의 Waypoint가 필요합니다.", nameof(points));
            }

            EdgeIndex = edgeIndex;
            LaneIndex = laneIndex;
            LaneCount = laneCount;
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