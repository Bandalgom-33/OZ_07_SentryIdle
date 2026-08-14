using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Raid.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    internal sealed class RaidOuterRuinView
    {
        private const int BaseLayers = 2;
        private const uint FloorSalt = 0x4F1B2A67u;

        private readonly RaidTileVisualSetSO visualSet;
        private readonly RaidVisualSpawner spawner;
        private readonly RaidOuterRuinPlanner planner = new RaidOuterRuinPlanner();

        public RaidOuterRuinView(RaidTileVisualSetSO visualSet, RaidVisualSpawner spawner)
        {
            if (visualSet == null)
            {
                throw new ArgumentNullException(nameof(visualSet));
            }

            if (spawner == null)
            {
                throw new ArgumentNullException(nameof(spawner));
            }

            this.visualSet = visualSet;
            this.spawner = spawner;
        }

        public void Build(RaidBoard board, int seed)
        {
            if (visualSet.OuterRuinChance <= 0f)
            {
                return;
            }

            Validate();
            RaidOuterRuinPlan plan = planner.Build(board, seed, visualSet.OuterRuinChance);

            if (plan.Count == 0)
            {
                return;
            }

            float scale = board.TileSize / RaidDungeonMetrics.TileSize;
            SpawnFloorAndFoundation(board, plan, scale, seed);

            RaidOuterRuinWall outerWall = new RaidOuterRuinWall(visualSet, spawner);
            outerWall.Build(board, plan, scale, seed);

            RaidOuterRuinRail outerRail = new RaidOuterRuinRail(visualSet, spawner);
            outerRail.Build(board, plan, scale, seed);
        }

        private void Validate()
        {
            if (visualSet.OuterRuinBasePrefab == null)
            {
                throw new InvalidOperationException("Raid Outer Ruin Base Prefab이 연결되지 않았습니다.");
            }

            if (visualSet.OuterRuinFloorPrefabs == null || visualSet.OuterRuinFloorPrefabs.Count == 0)
            {
                throw new InvalidOperationException("Raid Outer Ruin Floor Prefabs가 비어 있습니다.");
            }

            if (visualSet.OuterRuinCutFloorPrefab == null || visualSet.OuterRuinCutBasePrefab == null)
            {
                throw new InvalidOperationException("Raid Outer Ruin Cut Prefab이 연결되지 않았습니다.");
            }
        }

        private void SpawnFloorAndFoundation(RaidBoard board, RaidOuterRuinPlan plan, float scale, int seed)
        {
            foreach (Vector2Int coordinate in plan.Tiles)
            {
                if (plan.TryGetCutYaw(coordinate, out float yaw))
                {
                    SpawnCut(board, coordinate, yaw, scale);
                }
                else
                {
                    SpawnRegular(board, coordinate, scale, seed);
                }
            }
        }

        private void SpawnRegular(RaidBoard board, Vector2Int coordinate, float scale, int seed)
        {
            GameObject floorPrefab = PickFloor(coordinate, seed);
            spawner.SpawnArt(floorPrefab, board.TileToWorld(coordinate), Quaternion.identity, scale);

            for (int layer = 1; layer <= BaseLayers; layer++)
            {
                float heightOffset = -RaidDungeonMetrics.FoundationHeight * scale * layer;
                spawner.SpawnArt(visualSet.OuterRuinBasePrefab, board.TileToWorld(coordinate, heightOffset), Quaternion.identity, scale);
            }
        }

        private void SpawnCut(RaidBoard board, Vector2Int coordinate, float yaw, float scale)
        {
            Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
            spawner.SpawnArt(visualSet.OuterRuinCutFloorPrefab, board.TileToWorld(coordinate), rotation, scale);

            for (int layer = 1; layer <= BaseLayers; layer++)
            {
                float heightOffset = -RaidDungeonMetrics.FoundationHeight * scale * layer;
                spawner.SpawnArt(visualSet.OuterRuinCutBasePrefab, board.TileToWorld(coordinate, heightOffset), rotation, scale);
            }
        }

        private GameObject PickFloor(Vector2Int coordinate, int seed)
        {
            IReadOnlyList<GameObject> prefabs = visualSet.OuterRuinFloorPrefabs;
            float value = RaidBoundaryRules.GetStable01(coordinate, Vector2Int.zero, seed, FloorSalt);
            int index = Mathf.Clamp(Mathf.FloorToInt(value * prefabs.Count), 0, prefabs.Count - 1);
            return prefabs[index];
        }
    }
}
