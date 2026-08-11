using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    public enum AttackMode
    {
        [InspectorName("공격하지 않음")]
        None = 0,

        [InspectorName("근거리")]
        Melee = 1,

        [InspectorName("원거리")]
        Ranged = 2
    }

    public enum DamageType
    {
        [InspectorName("피해 없음")]
        None = 0,

        [InspectorName("물리 피해")]
        Physical = 1,

        [InspectorName("마법 피해")]
        Magical = 2
    }

    public enum AttackTarget
    {
        [InspectorName("공격 대상 없음")]
        None = 0,

        [InspectorName("지상")]
        Ground = 1,

        [InspectorName("공중")]
        Air = 2,

        [InspectorName("지상·공중")]
        GroundAndAir = 3
    }

    public enum AttackRangeRotationMode
    {
        [InspectorName("방향 고정")]
        Fixed = 0,

        [InspectorName("바라보는 방향 따라 회전")]
        FollowFacing = 1
    }

    public enum CombatTargetLayer
    {
        [InspectorName("지상")]
        Ground = 1,

        [InspectorName("공중")]
        Air = 2
    }

    public enum GridFacingDirection
    {
        [InspectorName("위쪽")]
        North = 0,

        [InspectorName("오른쪽")]
        East = 1,

        [InspectorName("아래쪽")]
        South = 2,

        [InspectorName("왼쪽")]
        West = 3
    }
}