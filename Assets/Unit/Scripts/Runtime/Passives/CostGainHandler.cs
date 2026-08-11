using System;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    internal sealed class CostGainHandler : IUnitRuntimePassiveHandler, IUnitBasicAttackResolvedPassiveHandler, IUnitBasicAttackReceivedPassiveHandler
    {
        public Type DataType => typeof(CostGainPassiveSO);

        public IPassiveRuntimeBinding CreateBinding(UnitRuntimeState owner, PassiveDataSO passive, PassiveTuning tuning)
        {
            CostGainPassiveSO data = passive as CostGainPassiveSO;

            if (owner == null || data == null)
            {
                return null;
            }

            switch (data.Trigger)
            {
                case CostGainTrigger.AllySummonCreated:
                case CostGainTrigger.OwnSkillSucceeded:
                case CostGainTrigger.AllySummonDestroyed:
                    return new Binding(owner, data, tuning);

                default:
                    return null;
            }
        }

        public void OnBasicAttackResolved(UnitRuntimeState owner, EnemyRuntimeState target, PassiveDataSO passive, PassiveTuning tuning, BasicAttackResult result)
        {
            CostGainPassiveSO data = passive as CostGainPassiveSO;

            if (owner == null || data == null || !result.WasHit)
            {
                return;
            }

            if (data.Trigger == CostGainTrigger.BasicAttackHit || (data.Trigger == CostGainTrigger.CriticalHit && result.IsCritical))
            {
                Request(owner, data, tuning);
            }
        }

        public void OnBasicAttackReceived(UnitRuntimeState owner, EnemyRuntimeState attacker, PassiveDataSO passive, PassiveTuning tuning, BasicAttackResult result)
        {
            CostGainPassiveSO data = passive as CostGainPassiveSO;

            if (owner == null || data == null || data.Trigger != CostGainTrigger.EvadeSuccess || !result.Succeeded || result.WasHit)
            {
                return;
            }

            Request(owner, data, tuning);
        }

        private static void Request(UnitRuntimeState owner, CostGainPassiveSO data, PassiveTuning tuning)
        {
            float rawAmount = tuning != null ? tuning.GetValue(PassiveValueKey.SummonCostGain) : data.SummonCostGain;
            int amount = Mathf.Max(0, Mathf.RoundToInt(rawAmount));
            PassiveRuntimeEvents.RequestSummonCostGain(owner, amount, data);
        }

        private sealed class Binding : IPassiveRuntimeBinding
        {
            private readonly UnitRuntimeState owner;
            private readonly CostGainPassiveSO data;
            private readonly PassiveTuning tuning;
            private bool active;

            public Binding(UnitRuntimeState owner, CostGainPassiveSO data, PassiveTuning tuning)
            {
                this.owner = owner;
                this.data = data;
                this.tuning = tuning;
            }

            public void Activate()
            {
                if (active)
                {
                    return;
                }

                active = true;
                PassiveRuntimeEvents.OnUnitSkillSucceeded += HandleSkillSucceeded;
                PassiveRuntimeEvents.OnUnitSummonCreated += HandleSummonCreated;
                PassiveRuntimeEvents.OnUnitSummonDestroyed += HandleSummonDestroyed;
            }

            public void Deactivate()
            {
                if (!active)
                {
                    return;
                }

                active = false;
                PassiveRuntimeEvents.OnUnitSkillSucceeded -= HandleSkillSucceeded;
                PassiveRuntimeEvents.OnUnitSummonCreated -= HandleSummonCreated;
                PassiveRuntimeEvents.OnUnitSummonDestroyed -= HandleSummonDestroyed;
            }

            private void HandleSkillSucceeded(UnitRuntimeState source)
            {
                if (data.Trigger == CostGainTrigger.OwnSkillSucceeded && source == owner)
                {
                    Request(owner, data, tuning);
                }
            }

            private void HandleSummonCreated(UnitRuntimeState source, GameObject summon)
            {
                if (data.Trigger == CostGainTrigger.AllySummonCreated)
                {
                    Request(owner, data, tuning);
                }
            }

            private void HandleSummonDestroyed(UnitRuntimeState source, GameObject summon)
            {
                if (data.Trigger == CostGainTrigger.AllySummonDestroyed)
                {
                    Request(owner, data, tuning);
                }
            }
        }
    }
}
