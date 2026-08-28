using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using UnityEngine;

// 가챠 유닛 풀 적재 및 2단계 추첨(등급 판정 -> 유닛 선택)을 담당하는 데이터 프로바이더
public class GachaDataProvider : MonoBehaviour
{
    #region 직렬화 변수 (인스펙터 바인딩)

    [Header("--- 유닛 카탈로그 참조 ---")]
    [Tooltip("게임 내 전체 유닛 카탈로그 SO 참조")]
    [SerializeField] private UnitCatalog unitCatalog;

    #endregion

    #region 상수 및 확률 테이블 정의

    private static readonly float[] BaseGradeWeights = { 30.0f, 30.0f, 25.0f, 12.0f, 2.9f, 0.1f };
    private const float BaseNonSixStarTotal = 99.9f;

    #endregion

    #region 내부 변수 모음

    private readonly Dictionary<UnitGrade, List<IGachaRewardItem>> _rewardPool = new Dictionary<UnitGrade, List<IGachaRewardItem>>();

    #endregion

    #region 라이프 사이클 및 초기화

    // 보상 풀 초기화 연산
    private void Awake()
    {
        InitializePoolFromCatalog();
    }

    // 카탈로그 기반 등급별 보상 풀 구성 연산
    public void InitializePoolFromCatalog()
    {
        _rewardPool.Clear();

        foreach (UnitGrade grade in Enum.GetValues(typeof(UnitGrade)))
        {
            if (grade == UnitGrade.None) continue;
            _rewardPool[grade] = new List<IGachaRewardItem>();
        }

        if (unitCatalog == null)
        {
            unitCatalog = CollectionDataProvider.Instance != null 
                ? CollectionDataProvider.Instance.UnitCatalog 
                : Resources.Load<UnitCatalog>("Catalogs/UnitCatalog");
        }

        if (unitCatalog == null || unitCatalog.Units == null)
        {
            Debug.LogWarning("[GachaDataProvider] UnitCatalog를 찾을 수 없거나 데이터가 비어 있습니다.");
            return;
        }

        foreach (UnitDataSO unitData in unitCatalog.Units)
        {
            if (unitData == null || unitData.Grade == UnitGrade.None) continue;

            if (_rewardPool.ContainsKey(unitData.Grade))
            {
                _rewardPool[unitData.Grade].Add(new UnitGachaItemAdapter(unitData));
            }
        }
    }

    #endregion

    #region 2단계 추첨 핵심 메서드

    // 확률 기반 성 등급 추첨 연산
    public UnitGrade RollGrade(float sixStarProbability)
    {
        if (sixStarProbability >= 1.0f)
        {
            return UnitGrade.SixStar;
        }

        float randomVal = UnityEngine.Random.Range(0f, 100.0f);
        float sixStarPercent = sixStarProbability * 100.0f;

        if (randomVal < sixStarPercent)
        {
            return UnitGrade.SixStar;
        }

        float remainingWeight = 100.0f - sixStarPercent;
        float accumulated = sixStarPercent;

        for (int i = 0; i < 5; i++)
        {
            float normalizedRate = (BaseGradeWeights[i] / BaseNonSixStarTotal) * remainingWeight;
            accumulated += normalizedRate;
            if (randomVal < accumulated)
            {
                return (UnitGrade)(i + 1);
            }
        }

        return UnitGrade.OneStar;
    }

    // 천장 플래그 기반 성 등급 추첨 연산 (오버로딩)
    public UnitGrade RollGrade(bool isPity)
    {
        return RollGrade(isPity ? 1.0f : 0.001f);
    }

    // 지정 등급 내 무작위 유닛 선택 연산
    public IGachaRewardItem GetRandomItemByGrade(UnitGrade grade)
    {
        if (_rewardPool.TryGetValue(grade, out List<IGachaRewardItem> itemList) && itemList.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, itemList.Count);
            return itemList[randomIndex];
        }

        foreach (var pair in _rewardPool)
        {
            if (pair.Value.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, pair.Value.Count);
                return pair.Value[randomIndex];
            }
        }

        return default;
    }

    #endregion
}
