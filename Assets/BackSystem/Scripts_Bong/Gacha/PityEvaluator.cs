using UnityEngine;

// 가챠 천장(Pity) 스택 누적 및 구간별 6성 확률을 연산하는 평가 클래스
public class PityEvaluator
{
    // 현재 누적된 천장 스택 횟수 (0 ~ 100)
    public int CurrentPityStack { get; private set; }

    // 천장 연산기 생성자
    public PityEvaluator(int initialPityStack = 0)
    {
        CurrentPityStack = Mathf.Max(0, initialPityStack);
    }

    // 세이브 데이터 로드 등을 위한 천장 스택 직접 설정 메서드
    public void SetPityStack(int stack)
    {
        CurrentPityStack = Mathf.Max(0, stack);
    }

    // 1회 뽑기 실행 시 천장 스택 1 증가
    public void IncreasePity()
    {
        CurrentPityStack++;
    }

    // 6성 캐릭터 획득 시 천장 스택을 0으로 초기화
    public void ResetPity()
    {
        CurrentPityStack = 0;
    }

    // 기획서 4.2.1 2) 명세에 따른 현재 천장 스택 구간별 6성(최고 등급) 확률 계산 메서드
    public float GetTopGradeProbability()
    {
        // 100회 도달 시 (Hard Pity): 100% 확률로 6성 확정 지급
        if (CurrentPityStack >= 100)
        {
            return 1.0f;
        }

        // 50 ~ 99회 구간: 6성 확률이 10.0%로 대폭 급증
        if (CurrentPityStack >= 50)
        {
            return 0.10f;
        }

        // 1 ~ 49회 구간: 6성 기본 확률 0.1%
        return 0.001f;
    }
}

