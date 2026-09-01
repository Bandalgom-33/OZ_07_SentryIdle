using System;
using System.Collections.Generic;
using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    /// <summary>
    /// 범위형 SP 스킬이 대표 대상의 타일을 중심으로 어떤 상대 타일까지 영향을 줄지 정의합니다.
    /// (0, 0)은 대표 대상 타일이며 항상 포함됩니다.
    /// </summary>
    [Serializable]
    public sealed class SkillAreaTileData
    {
        [Header("격자 표시 설정")]
        [Tooltip("중심 대상의 좌우에 표시할 타일 수입니다.")]
        [Range(0, 6)]
        [SerializeField] private int horizontalRadius = 2;

        [Tooltip("중심 대상 위쪽에 표시할 타일 수입니다.")]
        [Range(0, 10)]
        [SerializeField] private int forwardDistance = 2;

        [Tooltip("중심 대상 아래쪽에 표시할 타일 수입니다.")]
        [Range(0, 10)]
        [SerializeField] private int backwardDistance = 2;

        [Header("선택된 추가 영향 타일")]
        [Tooltip("대표 대상 타일을 (0, 0)으로 했을 때 같이 피해를 받을 추가 타일 좌표입니다. 중심 타일 (0, 0)은 목록에 없어도 항상 포함됩니다.")]
        [SerializeField] private List<Vector2Int> affectedTiles = new List<Vector2Int>
        {
            new Vector2Int(-1, 1),
            new Vector2Int(0, 1),
            new Vector2Int(1, 1),
            new Vector2Int(-1, 0),
            new Vector2Int(1, 0),
            new Vector2Int(-1, -1),
            new Vector2Int(0, -1),
            new Vector2Int(1, -1)
        };

        public int HorizontalRadius => horizontalRadius;
        public int ForwardDistance => forwardDistance;
        public int BackwardDistance => backwardDistance;
        public IReadOnlyList<Vector2Int> AffectedTiles => affectedTiles;

        public bool Contains(Vector2Int relativeTile)
        {
            if (relativeTile == Vector2Int.zero)
            {
                return true;
            }

            if (affectedTiles == null)
            {
                return false;
            }

            for (int i = 0; i < affectedTiles.Count; i++)
            {
                if (affectedTiles[i] == relativeTile)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
