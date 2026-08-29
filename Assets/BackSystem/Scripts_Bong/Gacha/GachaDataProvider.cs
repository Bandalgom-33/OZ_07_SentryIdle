using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using UnityEngine;

// 가챠 유닛 ID 풀 적재 및 2단계 추첨(등급 판정 -> 유닛 선택)을 담당하는 데이터 프로바이더
public class GachaDataProvider : MonoBehaviour
{
    #region 직렬화 변수 (인스펙터 바인딩)

    [Header("--- 필수 설정 및 카탈로그 참조 ---")]
    [Tooltip("게임 내 전체 유닛 카탈로그 SO 참조")]
    [SerializeField] private UnitCatalog unitCatalog;

    [Tooltip("가챠 확률 및 비용 설정 SO 참조")]
    [SerializeField] private GachaConfigSO gachaConfig;

    #endregion

    #region 내부 캐시 변수

    // 등급별 유닛 정수 ID 목록 풀 (박싱 및 중복 인스턴스 생성을 방지하기 위해 정수 ID 리스트로 경량 관리)
    private readonly Dictionary<UnitGrade, List<int>> _rewardIdPool = new Dictionary<UnitGrade, List<int>>();

    // UnitCatalog 빠른 조회를 위한 내부 캐시 맵 (ID -> UnitDataSO)
    private readonly Dictionary<int, UnitDataSO> _unitDataMap = new Dictionary<int, UnitDataSO>();

    // 정상 초기화 완료 여부 플래그
    public bool IsInitialized { get; private set; } = false;

    public UnitCatalog UnitCatalog => unitCatalog;
    public GachaConfigSO GachaConfig => gachaConfig;

    #endregion

    #region 라이프 사이클 및 초기화

    // 보상 풀 및 카탈로그 캐시 초기화
    private void Awake()
    {
        InitializePoolFromCatalog();
    }

    // 카탈로그 기반 등급별 보상 풀 구성 연산
    public bool InitializePoolFromCatalog()
    {
        _rewardIdPool.Clear();
        _unitDataMap.Clear();
        IsInitialized = false;

        // 1성부터 6성까지 각 등급별 빈 리스트 사전 생성
        foreach (UnitGrade grade in Enum.GetValues(typeof(UnitGrade)))
        {
            if (grade == UnitGrade.None) continue;
            _rewardIdPool[grade] = new List<int>();
        }

        // 인스펙터 참조가 누락된 경우 명시적 에러 로그 출력 후 초기화 중단 (묵시적 Resources.Load 폴백 방지)
        if (unitCatalog == null)
        {
            // CollectionDataProvider의 카탈로그를 우선 안전하게 확인
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

        // 카탈로그 내 유닛 순회 및 정수 ID 기반 풀 적재
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

    #region 2단계 추첨 핵심 메서드

    // 확률 기반 성 등급 추첨 연산 (GachaConfigSO 가중치 기반 정규화 연산)
    public UnitGrade RollGrade(float sixStarProbability)
    {
        // 6성 확률이 100%(1.0f) 이상이면 하드 천장으로 즉시 6성 반환
        if (sixStarProbability >= 1.0f)
        {
            return UnitGrade.SixStar;
        }

        float randomVal = UnityEngine.Random.Range(0f, 100.0f);
        float sixStarPercent = Mathf.Clamp(sixStarProbability * 100.0f, 0f, 100f);

        // 6성 당첨 판정
        if (randomVal < sixStarPercent)
        {
            return UnitGrade.SixStar;
        }

        // 6성을 제외한 나머지 1성~5성에 대해 남은 확률 가중치(100% - 6성%)를 비례 분배
        float remainingWeight = 100.0f - sixStarPercent;
        float accumulated = sixStarPercent;

        // Config가 있을 경우 Config 가중치 사용, 없을 경우 기본 가중치 적용
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

    // 지정 등급 내 무작위 유닛 정수 ID 추첨 연산
    public int GetRandomUnitIdByGrade(UnitGrade grade)
    {
        if (_rewardIdPool.TryGetValue(grade, out List<int> idList) && idList.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, idList.Count);
            return idList[randomIndex];
        }

        // 해당 등급의 유닛 풀이 비어있을 경우 전체 풀에서 대체 탐색
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

    // 유닛 정수 ID로 원본 UnitDataSO 조회 연산 (캐시 맵 활용으로 O(1) 초고속 조회)
    public UnitDataSO GetUnitData(int unitId)
    {
        if (_unitDataMap.TryGetValue(unitId, out UnitDataSO dataSO))
        {
            return dataSO;
        }

        // 캐시 맵에 없을 경우 카탈로그에서 추가 검색
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
