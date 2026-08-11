using EndlessGuard.Unit.Data;

namespace EndlessGuard.Unit.Runtime
{
    public readonly struct DamageInfo
    {
        public float FinalDamage { get; }
        public DamageType DamageType { get; }
        public bool IsCritical { get; }

        public DamageInfo(float finalDamage, DamageType damageType, bool isCritical)
        {
            FinalDamage = finalDamage;
            DamageType = damageType;
            IsCritical = isCritical;
        }
    }
}