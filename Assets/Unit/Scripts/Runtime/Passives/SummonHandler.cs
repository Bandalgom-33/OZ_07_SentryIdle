using System;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    internal sealed class SummonHandler : IEnemyRuntimePassiveHandler
    {
        public Type DataType => typeof(SummonSO);

        public IPassiveRuntimeBinding CreateBinding(EnemyRuntimeState owner, PassiveDataSO passive, PassiveTuning tuning)
        {
            SummonSO data = passive as SummonSO;
            return owner == null || data == null ? null : new Binding(owner, data, tuning);
        }

        private sealed class Binding : IPassiveRuntimeBinding, IPassiveTickBinding
        {
            private readonly EnemyRuntimeState owner;
            private readonly SummonSO data;
            private readonly PassiveTuning tuning;
            private float elapsed;
            private bool active;

            public Binding(EnemyRuntimeState owner, SummonSO data, PassiveTuning tuning)
            {
                this.owner = owner;
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
                if (!active || owner == null || owner.Health == null || owner.Health.IsDead || deltaTime <= 0f)
                {
                    return;
                }

                float interval = tuning != null ? tuning.GetValue(PassiveValueKey.SummonIntervalSeconds) : data.SummonIntervalSeconds;
                interval = Mathf.Max(0.1f, interval);
                elapsed += deltaTime;

                while (elapsed >= interval)
                {
                    elapsed -= interval;
                    float rawCount = tuning != null ? tuning.GetValue(PassiveValueKey.SummonCount) : data.SummonCount;
                    int count = Mathf.Max(1, Mathf.RoundToInt(rawCount));
                    GameObject prefab = tuning != null ? tuning.GetReference<GameObject>(PassiveRefKey.SummonPrefab) : data.SummonPrefab;
                    PassiveRuntimeEvents.RequestSummon(owner, prefab, count, data);
                }
            }
        }
    }
}
