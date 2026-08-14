using UnityEngine;

public class PityEvaluator
{
    public int CurrentPityStack { get; private set; }

    // 천장 연산기 생성자
    public PityEvaluator(int initialPityStack = 0)
    {
        CurrentPityStack = initialPityStack;
    }

    // 천장 스택 설정 연산
    public void SetPityStack(int stack)
    {
        CurrentPityStack = Mathf.Max(0, stack);
    }

    // 천장 스택 증가 연산
    public void IncreasePity()
    {
        CurrentPityStack++;
    }

    // 천장 스택 초기화 연산
    public void ResetPity()
    {
        CurrentPityStack = 0;
    }

    // 최고 등급 확률 연산
    public float GetTopGradeProbability()
    {
        if (CurrentPityStack >= 100)
        {
            return 1.0f;
        }
        
        if (CurrentPityStack >= 50)
        {
            return 0.10f + (CurrentPityStack - 50) * 0.10f;
        }

        return 0.001f;
    }
}
