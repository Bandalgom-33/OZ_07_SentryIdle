using System;

namespace EndlessGuard.Unit.Runtime
{
    public static class CombatEvents
    {
        public static event Action<UnitRuntimeState> OnUnitDied;
        public static event Action<EnemyRuntimeState> OnEnemyDied;

        internal static void PublishUnitDied(UnitRuntimeState unit)
        {
            OnUnitDied?.Invoke(unit);
        }

        internal static void PublishEnemyDied(EnemyRuntimeState enemy)
        {
            OnEnemyDied?.Invoke(enemy);
        }
    }
}