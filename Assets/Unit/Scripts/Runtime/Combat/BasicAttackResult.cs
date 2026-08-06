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
        GridContextUnavailable = 15
    }

    public readonly struct BasicAttackResult
    {
        public bool Succeeded { get; }
        public BasicAttackFailureReason FailureReason { get; }
        public DamageType DamageType { get; }
        public float AttackPower { get; }
        public float Defense { get; }
        public float CalculatedDamage { get; }
        public float AppliedDamage { get; }
        public float SkillGaugeGained { get; }
        public bool TargetDied { get; }

        public BasicAttackResult(bool succeeded, BasicAttackFailureReason failureReason, DamageType damageType, float attackPower, float defense, float calculatedDamage, float appliedDamage, float skillGaugeGained, bool targetDied)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
            DamageType = damageType;
            AttackPower = attackPower;
            Defense = defense;
            CalculatedDamage = calculatedDamage;
            AppliedDamage = appliedDamage;
            SkillGaugeGained = skillGaugeGained;
            TargetDied = targetDied;
        }

        public static BasicAttackResult Failed(BasicAttackFailureReason failureReason)
        {
            return new BasicAttackResult(false, failureReason, DamageType.None, 0f, 0f, 0f, 0f, 0f, false);
        }
    }
}