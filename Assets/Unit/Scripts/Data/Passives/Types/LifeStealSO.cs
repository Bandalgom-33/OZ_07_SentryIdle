using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [CreateAssetMenu(fileName = "LifeSteal", menuName = "Endless Guard/Passive/흡혈")]
    public sealed class LifeStealSO : PassiveDataSO
    {
        [Header("흡혈 기본값")]
        [Tooltip("기본 공격으로 실제 적용한 피해량 중 자신의 HP로 회복하는 비율입니다. 몬스터별 패시브 개별 수치에서 따로 조정할 수 있습니다.")]
        [Range(0f, 100f)]
        [SerializeField] private float lifeStealPercent = 30f;

        public float LifeStealPercent => lifeStealPercent;

        public override bool TryGetDefaultValue(PassiveValueKey key, out float value)
        {
            if (key == PassiveValueKey.LifeStealPercent)
            {
                value = lifeStealPercent;
                return true;
            }

            value = 0f;
            return false;
        }
    }
}