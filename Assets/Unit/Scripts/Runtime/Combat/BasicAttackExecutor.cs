using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    public static class BasicAttackExecutor
    {
        public static bool TryExecute(UnitRuntimeState attacker, EnemyRuntimeState target, BasicAttackContext context, out BasicAttackResult result)
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

            BasicAttackFailureReason failureReason = Validate(attacker.IsInitialized, attacker.Health, attacker.DataLink != null && attacker.DataLink.HasData, target.IsInitialized, target.Health, target.DataLink != null && target.DataLink.HasData);

            if (failureReason != BasicAttackFailureReason.None)
            {
                result = BasicAttackResult.Failed(failureReason);
                return false;
            }

            CombatStats attackerStats = attacker.DataLink.UnitData.BaseStats;
            AttackSettings attackSettings = attacker.DataLink.UnitData.AttackSettings;
            CombatStats targetStats = target.DataLink.EnemyData.BaseStats;

            if (!BasicAttackRangeEvaluator.TryEvaluate(attackSettings, context, out _, out failureReason))
            {
                result = BasicAttackResult.Failed(failureReason);
                return false;
            }

            if (!TryResolveDamageValues(attackerStats, attackSettings, targetStats, out float attackPower, out float defense, out failureReason))
            {
                result = BasicAttackResult.Failed(failureReason);
                return false;
            }

            if (attacker.ReadyAttackCount <= 0)
            {
                result = BasicAttackResult.Failed(BasicAttackFailureReason.NoReadyAttack);
                return false;
            }

            float calculatedDamage = CalculateDamage(attackPower, defense);
            int consumedCount = attacker.ConsumeReadyAttacks(1);

            if (consumedCount != 1)
            {
                result = BasicAttackResult.Failed(BasicAttackFailureReason.NoReadyAttack);
                return false;
            }

            float appliedDamage = target.ApplyDamage(calculatedDamage);
            float gainedSkillGauge = appliedDamage > 0f ? attacker.AddSkillGauge(attacker.DataLink.UnitData.SkillGaugePerAttack) : 0f;
            result = new BasicAttackResult(true, BasicAttackFailureReason.None, attackSettings.DamageType, attackPower, defense, calculatedDamage, appliedDamage, gainedSkillGauge, target.Health.IsDead);
            return true;
        }

        public static bool TryExecute(EnemyRuntimeState attacker, UnitRuntimeState target, BasicAttackContext context, out BasicAttackResult result)
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

            BasicAttackFailureReason failureReason = Validate(attacker.IsInitialized, attacker.Health, attacker.DataLink != null && attacker.DataLink.HasData, target.IsInitialized, target.Health, target.DataLink != null && target.DataLink.HasData);

            if (failureReason != BasicAttackFailureReason.None)
            {
                result = BasicAttackResult.Failed(failureReason);
                return false;
            }

            CombatStats attackerStats = attacker.DataLink.EnemyData.BaseStats;
            AttackSettings attackSettings = attacker.DataLink.EnemyData.AttackSettings;
            CombatStats targetStats = target.DataLink.UnitData.BaseStats;

            if (!BasicAttackRangeEvaluator.TryEvaluate(attackSettings, context, out _, out failureReason))
            {
                result = BasicAttackResult.Failed(failureReason);
                return false;
            }

            if (!TryResolveDamageValues(attackerStats, attackSettings, targetStats, out float attackPower, out float defense, out failureReason))
            {
                result = BasicAttackResult.Failed(failureReason);
                return false;
            }

            if (attacker.ReadyAttackCount <= 0)
            {
                result = BasicAttackResult.Failed(BasicAttackFailureReason.NoReadyAttack);
                return false;
            }

            float calculatedDamage = CalculateDamage(attackPower, defense);
            int consumedCount = attacker.ConsumeReadyAttacks(1);

            if (consumedCount != 1)
            {
                result = BasicAttackResult.Failed(BasicAttackFailureReason.NoReadyAttack);
                return false;
            }

            float appliedDamage = target.ApplyDamage(calculatedDamage);
            result = new BasicAttackResult(true, BasicAttackFailureReason.None, attackSettings.DamageType, attackPower, defense, calculatedDamage, appliedDamage, 0f, target.Health.IsDead);
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

        private static bool TryResolveDamageValues(CombatStats attackerStats, AttackSettings attackSettings, CombatStats targetStats, out float attackPower, out float defense, out BasicAttackFailureReason failureReason)
        {
            attackPower = 0f;
            defense = 0f;
            failureReason = BasicAttackFailureReason.None;

            if (attackerStats == null || attackSettings == null || targetStats == null)
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

        private static float CalculateDamage(float attackPower, float defense)
        {
            return Mathf.Max(1f, attackPower - Mathf.Max(0f, defense));
        }
    }
}