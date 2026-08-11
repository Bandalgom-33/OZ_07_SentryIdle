using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [CreateAssetMenu(fileName = "Explosion", menuName = "Endless Guard/Passive/사망 폭발")]
    public sealed class ExplosionSO : PassiveDataSO
    {
        [Header("사망 폭발 설정")]
        [Tooltip("사망 시 주변 캐릭터에게 적용할 피해 유형입니다.")]
        [SerializeField] private DamageType damageType = DamageType.Physical;

        [Header("사망 폭발 기본값")]
        [Tooltip("사망 시 폭발 범위 안의 캐릭터에게 적용하는 기본 피해량입니다. 몬스터별 패시브 개별 수치에서 따로 조정할 수 있습니다.")]
        [Min(0f)]
        [SerializeField] private float explosionDamage = 1000f;

        [Tooltip("사망 폭발이 적용되는 격자 반경입니다. 몬스터별 패시브 개별 수치에서 따로 조정할 수 있습니다.")]
        [Min(0f)]
        [SerializeField] private float explosionRadiusTiles = 1f;

        public DamageType DamageType => damageType;
        public float ExplosionDamage => explosionDamage;
        public float ExplosionRadiusTiles => explosionRadiusTiles;

        public override bool TryGetDefaultValue(PassiveValueKey key, out float value)
        {
            switch (key)
            {
                case PassiveValueKey.ExplosionDamage:
                    value = explosionDamage;
                    return true;

                case PassiveValueKey.ExplosionRadiusTiles:
                    value = explosionRadiusTiles;
                    return true;

                default:
                    value = 0f;
                    return false;
            }
        }
    }
}