using System;
using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [Serializable]
    public sealed class AttackSettings
    {
        [Header("기본 공격 규칙")]
        [Tooltip("기본 공격을 하지 않는지, 근거리 또는 원거리 방식인지 설정합니다.")]
        [SerializeField] private AttackMode attackMode = AttackMode.None;

        [Tooltip("기본 공격이 물리 피해인지 마법 피해인지 설정합니다.")]
        [SerializeField] private DamageType damageType = DamageType.None;

        [Tooltip("기본 공격이 지상, 공중 또는 양쪽 모두를 대상으로 할 수 있는지 설정합니다.")]
        [SerializeField] private AttackTarget attackTarget = AttackTarget.None;

        [Tooltip("기본 공격으로 대상을 탐색하고 공격할 수 있는 기준 월드 거리입니다.")]
        [Min(0f)]
        [SerializeField] private float attackRange;

        [Tooltip("한 번의 기본 공격으로 동시에 공격할 수 있는 최대 대상 수입니다.")]
        [Min(0)]
        [SerializeField] private int targetCount;

        [Tooltip("기본 공격 타일 범위는 현재 Facing 방향에 맞춰 사용하며, 전투 중 Facing을 고정할지 유효한 대상 방향에 따라 자동 변경할지 설정합니다.")]
        [SerializeField] private AttackRangeRotationMode rangeRotationMode = AttackRangeRotationMode.Fixed;

        [Tooltip("공격 주체의 위치를 기준으로 기본 공격이 가능한 상대 타일 좌표를 설정합니다.")]
        [SerializeField] private BasicAttackRangeData basicAttackRange = new BasicAttackRangeData();

        public AttackMode AttackMode => attackMode;
        public DamageType DamageType => damageType;
        public AttackTarget AttackTarget => attackTarget;
        public float AttackRange => attackRange;
        public int TargetCount => targetCount;
        public AttackRangeRotationMode RangeRotationMode => rangeRotationMode;
        public BasicAttackRangeData BasicAttackRange => basicAttackRange;
    }
}