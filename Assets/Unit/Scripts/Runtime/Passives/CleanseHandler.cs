using System;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    internal sealed class CleanseHandler : IEnemyRuntimePassiveHandler
    {
        public Type DataType => typeof(CleanseSO);

        public IPassiveRuntimeBinding CreateBinding(EnemyRuntimeState owner, PassiveDataSO passive, PassiveTuning tuning)
        {
            CleanseSO data = passive as CleanseSO;
            return owner == null || data == null ? null : new Binding(data, tuning);
        }

        private sealed class Binding : IPassiveRuntimeBinding, IPassiveTickBinding
        {
            private readonly CleanseSO data;
            private readonly PassiveTuning tuning;
            private float elapsed;
            private bool active;

            public Binding(CleanseSO data, PassiveTuning tuning)
            {
                this.data = data;
                this.tuning = tuning;
            }

            public void Activate()
            {
                elapsed = 0f;
                active = true;
            }

            public void Deactivate()
            {
                active = false;
                elapsed = 0f;
            }

            public void Step(float deltaTime)
            {
                if (!active || deltaTime <= 0f)
                {
                    return;
                }

                float interval = tuning != null ? tuning.GetValue(PassiveValueKey.CleanseIntervalSeconds) : data.CleanseIntervalSeconds;
                interval = Mathf.Max(0.1f, interval);
                elapsed += deltaTime;

                while (elapsed >= interval)
                {
                    elapsed -= interval;
                    CleanseAllies();
                }
            }

            private static void CleanseAllies()
            {
                foreach (EnemyRuntimeState enemy in CombatRegistry.Enemies)
                {
                    if (enemy != null && enemy.IsInitialized && enemy.Health != null && !enemy.Health.IsDead)
                    {
                        enemy.Statuses?.CleanseNegative();
                    }
                }
            }
        }
    }
}
