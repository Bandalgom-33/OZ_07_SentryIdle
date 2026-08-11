using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Runtime;
using UnityEngine;

namespace EndlessGuard.Unit.Prototype
{
    [DisallowMultipleComponent]
    public sealed class DamageRuleTest : MonoBehaviour
    {
        [Header("피해 규칙")]
        [Tooltip("검증에 사용할 공통 기본 피해 계산 규칙입니다.")]
        [SerializeField] private DamageRuleSO damageRule;

        [Header("현재 전투 수치 검증")]
        [SerializeField] private float unitAttack = 600f;
        [SerializeField] private float enemyDefense = 200f;
        [SerializeField] private float unitDamage;

        [SerializeField] private float enemyAttack = 900f;
        [SerializeField] private float unitDefense = 300f;
        [SerializeField] private float enemyDamage;

        [Header("방어력 증가 검증")]
        [SerializeField] private float zeroDefenseDamage;
        [SerializeField] private float equalDefenseDamage;
        [SerializeField] private float highDefenseDamage;

        [Header("최종 검증")]
        [SerializeField] private bool unitDamageCorrect;
        [SerializeField] private bool enemyDamageCorrect;
        [SerializeField] private bool defenseCurveCorrect;
        [SerializeField] private bool finalSuccess;

        public void RunTest()
        {
            if (damageRule == null)
            {
                finalSuccess = false;
                return;
            }

            unitDamage = DamageCalculator.Calculate(unitAttack, enemyDefense, damageRule);
            enemyDamage = DamageCalculator.Calculate(enemyAttack, unitDefense, damageRule);

            zeroDefenseDamage = DamageCalculator.Calculate(600f, 0f, damageRule);
            equalDefenseDamage = DamageCalculator.Calculate(600f, 600f, damageRule);
            highDefenseDamage = DamageCalculator.Calculate(600f, 1200f, damageRule);

            unitDamageCorrect = Mathf.Approximately(unitDamage, 400f);
            enemyDamageCorrect = Mathf.Approximately(enemyDamage, 600f);
            defenseCurveCorrect = zeroDefenseDamage > equalDefenseDamage && equalDefenseDamage > highDefenseDamage && highDefenseDamage >= damageRule.MinimumDamage;
            finalSuccess = unitDamageCorrect && enemyDamageCorrect && defenseCurveCorrect;

            Debug.Log($"피해 공식 검증 완료: 캐릭터 피해 {unitDamage:F2}, 몬스터 피해 {enemyDamage:F2}, 방어 0 피해 {zeroDefenseDamage:F2}, 방어 600 피해 {equalDefenseDamage:F2}, 방어 1200 피해 {highDefenseDamage:F2}, 최종 성공 {finalSuccess}", this);
        }
    }
}