using System;
using EndlessGuard.Unit.Raid.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    public sealed class RaidBoardView : MonoBehaviour
    {
        private const float RouteHeightOffset = 0.01f;

        [Header("시각")]
        [SerializeField] private RaidTileVisualSetSO visualSet;

        private RaidVisualSpawner spawner;

        public RaidTileVisualSetSO VisualSet => visualSet;

        public void Build(RaidBoard board, int visualKey)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (visualSet == null)
            {
                throw new InvalidOperationException("Raid Tile Visual Set이 연결되지 않았습니다.");
            }

            EnsureSpawner();
            RaidEdgeFoundationView edgeFoundation = new RaidEdgeFoundationView(visualSet, spawner);
            RaidOuterRuinView outerRuinView = new RaidOuterRuinView(visualSet, spawner);
            spawner.Clear();

            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    Vector2Int coordinate = new Vector2Int(x, y);
                    RaidTile tile = board.GetTile(coordinate);
                    Vector3 worldPosition = board.TileToWorld(coordinate);

                    if (tile.IsBridge)
                    {
                        SpawnBridge(tile.Bridge, worldPosition, board.TileSize);
                    }
                    else
                    {
                        GameObject surfacePrefab = RaidTileVisualPicker.PickSurface(visualSet, tile.Surface, coordinate, visualKey);
                        SpawnSurface(surfacePrefab, tile.Surface, worldPosition, board.TileSize);
                    }

                    edgeFoundation.Spawn(board, coordinate, tile);

                    if (tile.HasBlock)
                    {
                        SpawnBlock(tile, coordinate, worldPosition, board.TileSize, visualKey);
                    }

                    GameObject routePrefab = RaidTileVisualPicker.PickRoute(visualSet, tile.Route, coordinate, visualKey);
                    spawner.SpawnTile(routePrefab, worldPosition + Vector3.up * RouteHeightOffset, board.TileSize);

                    GameObject markerPrefab = RaidTileVisualPicker.PickMarker(visualSet, tile.Marker, coordinate, visualKey);
                    spawner.SpawnTile(markerPrefab, worldPosition, board.TileSize);
                }
            }

            outerRuinView.Build(board, visualKey);
        }

        public void Clear()
        {
            if (spawner != null)
            {
                spawner.Clear();
            }
        }

        private void SpawnBridge(RaidTileBridge bridge, Vector3 worldPosition, float tileSize)
        {
            if (visualSet.BridgePrefab == null)
            {
                throw new InvalidOperationException("Raid Bridge Prefab이 연결되지 않았습니다.");
            }

            float scale = tileSize / RaidDungeonMetrics.TileSize;
            float heightOffset = -RaidDungeonMetrics.FoundationHeight * scale;
            Quaternion rotation = bridge == RaidTileBridge.Vertical ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;
            spawner.SpawnArt(visualSet.BridgePrefab, worldPosition + Vector3.up * heightOffset, rotation, scale);
        }

        private void SpawnSurface(GameObject prefab, RaidTileSurface surface, Vector3 worldPosition, float tileSize)
        {
            if (surface == RaidTileSurface.Ground || surface == RaidTileSurface.HighGround)
            {
                float scale = tileSize / RaidDungeonMetrics.TileSize;
                spawner.SpawnArt(prefab, worldPosition, Quaternion.identity, scale);
                return;
            }

            spawner.SpawnTile(prefab, worldPosition, tileSize);
        }

        private void SpawnBlock(RaidTile tile, Vector2Int coordinate, Vector3 worldPosition, float tileSize, int visualKey)
        {
            GameObject prefab = RaidTileVisualPicker.PickBlock(visualSet, tile.Block, coordinate, visualKey);

            if (prefab == null)
            {
                throw new InvalidOperationException($"Raid Block Prefab이 없습니다. Block: {tile.Block}");
            }

            float scale = tileSize / RaidDungeonMetrics.TileSize;
            float yaw = (int)tile.BlockRotation * 90f;
            spawner.SpawnArt(prefab, worldPosition, Quaternion.Euler(0f, yaw, 0f), scale);
        }

        private void EnsureSpawner()
        {
            if (spawner == null)
            {
                spawner = new RaidVisualSpawner(transform);
            }
        }
    }
}
