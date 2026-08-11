using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [CreateAssetMenu(fileName = "HeavyArmor", menuName = "Endless Guard/Passive/중갑")]
    public sealed class HeavyArmorSO : PassiveDataSO
    {
        [Header("중갑 방어력 기본값")]
        [Tooltip("중갑 패시브로 증가하는 물리 방어력 비율입니다. 몬스터별 패시브 개별 수치에서 따로 조정할 수 있습니다.")]
        [Min(0f)]
        [SerializeField] private float physicalDefenseBonusPercent = 50f;

        [Tooltip("중갑 패시브로 증가하는 마법 방어력 비율입니다. 몬스터별 패시브 개별 수치에서 따로 조정할 수 있습니다.")]
        [Min(0f)]
        [SerializeField] private float magicalDefenseBonusPercent = 50f;

        [Header("중갑 이동속도 기본값")]
        [Tooltip("중갑 패시브로 감소하는 이동속도 비율입니다. 몬스터별 패시브 개별 수치에서 따로 조정할 수 있습니다.")]
        [Range(0f, 100f)]
        [SerializeField] private float moveSpeedReductionPercent = 20f;

        public float PhysicalDefenseBonusPercent => physicalDefenseBonusPercent;
        public float MagicalDefenseBonusPercent => magicalDefenseBonusPercent;
        public float MoveSpeedReductionPercent => moveSpeedReductionPercent;

        public override bool TryGetDefaultValue(PassiveValueKey key, out float value)
        {
            switch (key)
            {
                case PassiveValueKey.PhysicalDefenseBonusPercent:
                    value = physicalDefenseBonusPercent;
                    return true;

                case PassiveValueKey.MagicalDefenseBonusPercent:
                    value = magicalDefenseBonusPercent;
                    return true;

                case PassiveValueKey.MoveSpeedReductionPercent:
                    value = moveSpeedReductionPercent;
                    return true;

                default:
                    value = 0f;
                    return false;
            }
        }
    }
}