using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyDataLink))]
    [RequireComponent(typeof(CombatHealth))]
    [RequireComponent(typeof(CombatGridPosition))]
    [RequireComponent(typeof(EnemyBlock))]
    public sealed class EnemyMove : MonoBehaviour
    {
        private const float ArriveSqrDistance = 0.000001f;

        private EnemyDataLink dataLink;
        private EnemyRuntimeState state;
        private CombatHealth health;
        private CombatGridPosition gridPosition;
        private EnemyBlock block;

        private PathNode[] path;
        private int nodeIndex;
        private bool hasPath;
        private bool hasReachedGoal;
        private bool isPaused;
        private bool isAttackPaused;
        private float totalPathDistance;
        private float traveledPathDistance;

        public bool HasPath => hasPath;
        public bool HasReachedGoal => hasReachedGoal;
        public bool IsPaused => isPaused || isAttackPaused;
        public bool IsAttackPaused => isAttackPaused;
        public bool IsBlocked => block != null && block.IsBlocked;
        public bool IsMoving => hasPath && !hasReachedGoal && !IsPaused && !IsBlocked && health != null && !health.IsDead;
        public int NodeIndex => nodeIndex;
        public int NodeCount => path == null ? 0 : path.Length;
        public float TotalPathDistance => totalPathDistance;
        public float TraveledPathDistance => traveledPathDistance;
        public float RemainingPathDistance => Mathf.Max(0f, totalPathDistance - traveledPathDistance);
        public float PathProgress => totalPathDistance > 0f ? Mathf.Clamp01(traveledPathDistance / totalPathDistance) : 0f;

        private void Awake()
        {
            dataLink = GetComponent<EnemyDataLink>();
            state = GetComponent<EnemyRuntimeState>();
            health = GetComponent<CombatHealth>();
            gridPosition = GetComponent<CombatGridPosition>();
            block = GetComponent<EnemyBlock>();
        }

        private void OnDisable()
        {
            ClearPath();
        }

        public bool SetPath(PathNode[] newPath)
        {
            if (newPath == null || newPath.Length == 0 || dataLink == null || !dataLink.HasData || gridPosition == null || block == null)
            {
                ClearPath();
                return false;
            }

            path = newPath;
            nodeIndex = 0;
            hasPath = true;
            hasReachedGoal = false;
            isPaused = false;
            isAttackPaused = false;
            totalPathDistance = CalculatePathDistance(path);
            traveledPathDistance = 0f;

            PathNode startNode = path[0];

            transform.position = startNode.Position;

            CombatTargetLayer targetLayer = dataLink.EnemyData.MovementType == EnemyMovementType.Air ? CombatTargetLayer.Air : CombatTargetLayer.Ground;

            gridPosition.Initialize(startNode.Tile, startNode.Facing, targetLayer);

            nodeIndex = 1;

            if (nodeIndex >= path.Length)
            {
                CompletePath();
            }

            return true;
        }

        public bool TryCreateRemainingPath(out PathNode[] remainingPath)
        {
            remainingPath = null;

            if (!hasPath || path == null || nodeIndex >= path.Length || gridPosition == null || !gridPosition.IsInitialized)
            {
                return false;
            }

            int remainingNodeCount = path.Length - nodeIndex;
            remainingPath = new PathNode[remainingNodeCount + 1];
            remainingPath[0] = new PathNode(transform.position, gridPosition.TileCoordinate, gridPosition.FacingDirection);

            for (int i = 0; i < remainingNodeCount; i++)
            {
                remainingPath[i + 1] = path[nodeIndex + i];
            }

            return true;
        }

        public void Step(float deltaTime)
        {
            if (!IsMoving || deltaTime <= 0f || path == null || state == null || !state.IsInitialized || state.Stats == null || !state.Stats.IsInitialized)
            {
                return;
            }

            if (nodeIndex >= path.Length)
            {
                CompletePath();
                return;
            }

            float moveSpeed = state.Stats.MoveSpeed;

            if (moveSpeed <= 0f)
            {
                return;
            }

            float maxMoveDistance = moveSpeed * deltaTime;

            if (TryMoveToBlockPosition(maxMoveDistance))
            {
                return;
            }

            MoveToNextNode(maxMoveDistance);
        }

        public void SetPaused(bool paused)
        {
            isPaused = paused;
        }

        internal void SetAttackPaused(bool paused)
        {
            isAttackPaused = paused;
        }

        internal void PrepareForSpawn()
        {
            ClearPath();
            hasReachedGoal = false;
        }

        public void ClearPath()
        {
            path = null;
            nodeIndex = 0;
            hasPath = false;
            isPaused = false;
            isAttackPaused = false;
            totalPathDistance = 0f;
            traveledPathDistance = 0f;
        }

        private bool TryMoveToBlockPosition(float maxMoveDistance)
        {
            if (!TryFindUpcomingBlock(out UnitBlock unit, out int unitNodeIndex, out int stopSegmentTargetIndex, out Vector3 stopPosition))
            {
                return false;
            }

            if (stopSegmentTargetIndex != nodeIndex)
            {
                return false;
            }

            gridPosition.SetFacingDirection(path[nodeIndex].Facing);

            MoveTracked(stopPosition, maxMoveDistance);

            Vector3 offset = stopPosition - transform.position;

            if (offset.sqrMagnitude > ArriveSqrDistance)
            {
                return true;
            }

            transform.position = stopPosition;

            if (!BlockLink.TryBind(unit, block))
            {
                return true;
            }

            gridPosition.SetTileCoordinate(path[unitNodeIndex - 1].Tile);
            gridPosition.SetFacingDirection(path[unitNodeIndex].Facing);

            nodeIndex = unitNodeIndex;

            return true;
        }

        private bool TryFindUpcomingBlock(out UnitBlock unit, out int unitNodeIndex, out int stopSegmentTargetIndex, out Vector3 stopPosition)
        {
            unit = null;
            unitNodeIndex = -1;
            stopSegmentTargetIndex = -1;
            stopPosition = Vector3.zero;

            int lookAheadCount = Mathf.CeilToInt(block.BlockStopDistance) + 1;
            int lastNodeIndex = Mathf.Min(path.Length - 1, nodeIndex + lookAheadCount);

            for (int candidateNodeIndex = nodeIndex; candidateNodeIndex <= lastNodeIndex; candidateNodeIndex++)
            {
                if (!BlockFinder.TryFind(block, path[candidateNodeIndex].Tile, out UnitBlock candidateUnit))
                {
                    continue;
                }

                CalculateBlockStopPosition(candidateNodeIndex, out int candidateStopSegmentTargetIndex, out Vector3 candidateStopPosition);

                if (candidateStopSegmentTargetIndex < nodeIndex)
                {
                    continue;
                }

                if (candidateStopSegmentTargetIndex == nodeIndex && !IsPointAheadOnCurrentSegment(candidateStopPosition))
                {
                    continue;
                }

                unit = candidateUnit;
                unitNodeIndex = candidateNodeIndex;
                stopSegmentTargetIndex = candidateStopSegmentTargetIndex;
                stopPosition = candidateStopPosition;

                return true;
            }

            return false;
        }

        private void CalculateBlockStopPosition(int unitNodeIndex, out int stopSegmentTargetIndex, out Vector3 stopPosition)
        {
            float remainingDistance = block.BlockStopDistance;

            for (int segmentTargetIndex = unitNodeIndex; segmentTargetIndex > 0; segmentTargetIndex--)
            {
                PathNode segmentTarget = path[segmentTargetIndex];
                PathNode segmentStart = path[segmentTargetIndex - 1];

                Vector2Int tileDelta = segmentTarget.Tile - segmentStart.Tile;
                float segmentTileDistance = Mathf.Abs(tileDelta.x) + Mathf.Abs(tileDelta.y);

                if (segmentTileDistance <= 0f)
                {
                    continue;
                }

                if (remainingDistance <= segmentTileDistance)
                {
                    float normalizedDistance = remainingDistance / segmentTileDistance;

                    stopSegmentTargetIndex = segmentTargetIndex;
                    stopPosition = Vector3.Lerp(segmentTarget.Position, segmentStart.Position, normalizedDistance);

                    return;
                }

                remainingDistance -= segmentTileDistance;
            }

            stopSegmentTargetIndex = 1;
            stopPosition = path[0].Position;
        }

        private bool IsPointAheadOnCurrentSegment(Vector3 point)
        {
            if (nodeIndex <= 0 || nodeIndex >= path.Length)
            {
                return false;
            }

            Vector3 segmentDirection = path[nodeIndex].Position - path[nodeIndex - 1].Position;

            if (segmentDirection.sqrMagnitude <= ArriveSqrDistance)
            {
                return (point - transform.position).sqrMagnitude <= ArriveSqrDistance;
            }

            Vector3 pointDirection = point - transform.position;

            return Vector3.Dot(pointDirection, segmentDirection) >= 0f;
        }

        private void MoveToNextNode(float maxMoveDistance)
        {
            PathNode targetNode = path[nodeIndex];

            gridPosition.SetFacingDirection(targetNode.Facing);

            MoveTracked(targetNode.Position, maxMoveDistance);

            Vector3 offset = targetNode.Position - transform.position;

            if (offset.sqrMagnitude > ArriveSqrDistance)
            {
                return;
            }

            transform.position = targetNode.Position;
            gridPosition.SetTileCoordinate(targetNode.Tile);

            nodeIndex++;

            if (nodeIndex >= path.Length)
            {
                CompletePath();
            }
        }

        private void MoveTracked(Vector3 targetPosition, float maxMoveDistance)
        {
            Vector3 previousPosition = transform.position;

            transform.position = Vector3.MoveTowards(previousPosition, targetPosition, maxMoveDistance);

            traveledPathDistance = Mathf.Min(totalPathDistance, traveledPathDistance + Vector3.Distance(previousPosition, transform.position));
            SyncCurrentSegmentTile();
        }

        private void SyncCurrentSegmentTile()
        {
            if (path == null || nodeIndex <= 0 || nodeIndex >= path.Length || gridPosition == null || !gridPosition.IsInitialized)
            {
                return;
            }

            PathNode segmentStart = path[nodeIndex - 1];
            PathNode segmentTarget = path[nodeIndex];
            Vector3 segment = segmentTarget.Position - segmentStart.Position;
            float segmentSqrLength = segment.sqrMagnitude;

            if (segmentSqrLength <= ArriveSqrDistance)
            {
                gridPosition.SetTileCoordinate(segmentTarget.Tile);
                return;
            }

            float progress = Mathf.Clamp01(Vector3.Dot(transform.position - segmentStart.Position, segment) / segmentSqrLength);
            int tileX = Mathf.RoundToInt(Mathf.Lerp(segmentStart.Tile.x, segmentTarget.Tile.x, progress));
            int tileY = Mathf.RoundToInt(Mathf.Lerp(segmentStart.Tile.y, segmentTarget.Tile.y, progress));

            gridPosition.SetTileCoordinate(new Vector2Int(tileX, tileY));
        }

        private static float CalculatePathDistance(PathNode[] targetPath)
        {
            float distance = 0f;

            for (int i = 1; i < targetPath.Length; i++)
            {
                distance += Vector3.Distance(targetPath[i - 1].Position, targetPath[i].Position);
            }

            return distance;
        }

        private void CompletePath()
        {
            if (hasReachedGoal)
            {
                return;
            }

            traveledPathDistance = totalPathDistance;
            hasPath = false;
            hasReachedGoal = true;
            isPaused = false;
            isAttackPaused = false;

            if (state != null && state.IsInitialized)
            {
                CombatEvents.PublishEnemyReachedGoal(new EnemyReachedGoalInfo(state.RuntimeId, state.EnemyId, transform.position));
            }
        }
    }
}