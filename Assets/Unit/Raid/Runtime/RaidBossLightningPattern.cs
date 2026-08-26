using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Raid.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    internal static class RaidBossLightningPattern
    {
        private static readonly Comparison<Vector3> CompareX = ComparePositionX;
        private static readonly Comparison<Vector3> CompareZ = ComparePositionZ;

        public static void Build(RaidBoard board, RaidBattleConfigSO config, int desiredCount, float yOffset, List<Vector3> candidates, List<Vector3> results)
        {
            candidates.Clear();
            results.Clear();

            if (board == null || desiredCount <= 0)
            {
                return;
            }

            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    Vector2Int coordinate = new Vector2Int(x, y);
                    if (!board.TryGetTile(coordinate, out RaidTile tile) || (tile.Surface != RaidTileSurface.Ground && tile.Surface != RaidTileSurface.HighGround))
                    {
                        continue;
                    }

                    float heightOffset = ResolveHeightOffset(config, tile.Surface);
                    candidates.Add(board.TileToWorld(coordinate, heightOffset) + Vector3.up * yOffset);
                }
            }

            if (candidates.Count == 0)
            {
                return;
            }

            candidates.Sort(UnityEngine.Random.value >= 0.5f ? CompareX : CompareZ);
            int strikeCount = Mathf.Min(desiredCount, candidates.Count);

            for (int i = 0; i < strikeCount; i++)
            {
                int start = Mathf.FloorToInt((float)i * candidates.Count / strikeCount);
                int endExclusive = Mathf.FloorToInt((float)(i + 1) * candidates.Count / strikeCount);
                endExclusive = Mathf.Max(start + 1, endExclusive);
                int chosenIndex = UnityEngine.Random.Range(start, Mathf.Min(endExclusive, candidates.Count));
                results.Add(candidates[chosenIndex]);
            }

            if (UnityEngine.Random.value < 0.5f)
            {
                results.Reverse();
            }
        }

        private static float ResolveHeightOffset(RaidBattleConfigSO config, RaidTileSurface surface)
        {
            if (surface == RaidTileSurface.HighGround)
            {
                return config != null ? config.HighGroundDeployHeight : 0.82f;
            }

            return config != null ? config.GroundDeployHeight : 0.08f;
        }

        private static int ComparePositionX(Vector3 a, Vector3 b)
        {
            return a.x.CompareTo(b.x);
        }

        private static int ComparePositionZ(Vector3 a, Vector3 b)
        {
            return a.z.CompareTo(b.z);
        }
    }
}
