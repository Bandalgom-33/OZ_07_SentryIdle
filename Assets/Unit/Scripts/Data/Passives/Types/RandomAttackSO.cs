using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [CreateAssetMenu(fileName = "RandomAttack", menuName = "Endless Guard/Passive/무작위 다중 공격")]
    public sealed class RandomAttackSO : PassiveDataSO
    {
        [Header("무작위 다중 공격 기본값")]
        [Tooltip("기본 공격 시 필드에 살아있는 유효한 캐릭터 중 중복 없이 무작위로 선택할 기본 공격 대상 수입니다. 몬스터별 패시브 개별 수치에서 따로 조정할 수 있습니다.")]
        [Min(1)]
        [SerializeField] private int randomTargetCount = 3;

        public int RandomTargetCount => randomTargetCount;

        public override bool TryGetDefaultValue(PassiveValueKey key, out float value)
        {
            if (key == PassiveValueKey.RandomTargetCount)
            {
                value = randomTargetCount;
                return true;
            }

            value = 0f;
            return false;
        }
    }
}