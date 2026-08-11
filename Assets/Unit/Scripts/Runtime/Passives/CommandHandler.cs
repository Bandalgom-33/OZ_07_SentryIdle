using System;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    internal sealed class CommandHandler : IEnemyRuntimePassiveHandler
    {
        public Type DataType => typeof(CommandSO);

        public IPassiveRuntimeBinding CreateBinding(EnemyRuntimeState owner, PassiveDataSO passive, PassiveTuning tuning)
        {
            CommandSO data = passive as CommandSO;
            return owner == null || data == null ? null : new Binding(owner, data, tuning);
        }

        private sealed class Binding : IPassiveRuntimeBinding
        {
            private readonly EnemyRuntimeState owner;
            private readonly CommandSO data;
            private readonly PassiveTuning tuning;
            private bool active;

            public Binding(EnemyRuntimeState owner, CommandSO data, PassiveTuning tuning)
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

                float attack = tuning != null ? tuning.GetValue(PassiveValueKey.AttackBonusPercent) : data.AttackBonusPercent;
                float speed = tuning != null ? tuning.GetValue(PassiveValueKey.AttackSpeedBonusPercent) : data.AttackSpeedBonusPercent;

                target.Statuses.ApplyPersistentModifier(owner, data, PassiveStatType.PhysicalAttack, 0f, Mathf.Max(0f, attack), false);
                target.Statuses.ApplyPersistentModifier(owner, data, PassiveStatType.MagicalAttack, 0f, Mathf.Max(0f, attack), false);
                target.Statuses.ApplyPersistentModifier(owner, data, PassiveStatType.AttacksPerSecond, 0f, Mathf.Max(0f, speed), false);
            }

            private void RemoveFrom(EnemyRuntimeState target)
            {
                if (target == null || target.Statuses == null)
                {
                    return;
                }

                target.Statuses.RemoveModifier(owner, data, PassiveStatType.PhysicalAttack);
                target.Statuses.RemoveModifier(owner, data, PassiveStatType.MagicalAttack);
                target.Statuses.RemoveModifier(owner, data, PassiveStatType.AttacksPerSecond);
            }
        }
    }
}
