using System;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    public readonly struct UnitDiedInfo
    {
        public int RuntimeId { get; }
        public string UnitId { get; }
        public Vector3 Position { get; }

        public UnitDiedInfo(int runtimeId, string unitId, Vector3 position)
        {
            RuntimeId = runtimeId;
            UnitId = unitId ?? string.Empty;
            Position = position;
        }
    }

    public readonly struct EnemyDiedInfo
    {
        public int RuntimeId { get; }
        public string EnemyId { get; }
        public EnemySize EnemySize { get; }
        public Vector3 Position { get; }

        public EnemyDiedInfo(int runtimeId, string enemyId, EnemySize enemySize, Vector3 position)
        {
            RuntimeId = runtimeId;
            EnemyId = enemyId ?? string.Empty;
            EnemySize = enemySize;
            Position = position;
        }
    }

    public readonly struct UnitDamageDealtInfo
    {
        public UnitRuntimeState Source { get; }
        public UnitRuntimeState ActualAttacker { get; }
        public EnemyRuntimeState Target { get; }
        public float AppliedDamage { get; }
        public DamageType DamageType { get; }
        public bool IsCritical { get; }
        public bool IsSummonAttack { get; }

        public UnitDamageDealtInfo(
            UnitRuntimeState source,
            UnitRuntimeState actualAttacker,
            EnemyRuntimeState target,
            float appliedDamage,
            DamageType damageType,
            bool isCritical,
            bool isSummonAttack)
        {
            Source = source;
            ActualAttacker = actualAttacker;
            Target = target;
            AppliedDamage = Mathf.Max(0f, appliedDamage);
            DamageType = damageType;
            IsCritical = isCritical;
            IsSummonAttack = isSummonAttack;
        }
    }

    public readonly struct EnemyReachedGoalInfo
    {
        public int RuntimeId { get; }
        public string EnemyId { get; }
        public Vector3 Position { get; }

        public EnemyReachedGoalInfo(int runtimeId, string enemyId, Vector3 position)
        {
            RuntimeId = runtimeId;
            EnemyId = enemyId ?? string.Empty;
            Position = position;
        }
    }

    public static class CombatEvents
    {
        private static int nextRuntimeId;

        public static event Action<UnitDiedInfo> OnUnitDied;
        public static event Action<UnitDamageDealtInfo> OnUnitDamageDealt;
        public static event Action<EnemyDiedInfo> OnEnemyDied;
        public static event Action<EnemyReachedGoalInfo> OnEnemyReachedGoal;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            nextRuntimeId = 0;
            OnUnitDied = null;
            OnUnitDamageDealt = null;
            OnEnemyDied = null;
            OnEnemyReachedGoal = null;
        }

        internal static int AllocateRuntimeId()
        {
            if (nextRuntimeId == int.MaxValue)
            {
                nextRuntimeId = 0;
            }

            nextRuntimeId++;
            return nextRuntimeId;
        }

        internal static void PublishUnitDied(UnitDiedInfo info)
        {
            OnUnitDied?.Invoke(info);
        }

        internal static void PublishUnitDamageDealt(UnitDamageDealtInfo info)
        {
            if (info.Source == null || info.Target == null || info.AppliedDamage <= 0f)
            {
                return;
            }

            OnUnitDamageDealt?.Invoke(info);
        }

        internal static void PublishEnemyDied(EnemyDiedInfo info)
        {
            OnEnemyDied?.Invoke(info);
        }

        internal static void PublishEnemyReachedGoal(EnemyReachedGoalInfo info)
        {
            OnEnemyReachedGoal?.Invoke(info);
        }
    }
}
