using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    public static class DamageCalculator
    {
        public static float Calculate(float attackPower, float defense, DamageRuleSO rule)
        {
            if (rule == null)
            {
                return 0f;
            }

            float safeAttack = Mathf.Max(0f, attackPower);
            float safeDefense = Mathf.Max(0f, defense);

            if (safeAttack <= 0f)
            {
                return 0f;
            }

            float denominator = safeAttack + safeDefense * rule.DefenseWeight;

            if (denominator <= 0f)
            {
                return rule.MinimumDamage;
            }

            float damage = safeAttack * safeAttack / denominator;
            return Mathf.Max(rule.MinimumDamage, damage);
        }
    }
}