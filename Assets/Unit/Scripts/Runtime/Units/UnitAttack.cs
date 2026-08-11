using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using UnityEngine;
using UnityEngine.Pool;

namespace EndlessGuard.Unit.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UnitRuntimeState))]
    public sealed class UnitAttack : MonoBehaviour
    {
        [Header("Common Hit Rule")]
        [SerializeField] private HitRuleSO hitRule;

        [Header("Common Damage Rule")]
        [SerializeField] private DamageRuleSO damageRule;

        private UnitRuntimeState state;

        public UnitRuntimeState State => state;
        public HitRuleSO HitRule => hitRule;
        public DamageRuleSO DamageRule => damageRule;

        private void Awake()
        {
            state = GetComponent<UnitRuntimeState>();

            if (hitRule == null)
            {
                Debug.LogError($"{name} UnitAttack is missing HitRule.", this);
            }

            if (damageRule == null)
            {
                Debug.LogError($"{name} UnitAttack is missing DamageRule.", this);
            }
        }

        public bool Step(float deltaTime)
        {
            if (!CanStep(deltaTime))
            {
                return false;
            }

            AttackSettings attackSettings = state.DataLink.UnitData.AttackSettings;
            List<EnemyRuntimeState> targets = ListPool<EnemyRuntimeState>.Get();
            List<EnemyRuntimeState> rangeTargets = ListPool<EnemyRuntimeState>.Get();

            try
            {
                UnitTargetFinder.FindTargets(state, attackSettings.TargetCount, rangeTargets);

                bool attackAllBlocked = state.Passives != null &&
                                        state.Passives.AttacksAllBlockedTargets(state) &&
                                        state.Block != null &&
                                        state.Block.Count > 0;

                if (attackAllBlocked)
                {
                    AddBlockedTargets(targets);
                }

                int targetLimit = Mathf.Max(1, attackSettings.TargetCount);
                AddRangeTargets(targets, rangeTargets, targetLimit);

                if (targets.Count <= 0)
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

                return TryAttackTargets(targets);
            }
            finally
            {
                ListPool<EnemyRuntimeState>.Release(rangeTargets);
                ListPool<EnemyRuntimeState>.Release(targets);
            }
        }

        private void AddBlockedTargets(List<EnemyRuntimeState> targets)
        {
            if (targets == null || state.Block == null || state.Block.Count <= 0)
            {
                return;
            }

            IReadOnlyList<EnemyBlock> blockedEnemies = state.Block.Enemies;

            for (int i = 0; i < blockedEnemies.Count; i++)
            {
                EnemyRuntimeState candidate = blockedEnemies[i] != null ? blockedEnemies[i].State : null;

                if (candidate == null || !candidate.IsInitialized || candidate.Health == null || candidate.Health.IsDead || targets.Contains(candidate))
                {
                    continue;
                }

                targets.Add(candidate);
            }
        }

        private static void AddRangeTargets(List<EnemyRuntimeState> targets, List<EnemyRuntimeState> rangeTargets, int targetLimit)
        {
            if (targets == null || rangeTargets == null || targetLimit <= 0)
            {
                return;
            }

            for (int i = 0; i < rangeTargets.Count && targets.Count < targetLimit; i++)
            {
                EnemyRuntimeState candidate = rangeTargets[i];

                if (candidate == null || targets.Contains(candidate))
                {
                    continue;
                }

                targets.Add(candidate);
            }
        }

        private bool TryAttackTargets(List<EnemyRuntimeState> targets)
        {
            bool consumedAttack = false;
            bool gainedSkillGauge = false;
            bool executedAny = false;

            for (int i = 0; i < targets.Count; i++)
            {
                EnemyRuntimeState target = targets[i];

                if (!BasicAttackContextFactory.TryCreate(state, target, out BasicAttackContext context))
                {
                    continue;
                }

                bool succeeded = BasicAttackExecutor.TryExecute(state, target, context, !consumedAttack, false, !gainedSkillGauge, out BasicAttackResult result);

                if (!succeeded)
                {
                    continue;
                }

                consumedAttack = true;
                gainedSkillGauge |= result.SkillGaugeGained > 0f;
                executedAny = true;
            }

            return executedAny;
        }

        private bool CanStep(float deltaTime)
        {
            if (deltaTime <= 0f || state == null || hitRule == null || damageRule == null || !state.IsInitialized || state.Stats == null || !state.Stats.IsInitialized || state.Health == null || state.Health.IsDead || state.DataLink == null || !state.DataLink.HasData)
            {
                return false;
            }

            UnitDataSO unitData = state.DataLink.UnitData;

            if (unitData.AttackSettings == null)
            {
                return false;
            }

            return unitData.AttackSettings.AttackMode != AttackMode.None && state.Stats.AttacksPerSecond > 0f;
        }
    }
}