using System;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    [Serializable]
    public sealed class SummonOwnerStatInheritance
    {
        [Tooltip("소환자로부터 물려받을 현재 전투 능력치입니다.")]
        [SerializeField] private PassiveStatType statType = PassiveStatType.None;

        [Tooltip("소환 시점의 소환자 현재 능력치에서 물려받을 비율(%)입니다. 예: 50이면 현재 능력치의 50%를 소환물에 적용합니다.")]
        [Min(0f)]
        [SerializeField] private float inheritPercent;

        public PassiveStatType StatType => statType;
        public float InheritPercent => float.IsNaN(inheritPercent) || float.IsInfinity(inheritPercent) ? 0f : Mathf.Max(0f, inheritPercent);
    }
}