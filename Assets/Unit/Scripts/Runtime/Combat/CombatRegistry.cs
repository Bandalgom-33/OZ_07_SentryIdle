using System.Collections.Generic;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    internal static class CombatRegistry
    {
        private static readonly HashSet<UnitRuntimeState> units = new HashSet<UnitRuntimeState>();
        private static readonly HashSet<EnemyRuntimeState> enemies = new HashSet<EnemyRuntimeState>();

        internal static HashSet<UnitRuntimeState> Units => units;
        internal static HashSet<EnemyRuntimeState> Enemies => enemies;
        internal static int UnitCount => units.Count;
        internal static int EnemyCount => enemies.Count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            units.Clear();
            enemies.Clear();
        }

        internal static void Register(UnitRuntimeState unit)
        {
            if (unit != null)
            {
                units.Add(unit);
            }
        }

        internal static void Register(EnemyRuntimeState enemy)
        {
            if (enemy != null)
            {
                enemies.Add(enemy);
            }
        }

        internal static void Unregister(UnitRuntimeState unit)
        {
            if (unit != null)
            {
                units.Remove(unit);
            }
        }

        internal static void Unregister(EnemyRuntimeState enemy)
        {
            if (enemy != null)
            {
                enemies.Remove(enemy);
            }
        }
    }
}