using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    public readonly struct PathNode
    {
        public Vector3 Position { get; }
        public Vector2Int Tile { get; }
        public GridFacingDirection Facing { get; }

        public PathNode(Vector3 position, Vector2Int tile, GridFacingDirection facing)
        {
            Position = position;
            Tile = tile;
            Facing = facing;
        }
    }
}