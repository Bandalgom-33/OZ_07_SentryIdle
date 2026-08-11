using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    public enum GrowthStackMode
    {
        [InspectorName("기본 능력치 기준 선형 누적")]
        LinearFromBase = 0,

        [InspectorName("단계별 복리 누적")]
        CompoundPerStep = 1
    }
}
