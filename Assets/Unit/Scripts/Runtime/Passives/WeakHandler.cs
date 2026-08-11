using System;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    internal sealed class WeakHandler : IUnitBasicAttackResolvedPassiveHandler, IEnemyBasicAttackResolvedPassiveHandler
    {
        public Type DataType => typeof(WeakSO);

        public void OnBasicAttackResolved(UnitRuntimeState owner, EnemyRuntimeState target, PassiveDataSO passive, PassiveTuning tuning, BasicAttackResult result)
        {
            if (owner == null || target == null || !result.WasHit)
            {
                return;
            }

            Apply(owner, target.Statuses, passive as WeakSO, tuning);
        }

        public void OnBasicAttackResolved(EnemyRuntimeState owner, UnitRuntimeState target, PassiveDataSO passive, PassiveTuning tuning, BasicAttackResult result)
        {
            if (owner == null || target == null || !result.WasHit)
            {
                return;
            }

            Apply(owner, target.Statuses, passive as WeakSO, tuning);
        }

        private static void Apply(UnityEngine.Object source, PassiveStatusRuntime statuses, WeakSO data, PassiveTuning tuning)
        {
            if (statuses == null || data == null)
            {
                return;
            }

            float physical = tuning != null ? tuning.GetValue(PassiveValueKey.PhysicalDefenseReductionPercent) : data.PhysicalDefenseReductionPercent;
            float magical = tuning != null ? tuning.GetValue(PassiveValueKey.MagicalDefenseReductionPercent) : data.MagicalDefenseReductionPercent;
            float duration = tuning != null ? tuning.GetValue(PassiveValueKey.DurationSeconds) : data.DurationSeconds;

            statuses.ApplyTimedModifier(source, data, PassiveStatType.PhysicalDefense, 0f, -Mathf.Clamp(physical, 0f, 100f), Mathf.Max(0f, duration), true);
            statuses.ApplyTimedModifier(source, data, PassiveStatType.MagicalDefense, 0f, -Mathf.Clamp(magical, 0f, 100f), Mathf.Max(0f, duration), true);
        }
    }
}
