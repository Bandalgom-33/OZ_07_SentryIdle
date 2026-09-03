using System;
using UnityEngine;

[CreateAssetMenu(fileName = "GachaConfig", menuName = "EndlessGuard/Gacha/GachaConfig", order = 1)]
public class GachaConfigSO : ScriptableObject
{
    #region 가챠 다이아 소모 비용 설정

    [Header("--- 가챠 다이아 비용 설정 ---")]
    [Tooltip("1회 단차 가챠 소모 다이아 수량")]
    [SerializeField] private int singleDrawCost = 300;

    [Tooltip("10회 연속 가챠 소모 다이아 수량")]
    [SerializeField] private int tenDrawCost = 3000;

    #endregion

    #region 등급별 기본 확률 가중치 설정

    [Header("--- 등급별 기본 확률 가중치 (총합 100.0%) ---")]
    [Tooltip("1성 유닛 기본 가중치 (%)")]
    [SerializeField] private float oneStarWeight = 30.0f;

    [Tooltip("2성 유닛 기본 가중치 (%)")]
    [SerializeField] private float twoStarWeight = 30.0f;

    [Tooltip("3성 유닛 기본 가중치 (%)")]
    [SerializeField] private float threeStarWeight = 25.0f;

    [Tooltip("4성 유닛 기본 가중치 (%)")]
    [SerializeField] private float fourStarWeight = 12.0f;

    [Tooltip("5성 유닛 기본 가중치 (%)")]
    [SerializeField] private float fiveStarWeight = 2.9f;

    [Tooltip("6성 유닛 기본 가중치 (%)")]
    [SerializeField] private float sixStarWeight = 0.1f;

    #endregion

    #region 천장(Pity) 스택 및 구간별 확률 설정

    [Header("--- 천장(Pity) 시스템 설정 ---")]
    [Tooltip("소프트 천장 진입 스택 기준값 (50회)")]
    [SerializeField] private int softPityThreshold = 50;

    [Tooltip("하드 천장(6성 확정) 진입 스택 기준값 (100회)")]
    [SerializeField] private int hardPityThreshold = 100;

    [Tooltip("기본 6성 확률 (0.1% = 0.001f)")]
    [SerializeField] private float baseSixStarRate = 0.001f;

    [Tooltip("하드 천장 진입 시 6성 확정 확률 (100.0% = 1.0f)")]
    [SerializeField] private float hardPityRate = 1.0f;

    #endregion

    #region 프로퍼티 (읽기 전용 인터페이스)

    public int SingleDrawCost => Mathf.Max(1, singleDrawCost);
    public int TenDrawCost => Mathf.Max(1, tenDrawCost);

    public float OneStarWeight => oneStarWeight;
    public float TwoStarWeight => twoStarWeight;
    public float ThreeStarWeight => threeStarWeight;
    public float FourStarWeight => fourStarWeight;
    public float FiveStarWeight => fiveStarWeight;
    public float SixStarWeight => sixStarWeight;

    public float NonSixStarWeightTotal => oneStarWeight + twoStarWeight + threeStarWeight + fourStarWeight + fiveStarWeight;
    public float TotalWeight => NonSixStarWeightTotal + sixStarWeight;

    public int SoftPityThreshold => softPityThreshold;
    public int HardPityThreshold => hardPityThreshold;
    public float BaseSixStarRate => baseSixStarRate;
    public float HardPityRate => hardPityRate;

    #endregion

    #region 유효성 검사 및 헬퍼

    // 등급별 가중치 합계 100% 유효성 검증
    private void OnValidate()
    {
        float total = TotalWeight;
        if (Mathf.Abs(total - 100.0f) > 0.001f)
        {
            Debug.LogWarning($"[GachaConfigSO] 등급별 가중치 총합이 100%가 아닙니다! (현재 총합: {total:F2}%)");
        }

        if (softPityThreshold >= hardPityThreshold)
        {
            Debug.LogWarning($"[GachaConfigSO] 소프트 천장 기준값({softPityThreshold})은 하드 천장 기준값({hardPityThreshold})보다 작아야 합니다.");
        }
    }

    // 1성~5성 기본 가중치 배열 반환
    public float[] GetNonSixStarBaseWeights()
    {
        return new float[] { oneStarWeight, twoStarWeight, threeStarWeight, fourStarWeight, fiveStarWeight };
    }

    #endregion
}
