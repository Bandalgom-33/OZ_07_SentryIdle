using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [CreateAssetMenu(fileName = "BlockGauge", menuName = "Endless Guard/Passive/저지 시 스킬게이지 획득")]
    public sealed class BlockGaugeSO : PassiveDataSO
    {
        [Header("저지 스킬게이지 기본값")]
        [Tooltip("새로운 몬스터를 저지하는 데 성공할 때 획득하는 스킬게이지입니다. 캐릭터별 패시브 개별 수치에서 따로 조정할 수 있습니다.")]
        [Min(0f)]
        [SerializeField] private float skillGaugeGain = 10f;

        public float SkillGaugeGain => skillGaugeGain;

        public override bool TryGetDefaultValue(PassiveValueKey key, out float value)
        {
            if (key == PassiveValueKey.SkillGaugeGain)
            {
                value = skillGaugeGain;
                return true;
            }

            value = 0f;
            return false;
        }
    }
}