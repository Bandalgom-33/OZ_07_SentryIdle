using System;
using EndlessGuard.Unit.Raid.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    internal sealed class RaidEdgeFoundationView
    {
        private readonly RaidTileVisualSetSO visualSet;
        private readonly RaidVisualSpawner spawner;

        public RaidEdgeFoundationView(RaidTileVisualSetSO visualSet, RaidVisualSpawner spawner)
        {
            this.visualSet = visualSet ?? throw new ArgumentNullException(nameof(visualSet));
            this.spawner = spawner ?? throw new ArgumentNullException(nameof(spawner));

            if (visualSet.BoundaryBasePrefab == null)
            {
                throw new InvalidOperationException("Raid Foundation Prefab이 연결되지 않았습니다.");
            }
        }

        public void Spawn(RaidBoard board, Vector2Int coordinate, RaidTile tile)
        {
            if (tile.IsBridge || tile.Surface != RaidTileSurface.Ground && tile.Surface != RaidTileSurface.HighGround || !TouchesVoid(board, coordinate))
            {
                return;
            }

            float scale = board.TileSize / RaidDungeonMetrics.TileSize;
            float heightOffset = -RaidDungeonMetrics.FoundationHeight * scale;
            spawner.SpawnArt(visualSet.BoundaryBasePrefab, board.TileToWorld(coordinate, heightOffset), Quaternion.identity, scale);
        }

        private static bool TouchesVoid(RaidBoard board, Vector2Int coordinate)
        {
            return IsVoid(board, coordinate + Vector2Int.right) || IsVoid(board, coordinate + Vector2Int.left) || IsVoid(board, coordinate + Vector2Int.up) || IsVoid(board, coordinate + Vector2Int.down);
        }

        private static bool IsVoid(RaidBoard board, Vector2Int coordinate)
        {
            return !board.IsInside(coordinate) || board.GetTile(coordinate).Surface == RaidTileSurface.Void;
        }
    }
}
