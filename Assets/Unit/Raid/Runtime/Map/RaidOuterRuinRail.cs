using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Raid.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    internal sealed class RaidOuterRuinRail
    {
        private const int MinSegmentLength = 1;
        private const int MaxSegmentLength = 4;
        private const uint RailSalt = 0xD47A39B1u;
        private const uint SelectionSalt = 0xB89F24C3u;
        private const uint LengthSalt = 0x6A0F3D25u;
        private const uint OffsetSalt = 0xBE519C73u;
        private const uint TailSalt = 0x63D8E241u;

        private readonly struct RailRun
        {
            public readonly Vector2Int Start;
            public readonly Vector2Int Direction;
            public readonly Vector2Int Tangent;
            public readonly int Length;

            public RailRun(Vector2Int start, Vector2Int direction, Vector2Int tangent, int length)
            {
                Start = start;
                Direction = direction;
                Tangent = tangent;
                Length = length;
            }
        }

        private readonly RaidTileVisualSetSO visualSet;
        private readonly RaidVisualSpawner spawner;

        public RaidOuterRuinRail(RaidTileVisualSetSO visualSet, RaidVisualSpawner spawner)
        {
            if (visualSet == null)
            {
                throw new ArgumentNullException(nameof(visualSet));
            }

            if (spawner == null)
            {
                throw new ArgumentNullException(nameof(spawner));
            }

            if (visualSet.OuterRuinRailHalfPrefab == null || visualSet.OuterRuinRailLongPrefab == null || visualSet.OuterRuinRailEndPrefab == null)
            {
                throw new InvalidOperationException("Raid Outer Ruin Rail Prefab이 연결되지 않았습니다.");
            }

            this.visualSet = visualSet;
            this.spawner = spawner;
        }

        public void Build(RaidBoard board, RaidOuterRuinPlan plan, float scale, int seed)
        {
            if (visualSet.OuterRuinRailChance <= 0f || plan == null || plan.Count == 0)
            {
                return;
            }

            List<RailRun> runs = BuildRuns(board, plan);
            int runIndex = FindBestRun(runs, seed);

            if (runIndex < 0)
            {
                return;
            }

            RailRun run = runs[runIndex];

            if (RaidBoundaryRules.GetStable01(run.Start, run.Direction, seed, RailSalt) >= visualSet.OuterRuinRailChance)
            {
                return;
            }

            int segmentLength = GetSegmentLength(run, seed);
            int segmentOffset = GetSegmentOffset(run, segmentLength, seed);
            SpawnSegment(board, run.Start + run.Tangent * segmentOffset, run.Direction, run.Tangent, segmentLength, scale, seed);
        }

        private static List<RailRun> BuildRuns(RaidBoard board, RaidOuterRuinPlan plan)
        {
            List<RailRun> runs = new List<RailRun>();
            AddRuns(board, plan, Vector2Int.right, runs);
            AddRuns(board, plan, Vector2Int.left, runs);
            AddRuns(board, plan, Vector2Int.up, runs);
            AddRuns(board, plan, Vector2Int.down, runs);
            return runs;
        }

        private static void AddRuns(RaidBoard board, RaidOuterRuinPlan plan, Vector2Int direction, List<RailRun> runs)
        {
            Vector2Int tangent = new Vector2Int(-direction.y, direction.x);

            foreach (Vector2Int coordinate in plan.Tiles)
            {
                if (!IsCandidate(board, plan, coordinate, direction) || IsCandidate(board, plan, coordinate - tangent, direction))
                {
                    continue;
                }

                int length = 1;

                while (IsCandidate(board, plan, coordinate + tangent * length, direction))
                {
                    length++;
                }

                runs.Add(new RailRun(coordinate, direction, tangent, length));
            }
        }

        private static bool IsCandidate(RaidBoard board, RaidOuterRuinPlan plan, Vector2Int coordinate, Vector2Int direction)
        {
            if (!plan.Contains(coordinate) || plan.IsCut(coordinate))
            {
                return false;
            }

            if (!IsExposedEdge(board, plan, coordinate, direction))
            {
                return false;
            }

            int exposedCount = 0;
            exposedCount += IsExposedEdge(board, plan, coordinate, Vector2Int.right) ? 1 : 0;
            exposedCount += IsExposedEdge(board, plan, coordinate, Vector2Int.left) ? 1 : 0;
            exposedCount += IsExposedEdge(board, plan, coordinate, Vector2Int.up) ? 1 : 0;
            exposedCount += IsExposedEdge(board, plan, coordinate, Vector2Int.down) ? 1 : 0;
            return exposedCount == 1;
        }

        private static int FindBestRun(List<RailRun> runs, int seed)
        {
            int bestIndex = -1;
            float bestScore = float.MinValue;

            for (int i = 0; i < runs.Count; i++)
            {
                RailRun run = runs[i];

                if (run.Length < MinSegmentLength)
                {
                    continue;
                }

                float randomScore = RaidBoundaryRules.GetStable01(run.Start, run.Direction, seed, SelectionSalt);
                float score = run.Length * 2f + randomScore;

                if (score > bestScore || Mathf.Approximately(score, bestScore) && IsBefore(run, runs[bestIndex]))
                {
                    bestScore = score;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        private static bool IsBefore(RailRun candidate, RailRun current)
        {
            if (candidate.Start.y != current.Start.y)
            {
                return candidate.Start.y < current.Start.y;
            }

            if (candidate.Start.x != current.Start.x)
            {
                return candidate.Start.x < current.Start.x;
            }

            if (candidate.Direction.y != current.Direction.y)
            {
                return candidate.Direction.y < current.Direction.y;
            }

            return candidate.Direction.x < current.Direction.x;
        }

        private static int GetSegmentLength(RailRun run, int seed)
        {
            int maxLength = Mathf.Min(MaxSegmentLength, run.Length);

            if (maxLength <= MinSegmentLength)
            {
                return maxLength;
            }

            int minLength = maxLength >= 2 ? 2 : 1;
            float value = RaidBoundaryRules.GetStable01(run.Start, run.Direction, seed, LengthSalt);
            int range = maxLength - minLength + 1;
            return minLength + Mathf.Clamp(Mathf.FloorToInt(value * range), 0, range - 1);
        }

        private static int GetSegmentOffset(RailRun run, int segmentLength, int seed)
        {
            int maxOffset = run.Length - segmentLength;

            if (maxOffset <= 0)
            {
                return 0;
            }

            float value = RaidBoundaryRules.GetStable01(run.Start, run.Direction, seed, OffsetSalt);
            return Mathf.Clamp(Mathf.FloorToInt(value * (maxOffset + 1)), 0, maxOffset);
        }

        private void SpawnSegment(RaidBoard board, Vector2Int startCoordinate, Vector2Int outerDirection, Vector2Int tangentDirection, int length, float scale, int seed)
        {
            Vector3 tileCenter = board.TileToWorld(startCoordinate);
            Vector3 outerCenter = board.TileToWorld(startCoordinate + outerDirection);
            Vector3 tangentCenter = board.TileToWorld(startCoordinate + tangentDirection);
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

            float railCenterOffset = (RaidDungeonMetrics.FoundationHalfExtent - RaidDungeonMetrics.RailHalfThickness) * scale;
            float halfTile = board.TileSize * 0.5f;
            Vector3 segmentStart = tileCenter + normal * railCenterOffset - tangent * halfTile;
            Quaternion rotation = Quaternion.FromToRotation(Vector3.right, tangent);
            int remaining = length;
            float cursor = 0f;

            while (remaining >= 2)
            {
                Vector3 pairCenter = segmentStart + tangent * (cursor + board.TileSize);
                spawner.SpawnArt(visualSet.OuterRuinRailLongPrefab, pairCenter, rotation, scale);
                cursor += board.TileSize * 2f;
                remaining -= 2;
            }

            if (remaining == 1)
            {
                Vector3 tailStart = segmentStart + tangent * cursor;
                float tailValue = RaidBoundaryRules.GetStable01(startCoordinate, outerDirection, seed, TailSalt);
                GameObject tailPrefab = tailValue < 0.65f ? visualSet.OuterRuinRailEndPrefab : visualSet.OuterRuinRailHalfPrefab;
                spawner.SpawnArt(tailPrefab, tailStart, rotation, scale);
            }
        }

        private static bool IsExposedEdge(RaidBoard board, RaidOuterRuinPlan plan, Vector2Int coordinate, Vector2Int direction)
        {
            Vector2Int neighbor = coordinate + direction;

            if (plan.Contains(neighbor))
            {
                return false;
            }

            return !board.IsInside(neighbor) || board.GetTile(neighbor).Surface == RaidTileSurface.Void;
        }
    }
}