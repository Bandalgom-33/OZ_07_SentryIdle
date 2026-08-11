using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using UnityEngine;
using UnityEngine.Pool;

namespace EndlessGuard.Unit.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyRuntimeState))]
    public sealed class EnemyAttack : MonoBehaviour
    {
        private enum InRangeCycleState
        {
            Moving = 0,
            Firing = 1,
            Advancing = 2
        }

        [Header("Common Hit Rule")]
        [SerializeField] private HitRuleSO hitRule;

        [Header("Common Damage Rule")]
        [SerializeField] private DamageRuleSO damageRule;

        private EnemyRuntimeState state;
        private InRangeCycleState inRangeCycleState;
        private float inRangeCycleTimer;

        public EnemyRuntimeState State => state;
        public HitRuleSO HitRule => hitRule;
        public DamageRuleSO DamageRule => damageRule;

        private void Awake()
        {
            state = GetComponent<EnemyRuntimeState>();

            if (hitRule == null)
            {
                Debug.LogError($"{name} EnemyAttack is missing HitRule.", this);
            }

            if (damageRule == null)
            {
                Debug.LogError($"{name} EnemyAttack is missing DamageRule.", this);
            }
        }

        private void OnEnable()
        {
            ResetInRangeCycle();
        }

        private void OnDisable()
        {
            ResetInRangeCycle();
            ReleaseAttackPause();
        }

        public bool Step(float deltaTime)
        {
            if (!CanStep(deltaTime))
            {
                ResetInRangeCycle();
                ReleaseAttackPause();
                return false;
            }

            if (state.Passives != null && state.Passives.IsForcedMovementActive)
            {
                ResetInRangeCycle();
                ReleaseAttackPause();
                return false;
            }

            if (state.IsSummon && state.SummonRuntime != null && state.SummonRuntime.IsInitialized && state.SummonRuntime.Chase != null)
            {
                ResetInRangeCycle();
                return StepSummon(deltaTime);
            }

            EnemyDataSO enemyData = state.DataLink.EnemyData;

            if (enemyData.UsesInRangeAttackCycle)
            {
                return StepInRangeAttackCycle(enemyData, deltaTime);
            }

            ResetInRangeCycle();
            return StepStandard(enemyData, deltaTime);
        }

        private bool StepStandard(EnemyDataSO enemyData, float deltaTime)
        {
            bool pauseForAttack = ShouldPauseForAttack(enemyData);

            if (!pauseForAttack)
            {
                ReleaseAttackPause();
            }

            if (!TryGetTriggerTarget(enemyData, out UnitRuntimeState triggerTarget, out BasicAttackContext triggerContext))
            {
                if (pauseForAttack)
                {
                    ReleaseAttackPause();
                }

                return false;
            }

            if (pauseForAttack && state.Move != null)
            {
                state.Move.SetAttackPaused(true);
            }

            if (state.ReadyAttackCount <= 0)
            {
                state.AdvanceAttackProgress(state.Stats.AttacksPerSecond, deltaTime);
            }

            if (state.ReadyAttackCount <= 0)
            {
                return false;
            }

            int randomTargetCount = state.Passives != null ? state.Passives.RandomTargetCount : 0;
            bool succeeded = randomTargetCount > 0 ? TryExecuteRandomTargets(randomTargetCount) : BasicAttackExecutor.TryExecute(state, triggerTarget, triggerContext, out _);

            if (!succeeded && pauseForAttack)
            {
                ReleaseAttackPause();
            }

            return succeeded;
        }

        private bool StepInRangeAttackCycle(EnemyDataSO enemyData, float deltaTime)
        {
            if (inRangeCycleState == InRangeCycleState.Advancing)
            {
                ReleaseAttackPause();
                inRangeCycleTimer = Mathf.Max(0f, inRangeCycleTimer - deltaTime);

                if (inRangeCycleTimer > 0f)
                {
                    return false;
                }

                inRangeCycleState = InRangeCycleState.Moving;
            }

            if (inRangeCycleState == InRangeCycleState.Moving)
            {
                ReleaseAttackPause();

                if (!TryGetTriggerTarget(enemyData, out UnitRuntimeState movingTarget, out BasicAttackContext movingContext))
                {
                    return false;
                }

                BeginFiring(enemyData);
                bool startedAttack = TryExecuteAttack(movingTarget, movingContext, deltaTime);
                StepFiringTimer(enemyData, deltaTime);
                return startedAttack;
            }

            state.Move.SetAttackPaused(true);

            if (!TryGetTriggerTarget(enemyData, out UnitRuntimeState firingTarget, out BasicAttackContext firingContext))
            {
                BeginAdvancing(enemyData);
                return false;
            }

            bool succeeded = TryExecuteAttack(firingTarget, firingContext, deltaTime);
            StepFiringTimer(enemyData, deltaTime);
            return succeeded;
        }

        private void StepFiringTimer(EnemyDataSO enemyData, float deltaTime)
        {
            if (inRangeCycleState != InRangeCycleState.Firing)
            {
                return;
            }

            inRangeCycleTimer = Mathf.Max(0f, inRangeCycleTimer - deltaTime);

            if (inRangeCycleTimer <= 0f)
            {
                BeginAdvancing(enemyData);
            }
        }

        private void BeginFiring(EnemyDataSO enemyData)
        {
            inRangeCycleState = InRangeCycleState.Firing;
            inRangeCycleTimer = Mathf.Max(0f, enemyData.InRangeFireDuration);
            state.Move.SetAttackPaused(true);
        }

        private void BeginAdvancing(EnemyDataSO enemyData)
        {
            inRangeCycleState = InRangeCycleState.Advancing;
            inRangeCycleTimer = Mathf.Max(0f, enemyData.InRangeAdvanceDuration);
            ReleaseAttackPause();
        }

        private void ResetInRangeCycle()
        {
            inRangeCycleState = InRangeCycleState.Moving;
            inRangeCycleTimer = 0f;
        }

        private bool TryGetTriggerTarget(EnemyDataSO enemyData, out UnitRuntimeState triggerTarget, out BasicAttackContext triggerContext)
        {
            triggerTarget = null;
            triggerContext = default;

            if (!EnemyTargetFinder.TryFind(state, out triggerTarget))
            {
                return false;
            }

            if (!BasicAttackContextFactory.TryCreate(state, triggerTarget, out triggerContext))
            {
                return false;
            }

            return BasicAttackRangeEvaluator.TryEvaluate(enemyData.AttackSettings, triggerContext, out _, out _);
        }

        private bool TryExecuteAttack(UnitRuntimeState triggerTarget, BasicAttackContext triggerContext, float deltaTime)
        {
            if (state.ReadyAttackCount <= 0)
            {
                state.AdvanceAttackProgress(state.Stats.AttacksPerSecond, deltaTime);
            }

            if (state.ReadyAttackCount <= 0)
            {
                return false;
            }

            int randomTargetCount = state.Passives != null ? state.Passives.RandomTargetCount : 0;
            return randomTargetCount > 0 ? TryExecuteRandomTargets(randomTargetCount) : BasicAttackExecutor.TryExecute(state, triggerTarget, triggerContext, out _);
        }

        private bool StepSummon(float deltaTime)
        {
            ReleaseAttackPause();

            EnemySummonChase chase = state.SummonRuntime.Chase;

            if (!chase.TryGetAttackTarget(out UnitRuntimeState target, out BasicAttackContext context))
            {
                return false;
            }

            if (state.ReadyAttackCount <= 0)
            {
                state.AdvanceAttackProgress(state.Stats.AttacksPerSecond, deltaTime);
            }

            if (state.ReadyAttackCount <= 0)
            {
                return false;
            }

            return BasicAttackExecutor.TryExecute(state, target, context, true, true, out _);
        }

        private bool TryExecuteRandomTargets(int targetCount)
        {
            List<UnitRuntimeState> targets = ListPool<UnitRuntimeState>.Get();

            try
            {
                foreach (UnitRuntimeState unit in CombatRegistry.Units)
                {
                    if (IsValidRandomTarget(unit))
                    {
                        targets.Add(unit);
                    }
                }

                if (targets.Count == 0)
                {
                    return false;
                }

                Shuffle(targets);
                int attackCount = Mathf.Min(Mathf.Max(1, targetCount), targets.Count);
                bool consumedAttack = false;
                bool executedAny = false;

                for (int i = 0; i < attackCount; i++)
                {
                    UnitRuntimeState target = targets[i];

                    if (!BasicAttackContextFactory.TryCreate(state, target, out BasicAttackContext context))
                    {
                        continue;
                    }

                    bool succeeded = BasicAttackExecutor.TryExecute(state, target, context, !consumedAttack, true, out _);

                    if (!succeeded)
                    {
                        continue;
                    }

                    consumedAttack = true;
                    executedAny = true;
                }

                return executedAny;
            }
            finally
            {
                ListPool<UnitRuntimeState>.Release(targets);
            }
        }

        private static void Shuffle(List<UnitRuntimeState> targets)
        {
            for (int i = targets.Count - 1; i > 0; i--)
            {
                int swapIndex = Random.Range(0, i + 1);
                UnitRuntimeState temp = targets[i];
                targets[i] = targets[swapIndex];
                targets[swapIndex] = temp;
            }
        }

        private static bool IsValidRandomTarget(UnitRuntimeState target)
        {
            return target != null && target.IsInitialized && target.Health != null && !target.Health.IsDead && target.GridPosition != null && target.GridPosition.IsInitialized && target.DataLink != null && target.DataLink.HasData;
        }

        private bool CanStep(float deltaTime)
        {
            if (deltaTime <= 0f || state == null || hitRule == null || damageRule == null || !state.IsInitialized || state.Stats == null || !state.Stats.IsInitialized || state.Health == null || state.Health.IsDead || state.DataLink == null || !state.DataLink.HasData || state.Move == null || state.Move.HasReachedGoal)
            {
                return false;
            }

            EnemyDataSO enemyData = state.DataLink.EnemyData;

            if (enemyData.AttackSettings == null)
            {
                return false;
            }

            return enemyData.AttackSettings.AttackMode != AttackMode.None && state.Stats.AttacksPerSecond > 0f;
        }

        private static bool ShouldPauseForAttack(EnemyDataSO enemyData)
        {
            return enemyData.AttackRule == EnemyAttackRule.InRange && enemyData.MovementType == EnemyMovementType.Ground;
        }

        private void ReleaseAttackPause()
        {
            if (state != null && state.Move != null)
            {
                state.Move.SetAttackPaused(false);
            }
        }
    }
}