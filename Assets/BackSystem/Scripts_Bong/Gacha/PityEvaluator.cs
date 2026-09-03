using UnityEngine;

public class PityEvaluator
{
    private readonly GachaConfigSO _config;

    public int CurrentPityStack { get; private set; }

    // 천장 연산기 인스턴스 생성
    public PityEvaluator(GachaConfigSO config, int initialPityStack = 0)
    {
        _config = config;
        CurrentPityStack = Mathf.Max(0, initialPityStack);
    }

    // 천장 누적 스택 수치 지정
    public void SetPityStack(int stack)
    {
        CurrentPityStack = Mathf.Max(0, stack);
    }

    // 천장 누적 스택 1 증가
    public void IncreasePity()
    {
        CurrentPityStack++;
    }

    // 천장 누적 스택 0으로 초기화
    public void ResetPity()
    {
        CurrentPityStack = 0;
    }

    // 현재 누적 스택 기반 6성 최고 등급 확률 연산
    public float GetTopGradeProbability()
    {
        int softThreshold = _config != null ? _config.SoftPityThreshold : 50;
        int hardThreshold = _config != null ? _config.HardPityThreshold : 100;
        float baseRate = _config != null ? _config.BaseSixStarRate : 0.001f;
        float hardRate = _config != null ? _config.HardPityRate : 1.0f;

        if (CurrentPityStack >= hardThreshold)
        {
            return hardRate;
        }

        if (CurrentPityStack >= softThreshold)
        {
            float t = (float)(CurrentPityStack - softThreshold) / (hardThreshold - softThreshold);
            return Mathf.Lerp(baseRate, hardRate, t);
        }

        return baseRate;
    }
}
