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
        public static event Action<EnemyDiedInfo> OnEnemyDied;
        public static event Action<EnemyReachedGoalInfo> OnEnemyReachedGoal;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            nextRuntimeId = 0;
            OnUnitDied = null;
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
