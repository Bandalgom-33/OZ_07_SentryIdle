using UnityEngine;

// 천장(Pity) 수치 관리 및 최고 등급 확률 연산기
public class PityEvaluator
{
    // 현재 누적 천장 횟수 (6성/SSR 미획득 누적 수)
    public int CurrentPityStack { get; private set; }

    // 천장 연산기 생성자 (초기 천장 수치 설정)
    public PityEvaluator(int initialPityStack = 0)
    {
        CurrentPityStack = initialPityStack;
    }

    // 데이터 복원 시 천장 수치 재설정 연산
    public void SetPityStack(int stack)
    {
        CurrentPityStack = Mathf.Max(0, stack);
    }

    // 미당첨 시 천장 스택 1 증가 처리
    public void IncreasePity()
    {
        CurrentPityStack++;
    }

    // 최고 등급 획득 시 천장 스택 초기화 처리
    public void ResetPity()
    {
        CurrentPityStack = 0;
    }

    // 현재 천장 스택 구간별(1~49회: 0.1%, 50~99회: +10%선형증가, 100회: 100%확정) 획득 확률 연산
    public float GetTopGradeProbability()
    {
        // 100회 도달 시 100% 확정
        if (CurrentPityStack >= 100)
        {
            return 1.0f;
        }
        
        // 50회 이상부터 스택당 +10%씩 확률 급증 (50회: 10%, 51회: 20% ... 59회: 100%)
        if (CurrentPityStack >= 50)
        {
            return 0.10f + (CurrentPityStack - 50) * 0.10f;
        }

        // 1~49회 구간 기본 확률 0.1%
        return 0.001f;
    }
}
