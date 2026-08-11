using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    [Serializable]
    public sealed class EnemyPassiveRuntime
    {
        [Header("Runtime Passive State")]
        [SerializeField] private int assignedPassiveCount;
        [SerializeField] private int appliedPassiveCount;
        [SerializeField] private int unsupportedPassiveCount;
        [SerializeField] private int rejectedPassiveCount;
        [SerializeField] private int randomTargetCount;
        [SerializeField] private int burstAttackCount;
        [SerializeField] private float forcedMoveSeconds;
        [SerializeField] private int currentBurstAttackCount;
        [SerializeField] private float forcedMoveRemainingSeconds;

        private readonly List<IPassiveRuntimeBinding> runtimeBindings = new List<IPassiveRuntimeBinding>(4);
        private readonly List<BasicAttackResolvedBinding> basicAttackResolvedBindings = new List<BasicAttackResolvedBinding>(4);
        private readonly List<BasicAttackReceivedBinding> basicAttackReceivedBindings = new List<BasicAttackReceivedBinding>(4);
        private readonly List<BlockedBinding> blockedBindings = new List<BlockedBinding>(2);
        private readonly List<DiedBinding> diedBindings = new List<DiedBinding>(2);

        public int AssignedPassiveCount => assignedPassiveCount;
        public int AppliedPassiveCount => appliedPassiveCount;
        public int UnsupportedPassiveCount => unsupportedPassiveCount;
        public int RejectedPassiveCount => rejectedPassiveCount;
        public int ActiveBindingCount => runtimeBindings.Count;
        public int RandomTargetCount => randomTargetCount;
        public bool PreferFarthestTarget => burstAttackCount > 0;
        public bool IsForcedMovementActive => forcedMoveRemainingSeconds > 0f;

        public void Initialize(EnemyRuntimeState owner, IReadOnlyList<PassiveDataSO> passives)
        {
            Deactivate();

            assignedPassiveCount = 0;
            appliedPassiveCount = 0;
            unsupportedPassiveCount = 0;
            rejectedPassiveCount = 0;
            randomTargetCount = 0;
            burstAttackCount = 0;
            forcedMoveSeconds = 0f;
            currentBurstAttackCount = 0;
            forcedMoveRemainingSeconds = 0f;

            if (owner == null || owner.DataLink == null || !owner.DataLink.HasData || passives == null)
            {
                return;
            }

            EnemyDataSO enemyData = owner.DataLink.EnemyData;

            for (int i = 0; i < passives.Count; i++)
            {
                PassiveDataSO passive = passives[i];

                if (passive == null)
                {
                    continue;
                }

                assignedPassiveCount++;

                if (!passive.CanBeUsedByEnemy(enemyData.Category, enemyData.MovementType, enemyData.Size, enemyData.Role))
                {
                    rejectedPassiveCount++;
                    continue;
                }

                if (!PassiveRegistry.TryGet(passive, out IPassiveHandler handler))
                {
                    unsupportedPassiveCount++;
                    continue;
                }

                PassiveTuning tuning = enemyData.GetPassiveTuning(passive);
                bool applied = false;

                if (handler is IEnemyInitializePassiveHandler initializeHandler)
                {
                    initializeHandler.Apply(owner, passive, tuning);
                    applied = true;
                }

                if (handler is IEnemyRuntimePassiveHandler runtimeHandler)
                {
                    IPassiveRuntimeBinding binding = runtimeHandler.CreateBinding(owner, passive, tuning);

                    if (binding != null)
                    {
                        binding.Activate();
                        runtimeBindings.Add(binding);
                        applied = true;
                    }
                }

                if (handler is IEnemyBasicAttackResolvedPassiveHandler basicAttackResolvedHandler)
                {
                    basicAttackResolvedBindings.Add(new BasicAttackResolvedBinding(basicAttackResolvedHandler, passive, tuning));
                    applied = true;
                }

                if (handler is IEnemyBasicAttackReceivedPassiveHandler basicAttackReceivedHandler)
                {
                    basicAttackReceivedBindings.Add(new BasicAttackReceivedBinding(basicAttackReceivedHandler, passive, tuning));
                    applied = true;
                }

                if (handler is IEnemyBlockedPassiveHandler blockedHandler)
                {
                    blockedBindings.Add(new BlockedBinding(blockedHandler, passive, tuning));
                    applied = true;
                }

                if (handler is IEnemyDiedPassiveHandler diedHandler)
                {
                    diedBindings.Add(new DiedBinding(diedHandler, passive, tuning));
                    applied = true;
                }

                if (handler is IEnemyRandomTargetPassiveHandler randomTargetHandler)
                {
                    randomTargetCount = Mathf.Max(randomTargetCount, randomTargetHandler.GetRandomTargetCount(owner, passive, tuning));
                    applied = true;
                }

                if (handler is IEnemySnipeBurstPassiveHandler snipeBurstHandler)
                {
                    int configuredBurstCount = snipeBurstHandler.GetBurstAttackCount(owner, passive, tuning);

                    if (configuredBurstCount > 0)
                    {
                        burstAttackCount = Mathf.Max(burstAttackCount, configuredBurstCount);
                        forcedMoveSeconds = Mathf.Max(forcedMoveSeconds, snipeBurstHandler.GetForcedMoveSeconds(owner, passive, tuning));
                    }

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

        internal void Step(EnemyRuntimeState owner, float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            if (forcedMoveRemainingSeconds > 0f)
            {
                forcedMoveRemainingSeconds = Mathf.Max(0f, forcedMoveRemainingSeconds - deltaTime);
            }

            for (int i = 0; i < runtimeBindings.Count; i++)
            {
                if (runtimeBindings[i] is IPassiveTickBinding tickBinding)
                {
                    tickBinding.Step(deltaTime);
                }
            }
        }

        internal void NotifyBasicAttackResolved(EnemyRuntimeState owner, UnitRuntimeState target, BasicAttackResult result)
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

            if (burstAttackCount > 0 && result.Succeeded && !IsForcedMovementActive)
            {
                currentBurstAttackCount++;

                if (currentBurstAttackCount >= burstAttackCount)
                {
                    currentBurstAttackCount = 0;
                    forcedMoveRemainingSeconds = Mathf.Max(0f, forcedMoveSeconds);

                    if (owner.Move != null)
                    {
                        owner.Move.SetAttackPaused(false);
                    }
                }
            }
        }

        internal void NotifyBasicAttackReceived(EnemyRuntimeState owner, UnitRuntimeState attacker, BasicAttackResult result)
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

        internal void NotifyBlocked(EnemyRuntimeState owner, UnitRuntimeState blocker)
        {
            if (owner == null || blocker == null)
            {
                return;
            }

            for (int i = 0; i < blockedBindings.Count; i++)
            {
                BlockedBinding binding = blockedBindings[i];
                binding.Handler.OnBlocked(owner, blocker, binding.Passive, binding.Tuning);
            }
        }

        internal void NotifyDied(EnemyRuntimeState owner)
        {
            if (owner == null)
            {
                return;
            }

            for (int i = 0; i < diedBindings.Count; i++)
            {
                DiedBinding binding = diedBindings[i];
                binding.Handler.OnDied(owner, binding.Passive, binding.Tuning);
            }
        }

        internal void Deactivate()
        {
            for (int i = runtimeBindings.Count - 1; i >= 0; i--)
            {
                runtimeBindings[i]?.Deactivate();
            }

            runtimeBindings.Clear();
            basicAttackResolvedBindings.Clear();
            basicAttackReceivedBindings.Clear();
            blockedBindings.Clear();
            diedBindings.Clear();
            randomTargetCount = 0;
            burstAttackCount = 0;
            forcedMoveSeconds = 0f;
            currentBurstAttackCount = 0;
            forcedMoveRemainingSeconds = 0f;
        }

        private readonly struct BasicAttackResolvedBinding
        {
            public readonly IEnemyBasicAttackResolvedPassiveHandler Handler;
            public readonly PassiveDataSO Passive;
            public readonly PassiveTuning Tuning;

            public BasicAttackResolvedBinding(IEnemyBasicAttackResolvedPassiveHandler handler, PassiveDataSO passive, PassiveTuning tuning)
            {
                Handler = handler;
                Passive = passive;
                Tuning = tuning;
            }
        }

        private readonly struct BasicAttackReceivedBinding
        {
            public readonly IEnemyBasicAttackReceivedPassiveHandler Handler;
            public readonly PassiveDataSO Passive;
            public readonly PassiveTuning Tuning;

            public BasicAttackReceivedBinding(IEnemyBasicAttackReceivedPassiveHandler handler, PassiveDataSO passive, PassiveTuning tuning)
            {
                Handler = handler;
                Passive = passive;
                Tuning = tuning;
            }
        }

        private readonly struct BlockedBinding
        {
            public readonly IEnemyBlockedPassiveHandler Handler;
            public readonly PassiveDataSO Passive;
            public readonly PassiveTuning Tuning;

            public BlockedBinding(IEnemyBlockedPassiveHandler handler, PassiveDataSO passive, PassiveTuning tuning)
            {
                Handler = handler;
                Passive = passive;
                Tuning = tuning;
            }
        }

        private readonly struct DiedBinding
        {
            public readonly IEnemyDiedPassiveHandler Handler;
            public readonly PassiveDataSO Passive;
            public readonly PassiveTuning Tuning;

            public DiedBinding(IEnemyDiedPassiveHandler handler, PassiveDataSO passive, PassiveTuning tuning)
            {
                Handler = handler;
                Passive = passive;
                Tuning = tuning;
            }
        }
    }
}
