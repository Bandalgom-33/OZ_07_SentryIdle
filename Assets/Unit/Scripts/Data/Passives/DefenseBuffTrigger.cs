using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    public enum DefenseBuffTrigger
    {
        [InspectorName("미설정")]
        None = 0,

        [InspectorName("회피 성공")]
        EvadeSuccess = 1,

        [InspectorName("소형 몬스터 저지 중")]
        BlockingSmall = 2,

        [InspectorName("중형 몬스터 저지 중")]
        BlockingMedium = 3,

        [InspectorName("대형 몬스터 저지 중")]
        BlockingLarge = 4
    }
}