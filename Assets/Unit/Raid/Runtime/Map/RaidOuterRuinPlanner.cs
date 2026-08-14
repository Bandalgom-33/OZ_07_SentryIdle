using System.Collections.Generic;
using EndlessGuard.Unit.Raid.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    internal sealed class RaidOuterRuinPlanner
    {
        private const int MaxZoneLength = 4;
        private const int MaxDepth = 2;
        private const float CutEndChance = 0.55f;
        private const uint SecondarySalt = 0x6F24C19Bu;
        private const uint LengthSalt = 0xA51E639Du;
        private const uint OffsetSalt = 0x4B92D73Fu;
        private const uint DepthSalt = 0xB3D1A527u;
        private const uint CutSalt = 0x68E31DA4u;
        private const uint CutSideSalt = 0xC4519B73u;

        private readonly RaidOuterRuinRunFinder runFinder = new RaidOuterRuinRunFinder();

        public RaidOuterRuinPlan Build(RaidBoard board, int seed, float secondaryChance)
        {
            List<RaidOuterRuinRun> runs = runFinder.Build(board);
            RaidOuterRuinPlan plan = new RaidOuterRuinPlan();

            if (runs.Count == 0)
            {
                return plan;
            }

            int primaryIndex = runFinder.FindBest(runs, seed, -1, null);

            if (primaryIndex < 0)
            {
                return plan;
            }

            AddRun(board, plan, runs[primaryIndex], seed);

            if (RaidBoundaryRules.GetStable01(runs[primaryIndex].Start, runs[primaryIndex].Direction, seed, SecondarySalt) < secondaryChance)
            {
                int secondaryIndex = runFinder.FindBest(runs, seed, primaryIndex, runs[primaryIndex].Center);

                if (secondaryIndex >= 0)
                {
                    AddRun(board, plan, runs[secondaryIndex], seed);
                }
            }

            return plan;
        }

        private static void AddRun(RaidBoard board, RaidOuterRuinPlan plan, RaidOuterRuinRun run, int seed)
        {
            int zoneLength = GetZoneLength(run, seed);
            int maxOffset = Mathf.Max(0, run.Length - zoneLength);
            int startOffset = GetZoneOffset(run, maxOffset, seed);

            for (int index = 0; index < zoneLength; index++)
            {
                Vector2Int boundaryCoordinate = run.Start + run.Tangent * (startOffset + index);
                int requestedDepth = GetRequestedDepth(boundaryCoordinate, run.Direction, index, zoneLength, seed);
                int availableDepth = GetAvailableDepth(board, boundaryCoordinate, run.Direction, requestedDepth);

                for (int depth = 1; depth <= availableDepth; depth++)
                {
                    Vector2Int outsideCoordinate = boundaryCoordinate + run.Direction * depth;
                    bool isEnd = depth == availableDepth;

                    if (isEnd && RaidBoundaryRules.GetStable01(outsideCoordinate, run.Direction, seed, CutSalt) < CutEndChance)
                    {
                        plan.AddCut(outsideCoordinate, GetCutYaw(outsideCoordinate, run.Direction, seed));
                    }
                    else
                    {
                        plan.AddRegular(outsideCoordinate);
                    }
                }
            }
        }

        private static int GetZoneLength(RaidOuterRuinRun run, int seed)
        {
            int maxLength = Mathf.Min(MaxZoneLength, run.Length);
            int minLength = Mathf.Min(2, maxLength);

            if (maxLength <= minLength)
            {
                return maxLength;
            }

            float value = RaidBoundaryRules.GetStable01(run.Start, run.Direction, seed, LengthSalt);
            int range = maxLength - minLength + 1;
            return minLength + Mathf.Clamp(Mathf.FloorToInt(value * range), 0, range - 1);
        }

        private static int GetZoneOffset(RaidOuterRuinRun run, int maxOffset, int seed)
        {
            if (maxOffset <= 0)
            {
                return 0;
            }

            float value = RaidBoundaryRules.GetStable01(run.Start, run.Direction, seed, OffsetSalt);
            return Mathf.Clamp(Mathf.FloorToInt(value * (maxOffset + 1)), 0, maxOffset);
        }

        private static int GetRequestedDepth(Vector2Int coordinate, Vector2Int direction, int zoneIndex, int zoneLength, int seed)
        {
            if (zoneLength >= 2 && zoneIndex == zoneLength / 2)
            {
                return MaxDepth;
            }

            float value = RaidBoundaryRules.GetStable01(coordinate, direction, seed, DepthSalt);
            return value < 0.45f ? MaxDepth : 1;
        }

        private static int GetAvailableDepth(RaidBoard board, Vector2Int coordinate, Vector2Int direction, int requestedDepth)
        {
            int depth = 0;

            for (int step = 1; step <= requestedDepth; step++)
            {
                Vector2Int outside = coordinate + direction * step;

                if (board.IsInside(outside) && board.GetTile(outside).Surface != RaidTileSurface.Void)
                {
                    break;
                }

                depth++;
            }

            return depth;
        }

        private static float GetCutYaw(Vector2Int coordinate, Vector2Int direction, int seed)
        {
            Vector2Int tangent = new Vector2Int(-direction.y, direction.x);
            bool positive = RaidBoundaryRules.GetStable01(coordinate, direction, seed, CutSideSalt) < 0.5f;
            Vector2Int cutDirection = direction + (positive ? tangent : -tangent);

            if (cutDirection.x < 0 && cutDirection.y > 0)
            {
                return 0f;
            }

            if (cutDirection.x > 0 && cutDirection.y > 0)
            {
                return 90f;
            }

            if (cutDirection.x > 0 && cutDirection.y < 0)
            {
                return 180f;
            }

            return -90f;
        }
    }
}
