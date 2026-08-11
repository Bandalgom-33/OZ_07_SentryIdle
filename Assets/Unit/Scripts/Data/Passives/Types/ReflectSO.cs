using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [CreateAssetMenu(fileName = "Reflect", menuName = "Endless Guard/Passive/피해 반사")]
    public sealed class ReflectSO : PassiveDataSO
    {
        [Header("피해 반사 기본값")]
        [Tooltip("캐릭터에게 실제로 받은 피해량 중 공격자에게 되돌려 주는 비율입니다. 몬스터별 패시브 개별 수치에서 따로 조정할 수 있습니다.")]
        [Min(0f)]
        [SerializeField] private float damageReflectPercent = 30f;

        public float DamageReflectPercent => damageReflectPercent;

        public override bool TryGetDefaultValue(PassiveValueKey key, out float value)
        {
            if (key == PassiveValueKey.DamageReflectPercent)
            {
                value = damageReflectPercent;
                return true;
            }

            value = 0f;
            return false;
        }
    }
}