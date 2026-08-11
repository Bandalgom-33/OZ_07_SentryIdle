using System;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    internal sealed class LostHpAttackHandler : IUnitRuntimePassiveHandler
    {
        public Type DataType => typeof(LostHpAttackSO);

        public IPassiveRuntimeBinding CreateBinding(UnitRuntimeState owner, PassiveDataSO passive, PassiveTuning tuning)
        {
            LostHpAttackSO data = passive as LostHpAttackSO;
            return owner == null || data == null ? null : new Binding(owner, data, tuning);
        }

        private sealed class Binding : IPassiveRuntimeBinding
        {
            private readonly UnitRuntimeState owner;
            private readonly LostHpAttackSO data;
            private readonly PassiveTuning tuning;
            private bool active;

            public Binding(UnitRuntimeState owner, LostHpAttackSO data, PassiveTuning tuning)
            {
                this.owner = owner;
                this.data = data;
                this.tuning = tuning;
            }

            public void Activate()
            {
                if (active || owner.Health == null)
                {
                    return;
                }

                active = true;
                owner.Health.OnHealthChanged += HandleHealthChanged;
                Refresh();
            }

            public void Deactivate()
            {
                if (!active)
                {
                    return;
                }

                active = false;
                owner.Health.OnHealthChanged -= HandleHealthChanged;
                owner.Statuses?.RemoveModifier(owner, data, PassiveStatType.PhysicalAttack);
            }

            private void HandleHealthChanged(CombatHealth health)
            {
                Refresh();
            }

            private void Refresh()
            {
                if (owner.Health == null || owner.Health.IsDead)
                {
                    return;
                }

                float perLost = tuning != null ? tuning.GetValue(PassiveValueKey.PhysicalAttackPerLostHpPercent) : data.PhysicalAttackPerLostHpPercent;
                float maxBonus = tuning != null ? tuning.GetValue(PassiveValueKey.MaxPhysicalAttackBonusPercent) : data.MaxPhysicalAttackBonusPercent;
                float lostHpPercent = (1f - owner.Health.NormalizedHp) * 100f;
                float bonus = Mathf.Min(Mathf.Max(0f, maxBonus), lostHpPercent * Mathf.Max(0f, perLost));
                owner.Statuses?.ApplyPersistentModifier(owner, data, PassiveStatType.PhysicalAttack, 0f, bonus, false);
            }
        }
    }
}
