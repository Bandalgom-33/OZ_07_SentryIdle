using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    [Serializable]
    public sealed class UnitPassiveRuntime
    {
        [Header("Runtime Passive State")]
        [SerializeField] private int assignedPassiveCount;
        [SerializeField] private int appliedPassiveCount;
        [SerializeField] private int unsupportedPassiveCount;
        [SerializeField] private int rejectedPassiveCount;

        private readonly List<AttackPowerBinding> attackPowerBindings = new List<AttackPowerBinding>(4);
        private readonly List<OutgoingDamageBinding> outgoingDamageBindings = new List<OutgoingDamageBinding>(4);
        private readonly List<TargetLayerBinding> targetLayerBindings = new List<TargetLayerBinding>(2);
        private readonly List<AttackAllBlockedBinding> attackAllBlockedBindings = new List<AttackAllBlockedBinding>(1);
        private readonly List<IPassiveRuntimeBinding> runtimeBindings = new List<IPassiveRuntimeBinding>(4);
        private readonly List<BasicAttackResolvedBinding> basicAttackResolvedBindings = new List<BasicAttackResolvedBinding>(4);
        private readonly List<BasicAttackReceivedBinding> basicAttackReceivedBindings = new List<BasicAttackReceivedBinding>(4);
        private readonly List<BlockStartedBinding> blockStartedBindings = new List<BlockStartedBinding>(2);
        private readonly List<BlockEndedBinding> blockEndedBindings = new List<BlockEndedBinding>(2);
        private readonly List<TimedDamageBonus> timedDamageBonuses = new List<TimedDamageBonus>(2);

        public int AssignedPassiveCount => assignedPassiveCount;
        public int AppliedPassiveCount => appliedPassiveCount;
        public int UnsupportedPassiveCount => unsupportedPassiveCount;
        public int RejectedPassiveCount => rejectedPassiveCount;
        public int ActiveBindingCount => runtimeBindings.Count;

        public void Initialize(UnitRuntimeState owner, IReadOnlyList<PassiveDataSO> passives)
        {
            Deactivate();

            assignedPassiveCount = 0;
            appliedPassiveCount = 0;
            unsupportedPassiveCount = 0;
            rejectedPassiveCount = 0;

            if (owner == null || owner.DataLink == null || !owner.DataLink.HasData || passives == null)
            {
                return;
            }

            UnitDataSO unitData = owner.DataLink.UnitData;

            for (int i = 0; i < passives.Count; i++)
            {
                PassiveDataSO passive = passives[i];

                if (passive == null)
                {
                    continue;
                }

                assignedPassiveCount++;

                if (!passive.CanBeUsedByUnit(unitData.Class, unitData.Subclass))
                {
                    rejectedPassiveCount++;
                    continue;
                }

                if (!PassiveRegistry.TryGet(passive, out IPassiveHandler handler))
                {
                    unsupportedPassiveCount++;
                    continue;
                }

                PassiveTuning tuning = unitData.GetPassiveTuning(passive);
                bool applied = false;

                if (handler is IUnitAttackPowerPassiveHandler attackPowerHandler)
                {
                    attackPowerBindings.Add(new AttackPowerBinding(attackPowerHandler, passive, tuning));
                    applied = true;
                }

                if (handler is IUnitOutgoingDamagePassiveHandler outgoingDamageHandler)
                {
                    outgoingDamageBindings.Add(new OutgoingDamageBinding(outgoingDamageHandler, passive, tuning));
                    applied = true;
                }

                if (handler is IUnitTargetLayerPassiveHandler targetLayerHandler)
                {
                    targetLayerBindings.Add(new TargetLayerBinding(targetLayerHandler, passive, tuning));
                    applied = true;
                }

                if (handler is IUnitAttackAllBlockedPassiveHandler attackAllBlockedHandler)
                {
                    attackAllBlockedBindings.Add(new AttackAllBlockedBinding(attackAllBlockedHandler, passive, tuning));
                    applied = true;
                }

                if (handler is IUnitRuntimePassiveHandler runtimeHandler)
                {
                    IPassiveRuntimeBinding binding = runtimeHandler.CreateBinding(owner, passive, tuning);

                    if (binding != null)
                    {
                        binding.Activate();
                        runtimeBindings.Add(binding);
                        applied = true;
                    }
                }

                if (handler is IUnitBasicAttackResolvedPassiveHandler basicAttackResolvedHandler)
                {
                    basicAttackResolvedBindings.Add(new BasicAttackResolvedBinding(basicAttackResolvedHandler, passive, tuning));
                    applied = true;
                }

                if (handler is IUnitBasicAttackReceivedPassiveHandler basicAttackReceivedHandler)
                {
                    basicAttackReceivedBindings.Add(new BasicAttackReceivedBinding(basicAttackReceivedHandler, passive, tuning));
                    applied = true;
                }

                if (handler is IUnitBlockStartedPassiveHandler blockStartedHandler)
                {
                    blockStartedBindings.Add(new BlockStartedBinding(blockStartedHandler, passive, tuning));
                    applied = true;
                }

                if (handler is IUnitBlockEndedPassiveHandler blockEndedHandler)
                {
                    blockEndedBindings.Add(new BlockEndedBinding(blockEndedHandler, passive, tuning));
                    applied = true;
                }

                if (applied)
                {
                    appliedPassiveCount++;
                }
                else
                {
                    unsupportedPassiveCount++;
                }
            }
        }

        public float ModifyAttackPower(UnitRuntimeState owner, EnemyRuntimeState target, float attackPower)
        {
            if (owner == null || target == null || attackPower <= 0f)
            {
                return attackPower;
            }

            float modifiedAttackPower = attackPower;

            for (int i = 0; i < attackPowerBindings.Count; i++)
            {
                AttackPowerBinding binding = attackPowerBindings[i];
                modifiedAttackPower = binding.Handler.ModifyAttackPower(owner, target, binding.Passive, binding.Tuning, modifiedAttackPower);
            }

            return Mathf.Max(0f, modifiedAttackPower);
        }

        public float ModifyOutgoingDamage(UnitRuntimeState owner, EnemyRuntimeState target, float damage)
        {
            if (owner == null || target == null || damage <= 0f)
            {
                return damage;
            }

            float modifiedDamage = damage;

            for (int i = 0; i < outgoingDamageBindings.Count; i++)
            {
                OutgoingDamageBinding binding = outgoingDamageBindings[i];
                modifiedDamage = binding.Handler.ModifyDamage(owner, target, binding.Passive, binding.Tuning, modifiedDamage);
            }

            for (int i = 0; i < timedDamageBonuses.Count; i++)
            {
                float bonusPercent = Mathf.Max(0f, timedDamageBonuses[i].BonusPercent);
                modifiedDamage *= 1f + bonusPercent * 0.01f;
            }

            return Mathf.Max(0f, modifiedDamage);
        }

        internal bool AllowsTargetLayer(UnitRuntimeState owner, CombatTargetLayer targetLayer)
        {
            if (owner == null)
            {
                return false;
            }

            for (int i = 0; i < targetLayerBindings.Count; i++)
            {
                TargetLayerBinding binding = targetLayerBindings[i];

                if (binding.Handler.AllowsTargetLayer(owner, binding.Passive, binding.Tuning, targetLayer))
                {
                    return true;
                }
            }

            return false;
        }

        internal bool AttacksAllBlockedTargets(UnitRuntimeState owner)
        {
            if (owner == null)
            {
                return false;
            }

            for (int i = 0; i < attackAllBlockedBindings.Count; i++)
            {
                AttackAllBlockedBinding binding = attackAllBlockedBindings[i];

                if (binding.Handler.IsEnabled(owner, binding.Passive, binding.Tuning))
                {
                    return true;
                }
            }

            return false;
        }

        internal void Step(UnitRuntimeState owner, float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            for (int i = 0; i < runtimeBindings.Count; i++)
            {
                if (runtimeBindings[i] is IPassiveTickBinding tickBinding)
                {
                    tickBinding.Step(deltaTime);
                }
            }

            for (int i = timedDamageBonuses.Count - 1; i >= 0; i--)
            {
                TimedDamageBonus bonus = timedDamageBonuses[i];
                bonus.RemainingSeconds -= deltaTime;

                if (bonus.RemainingSeconds <= 0f)
                {
                    timedDamageBonuses.RemoveAt(i);
                }
                else
                {
                    timedDamageBonuses[i] = bonus;
                }
            }
        }

        internal void SetTimedOutgoingDamageBonus(PassiveDataSO passive, float bonusPercent, float durationSeconds)
        {
            if (passive == null || bonusPercent <= 0f || durationSeconds <= 0f)
            {
                return;
            }

            int passiveId = passive.GetInstanceID();

            for (int i = 0; i < timedDamageBonuses.Count; i++)
            {
                TimedDamageBonus bonus = timedDamageBonuses[i];

                if (bonus.PassiveId != passiveId)
                {
                    continue;
                }

                bonus.BonusPercent = bonusPercent;
                bonus.RemainingSeconds = durationSeconds;
                timedDamageBonuses[i] = bonus;
                return;
            }

            timedDamageBonuses.Add(new TimedDamageBonus(passiveId, bonusPercent, durationSeconds));
        }

        internal void NotifyBasicAttackResolved(UnitRuntimeState owner, EnemyRuntimeState target, BasicAttackResult result)
        {
            if (owner == null || target == null)
            {
                return;
            }

            for (int i = 0; i < basicAttackResolvedBindings.Count; i++)
            {
                BasicAttackResolvedBinding binding = basicAttackResolvedBindings[i];
                binding.Handler.OnBasicAttackResolved(owner, target, binding.Passive, binding.Tuning, result);
            }
        }

        internal void NotifyBasicAttackReceived(UnitRuntimeState owner, EnemyRuntimeState attacker, BasicAttackResult result)
        {
            if (owner == null || attacker == null)
            {
                return;
            }

            for (int i = 0; i < basicAttackReceivedBindings.Count; i++)
            {
                BasicAttackReceivedBinding binding = basicAttackReceivedBindings[i];
                binding.Handler.OnBasicAttackReceived(owner, attacker, binding.Passive, binding.Tuning, result);
            }
        }

        internal void NotifyBlockStarted(UnitRuntimeState owner, EnemyRuntimeState enemy)
        {
            if (owner == null || enemy == null)
            {
                return;
            }

            for (int i = 0; i < blockStartedBindings.Count; i++)
            {
                BlockStartedBinding binding = blockStartedBindings[i];
                binding.Handler.OnBlockStarted(owner, enemy, binding.Passive, binding.Tuning);
            }
        }

        internal void NotifyBlockEnded(UnitRuntimeState owner, EnemyRuntimeState enemy)
        {
            if (owner == null || enemy == null)
            {
                return;
            }

            for (int i = 0; i < blockEndedBindings.Count; i++)
            {
                BlockEndedBinding binding = blockEndedBindings[i];
                binding.Handler.OnBlockEnded(owner, enemy, binding.Passive, binding.Tuning);
            }
        }

        internal void Deactivate()
        {
            for (int i = runtimeBindings.Count - 1; i >= 0; i--)
            {
                runtimeBindings[i]?.Deactivate();
            }

            runtimeBindings.Clear();
            attackPowerBindings.Clear();
            outgoingDamageBindings.Clear();
            targetLayerBindings.Clear();
            attackAllBlockedBindings.Clear();
            basicAttackResolvedBindings.Clear();
            basicAttackReceivedBindings.Clear();
            blockStartedBindings.Clear();
            blockEndedBindings.Clear();
            timedDamageBonuses.Clear();
        }

        private readonly struct AttackPowerBinding
        {
            public readonly IUnitAttackPowerPassiveHandler Handler;
            public readonly PassiveDataSO Passive;
            public readonly PassiveTuning Tuning;

            public AttackPowerBinding(IUnitAttackPowerPassiveHandler handler, PassiveDataSO passive, PassiveTuning tuning)
            {
                Handler = handler;
                Passive = passive;
                Tuning = tuning;
            }
        }

        private readonly struct OutgoingDamageBinding
        {
            public readonly IUnitOutgoingDamagePassiveHandler Handler;
            public readonly PassiveDataSO Passive;
            public readonly PassiveTuning Tuning;

            public OutgoingDamageBinding(IUnitOutgoingDamagePassiveHandler handler, PassiveDataSO passive, PassiveTuning tuning)
            {
                Handler = handler;
                Passive = passive;
                Tuning = tuning;
            }
        }

        private readonly struct TargetLayerBinding
        {
            public readonly IUnitTargetLayerPassiveHandler Handler;
            public readonly PassiveDataSO Passive;
            public readonly PassiveTuning Tuning;

            public TargetLayerBinding(IUnitTargetLayerPassiveHandler handler, PassiveDataSO passive, PassiveTuning tuning)
            {
                Handler = handler;
                Passive = passive;
                Tuning = tuning;
            }
        }

        private readonly struct AttackAllBlockedBinding
        {
            public readonly IUnitAttackAllBlockedPassiveHandler Handler;
            public readonly PassiveDataSO Passive;
            public readonly PassiveTuning Tuning;

            public AttackAllBlockedBinding(IUnitAttackAllBlockedPassiveHandler handler, PassiveDataSO passive, PassiveTuning tuning)
            {
                Handler = handler;
                Passive = passive;
                Tuning = tuning;
            }
        }

        private readonly struct BasicAttackResolvedBinding
        {
            public readonly IUnitBasicAttackResolvedPassiveHandler Handler;
            public readonly PassiveDataSO Passive;
            public readonly PassiveTuning Tuning;

            public BasicAttackResolvedBinding(IUnitBasicAttackResolvedPassiveHandler handler, PassiveDataSO passive, PassiveTuning tuning)
            {
                Handler = handler;
                Passive = passive;
                Tuning = tuning;
            }
        }

        private readonly struct BasicAttackReceivedBinding
        {
            public readonly IUnitBasicAttackReceivedPassiveHandler Handler;
            public readonly PassiveDataSO Passive;
            public readonly PassiveTuning Tuning;

            public BasicAttackReceivedBinding(IUnitBasicAttackReceivedPassiveHandler handler, PassiveDataSO passive, PassiveTuning tuning)
            {
                Handler = handler;
                Passive = passive;
                Tuning = tuning;
            }
        }

        private readonly struct BlockStartedBinding
        {
            public readonly IUnitBlockStartedPassiveHandler Handler;
            public readonly PassiveDataSO Passive;
            public readonly PassiveTuning Tuning;

            public BlockStartedBinding(IUnitBlockStartedPassiveHandler handler, PassiveDataSO passive, PassiveTuning tuning)
            {
                Handler = handler;
                Passive = passive;
                Tuning = tuning;
            }
        }

        private readonly struct BlockEndedBinding
        {
            public readonly IUnitBlockEndedPassiveHandler Handler;
            public readonly PassiveDataSO Passive;
            public readonly PassiveTuning Tuning;

            public BlockEndedBinding(IUnitBlockEndedPassiveHandler handler, PassiveDataSO passive, PassiveTuning tuning)
            {
                Handler = handler;
                Passive = passive;
                Tuning = tuning;
            }
        }

        private struct TimedDamageBonus
        {
            public int PassiveId;
            public float BonusPercent;
            public float RemainingSeconds;

            public TimedDamageBonus(int passiveId, float bonusPercent, float remainingSeconds)
            {
                PassiveId = passiveId;
                BonusPercent = bonusPercent;
                RemainingSeconds = remainingSeconds;
            }
        }
    }
}
