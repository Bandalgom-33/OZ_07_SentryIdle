using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    public enum EnemyCategory
    {
        [InspectorName("미설정")]
        None = 0,

        [InspectorName("일반")]
        Normal = 1,

        [InspectorName("엘리트")]
        Elite = 2,

        [InspectorName("보스")]
        Boss = 3
    }

    public enum EnemyMovementType
    {
        [InspectorName("미설정")]
        None = 0,

        [InspectorName("지상")]
        Ground = 1,

        [InspectorName("공중")]
        Air = 2
    }

    public enum EnemySize
    {
        [InspectorName("미설정")]
        None = 0,

        [InspectorName("소형")]
        Small = 1,

        [InspectorName("중형")]
        Medium = 2,

        [InspectorName("대형")]
        Large = 3
    }

    public enum EnemyRole
    {
        [InspectorName("미설정")]
        None = 0,

        [InspectorName("공격형")]
        Attacker = 1,

        [InspectorName("서포터")]
        Supporter = 2
    }

    public enum EnemyAttackRule
    {
        [InspectorName("미설정")]
        None = 0,

        [InspectorName("저지된 대상만 공격")]
        BlockedOnly = 1,

        [InspectorName("범위 내 대상 공격")]
        InRange = 2
    }
}