using System;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    internal sealed class DefenseBuffHandler : IUnitBasicAttackReceivedPassiveHandler, IUnitBlockStartedPassiveHandler, IUnitBlockEndedPassiveHandler
    {
        public Type DataType => typeof(DefenseBuffSO);

        public void OnBasicAttackReceived(UnitRuntimeState owner, EnemyRuntimeState attacker, PassiveDataSO passive, PassiveTuning tuning, BasicAttackResult result)
        {
            DefenseBuffSO data = passive as DefenseBuffSO;

            if (owner == null || data == null || data.Trigger != DefenseBuffTrigger.EvadeSuccess || !result.Succeeded || result.WasHit)
            {
                return;
            }

            float physical = tuning != null ? tuning.GetValue(PassiveValueKey.PhysicalDefenseBonusPercent) : data.PhysicalDefenseBonusPercent;
            float magical = tuning != null ? tuning.GetValue(PassiveValueKey.MagicalDefenseBonusPercent) : data.MagicalDefenseBonusPercent;
            float duration = tuning != null ? tuning.GetValue(PassiveValueKey.DurationSeconds) : data.DurationSeconds;

            owner.Statuses?.ApplyTimedModifier(owner, passive, PassiveStatType.PhysicalDefense, 0f, Mathf.Max(0f, physical), Mathf.Max(0f, duration), false);
            owner.Statuses?.ApplyTimedModifier(owner, passive, PassiveStatType.MagicalDefense, 0f, Mathf.Max(0f, magical), Mathf.Max(0f, duration), false);
        }

        public void OnBlockStarted(UnitRuntimeState owner, EnemyRuntimeState enemy, PassiveDataSO passive, PassiveTuning tuning)
        {
            RefreshBlockingBuff(owner, passive as DefenseBuffSO, tuning);
        }

        public void OnBlockEnded(UnitRuntimeState owner, EnemyRuntimeState enemy, PassiveDataSO passive, PassiveTuning tuning)
        {
            RefreshBlockingBuff(owner, passive as DefenseBuffSO, tuning);
        }

        private static void RefreshBlockingBuff(UnitRuntimeState owner, DefenseBuffSO data, PassiveTuning tuning)
        {
            if (owner == null || data == null || data.Trigger == DefenseBuffTrigger.EvadeSuccess || data.Trigger == DefenseBuffTrigger.None)
            {
                return;
            }

            bool matchingBlockedEnemy = HasMatchingBlockedEnemy(owner, data.Trigger);

            if (!matchingBlockedEnemy)
            {
                owner.Statuses?.RemoveModifier(owner, data, PassiveStatType.PhysicalDefense);
                owner.Statuses?.RemoveModifier(owner, data, PassiveStatType.MagicalDefense);
                return;
            }

            float physical = tuning != null ? tuning.GetValue(PassiveValueKey.PhysicalDefenseBonusPercent) : data.PhysicalDefenseBonusPercent;
            float magical = tuning != null ? tuning.GetValue(PassiveValueKey.MagicalDefenseBonusPercent) : data.MagicalDefenseBonusPercent;

            owner.Statuses?.ApplyPersistentModifier(owner, data, PassiveStatType.PhysicalDefense, 0f, Mathf.Max(0f, physical), false);
            owner.Statuses?.ApplyPersistentModifier(owner, data, PassiveStatType.MagicalDefense, 0f, Mathf.Max(0f, magical), false);
        }

        private static bool HasMatchingBlockedEnemy(UnitRuntimeState owner, DefenseBuffTrigger trigger)
        {
            if (owner.Block == null)
            {
                return false;
            }

            EnemySize expectedSize;

            switch (trigger)
            {
                case DefenseBuffTrigger.BlockingSmall:
                    expectedSize = EnemySize.Small;
                    break;
                case DefenseBuffTrigger.BlockingMedium:
                    expectedSize = EnemySize.Medium;
                    break;
                case DefenseBuffTrigger.BlockingLarge:
                    expectedSize = EnemySize.Large;
                    break;
                default:
                    return false;
            }

            for (int i = 0; i < owner.Block.Enemies.Count; i++)
            {
                EnemyRuntimeState enemy = owner.Block.Enemies[i] != null ? owner.Block.Enemies[i].State : null;

                if (enemy != null && enemy.DataLink != null && enemy.DataLink.HasData && enemy.DataLink.EnemyData.Size == expectedSize)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
