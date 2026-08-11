using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [CreateAssetMenu(fileName = "Slow", menuName = "Endless Guard/Passive/이동속도 감소")]
    public sealed class SlowSO : PassiveDataSO
    {
        [Header("이동속도 감소 기본값")]
        [Tooltip("기본 공격 적중 시 대상의 이동속도를 감소시키는 비율입니다. 캐릭터별 패시브 개별 수치에서 따로 조정할 수 있습니다.")]
        [Range(0f, 100f)]
        [SerializeField] private float moveSpeedReductionPercent = 30f;

        [Tooltip("이동속도 감소 효과가 유지되는 시간입니다. 단위는 초입니다. 캐릭터별 패시브 개별 수치에서 따로 조정할 수 있습니다.")]
        [Min(0f)]
        [SerializeField] private float durationSeconds = 3f;

        public float MoveSpeedReductionPercent => moveSpeedReductionPercent;
        public float DurationSeconds => durationSeconds;

        public override bool TryGetDefaultValue(PassiveValueKey key, out float value)
        {
            switch (key)
            {
                case PassiveValueKey.MoveSpeedReductionPercent:
                    value = moveSpeedReductionPercent;
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