using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [CreateAssetMenu(fileName = "Snipe", menuName = "Endless Guard/Passive/거리 비례 추가 피해")]
    public sealed class SnipeSO : PassiveDataSO
    {
        [Header("저격 대상 조건")]
        [Tooltip("저격수 패시브의 추가 피해를 적용할 몬스터 크기입니다.")]
        [SerializeField] private EnemySize targetSize = EnemySize.Large;

        [Header("저격 추가 피해 기본값")]
        [Tooltip("조건에 맞는 몬스터에게 기본적으로 추가되는 최종 피해 비율입니다. 캐릭터별 패시브 개별 수치에서 따로 조정할 수 있습니다.")]
        [Min(0f)]
        [SerializeField] private float bonusDamagePercent = 50f;

        [Tooltip("공격자와 대상 사이의 거리 1당 추가되는 최종 피해 비율입니다. 캐릭터별 패시브 개별 수치에서 따로 조정할 수 있습니다.")]
        [Min(0f)]
        [SerializeField] private float damagePerDistancePercent = 10f;

        [Tooltip("거리로 얻을 수 있는 추가 피해 비율의 최대치입니다. 캐릭터별 패시브 개별 수치에서 따로 조정할 수 있습니다.")]
        [Min(0f)]
        [SerializeField] private float maxDistanceDamagePercent = 50f;

        public EnemySize TargetSize => targetSize;
        public float BonusDamagePercent => bonusDamagePercent;
        public float DamagePerDistancePercent => damagePerDistancePercent;
        public float MaxDistanceDamagePercent => maxDistanceDamagePercent;

        public override bool TryGetDefaultValue(PassiveValueKey key, out float value)
        {
            switch (key)
            {
                case PassiveValueKey.BonusDamagePercent:
                    value = bonusDamagePercent;
                    return true;

                case PassiveValueKey.DamagePerDistancePercent:
                    value = damagePerDistancePercent;
                    return true;

                case PassiveValueKey.MaxDistanceDamagePercent:
                    value = maxDistanceDamagePercent;
                    return true;

                default:
                    value = 0f;
                    return false;
            }
        }
    }
}