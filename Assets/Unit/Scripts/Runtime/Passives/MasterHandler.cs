using System;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    internal sealed class MasterHandler : IUnitRuntimePassiveHandler, IUnitTargetLayerPassiveHandler
    {
        public Type DataType => typeof(MasterSO);

        public bool AllowsTargetLayer(UnitRuntimeState owner, PassiveDataSO passive, PassiveTuning tuning, CombatTargetLayer targetLayer)
        {
            return targetLayer == CombatTargetLayer.Ground || targetLayer == CombatTargetLayer.Air;
        }

        public IPassiveRuntimeBinding CreateBinding(UnitRuntimeState owner, PassiveDataSO passive, PassiveTuning tuning)
        {
            MasterSO data = passive as MasterSO;
            return owner == null || data == null || data.StatType == PassiveStatType.None ? null : new Binding(owner, data, tuning);
        }

        private sealed class Binding : IPassiveRuntimeBinding
        {
            private readonly UnitRuntimeState owner;
            private readonly MasterSO data;
            private readonly PassiveTuning tuning;

            public Binding(UnitRuntimeState owner, MasterSO data, PassiveTuning tuning)
            {
                this.owner = owner;
                this.data = data;
                this.tuning = tuning;
            }

            public void Activate()
            {
                float bonus = tuning != null ? tuning.GetValue(PassiveValueKey.StatBonusPercent) : data.StatBonusPercent;
                owner.Statuses?.ApplyPersistentModifier(owner, data, data.StatType, 0f, Mathf.Max(0f, bonus), false);
                SyncMaxHp();
            }

            public void Deactivate()
            {
                owner.Statuses?.RemoveModifier(owner, data, data.StatType);
                SyncMaxHp();
            }

            private void SyncMaxHp()
            {
                if (data.StatType == PassiveStatType.MaxHp && owner.Stats != null && owner.Stats.IsInitialized)
                {
                    owner.SyncHealthMaxHpFromStats();
                }
            }
        }
    }
}
