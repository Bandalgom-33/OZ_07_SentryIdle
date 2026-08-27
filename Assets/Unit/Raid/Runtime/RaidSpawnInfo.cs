using System;
using EndlessGuard.Unit.Runtime;

namespace EndlessGuard.Unit.Raid.Runtime
{
    public readonly struct RaidSpawnInfo
    {
        public EnemyRuntimeState Enemy { get; }
        public int EntryNodeId { get; }
        public int PathIndex { get; }

        internal RaidSpawnInfo(EnemyRuntimeState enemy, int entryNodeId, int pathIndex)
        {
            Enemy = enemy != null ? enemy : throw new ArgumentNullException(nameof(enemy));

            if (entryNodeId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(entryNodeId));
            }

            if (pathIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pathIndex));
            }

            EntryNodeId = entryNodeId;
            PathIndex = pathIndex;
        }
    }
}