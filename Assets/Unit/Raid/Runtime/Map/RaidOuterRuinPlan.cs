using System.Collections.Generic;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    internal sealed class RaidOuterRuinPlan
    {
        private readonly HashSet<Vector2Int> tiles = new HashSet<Vector2Int>();
        private readonly Dictionary<Vector2Int, float> cutYaw = new Dictionary<Vector2Int, float>();

        public IEnumerable<Vector2Int> Tiles => tiles;
        public int Count => tiles.Count;

        public bool Contains(Vector2Int coordinate)
        {
            return tiles.Contains(coordinate);
        }

        public bool IsCut(Vector2Int coordinate)
        {
            return cutYaw.ContainsKey(coordinate);
        }

        public bool TryGetCutYaw(Vector2Int coordinate, out float yaw)
        {
            return cutYaw.TryGetValue(coordinate, out yaw);
        }

        public void AddRegular(Vector2Int coordinate)
        {
            tiles.Add(coordinate);
        }

        public void AddCut(Vector2Int coordinate, float yaw)
        {
            tiles.Add(coordinate);
            cutYaw[coordinate] = yaw;
        }
    }
}
