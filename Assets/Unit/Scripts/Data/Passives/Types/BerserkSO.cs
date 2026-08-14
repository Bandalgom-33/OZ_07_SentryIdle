using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [CreateAssetMenu(fileName = "Berserk", menuName = "Endless Guard/Passive/광전")]
    public sealed class BerserkSO : PassiveDataSO
    {
        [Header("광전 물리 공격력 기본값")]
        [Tooltip("잃은 HP 1%마다 증가하는 물리 공격력 비율입니다. 몬스터별 패시브 개별 수치에서 따로 조정할 수 있습니다.")]
        [Min(0f)]
        [SerializeField] private float physicalAttackPerLostHpPercent = 1f;

        [Tooltip("광전 패시브로 증가할 수 있는 물리 공격력의 최대 비율입니다. 몬스터별 패시브 개별 수치에서 따로 조정할 수 있습니다.")]
        [Min(0f)]
        [SerializeField] private float maxPhysicalAttackBonusPercent = 100f;

        [Header("광전 마법 공격력 기본값")]
        [Tooltip("잃은 HP 1%마다 증가하는 마법 공격력 비율입니다. 몬스터별 패시브 개별 수치에서 따로 조정할 수 있습니다.")]
        [Min(0f)]
        [SerializeField] private float magicalAttackPerLostHpPercent = 1f;

        [Tooltip("광전 패시브로 증가할 수 있는 마법 공격력의 최대 비율입니다. 몬스터별 패시브 개별 수치에서 따로 조정할 수 있습니다.")]
        [Min(0f)]
        [SerializeField] private float maxMagicalAttackBonusPercent = 100f;

        public float PhysicalAttackPerLostHpPercent => physicalAttackPerLostHpPercent;
        public float MaxPhysicalAttackBonusPercent => maxPhysicalAttackBonusPercent;
        public float MagicalAttackPerLostHpPercent => magicalAttackPerLostHpPercent;
        public float MaxMagicalAttackBonusPercent => maxMagicalAttackBonusPercent;

        public override bool TryGetDefaultValue(PassiveValueKey key, out float value)
        {
            switch (key)
            {
                case PassiveValueKey.PhysicalAttackPerLostHpPercent:
                    value = physicalAttackPerLostHpPercent;
                    return true;

                case PassiveValueKey.MaxPhysicalAttackBonusPercent:
                    value = maxPhysicalAttackBonusPercent;
                    return true;

                case PassiveValueKey.MagicalAttackPerLostHpPercent:
                    value = magicalAttackPerLostHpPercent;
                    return true;

                case PassiveValueKey.MaxMagicalAttackBonusPercent:
                    value = maxMagicalAttackBonusPercent;
                    return true;

                default:
                    value = 0f;
                    return false;
            }
        }
    }
}