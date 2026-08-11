using System;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    internal sealed class SnipeHandler : IUnitOutgoingDamagePassiveHandler
    {
        public Type DataType => typeof(SnipeSO);

        public float ModifyDamage(UnitRuntimeState owner, EnemyRuntimeState target, PassiveDataSO passive, PassiveTuning tuning, float damage)
        {
            SnipeSO data = passive as SnipeSO;

            if (data == null || owner == null || target == null || target.DataLink == null || !target.DataLink.HasData || target.DataLink.EnemyData.Size != data.TargetSize)
            {
                return damage;
            }

            float baseBonus = tuning != null ? tuning.GetValue(PassiveValueKey.BonusDamagePercent) : data.BonusDamagePercent;
            float perDistance = tuning != null ? tuning.GetValue(PassiveValueKey.DamagePerDistancePercent) : data.DamagePerDistancePercent;
            float maxDistanceBonus = tuning != null ? tuning.GetValue(PassiveValueKey.MaxDistanceDamagePercent) : data.MaxDistanceDamagePercent;

            Vector3 offset = target.transform.position - owner.transform.position;
            float horizontalDistance = Mathf.Sqrt(offset.x * offset.x + offset.z * offset.z);
            float distanceBonus = Mathf.Min(Mathf.Max(0f, maxDistanceBonus), horizontalDistance * Mathf.Max(0f, perDistance));
            float totalBonus = Mathf.Max(0f, baseBonus) + distanceBonus;

            return Mathf.Max(0f, damage * (1f + totalBonus * 0.01f));
        }
    }
}
