using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [CreateAssetMenu(fileName = "LostHpAttack", menuName = "Endless Guard/Passive/잃은 HP 공격력 증가")]
    public sealed class LostHpAttackSO : PassiveDataSO
    {
        [Header("잃은 HP 공격력 증가 기본값")]
        [Tooltip("잃은 HP 1%마다 증가하는 물리 공격력 비율입니다. 캐릭터별 패시브 개별 수치에서 별도로 조정할 수 있습니다.")]
        [Min(0f)]
        [SerializeField] private float physicalAttackPerLostHpPercent = 1f;

        [Tooltip("이 패시브로 증가할 수 있는 물리 공격력의 최대 비율입니다. 캐릭터별 패시브 개별 수치에서 별도로 조정할 수 있습니다.")]
        [Min(0f)]
        [SerializeField] private float maxPhysicalAttackBonusPercent = 100f;

        public float PhysicalAttackPerLostHpPercent => physicalAttackPerLostHpPercent;

        public float MaxPhysicalAttackBonusPercent => maxPhysicalAttackBonusPercent;

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

                default:
                    value = 0f;
                    return false;
            }
        }
    }
}