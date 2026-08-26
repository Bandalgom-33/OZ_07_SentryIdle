using System;
using EndlessGuard.Unit.Raid.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    internal sealed class RaidMapDecorView
    {
        private readonly RaidTileVisualSetSO visualSet;
        private readonly RaidVisualSpawner spawner;

        public RaidMapDecorView(RaidTileVisualSetSO visualSet, RaidVisualSpawner spawner)
        {
            this.visualSet = visualSet ?? throw new ArgumentNullException(nameof(visualSet));
            this.spawner = spawner ?? throw new ArgumentNullException(nameof(spawner));
        }

        public void Build(RaidBoard board, RaidMapSO mapData, Action<Vector2Int, GameObject> onSpawned = null)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (mapData == null)
            {
                throw new ArgumentNullException(nameof(mapData));
            }

            float artScale = board.TileSize / RaidDungeonMetrics.TileSize;
            Vector3 origin = board.TileToWorld(Vector2Int.zero);
            Vector3 tileRight = board.TileToWorld(Vector2Int.right) - origin;
            Vector3 tileForward = board.TileToWorld(Vector2Int.up) - origin;

            for (int i = 0; i < mapData.DecorCount; i++)
            {
                RaidMapDecorData decor = mapData.GetDecor(i);
                GameObject prefab = visualSet.GetDecorPrefab(decor.Kind);

                if (prefab == null)
                {
                    throw new InvalidOperationException($"Raid 고정 장식 Prefab이 없습니다. Kind: {decor.Kind}");
                }

                Vector3 position = board.TileToWorld(decor.Coordinate, decor.HeightOffset * artScale);
                position += tileRight * decor.TileOffset.x + tileForward * decor.TileOffset.y;
                GameObject instance = spawner.SpawnArt(prefab, position, Quaternion.Euler(0f, decor.Yaw, 0f), artScale * decor.Scale);
                onSpawned?.Invoke(decor.Coordinate, instance);
            }
        }
    }
}
