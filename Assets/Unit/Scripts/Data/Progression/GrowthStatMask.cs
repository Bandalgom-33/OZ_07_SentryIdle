using System;
using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [Flags]
    public enum GrowthStatMask
    {
        [InspectorName("없음")]
        None = 0,

        [InspectorName("최대 HP")]
        MaxHp = 1 << 0,

        [InspectorName("초당 HP 재생력")]
        HpRegenPerSecond = 1 << 1,

        [InspectorName("물리 공격력")]
        PhysicalAttack = 1 << 2,

        [InspectorName("마법 공격력")]
        MagicalAttack = 1 << 3,

        [InspectorName("공격속도")]
        AttacksPerSecond = 1 << 4,

        [InspectorName("물리 방어력")]
        PhysicalDefense = 1 << 5,

        [InspectorName("마법 방어력")]
        MagicalDefense = 1 << 6,

        [InspectorName("명중력")]
        Accuracy = 1 << 7,

        [InspectorName("회피력")]
        Evasion = 1 << 8,

        [InspectorName("치명타 확률")]
        CriticalChancePercent = 1 << 9,

        [InspectorName("치명타 피해량")]
        CriticalDamageBonusPercent = 1 << 10,

        All = MaxHp
            | HpRegenPerSecond
            | PhysicalAttack
            | MagicalAttack
            | AttacksPerSecond
            | PhysicalDefense
            | MagicalDefense
            | Accuracy
            | Evasion
            | CriticalChancePercent
            | CriticalDamageBonusPercent
    }
}
