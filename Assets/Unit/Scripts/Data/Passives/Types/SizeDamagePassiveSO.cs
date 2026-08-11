using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [CreateAssetMenu(fileName = "SizeDamagePassive", menuName = "Endless Guard/Passive/크기 대상 추가 피해")]
    public sealed class SizeDamagePassiveSO : PassiveDataSO
    {
        [Header("크기 대상 추가 피해 설정")]
        [Tooltip("추가 피해를 적용할 몬스터 크기입니다. 이 값은 패시브 기능 자체의 조건이므로 캐릭터별 숫자 조정 대상이 아닙니다.")]
        [SerializeField] private EnemySize targetSize = EnemySize.None;

        [Tooltip("새 캐릭터가 이 패시브를 선택할 때 사용할 기본 추천 추가 피해율입니다. 캐릭터별 PassiveTuning에서 별도로 조정할 수 있습니다.")]
        [Min(0f)]
        [SerializeField] private float bonusDamagePercent;

        public EnemySize TargetSize => targetSize;

        public float BonusDamagePercent => bonusDamagePercent;

        public override bool TryGetDefaultValue(PassiveValueKey key, out float value)
        {
            if (key == PassiveValueKey.BonusDamagePercent)
            {
                value = bonusDamagePercent;
                return true;
            }

            value = 0f;
            return false;
        }
    }
}