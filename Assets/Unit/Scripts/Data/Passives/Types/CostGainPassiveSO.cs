using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [CreateAssetMenu(fileName = "CostGainPassive", menuName = "Endless Guard/Passive/소환 코스트 획득")]
    public sealed class CostGainPassiveSO : PassiveDataSO
    {
        [Header("소환 코스트 획득 설정")]
        [Tooltip("소환 코스트를 획득하는 발동 조건입니다.")]
        [SerializeField] private CostGainTrigger trigger = CostGainTrigger.None;

        [Tooltip("새 캐릭터가 이 패시브를 선택할 때 사용하는 기본 추천 소환 코스트 획득량입니다. 캐릭터별 PassiveTuning에서 별도로 조정할 수 있습니다.")]
        [Min(0)]
        [SerializeField] private int summonCostGain;

        public CostGainTrigger Trigger => trigger;

        public int SummonCostGain => summonCostGain;

        public override bool TryGetDefaultValue(PassiveValueKey key, out float value)
        {
            if (key == PassiveValueKey.SummonCostGain)
            {
                value = summonCostGain;
                return true;
            }

            value = 0f;
            return false;
        }
    }
}