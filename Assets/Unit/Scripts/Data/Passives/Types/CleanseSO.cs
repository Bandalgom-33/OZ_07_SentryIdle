using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [CreateAssetMenu(fileName = "Cleanse", menuName = "Endless Guard/Passive/주기적 상태이상 정화")]
    public sealed class CleanseSO : PassiveDataSO
    {
        [Header("상태이상 정화 기본값")]
        [Tooltip("아군 몬스터의 상태이상을 제거하는 효과가 반복해서 발동하는 주기입니다. 단위는 초입니다. 몬스터별 패시브 개별 수치에서 따로 조정할 수 있습니다.")]
        [Min(0.1f)]
        [SerializeField] private float cleanseIntervalSeconds = 5f;

        public float CleanseIntervalSeconds => cleanseIntervalSeconds;

        public override bool TryGetDefaultValue(PassiveValueKey key, out float value)
        {
            if (key == PassiveValueKey.CleanseIntervalSeconds)
            {
                value = cleanseIntervalSeconds;
                return true;
            }

            value = 0f;
            return false;
        }
    }
}