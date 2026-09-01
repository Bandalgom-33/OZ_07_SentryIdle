using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using UnityEngine;
using UnityEngine.Pool;

namespace EndlessGuard.Unit.Runtime
{
    internal sealed class AllyAidHandler : IUnitBasicAttackResolvedPassiveHandler
    {
        public Type DataType => typeof(AllyAidSO);

        public void OnBasicAttackResolved(UnitRuntimeState owner, EnemyRuntimeState target, PassiveDataSO passive, PassiveTuning tuning, BasicAttackResult result)
        {
            if (owner == null || !result.WasHit || !result.IsCritical)
            {
                return;
            }

            AllyAidSO data = passive as AllyAidSO;

            if (data == null)
            {
                return;
            }

            List<UnitRuntimeState> allies = ListPool<UnitRuntimeState>.Get();

            try
            {
                foreach (UnitRuntimeState unit in CombatRegistry.Units)
                {
                    if (unit != null && unit.IsInitialized && unit.Health != null && !unit.Health.IsDead)
                    {
                        allies.Add(unit);
                    }
                }

                if (allies.Count == 0)
                {
                    return;
                }

                UnitRuntimeState ally = allies[UnityEngine.Random.Range(0, allies.Count)];
                int effectIndex = UnityEngine.Random.Range(0, 3);
                bool effectApplied = false;

                switch (effectIndex)
                {
                    case 0:
                        {
                            float amount = tuning != null ? tuning.GetValue(PassiveValueKey.ShieldAmount) : data.ShieldAmount;
                            float appliedAmount = ally.Health.AddShield(Mathf.Max(0f, amount));

                            if (appliedAmount > 0f)
                            {
                                AidEffect.ShowShield(ally);
                                effectApplied = true;
                            }

                            break;
                        }

                    case 1:
                        {
                            float amount = tuning != null ? tuning.GetValue(PassiveValueKey.HealAmount) : data.HealAmount;
                            float appliedAmount = ally.Heal(Mathf.Max(0f, amount));

                            if (appliedAmount > 0f)
                            {
                                AidEffect.ShowHeal(ally);
                                effectApplied = true;
                            }

                            break;
                        }

                    default:
                        {
                            float amount = tuning != null ? tuning.GetValue(PassiveValueKey.SkillGaugeGain) : data.SkillGaugeGain;
                            float appliedAmount = ally.AddSkillGauge(Mathf.Max(0f, amount));

                            if (appliedAmount > 0f)
                            {
                                AidEffect.ShowSkill(ally);
                                effectApplied = true;
                            }

                            break;
                        }
                }

                if (effectApplied)
                {
                    UnitAnimationCueEvents.NotifyBuff(owner);
                }
            }
            finally
            {
                ListPool<UnitRuntimeState>.Release(allies);
            }
        }
    }
}