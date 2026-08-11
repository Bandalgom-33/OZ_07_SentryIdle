using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    public static class BasicAttackContextFactory
    {
        public static bool TryCreate(UnitRuntimeState attacker, EnemyRuntimeState target, out BasicAttackContext context)
        {
            context = default;

            if (attacker == null || target == null)
            {
                return false;
            }

            return TryCreate(attacker.GridPosition, target.GridPosition, out context);
        }

        public static bool TryCreate(EnemyRuntimeState attacker, UnitRuntimeState target, out BasicAttackContext context)
        {
            context = default;

            if (attacker == null || target == null)
            {
                return false;
            }

            return TryCreate(attacker.GridPosition, target.GridPosition, out context);
        }

        public static bool TryCreate(CombatGridPosition attacker, CombatGridPosition target, out BasicAttackContext context)
        {
            context = default;

            if (attacker == null || target == null)
            {
                return false;
            }

            if (!attacker.IsInitialized || !target.IsInitialized)
            {
                return false;
            }

            Vector2Int relativeTargetTile = target.TileCoordinate - attacker.TileCoordinate;
            Vector3 worldOffset = target.transform.position - attacker.transform.position;
            float horizontalWorldDistance = Mathf.Sqrt(worldOffset.x * worldOffset.x + worldOffset.z * worldOffset.z);
            context = new BasicAttackContext(relativeTargetTile, horizontalWorldDistance, attacker.FacingDirection, target.TargetLayer);
            return true;
        }
    }
}