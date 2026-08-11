using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [CreateAssetMenu(fileName = "Heal", menuName = "Endless Guard/Passive/주기적 아군 회복")]
    public sealed class HealSO : PassiveDataSO
    {
        [Header("아군 회복 기본값")]
        [Tooltip("회복이 발동했을 때 대상 아군 몬스터에게 회복하는 HP입니다. 몬스터별 패시브 개별 수치에서 따로 조정할 수 있습니다.")]
        [Min(0f)]
        [SerializeField] private float healAmount = 500f;

        [Tooltip("아군 회복 효과가 반복해서 발동하는 주기입니다. 단위는 초입니다. 몬스터별 패시브 개별 수치에서 따로 조정할 수 있습니다.")]
        [Min(0.1f)]
        [SerializeField] private float healIntervalSeconds = 5f;

        public float HealAmount => healAmount;
        public float HealIntervalSeconds => healIntervalSeconds;

        public override bool TryGetDefaultValue(PassiveValueKey key, out float value)
        {
            switch (key)
            {
                case PassiveValueKey.HealAmount:
                    value = healAmount;
                    return true;

                case PassiveValueKey.HealIntervalSeconds:
                    value = healIntervalSeconds;
                    return true;

                default:
                    value = 0f;
                    return false;
            }
        }
    }
}