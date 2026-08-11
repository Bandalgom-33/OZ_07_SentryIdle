using System;
using System.Collections.Generic;
using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [Serializable]
    public sealed class BasicAttackRangeData
    {
        [Header("격자 표시 설정")]
        [Tooltip("공격 주체의 좌우에 표시할 타일 수입니다. 3이면 왼쪽 3칸과 오른쪽 3칸을 표시합니다.")]
        [Range(0, 6)]
        [SerializeField] private int horizontalRadius = 3;

        [Tooltip("공격 주체의 정면에 표시할 타일 수입니다. 기준 방향에서 양의 Y 좌표가 정면입니다.")]
        [Range(0, 10)]
        [SerializeField] private int forwardDistance = 3;

        [Tooltip("공격 주체의 후방에 표시할 타일 수입니다. 기준 방향에서 음의 Y 좌표가 후방입니다.")]
        [Range(0, 10)]
        [SerializeField] private int backwardDistance = 1;

        [Header("선택된 공격 타일")]
        [Tooltip("공격 주체의 위치를 (0, 0)으로 했을 때 기본 공격이 가능한 상대 타일 좌표입니다.")]
        [SerializeField] private List<Vector2Int> attackTiles = new List<Vector2Int>();

        public int HorizontalRadius => horizontalRadius;
        public int ForwardDistance => forwardDistance;
        public int BackwardDistance => backwardDistance;
        public IReadOnlyList<Vector2Int> AttackTiles => attackTiles;

        public bool Contains(Vector2Int relativeTile)
        {
            for (int i = 0; i < attackTiles.Count; i++)
            {
                if (attackTiles[i] == relativeTile)
                {
                    return true;
                }
            }

            return false;
        }
    }
}