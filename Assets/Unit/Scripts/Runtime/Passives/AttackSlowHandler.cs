using System;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    internal sealed class AttackSlowHandler : IEnemyBasicAttackResolvedPassiveHandler
    {
        public Type DataType => typeof(AttackSlowSO);

        public void OnBasicAttackResolved(EnemyRuntimeState owner, UnitRuntimeState target, PassiveDataSO passive, PassiveTuning tuning, BasicAttackResult result)
        {
            AttackSlowSO data = passive as AttackSlowSO;

            if (owner == null || target == null || data == null || !result.WasHit)
            {
                return;
            }

            float reduction = tuning != null ? tuning.GetValue(PassiveValueKey.AttackSpeedReductionPercent) : data.AttackSpeedReductionPercent;
            float duration = tuning != null ? tuning.GetValue(PassiveValueKey.DurationSeconds) : data.DurationSeconds;

            target.Statuses?.ApplyTimedModifier(owner, data, PassiveStatType.AttacksPerSecond, 0f, -Mathf.Clamp(reduction, 0f, 100f), Mathf.Max(0f, duration), true);
        }
    }
}
