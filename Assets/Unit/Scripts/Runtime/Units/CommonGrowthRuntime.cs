using System;
using EndlessGuard.Unit.Data;

namespace EndlessGuard.Unit.Runtime
{
    [Serializable]
    public sealed class CommonGrowthRuntime
    {
        private const int StatCount = 11;
        [NonSerialized] private int[] modifierIds;

        public void Reset()
        {
            EnsureBuffer();
            Array.Clear(modifierIds, 0, modifierIds.Length);
        }

        public void ApplyAll(RuntimeStats stats)
        {
            Apply(stats, GrowthStatMask.All);
        }

        public void Apply(RuntimeStats stats, GrowthStatMask changedStats)
        {
            if (stats == null || !stats.IsInitialized || changedStats == GrowthStatMask.None)
            {
                return;
            }

            EnsureBuffer();
            ApplyStat(stats, changedStats, GrowthStatMask.MaxHp, PassiveStatType.MaxHp, 0);
            ApplyStat(stats, changedStats, GrowthStatMask.HpRegenPerSecond, PassiveStatType.HpRegenPerSecond, 1);
            ApplyStat(stats, changedStats, GrowthStatMask.PhysicalAttack, PassiveStatType.PhysicalAttack, 2);
            ApplyStat(stats, changedStats, GrowthStatMask.MagicalAttack, PassiveStatType.MagicalAttack, 3);
            ApplyStat(stats, changedStats, GrowthStatMask.AttacksPerSecond, PassiveStatType.AttacksPerSecond, 4);
            ApplyStat(stats, changedStats, GrowthStatMask.PhysicalDefense, PassiveStatType.PhysicalDefense, 5);
            ApplyStat(stats, changedStats, GrowthStatMask.MagicalDefense, PassiveStatType.MagicalDefense, 6);
            ApplyStat(stats, changedStats, GrowthStatMask.Accuracy, PassiveStatType.Accuracy, 7);
            ApplyStat(stats, changedStats, GrowthStatMask.Evasion, PassiveStatType.Evasion, 8);
            ApplyStat(stats, changedStats, GrowthStatMask.CriticalChancePercent, PassiveStatType.CriticalChancePercent, 9);
            ApplyStat(stats, changedStats, GrowthStatMask.CriticalDamageBonusPercent, PassiveStatType.CriticalDamageBonusPercent, 10);
        }

        private void ApplyStat(RuntimeStats stats, GrowthStatMask changedStats, GrowthStatMask growthStat, PassiveStatType runtimeStat, int index)
        {
            if ((changedStats & growthStat) == 0)
            {
                return;
            }

            float bonus = CommonGrowthService.Get(growthStat);
            int modifierId = modifierIds[index];

            if (bonus <= 0f)
            {
                if (modifierId != 0)
                {
                    stats.RemoveModifier(modifierId);
                    modifierIds[index] = 0;
                }

                return;
            }

            if (modifierId == 0)
            {
                modifierIds[index] = stats.AddModifier(runtimeStat, bonus, 0f);
                return;
            }

            if (!stats.UpdateModifier(modifierId, bonus, 0f))
            {
                modifierIds[index] = stats.AddModifier(runtimeStat, bonus, 0f);
            }
        }

        private void EnsureBuffer()
        {
            if (modifierIds == null || modifierIds.Length != StatCount)
            {
                modifierIds = new int[StatCount];
            }
        }
    }
}