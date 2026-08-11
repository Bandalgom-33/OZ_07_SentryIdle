using System.Collections.Generic;
using EndlessGuard.Unit.Data;

namespace EndlessGuard.Unit.Runtime
{
    internal static class SummonStatModifierRuntime
    {
        public static bool Apply(RuntimeStats stats, IReadOnlyList<SummonStatModifier> modifiers, List<int> modifierIds)
        {
            if (stats == null || !stats.IsInitialized || modifiers == null || modifierIds == null)
            {
                return false;
            }

            bool maxHpChanged = false;

            for (int i = 0; i < modifiers.Count; i++)
            {
                SummonStatModifier modifier = modifiers[i];

                if (modifier == null || modifier.StatType == PassiveStatType.None)
                {
                    continue;
                }

                int modifierId = stats.AddModifier(modifier.StatType, modifier.FlatBonus, modifier.PercentBonus);

                if (modifierId == 0)
                {
                    continue;
                }

                modifierIds.Add(modifierId);
                maxHpChanged |= modifier.StatType == PassiveStatType.MaxHp;
            }

            return maxHpChanged;
        }

        public static bool ApplyOwnerInheritance(RuntimeStats summonStats, RuntimeStats ownerStats, IReadOnlyList<SummonOwnerStatInheritance> inheritances, List<int> modifierIds)
        {
            if (summonStats == null || !summonStats.IsInitialized || ownerStats == null || !ownerStats.IsInitialized || inheritances == null || modifierIds == null)
            {
                return false;
            }

            bool maxHpChanged = false;

            for (int i = 0; i < inheritances.Count; i++)
            {
                SummonOwnerStatInheritance inheritance = inheritances[i];

                if (inheritance == null || inheritance.StatType == PassiveStatType.None || inheritance.InheritPercent <= 0f)
                {
                    continue;
                }

                float ownerValue = GetCurrentStatValue(ownerStats, inheritance.StatType);
                float inheritedValue = ownerValue * inheritance.InheritPercent * 0.01f;

                if (inheritedValue == 0f)
                {
                    continue;
                }

                int modifierId = summonStats.AddModifier(inheritance.StatType, inheritedValue, 0f);

                if (modifierId == 0)
                {
                    continue;
                }

                modifierIds.Add(modifierId);
                maxHpChanged |= inheritance.StatType == PassiveStatType.MaxHp;
            }

            return maxHpChanged;
        }

        public static void Remove(RuntimeStats stats, List<int> modifierIds)
        {
            if (modifierIds == null)
            {
                return;
            }

            if (stats != null && stats.IsInitialized)
            {
                for (int i = 0; i < modifierIds.Count; i++)
                {
                    int modifierId = modifierIds[i];

                    if (modifierId != 0)
                    {
                        stats.RemoveModifier(modifierId);
                    }
                }
            }

            modifierIds.Clear();
        }

        private static float GetCurrentStatValue(RuntimeStats stats, PassiveStatType statType)
        {
            switch (statType)
            {
                case PassiveStatType.MaxHp:
                    return stats.MaxHp;

                case PassiveStatType.HpRegenPerSecond:
                    return stats.HpRegenPerSecond;

                case PassiveStatType.PhysicalAttack:
                    return stats.PhysicalAttack;

                case PassiveStatType.MagicalAttack:
                    return stats.MagicalAttack;

                case PassiveStatType.PhysicalDefense:
                    return stats.PhysicalDefense;

                case PassiveStatType.MagicalDefense:
                    return stats.MagicalDefense;

                case PassiveStatType.AttacksPerSecond:
                    return stats.AttacksPerSecond;

                case PassiveStatType.Accuracy:
                    return stats.Accuracy;

                case PassiveStatType.Evasion:
                    return stats.Evasion;

                case PassiveStatType.CriticalChancePercent:
                    return stats.CriticalChancePercent;

                case PassiveStatType.CriticalDamageBonusPercent:
                    return stats.CriticalDamageBonusPercent;

                case PassiveStatType.MoveSpeed:
                    return stats.MoveSpeed;

                default:
                    return 0f;
            }
        }
    }
}