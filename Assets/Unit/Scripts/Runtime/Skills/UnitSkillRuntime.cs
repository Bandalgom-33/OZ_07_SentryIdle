using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    [Serializable]
    public sealed class UnitSkillRuntime
    {
        [Header("런타임 SP 스킬 상태")]
        [SerializeField] private bool isCasting;
        [SerializeField] private int currentHitIndex;
        [SerializeField] private int totalHitCount;
        [SerializeField] private float nextHitRemainingSeconds;
        [SerializeField] private float castLockRemainingSeconds;

        private readonly List<EnemyRuntimeState> candidates = new List<EnemyRuntimeState>(32);
        private readonly List<EnemyRuntimeState> targets = new List<EnemyRuntimeState>(32);

        private UnitRuntimeState owner;
        private UnitSkillSettings activeSettings;
        private EnemyRuntimeState primaryTarget;
        private bool vfxPlayedForCast;

        public bool IsCasting => isCasting;
        public bool BlocksBasicAttack => isCasting || castLockRemainingSeconds > 0f;
        public int CurrentHitIndex => currentHitIndex;
        public int TotalHitCount => totalHitCount;

        public void Initialize(UnitRuntimeState runtimeOwner)
        {
            owner = runtimeOwner;
            ResetCast();
        }

        public void Deactivate()
        {
            ResetCast();
            owner = null;
        }

        /// <summary>
        /// 반환값은 이 프레임에 기본 공격을 막아야 하는지를 의미합니다.
        /// </summary>
        internal bool Step(float deltaTime)
        {
            if (!CanRun(deltaTime))
            {
                ResetCast();
                return false;
            }

            if (castLockRemainingSeconds > 0f)
            {
                castLockRemainingSeconds = Mathf.Max(0f, castLockRemainingSeconds - deltaTime);
            }

            if (isCasting)
            {
                StepActiveCast(deltaTime);
                return BlocksBasicAttack;
            }

            if (castLockRemainingSeconds > 0f)
            {
                return true;
            }

            UnitSkillSettings settings = owner.DataLink.UnitData.SkillSettings;
            if (settings == null || !settings.Enabled || !settings.AutoCastWhenReady)
            {
                return false;
            }

            float gaugeCost = settings.ResolveGaugeCost(owner.MaxSkillGauge);
            if (gaugeCost <= 0f || owner.CurrentSkillGauge + 0.0001f < gaugeCost)
            {
                return false;
            }

            if (!TryAcquireTargets(settings))
            {
                // 대상이 없을 때는 SP를 소비하지 않고 준비 상태를 유지합니다.
                return false;
            }

            if (!owner.TryConsumeSkillGauge(gaugeCost))
            {
                return false;
            }

            BeginCast(settings);
            return true;
        }

        private bool CanRun(float deltaTime)
        {
            return deltaTime > 0f &&
                   owner != null &&
                   owner.IsInitialized &&
                   owner.Health != null &&
                   !owner.Health.IsDead &&
                   owner.DataLink != null &&
                   owner.DataLink.HasData &&
                   owner.Stats != null &&
                   owner.Stats.IsInitialized;
        }

        private void BeginCast(UnitSkillSettings settings)
        {
            activeSettings = settings;
            totalHitCount = Mathf.Max(1, settings.HitCount);
            currentHitIndex = 0;
            nextHitRemainingSeconds = 0f;
            castLockRemainingSeconds = Mathf.Max(settings.CastLockSeconds, settings.HitIntervalSeconds * Mathf.Max(0, totalHitCount - 1));
            vfxPlayedForCast = false;
            isCasting = true;

            UnitAnimationCueEvents.NotifySkill(owner);
            PassiveRuntimeEvents.NotifyUnitSkillSucceeded(owner);
            ExecuteCurrentHit();
        }

        private void StepActiveCast(float deltaTime)
        {
            if (!isCasting || activeSettings == null)
            {
                return;
            }

            if (currentHitIndex >= totalHitCount)
            {
                isCasting = false;
                return;
            }

            nextHitRemainingSeconds -= deltaTime;
            int safety = 0;

            while (isCasting && nextHitRemainingSeconds <= 0f && safety++ < 16)
            {
                ExecuteCurrentHit();
            }
        }

        private void ExecuteCurrentHit()
        {
            if (!isCasting || activeSettings == null || currentHitIndex >= totalHitCount)
            {
                isCasting = false;
                return;
            }

            int hitIndex = currentHitIndex;
            bool allowVfx = activeSettings.PlayVfxEveryHit || !vfxPlayedForCast;

            for (int i = 0; i < targets.Count; i++)
            {
                EnemyRuntimeState target = targets[i];
                if (!IsValidTarget(target, activeSettings.AttackTarget))
                {
                    continue;
                }

                float appliedDamage = ApplyDamage(target, activeSettings);
                if (appliedDamage > 0f && allowVfx && activeSettings.VfxSpawnMode == UnitSkillVfxSpawnMode.EachTarget)
                {
                    PlayVfxAtTarget(target, activeSettings);
                }
            }

            if (allowVfx && activeSettings.VfxSpawnMode == UnitSkillVfxSpawnMode.PrimaryTarget)
            {
                EnemyRuntimeState visualTarget = ResolveVisualPrimaryTarget();
                if (visualTarget != null)
                {
                    PlayVfxAtTarget(visualTarget, activeSettings);
                }
            }
            else if (allowVfx && activeSettings.VfxSpawnMode == UnitSkillVfxSpawnMode.Caster)
            {
                UnitSkillVfxPool.Play(activeSettings.VfxPrefab, owner.EffectPosition + activeSettings.VfxOffset, activeSettings.VfxScale);
            }

            if (allowVfx)
            {
                vfxPlayedForCast = true;
            }

            currentHitIndex = hitIndex + 1;

            if (currentHitIndex >= totalHitCount)
            {
                isCasting = false;
                nextHitRemainingSeconds = 0f;
                return;
            }

            nextHitRemainingSeconds += Mathf.Max(0.01f, activeSettings.HitIntervalSeconds);
        }

        private float ApplyDamage(EnemyRuntimeState target, UnitSkillSettings settings)
        {
            float attackPower = ResolveAttackPower(settings);
            float percent = settings.ResolvePerHitPowerPercent();
            float flat = settings.ResolvePerHitFlatDamage();
            float scaledPower = attackPower * percent * 0.01f + flat;

            if (settings.ApplyPassiveDamageModifiers && owner.Passives != null)
            {
                scaledPower = owner.Passives.ModifyAttackPower(owner, target, scaledPower);
            }

            float calculatedDamage;
            if (settings.ApplyDefense)
            {
                float defense = ResolveDefense(target, settings.DamageType);
                DamageRuleSO damageRule = owner.Attack != null ? owner.Attack.DamageRule : null;
                calculatedDamage = DamageCalculator.Calculate(scaledPower, defense, damageRule);
            }
            else
            {
                calculatedDamage = Mathf.Max(0f, scaledPower);
            }

            bool isCritical = settings.CanCritical && RollCritical(owner.Stats.CriticalChancePercent);
            if (isCritical)
            {
                calculatedDamage *= 1f + Mathf.Max(0f, owner.Stats.CriticalDamageBonusPercent) * 0.01f;
            }

            if (settings.ApplyPassiveDamageModifiers && owner.Passives != null)
            {
                calculatedDamage = owner.Passives.ModifyOutgoingDamage(owner, target, calculatedDamage);
            }

            if (calculatedDamage <= 0f)
            {
                return 0f;
            }

            DamageInfo damageInfo = new DamageInfo(calculatedDamage, settings.DamageType, isCritical);
            return target.ApplyDamage(owner, damageInfo);
        }

        private float ResolveAttackPower(UnitSkillSettings settings)
        {
            switch (settings.AttackPowerSource)
            {
                case UnitSkillAttackPowerSource.MagicalAttack:
                    return Mathf.Max(0f, owner.Stats.MagicalAttack);

                case UnitSkillAttackPowerSource.HigherAttack:
                    return Mathf.Max(owner.Stats.PhysicalAttack, owner.Stats.MagicalAttack);

                case UnitSkillAttackPowerSource.FixedOnly:
                    return 0f;

                default:
                    return Mathf.Max(0f, owner.Stats.PhysicalAttack);
            }
        }

        private static float ResolveDefense(EnemyRuntimeState target, DamageType damageType)
        {
            if (target == null || target.Stats == null)
            {
                return 0f;
            }

            switch (damageType)
            {
                case DamageType.Magical:
                    return Mathf.Max(0f, target.Stats.MagicalDefense);

                case DamageType.Physical:
                    return Mathf.Max(0f, target.Stats.PhysicalDefense);

                default:
                    return 0f;
            }
        }

        private bool TryAcquireTargets(UnitSkillSettings settings)
        {
            candidates.Clear();
            targets.Clear();
            primaryTarget = null;

            bool ignoresCastRange = settings.TargetScope == UnitSkillTargetScope.MapWide;

            foreach (EnemyRuntimeState enemy in CombatRegistry.Enemies)
            {
                if (!IsValidTarget(enemy, settings.AttackTarget))
                {
                    continue;
                }

                // 단일/범위 스킬은 기본 공격 타일 범위를 스킬 사거리로 사용합니다.
                if (!ignoresCastRange && !IsWithinBasicAttackTileRange(enemy))
                {
                    continue;
                }

                candidates.Add(enemy);
            }

            if (candidates.Count == 0)
            {
                return false;
            }

            if (settings.TargetPriority == UnitSkillTargetPriority.Random)
            {
                int randomIndex = UnityEngine.Random.Range(0, candidates.Count);
                primaryTarget = candidates[randomIndex];
            }
            else
            {
                candidates.Sort((a, b) => CompareTargets(a, b, settings.TargetPriority));
                primaryTarget = candidates[0];
            }

            switch (settings.TargetScope)
            {
                case UnitSkillTargetScope.MapWide:
                    targets.AddRange(candidates);
                    break;

                case UnitSkillTargetScope.Area:
                    AddAreaTargets(primaryTarget, settings);
                    break;

                default:
                    targets.Add(primaryTarget);
                    break;
            }

            return targets.Count > 0;
        }

        private void AddAreaTargets(EnemyRuntimeState center, UnitSkillSettings settings)
        {
            if (center == null)
            {
                return;
            }

            SkillAreaTileData areaTileRange = settings.AreaTileRange;
            int limit = settings.AreaTargetLimit;

            // 대표 대상의 중심 타일 (0,0)은 범위 패턴과 관계없이 항상 적중합니다.
            if (IsValidTarget(center, settings.AttackTarget))
            {
                targets.Add(center);
            }

            if (limit > 0 && targets.Count >= limit)
            {
                return;
            }

            if (areaTileRange == null)
            {
                return;
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                EnemyRuntimeState candidate = candidates[i];
                if (candidate == center || !IsWithinAreaTilePattern(center, candidate, areaTileRange))
                {
                    continue;
                }

                targets.Add(candidate);
                if (limit > 0 && targets.Count >= limit)
                {
                    break;
                }
            }
        }

        private static bool IsWithinAreaTilePattern(EnemyRuntimeState center, EnemyRuntimeState candidate, SkillAreaTileData areaTileRange)
        {
            if (center == null || candidate == null || areaTileRange == null)
            {
                return false;
            }

            CombatGridPosition centerGrid = center.GridPosition;
            CombatGridPosition candidateGrid = candidate.GridPosition;

            if (centerGrid == null || candidateGrid == null || !centerGrid.IsInitialized || !candidateGrid.IsInitialized)
            {
                return false;
            }

            Vector2Int relativeTile = candidateGrid.TileCoordinate - centerGrid.TileCoordinate;
            return areaTileRange.Contains(relativeTile);
        }

        private bool IsWithinBasicAttackTileRange(EnemyRuntimeState enemy)
        {
            if (owner == null || enemy == null || owner.DataLink == null || !owner.DataLink.HasData)
            {
                return false;
            }

            AttackSettings attackSettings = owner.DataLink.UnitData.AttackSettings;
            if (attackSettings == null || attackSettings.BasicAttackRange == null)
            {
                return false;
            }

            if (!BasicAttackContextFactory.TryCreate(owner, enemy, out BasicAttackContext context))
            {
                return false;
            }

            // 대상 층 판정은 SP 스킬 설정을 사용하고, 타일 모양/거리/방향만 기본 공격 범위를 재사용합니다.
            return BasicAttackRangeEvaluator.TryEvaluate(attackSettings, context, true, out _, out _);
        }

        private int CompareTargets(EnemyRuntimeState a, EnemyRuntimeState b, UnitSkillTargetPriority priority)
        {
            if (a == b)
            {
                return 0;
            }

            float aValue;
            float bValue;

            switch (priority)
            {
                case UnitSkillTargetPriority.NearestToCaster:
                    aValue = HorizontalSqrDistance(owner.transform.position, a.transform.position);
                    bValue = HorizontalSqrDistance(owner.transform.position, b.transform.position);
                    break;

                case UnitSkillTargetPriority.LowestHp:
                    aValue = a.Health != null ? a.Health.CurrentHp : float.MaxValue;
                    bValue = b.Health != null ? b.Health.CurrentHp : float.MaxValue;
                    break;

                default:
                    aValue = ResolveRemainingPathDistance(a);
                    bValue = ResolveRemainingPathDistance(b);
                    break;
            }

            int compare = aValue.CompareTo(bValue);
            if (compare != 0)
            {
                return compare;
            }

            return a.GetInstanceID().CompareTo(b.GetInstanceID());
        }

        private static float ResolveRemainingPathDistance(EnemyRuntimeState enemy)
        {
            if (enemy == null)
            {
                return float.MaxValue;
            }

            if (enemy.IsSummon)
            {
                return float.MaxValue - 1f;
            }

            return enemy.Move != null && enemy.Move.HasPath ? enemy.Move.RemainingPathDistance : float.MaxValue;
        }

        private static float HorizontalSqrDistance(Vector3 a, Vector3 b)
        {
            float x = a.x - b.x;
            float z = a.z - b.z;
            return x * x + z * z;
        }

        private static bool RollCritical(float criticalChancePercent)
        {
            float chance = Mathf.Clamp(criticalChancePercent, 0f, 100f) * 0.01f;
            return chance > 0f && UnityEngine.Random.value < chance;
        }

        private static bool IsValidTarget(EnemyRuntimeState enemy, AttackTarget attackTarget)
        {
            if (enemy == null || !enemy.IsInitialized || enemy.Health == null || enemy.Health.IsDead || enemy.GridPosition == null)
            {
                return false;
            }

            if (!BasicAttackRangeEvaluator.CanAttackTargetLayer(attackTarget, enemy.GridPosition.TargetLayer))
            {
                return false;
            }

            if (enemy.IsSummon)
            {
                return enemy.SummonRuntime != null && enemy.SummonRuntime.IsInitialized;
            }

            return enemy.Move != null && enemy.Move.HasPath && !enemy.Move.HasReachedGoal;
        }

        private EnemyRuntimeState ResolveVisualPrimaryTarget()
        {
            if (IsValidTarget(primaryTarget, activeSettings != null ? activeSettings.AttackTarget : AttackTarget.GroundAndAir))
            {
                return primaryTarget;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                EnemyRuntimeState candidate = targets[i];
                if (candidate != null && candidate.Health != null)
                {
                    return candidate;
                }
            }

            return null;
        }

        private static void PlayVfxAtTarget(EnemyRuntimeState target, UnitSkillSettings settings)
        {
            if (target == null || settings == null || settings.VfxPrefab == null)
            {
                return;
            }

            Vector3 position = target.Anchors != null && target.Anchors.EffectPoint != null
                ? target.Anchors.EffectPoint.position
                : target.transform.position;

            UnitSkillVfxPool.Play(settings.VfxPrefab, position + settings.VfxOffset, settings.VfxScale);
        }

        private void ResetCast()
        {
            isCasting = false;
            currentHitIndex = 0;
            totalHitCount = 0;
            nextHitRemainingSeconds = 0f;
            castLockRemainingSeconds = 0f;
            activeSettings = null;
            primaryTarget = null;
            vfxPlayedForCast = false;
            candidates.Clear();
            targets.Clear();
        }
    }
}
