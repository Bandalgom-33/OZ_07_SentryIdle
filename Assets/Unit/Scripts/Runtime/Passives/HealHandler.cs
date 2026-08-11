using System;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    internal sealed class HealHandler : IEnemyRuntimePassiveHandler
    {
        public Type DataType => typeof(HealSO);

        public IPassiveRuntimeBinding CreateBinding(EnemyRuntimeState owner, PassiveDataSO passive, PassiveTuning tuning)
        {
            HealSO data = passive as HealSO;
            return owner == null || data == null ? null : new Binding(data, tuning);
        }

        private sealed class Binding : IPassiveRuntimeBinding, IPassiveTickBinding
        {
            private readonly HealSO data;
            private readonly PassiveTuning tuning;
            private float elapsed;
            private bool active;

            public Binding(HealSO data, PassiveTuning tuning)
            {
                this.data = data;
                this.tuning = tuning;
            }

            public void Activate()
            {
                active = true;
                elapsed = 0f;
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

                float interval = tuning != null ? tuning.GetValue(PassiveValueKey.HealIntervalSeconds) : data.HealIntervalSeconds;
                interval = Mathf.Max(0.1f, interval);
                elapsed += deltaTime;

                while (elapsed >= interval)
                {
                    elapsed -= interval;
                    HealLowestAlly();
                }
            }

            private void HealLowestAlly()
            {
                EnemyRuntimeState best = null;
                float bestNormalizedHp = float.MaxValue;
                int bestId = int.MaxValue;

                foreach (EnemyRuntimeState enemy in CombatRegistry.Enemies)
                {
                    if (enemy == null || !enemy.IsInitialized || enemy.Health == null || enemy.Health.IsDead || enemy.Health.CurrentHp >= enemy.Health.MaxHp)
                    {
                        continue;
                    }

                    float normalizedHp = enemy.Health.NormalizedHp;
                    int id = enemy.GetInstanceID();

                    if (normalizedHp < bestNormalizedHp || (Mathf.Approximately(normalizedHp, bestNormalizedHp) && id < bestId))
                    {
                        best = enemy;
                        bestNormalizedHp = normalizedHp;
                        bestId = id;
                    }
                }

                if (best == null)
                {
                    return;
                }

                float amount = tuning != null ? tuning.GetValue(PassiveValueKey.HealAmount) : data.HealAmount;
                best.Heal(Mathf.Max(0f, amount));
            }
        }
    }
}
