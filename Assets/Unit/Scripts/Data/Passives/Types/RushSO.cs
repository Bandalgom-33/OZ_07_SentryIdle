using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [CreateAssetMenu(fileName = "Rush", menuName = "Endless Guard/Passive/첫 저지 전 이동속도 증가")]
    public sealed class RushSO : PassiveDataSO
    {
        [Header("돌격 이동속도 기본값")]
        [Tooltip("몬스터가 처음으로 저지되기 전까지 증가하는 이동속도 비율입니다. 몬스터별 패시브 개별 수치에서 따로 조정할 수 있습니다.")]
        [Min(0f)]
        [SerializeField] private float bonusMoveSpeedPercent = 40f;

        public float BonusMoveSpeedPercent => bonusMoveSpeedPercent;

        public override bool TryGetDefaultValue(PassiveValueKey key, out float value)
        {
            if (key == PassiveValueKey.BonusMoveSpeedPercent)
            {
                value = bonusMoveSpeedPercent;
                return true;
            }

            value = 0f;
            return false;
        }
    }
}