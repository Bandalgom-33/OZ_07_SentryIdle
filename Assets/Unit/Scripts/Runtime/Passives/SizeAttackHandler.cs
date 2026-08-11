using System;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    internal sealed class SizeAttackHandler : IUnitAttackPowerPassiveHandler
    {
        public Type DataType => typeof(SizeAttackSO);

        public float ModifyAttackPower(UnitRuntimeState owner, EnemyRuntimeState target, PassiveDataSO passive, PassiveTuning tuning, float attackPower)
        {
            SizeAttackSO data = passive as SizeAttackSO;

            if (data == null || target == null || target.DataLink == null || !target.DataLink.HasData || target.DataLink.EnemyData.Size != data.TargetSize)
            {
                return attackPower;
            }

            float bonusPercent = tuning != null ? tuning.GetValue(PassiveValueKey.AttackBonusPercent) : data.AttackBonusPercent;
            return Mathf.Max(0f, attackPower * (1f + Mathf.Max(0f, bonusPercent) * 0.01f));
        }
    }
}
