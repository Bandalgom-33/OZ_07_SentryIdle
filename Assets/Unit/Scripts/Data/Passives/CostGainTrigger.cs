using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    public enum CostGainTrigger
    {
        [InspectorName("미설정")]
        None = 0,

        [InspectorName("기본 공격 적중")]
        BasicAttackHit = 1,

        [InspectorName("공격 회피 성공")]
        EvadeSuccess = 2,

        [InspectorName("치명타 적중")]
        CriticalHit = 3,

        [InspectorName("아군 소환물 생성")]
        AllySummonCreated = 4,

        [InspectorName("자신의 스킬 사용 성공")]
        OwnSkillSucceeded = 5,

        [InspectorName("아군 소환물 소멸")]
        AllySummonDestroyed = 6
    }
}