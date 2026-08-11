using System;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    internal sealed class SizeDamageHandler : IUnitOutgoingDamagePassiveHandler
    {
        public Type DataType => typeof(SizeDamagePassiveSO);

        public float ModifyDamage(UnitRuntimeState owner, EnemyRuntimeState target, PassiveDataSO passive, PassiveTuning tuning, float damage)
        {
            if (owner == null || target == null || damage <= 0f)
            {
                return damage;
            }

            SizeDamagePassiveSO data = passive as SizeDamagePassiveSO;

            if (data == null || data.TargetSize == EnemySize.None)
            {
                return damage;
            }

            if (target.DataLink == null || !target.DataLink.HasData)
            {
                return damage;
            }

            if (target.DataLink.EnemyData.Size != data.TargetSize)
            {
                return damage;
            }

            float bonusPercent = tuning != null
                ? tuning.GetValue(PassiveValueKey.BonusDamagePercent)
                : data.BonusDamagePercent;

            if (float.IsNaN(bonusPercent) || float.IsInfinity(bonusPercent) || bonusPercent <= 0f)
            {
                return damage;
            }

            float multiplier = 1f + bonusPercent * 0.01f;

            return Mathf.Max(0f, damage * multiplier);
        }
    }
}