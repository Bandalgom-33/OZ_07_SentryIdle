using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Raid.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    public sealed class RaidBoardView : MonoBehaviour
    {
        [Header("시각")]
        [SerializeField] private RaidTileVisualSetSO visualSet;

        private RaidVisualSpawner spawner;
        private List<GameObject>[] tileVisuals = Array.Empty<List<GameObject>>();
        private readonly List<GameObject> persistentEffects = new List<GameObject>(4);
        private readonly List<Mesh> persistentEffectMeshes = new List<Mesh>(4);
        private RaidBoard trackedBoard;

        public RaidTileVisualSetSO VisualSet => visualSet;

        public void Build(RaidBoard board, RaidMapSO mapData)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (mapData == null)
            {
                throw new ArgumentNullException(nameof(mapData));
            }

            if (visualSet == null)
            {
                throw new InvalidOperationException("Raid Tile Visual Set이 연결되지 않았습니다.");
            }

            EnsureSpawner();
            RaidEdgeFoundationView edgeFoundation = new RaidEdgeFoundationView(visualSet, spawner);
            RaidMapDecorView decorView = new RaidMapDecorView(visualSet, spawner);
            spawner.Clear();
            PrepareTracking(board);

            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    Vector2Int coordinate = new Vector2Int(x, y);
                    RaidTile tile = board.GetTile(coordinate);
                    Vector3 worldPosition = board.TileToWorld(coordinate);

                    if (tile.IsBridge)
                    {
                        Register(coordinate, SpawnBridge(tile.Bridge, worldPosition, board.TileSize));
                    }
                    else
                    {
                        GameObject surfacePrefab = RaidTileVisualPicker.PickSurface(visualSet, tile, coordinate, mapData.VisualKey);
                        Register(coordinate, SpawnSurface(surfacePrefab, tile, worldPosition, board.TileSize));
                    }

                    Register(coordinate, edgeFoundation.Spawn(board, coordinate, tile));

                    if (tile.HasBlock)
                    {
                        Register(coordinate, SpawnBlock(tile, coordinate, worldPosition, board.TileSize, mapData.VisualKey));
                    }

                    GameObject markerPrefab = RaidTileVisualPicker.PickMarker(visualSet, tile.Marker, coordinate, mapData.VisualKey);
                    Register(coordinate, spawner.SpawnTile(markerPrefab, worldPosition, board.TileSize));
                }
            }

            decorView.Build(board, mapData, Register);
        }

        public void Clear()
        {
            if (spawner != null)
            {
                spawner.Clear();
            }

            ClearPersistentEffects();
            trackedBoard = null;
            tileVisuals = Array.Empty<List<GameObject>>();
        }

        internal void RegisterPersistentEffect(GameObject instance, Mesh mesh)
        {
            if (instance == null || mesh == null)
            {
                return;
            }

            persistentEffects.Add(instance);
            persistentEffectMeshes.Add(mesh);
        }

        internal void ClearPersistentEffects()
        {
            for (int i = persistentEffects.Count - 1; i >= 0; i--)
            {
                if (persistentEffects[i] != null)
                {
                    UnityEngine.Object.Destroy(persistentEffects[i]);
                }
            }

            persistentEffects.Clear();

            for (int i = persistentEffectMeshes.Count - 1; i >= 0; i--)
            {
                if (persistentEffectMeshes[i] != null)
                {
                    UnityEngine.Object.Destroy(persistentEffectMeshes[i]);
                }
            }

            persistentEffectMeshes.Clear();
        }

        internal void DetachTileVisuals(Vector2Int coordinate, Transform targetParent, List<GameObject> output)
        {
            if (trackedBoard == null || targetParent == null || output == null || !trackedBoard.IsInside(coordinate))
            {
                return;
            }

            int index = coordinate.y * trackedBoard.Width + coordinate.x;

            if (index < 0 || index >= tileVisuals.Length)
            {
                return;
            }

            List<GameObject> instances = tileVisuals[index];

            if (instances == null)
            {
                return;
            }

            for (int i = 0; i < instances.Count; i++)
            {
                GameObject instance = instances[i];

                if (instance == null)
                {
                    continue;
                }

                spawner.ReleaseInstance(instance);
                instance.transform.SetParent(targetParent, true);
                output.Add(instance);
            }

            instances.Clear();
        }

        private void OnDestroy()
        {
            Clear();
        }

        private GameObject SpawnBridge(RaidTileBridge bridge, Vector3 worldPosition, float tileSize)
        {
            if (visualSet.BridgePrefab == null)
            {
                throw new InvalidOperationException("Raid Bridge Prefab이 연결되지 않았습니다.");
            }

            float scale = tileSize / RaidDungeonMetrics.TileSize;
            float heightOffset = -RaidDungeonMetrics.FoundationHeight * scale;
            Quaternion rotation = bridge == RaidTileBridge.Vertical ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;
            return spawner.SpawnArt(visualSet.BridgePrefab, worldPosition + Vector3.up * heightOffset, rotation, scale);
        }

        private GameObject SpawnSurface(GameObject prefab, RaidTile tile, Vector3 worldPosition, float tileSize)
        {
            if (tile.Surface == RaidTileSurface.Ground || tile.Surface == RaidTileSurface.HighGround)
            {
                float scale = tileSize / RaidDungeonMetrics.TileSize;
                Quaternion rotation = Quaternion.Euler(0f, (int)tile.SurfaceRotation * 90f, 0f);
                return spawner.SpawnArt(prefab, worldPosition, rotation, scale);
            }

            return spawner.SpawnTile(prefab, worldPosition, tileSize);
        }

        private GameObject SpawnBlock(RaidTile tile, Vector2Int coordinate, Vector3 worldPosition, float tileSize, int visualKey)
        {
            GameObject prefab = RaidTileVisualPicker.PickBlock(visualSet, tile.Block, coordinate, visualKey);

            if (prefab == null)
            {
                throw new InvalidOperationException($"Raid Block Prefab이 없습니다. Block: {tile.Block}");
            }

            float scale = tileSize / RaidDungeonMetrics.TileSize;
            float yaw = (int)tile.BlockRotation * 90f;
            return spawner.SpawnArt(prefab, worldPosition, Quaternion.Euler(0f, yaw, 0f), scale);
        }

        private void PrepareTracking(RaidBoard board)
        {
            trackedBoard = board;
            tileVisuals = new List<GameObject>[board.Count];
        }

        private void Register(Vector2Int coordinate, GameObject instance)
        {
            if (instance == null || trackedBoard == null || !trackedBoard.IsInside(coordinate))
            {
                return;
            }

            int index = coordinate.y * trackedBoard.Width + coordinate.x;
            List<GameObject> list = tileVisuals[index];

            if (list == null)
            {
                list = new List<GameObject>(4);
                tileVisuals[index] = list;
            }

            list.Add(instance);
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
