using System.Collections.Generic;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    internal static class BlockFinder
    {
        internal static bool TryFind(EnemyBlock enemy, Vector2Int tile, out UnitBlock block)
        {
            block = null;

            if (enemy == null || !enemy.CanBeBlocked || enemy.IsBlocked)
            {
                return false;
            }

            if (!CombatRegistry.TryGetUnitsAt(tile, out HashSet<UnitRuntimeState> tileUnits))
            {
                return false;
            }

            foreach (UnitRuntimeState unit in tileUnits)
            {
                if (!IsValid(unit, tile))
                {
                    continue;
                }

                UnitBlock candidate = unit.Block;

                if (candidate != null && candidate.CanBlock(enemy))
                {
                    block = candidate;
                    return true;
                }
            }

            return false;
        }

        private static bool IsValid(UnitRuntimeState unit, Vector2Int tile)
        {
            if (unit == null || !unit.IsInitialized || unit.Health == null || unit.Health.IsDead)
            {
                return false;
            }

            if (unit.GridPosition == null || !unit.GridPosition.IsInitialized)
            {
                return false;
            }

            return unit.GridPosition.TileCoordinate == tile;
        }
    }
}