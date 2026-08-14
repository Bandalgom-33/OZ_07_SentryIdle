using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Raid.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    internal readonly struct RaidOuterRuinRun
    {
        public readonly Vector2Int Start;
        public readonly Vector2Int Direction;
        public readonly Vector2Int Tangent;
        public readonly int Length;

        public RaidOuterRuinRun(Vector2Int start, Vector2Int direction, Vector2Int tangent, int length)
        {
            Start = start;
            Direction = direction;
            Tangent = tangent;
            Length = length;
        }

        public Vector2 Center
        {
            get
            {
                Vector2Int end = Start + Tangent * (Length - 1);
                return new Vector2((Start.x + end.x) * 0.5f, (Start.y + end.y) * 0.5f);
            }
        }
    }

    internal sealed class RaidOuterRuinRunFinder
    {
        private const int MinZoneLength = 2;
        private const float MinZoneSeparation = 5f;
        private const uint RunScoreSalt = 0x31D4A8C7u;

        public List<RaidOuterRuinRun> Build(RaidBoard board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            List<RaidOuterRuinRun> runs = new List<RaidOuterRuinRun>();
            AddRuns(board, Vector2Int.right, runs);
            AddRuns(board, Vector2Int.left, runs);
            AddRuns(board, Vector2Int.up, runs);
            AddRuns(board, Vector2Int.down, runs);
            return runs;
        }

        public int FindBest(IReadOnlyList<RaidOuterRuinRun> runs, int seed, int excludedIndex, Vector2? excludedCenter)
        {
            int bestIndex = -1;
            float bestScore = float.MinValue;

            for (int i = 0; i < runs.Count; i++)
            {
                if (i == excludedIndex)
                {
                    continue;
                }

                RaidOuterRuinRun run = runs[i];

                if (excludedCenter.HasValue && Vector2.Distance(run.Center, excludedCenter.Value) < MinZoneSeparation)
                {
                    continue;
                }

                float randomScore = RaidBoundaryRules.GetStable01(run.Start, run.Direction, seed, RunScoreSalt);
                float score = run.Length * 2f + randomScore * 2f;

                if (run.Length < MinZoneLength)
                {
                    score -= 2f;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        private static void AddRuns(RaidBoard board, Vector2Int direction, List<RaidOuterRuinRun> runs)
        {
            Vector2Int tangent = new Vector2Int(-direction.y, direction.x);

            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    Vector2Int coordinate = new Vector2Int(x, y);

                    if (!IsExteriorEdge(board, coordinate, direction) || IsExteriorEdge(board, coordinate - tangent, direction))
                    {
                        continue;
                    }

                    int length = 1;

                    while (IsExteriorEdge(board, coordinate + tangent * length, direction))
                    {
                        length++;
                    }

                    runs.Add(new RaidOuterRuinRun(coordinate, direction, tangent, length));
                }
            }
        }

        private static bool IsExteriorEdge(RaidBoard board, Vector2Int boundaryCoordinate, Vector2Int direction)
        {
            if (!board.IsInside(boundaryCoordinate))
            {
                return false;
            }

            RaidTile tile = board.GetTile(boundaryCoordinate);

            if (tile.Surface != RaidTileSurface.Ground && tile.Surface != RaidTileSurface.HighGround || tile.IsBridge)
            {
                return false;
            }

            Vector2Int outside = boundaryCoordinate + direction;

            while (board.IsInside(outside))
            {
                if (board.GetTile(outside).Surface != RaidTileSurface.Void)
                {
                    return false;
                }

                outside += direction;
            }

            return true;
        }
    }
}
