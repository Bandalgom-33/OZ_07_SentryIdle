using System;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    internal sealed class HeavyArmorHandler : IEnemyRuntimePassiveHandler
    {
        public Type DataType => typeof(HeavyArmorSO);

        public IPassiveRuntimeBinding CreateBinding(EnemyRuntimeState owner, PassiveDataSO passive, PassiveTuning tuning)
        {
            if (owner == null || owner.Stats == null || !owner.Stats.IsInitialized)
            {
                return null;
            }

            HeavyArmorSO data = passive as HeavyArmorSO;

            if (data == null)
            {
                return null;
            }

            float physicalDefenseBonusPercent = tuning != null ? tuning.GetValue(PassiveValueKey.PhysicalDefenseBonusPercent) : data.PhysicalDefenseBonusPercent;
            float magicalDefenseBonusPercent = tuning != null ? tuning.GetValue(PassiveValueKey.MagicalDefenseBonusPercent) : data.MagicalDefenseBonusPercent;
            float moveSpeedReductionPercent = tuning != null ? tuning.GetValue(PassiveValueKey.MoveSpeedReductionPercent) : data.MoveSpeedReductionPercent;

            physicalDefenseBonusPercent = Mathf.Max(0f, physicalDefenseBonusPercent);
            magicalDefenseBonusPercent = Mathf.Max(0f, magicalDefenseBonusPercent);
            moveSpeedReductionPercent = Mathf.Clamp(moveSpeedReductionPercent, 0f, 100f);

            return new Binding(owner.Stats, physicalDefenseBonusPercent, magicalDefenseBonusPercent, moveSpeedReductionPercent);
        }

        private sealed class Binding : IPassiveRuntimeBinding
        {
            private readonly RuntimeStats stats;
            private readonly float physicalDefenseBonusPercent;
            private readonly float magicalDefenseBonusPercent;
            private readonly float moveSpeedReductionPercent;

            private int physicalDefenseModifierId;
            private int magicalDefenseModifierId;
            private int moveSpeedModifierId;

            public Binding(RuntimeStats stats, float physicalDefenseBonusPercent, float magicalDefenseBonusPercent, float moveSpeedReductionPercent)
            {
                this.stats = stats;
                this.physicalDefenseBonusPercent = physicalDefenseBonusPercent;
                this.magicalDefenseBonusPercent = magicalDefenseBonusPercent;
                this.moveSpeedReductionPercent = moveSpeedReductionPercent;
            }

            public void Activate()
            {
                if (stats == null || !stats.IsInitialized)
                {
                    return;
                }

                if (physicalDefenseModifierId == 0 && physicalDefenseBonusPercent > 0f)
                {
                    physicalDefenseModifierId = stats.AddModifier(PassiveStatType.PhysicalDefense, 0f, physicalDefenseBonusPercent);
                }

                if (magicalDefenseModifierId == 0 && magicalDefenseBonusPercent > 0f)
                {
                    magicalDefenseModifierId = stats.AddModifier(PassiveStatType.MagicalDefense, 0f, magicalDefenseBonusPercent);
                }

                if (moveSpeedModifierId == 0 && moveSpeedReductionPercent > 0f)
                {
                    moveSpeedModifierId = stats.AddModifier(PassiveStatType.MoveSpeed, 0f, -moveSpeedReductionPercent);
                }
            }

            public void Deactivate()
            {
                if (stats == null)
                {
                    return;
                }

                if (physicalDefenseModifierId != 0)
                {
                    stats.RemoveModifier(physicalDefenseModifierId);
                    physicalDefenseModifierId = 0;
                }

                if (magicalDefenseModifierId != 0)
                {
                    stats.RemoveModifier(magicalDefenseModifierId);
                    magicalDefenseModifierId = 0;
                }

                if (moveSpeedModifierId != 0)
                {
                    stats.RemoveModifier(moveSpeedModifierId);
                    moveSpeedModifierId = 0;
                }
            }
        }
    }
}