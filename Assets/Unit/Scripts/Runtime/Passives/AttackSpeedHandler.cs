using System;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    internal sealed class AttackSpeedHandler : IUnitBasicAttackResolvedPassiveHandler
    {
        public Type DataType => typeof(AttackSpeedSO);

        public void OnBasicAttackResolved(UnitRuntimeState owner, EnemyRuntimeState target, PassiveDataSO passive, PassiveTuning tuning, BasicAttackResult result)
        {
            AttackSpeedSO data = passive as AttackSpeedSO;

            if (owner == null || target == null || data == null || !result.WasHit || target.DataLink == null || !target.DataLink.HasData || target.DataLink.EnemyData.Size != data.TargetSize)
            {
                return;
            }

            float bonusPercent = tuning != null ? tuning.GetValue(PassiveValueKey.AttackSpeedBonusPercent) : data.AttackSpeedBonusPercent;
            float duration = tuning != null ? tuning.GetValue(PassiveValueKey.DurationSeconds) : data.DurationSeconds;

            owner.Statuses?.ApplyTimedModifier(owner, passive, PassiveStatType.AttacksPerSecond, 0f, Mathf.Max(0f, bonusPercent), Mathf.Max(0f, duration), false);
        }
    }
}
