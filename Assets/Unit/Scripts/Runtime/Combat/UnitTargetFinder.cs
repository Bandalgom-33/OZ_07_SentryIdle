using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    internal static class UnitTargetFinder
    {
        private const float PriorityTolerance = 0.0001f;

        internal static bool TryFind(UnitRuntimeState attacker, out EnemyRuntimeState target)
        {
            target = null;

            if (!CanSearch(attacker))
            {
                return false;
            }

            AttackSettings attackSettings = attacker.DataLink.UnitData.AttackSettings;
            BasicAttackRangeData rangeData = attackSettings.BasicAttackRange;

            if (rangeData == null)
            {
                return false;
            }

            Vector2Int attackerTile = attacker.GridPosition.TileCoordinate;
            GridFacingDirection currentFacing = attacker.GridPosition.FacingDirection;
            GridFacingDirection bestFacing = currentFacing;
            float bestRemainingDistance = float.MaxValue;
            float bestWorldDistance = float.MaxValue;
            int bestInstanceId = int.MaxValue;
            IReadOnlyList<Vector2Int> attackTiles = rangeData.AttackTiles;

            if (attackSettings.RangeRotationMode == AttackRangeRotationMode.Fixed)
            {
                for (int i = 0; i < attackTiles.Count; i++)
                {
                    Vector2Int worldTile = attackerTile + RotatePatternTile(attackTiles[i], currentFacing);
                    EvaluateTile(attacker, attackSettings, currentFacing, worldTile, ref target, ref bestFacing, ref bestRemainingDistance, ref bestWorldDistance, ref bestInstanceId);
                }
            }
            else
            {
                EvaluateFacingTiles(attacker, attackSettings, attackTiles, attackerTile, currentFacing, GridFacingDirection.North, ref target, ref bestFacing, ref bestRemainingDistance, ref bestWorldDistance, ref bestInstanceId);
                EvaluateFacingTiles(attacker, attackSettings, attackTiles, attackerTile, currentFacing, GridFacingDirection.East, ref target, ref bestFacing, ref bestRemainingDistance, ref bestWorldDistance, ref bestInstanceId);
                EvaluateFacingTiles(attacker, attackSettings, attackTiles, attackerTile, currentFacing, GridFacingDirection.South, ref target, ref bestFacing, ref bestRemainingDistance, ref bestWorldDistance, ref bestInstanceId);
                EvaluateFacingTiles(attacker, attackSettings, attackTiles, attackerTile, currentFacing, GridFacingDirection.West, ref target, ref bestFacing, ref bestRemainingDistance, ref bestWorldDistance, ref bestInstanceId);
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

        internal static int FindTargets(UnitRuntimeState attacker, int maxTargetCount, List<EnemyRuntimeState> targets)
        {
            if (targets == null)
            {
                return 0;
            }

            targets.Clear();

            if (!TryFind(attacker, out EnemyRuntimeState primaryTarget))
            {
                return 0;
            }

            targets.Add(primaryTarget);
            int targetLimit = Mathf.Max(1, maxTargetCount);

            while (targets.Count < targetLimit && TryFindNextTargetInCurrentRange(attacker, targets, out EnemyRuntimeState nextTarget))
            {
                targets.Add(nextTarget);
            }

            return targets.Count;
        }

        private static bool TryFindNextTargetInCurrentRange(UnitRuntimeState attacker, List<EnemyRuntimeState> selectedTargets, out EnemyRuntimeState target)
        {
            target = null;

            if (!CanSearch(attacker) || selectedTargets == null)
            {
                return false;
            }

            AttackSettings attackSettings = attacker.DataLink.UnitData.AttackSettings;
            BasicAttackRangeData rangeData = attackSettings.BasicAttackRange;

            if (rangeData == null)
            {
                return false;
            }

            Vector2Int attackerTile = attacker.GridPosition.TileCoordinate;
            GridFacingDirection facing = attacker.GridPosition.FacingDirection;
            float bestRemainingDistance = float.MaxValue;
            float bestWorldDistance = float.MaxValue;
            int bestInstanceId = int.MaxValue;
            IReadOnlyList<Vector2Int> attackTiles = rangeData.AttackTiles;

            for (int i = 0; i < attackTiles.Count; i++)
            {
                Vector2Int relativeWorldTile = RotatePatternTile(attackTiles[i], facing);
                Vector2Int worldTile = attackerTile + relativeWorldTile;
                EvaluateAdditionalTile(attacker, attackSettings, facing, worldTile, selectedTargets, ref target, ref bestRemainingDistance, ref bestWorldDistance, ref bestInstanceId);
            }

            return target != null;
        }

        private static void EvaluateAdditionalTile(UnitRuntimeState attacker, AttackSettings attackSettings, GridFacingDirection facing, Vector2Int tile, List<EnemyRuntimeState> selectedTargets, ref EnemyRuntimeState target, ref float bestRemainingDistance, ref float bestWorldDistance, ref int bestInstanceId)
        {
            if (!CombatRegistry.TryGetEnemiesAt(tile, out HashSet<EnemyRuntimeState> tileEnemies))
            {
                return;
            }

            foreach (EnemyRuntimeState candidate in tileEnemies)
            {
                if (selectedTargets.Contains(candidate) || !IsValidTarget(candidate))
                {
                    continue;
                }

                if (!BasicAttackContextFactory.TryCreate(attacker, candidate, out BasicAttackContext baseContext))
                {
                    continue;
                }

                BasicAttackContext candidateContext = CreateFacingContext(baseContext, facing);
                bool baseLayerAllowed = BasicAttackRangeEvaluator.CanAttackTargetLayer(attackSettings.AttackTarget, candidateContext.TargetLayer);
                bool passiveLayerAllowed = attacker.Passives != null && attacker.Passives.AllowsTargetLayer(attacker, candidateContext.TargetLayer);
                bool ignoreTargetLayer = !baseLayerAllowed && passiveLayerAllowed;

                if (!BasicAttackRangeEvaluator.TryEvaluate(attackSettings, candidateContext, ignoreTargetLayer, out _, out _))
                {
                    continue;
                }

                float remainingDistance = candidate.IsSummon ? float.MaxValue : candidate.Move.RemainingPathDistance;
                float worldDistance = candidateContext.HorizontalWorldDistance;
                int instanceId = candidate.GetInstanceID();

                if (!IsBetterTarget(target, remainingDistance, worldDistance, instanceId, bestRemainingDistance, bestWorldDistance, bestInstanceId))
                {
                    continue;
                }

                target = candidate;
                bestRemainingDistance = remainingDistance;
                bestWorldDistance = worldDistance;
                bestInstanceId = instanceId;
            }
        }

        private static void EvaluateFacingTiles(UnitRuntimeState attacker, AttackSettings attackSettings, IReadOnlyList<Vector2Int> attackTiles, Vector2Int attackerTile, GridFacingDirection currentFacing, GridFacingDirection facing, ref EnemyRuntimeState target, ref GridFacingDirection bestFacing, ref float bestRemainingDistance, ref float bestWorldDistance, ref int bestInstanceId)
        {
            for (int i = 0; i < attackTiles.Count; i++)
            {
                Vector2Int relativeWorldTile = RotatePatternTile(attackTiles[i], facing);

                if (GetFacingDirection(relativeWorldTile, currentFacing) != facing)
                {
                    continue;
                }

                Vector2Int worldTile = attackerTile + relativeWorldTile;
                EvaluateTile(attacker, attackSettings, currentFacing, worldTile, ref target, ref bestFacing, ref bestRemainingDistance, ref bestWorldDistance, ref bestInstanceId);
            }
        }

        private static void EvaluateTile(UnitRuntimeState attacker, AttackSettings attackSettings, GridFacingDirection currentFacing, Vector2Int tile, ref EnemyRuntimeState target, ref GridFacingDirection bestFacing, ref float bestRemainingDistance, ref float bestWorldDistance, ref int bestInstanceId)
        {
            if (!CombatRegistry.TryGetEnemiesAt(tile, out HashSet<EnemyRuntimeState> tileEnemies))
            {
                return;
            }

            foreach (EnemyRuntimeState candidate in tileEnemies)
            {
                if (!IsValidTarget(candidate))
                {
                    continue;
                }

                if (!BasicAttackContextFactory.TryCreate(attacker, candidate, out BasicAttackContext baseContext))
                {
                    continue;
                }

                GridFacingDirection candidateFacing = attackSettings.RangeRotationMode == AttackRangeRotationMode.FollowFacing ? GetFacingDirection(baseContext.RelativeTargetTile, currentFacing) : currentFacing;
                BasicAttackContext candidateContext = CreateFacingContext(baseContext, candidateFacing);

                bool baseLayerAllowed = BasicAttackRangeEvaluator.CanAttackTargetLayer(attackSettings.AttackTarget, candidateContext.TargetLayer);
                bool passiveLayerAllowed = attacker.Passives != null && attacker.Passives.AllowsTargetLayer(attacker, candidateContext.TargetLayer);
                bool ignoreTargetLayer = !baseLayerAllowed && passiveLayerAllowed;

                if (!BasicAttackRangeEvaluator.TryEvaluate(attackSettings, candidateContext, ignoreTargetLayer, out _, out _))
                {
                    continue;
                }

                float remainingDistance = candidate.IsSummon ? float.MaxValue : candidate.Move.RemainingPathDistance;
                float worldDistance = candidateContext.HorizontalWorldDistance;
                int instanceId = candidate.GetInstanceID();

                if (!IsBetterTarget(target, remainingDistance, worldDistance, instanceId, bestRemainingDistance, bestWorldDistance, bestInstanceId))
                {
                    continue;
                }

                target = candidate;
                bestFacing = candidateFacing;
                bestRemainingDistance = remainingDistance;
                bestWorldDistance = worldDistance;
                bestInstanceId = instanceId;
            }
        }

        private static bool CanSearch(UnitRuntimeState attacker)
        {
            return attacker != null && attacker.IsInitialized && attacker.Health != null && !attacker.Health.IsDead && attacker.GridPosition != null && attacker.GridPosition.IsInitialized && attacker.DataLink != null && attacker.DataLink.HasData && attacker.DataLink.UnitData.AttackSettings != null;
        }

        private static bool IsValidTarget(EnemyRuntimeState target)
        {
            if (target == null || !target.IsInitialized || target.Health == null || target.Health.IsDead || target.GridPosition == null || !target.GridPosition.IsInitialized || target.DataLink == null || !target.DataLink.HasData || target.Move == null)
            {
                return false;
            }

            if (target.IsSummon)
            {
                return target.SummonRuntime != null && target.SummonRuntime.IsInitialized;
            }

            return target.Move.HasPath;
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

        private static bool IsBetterTarget(EnemyRuntimeState currentTarget, float remainingDistance, float worldDistance, int instanceId, float bestRemainingDistance, float bestWorldDistance, int bestInstanceId)
        {
            if (currentTarget == null)
            {
                return true;
            }

            if (remainingDistance < bestRemainingDistance - PriorityTolerance)
            {
                return true;
            }

            if (remainingDistance > bestRemainingDistance + PriorityTolerance)
            {
                return false;
            }

            if (worldDistance < bestWorldDistance - PriorityTolerance)
            {
                return true;
            }

            if (worldDistance > bestWorldDistance + PriorityTolerance)
            {
                return false;
            }

            return instanceId < bestInstanceId;
        }
    }
}