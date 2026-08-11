using System;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    [DisallowMultipleComponent]
    public sealed class CombatGridPosition : MonoBehaviour
    {
        [Header("런타임 격자 상태")]
        [Tooltip("현재 개체가 전투에서 사용하는 논리 격자 타일 좌표입니다.")]
        [SerializeField] private Vector2Int tileCoordinate;

        [Tooltip("현재 개체가 전투에서 바라보는 논리 격자 방향입니다.")]
        [SerializeField] private GridFacingDirection facingDirection = GridFacingDirection.North;

        [Tooltip("다른 공격자가 이 개체를 공격할 때 사용하는 전투 대상 층입니다.")]
        [SerializeField] private CombatTargetLayer targetLayer = CombatTargetLayer.Ground;

        [Tooltip("배치 또는 이동 시스템에서 논리 격자 상태가 초기화됐는지 표시합니다.")]
        [SerializeField] private bool isInitialized;

        internal event Action<CombatGridPosition> OnTileChanged;
        internal event Action<CombatGridPosition> OnFacingChanged;

        public Vector2Int TileCoordinate => tileCoordinate;
        public GridFacingDirection FacingDirection => facingDirection;
        public CombatTargetLayer TargetLayer => targetLayer;
        public bool IsInitialized => isInitialized;

        public void Initialize(Vector2Int initialTileCoordinate, GridFacingDirection initialFacingDirection, CombatTargetLayer initialTargetLayer)
        {
            bool tileChanged = !isInitialized || tileCoordinate != initialTileCoordinate;
            bool facingChanged = !isInitialized || facingDirection != initialFacingDirection;

            tileCoordinate = initialTileCoordinate;
            facingDirection = initialFacingDirection;
            targetLayer = initialTargetLayer;
            isInitialized = true;

            if (tileChanged)
            {
                OnTileChanged?.Invoke(this);
            }

            if (facingChanged)
            {
                OnFacingChanged?.Invoke(this);
            }
        }

        public void SetTileCoordinate(Vector2Int newTileCoordinate)
        {
            if (isInitialized && tileCoordinate == newTileCoordinate)
            {
                return;
            }

            tileCoordinate = newTileCoordinate;
            isInitialized = true;

            OnTileChanged?.Invoke(this);
        }

        public void SetFacingDirection(GridFacingDirection newFacingDirection)
        {
            if (facingDirection == newFacingDirection)
            {
                return;
            }

            facingDirection = newFacingDirection;

            if (isInitialized)
            {
                OnFacingChanged?.Invoke(this);
            }
        }

        public void SetTargetLayer(CombatTargetLayer newTargetLayer)
        {
            targetLayer = newTargetLayer;
        }

        public void Clear()
        {
            bool wasInitialized = isInitialized;

            tileCoordinate = Vector2Int.zero;
            facingDirection = GridFacingDirection.North;
            targetLayer = CombatTargetLayer.Ground;
            isInitialized = false;

            if (wasInitialized)
            {
                OnTileChanged?.Invoke(this);
            }
        }
    }
}