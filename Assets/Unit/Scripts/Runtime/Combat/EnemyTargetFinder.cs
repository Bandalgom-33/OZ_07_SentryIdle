using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    internal static class EnemyTargetFinder
    {
        private const float PriorityTolerance = 0.0001f;

        internal static bool TryFind(EnemyRuntimeState attacker, out UnitRuntimeState target)
        {
            target = null;

            if (!CanSearch(attacker))
            {
                return false;
            }

            switch (attacker.DataLink.EnemyData.AttackRule)
            {
                case EnemyAttackRule.BlockedOnly:
                    return TryFindBlocker(attacker, out target);

                case EnemyAttackRule.InRange:
                    return TryFindInRange(attacker, out target);

                default:
                    return false;
            }
        }

        private static bool TryFindBlocker(EnemyRuntimeState attacker, out UnitRuntimeState target)
        {
            target = null;
            EnemyBlock block = attacker.Block;

            if (block == null || !block.IsBlocked || block.Blocker == null)
            {
                return false;
            }

            UnitRuntimeState blocker = block.Blocker.State;

            if (!IsValid(blocker))
            {
                return false;
            }

            target = blocker;
            return true;
        }

        private static bool TryFindInRange(EnemyRuntimeState attacker, out UnitRuntimeState target)
        {
            target = null;

            AttackSettings attackSettings = attacker.DataLink.EnemyData.AttackSettings;
            BasicAttackRangeData rangeData = attackSettings.BasicAttackRange;

            if (rangeData == null)
            {
                return false;
            }

            Vector2Int attackerTile = attacker.GridPosition.TileCoordinate;
            GridFacingDirection currentFacing = attacker.GridPosition.FacingDirection;
            GridFacingDirection bestFacing = currentFacing;
            float bestWorldDistance = float.MaxValue;
            int bestInstanceId = int.MaxValue;
            IReadOnlyList<Vector2Int> attackTiles = rangeData.AttackTiles;

            if (attackSettings.RangeRotationMode == AttackRangeRotationMode.Fixed)
            {
                for (int i = 0; i < attackTiles.Count; i++)
                {
                    Vector2Int worldTile = attackerTile + RotatePatternTile(attackTiles[i], currentFacing);
                    EvaluateTile(attacker, attackSettings, currentFacing, worldTile, ref target, ref bestFacing, ref bestWorldDistance, ref bestInstanceId);
                }
            }
            else
            {
                EvaluateFacingTiles(attacker, attackSettings, attackTiles, attackerTile, currentFacing, GridFacingDirection.North, ref target, ref bestFacing, ref bestWorldDistance, ref bestInstanceId);
                EvaluateFacingTiles(attacker, attackSettings, attackTiles, attackerTile, currentFacing, GridFacingDirection.East, ref target, ref bestFacing, ref bestWorldDistance, ref bestInstanceId);
                EvaluateFacingTiles(attacker, attackSettings, attackTiles, attackerTile, currentFacing, GridFacingDirection.South, ref target, ref bestFacing, ref bestWorldDistance, ref bestInstanceId);
                EvaluateFacingTiles(attacker, attackSettings, attackTiles, attackerTile, currentFacing, GridFacingDirection.West, ref target, ref bestFacing, ref bestWorldDistance, ref bestInstanceId);
            }

            if (target == null)
            {
                return false;
            }

            if (attackSettings.RangeRotationMode == AttackRangeRotationMode.FollowFacing)
            {
                attacker.GridPosition.SetFacingDirection(bestFacing);
            }

            return true;
        }

        private static void EvaluateFacingTiles(EnemyRuntimeState attacker, AttackSettings attackSettings, IReadOnlyList<Vector2Int> attackTiles, Vector2Int attackerTile, GridFacingDirection currentFacing, GridFacingDirection facing, ref UnitRuntimeState target, ref GridFacingDirection bestFacing, ref float bestWorldDistance, ref int bestInstanceId)
        {
            for (int i = 0; i < attackTiles.Count; i++)
            {
                Vector2Int relativeWorldTile = RotatePatternTile(attackTiles[i], facing);

                if (GetFacingDirection(relativeWorldTile, currentFacing) != facing)
                {
                    continue;
                }

                Vector2Int worldTile = attackerTile + relativeWorldTile;
                EvaluateTile(attacker, attackSettings, currentFacing, worldTile, ref target, ref bestFacing, ref bestWorldDistance, ref bestInstanceId);
            }
        }

        private static void EvaluateTile(EnemyRuntimeState attacker, AttackSettings attackSettings, GridFacingDirection currentFacing, Vector2Int tile, ref UnitRuntimeState target, ref GridFacingDirection bestFacing, ref float bestWorldDistance, ref int bestInstanceId)
        {
            if (!CombatRegistry.TryGetUnitsAt(tile, out HashSet<UnitRuntimeState> tileUnits))
            {
                return;
            }

            foreach (UnitRuntimeState candidate in tileUnits)
            {
                if (!IsValid(candidate))
                {
                    continue;
                }

                if (!BasicAttackContextFactory.TryCreate(attacker, candidate, out BasicAttackContext baseContext))
                {
                    continue;
                }

                GridFacingDirection candidateFacing = attackSettings.RangeRotationMode == AttackRangeRotationMode.FollowFacing ? GetFacingDirection(baseContext.RelativeTargetTile, currentFacing) : currentFacing;
                BasicAttackContext candidateContext = CreateFacingContext(baseContext, candidateFacing);

                if (!BasicAttackRangeEvaluator.TryEvaluate(attackSettings, candidateContext, out _, out _))
                {
                    continue;
                }

                float worldDistance = candidateContext.HorizontalWorldDistance;
                int instanceId = candidate.GetInstanceID();

                bool preferFarthest = attacker.Passives != null && attacker.Passives.PreferFarthestTarget;

                if (!IsBetterTarget(target, worldDistance, instanceId, bestWorldDistance, bestInstanceId, preferFarthest))
                {
                    continue;
                }

                target = candidate;
                bestFacing = candidateFacing;
                bestWorldDistance = worldDistance;
                bestInstanceId = instanceId;
            }
        }

        private static bool CanSearch(EnemyRuntimeState attacker)
        {
            return attacker != null && attacker.IsInitialized && attacker.Health != null && !attacker.Health.IsDead && attacker.GridPosition != null && attacker.GridPosition.IsInitialized && attacker.DataLink != null && attacker.DataLink.HasData && attacker.DataLink.EnemyData.AttackSettings != null;
        }

        private static bool IsValid(UnitRuntimeState target)
        {
            return target != null && target.IsInitialized && target.Health != null && !target.Health.IsDead && target.GridPosition != null && target.GridPosition.IsInitialized && target.DataLink != null && target.DataLink.HasData;
        }

        private static Vector2Int RotatePatternTile(Vector2Int patternTile, GridFacingDirection facing)
        {
            switch (facing)
            {
                case GridFacingDirection.East:
                    return new Vector2Int(patternTile.y, -patternTile.x);

                case GridFacingDirection.South:
                    return new Vector2Int(-patternTile.x, -patternTile.y);

                case GridFacingDirection.West:
                    return new Vector2Int(-patternTile.y, patternTile.x);

                default:
                    return patternTile;
            }
        }

        private static GridFacingDirection GetFacingDirection(Vector2Int relativeTargetTile, GridFacingDirection fallback)
        {
            if (relativeTargetTile == Vector2Int.zero)
            {
                return fallback;
            }

            int horizontalDistance = Mathf.Abs(relativeTargetTile.x);
            int verticalDistance = Mathf.Abs(relativeTargetTile.y);

            if (horizontalDistance >= verticalDistance)
            {
                return relativeTargetTile.x > 0 ? GridFacingDirection.East : GridFacingDirection.West;
            }

            return relativeTargetTile.y > 0 ? GridFacingDirection.North : GridFacingDirection.South;
        }

        private static BasicAttackContext CreateFacingContext(BasicAttackContext source, GridFacingDirection facing)
        {
            return new BasicAttackContext(source.RelativeTargetTile, source.HorizontalWorldDistance, facing, source.TargetLayer);
        }

        private static bool IsBetterTarget(UnitRuntimeState currentTarget, float worldDistance, int instanceId, float bestWorldDistance, int bestInstanceId, bool preferFarthest)
        {
            if (currentTarget == null)
            {
                return true;
            }

            if (preferFarthest)
            {
                if (worldDistance > bestWorldDistance + PriorityTolerance)
                {
                    return true;
                }

                if (worldDistance < bestWorldDistance - PriorityTolerance)
                {
                    return false;
                }
            }
            else
            {
                if (worldDistance < bestWorldDistance - PriorityTolerance)
                {
                    return true;
                }

                if (worldDistance > bestWorldDistance + PriorityTolerance)
                {
                    return false;
                }
            }

            return instanceId < bestInstanceId;
        }
    }
}