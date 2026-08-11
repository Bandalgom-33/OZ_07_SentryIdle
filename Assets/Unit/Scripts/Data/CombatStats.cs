using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace EndlessGuard.Unit.Data
{
    [Serializable]
    public sealed class CombatStats
    {
        [Header("기본 생존 능력치")]
        [Tooltip("레벨 성장, 공통 강화, 패시브와 버프가 적용되기 전 기준 최대 HP입니다.")]
        [Min(0f)]
        [SerializeField] private float maxHp;

        [Header("기본 공격 능력치")]
        [Tooltip("물리 기본 공격과 물리 공격형 능력의 기준 공격력입니다.")]
        [Min(0f)]
        [SerializeField] private float physicalAttack;

        [Tooltip("마법 기본 공격과 마법 공격형 능력의 기준 공격력입니다.")]
        [Min(0f)]
        [SerializeField] private float magicalAttack;

        [FormerlySerializedAs("attackInterval")]
        [Tooltip("강화가 적용되기 전 1초당 기본 공격 횟수입니다. 2는 1초에 2회, 0.5는 2초에 1회를 의미합니다.")]
        [Min(0f)]
        [SerializeField] private float baseAttacksPerSecond;

        [Header("기본 방어 능력치")]
        [Tooltip("물리 피해를 받을 때 사용하는 기준 방어력입니다.")]
        [Min(0f)]
        [SerializeField] private float physicalDefense;

        [Tooltip("마법 피해를 받을 때 사용하는 기준 방어력입니다.")]
        [Min(0f)]
        [SerializeField] private float magicalDefense;

        [Header("기본 명중·회피 능력치")]
        [Tooltip("공격자의 최종 명중 확률을 계산할 때 사용하는 기준 능력치입니다. 그 자체가 퍼센트 값은 아닙니다.")]
        [Min(0f)]
        [SerializeField] private float accuracy;

        [Tooltip("공격을 회피할 최종 확률을 계산할 때 사용하는 기준 능력치입니다. 그 자체가 퍼센트 값은 아닙니다.")]
        [Min(0f)]
        [SerializeField] private float evasion;

        [Header("기본 이동 능력치")]
        [Tooltip("1초 동안 이동하는 월드 거리의 기준값입니다. 몬스터는 Transform 경로 이동에 사용하며 캐릭터는 향후 이동형 콘텐츠에 사용할 수 있습니다.")]
        [Min(0f)]
        [SerializeField] private float moveSpeed;

        public float MaxHp => maxHp;
        public float PhysicalAttack => physicalAttack;
        public float MagicalAttack => magicalAttack;
        public float BaseAttacksPerSecond => baseAttacksPerSecond;
        public float BaseAttackInterval => baseAttacksPerSecond > 0f ? 1f / baseAttacksPerSecond : float.PositiveInfinity;
        public float PhysicalDefense => physicalDefense;
        public float MagicalDefense => magicalDefense;
        public float Accuracy => accuracy;
        public float Evasion => evasion;
        public float MoveSpeed => moveSpeed;
    }
}