using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [CreateAssetMenu(fileName = "Weak", menuName = "Endless Guard/Passive/방어력 감소")]
    public sealed class WeakSO : PassiveDataSO
    {
        [Header("방어력 감소 기본값")]
        [Tooltip("기본 공격 적중 시 대상의 물리 방어력을 감소시키는 비율입니다. 캐릭터별 패시브 개별 수치에서 따로 조정할 수 있습니다.")]
        [Range(0f, 100f)]
        [SerializeField] private float physicalDefenseReductionPercent = 30f;

        [Tooltip("기본 공격 적중 시 대상의 마법 방어력을 감소시키는 비율입니다. 캐릭터별 패시브 개별 수치에서 따로 조정할 수 있습니다.")]
        [Range(0f, 100f)]
        [SerializeField] private float magicalDefenseReductionPercent = 30f;

        [Tooltip("방어력 감소 효과가 유지되는 시간입니다. 단위는 초입니다. 캐릭터별 패시브 개별 수치에서 따로 조정할 수 있습니다.")]
        [Min(0f)]
        [SerializeField] private float durationSeconds = 3f;

        public float PhysicalDefenseReductionPercent => physicalDefenseReductionPercent;
        public float MagicalDefenseReductionPercent => magicalDefenseReductionPercent;
        public float DurationSeconds => durationSeconds;

        public override bool TryGetDefaultValue(PassiveValueKey key, out float value)
        {
            switch (key)
            {
                case PassiveValueKey.PhysicalDefenseReductionPercent:
                    value = physicalDefenseReductionPercent;
                    return true;

                case PassiveValueKey.MagicalDefenseReductionPercent:
                    value = magicalDefenseReductionPercent;
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