using System;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    internal sealed class CritBuffHandler : IUnitBasicAttackResolvedPassiveHandler
    {
        public Type DataType => typeof(CritBuffSO);

        public void OnBasicAttackResolved(UnitRuntimeState owner, EnemyRuntimeState target, PassiveDataSO passive, PassiveTuning tuning, BasicAttackResult result)
        {
            if (owner == null || !result.WasHit || !result.IsCritical)
            {
                return;
            }

            CritBuffSO data = passive as CritBuffSO;

            if (data == null)
            {
                return;
            }

            float bonusPercent = tuning != null ? tuning.GetValue(PassiveValueKey.FinalDamageBonusPercent) : data.FinalDamageBonusPercent;
            float duration = tuning != null ? tuning.GetValue(PassiveValueKey.DurationSeconds) : data.DurationSeconds;
            owner.Passives?.SetTimedOutgoingDamageBonus(passive, Mathf.Max(0f, bonusPercent), Mathf.Max(0f, duration));
        }
    }
}
