using System;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    internal sealed class LifeStealHandler : IEnemyBasicAttackResolvedPassiveHandler
    {
        public Type DataType => typeof(LifeStealSO);

        public void OnBasicAttackResolved(EnemyRuntimeState owner, UnitRuntimeState target, PassiveDataSO passive, PassiveTuning tuning, BasicAttackResult result)
        {
            if (owner == null || !owner.IsInitialized || owner.Health == null || owner.Health.IsDead || !result.WasHit || result.AppliedDamage <= 0f)
            {
                return;
            }

            LifeStealSO data = passive as LifeStealSO;

            if (data == null)
            {
                return;
            }

            float lifeStealPercent = tuning != null ? tuning.GetValue(PassiveValueKey.LifeStealPercent) : data.LifeStealPercent;

            if (float.IsNaN(lifeStealPercent) || float.IsInfinity(lifeStealPercent) || lifeStealPercent <= 0f)
            {
                return;
            }

            float healAmount = result.AppliedDamage * Mathf.Clamp(lifeStealPercent, 0f, 100f) * 0.01f;
            owner.Heal(healAmount);
        }
    }
}