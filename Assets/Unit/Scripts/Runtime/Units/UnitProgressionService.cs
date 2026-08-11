using System;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    public static class UnitProgressionService
    {
        public static int GetMaxLevel(UnitDataSO unitData, UnitProgressData progress)
        {
            if (unitData == null)
            {
                return 1;
            }

            int fallback = Mathf.Max(1, unitData.InitialLevel);
            UnitClassGrowthTableSO growthTable = unitData.GrowthTable;

            if (growthTable == null)
            {
                return fallback;
            }

            int promotionStage = progress != null && progress.Matches(unitData) ? progress.PromotionStage : 0;
            return growthTable.GetMaxLevel(unitData.Class, promotionStage, fallback);
        }

        public static bool TryAddExperience(UnitDataSO unitData, UnitProgressData progress, long gainedExp, out UnitLevelResult result)
        {
            result = default;

            if (!IsValid(unitData, progress) || unitData.GrowthTable == null || unitData.GrowthTable.LevelCurve == null)
            {
                return false;
            }

            int previousLevel = progress.CurrentLevel;
            long previousExp = progress.CurrentExp;
            int promotionStage = progress.PromotionStage;
            int maxLevel = GetMaxLevel(unitData, progress);

            result = UnitLevelCalculator.AddExperience(progress, unitData.GrowthTable.LevelCurve, maxLevel, gainedExp);

            UnitProgressChangeType changeType = UnitProgressChangeType.None;

            if (previousExp != progress.CurrentExp)
            {
                changeType |= UnitProgressChangeType.Experience;
            }

            if (previousLevel != progress.CurrentLevel)
            {
                changeType |= UnitProgressChangeType.Level;
            }

            if (changeType != UnitProgressChangeType.None)
            {
                UnitProgressEvents.PublishProgressChanged(new UnitProgressChangedInfo(
                    unitData,
                    progress,
                    changeType,
                    previousLevel,
                    progress.CurrentLevel,
                    previousExp,
                    progress.CurrentExp,
                    promotionStage,
                    promotionStage,
                    maxLevel,
                    maxLevel));
            }

            return true;
        }

        public static bool ApplyApprovedPromotion(UnitDataSO unitData, UnitProgressData progress)
        {
            if (!IsValid(unitData, progress) || unitData.GrowthTable == null || !unitData.GrowthTable.TryGetProfile(unitData.Class, out UnitClassGrowthProfile profile))
            {
                return false;
            }

            int previousStage = progress.PromotionStage;
            int nextStage = previousStage + 1;

            if (!profile.HasPromotionStage(nextStage))
            {
                return false;
            }

            int previousMaxLevel = profile.GetMaxLevel(previousStage);
            int nextMaxLevel = profile.GetMaxLevel(nextStage);

            if (nextMaxLevel < progress.CurrentLevel || nextMaxLevel <= previousMaxLevel)
            {
                return false;
            }

            progress.SetPromotionStage(nextStage);

            UnitProgressEvents.PublishProgressChanged(new UnitProgressChangedInfo(
                unitData,
                progress,
                UnitProgressChangeType.Promotion,
                progress.CurrentLevel,
                progress.CurrentLevel,
                progress.CurrentExp,
                progress.CurrentExp,
                previousStage,
                nextStage,
                previousMaxLevel,
                nextMaxLevel));

            return true;
        }

        private static bool IsValid(UnitDataSO unitData, UnitProgressData progress)
        {
            return unitData != null && progress != null && progress.Matches(unitData);
        }
    }
}
