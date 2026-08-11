using System;
using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [Serializable]
    public sealed class UnitGrowthRule
    {
        [Tooltip("이 성장 규칙으로 올릴 능력치를 복수 선택합니다. 선택하지 않은 능력치는 증가하지 않습니다.")]
        [SerializeField] private GrowthStatMask affectedStats = GrowthStatMask.None;

        [Tooltip("선택한 모든 능력치에 동일하게 적용할 1단계당 성장률(%)입니다. 예: 1 입력 시 선택 능력치가 단계마다 1% 성장합니다.")]
        [Min(0f)]
        [SerializeField] private float percentPerStep;

        [Tooltip("레벨/승급이 여러 단계 누적될 때 성장률을 계산하는 방식입니다. 기본값은 원본 능력치 기준 선형 누적입니다.")]
        [SerializeField] private GrowthStackMode stackMode = GrowthStackMode.LinearFromBase;

        public GrowthStatMask AffectedStats => affectedStats;
        public float PercentPerStep => Mathf.Max(0f, percentPerStep);
        public GrowthStackMode StackMode => stackMode;
        public bool HasGrowth => affectedStats != GrowthStatMask.None && PercentPerStep > 0f;

        public bool Affects(GrowthStatMask stat)
        {
            return stat != GrowthStatMask.None && (affectedStats & stat) != 0;
        }

        public float CalculateTotalPercent(int stepCount)
        {
            stepCount = Mathf.Max(0, stepCount);

            if (stepCount == 0 || !HasGrowth)
            {
                return 0f;
            }

            double perStep = PercentPerStep * 0.01d;
            double total;

            if (stackMode == GrowthStackMode.CompoundPerStep)
            {
                total = (Math.Pow(1d + perStep, stepCount) - 1d) * 100d;
            }
            else
            {
                total = PercentPerStep * stepCount;
            }

            if (double.IsNaN(total) || total <= 0d)
            {
                return 0f;
            }

            return total >= float.MaxValue ? float.MaxValue : (float)total;
        }
    }
}
