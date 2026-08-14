using System;
using EndlessGuard.Unit.Raid.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    internal sealed class RaidOuterRuinWall
    {
        private const int BaseLayers = 2;
        private const int WallGroupSize = 3;
        private const uint WallGroupSalt = 0x7C29A15Du;
        private const uint BrokenSalt = 0xE51D927Bu;

        private readonly RaidTileVisualSetSO visualSet;
        private readonly RaidVisualSpawner spawner;

        public RaidOuterRuinWall(RaidTileVisualSetSO visualSet, RaidVisualSpawner spawner)
        {
            if (visualSet == null)
            {
                throw new ArgumentNullException(nameof(visualSet));
            }

            if (spawner == null)
            {
                throw new ArgumentNullException(nameof(spawner));
            }

            if (visualSet.BoundaryWallPrefab == null || visualSet.BoundaryBrokenPrefab == null)
            {
                throw new InvalidOperationException("Raid Outer Ruin Wall Prefab이 연결되지 않았습니다.");
            }

            this.visualSet = visualSet;
            this.spawner = spawner;
        }

        public void Build(RaidBoard board, RaidOuterRuinPlan plan, float scale, int seed)
        {
            if (plan == null || plan.Count == 0)
            {
                return;
            }

            foreach (Vector2Int coordinate in plan.Tiles)
            {
                if (plan.IsCut(coordinate))
                {
                    continue;
                }

                TrySpawnEdge(board, plan, coordinate, Vector2Int.right, scale, seed);
                TrySpawnEdge(board, plan, coordinate, Vector2Int.left, scale, seed);
                TrySpawnEdge(board, plan, coordinate, Vector2Int.up, scale, seed);
                TrySpawnEdge(board, plan, coordinate, Vector2Int.down, scale, seed);
            }
        }

        private void TrySpawnEdge(RaidBoard board, RaidOuterRuinPlan plan, Vector2Int coordinate, Vector2Int outerDirection, float scale, int seed)
        {
            if (!IsExposedOuterEdge(board, plan, coordinate, outerDirection))
            {
                return;
            }

            Vector2Int tangentDirection = new Vector2Int(-outerDirection.y, outerDirection.x);

            if (!ShouldKeepWallGroup(coordinate, outerDirection, tangentDirection, seed))
            {
                return;
            }

            if (!TrySpawnBrokenPair(board, plan, coordinate, outerDirection, tangentDirection, scale, seed))
            {
                SpawnHalfWall(board, coordinate, outerDirection, scale);
            }
        }

        private bool ShouldKeepWallGroup(Vector2Int coordinate, Vector2Int outerDirection, Vector2Int tangentDirection, int seed)
        {
            if (visualSet.OuterRuinWallChance <= 0f)
            {
                return false;
            }

            Vector2Int groupAnchor = GetGroupAnchor(coordinate, tangentDirection);
            return RaidBoundaryRules.GetStable01(groupAnchor, outerDirection, seed, WallGroupSalt) < visualSet.OuterRuinWallChance;
        }

        private bool TrySpawnBrokenPair(RaidBoard board, RaidOuterRuinPlan plan, Vector2Int coordinate, Vector2Int outerDirection, Vector2Int tangentDirection, float scale, int seed)
        {
            if (visualSet.OuterRuinBrokenChance <= 0f)
            {
                return false;
            }

            Vector2Int pairDirection = outerDirection.x != 0 ? Vector2Int.up : Vector2Int.right;
            int axisValue = pairDirection.x != 0 ? coordinate.x : coordinate.y;
            Vector2Int pairStart = (axisValue & 1) == 0 ? coordinate : coordinate - pairDirection;
            Vector2Int secondCoordinate = pairStart + pairDirection;

            if (GetGroupAnchor(pairStart, tangentDirection) != GetGroupAnchor(secondCoordinate, tangentDirection))
            {
                return false;
            }

            if (!CanUseBrokenPair(board, plan, pairStart, secondCoordinate, outerDirection, seed))
            {
                return false;
            }

            if (coordinate == pairStart)
            {
                SpawnBrokenWall(board, pairStart, outerDirection, pairDirection, scale);
            }

            return true;
        }

        private bool CanUseBrokenPair(RaidBoard board, RaidOuterRuinPlan plan, Vector2Int first, Vector2Int second, Vector2Int outerDirection, int seed)
        {
            if (!plan.Contains(first) || !plan.Contains(second) || plan.IsCut(first) || plan.IsCut(second))
            {
                return false;
            }

            if (!IsExposedOuterEdge(board, plan, first, outerDirection) || !IsExposedOuterEdge(board, plan, second, outerDirection))
            {
                return false;
            }

            return RaidBoundaryRules.GetStable01(first, outerDirection, seed, BrokenSalt) < visualSet.OuterRuinBrokenChance;
        }

        private void SpawnHalfWall(RaidBoard board, Vector2Int coordinate, Vector2Int outerDirection, float scale)
        {
            float wallHeightOffset = -(RaidDungeonMetrics.FoundationHeight * BaseLayers + RaidDungeonMetrics.WallHeight) * scale;
            float wallCenterOffset = (RaidDungeonMetrics.FoundationHalfExtent - RaidDungeonMetrics.WallHalfThickness) * scale;
            Vector2Int tangentDirection = new Vector2Int(-outerDirection.y, outerDirection.x);
            Vector3 tileCenter = board.TileToWorld(coordinate, wallHeightOffset);
            Vector3 outerCenter = board.TileToWorld(coordinate + outerDirection, wallHeightOffset);
            Vector3 tangentCenter = board.TileToWorld(coordinate + tangentDirection, wallHeightOffset);
            Vector3 normal = outerCenter - tileCenter;
            Vector3 tangent = tangentCenter - tileCenter;
            normal.y = 0f;
            tangent.y = 0f;

            if (normal.sqrMagnitude <= Mathf.Epsilon || tangent.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            normal.Normalize();
            tangent.Normalize();
            float halfTile = board.TileSize * 0.5f;
            Vector3 wallStart = tileCenter + normal * wallCenterOffset - tangent * halfTile;
            Quaternion rotation = Quaternion.FromToRotation(Vector3.right, tangent);
            spawner.SpawnArt(visualSet.BoundaryWallPrefab, wallStart, rotation, scale);
        }

        private void SpawnBrokenWall(RaidBoard board, Vector2Int pairStart, Vector2Int outerDirection, Vector2Int pairDirection, float scale)
        {
            float wallHeightOffset = -(RaidDungeonMetrics.FoundationHeight * BaseLayers + RaidDungeonMetrics.WallHeight) * scale;
            float wallCenterOffset = (RaidDungeonMetrics.FoundationHalfExtent - RaidDungeonMetrics.WallHalfThickness) * scale;
            Vector2Int secondCoordinate = pairStart + pairDirection;
            Vector3 firstCenter = board.TileToWorld(pairStart, wallHeightOffset);
            Vector3 secondCenter = board.TileToWorld(secondCoordinate, wallHeightOffset);
            Vector3 outerCenter = board.TileToWorld(pairStart + outerDirection, wallHeightOffset);
            Vector3 normal = outerCenter - firstCenter;
            Vector3 tangent = secondCenter - firstCenter;
            normal.y = 0f;
            tangent.y = 0f;

            if (normal.sqrMagnitude <= Mathf.Epsilon || tangent.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            normal.Normalize();
            tangent.Normalize();
            Vector3 pairCenter = (firstCenter + secondCenter) * 0.5f + normal * wallCenterOffset;
            Quaternion rotation = Quaternion.FromToRotation(Vector3.right, tangent);
            spawner.SpawnArt(visualSet.BoundaryBrokenPrefab, pairCenter, rotation, scale);
        }

        private static bool IsExposedOuterEdge(RaidBoard board, RaidOuterRuinPlan plan, Vector2Int coordinate, Vector2Int outerDirection)
        {
            Vector2Int neighbor = coordinate + outerDirection;

            if (plan.Contains(neighbor))
            {
                return false;
            }

            return !board.IsInside(neighbor) || board.GetTile(neighbor).Surface == RaidTileSurface.Void;
        }

        private static Vector2Int GetGroupAnchor(Vector2Int coordinate, Vector2Int tangentDirection)
        {
            Vector2Int anchor = coordinate;

            if (tangentDirection.x != 0)
            {
                anchor.x = FloorDiv(coordinate.x, WallGroupSize) * WallGroupSize;
            }
            else
            {
                anchor.y = FloorDiv(coordinate.y, WallGroupSize) * WallGroupSize;
            }

            return anchor;
        }

        private static int FloorDiv(int value, int divisor)
        {
            return value >= 0 ? value / divisor : -((-value + divisor - 1) / divisor);
        }
    }
}
