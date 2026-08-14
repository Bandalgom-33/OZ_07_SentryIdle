using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    public enum PassiveValueKey
    {
        [InspectorName("미설정")]
        None = 0,

        [InspectorName("이동속도 증가율 (%)")]
        BonusMoveSpeedPercent = 1,

        [InspectorName("추가 피해율 (%)")]
        BonusDamagePercent = 2,

        [InspectorName("소환 코스트 획득량")]
        SummonCostGain = 3,

        [InspectorName("잃은 HP 1%당 물리 공격력 증가율 (%)")]
        PhysicalAttackPerLostHpPercent = 4,

        [InspectorName("최대 물리 공격력 증가율 (%)")]
        MaxPhysicalAttackBonusPercent = 5,

        [InspectorName("최종 피해 증가율 (%)")]
        FinalDamageBonusPercent = 6,

        [InspectorName("지속시간 (초)")]
        DurationSeconds = 7,

        [InspectorName("물리 방어력 증가율 (%)")]
        PhysicalDefenseBonusPercent = 8,

        [InspectorName("마법 방어력 증가율 (%)")]
        MagicalDefenseBonusPercent = 9,

        [InspectorName("스킬게이지 획득량")]
        SkillGaugeGain = 10,

        [InspectorName("소환물 1개당 물리 방어력 증가율 (%)")]
        PhysicalDefensePerSummonPercent = 11,

        [InspectorName("소환물 1개당 마법 방어력 증가율 (%)")]
        MagicalDefensePerSummonPercent = 12,

        [InspectorName("이동속도 감소율 (%)")]
        MoveSpeedReductionPercent = 13,

        [InspectorName("보호막량")]
        ShieldAmount = 14,

        [InspectorName("HP 회복량")]
        HealAmount = 15,

        [InspectorName("물리 방어력 감소율 (%)")]
        PhysicalDefenseReductionPercent = 16,

        [InspectorName("마법 방어력 감소율 (%)")]
        MagicalDefenseReductionPercent = 17,

        [InspectorName("공격력 증가율 (%)")]
        AttackBonusPercent = 18,

        [InspectorName("공격속도 증가율 (%)")]
        AttackSpeedBonusPercent = 19,

        [InspectorName("거리 1당 추가 피해율 (%)")]
        DamagePerDistancePercent = 20,

        [InspectorName("거리 추가 피해 최대치 (%)")]
        MaxDistanceDamagePercent = 21,

        [InspectorName("지정 능력치 증가율 (%)")]
        StatBonusPercent = 22,

        [InspectorName("흡혈 비율 (%)")]
        LifeStealPercent = 23,

        [InspectorName("폭발 피해량")]
        ExplosionDamage = 24,

        [InspectorName("폭발 반경 (타일)")]
        ExplosionRadiusTiles = 25,

        [InspectorName("공격속도 감소율 (%)")]
        AttackSpeedReductionPercent = 26,

        [InspectorName("회복 주기 (초)")]
        HealIntervalSeconds = 27,

        [InspectorName("잃은 HP 1%당 마법 공격력 증가율 (%)")]
        MagicalAttackPerLostHpPercent = 28,

        [InspectorName("최대 마법 공격력 증가율 (%)")]
        MaxMagicalAttackBonusPercent = 29,

        [InspectorName("피해 반사율 (%)")]
        DamageReflectPercent = 30,

        [InspectorName("정화 주기 (초)")]
        CleanseIntervalSeconds = 31,

        [InspectorName("무작위 공격 대상 수")]
        RandomTargetCount = 32,

        [InspectorName("연속 공격 횟수")]
        BurstAttackCount = 33,

        [InspectorName("공격 후 강제 이동 시간 (초)")]
        ForcedMoveSeconds = 34,

        [InspectorName("소환 주기 (초)")]
        SummonIntervalSeconds = 35,

        [InspectorName("한 번에 소환하는 수")]
        SummonCount = 36
    }
}