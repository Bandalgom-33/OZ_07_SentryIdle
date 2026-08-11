using System;
using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [Serializable]
    public sealed class PromotionLevelCap
    {
        [Tooltip("적용할 승급 단계입니다. 1이면 첫 승급, 2이면 두 번째 승급입니다.")]
        [Min(1)]
        [SerializeField] private int promotionStage = 1;

        [Tooltip("해당 승급 단계에서 허용되는 최대 레벨입니다.")]
        [Min(1)]
        [SerializeField] private int maxLevel = 30;

        public int PromotionStage => Mathf.Max(1, promotionStage);
        public int MaxLevel => Mathf.Max(1, maxLevel);
    }
}
