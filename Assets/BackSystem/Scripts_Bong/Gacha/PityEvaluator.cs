using UnityEngine;

// 가챠 천장(Pity) 스택 누적 및 구간별 최고 등급 확률을 연산하는 평가 클래스
public class PityEvaluator
{
    private const int SoftPityThreshold = 50;
    private const int HardPityThreshold = 100;
    private const float BaseRate = 0.001f;     // 0.1%
    private const float SoftPityRate = 0.10f;   // 10.0%
    private const float HardPityRate = 1.0f;    // 100%

    public int CurrentPityStack { get; private set; }

    // 천장 연산기 생성자
    public PityEvaluator(int initialPityStack = 0)
    {
        CurrentPityStack = Mathf.Max(0, initialPityStack);
    }

    // 천장 스택 직접 설정 연산
    public void SetPityStack(int stack)
    {
        CurrentPityStack = Mathf.Max(0, stack);
    }

    // 천장 스택 단일 증가 연산
    public void IncreasePity()
    {
        CurrentPityStack++;
    }

    // 천장 스택 초기화 연산
    public void ResetPity()
    {
        CurrentPityStack = 0;
    }

    // 현재 스택 구간별 최고 등급 확률 계산 연산
    public float GetTopGradeProbability()
    {
        if (CurrentPityStack >= HardPityThreshold)
        {
            return HardPityRate;
        }

        if (CurrentPityStack >= SoftPityThreshold)
        {
            return SoftPityRate;
        }

        return BaseRate;
    }
}
