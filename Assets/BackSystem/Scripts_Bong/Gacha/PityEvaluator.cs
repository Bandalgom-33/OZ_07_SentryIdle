using UnityEngine;

// 가챠 천장 관리 클래스
public class PityEvaluator
{
    // 현재 누적 스택
    public int CurrentPityStack { get; private set; }

    public PityEvaluator(int initialPityStack = 0)
    {
        CurrentPityStack = initialPityStack;
    }

    // 저장 기록 적용
    public void SetPityStack(int stack)
    {
        CurrentPityStack = Mathf.Max(0, stack);
    }
    // 스택 누적
    public void IncreasePity()
    {
        CurrentPityStack++;
    }

    // 스택 초기화
    public void ResetPity()
    {
        CurrentPityStack = 0;
    }

    // 스택에 따른 확률 계산
    public float GetTopGradeProbability()
    {
        // 100회 도달 시 100% 확정
        if (CurrentPityStack >= 100)
        {
            return 1.0f;
        }
        
        // 50이상일 때 확률 10%
        if (CurrentPityStack >= 50)
        {
            return 0.10f ;
        }

        // 1~49회 구간 기본 확률 0.1%
        return 0.001f;
    }
}
