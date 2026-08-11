using System;
using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    public static class UnitLevelCalculator
    {
        public static UnitLevelResult AddExperience(UnitProgressData progress, UnitLevelCurveSO levelCurve, int maxLevel, long gainedExp)
        {
            if (progress == null)
            {
                throw new ArgumentNullException(nameof(progress));
            }

            if (levelCurve == null)
            {
                throw new ArgumentNullException(nameof(levelCurve));
            }

            int previousLevel = progress.CurrentLevel;
            long previousExp = progress.CurrentExp;
            gainedExp = Math.Max(0L, gainedExp);
            maxLevel = Mathf.Max(previousLevel, maxLevel);

            if (gainedExp <= 0L)
            {
                return new UnitLevelResult(previousLevel, previousLevel, previousExp, previousExp, 0L, 0L, 0L, previousLevel >= maxLevel);
            }

            if (previousLevel >= maxLevel)
            {
                long discardedExp = AddSaturated(previousExp, gainedExp);
                progress.SetProgress(previousLevel, 0L);
                return new UnitLevelResult(previousLevel, previousLevel, previousExp, 0L, gainedExp, 0L, discardedExp, true);
            }

            long availableExp = AddWithOverflowCheck(previousExp, gainedExp, out long overflowExp);
            long consumedExp = 0L;
            long discardedExpTotal = overflowExp;
            int currentLevel = previousLevel;

            while (currentLevel < maxLevel)
            {
                long requiredExp = Math.Max(1L, levelCurve.GetRequiredExp(currentLevel));

                if (availableExp < requiredExp)
                {
                    break;
                }

                availableExp -= requiredExp;
                consumedExp = AddSaturated(consumedExp, requiredExp);
                currentLevel++;

                if (currentLevel >= maxLevel)
                {
                    discardedExpTotal = AddSaturated(discardedExpTotal, availableExp);
                    availableExp = 0L;
                    break;
                }
            }

            progress.SetProgress(currentLevel, availableExp);

            return new UnitLevelResult(previousLevel, currentLevel, previousExp, availableExp, gainedExp, consumedExp, discardedExpTotal, currentLevel >= maxLevel);
        }

        private static long AddWithOverflowCheck(long left, long right, out long overflow)
        {
            left = Math.Max(0L, left);
            right = Math.Max(0L, right);

            if (right <= long.MaxValue - left)
            {
                overflow = 0L;
                return left + right;
            }

            overflow = right - (long.MaxValue - left);
            return long.MaxValue;
        }

        private static long AddSaturated(long left, long right)
        {
            left = Math.Max(0L, left);
            right = Math.Max(0L, right);
            return right > long.MaxValue - left ? long.MaxValue : left + right;
        }
    }
}