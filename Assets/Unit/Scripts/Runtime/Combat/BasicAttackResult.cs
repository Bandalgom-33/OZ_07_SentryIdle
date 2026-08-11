using EndlessGuard.Unit.Data;

namespace EndlessGuard.Unit.Runtime
{
    public enum BasicAttackFailureReason
    {
        None = 0,
        MissingAttacker = 1,
        MissingTarget = 2,
        AttackerNotInitialized = 3,
        TargetNotInitialized = 4,
        AttackerDead = 5,
        TargetDead = 6,
        MissingData = 7,
        AttackDisabled = 8,
        InvalidDamageType = 9,
        NoAttackPower = 10,
        NoReadyAttack = 11,
        TargetLayerNotAllowed = 12,
        OutsideWorldRange = 13,
        OutsideAttackTileRange = 14,
        GridContextUnavailable = 15,
        MissingHitRule = 16
    }

    public readonly struct BasicAttackResult
    {
        public bool Succeeded { get; }
        public BasicAttackFailureReason FailureReason { get; }
        public DamageType DamageType { get; }
        public float AttackPower { get; }
        public float Defense { get; }
        public float HitChancePercent { get; }
        public bool WasHit { get; }
        public float CalculatedDamage { get; }
        public float AppliedDamage { get; }
        public bool IsCritical { get; }
        public float SkillGaugeGained { get; }
        public bool TargetDied { get; }

        public BasicAttackResult(bool succeeded, BasicAttackFailureReason failureReason, DamageType damageType, float attackPower, float defense, float calculatedDamage, float appliedDamage, float skillGaugeGained, bool targetDied)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
            DamageType = damageType;
            AttackPower = attackPower;
            Defense = defense;
            HitChancePercent = succeeded ? 100f : 0f;
            WasHit = succeeded;
            CalculatedDamage = calculatedDamage;
            AppliedDamage = appliedDamage;
            IsCritical = false;
            SkillGaugeGained = skillGaugeGained;
            TargetDied = targetDied;
        }

        public BasicAttackResult(bool succeeded, BasicAttackFailureReason failureReason, DamageType damageType, float attackPower, float defense, float calculatedDamage, float appliedDamage, bool isCritical, float skillGaugeGained, bool targetDied)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
            DamageType = damageType;
            AttackPower = attackPower;
            Defense = defense;
            HitChancePercent = succeeded ? 100f : 0f;
            WasHit = succeeded;
            CalculatedDamage = calculatedDamage;
            AppliedDamage = appliedDamage;
            IsCritical = isCritical;
            SkillGaugeGained = skillGaugeGained;
            TargetDied = targetDied;
        }

        public BasicAttackResult(bool succeeded, BasicAttackFailureReason failureReason, DamageType damageType, float attackPower, float defense, float hitChancePercent, bool wasHit, float calculatedDamage, float appliedDamage, bool isCritical, float skillGaugeGained, bool targetDied)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
            DamageType = damageType;
            AttackPower = attackPower;
            Defense = defense;
            HitChancePercent = hitChancePercent;
            WasHit = wasHit;
            CalculatedDamage = calculatedDamage;
            AppliedDamage = appliedDamage;
            IsCritical = isCritical;
            SkillGaugeGained = skillGaugeGained;
            TargetDied = targetDied;
        }

        public static BasicAttackResult Missed(DamageType damageType, float attackPower, float defense, float hitChancePercent)
        {
            return new BasicAttackResult(true, BasicAttackFailureReason.None, damageType, attackPower, defense, hitChancePercent, false, 0f, 0f, false, 0f, false);
        }

        public static BasicAttackResult Failed(BasicAttackFailureReason failureReason)
        {
            return new BasicAttackResult(false, failureReason, DamageType.None, 0f, 0f, 0f, false, 0f, 0f, false, 0f, false);
        }
    }
}