using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    [DisallowMultipleComponent]
    public sealed class CombatGridPosition : MonoBehaviour
    {
        [Header("런타임 격자 상태")]
        [Tooltip("현재 개체가 위치한 격자 타일 좌표입니다. 월드 X축은 타일 X, 월드 Z축은 타일 Y에 대응합니다.")]
        [SerializeField] private Vector2Int tileCoordinate;

        [Tooltip("현재 개체가 바라보는 격자 방향입니다.")]
        [SerializeField] private GridFacingDirection facingDirection = GridFacingDirection.North;

        [Tooltip("다른 공격자가 이 개체를 공격할 때 사용하는 대상 유형입니다.")]
        [SerializeField] private CombatTargetLayer targetLayer = CombatTargetLayer.Ground;

        [Tooltip("배치 또는 이동 시스템에서 격자 상태가 초기화됐는지 표시합니다.")]
        [SerializeField] private bool isInitialized;

        public Vector2Int TileCoordinate => tileCoordinate;
        public GridFacingDirection FacingDirection => facingDirection;
        public CombatTargetLayer TargetLayer => targetLayer;
        public bool IsInitialized => isInitialized;

        public void Initialize(Vector2Int initialTileCoordinate, GridFacingDirection initialFacingDirection, CombatTargetLayer initialTargetLayer)
        {
            tileCoordinate = initialTileCoordinate;
            facingDirection = initialFacingDirection;
            targetLayer = initialTargetLayer;
            isInitialized = true;
        }

        public void SetTileCoordinate(Vector2Int newTileCoordinate)
        {
            tileCoordinate = newTileCoordinate;
            isInitialized = true;
        }

        public void SetFacingDirection(GridFacingDirection newFacingDirection)
        {
            facingDirection = newFacingDirection;
        }

        public void SetTargetLayer(CombatTargetLayer newTargetLayer)
        {
            targetLayer = newTargetLayer;
        }

        public void Clear()
        {
            tileCoordinate = Vector2Int.zero;
            facingDirection = GridFacingDirection.North;
            targetLayer = CombatTargetLayer.Ground;
            isInitialized = false;
        }
    }
}