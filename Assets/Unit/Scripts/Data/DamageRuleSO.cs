using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [CreateAssetMenu(fileName = "DamageRule", menuName = "Endless Guard/Combat/Damage Rule")]
    public sealed class DamageRuleSO : ScriptableObject
    {
        [Header("기본 피해 계산 규칙")]
        [Tooltip("방어력을 기본 피해 계산식에 반영할 때 곱하는 가중치입니다.")]
        [Min(0f)]
        [SerializeField] private float defenseWeight = 1.5f;

        [Tooltip("공격력이 0보다 큰 공격이 명중했을 때 보장할 최소 피해량입니다.")]
        [Min(0f)]
        [SerializeField] private float minimumDamage = 1f;

        public float DefenseWeight => Mathf.Max(0f, defenseWeight);
        public float MinimumDamage => Mathf.Max(0f, minimumDamage);

        private void OnValidate()
        {
            defenseWeight = Mathf.Max(0f, defenseWeight);
            minimumDamage = Mathf.Max(0f, minimumDamage);
        }
    }
}