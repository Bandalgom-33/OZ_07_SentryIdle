using System;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    internal sealed class BlockGaugeHandler : IUnitBlockStartedPassiveHandler
    {
        public Type DataType => typeof(BlockGaugeSO);

        public void OnBlockStarted(UnitRuntimeState owner, EnemyRuntimeState enemy, PassiveDataSO passive, PassiveTuning tuning)
        {
            if (owner == null || owner.Health == null || owner.Health.IsDead)
            {
                return;
            }

            BlockGaugeSO data = passive as BlockGaugeSO;

            if (data == null)
            {
                return;
            }

            float amount = tuning != null ? tuning.GetValue(PassiveValueKey.SkillGaugeGain) : data.SkillGaugeGain;
            owner.AddSkillGauge(Mathf.Max(0f, amount));
        }
    }
}
