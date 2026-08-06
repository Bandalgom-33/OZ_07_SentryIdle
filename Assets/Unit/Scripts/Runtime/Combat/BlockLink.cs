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
            return true;
        }

        public static bool Release(EnemyBlock enemy)
        {
            if (enemy == null || !enemy.IsBlocked)
            {
                return false;
            }

            UnitBlock unit = enemy.Blocker;
            unit.Detach(enemy);
            enemy.Detach();
            return true;
        }
    }
}