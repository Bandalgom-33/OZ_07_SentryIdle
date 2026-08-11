using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    public enum PassiveStatType
    {
        [InspectorName("미설정")]
        None = 0,

        [InspectorName("최대 HP")]
        MaxHp = 1,

        [InspectorName("초당 HP 재생")]
        HpRegenPerSecond = 2,

        [InspectorName("물리 공격력")]
        PhysicalAttack = 3,

        [InspectorName("마법 공격력")]
        MagicalAttack = 4,

        [InspectorName("물리 방어력")]
        PhysicalDefense = 5,

        [InspectorName("마법 방어력")]
        MagicalDefense = 6,

        [InspectorName("공격속도")]
        AttacksPerSecond = 7,

        [InspectorName("명중")]
        Accuracy = 8,

        [InspectorName("회피")]
        Evasion = 9,

        [InspectorName("치명타 확률")]
        CriticalChancePercent = 10,

        [InspectorName("치명타 피해량")]
        CriticalDamageBonusPercent = 11,

        [InspectorName("이동속도")]
        MoveSpeed = 12
    }
}