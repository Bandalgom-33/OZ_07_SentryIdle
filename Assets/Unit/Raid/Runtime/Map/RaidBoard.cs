using EndlessGuard.Unit.Raid.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    public sealed class RaidBoard
    {
        private readonly RaidGrid grid;
        private readonly RaidGridSpace space;

        public int Width => grid.Width;
        public int Height => grid.Height;
        public int Count => grid.Count;
        public float TileSize => space.TileSize;
        public Vector3 LocalOrigin => space.LocalOrigin;

        public RaidBoard(Transform root, int width, int height, Vector3 localOrigin, float tileSize)
        {
            grid = new RaidGrid(width, height);
            space = new RaidGridSpace(root, localOrigin, tileSize);
        }

        public bool IsInside(Vector2Int coordinate)
        {
            return grid.IsInside(coordinate);
        }

        public bool TryGetTile(Vector2Int coordinate, out RaidTile tile)
        {
            return grid.TryGetTile(coordinate, out tile);
        }

        public RaidTile GetTile(Vector2Int coordinate)
        {
            return grid.GetTile(coordinate);
        }

        public void SetTile(Vector2Int coordinate, RaidTile tile)
        {
            grid.SetTile(coordinate, tile);
        }

        public Vector3 TileToWorld(Vector2Int coordinate)
        {
            return space.TileToWorld(coordinate);
        }

        public Vector3 TileToWorld(Vector2Int coordinate, float heightOffset)
        {
            return space.TileToWorld(coordinate, heightOffset);
        }

        public bool TryWorldToTile(Vector3 worldPosition, out Vector2Int coordinate)
        {
            coordinate = space.WorldToTile(worldPosition);
            return grid.IsInside(coordinate);
        }
    }
}