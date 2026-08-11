using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    public static class HitCalculator
    {
        public static float CalculatePercent(float accuracy, float evasion, HitRuleSO rule)
        {
            if (rule == null)
            {
                return 0f;
            }

            float safeAccuracy = Mathf.Max(0f, accuracy);
            float safeEvasion = Mathf.Max(0f, evasion);

            if (safeAccuracy <= 0f && safeEvasion <= 0f)
            {
                return rule.ZeroStatHitChancePercent;
            }

            float weightedEvasion = safeEvasion * rule.EvasionWeight;
            float denominator = safeAccuracy + weightedEvasion;

            if (denominator <= 0f)
            {
                return rule.MinimumHitChancePercent;
            }

            float calculatedPercent = safeAccuracy / denominator * 100f;
            return Mathf.Clamp(calculatedPercent, rule.MinimumHitChancePercent, rule.MaximumHitChancePercent);
        }

        public static bool Roll(float hitChancePercent)
        {
            float chance = Mathf.Clamp(hitChancePercent, 0f, 100f);

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
    }
}