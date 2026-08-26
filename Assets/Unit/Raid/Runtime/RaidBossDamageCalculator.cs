using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Raid.Data;
using EndlessGuard.Unit.Runtime;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    public static class RaidBossDamageCalculator
    {
        public static bool TryCalculate(UnitRuntimeState attacker, RaidBattleConfigSO config, out DamageInfo damageInfo)
        {
            damageInfo = default;

            if (attacker == null ||
                config == null ||
                !attacker.IsInitialized ||
                attacker.Health == null ||
                attacker.Health.IsDead ||
                attacker.Stats == null ||
                !attacker.Stats.IsInitialized ||
                attacker.DataLink == null ||
                !attacker.DataLink.HasData)
            {
                return false;
            }

            AttackSettings attackSettings = attacker.DataLink.UnitData.AttackSettings;

            if (attackSettings == null || attackSettings.AttackMode == AttackMode.None)
            {
                return false;
            }

            UnitAttack unitAttack = attacker.GetComponent<UnitAttack>();

            if (unitAttack == null || unitAttack.DamageRule == null)
            {
                return false;
            }

            float attackPower;
            float defense;

            switch (attackSettings.DamageType)
            {
                case DamageType.Physical:
                    attackPower = attacker.Stats.PhysicalAttack;
                    defense = config.BossPhysicalDefense;
                    break;

                case DamageType.Magical:
                    attackPower = attacker.Stats.MagicalAttack;
                    defense = config.BossMagicalDefense;
                    break;

                default:
                    return false;
            }

            if (attackPower <= 0f)
            {
                return false;
            }

            float baseDamage = DamageCalculator.Calculate(attackPower, defense, unitAttack.DamageRule);

            if (baseDamage <= 0f)
            {
                return false;
            }

            bool isCritical = RollCritical(attacker.Stats.CriticalChancePercent);
            float finalDamage = baseDamage;

            if (isCritical)
            {
                float criticalMultiplier = 1f + Mathf.Max(0f, attacker.Stats.CriticalDamageBonusPercent) * 0.01f;
                finalDamage *= criticalMultiplier;
            }

            finalDamage *= config.RaidAttackDamageMultiplier;
            damageInfo = new DamageInfo(Mathf.Max(1f, finalDamage), attackSettings.DamageType, isCritical);
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

            return UnityEngine.Random.value < chance * 0.01f;
        }
    }
}
