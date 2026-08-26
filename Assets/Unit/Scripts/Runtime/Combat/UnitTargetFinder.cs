using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    internal static class UnitTargetFinder
    {
        internal sealed class SearchBuffer
        {
            internal readonly List<TargetCandidate> Candidates = new List<TargetCandidate>(16);
        }

        private const float PriorityTolerance = 0.0001f;

        internal readonly struct TargetCandidate
        {
            public EnemyRuntimeState Target { get; }
            public int LayerPriority { get; }
            public float RemainingDistance { get; }
            public float WorldDistance { get; }
            public int InstanceId { get; }

            public TargetCandidate(EnemyRuntimeState target, int layerPriority, float remainingDistance, float worldDistance, int instanceId)
            {
                Target = target;
                LayerPriority = layerPriority;
                RemainingDistance = remainingDistance;
                WorldDistance = worldDistance;
                InstanceId = instanceId;
            }
        }

        private sealed class TargetCandidateComparer : IComparer<TargetCandidate>
        {
            public static readonly TargetCandidateComparer Instance = new TargetCandidateComparer();

            public int Compare(TargetCandidate x, TargetCandidate y)
            {
                int layer = x.LayerPriority.CompareTo(y.LayerPriority);
                if (layer != 0)
                {
                    return layer;
                }

                if (x.RemainingDistance < y.RemainingDistance - PriorityTolerance)
                {
                    return -1;
                }

                if (x.RemainingDistance > y.RemainingDistance + PriorityTolerance)
                {
                    return 1;
                }

                if (x.WorldDistance < y.WorldDistance - PriorityTolerance)
                {
                    return -1;
                }

                if (x.WorldDistance > y.WorldDistance + PriorityTolerance)
                {
                    return 1;
                }

                return x.InstanceId.CompareTo(y.InstanceId);
            }
        }

        internal static int FindTargets(UnitRuntimeState attacker, int maxTargetCount, List<EnemyRuntimeState> targets, SearchBuffer buffer)
        {
            if (targets == null || buffer == null)
            {
                return 0;
            }

            targets.Clear();
            List<TargetCandidate> candidates = buffer.Candidates;
            candidates.Clear();

            if (maxTargetCount <= 0 || !CanSearch(attacker))
            {
                return 0;
            }

            AttackSettings attackSettings = attacker.DataLink.UnitData.AttackSettings;
            BasicAttackRangeData rangeData = attackSettings.BasicAttackRange;
            if (rangeData == null)
            {
                return 0;
            }

            IReadOnlyList<Vector2Int> attackTiles = rangeData.AttackTiles;
            if (attackTiles == null || attackTiles.Count == 0)
            {
                return 0;
            }

            Vector2Int attackerTile = attacker.GridPosition.TileCoordinate;
            GridFacingDirection currentFacing = attacker.GridPosition.FacingDirection;
            GridFacingDirection selectedFacing = currentFacing;
            bool preferAir = CanPrioritizeAir(attacker, attackSettings);

            if (attackSettings.RangeRotationMode == AttackRangeRotationMode.FollowFacing && !TryFindBestFacing(attacker, attackSettings, attackTiles, attackerTile, currentFacing, preferAir, out selectedFacing))
            {
                return 0;
            }

            if (attackSettings.RangeRotationMode == AttackRangeRotationMode.FollowFacing && selectedFacing != currentFacing)
            {
                attacker.GridPosition.SetFacingDirection(selectedFacing);
            }

            GatherFacingCandidates(attacker, attackSettings, attackTiles, attackerTile, selectedFacing, preferAir, candidates);
            if (candidates.Count == 0)
            {
                return 0;
            }

            candidates.Sort(TargetCandidateComparer.Instance);
            int targetLimit = Mathf.Min(maxTargetCount, candidates.Count);

            for (int i = 0; i < candidates.Count && targets.Count < targetLimit; i++)
            {
                EnemyRuntimeState candidate = candidates[i].Target;
                if (candidate == null || targets.Contains(candidate))
                {
                    continue;
                }

                targets.Add(candidate);
            }

            return targets.Count;
        }

        private static bool TryFindBestFacing(UnitRuntimeState attacker, AttackSettings attackSettings, IReadOnlyList<Vector2Int> attackTiles, Vector2Int attackerTile, GridFacingDirection currentFacing, bool preferAir, out GridFacingDirection bestFacing)
        {
            bestFacing = currentFacing;
            bool found = false;
            TargetCandidate bestCandidate = default;

            for (int offset = 0; offset < 4; offset++)
            {
                GridFacingDirection facing = (GridFacingDirection)(((int)currentFacing + offset) & 3);

                for (int i = 0; i < attackTiles.Count; i++)
                {
                    Vector2Int relativeWorldTile = BasicAttackRangeEvaluator.ConvertPatternTileToWorldTile(attackTiles[i], attackSettings.RangeRotationMode, facing);
                    Vector2Int worldTile = attackerTile + relativeWorldTile;
                    if (!CombatRegistry.TryGetEnemiesAt(worldTile, out HashSet<EnemyRuntimeState> tileEnemies))
                    {
                        continue;
                    }

                    foreach (EnemyRuntimeState candidate in tileEnemies)
                    {
                        if (!TryCreateCandidate(attacker, attackSettings, facing, candidate, preferAir, out TargetCandidate evaluated))
                        {
                            continue;
                        }

                        if (!found || TargetCandidateComparer.Instance.Compare(evaluated, bestCandidate) < 0)
                        {
                            bestCandidate = evaluated;
                            bestFacing = facing;
                            found = true;
                        }
                    }
                }
            }

            return found;
        }

        private static void GatherFacingCandidates(UnitRuntimeState attacker, AttackSettings attackSettings, IReadOnlyList<Vector2Int> attackTiles, Vector2Int attackerTile, GridFacingDirection facing, bool preferAir, List<TargetCandidate> candidates)
        {
            for (int i = 0; i < attackTiles.Count; i++)
            {
                Vector2Int relativeWorldTile = BasicAttackRangeEvaluator.ConvertPatternTileToWorldTile(attackTiles[i], attackSettings.RangeRotationMode, facing);
                Vector2Int worldTile = attackerTile + relativeWorldTile;
                if (!CombatRegistry.TryGetEnemiesAt(worldTile, out HashSet<EnemyRuntimeState> tileEnemies))
                {
                    continue;
                }

                foreach (EnemyRuntimeState candidate in tileEnemies)
                {
                    if (TryCreateCandidate(attacker, attackSettings, facing, candidate, preferAir, out TargetCandidate evaluated))
                    {
                        candidates.Add(evaluated);
                    }
                }
            }
        }

        private static bool TryCreateCandidate(UnitRuntimeState attacker, AttackSettings attackSettings, GridFacingDirection facing, EnemyRuntimeState candidate, bool preferAir, out TargetCandidate evaluated)
        {
            evaluated = default;

            if (!IsValidTarget(candidate) || !BasicAttackContextFactory.TryCreate(attacker, candidate, out BasicAttackContext baseContext))
            {
                return false;
            }

            BasicAttackContext candidateContext = new BasicAttackContext(baseContext.RelativeTargetTile, baseContext.HorizontalWorldDistance, facing, baseContext.TargetLayer);
            bool baseLayerAllowed = BasicAttackRangeEvaluator.CanAttackTargetLayer(attackSettings.AttackTarget, candidateContext.TargetLayer);
            bool passiveLayerAllowed = attacker.Passives != null && attacker.Passives.AllowsTargetLayer(attacker, candidateContext.TargetLayer);
            bool ignoreTargetLayer = !baseLayerAllowed && passiveLayerAllowed;

            if (!BasicAttackRangeEvaluator.TryEvaluate(attackSettings, candidateContext, ignoreTargetLayer, out _, out _))
            {
                return false;
            }

            int layerPriority = preferAir && candidateContext.TargetLayer == CombatTargetLayer.Air ? 0 : 1;
            float remainingDistance = candidate.IsSummon ? float.MaxValue : candidate.Move.RemainingPathDistance;
            evaluated = new TargetCandidate(candidate, layerPriority, remainingDistance, candidateContext.HorizontalWorldDistance, candidate.GetInstanceID());
            return true;
        }

        private static bool CanPrioritizeAir(UnitRuntimeState attacker, AttackSettings attackSettings)
        {
            if (attackSettings != null && BasicAttackRangeEvaluator.CanAttackTargetLayer(attackSettings.AttackTarget, CombatTargetLayer.Air))
            {
                return true;
            }

            return attacker != null && attacker.Passives != null && attacker.Passives.AllowsTargetLayer(attacker, CombatTargetLayer.Air);
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
    }
}
