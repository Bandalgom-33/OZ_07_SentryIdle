using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    public readonly struct BasicAttackContext
    {
        public Vector2Int RelativeTargetTile { get; }
        public float HorizontalWorldDistance { get; }
        public GridFacingDirection FacingDirection { get; }
        public CombatTargetLayer TargetLayer { get; }

        public BasicAttackContext(Vector2Int relativeTargetTile, float horizontalWorldDistance, GridFacingDirection facingDirection, CombatTargetLayer targetLayer)
        {
            RelativeTargetTile = relativeTargetTile;
            HorizontalWorldDistance = Mathf.Max(0f, horizontalWorldDistance);
            FacingDirection = facingDirection;
            TargetLayer = targetLayer;
        }
    }
}