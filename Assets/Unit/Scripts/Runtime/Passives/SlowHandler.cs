using System;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    internal sealed class SlowHandler : IUnitBasicAttackResolvedPassiveHandler
    {
        public Type DataType => typeof(SlowSO);

        public void OnBasicAttackResolved(UnitRuntimeState owner, EnemyRuntimeState target, PassiveDataSO passive, PassiveTuning tuning, BasicAttackResult result)
        {
            SlowSO data = passive as SlowSO;

            if (owner == null || target == null || data == null || !result.WasHit)
            {
                return;
            }

            float reduction = tuning != null ? tuning.GetValue(PassiveValueKey.MoveSpeedReductionPercent) : data.MoveSpeedReductionPercent;
            float duration = tuning != null ? tuning.GetValue(PassiveValueKey.DurationSeconds) : data.DurationSeconds;

            target.Statuses?.ApplyTimedModifier(owner, passive, PassiveStatType.MoveSpeed, 0f, -Mathf.Clamp(reduction, 0f, 100f), Mathf.Max(0f, duration), true);
        }
    }
}
