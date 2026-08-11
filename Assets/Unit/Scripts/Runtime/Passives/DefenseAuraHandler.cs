using System;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    internal sealed class DefenseAuraHandler : IEnemyRuntimePassiveHandler
    {
        public Type DataType => typeof(DefenseAuraSO);

        public IPassiveRuntimeBinding CreateBinding(EnemyRuntimeState owner, PassiveDataSO passive, PassiveTuning tuning)
        {
            DefenseAuraSO data = passive as DefenseAuraSO;
            return owner == null || data == null ? null : new Binding(owner, data, tuning);
        }

        private sealed class Binding : IPassiveRuntimeBinding
        {
            private readonly EnemyRuntimeState owner;
            private readonly DefenseAuraSO data;
            private readonly PassiveTuning tuning;
            private bool active;

            public Binding(EnemyRuntimeState owner, DefenseAuraSO data, PassiveTuning tuning)
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
                CombatRegistry.OnEnemyRegistered += HandleEnemyRegistered;

                if (owner.Health != null)
                {
                    owner.Health.OnDied += HandleOwnerDied;
                }

                ApplyTo(owner);

                foreach (EnemyRuntimeState enemy in CombatRegistry.Enemies)
                {
                    ApplyTo(enemy);
                }
            }

            public void Deactivate()
            {
                if (!active)
                {
                    return;
                }

                active = false;
                CombatRegistry.OnEnemyRegistered -= HandleEnemyRegistered;

                if (owner.Health != null)
                {
                    owner.Health.OnDied -= HandleOwnerDied;
                }

                RemoveFrom(owner);

                foreach (EnemyRuntimeState enemy in CombatRegistry.Enemies)
                {
                    RemoveFrom(enemy);
                }
            }

            private void HandleEnemyRegistered(EnemyRuntimeState enemy)
            {
                ApplyTo(enemy);
            }

            private void HandleOwnerDied(CombatHealth health)
            {
                Deactivate();
            }

            private void ApplyTo(EnemyRuntimeState target)
            {
                if (target == null || target.Statuses == null)
                {
                    return;
                }

                float physical = tuning != null ? tuning.GetValue(PassiveValueKey.PhysicalDefenseBonusPercent) : data.PhysicalDefenseBonusPercent;
                float magical = tuning != null ? tuning.GetValue(PassiveValueKey.MagicalDefenseBonusPercent) : data.MagicalDefenseBonusPercent;

                target.Statuses.ApplyPersistentModifier(owner, data, PassiveStatType.PhysicalDefense, 0f, Mathf.Max(0f, physical), false);
                target.Statuses.ApplyPersistentModifier(owner, data, PassiveStatType.MagicalDefense, 0f, Mathf.Max(0f, magical), false);
            }

            private void RemoveFrom(EnemyRuntimeState target)
            {
                if (target == null || target.Statuses == null)
                {
                    return;
                }

                target.Statuses.RemoveModifier(owner, data, PassiveStatType.PhysicalDefense);
                target.Statuses.RemoveModifier(owner, data, PassiveStatType.MagicalDefense);
            }
        }
    }
}
