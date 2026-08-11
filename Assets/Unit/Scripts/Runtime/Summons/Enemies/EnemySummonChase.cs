using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyRuntimeState))]
    public sealed class EnemySummonChase : MonoBehaviour
    {
        private const float DistanceTolerance = 0.0001f;
        private const float TargetMoveSqrTolerance = 0.0001f;

        private EnemyRuntimeState state;
        private UnitRuntimeState target;
        private Transform targetTransform;

        private bool trackingSegmentInitialized;
        private Vector3 trackingStartWorldPosition;
        private Vector3 trackingTargetWorldPosition;
        private Vector2Int trackingStartTile;
        private Vector2Int trackingTargetTile;
        private float trackingInitialWorldDistance;

        public UnitRuntimeState Target => target;
        public bool HasTarget => IsValidTarget(target);

        private void Awake()
        {
            state = GetComponent<EnemyRuntimeState>();
        }

        private void OnDisable()
        {
            ClearTarget();
        }

        internal void Step(float deltaTime)
        {
            if (!CanStep(deltaTime) || !EnsureTarget())
            {
                return;
            }

            if (state.Move.IsPaused || state.Move.IsBlocked)
            {
                return;
            }

            EnsureTrackingSegment();

            Vector3 offset = targetTransform.position - transform.position;
            offset.y = 0f;

            float sqrDistance = offset.sqrMagnitude;
            float stopDistance = GetAttackRange();

            if (sqrDistance <= stopDistance * stopDistance + DistanceTolerance)
            {
                UpdateFacing(offset);
                SyncLogicalTile();
                return;
            }

            float distance = Mathf.Sqrt(sqrDistance);

            if (distance <= DistanceTolerance)
            {
                SyncLogicalTile();
                return;
            }

            float moveDistance = Mathf.Min(state.Stats.MoveSpeed * deltaTime, distance - stopDistance);

            if (moveDistance <= 0f)
            {
                SyncLogicalTile();
                return;
            }

            UpdateFacing(offset);
            transform.position += offset / distance * moveDistance;
            SyncLogicalTile();
        }

        internal bool TryGetTarget(out UnitRuntimeState currentTarget)
        {
            if (!EnsureTarget())
            {
                currentTarget = null;
                return false;
            }

            currentTarget = target;
            return true;
        }

        internal bool TryGetAttackTarget(out UnitRuntimeState attackTarget, out BasicAttackContext context)
        {
            attackTarget = null;
            context = default;

            if (!EnsureTarget() || state == null || state.DataLink == null || !state.DataLink.HasData)
            {
                return false;
            }

            AttackSettings attackSettings = state.DataLink.EnemyData.AttackSettings;

            if (attackSettings == null || attackSettings.AttackMode == AttackMode.None)
            {
                return false;
            }

            Vector3 offset = targetTransform.position - transform.position;
            offset.y = 0f;

            float distance = offset.magnitude;

            if (distance > attackSettings.AttackRange + DistanceTolerance)
            {
                return false;
            }

            CombatTargetLayer targetLayer = target.GridPosition.TargetLayer;

            if (!BasicAttackRangeEvaluator.CanAttackTargetLayer(attackSettings.AttackTarget, targetLayer))
            {
                return false;
            }

            GridFacingDirection facing = state.GridPosition != null ? state.GridPosition.FacingDirection : GridFacingDirection.North;

            attackTarget = target;
            context = new BasicAttackContext(Vector2Int.zero, distance, facing, targetLayer);
            return true;
        }

        private bool EnsureTarget()
        {
            if (IsValidTarget(target))
            {
                return true;
            }

            ClearTarget();
            return TryAcquireNearestTarget();
        }

        private bool TryAcquireNearestTarget()
        {
            UnitRuntimeState nearestTarget = null;
            float nearestSqrDistance = float.MaxValue;
            int nearestInstanceId = int.MaxValue;
            Vector3 currentPosition = transform.position;

            foreach (UnitRuntimeState candidate in CombatRegistry.Units)
            {
                if (!IsValidTarget(candidate))
                {
                    continue;
                }

                Vector3 offset = candidate.transform.position - currentPosition;
                offset.y = 0f;

                float sqrDistance = offset.sqrMagnitude;
                int instanceId = candidate.GetInstanceID();

                if (sqrDistance > nearestSqrDistance)
                {
                    continue;
                }

                if (Mathf.Approximately(sqrDistance, nearestSqrDistance) && instanceId >= nearestInstanceId)
                {
                    continue;
                }

                nearestTarget = candidate;
                nearestSqrDistance = sqrDistance;
                nearestInstanceId = instanceId;
            }

            if (nearestTarget == null)
            {
                return false;
            }

            target = nearestTarget;
            targetTransform = nearestTarget.transform;
            BeginTrackingSegment();
            return true;
        }

        private void BeginTrackingSegment()
        {
            trackingSegmentInitialized = false;

            if (state == null || state.GridPosition == null || !state.GridPosition.IsInitialized || !IsValidTarget(target) || targetTransform == null)
            {
                return;
            }

            trackingStartWorldPosition = transform.position;
            trackingTargetWorldPosition = targetTransform.position;
            trackingStartTile = state.GridPosition.TileCoordinate;
            trackingTargetTile = target.GridPosition.TileCoordinate;

            Vector3 offset = trackingTargetWorldPosition - trackingStartWorldPosition;
            offset.y = 0f;

            trackingInitialWorldDistance = offset.magnitude;
            trackingSegmentInitialized = true;
        }

        private void EnsureTrackingSegment()
        {
            if (!trackingSegmentInitialized)
            {
                BeginTrackingSegment();
                return;
            }

            if (!IsValidTarget(target) || targetTransform == null)
            {
                trackingSegmentInitialized = false;
                return;
            }

            if (target.GridPosition.TileCoordinate != trackingTargetTile)
            {
                BeginTrackingSegment();
                return;
            }

            Vector3 targetDelta = targetTransform.position - trackingTargetWorldPosition;
            targetDelta.y = 0f;

            if (targetDelta.sqrMagnitude > TargetMoveSqrTolerance)
            {
                BeginTrackingSegment();
            }
        }

        private void SyncLogicalTile()
        {
            if (state == null || state.GridPosition == null || !state.GridPosition.IsInitialized)
            {
                return;
            }

            EnsureTrackingSegment();

            if (!trackingSegmentInitialized)
            {
                return;
            }

            if (trackingInitialWorldDistance <= DistanceTolerance)
            {
                state.GridPosition.SetTileCoordinate(trackingTargetTile);
                return;
            }

            Vector3 remainingOffset = trackingTargetWorldPosition - transform.position;
            remainingOffset.y = 0f;

            float progress = 1f - Mathf.Clamp01(remainingOffset.magnitude / trackingInitialWorldDistance);
            int tileX = Mathf.RoundToInt(Mathf.Lerp(trackingStartTile.x, trackingTargetTile.x, progress));
            int tileY = Mathf.RoundToInt(Mathf.Lerp(trackingStartTile.y, trackingTargetTile.y, progress));

            state.GridPosition.SetTileCoordinate(new Vector2Int(tileX, tileY));
        }

        private void ClearTarget()
        {
            target = null;
            targetTransform = null;
            trackingSegmentInitialized = false;
            trackingInitialWorldDistance = 0f;
        }

        private float GetAttackRange()
        {
            if (state == null || state.DataLink == null || !state.DataLink.HasData)
            {
                return 0f;
            }

            AttackSettings attackSettings = state.DataLink.EnemyData.AttackSettings;
            return attackSettings != null && attackSettings.AttackMode != AttackMode.None ? Mathf.Max(0f, attackSettings.AttackRange) : 0f;
        }

        private void UpdateFacing(Vector3 direction)
        {
            if (state == null || state.GridPosition == null || direction.sqrMagnitude <= DistanceTolerance)
            {
                return;
            }

            GridFacingDirection facing;

            if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.z))
            {
                facing = direction.x >= 0f ? GridFacingDirection.East : GridFacingDirection.West;
            }
            else
            {
                facing = direction.z >= 0f ? GridFacingDirection.North : GridFacingDirection.South;
            }

            state.GridPosition.SetFacingDirection(facing);
        }

        private bool CanStep(float deltaTime)
        {
            return deltaTime > 0f && state != null && state.IsInitialized && state.Health != null && !state.Health.IsDead && state.Stats != null && state.Stats.IsInitialized && state.Move != null && state.GridPosition != null && state.GridPosition.IsInitialized;
        }

        private static bool IsValidTarget(UnitRuntimeState candidate)
        {
            return candidate != null && candidate.gameObject.activeInHierarchy && candidate.IsInitialized && candidate.Health != null && !candidate.Health.IsDead && candidate.GridPosition != null && candidate.GridPosition.IsInitialized && candidate.DataLink != null && candidate.DataLink.HasData;
        }
    }
}