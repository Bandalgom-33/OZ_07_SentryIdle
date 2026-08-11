using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [CreateAssetMenu(fileName = "DefenseAura", menuName = "Endless Guard/Passive/방어 오라")]
    public sealed class DefenseAuraSO : PassiveDataSO
    {
        [Header("방어 오라 기본값")]
        [Tooltip("비호자가 살아있는 동안 아군 몬스터에게 적용하는 물리 방어력 증가 비율입니다. 몬스터별 패시브 개별 수치에서 따로 조정할 수 있습니다.")]
        [Min(0f)]
        [SerializeField] private float physicalDefenseBonusPercent = 30f;

        [Tooltip("비호자가 살아있는 동안 아군 몬스터에게 적용하는 마법 방어력 증가 비율입니다. 몬스터별 패시브 개별 수치에서 따로 조정할 수 있습니다.")]
        [Min(0f)]
        [SerializeField] private float magicalDefenseBonusPercent = 30f;

        public float PhysicalDefenseBonusPercent => physicalDefenseBonusPercent;
        public float MagicalDefenseBonusPercent => magicalDefenseBonusPercent;

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

                default:
                    value = 0f;
                    return false;
            }
        }
    }
}