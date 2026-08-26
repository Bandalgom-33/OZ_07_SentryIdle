using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [CreateAssetMenu(fileName = "FrontlineCommand", menuName = "Endless Guard/Passive/전선 지휘")]
    public sealed class FrontlineCommandSO : PassiveDataSO
    {
        [Header("전선 지휘 기본값")]
        [Tooltip("아군 소환물이 필드에 1개 이상 존재할 때 자신의 공격속도가 증가하는 비율입니다. 캐릭터별 패시브 개별 수치에서 따로 조정할 수 있습니다.")]
        [Min(0f)]
        [SerializeField] private float attackSpeedBonusPercent = 20f;

        public float AttackSpeedBonusPercent => attackSpeedBonusPercent;

        public override bool TryGetDefaultValue(PassiveValueKey key, out float value)
        {
            if (key == PassiveValueKey.AttackSpeedBonusPercent)
            {
                value = attackSpeedBonusPercent;
                return true;
            }

            value = 0f;
            return false;
        }
    }
}