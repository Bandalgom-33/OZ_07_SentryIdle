using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    public static class BasicAttackExecutor
    {
        public static bool TryExecute(UnitRuntimeState attacker, EnemyRuntimeState target, BasicAttackContext context, out BasicAttackResult result)
        {
            HitRuleSO hitRule = attacker != null && attacker.Attack != null ? attacker.Attack.HitRule : null;
            DamageRuleSO damageRule = attacker != null && attacker.Attack != null ? attacker.Attack.DamageRule : null;
            return TryExecuteInternal(attacker, target, context, hitRule, damageRule, true, false, true, out result);
        }

        public static bool TryExecute(EnemyRuntimeState attacker, UnitRuntimeState target, BasicAttackContext context, out BasicAttackResult result)
        {
            HitRuleSO hitRule = attacker != null && attacker.Attack != null ? attacker.Attack.HitRule : null;
            DamageRuleSO damageRule = attacker != null && attacker.Attack != null ? attacker.Attack.DamageRule : null;
            return TryExecuteInternal(attacker, target, context, hitRule, damageRule, true, false, out result);
        }

        public static bool TryExecute(UnitRuntimeState attacker, EnemyRuntimeState target, BasicAttackContext context, HitRuleSO hitRule, out BasicAttackResult result)
        {
            DamageRuleSO damageRule = attacker != null && attacker.Attack != null ? attacker.Attack.DamageRule : null;
            return TryExecuteInternal(attacker, target, context, hitRule, damageRule, true, false, true, out result);
        }

        public static bool TryExecute(EnemyRuntimeState attacker, UnitRuntimeState target, BasicAttackContext context, HitRuleSO hitRule, out BasicAttackResult result)
        {
            DamageRuleSO damageRule = attacker != null && attacker.Attack != null ? attacker.Attack.DamageRule : null;
            return TryExecuteInternal(attacker, target, context, hitRule, damageRule, true, false, out result);
        }

        public static bool TryExecute(UnitRuntimeState attacker, EnemyRuntimeState target, BasicAttackContext context, HitRuleSO hitRule, DamageRuleSO damageRule, out BasicAttackResult result)
        {
            return TryExecuteInternal(attacker, target, context, hitRule, damageRule, true, false, true, out result);
        }

        public static bool TryExecute(EnemyRuntimeState attacker, UnitRuntimeState target, BasicAttackContext context, HitRuleSO hitRule, DamageRuleSO damageRule, out BasicAttackResult result)
        {
            return TryExecuteInternal(attacker, target, context, hitRule, damageRule, true, false, out result);
        }

        internal static bool TryExecute(UnitRuntimeState attacker, EnemyRuntimeState target, BasicAttackContext context, bool consumeReadyAttack, bool ignoreRange, out BasicAttackResult result)
        {
            return TryExecute(attacker, target, context, consumeReadyAttack, ignoreRange, true, out result);
        }

        internal static bool TryExecute(UnitRuntimeState attacker, EnemyRuntimeState target, BasicAttackContext context, bool consumeReadyAttack, bool ignoreRange, bool gainSkillGauge, out BasicAttackResult result)
        {
            HitRuleSO hitRule = attacker != null && attacker.Attack != null ? attacker.Attack.HitRule : null;
            DamageRuleSO damageRule = attacker != null && attacker.Attack != null ? attacker.Attack.DamageRule : null;
            return TryExecuteInternal(attacker, target, context, hitRule, damageRule, consumeReadyAttack, ignoreRange, gainSkillGauge, out result);
        }

        internal static bool TryExecute(EnemyRuntimeState attacker, UnitRuntimeState target, BasicAttackContext context, bool consumeReadyAttack, bool ignoreRange, out BasicAttackResult result)
        {
            HitRuleSO hitRule = attacker != null && attacker.Attack != null ? attacker.Attack.HitRule : null;
            DamageRuleSO damageRule = attacker != null && attacker.Attack != null ? attacker.Attack.DamageRule : null;
            return TryExecuteInternal(attacker, target, context, hitRule, damageRule, consumeReadyAttack, ignoreRange, out result);
        }

        private static bool TryExecuteInternal(UnitRuntimeState attacker, EnemyRuntimeState target, BasicAttackContext context, HitRuleSO hitRule, DamageRuleSO damageRule, bool consumeReadyAttack, bool ignoreRange, bool gainSkillGauge, out BasicAttackResult result)
        {
            if (attacker == null)
            {
                result = BasicAttackResult.Failed(BasicAttackFailureReason.MissingAttacker);
                return false;
            }

            if (target == null)
            {
                result = BasicAttackResult.Failed(BasicAttackFailureReason.MissingTarget);
                return false;
            }

            if (hitRule == null)
            {
                result = BasicAttackResult.Failed(BasicAttackFailureReason.MissingHitRule);
                return false;
            }

            if (damageRule == null)
            {
                result = BasicAttackResult.Failed(BasicAttackFailureReason.MissingData);
                return false;
            }

            BasicAttackFailureReason failureReason = Validate(attacker.IsInitialized, attacker.Health, attacker.DataLink != null && attacker.DataLink.HasData, target.IsInitialized, target.Health, target.DataLink != null && target.DataLink.HasData);

            if (failureReason != BasicAttackFailureReason.None)
            {
                result = BasicAttackResult.Failed(failureReason);
                return false;
            }

            RuntimeStats attackerStats = attacker.Stats;
            AttackSettings attackSettings = attacker.DataLink.UnitData.AttackSettings;
            RuntimeStats targetStats = target.Stats;

            if (!ignoreRange)
            {
                bool baseLayerAllowed = BasicAttackRangeEvaluator.CanAttackTargetLayer(attackSettings.AttackTarget, context.TargetLayer);
                bool passiveLayerAllowed = attacker.Passives != null && attacker.Passives.AllowsTargetLayer(attacker, context.TargetLayer);
                bool ignoreTargetLayer = !baseLayerAllowed && passiveLayerAllowed;

                if (!BasicAttackRangeEvaluator.TryEvaluate(attackSettings, context, ignoreTargetLayer, out _, out failureReason))
                {
                    result = BasicAttackResult.Failed(failureReason);
                    return false;
                }
            }

            if (!TryResolveDamageValues(attackerStats, attackSettings, targetStats, out float attackPower, out float defense, out failureReason))
            {
                result = BasicAttackResult.Failed(failureReason);
                return false;
            }

            if (attacker.Passives != null)
            {
                attackPower = attacker.Passives.ModifyAttackPower(attacker, target, attackPower);
            }

            if (attackPower <= 0f)
            {
                result = BasicAttackResult.Failed(BasicAttackFailureReason.NoAttackPower);
                return false;
            }

            if (consumeReadyAttack && !TryConsumeReadyAttack(attacker, out result))
            {
                return false;
            }

            float hitChancePercent = HitCalculator.CalculatePercent(attackerStats.Accuracy, targetStats.Evasion, hitRule);
            bool wasHit = HitCalculator.Roll(hitChancePercent);

            if (!wasHit)
            {
                PublishMiss(target.Health, target.Anchors, target.transform);
                result = BasicAttackResult.Missed(attackSettings.DamageType, attackPower, defense, hitChancePercent);
                NotifyResolved(attacker, target, result);
                return true;
            }

            float baseDamage = DamageCalculator.Calculate(attackPower, defense, damageRule);
            bool isCritical = RollCritical(attackerStats.CriticalChancePercent);
            float criticalDamage = CalculateCriticalDamage(baseDamage, attackerStats.CriticalDamageBonusPercent, isCritical);
            float calculatedDamage = attacker.Passives != null ? attacker.Passives.ModifyOutgoingDamage(attacker, target, criticalDamage) : criticalDamage;

            DamageInfo damageInfo = new DamageInfo(calculatedDamage, attackSettings.DamageType, isCritical);
            float appliedDamage = target.ApplyDamage(damageInfo);
            float gainedSkillGauge = gainSkillGauge && appliedDamage > 0f ? attacker.AddSkillGauge(attacker.DataLink.UnitData.SkillGaugePerAttack) : 0f;

            result = new BasicAttackResult(true, BasicAttackFailureReason.None, attackSettings.DamageType, attackPower, defense, hitChancePercent, true, calculatedDamage, appliedDamage, isCritical, gainedSkillGauge, target.Health.IsDead);
            NotifyResolved(attacker, target, result);
            return true;
        }

        private static bool TryExecuteInternal(EnemyRuntimeState attacker, UnitRuntimeState target, BasicAttackContext context, HitRuleSO hitRule, DamageRuleSO damageRule, bool consumeReadyAttack, bool ignoreRange, out BasicAttackResult result)
        {
            if (attacker == null)
            {
                result = BasicAttackResult.Failed(BasicAttackFailureReason.MissingAttacker);
                return false;
            }

            if (target == null)
            {
                result = BasicAttackResult.Failed(BasicAttackFailureReason.MissingTarget);
                return false;
            }

            if (hitRule == null)
            {
                result = BasicAttackResult.Failed(BasicAttackFailureReason.MissingHitRule);
                return false;
            }

            if (damageRule == null)
            {
                result = BasicAttackResult.Failed(BasicAttackFailureReason.MissingData);
                return false;
            }

            BasicAttackFailureReason failureReason = Validate(attacker.IsInitialized, attacker.Health, attacker.DataLink != null && attacker.DataLink.HasData, target.IsInitialized, target.Health, target.DataLink != null && target.DataLink.HasData);

            if (failureReason != BasicAttackFailureReason.None)
            {
                result = BasicAttackResult.Failed(failureReason);
                return false;
            }

            RuntimeStats attackerStats = attacker.Stats;
            AttackSettings attackSettings = attacker.DataLink.EnemyData.AttackSettings;
            RuntimeStats targetStats = target.Stats;

            if (!ignoreRange && !BasicAttackRangeEvaluator.TryEvaluate(attackSettings, context, out _, out failureReason))
            {
                result = BasicAttackResult.Failed(failureReason);
                return false;
            }

            if (!TryResolveDamageValues(attackerStats, attackSettings, targetStats, out float attackPower, out float defense, out failureReason))
            {
                result = BasicAttackResult.Failed(failureReason);
                return false;
            }

            if (consumeReadyAttack && !TryConsumeReadyAttack(attacker, out result))
            {
                return false;
            }

            float hitChancePercent = HitCalculator.CalculatePercent(attackerStats.Accuracy, targetStats.Evasion, hitRule);
            bool wasHit = HitCalculator.Roll(hitChancePercent);

            if (!wasHit)
            {
                PublishMiss(target.Health, target.Anchors, target.transform);
                result = BasicAttackResult.Missed(attackSettings.DamageType, attackPower, defense, hitChancePercent);
                NotifyResolved(attacker, target, result);
                return true;
            }

            float calculatedDamage = DamageCalculator.Calculate(attackPower, defense, damageRule);
            DamageInfo damageInfo = new DamageInfo(calculatedDamage, attackSettings.DamageType, false);
            float appliedDamage = target.ApplyDamage(damageInfo);

            result = new BasicAttackResult(true, BasicAttackFailureReason.None, attackSettings.DamageType, attackPower, defense, hitChancePercent, true, calculatedDamage, appliedDamage, false, 0f, target.Health.IsDead);
            NotifyResolved(attacker, target, result);
            return true;
        }

        private static void NotifyResolved(UnitRuntimeState attacker, EnemyRuntimeState target, BasicAttackResult result)
        {
            attacker.Passives?.NotifyBasicAttackResolved(attacker, target, result);
            target.Passives?.NotifyBasicAttackReceived(target, attacker, result);
        }

        private static void NotifyResolved(EnemyRuntimeState attacker, UnitRuntimeState target, BasicAttackResult result)
        {
            attacker.Passives?.NotifyBasicAttackResolved(attacker, target, result);
            target.Passives?.NotifyBasicAttackReceived(target, attacker, result);
        }

        private static bool TryConsumeReadyAttack(UnitRuntimeState attacker, out BasicAttackResult result)
        {
            if (attacker.ReadyAttackCount <= 0 || attacker.ConsumeReadyAttacks(1) != 1)
            {
                result = BasicAttackResult.Failed(BasicAttackFailureReason.NoReadyAttack);
                return false;
            }

            result = default;
            return true;
        }

        private static bool TryConsumeReadyAttack(EnemyRuntimeState attacker, out BasicAttackResult result)
        {
            if (attacker.ReadyAttackCount <= 0 || attacker.ConsumeReadyAttacks(1) != 1)
            {
                result = BasicAttackResult.Failed(BasicAttackFailureReason.NoReadyAttack);
                return false;
            }

            result = default;
            return true;
        }

        private static BasicAttackFailureReason Validate(bool attackerInitialized, CombatHealth attackerHealth, bool attackerHasData, bool targetInitialized, CombatHealth targetHealth, bool targetHasData)
        {
            if (!attackerInitialized)
            {
                return BasicAttackFailureReason.AttackerNotInitialized;
            }

            if (!targetInitialized)
            {
                return BasicAttackFailureReason.TargetNotInitialized;
            }

            if (attackerHealth == null || targetHealth == null || !attackerHasData || !targetHasData)
            {
                return BasicAttackFailureReason.MissingData;
            }

            if (attackerHealth.IsDead)
            {
                return BasicAttackFailureReason.AttackerDead;
            }

            if (targetHealth.IsDead)
            {
                return BasicAttackFailureReason.TargetDead;
            }

            return BasicAttackFailureReason.None;
        }

        private static bool TryResolveDamageValues(RuntimeStats attackerStats, AttackSettings attackSettings, RuntimeStats targetStats, out float attackPower, out float defense, out BasicAttackFailureReason failureReason)
        {
            attackPower = 0f;
            defense = 0f;
            failureReason = BasicAttackFailureReason.None;

            if (attackerStats == null || !attackerStats.IsInitialized || attackSettings == null || targetStats == null || !targetStats.IsInitialized)
            {
                failureReason = BasicAttackFailureReason.MissingData;
                return false;
            }

            if (attackSettings.AttackMode == AttackMode.None)
            {
                failureReason = BasicAttackFailureReason.AttackDisabled;
                return false;
            }

            switch (attackSettings.DamageType)
            {
                case DamageType.Physical:
                    attackPower = attackerStats.PhysicalAttack;
                    defense = targetStats.PhysicalDefense;
                    break;

                case DamageType.Magical:
                    attackPower = attackerStats.MagicalAttack;
                    defense = targetStats.MagicalDefense;
                    break;

                default:
                    failureReason = BasicAttackFailureReason.InvalidDamageType;
                    return false;
            }

            if (attackPower <= 0f)
            {
                failureReason = BasicAttackFailureReason.NoAttackPower;
                return false;
            }

            return true;
        }

        private static bool RollCritical(float criticalChancePercent)
        {
            float chance = Mathf.Clamp(criticalChancePercent, 0f, 100f);

            if (chance <= 0f)
            {
                return false;
            }

            if (chance >= 100f)
            {
                return true;
            }

            return Random.value < chance * 0.01f;
        }

        private static float CalculateCriticalDamage(float baseDamage, float criticalDamageBonusPercent, bool isCritical)
        {
            if (!isCritical)
            {
                return baseDamage;
            }

            float bonusMultiplier = Mathf.Max(0f, criticalDamageBonusPercent) * 0.01f;
            return Mathf.Max(1f, baseDamage * (1f + bonusMultiplier));
        }

        private static void PublishMiss(CombatHealth targetHealth, CombatEntityAnchors anchors, Transform fallback)
        {
            if (targetHealth == null)
            {
                return;
            }

            Vector3 worldPosition = anchors != null && anchors.EffectPoint != null ? anchors.EffectPoint.position : fallback.position;
            CombatFeedbackEvents.PublishAttackMissed(targetHealth, worldPosition);
        }
    }
}
