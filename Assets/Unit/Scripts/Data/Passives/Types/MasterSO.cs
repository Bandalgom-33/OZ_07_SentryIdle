using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [CreateAssetMenu(fileName = "Master", menuName = "Endless Guard/Passive/마스터")]
    public sealed class MasterSO : PassiveDataSO
    {
        [Header("마스터 능력치 설정")]
        [Tooltip("마스터 패시브로 증가시킬 전투 능력치입니다.")]
        [SerializeField] private PassiveStatType statType = PassiveStatType.None;

        [Tooltip("지정한 전투 능력치의 기본 증가 비율입니다. 캐릭터별 패시브 개별 수치에서 따로 조정할 수 있습니다.")]
        [Min(0f)]
        [SerializeField] private float statBonusPercent = 100f;

        public PassiveStatType StatType => statType;
        public float StatBonusPercent => statBonusPercent;

        public override bool TryGetDefaultValue(PassiveValueKey key, out float value)
        {
            if (key == PassiveValueKey.StatBonusPercent)
            {
                value = statBonusPercent;
                return true;
            }

            value = 0f;
            return false;
        }
    }
}