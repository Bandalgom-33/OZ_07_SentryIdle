using System;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    internal sealed class RushHandler : IEnemyRuntimePassiveHandler, IEnemyBlockedPassiveHandler
    {
        public Type DataType => typeof(RushSO);

        public IPassiveRuntimeBinding CreateBinding(EnemyRuntimeState owner, PassiveDataSO passive, PassiveTuning tuning)
        {
            RushSO data = passive as RushSO;
            return owner == null || data == null ? null : new Binding(owner, data, tuning);
        }

        public void OnBlocked(EnemyRuntimeState owner, UnitRuntimeState blocker, PassiveDataSO passive, PassiveTuning tuning)
        {
            if (owner == null)
            {
                return;
            }

            owner.Statuses?.RemoveModifier(owner, passive, PassiveStatType.MoveSpeed);
        }

        private sealed class Binding : IPassiveRuntimeBinding
        {
            private readonly EnemyRuntimeState owner;
            private readonly RushSO data;
            private readonly PassiveTuning tuning;

            public Binding(EnemyRuntimeState owner, RushSO data, PassiveTuning tuning)
            {
                this.owner = owner;
                this.data = data;
                this.tuning = tuning;
            }

            public void Activate()
            {
                float bonus = tuning != null ? tuning.GetValue(PassiveValueKey.BonusMoveSpeedPercent) : data.BonusMoveSpeedPercent;
                owner.Statuses?.ApplyPersistentModifier(owner, data, PassiveStatType.MoveSpeed, 0f, Mathf.Max(0f, bonus), false);
            }

            public void Deactivate()
            {
                owner.Statuses?.RemoveModifier(owner, data, PassiveStatType.MoveSpeed);
            }
        }
    }
}
