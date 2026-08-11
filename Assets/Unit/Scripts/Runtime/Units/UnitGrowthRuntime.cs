using System;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    [Serializable]
    public sealed class UnitGrowthRuntime
    {
        private const int StatCount = 11;

        [Header("성장 적용 상태")]
        [SerializeField] private int appliedLevel = 1;
        [SerializeField] private int appliedPromotionStage;
        [SerializeField] private float appliedLevelGrowthPercent;
        [SerializeField] private float appliedPromotionGrowthPercent;

        [NonSerialized] private int[] modifierIds;

        public int AppliedLevel => appliedLevel;
        public int AppliedPromotionStage => appliedPromotionStage;
        public float AppliedLevelGrowthPercent => appliedLevelGrowthPercent;
        public float AppliedPromotionGrowthPercent => appliedPromotionGrowthPercent;

        public bool Apply(RuntimeStats stats, UnitDataSO unitData, UnitProgressData progress)
        {
            if (stats == null || !stats.IsInitialized || unitData == null || progress == null || !progress.Matches(unitData))
            {
                return false;
            }

            EnsureModifierBuffer();
            RemoveModifiers(stats);

            appliedLevel = progress.CurrentLevel;
            appliedPromotionStage = progress.PromotionStage;
            appliedLevelGrowthPercent = 0f;
            appliedPromotionGrowthPercent = 0f;

            UnitClassGrowthTableSO growthTable = unitData.GrowthTable;

            if (growthTable == null || !growthTable.TryGetProfile(unitData.Class, out UnitClassGrowthProfile profile))
            {
                return true;
            }

            int levelSteps = Mathf.Max(0, progress.CurrentLevel - Mathf.Max(1, unitData.InitialLevel));
            int promotionSteps = Mathf.Max(0, progress.PromotionStage);

            appliedLevelGrowthPercent = profile.LevelGrowth != null ? profile.LevelGrowth.CalculateTotalPercent(levelSteps) : 0f;
            appliedPromotionGrowthPercent = profile.PromotionGrowth != null ? profile.PromotionGrowth.CalculateTotalPercent(promotionSteps) : 0f;

            ApplyStat(stats, profile, GrowthStatMask.MaxHp, PassiveStatType.MaxHp, 0);
            ApplyStat(stats, profile, GrowthStatMask.HpRegenPerSecond, PassiveStatType.HpRegenPerSecond, 1);
            ApplyStat(stats, profile, GrowthStatMask.PhysicalAttack, PassiveStatType.PhysicalAttack, 2);
            ApplyStat(stats, profile, GrowthStatMask.MagicalAttack, PassiveStatType.MagicalAttack, 3);
            ApplyStat(stats, profile, GrowthStatMask.AttacksPerSecond, PassiveStatType.AttacksPerSecond, 4);
            ApplyStat(stats, profile, GrowthStatMask.PhysicalDefense, PassiveStatType.PhysicalDefense, 5);
            ApplyStat(stats, profile, GrowthStatMask.MagicalDefense, PassiveStatType.MagicalDefense, 6);
            ApplyStat(stats, profile, GrowthStatMask.Accuracy, PassiveStatType.Accuracy, 7);
            ApplyStat(stats, profile, GrowthStatMask.Evasion, PassiveStatType.Evasion, 8);
            ApplyStat(stats, profile, GrowthStatMask.CriticalChancePercent, PassiveStatType.CriticalChancePercent, 9);
            ApplyStat(stats, profile, GrowthStatMask.CriticalDamageBonusPercent, PassiveStatType.CriticalDamageBonusPercent, 10);

            return true;
        }

        public void Clear(RuntimeStats stats)
        {
            if (stats != null && stats.IsInitialized)
            {
                EnsureModifierBuffer();
                RemoveModifiers(stats);
            }

            appliedLevel = 1;
            appliedPromotionStage = 0;
            appliedLevelGrowthPercent = 0f;
            appliedPromotionGrowthPercent = 0f;
        }

        private void ApplyStat(RuntimeStats stats, UnitClassGrowthProfile profile, GrowthStatMask stat, PassiveStatType runtimeStat, int index)
        {
            float percent = 0f;

            if (profile.LevelGrowth != null && profile.LevelGrowth.Affects(stat))
            {
                percent += appliedLevelGrowthPercent;
            }

            if (profile.PromotionGrowth != null && profile.PromotionGrowth.Affects(stat))
            {
                percent += appliedPromotionGrowthPercent;
            }

            if (percent <= 0f)
            {
                return;
            }

            modifierIds[index] = stats.AddModifier(runtimeStat, 0f, percent);
        }

        private void EnsureModifierBuffer()
        {
            if (modifierIds == null || modifierIds.Length != StatCount)
            {
                modifierIds = new int[StatCount];
            }
        }

        private void RemoveModifiers(RuntimeStats stats)
        {
            for (int i = 0; i < modifierIds.Length; i++)
            {
                int modifierId = modifierIds[i];

                if (modifierId != 0)
                {
                    stats.RemoveModifier(modifierId);
                    modifierIds[i] = 0;
                }
            }
        }
    }
}
