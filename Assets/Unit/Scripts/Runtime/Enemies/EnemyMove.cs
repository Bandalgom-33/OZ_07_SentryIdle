using System;
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
        private CombatHealth health;
        private CombatGridPosition gridPosition;
        private EnemyBlock block;
        private PathNode[] path;
        private int nodeIndex;
        private bool hasPath;
        private bool isPaused;

        public event Action<EnemyMove> OnGoalReached;

        public bool HasPath => hasPath;
        public bool IsPaused => isPaused;
        public bool IsBlocked => block != null && block.IsBlocked;
        public bool IsMoving => hasPath && !isPaused && !IsBlocked && health != null && !health.IsDead;
        public int NodeIndex => nodeIndex;
        public int NodeCount => path == null ? 0 : path.Length;

        private void Awake()
        {
            dataLink = GetComponent<EnemyDataLink>();
            health = GetComponent<CombatHealth>();
            gridPosition = GetComponent<CombatGridPosition>();
            block = GetComponent<EnemyBlock>();
        }

        private void OnDisable()
        {
            ClearPath();
            OnGoalReached = null;
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
            isPaused = false;

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

        public void Step(float deltaTime)
        {
            if (!IsMoving || deltaTime <= 0f || path == null)
            {
                return;
            }

            if (nodeIndex >= path.Length)
            {
                CompletePath();
                return;
            }

            float moveSpeed = Mathf.Max(0f, dataLink.EnemyData.BaseStats.MoveSpeed);

            if (moveSpeed <= 0f)
            {
                return;
            }

            PathNode targetNode = path[nodeIndex];
            gridPosition.SetFacingDirection(targetNode.Facing);

            float maxMoveDistance = moveSpeed * deltaTime;

            if (TryBlockBeforeEntering(targetNode, maxMoveDistance))
            {
                return;
            }

            transform.position = Vector3.MoveTowards(transform.position, targetNode.Position, maxMoveDistance);

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

        public void SetPaused(bool paused)
        {
            isPaused = paused;
        }

        public void ClearPath()
        {
            path = null;
            nodeIndex = 0;
            hasPath = false;
            isPaused = false;
        }

        private bool TryBlockBeforeEntering(PathNode targetNode, float maxMoveDistance)
        {
            if (!BlockFinder.TryFind(block, targetNode.Tile, out UnitBlock unit))
            {
                return false;
            }

            Vector3 previousNodePosition = nodeIndex > 0 ? path[nodeIndex - 1].Position : transform.position;
            Vector3 stopPosition = Vector3.Lerp(targetNode.Position, previousNodePosition, unit.StopOffset);

            transform.position = Vector3.MoveTowards(transform.position, stopPosition, maxMoveDistance);

            Vector3 offset = stopPosition - transform.position;

            if (offset.sqrMagnitude > ArriveSqrDistance)
            {
                return true;
            }

            transform.position = stopPosition;
            BlockLink.TryBind(unit, block);
            return true;
        }

        private void CompletePath()
        {
            hasPath = false;
            OnGoalReached?.Invoke(this);
        }
    }
}