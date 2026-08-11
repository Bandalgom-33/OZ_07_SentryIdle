using System;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    internal sealed class ReflectHandler : IEnemyBasicAttackReceivedPassiveHandler
    {
        public Type DataType => typeof(ReflectSO);

        public void OnBasicAttackReceived(EnemyRuntimeState owner, UnitRuntimeState attacker, PassiveDataSO passive, PassiveTuning tuning, BasicAttackResult result)
        {
            ReflectSO data = passive as ReflectSO;

            if (owner == null || attacker == null || data == null || !result.WasHit || result.AppliedDamage <= 0f || attacker.Health == null || attacker.Health.IsDead)
            {
                return;
            }

            float percent = tuning != null ? tuning.GetValue(PassiveValueKey.DamageReflectPercent) : data.DamageReflectPercent;
            float reflectedDamage = result.AppliedDamage * Mathf.Max(0f, percent) * 0.01f;

            if (reflectedDamage > 0f)
            {
                attacker.ApplyDamage(new DamageInfo(reflectedDamage, result.DamageType, false));
            }
        }
    }
}
