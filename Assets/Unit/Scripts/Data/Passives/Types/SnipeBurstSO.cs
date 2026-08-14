using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [CreateAssetMenu(fileName = "SnipeBurst", menuName = "Endless Guard/Passive/저격 연속 공격")]
    public sealed class SnipeBurstSO : PassiveDataSO
    {
        [Header("저격 연속 공격 기본값")]
        [Tooltip("공격 사거리 안에서 가장 먼 캐릭터를 선택한 뒤 연속으로 공격하는 기본 횟수입니다. 몬스터별 패시브 개별 수치에서 따로 조정할 수 있습니다.")]
        [Min(1)]
        [SerializeField] private int burstAttackCount = 3;

        [Tooltip("연속 공격을 끝낸 뒤 다시 공격 대상을 탐색하기 전에 강제로 이동하는 시간입니다. 단위는 초입니다. 몬스터별 패시브 개별 수치에서 따로 조정할 수 있습니다.")]
        [Min(0f)]
        [SerializeField] private float forcedMoveSeconds = 2f;

        public int BurstAttackCount => burstAttackCount;
        public float ForcedMoveSeconds => forcedMoveSeconds;

        public override bool TryGetDefaultValue(PassiveValueKey key, out float value)
        {
            switch (key)
            {
                case PassiveValueKey.BurstAttackCount:
                    value = burstAttackCount;
                    return true;

                case PassiveValueKey.ForcedMoveSeconds:
                    value = forcedMoveSeconds;
                    return true;

                default:
                    value = 0f;
                    return false;
            }
        }
    }
}