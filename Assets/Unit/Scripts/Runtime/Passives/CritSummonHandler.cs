using System;
using System.Runtime.CompilerServices;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    internal sealed class CritSummonHandler : IUnitBasicAttackResolvedPassiveHandler
    {
        private readonly ConditionalWeakTable<UnitRuntimeState, CooldownState> cooldownStates = new ConditionalWeakTable<UnitRuntimeState, CooldownState>();

        public Type DataType => typeof(CritSummonSO);

        public void OnBasicAttackResolved(UnitRuntimeState owner, EnemyRuntimeState target, PassiveDataSO passive, PassiveTuning tuning, BasicAttackResult result)
        {
            if (owner == null || owner.IsSummon || !result.WasHit || !result.IsCritical)
            {
                return;
            }

            CritSummonSO data = passive as CritSummonSO;

            if (data == null)
            {
                return;
            }

            GameObject prefab = tuning != null ? tuning.GetReference<GameObject>(PassiveRefKey.SummonPrefab) : data.SummonPrefab;

            if (prefab == null)
            {
                return;
            }

            CooldownState cooldownState = cooldownStates.GetOrCreateValue(owner);
            float currentTime = Time.time;

            if (currentTime < cooldownState.NextAllowedTime)
            {
                return;
            }

            if (CountActiveSummons(owner, passive) >= data.MaxActiveSummons)
            {
                return;
            }

            PassiveRuntimeEvents.RequestSummon(owner, prefab, 1, passive);
            cooldownState.NextAllowedTime = currentTime + data.SummonCooldownSeconds;
        }

        private static int CountActiveSummons(UnitRuntimeState owner, PassiveDataSO passive)
        {
            int count = 0;

            foreach (UnitRuntimeState unit in CombatRegistry.Units)
            {
                if (unit == null || !unit.IsSummon || !unit.IsInitialized || unit.Health == null || unit.Health.IsDead || unit.SummonRuntime == null || !unit.SummonRuntime.IsInitialized)
                {
                    continue;
                }

                if (unit.SummonRuntime.Owner != owner || unit.SummonRuntime.Source != passive)
                {
                    continue;
                }

                count++;
            }

            return count;
        }

        private sealed class CooldownState
        {
            public float NextAllowedTime;
        }
    }
}