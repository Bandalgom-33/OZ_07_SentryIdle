using System;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    internal sealed class BerserkHandler : IEnemyRuntimePassiveHandler
    {
        public Type DataType => typeof(BerserkSO);

        public IPassiveRuntimeBinding CreateBinding(EnemyRuntimeState owner, PassiveDataSO passive, PassiveTuning tuning)
        {
            BerserkSO data = passive as BerserkSO;
            return owner == null || data == null ? null : new Binding(owner, data, tuning);
        }

        private sealed class Binding : IPassiveRuntimeBinding
        {
            private readonly EnemyRuntimeState owner;
            private readonly BerserkSO data;
            private readonly PassiveTuning tuning;
            private bool active;

            public Binding(EnemyRuntimeState owner, BerserkSO data, PassiveTuning tuning)
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
                owner.Statuses?.RemoveModifier(owner, data, PassiveStatType.MagicalAttack);
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

                float lostHpPercent = (1f - owner.Health.NormalizedHp) * 100f;
                float physicalPerLost = tuning != null ? tuning.GetValue(PassiveValueKey.PhysicalAttackPerLostHpPercent) : data.PhysicalAttackPerLostHpPercent;
                float maxPhysical = tuning != null ? tuning.GetValue(PassiveValueKey.MaxPhysicalAttackBonusPercent) : data.MaxPhysicalAttackBonusPercent;
                float magicalPerLost = tuning != null ? tuning.GetValue(PassiveValueKey.MagicalAttackPerLostHpPercent) : data.MagicalAttackPerLostHpPercent;
                float maxMagical = tuning != null ? tuning.GetValue(PassiveValueKey.MaxMagicalAttackBonusPercent) : data.MaxMagicalAttackBonusPercent;

                float physicalBonus = Mathf.Min(Mathf.Max(0f, maxPhysical), lostHpPercent * Mathf.Max(0f, physicalPerLost));
                float magicalBonus = Mathf.Min(Mathf.Max(0f, maxMagical), lostHpPercent * Mathf.Max(0f, magicalPerLost));

                owner.Statuses?.ApplyPersistentModifier(owner, data, PassiveStatType.PhysicalAttack, 0f, physicalBonus, false);
                owner.Statuses?.ApplyPersistentModifier(owner, data, PassiveStatType.MagicalAttack, 0f, magicalBonus, false);
            }
        }
    }
}
