namespace EndlessGuard.Unit.Runtime
{
    public static class BlockLink
    {
        public static bool TryBind(UnitBlock unit, EnemyBlock enemy)
        {
            if (unit == null || enemy == null)
            {
                return false;
            }

            if (enemy.Blocker == unit)
            {
                return true;
            }

            if (enemy.IsBlocked || !unit.CanBlock(enemy))
            {
                return false;
            }

            unit.Attach(enemy);
            enemy.Attach(unit);

            UnitRuntimeState unitState = unit.State;
            EnemyRuntimeState enemyState = enemy.State;

            unitState?.Passives?.NotifyBlockStarted(unitState, enemyState);
            enemyState?.Passives?.NotifyBlocked(enemyState, unitState);

            return true;
        }

        public static bool Release(EnemyBlock enemy)
        {
            if (enemy == null || !enemy.IsBlocked)
            {
                return false;
            }

            UnitBlock unit = enemy.Blocker;
            UnitRuntimeState unitState = unit != null ? unit.State : null;
            EnemyRuntimeState enemyState = enemy.State;

            unit.Detach(enemy);
            enemy.Detach();

            unitState?.Passives?.NotifyBlockEnded(unitState, enemyState);

            return true;
        }
    }
}
