using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    public enum UnitSkillTargetScope
    {
        [InspectorName("단일 대상")]
        Single = 0,

        [InspectorName("범위 공격")]
        Area = 1,

        [InspectorName("맵 전체")]
        MapWide = 2
    }

    public enum UnitSkillTargetPriority
    {
        [InspectorName("골에 가장 가까운 적")]
        ClosestToGoal = 0,

        [InspectorName("시전자에게 가장 가까운 적")]
        NearestToCaster = 1,

        [InspectorName("현재 HP가 가장 낮은 적")]
        LowestHp = 2,

        [InspectorName("무작위 적")]
        Random = 3
    }

    public enum UnitSkillAttackPowerSource
    {
        [InspectorName("물리 공격력")]
        PhysicalAttack = 0,

        [InspectorName("마법 공격력")]
        MagicalAttack = 1,

        [InspectorName("물리/마법 중 높은 공격력")]
        HigherAttack = 2,

        [InspectorName("고정 피해만 사용")]
        FixedOnly = 3
    }

    public enum UnitSkillHitMode
    {
        [InspectorName("한 번에 공격")]
        SingleHit = 0,

        [InspectorName("나누어서 연속 공격")]
        MultiHit = 1
    }

    public enum UnitSkillMultiHitDamageMode
    {
        [InspectorName("총 피해를 타수로 분할")]
        SplitTotalPower = 0,

        [InspectorName("매 타격마다 설정 피해 전부 적용")]
        FullPowerEachHit = 1
    }

    public enum UnitSkillVfxSpawnMode
    {
        [InspectorName("맞는 적마다")]
        EachTarget = 0,

        [InspectorName("대표 대상 위치에 한 번")]
        PrimaryTarget = 1,

        [InspectorName("시전자 위치")]
        Caster = 2
    }
}
