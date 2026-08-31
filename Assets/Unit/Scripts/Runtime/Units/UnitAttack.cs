using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using UnityEngine;

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

        private readonly List<EnemyRuntimeState> attackTargets = new List<EnemyRuntimeState>(8);
        private readonly List<EnemyRuntimeState> rangeTargets = new List<EnemyRuntimeState>(8);
        private readonly UnitTargetFinder.SearchBuffer targetSearchBuffer = new UnitTargetFinder.SearchBuffer();
        private UnitRuntimeState state;
        private float basicAttackRepeatMultiplier = 1f;
        private float basicAttackRepeatCarry;
        private bool hasCombatTarget;
        private AttackImpactVfxTemplate attackImpactTemplate;
        private AttackHitSoundTemplate attackHitSoundTemplate;

        public UnitRuntimeState State => state;
        public HitRuleSO HitRule => hitRule;
        public DamageRuleSO DamageRule => damageRule;
        public float BasicAttackRepeatMultiplier => basicAttackRepeatMultiplier;
        public bool HasCombatTarget => hasCombatTarget;
        public AttackImpactVfxTemplate AttackImpactTemplate => attackImpactTemplate;
        public AttackHitSoundTemplate AttackHitSoundTemplate => attackHitSoundTemplate;

        public event Action<UnitAttack> OnAttackExecuted;

        private void Awake()
        {
            state = GetComponent<UnitRuntimeState>();
            CombatEntityAnchors anchors = GetComponent<CombatEntityAnchors>();
            if (anchors != null && anchors.AttackPoint != null)
            {
                attackImpactTemplate = anchors.AttackPoint.GetComponentInChildren<AttackImpactVfxTemplate>(true);
                attackHitSoundTemplate = anchors.AttackPoint.GetComponentInChildren<AttackHitSoundTemplate>(true);

                if (attackImpactTemplate != null)
                {
                    attackImpactTemplate.gameObject.SetActive(false);
                }

                if (attackHitSoundTemplate != null)
                {
                    attackHitSoundTemplate.gameObject.SetActive(false);
                }
            }

            if (hitRule == null)
            {
                Debug.LogError($"{name} UnitAttack is missing HitRule.", this);
            }

            if (damageRule == null)
            {
                Debug.LogError($"{name} UnitAttack is missing DamageRule.", this);
            }
        }

        private void OnDisable()
        {
            hasCombatTarget = false;
            attackTargets.Clear();
            rangeTargets.Clear();
            ResetBasicAttackRepeatMultiplier();
        }

        public void SetBasicAttackRepeatMultiplier(float multiplier)
        {
            float sanitized = Mathf.Max(1f, multiplier);
            if (Mathf.Approximately(basicAttackRepeatMultiplier, sanitized))
            {
                return;
            }

            basicAttackRepeatMultiplier = sanitized;
            basicAttackRepeatCarry = 0f;
        }

        public void ResetBasicAttackRepeatMultiplier()
        {
            basicAttackRepeatMultiplier = 1f;
            basicAttackRepeatCarry = 0f;
        }

        public bool Step(float deltaTime)
        {
            if (!CanStep(deltaTime))
            {
                hasCombatTarget = false;
                return false;
            }

            AttackSettings attackSettings = state.DataLink.UnitData.AttackSettings;
            attackTargets.Clear();
            rangeTargets.Clear();
            UnitTargetFinder.FindTargets(state, attackSettings.TargetCount, rangeTargets, targetSearchBuffer);

            bool attackAllBlocked = state.Passives != null && state.Passives.AttacksAllBlockedTargets(state) && state.Block != null && state.Block.Count > 0;
            if (attackAllBlocked)
            {
                AddBlockedTargets(attackTargets);
            }

            int targetLimit = Mathf.Max(0, attackSettings.TargetCount);
            AddRangeTargets(attackTargets, rangeTargets, targetLimit);

            hasCombatTarget = attackTargets.Count > 0;
            if (!hasCombatTarget)
            {
                return false;
            }

            if (state.ReadyAttackCount <= 0)
            {
                state.AdvanceAttackProgress(state.Stats.AttacksPerSecond, deltaTime);
            }

            if (state.ReadyAttackCount <= 0 || !TryAttackTargets(attackTargets, true))
            {
                return false;
            }

            int repeatCount = ResolveBasicAttackRepeatCount();
            for (int repeatIndex = 1; repeatIndex < repeatCount; repeatIndex++)
            {
                if (!TryAttackTargets(attackTargets, false))
                {
                    break;
                }
            }

            OnAttackExecuted?.Invoke(this);
            return true;
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

        private bool TryAttackTargets(List<EnemyRuntimeState> targets, bool consumeReadyAttack)
        {
            bool consumedAttack = !consumeReadyAttack;
            bool gainedSkillGauge = false;
            bool executedAny = false;

            for (int i = 0; i < targets.Count; i++)
            {
                EnemyRuntimeState target = targets[i];
                if (!BasicAttackContextFactory.TryCreate(state, target, out BasicAttackContext context))
                {
                    continue;
                }

                bool succeeded = BasicAttackExecutor.TryExecute(state, target, context, consumeReadyAttack && !consumedAttack, false, !gainedSkillGauge, out BasicAttackResult result);
                if (!succeeded)
                {
                    continue;
                }

                if (consumeReadyAttack)
                {
                    consumedAttack = true;
                }

                gainedSkillGauge |= result.SkillGaugeGained > 0f;
                executedAny = true;
            }

            return executedAny;
        }

        private int ResolveBasicAttackRepeatCount()
        {
            float total = basicAttackRepeatMultiplier + basicAttackRepeatCarry;
            int repeatCount = Mathf.Max(1, Mathf.FloorToInt(total));
            basicAttackRepeatCarry = Mathf.Clamp(total - repeatCount, 0f, 0.999999f);
            return repeatCount;
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

            return unitData.AttackSettings.AttackMode != AttackMode.None && unitData.AttackSettings.TargetCount > 0 && state.Stats.AttacksPerSecond > 0f;
        }
    }
}
