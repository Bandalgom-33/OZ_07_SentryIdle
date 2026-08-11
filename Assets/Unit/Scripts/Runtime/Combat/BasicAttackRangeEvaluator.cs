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

            if (context.HorizontalWorldDistance > attackSettings.AttackRange + DistanceTolerance)
            {
                failureReason = BasicAttackFailureReason.OutsideWorldRange;
                return false;
            }

            evaluatedPatternTile = ConvertWorldTileToPatternTile(context.RelativeTargetTile, attackSettings.RangeRotationMode, context.FacingDirection);

            if (!attackSettings.BasicAttackRange.Contains(evaluatedPatternTile))
            {
                failureReason = BasicAttackFailureReason.OutsideAttackTileRange;
                return false;
            }

            return true;
        }

        public static Vector2Int ConvertWorldTileToPatternTile(Vector2Int worldRelativeTile, AttackRangeRotationMode rotationMode, GridFacingDirection facingDirection)
        {
            _ = rotationMode;

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
