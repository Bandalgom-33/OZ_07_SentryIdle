using UnityEngine;

// 가챠 천장(Pity) 스택 누적 및 구간별 최고 등급 확률을 연산하는 순수 계산 클래스
// GachaConfigSO에 정의된 기획 데이터를 주입받아 소프트/하드 천장 확률을 단일 진실 공급원(SSOT) 기준으로 산출함
public class PityEvaluator
{
    private readonly GachaConfigSO _config;

    // 현재 누적된 천장 뽑기 횟수
    public int CurrentPityStack { get; private set; }

    // 천장 연산기 생성자 (설정 SO 및 초기 스택 주입)
    public PityEvaluator(GachaConfigSO config, int initialPityStack = 0)
    {
        _config = config;
        CurrentPityStack = Mathf.Max(0, initialPityStack);
    }

    // 천장 스택 직접 설정 연산 (세이브 로드 시 활용)
    public void SetPityStack(int stack)
    {
        CurrentPityStack = Mathf.Max(0, stack);
    }

    // 천장 스택 단일 증가 연산 (매 1회 뽑기 시도마다 호출)
    public void IncreasePity()
    {
        CurrentPityStack++;
    }

    // 천장 스택 초기화 연산 (6성 최고 등급 획득 시 0으로 리셋)
    public void ResetPity()
    {
        CurrentPityStack = 0;
    }

    // 현재 누적 스택에 따른 6성 최고 등급 당첨 확률 계산 연산
    public float GetTopGradeProbability()
    {
        // GachaConfigSO 에셋이 연결되어 있는 경우 기획 설정값을 우선 적용
        if (_config != null)
        {
            if (CurrentPityStack >= _config.HardPityThreshold)
            {
                return _config.HardPityRate; // 하드 천장: 100% (1.0f)
            }

            if (CurrentPityStack >= _config.SoftPityThreshold)
            {
                return _config.SoftPityRate; // 소프트 천장: 10% (0.10f)
            }

            return _config.BaseSixStarRate;   // 기본 확률: 0.1% (0.001f)
        }

        // 설정 에셋이 누락된 경우 안전 Fallback 상수 적용
        if (CurrentPityStack >= 100) return 1.0f;
        if (CurrentPityStack >= 50) return 0.10f;
        return 0.001f;
    }
}
