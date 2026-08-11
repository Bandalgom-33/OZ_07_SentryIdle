using System;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    [Serializable]
    public sealed class SummonStatModifier
    {
        [Tooltip("소환물에 추가로 보정할 전투 능력치입니다.")]
        [SerializeField] private PassiveStatType statType = PassiveStatType.None;

        [Tooltip("소환물의 기준 능력치에 더할 고정값입니다.")]
        [SerializeField] private float flatBonus;

        [Tooltip("소환물의 기준 능력치에 적용할 비율 보정값(%)입니다.")]
        [SerializeField] private float percentBonus;

        public PassiveStatType StatType => statType;
        public float FlatBonus => float.IsNaN(flatBonus) || float.IsInfinity(flatBonus) ? 0f : flatBonus;
        public float PercentBonus => float.IsNaN(percentBonus) || float.IsInfinity(percentBonus) ? 0f : percentBonus;
    }
}
