using System;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    internal sealed class FrontlineCommandHandler : IUnitRuntimePassiveHandler
    {
        public Type DataType => typeof(FrontlineCommandSO);

        public IPassiveRuntimeBinding CreateBinding(UnitRuntimeState owner, PassiveDataSO passive, PassiveTuning tuning)
        {
            if (owner == null || owner.Stats == null || !owner.Stats.IsInitialized)
            {
                return null;
            }

            FrontlineCommandSO data = passive as FrontlineCommandSO;

            if (data == null)
            {
                return null;
            }

            float bonusPercent = tuning != null ? tuning.GetValue(PassiveValueKey.AttackSpeedBonusPercent) : data.AttackSpeedBonusPercent;
            bonusPercent = Mathf.Max(0f, bonusPercent);

            return new Binding(owner, bonusPercent);
        }

        private sealed class Binding : IPassiveRuntimeBinding
        {
            private readonly UnitRuntimeState owner;
            private readonly RuntimeStats stats;
            private readonly float bonusPercent;

            private int modifierId;
            private bool active;

            public Binding(UnitRuntimeState owner, float bonusPercent)
            {
                this.owner = owner;
                stats = owner != null ? owner.Stats : null;
                this.bonusPercent = bonusPercent;
            }

            public void Activate()
            {
                if (active)
                {
                    return;
                }

                active = true;
                PassiveRuntimeEvents.OnUnitSummonCreated += HandleUnitSummonCreated;
                PassiveRuntimeEvents.OnUnitSummonDestroyed += HandleUnitSummonDestroyed;
                Refresh();
            }

            public void Deactivate()
            {
                if (!active)
                {
                    return;
                }

                active = false;
                PassiveRuntimeEvents.OnUnitSummonCreated -= HandleUnitSummonCreated;
                PassiveRuntimeEvents.OnUnitSummonDestroyed -= HandleUnitSummonDestroyed;
                RemoveModifier();
            }

            private void HandleUnitSummonCreated(UnitRuntimeState source, GameObject summon)
            {
                Refresh();
            }

            private void HandleUnitSummonDestroyed(UnitRuntimeState source, GameObject summon)
            {
                Refresh();
            }

            private void Refresh()
            {
                if (!active || owner == null || owner.Health == null || owner.Health.IsDead || stats == null || !stats.IsInitialized)
                {
                    RemoveModifier();
                    return;
                }

                if (PassiveRuntimeEvents.ActiveUnitSummonCount > 0 && bonusPercent > 0f)
                {
                    if (modifierId == 0)
                    {
                        modifierId = stats.AddModifier(PassiveStatType.AttacksPerSecond, 0f, bonusPercent);
                    }

                    return;
                }

                RemoveModifier();
            }

            private void RemoveModifier()
            {
                if (stats == null || modifierId == 0)
                {
                    return;
                }

                stats.RemoveModifier(modifierId);
                modifierId = 0;
            }
        }
    }
}