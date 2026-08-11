using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [CreateAssetMenu(fileName = "AttackSlow", menuName = "Endless Guard/Passive/공격속도 감소")]
    public sealed class AttackSlowSO : PassiveDataSO
    {
        [Header("공격속도 감소 기본값")]
        [Tooltip("기본 공격 적중 시 대상 캐릭터의 공격속도를 감소시키는 비율입니다. 몬스터별 패시브 개별 수치에서 따로 조정할 수 있습니다.")]
        [Range(0f, 100f)]
        [SerializeField] private float attackSpeedReductionPercent = 30f;

        [Tooltip("공격속도 감소 효과가 유지되는 시간입니다. 단위는 초입니다. 몬스터별 패시브 개별 수치에서 따로 조정할 수 있습니다.")]
        [Min(0f)]
        [SerializeField] private float durationSeconds = 5f;

        public float AttackSpeedReductionPercent => attackSpeedReductionPercent;
        public float DurationSeconds => durationSeconds;

        public override bool TryGetDefaultValue(PassiveValueKey key, out float value)
        {
            switch (key)
            {
                case PassiveValueKey.AttackSpeedReductionPercent:
                    value = attackSpeedReductionPercent;
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