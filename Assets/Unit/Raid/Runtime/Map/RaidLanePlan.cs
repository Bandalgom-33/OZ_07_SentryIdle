using System;

namespace EndlessGuard.Unit.Raid.Runtime
{
    public sealed class RaidLanePlan
    {
        private readonly int[] laneIndices;

        public int RoutePlanIndex { get; }
        public int VariantIndex { get; }
        public int VariantCount { get; }
        public int StepCount => laneIndices.Length;

        internal RaidLanePlan(int routePlanIndex, int variantIndex, int variantCount, int[] laneIndices)
        {
            if (routePlanIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(routePlanIndex));
            }

            if (variantCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(variantCount), variantCount, "Variant Count는 1 이상이어야 합니다.");
            }

            if (variantIndex < 0 || variantIndex >= variantCount)
            {
                throw new ArgumentOutOfRangeException(nameof(variantIndex), variantIndex, "Variant Index가 범위를 벗어났습니다.");
            }

            if (laneIndices == null)
            {
                throw new ArgumentNullException(nameof(laneIndices));
            }

            if (laneIndices.Length == 0)
            {
                throw new ArgumentException("Lane Plan에는 최소 하나의 Lane이 필요합니다.", nameof(laneIndices));
            }

            RoutePlanIndex = routePlanIndex;
            VariantIndex = variantIndex;
            VariantCount = variantCount;
            this.laneIndices = laneIndices;
        }

        public int GetLaneIndex(int stepIndex)
        {
            if (stepIndex < 0 || stepIndex >= laneIndices.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(stepIndex));
            }

            return laneIndices[stepIndex];
        }
    }
}