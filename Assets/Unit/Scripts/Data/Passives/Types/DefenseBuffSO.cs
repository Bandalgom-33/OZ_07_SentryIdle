using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [CreateAssetMenu(fileName = "DefenseBuff", menuName = "Endless Guard/Passive/방어력 증가")]
    public sealed class DefenseBuffSO : PassiveDataSO
    {
        [Header("방어력 증가 조건")]
        [Tooltip("물리·마법 방어력 증가 효과가 활성화되는 조건입니다.")]
        [SerializeField] private DefenseBuffTrigger trigger = DefenseBuffTrigger.None;

        [Header("방어력 증가 기본값")]
        [Tooltip("조건을 만족하는 동안 증가하는 물리 방어력 비율입니다. 캐릭터별 패시브 개별 수치에서 따로 조정할 수 있습니다.")]
        [Min(0f)]
        [SerializeField] private float physicalDefenseBonusPercent = 100f;

        [Tooltip("조건을 만족하는 동안 증가하는 마법 방어력 비율입니다. 캐릭터별 패시브 개별 수치에서 따로 조정할 수 있습니다.")]
        [Min(0f)]
        [SerializeField] private float magicalDefenseBonusPercent = 100f;

        [Tooltip("회피 성공 조건에서 방어력 증가 효과가 유지되는 시간입니다. 단위는 초입니다. 저지 중 조건에서는 사용하지 않습니다.")]
        [Min(0f)]
        [SerializeField] private float durationSeconds = 3f;

        public DefenseBuffTrigger Trigger => trigger;
        public float PhysicalDefenseBonusPercent => physicalDefenseBonusPercent;
        public float MagicalDefenseBonusPercent => magicalDefenseBonusPercent;
        public float DurationSeconds => durationSeconds;

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

                case PassiveValueKey.DurationSeconds:
                    if (trigger == DefenseBuffTrigger.EvadeSuccess)
                    {
                        value = durationSeconds;
                        return true;
                    }

                    break;
            }

            value = 0f;
            return false;
        }
    }
}