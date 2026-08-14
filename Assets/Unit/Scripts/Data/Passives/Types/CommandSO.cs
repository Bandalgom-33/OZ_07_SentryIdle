using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [CreateAssetMenu(fileName = "Command", menuName = "Endless Guard/Passive/지휘 오라")]
    public sealed class CommandSO : PassiveDataSO
    {
        [Header("지휘 오라 기본값")]
        [Tooltip("지휘관이 살아있는 동안 아군 몬스터에게 적용하는 공격력 증가 비율입니다. 몬스터별 패시브 개별 수치에서 따로 조정할 수 있습니다.")]
        [Min(0f)]
        [SerializeField] private float attackBonusPercent = 30f;

        [Tooltip("지휘관이 살아있는 동안 아군 몬스터에게 적용하는 공격속도 증가 비율입니다. 몬스터별 패시브 개별 수치에서 따로 조정할 수 있습니다.")]
        [Min(0f)]
        [SerializeField] private float attackSpeedBonusPercent = 20f;

        public float AttackBonusPercent => attackBonusPercent;
        public float AttackSpeedBonusPercent => attackSpeedBonusPercent;

        public override bool TryGetDefaultValue(PassiveValueKey key, out float value)
        {
            switch (key)
            {
                case PassiveValueKey.AttackBonusPercent:
                    value = attackBonusPercent;
                    return true;

                case PassiveValueKey.AttackSpeedBonusPercent:
                    value = attackSpeedBonusPercent;
                    return true;

                default:
                    value = 0f;
                    return false;
            }
        }
    }
}