using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [CreateAssetMenu(fileName = "SummonDefense", menuName = "Endless Guard/Passive/소환물 수 방어력 증가")]
    public sealed class SummonDefenseSO : PassiveDataSO
    {
        [Header("소환물 방어력 증가 기본값")]
        [Tooltip("아군 소환물 1개당 증가하는 물리 방어력 비율입니다. 캐릭터별 패시브 개별 수치에서 따로 조정할 수 있습니다.")]
        [Min(0f)]
        [SerializeField] private float physicalDefensePerSummonPercent = 10f;

        [Tooltip("아군 소환물 1개당 증가하는 마법 방어력 비율입니다. 캐릭터별 패시브 개별 수치에서 따로 조정할 수 있습니다.")]
        [Min(0f)]
        [SerializeField] private float magicalDefensePerSummonPercent = 10f;

        public float PhysicalDefensePerSummonPercent => physicalDefensePerSummonPercent;
        public float MagicalDefensePerSummonPercent => magicalDefensePerSummonPercent;

        public override bool TryGetDefaultValue(PassiveValueKey key, out float value)
        {
            switch (key)
            {
                case PassiveValueKey.PhysicalDefensePerSummonPercent:
                    value = physicalDefensePerSummonPercent;
                    return true;

                case PassiveValueKey.MagicalDefensePerSummonPercent:
                    value = magicalDefensePerSummonPercent;
                    return true;

                default:
                    value = 0f;
                    return false;
            }
        }
    }
}