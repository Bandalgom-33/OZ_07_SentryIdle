using System;
using System.Collections.Generic;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    internal static class CombatRegistry
    {
        private static readonly HashSet<UnitRuntimeState> units = new HashSet<UnitRuntimeState>();
        private static readonly HashSet<EnemyRuntimeState> enemies = new HashSet<EnemyRuntimeState>();

        private static readonly Dictionary<Vector2Int, HashSet<UnitRuntimeState>> unitsByTile = new Dictionary<Vector2Int, HashSet<UnitRuntimeState>>();
        private static readonly Dictionary<Vector2Int, HashSet<EnemyRuntimeState>> enemiesByTile = new Dictionary<Vector2Int, HashSet<EnemyRuntimeState>>();

        private static readonly Dictionary<UnitRuntimeState, Vector2Int> indexedUnitTiles = new Dictionary<UnitRuntimeState, Vector2Int>();
        private static readonly Dictionary<EnemyRuntimeState, Vector2Int> indexedEnemyTiles = new Dictionary<EnemyRuntimeState, Vector2Int>();

        private static readonly Dictionary<CombatGridPosition, UnitRuntimeState> unitByGridPosition = new Dictionary<CombatGridPosition, UnitRuntimeState>();
        private static readonly Dictionary<CombatGridPosition, EnemyRuntimeState> enemyByGridPosition = new Dictionary<CombatGridPosition, EnemyRuntimeState>();

        internal static event Action<EnemyRuntimeState> OnEnemyRegistered;
        internal static event Action<EnemyRuntimeState> OnEnemyUnregistered;

        internal static HashSet<UnitRuntimeState> Units => units;
        internal static HashSet<EnemyRuntimeState> Enemies => enemies;
        internal static int UnitCount => units.Count;
        internal static int EnemyCount => enemies.Count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            units.Clear();
            enemies.Clear();

            unitsByTile.Clear();
            enemiesByTile.Clear();

            indexedUnitTiles.Clear();
            indexedEnemyTiles.Clear();

            unitByGridPosition.Clear();
            enemyByGridPosition.Clear();

            OnEnemyRegistered = null;
            OnEnemyUnregistered = null;
        }

        internal static void Register(UnitRuntimeState unit)
        {
            if (unit == null)
            {
                return;
            }

            if (!units.Add(unit))
            {
                RefreshUnitTile(unit);
                return;
            }

            CombatGridPosition gridPosition = unit.GridPosition;

            if (gridPosition != null)
            {
                unitByGridPosition[gridPosition] = unit;
                gridPosition.OnTileChanged += HandleUnitTileChanged;
            }

            RefreshUnitTile(unit);
        }

        internal static void Register(EnemyRuntimeState enemy)
        {
            if (enemy == null)
            {
                return;
            }

            if (!enemies.Add(enemy))
            {
                RefreshEnemyTile(enemy);
                return;
            }

            CombatGridPosition gridPosition = enemy.GridPosition;

            if (gridPosition != null)
            {
                enemyByGridPosition[gridPosition] = enemy;
                gridPosition.OnTileChanged += HandleEnemyTileChanged;
            }

            RefreshEnemyTile(enemy);

            OnEnemyRegistered?.Invoke(enemy);
        }

        internal static void Unregister(UnitRuntimeState unit)
        {
            if (unit == null)
            {
                return;
            }

            units.Remove(unit);

            CombatGridPosition gridPosition = unit.GridPosition;

            if (gridPosition != null)
            {
                gridPosition.OnTileChanged -= HandleUnitTileChanged;
                unitByGridPosition.Remove(gridPosition);
            }

            RemoveIndexedUnitTile(unit);
        }

        internal static void Unregister(EnemyRuntimeState enemy)
        {
            if (enemy == null)
            {
                return;
            }

            bool wasRegistered = enemies.Remove(enemy);

            CombatGridPosition gridPosition = enemy.GridPosition;

            if (gridPosition != null)
            {
                gridPosition.OnTileChanged -= HandleEnemyTileChanged;
                enemyByGridPosition.Remove(gridPosition);
            }

            RemoveIndexedEnemyTile(enemy);

            if (wasRegistered)
            {
                OnEnemyUnregistered?.Invoke(enemy);
            }
        }

        internal static bool TryGetUnitsAt(Vector2Int tile, out HashSet<UnitRuntimeState> tileUnits)
        {
            if (unitsByTile.TryGetValue(tile, out tileUnits) && tileUnits.Count > 0)
            {
                return true;
            }

            tileUnits = null;
            return false;
        }

        internal static bool TryGetEnemiesAt(Vector2Int tile, out HashSet<EnemyRuntimeState> tileEnemies)
        {
            if (enemiesByTile.TryGetValue(tile, out tileEnemies) && tileEnemies.Count > 0)
            {
                return true;
            }

            tileEnemies = null;
            return false;
        }

        private static void HandleUnitTileChanged(CombatGridPosition gridPosition)
        {
            if (gridPosition == null || !unitByGridPosition.TryGetValue(gridPosition, out UnitRuntimeState unit))
            {
                return;
            }

            RefreshUnitTile(unit);
        }

        private static void HandleEnemyTileChanged(CombatGridPosition gridPosition)
        {
            if (gridPosition == null || !enemyByGridPosition.TryGetValue(gridPosition, out EnemyRuntimeState enemy))
            {
                return;
            }

            RefreshEnemyTile(enemy);
        }

        private static void RefreshUnitTile(UnitRuntimeState unit)
        {
            RemoveIndexedUnitTile(unit);

            CombatGridPosition gridPosition = unit.GridPosition;

            if (gridPosition == null || !gridPosition.IsInitialized)
            {
                return;
            }

            Vector2Int tile = gridPosition.TileCoordinate;

            if (!unitsByTile.TryGetValue(tile, out HashSet<UnitRuntimeState> tileUnits))
            {
                tileUnits = new HashSet<UnitRuntimeState>();
                unitsByTile.Add(tile, tileUnits);
            }

            tileUnits.Add(unit);
            indexedUnitTiles[unit] = tile;
        }

        private static void RefreshEnemyTile(EnemyRuntimeState enemy)
        {
            RemoveIndexedEnemyTile(enemy);

            CombatGridPosition gridPosition = enemy.GridPosition;

            if (gridPosition == null || !gridPosition.IsInitialized)
            {
                return;
            }

            Vector2Int tile = gridPosition.TileCoordinate;

            if (!enemiesByTile.TryGetValue(tile, out HashSet<EnemyRuntimeState> tileEnemies))
            {
                tileEnemies = new HashSet<EnemyRuntimeState>();
                enemiesByTile.Add(tile, tileEnemies);
            }

            tileEnemies.Add(enemy);
            indexedEnemyTiles[enemy] = tile;
        }

        private static void RemoveIndexedUnitTile(UnitRuntimeState unit)
        {
            if (!indexedUnitTiles.TryGetValue(unit, out Vector2Int tile))
            {
                return;
            }

            if (unitsByTile.TryGetValue(tile, out HashSet<UnitRuntimeState> tileUnits))
            {
                tileUnits.Remove(unit);
            }

            indexedUnitTiles.Remove(unit);
        }

        private static void RemoveIndexedEnemyTile(EnemyRuntimeState enemy)
        {
            if (!indexedEnemyTiles.TryGetValue(enemy, out Vector2Int tile))
            {
                return;
            }

            if (enemiesByTile.TryGetValue(tile, out HashSet<EnemyRuntimeState> tileEnemies))
            {
                tileEnemies.Remove(enemy);
            }

            indexedEnemyTiles.Remove(enemy);
        }
    }
}