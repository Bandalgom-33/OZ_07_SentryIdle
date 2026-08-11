using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [CreateAssetMenu(fileName = "AttackSpeed", menuName = "Endless Guard/Passive/공격속도 증가")]
    public sealed class AttackSpeedSO : PassiveDataSO
    {
        [Header("공격속도 증가 조건")]
        [Tooltip("공격속도 증가 효과를 발동시키는 몬스터 크기입니다.")]
        [SerializeField] private EnemySize targetSize = EnemySize.None;

        [Header("공격속도 증가 기본값")]
        [Tooltip("조건에 맞는 몬스터에게 기본 공격이 적중했을 때 증가하는 공격속도 비율입니다. 캐릭터별 패시브 개별 수치에서 따로 조정할 수 있습니다.")]
        [Min(0f)]
        [SerializeField] private float attackSpeedBonusPercent = 50f;

        [Tooltip("공격속도 증가 효과가 유지되는 시간입니다. 단위는 초입니다. 캐릭터별 패시브 개별 수치에서 따로 조정할 수 있습니다.")]
        [Min(0f)]
        [SerializeField] private float durationSeconds = 3f;

        public EnemySize TargetSize => targetSize;
        public float AttackSpeedBonusPercent => attackSpeedBonusPercent;
        public float DurationSeconds => durationSeconds;

        public override bool TryGetDefaultValue(PassiveValueKey key, out float value)
        {
            switch (key)
            {
                case PassiveValueKey.AttackSpeedBonusPercent:
                    value = attackSpeedBonusPercent;
                    return true;

                case PassiveValueKey.DurationSeconds:
                    value = durationSeconds;
                    return true;

                default:
                    value = 0f;
                    return false;
            }
        }
    }
}