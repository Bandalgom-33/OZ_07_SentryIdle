using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [CreateAssetMenu(fileName = "AllyAid", menuName = "Endless Guard/Passive/아군 무작위 지원")]
    public sealed class AllyAidSO : PassiveDataSO
    {
        [Header("아군 지원 기본값")]
        [Tooltip("보호막 효과가 선택됐을 때 아군에게 부여하는 기본 보호막량입니다. 캐릭터별 패시브 개별 수치에서 따로 조정할 수 있습니다.")]
        [Min(0f)]
        [SerializeField] private float shieldAmount = 500f;

        [Tooltip("HP 회복 효과가 선택됐을 때 아군에게 회복하는 기본 HP입니다. 캐릭터별 패시브 개별 수치에서 따로 조정할 수 있습니다.")]
        [Min(0f)]
        [SerializeField] private float healAmount = 500f;

        [Tooltip("스킬게이지 회복 효과가 선택됐을 때 아군에게 부여하는 기본 스킬게이지입니다. 캐릭터별 패시브 개별 수치에서 따로 조정할 수 있습니다.")]
        [Min(0f)]
        [SerializeField] private float skillGaugeGain = 20f;

        public float ShieldAmount => shieldAmount;
        public float HealAmount => healAmount;
        public float SkillGaugeGain => skillGaugeGain;

        public override bool TryGetDefaultValue(PassiveValueKey key, out float value)
        {
            switch (key)
            {
                case PassiveValueKey.ShieldAmount:
                    value = shieldAmount;
                    return true;

                case PassiveValueKey.HealAmount:
                    value = healAmount;
                    return true;

                case PassiveValueKey.SkillGaugeGain:
                    value = skillGaugeGain;
                    return true;

                default:
                    value = 0f;
                    return false;
            }
        }
    }
}