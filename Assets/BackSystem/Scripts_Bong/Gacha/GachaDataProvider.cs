using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using UnityEngine;

public class GachaDataProvider : MonoBehaviour
{
    #region 직렬화 변수

    [Header("--- 필수 설정 및 카탈로그 참조 ---")]
    [Tooltip("게임 내 전체 유닛 카탈로그 SO 참조")]
    [SerializeField] private UnitCatalog unitCatalog;

    [Tooltip("가챠 확률 및 비용 설정 SO 참조")]
    [SerializeField] private GachaConfigSO gachaConfig;

    #endregion

    #region 내부 캐시 변수

    private readonly Dictionary<UnitGrade, List<int>> _rewardIdPool = new Dictionary<UnitGrade, List<int>>();
    private readonly Dictionary<int, UnitDataSO> _unitDataMap = new Dictionary<int, UnitDataSO>();

    public bool IsInitialized { get; private set; } = false;

    public UnitCatalog UnitCatalog => unitCatalog;
    public GachaConfigSO GachaConfig => gachaConfig;

    #endregion

    #region 라이프 사이클 및 초기화

    // 컴포넌트 초기화 및 유닛 풀 구성
    private void Awake()
    {
        InitializePoolFromCatalog();
    }

    // 카탈로그 기반 등급별 보상 풀 구성
    public bool InitializePoolFromCatalog()
    {
        _rewardIdPool.Clear();
        _unitDataMap.Clear();
        IsInitialized = false;

        foreach (UnitGrade grade in Enum.GetValues(typeof(UnitGrade)))
        {
            if (grade == UnitGrade.None) continue;
            _rewardIdPool[grade] = new List<int>();
        }

        if (unitCatalog == null)
        {
            if (CollectionDataProvider.Instance != null && CollectionDataProvider.Instance.UnitCatalog != null)
            {
                unitCatalog = CollectionDataProvider.Instance.UnitCatalog;
            }
            else
            {
                Debug.LogError("[GachaDataProvider] UnitCatalog 참조가 누락되었습니다! 인스펙터에서 UnitCatalog를 할당해주세요.");
                return false;
            }
        }

        if (unitCatalog.Units == null || unitCatalog.Units.Count == 0)
        {
            Debug.LogError("[GachaDataProvider] UnitCatalog의 Units 목록이 비어 있습니다.");
            return false;
        }

        foreach (UnitDataSO unitData in unitCatalog.Units)
        {
            if (unitData == null || unitData.Grade == UnitGrade.None) continue;

            int parsedId = UnitIdHelper.ParseUnitId(unitData.UnitId);
            if (parsedId <= 0)
            {
                Debug.LogWarning($"[GachaDataProvider] 유효하지 않은 유닛 ID({unitData.UnitId})를 건너뜁니다.");
                continue;
            }

            _unitDataMap[parsedId] = unitData;

            if (_rewardIdPool.ContainsKey(unitData.Grade))
            {
                _rewardIdPool[unitData.Grade].Add(parsedId);
            }
        }

        IsInitialized = true;
        return true;
    }

    #endregion

    #region 추첨 및 확률 계산 메서드

    // 확률 기반 성 등급 무작위 추첨
    public UnitGrade RollGrade(float sixStarProbability)
    {
        if (sixStarProbability >= 1.0f)
        {
            return UnitGrade.SixStar;
        }

        float randomVal = UnityEngine.Random.Range(0f, 100.0f);
        float sixStarPercent = Mathf.Clamp(sixStarProbability * 100.0f, 0f, 100f);

        if (randomVal < sixStarPercent)
        {
            return UnitGrade.SixStar;
        }

        float remainingWeight = 100.0f - sixStarPercent;
        float accumulated = sixStarPercent;

        float[] baseWeights = (gachaConfig != null)
            ? gachaConfig.GetNonSixStarBaseWeights()
            : new float[] { 30.0f, 30.0f, 25.0f, 12.0f, 2.9f };

        float nonSixStarTotal = (gachaConfig != null) ? gachaConfig.NonSixStarWeightTotal : 99.9f;
        if (nonSixStarTotal <= 0f) nonSixStarTotal = 99.9f;

        for (int i = 0; i < baseWeights.Length; i++)
        {
            float normalizedRate = (baseWeights[i] / nonSixStarTotal) * remainingWeight;
            accumulated += normalizedRate;
            if (randomVal < accumulated)
            {
                return (UnitGrade)(i + 1);
            }
        }

        return UnitGrade.OneStar;
    }

    // 6성 확률 기준 전체 등급별 실시간 확률표(%) 계산 및 반환
    public Dictionary<UnitGrade, float> CalculateGradeProbabilities(float sixStarProbability)
    {
        Dictionary<UnitGrade, float> result = new Dictionary<UnitGrade, float>();
        float sixStarPercent = Mathf.Clamp(sixStarProbability * 100.0f, 0f, 100f);
        result[UnitGrade.SixStar] = sixStarPercent;

        float remainingWeight = 100.0f - sixStarPercent;
        float[] baseWeights = (gachaConfig != null)
            ? gachaConfig.GetNonSixStarBaseWeights()
            : new float[] { 30.0f, 30.0f, 25.0f, 12.0f, 2.9f };

        float nonSixStarTotal = (gachaConfig != null) ? gachaConfig.NonSixStarWeightTotal : 99.9f;
        if (nonSixStarTotal <= 0f) nonSixStarTotal = 99.9f;

        for (int i = 0; i < baseWeights.Length; i++)
        {
            UnitGrade grade = (UnitGrade)(i + 1);
            float rate = (baseWeights[i] / nonSixStarTotal) * remainingWeight;
            result[grade] = rate;
        }

        return result;
    }

    // 지정 등급 내 무작위 유닛 정수 ID 추첨
    public int GetRandomUnitIdByGrade(UnitGrade grade)
    {
        if (_rewardIdPool.TryGetValue(grade, out List<int> idList) && idList.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, idList.Count);
            return idList[randomIndex];
        }

        foreach (var pair in _rewardIdPool)
        {
            if (pair.Value.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, pair.Value.Count);
                return pair.Value[randomIndex];
            }
        }

        Debug.LogError($"[GachaDataProvider] 추첨 가능한 유닛이 풀에 전혀 존재하지 않습니다. (Grade: {grade})");
        return -1;
    }

    // 유닛 정수 ID 기반 UnitDataSO 조회
    public UnitDataSO GetUnitData(int unitId)
    {
        if (_unitDataMap.TryGetValue(unitId, out UnitDataSO dataSO))
        {
            return dataSO;
        }

        if (unitCatalog != null)
        {
            string unitKey = UnitIdHelper.ToUnitKey(unitId);
            if (unitCatalog.TryGetById(unitKey, out UnitDataSO foundSO))
            {
                _unitDataMap[unitId] = foundSO;
                return foundSO;
            }
        }

        return null;
    }

    #endregion
}
