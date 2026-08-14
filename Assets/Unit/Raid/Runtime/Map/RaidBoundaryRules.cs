using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    internal static class RaidBoundaryRules
    {
        public static float GetStable01(Vector2Int coordinate, Vector2Int direction, int visualKey, uint salt)
        {
            uint hash = unchecked((uint)visualKey);
            hash ^= unchecked((uint)coordinate.x) * 0x9E3779B9u;
            hash ^= unchecked((uint)coordinate.y) * 0x85EBCA6Bu;
            hash ^= unchecked((uint)(direction.x + 2)) * 0xC2B2AE35u;
            hash ^= unchecked((uint)(direction.y + 2)) * 0x27D4EB2Fu;
            hash ^= salt;
            hash ^= hash >> 16;
            hash *= 0x7FEB352Du;
            hash ^= hash >> 15;
            hash *= 0x846CA68Bu;
            hash ^= hash >> 16;
            return (hash & 0x00FFFFFFu) / 16777216f;
        }
    }
}
