using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Runtime;
using UnityEngine;

namespace EndlessGuard.Unit.Prototype
{
    [DisallowMultipleComponent]
    public sealed class HitTest : MonoBehaviour
    {
        [Header("명중 규칙")]
        [Tooltip("검증에 사용할 공통 명중·회피 규칙입니다.")]
        [SerializeField] private HitRuleSO hitRule;

        [Header("검증 능력치")]
        [Tooltip("공격자의 명중력입니다.")]
        [Min(0f)]
        [SerializeField] private float accuracy = 100f;

        [Tooltip("대상의 회피력입니다.")]
        [Min(0f)]
        [SerializeField] private float evasion = 100f;

        [Header("검증 설정")]
        [Tooltip("명중 판정을 반복할 횟수입니다.")]
        [Min(1)]
        [SerializeField] private int trialCount = 10000;

        [Tooltip("같은 조건에서 동일한 결과를 재현하기 위한 랜덤 시드입니다.")]
        [SerializeField] private int randomSeed = 12345;

        [Header("검증 결과")]
        [SerializeField] private float calculatedHitChance;
        [SerializeField] private int hitCount;
        [SerializeField] private int missCount;
        [SerializeField] private float actualHitRate;
        [SerializeField] private float difference;

        public void RunTest()
        {
            if (hitRule == null || trialCount <= 0)
            {
                return;
            }

            calculatedHitChance = HitCalculator.CalculatePercent(accuracy, evasion, hitRule);
            hitCount = 0;
            missCount = 0;

            Random.State previousState = Random.state;
            Random.InitState(randomSeed);

            for (int i = 0; i < trialCount; i++)
            {
                if (HitCalculator.Roll(calculatedHitChance))
                {
                    hitCount++;
                }
                else
                {
                    missCount++;
                }
            }

            Random.state = previousState;

            actualHitRate = hitCount / (float)trialCount * 100f;
            difference = actualHitRate - calculatedHitChance;

            Debug.Log($"명중률 검증 완료: 계산 {calculatedHitChance:F2}%, 실제 {actualHitRate:F2}%, 명중 {hitCount}, MISS {missCount}, 차이 {difference:F2}%p", this);
        }
    }
}