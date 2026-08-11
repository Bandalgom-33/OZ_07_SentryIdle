using System;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    internal sealed class SummonDefenseHandler : IUnitRuntimePassiveHandler
    {
        public Type DataType => typeof(SummonDefenseSO);

        public IPassiveRuntimeBinding CreateBinding(UnitRuntimeState owner, PassiveDataSO passive, PassiveTuning tuning)
        {
            SummonDefenseSO data = passive as SummonDefenseSO;
            return owner == null || data == null ? null : new Binding(owner, data, tuning);
        }

        private sealed class Binding : IPassiveRuntimeBinding
        {
            private readonly UnitRuntimeState owner;
            private readonly SummonDefenseSO data;
            private readonly PassiveTuning tuning;
            private bool active;

            public Binding(UnitRuntimeState owner, SummonDefenseSO data, PassiveTuning tuning)
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
                PassiveRuntimeEvents.OnUnitSummonCreated += HandleSummonChanged;
                PassiveRuntimeEvents.OnUnitSummonDestroyed += HandleSummonChanged;
                Refresh();
            }

            public void Deactivate()
            {
                if (!active)
                {
                    return;
                }

                active = false;
                PassiveRuntimeEvents.OnUnitSummonCreated -= HandleSummonChanged;
                PassiveRuntimeEvents.OnUnitSummonDestroyed -= HandleSummonChanged;
                owner.Statuses?.RemoveModifier(owner, data, PassiveStatType.PhysicalDefense);
                owner.Statuses?.RemoveModifier(owner, data, PassiveStatType.MagicalDefense);
            }

            private void HandleSummonChanged(UnitRuntimeState source, GameObject summon)
            {
                Refresh();
            }

            private void Refresh()
            {
                int count = PassiveRuntimeEvents.ActiveUnitSummonCount;
                float physicalPerSummon = tuning != null ? tuning.GetValue(PassiveValueKey.PhysicalDefensePerSummonPercent) : data.PhysicalDefensePerSummonPercent;
                float magicalPerSummon = tuning != null ? tuning.GetValue(PassiveValueKey.MagicalDefensePerSummonPercent) : data.MagicalDefensePerSummonPercent;

                owner.Statuses?.ApplyPersistentModifier(owner, data, PassiveStatType.PhysicalDefense, 0f, Mathf.Max(0f, physicalPerSummon) * count, false);
                owner.Statuses?.ApplyPersistentModifier(owner, data, PassiveStatType.MagicalDefense, 0f, Mathf.Max(0f, magicalPerSummon) * count, false);
            }
        }
    }
}
