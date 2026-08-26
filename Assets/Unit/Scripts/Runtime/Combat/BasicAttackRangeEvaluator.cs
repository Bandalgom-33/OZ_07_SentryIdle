using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    public static class BasicAttackRangeEvaluator
    {
        private const float DistanceTolerance = 0.0001f;

        public static bool TryEvaluate(AttackSettings attackSettings, BasicAttackContext context, out Vector2Int evaluatedPatternTile, out BasicAttackFailureReason failureReason)
        {
            return TryEvaluate(attackSettings, context, false, out evaluatedPatternTile, out failureReason);
        }

        internal static bool TryEvaluate(AttackSettings attackSettings, BasicAttackContext context, bool ignoreTargetLayer, out Vector2Int evaluatedPatternTile, out BasicAttackFailureReason failureReason)
        {
            evaluatedPatternTile = context.RelativeTargetTile;
            failureReason = BasicAttackFailureReason.None;

            if (attackSettings == null || attackSettings.BasicAttackRange == null)
            {
                failureReason = BasicAttackFailureReason.MissingData;
                return false;
            }

            if (attackSettings.AttackMode == AttackMode.None)
            {
                failureReason = BasicAttackFailureReason.AttackDisabled;
                return false;
            }

            if (!ignoreTargetLayer && !CanAttackTargetLayer(attackSettings.AttackTarget, context.TargetLayer))
            {
                failureReason = BasicAttackFailureReason.TargetLayerNotAllowed;
                return false;
            }

            evaluatedPatternTile = ConvertWorldTileToPatternTile(
                context.RelativeTargetTile,
                attackSettings.RangeRotationMode,
                context.FacingDirection);

            if (!IsWithinAttackDistance(attackSettings, evaluatedPatternTile))
            {
                failureReason = BasicAttackFailureReason.OutsideWorldRange;
                return false;
            }

            if (!attackSettings.BasicAttackRange.Contains(evaluatedPatternTile))
            {
                failureReason = BasicAttackFailureReason.OutsideAttackTileRange;
                return false;
            }

            return true;
        }

        public static bool IsWithinAttackDistance(AttackSettings attackSettings, Vector2Int relativePatternTile)
        {
            if (attackSettings == null)
            {
                return false;
            }

            float tileDistance = Mathf.Sqrt(
                relativePatternTile.x * relativePatternTile.x +
                relativePatternTile.y * relativePatternTile.y);

            return tileDistance <= attackSettings.AttackRange + DistanceTolerance;
        }

        public static Vector2Int ConvertPatternTileToWorldTile(
            Vector2Int patternTile,
            AttackRangeRotationMode rotationMode,
            GridFacingDirection facingDirection)
        {
            if (rotationMode == AttackRangeRotationMode.Fixed)
            {
                return patternTile;
            }

            switch (facingDirection)
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

        public static Vector2Int ConvertWorldTileToPatternTile(Vector2Int worldRelativeTile, AttackRangeRotationMode rotationMode, GridFacingDirection facingDirection)
        {
            if (rotationMode == AttackRangeRotationMode.Fixed)
            {
                return worldRelativeTile;
            }

            switch (facingDirection)
            {
                case GridFacingDirection.East:
                    return new Vector2Int(-worldRelativeTile.y, worldRelativeTile.x);

                case GridFacingDirection.South:
                    return new Vector2Int(-worldRelativeTile.x, -worldRelativeTile.y);

                case GridFacingDirection.West:
                    return new Vector2Int(worldRelativeTile.y, -worldRelativeTile.x);

                default:
                    return worldRelativeTile;
            }
        }

        internal static bool CanAttackTargetLayer(AttackTarget allowedTargets, CombatTargetLayer targetLayer)
        {
            switch (allowedTargets)
            {
                case AttackTarget.Ground:
                    return targetLayer == CombatTargetLayer.Ground;

                case AttackTarget.Air:
                    return targetLayer == CombatTargetLayer.Air;

                case AttackTarget.GroundAndAir:
                    return true;

                default:
                    return false;
            }
        }
    }
}
