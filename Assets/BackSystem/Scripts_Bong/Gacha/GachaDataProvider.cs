using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using UnityEngine;

// 가챠 풀 관리 및 2단계 추첨(등급 추첨 ➔ 등급 내 유닛 선택)을 담당하는 데이터 프로바이더
public class GachaDataProvider : MonoBehaviour
{
    #region 직렬화 변수 (인스펙터 바인딩)

    [Header("--- 팀원 캐릭터 카탈로그 데이터 ---")]
    [Tooltip("팀원 캐릭터 카탈로그 SO 참조")]
    [SerializeField] private UnitCatalog unitCatalog;

    #endregion

    #region 상수 및 확률 테이블 정의

    // 기획서 4.2.1 2) 명세에 따른 성 등급별 기본 확률 (1성~6성 순서: 30%, 30%, 25%, 12%, 2.9%, 0.1%)
    private static readonly float[] BaseGradeWeights = { 30.0f, 30.0f, 25.0f, 12.0f, 2.9f, 0.1f };
    // 1성~5성 기본 가중치의 합 (30 + 30 + 25 + 12 + 2.9 = 99.9)
    private const float BaseNonSixStarTotal = 99.9f;

    #endregion

    #region 내부 변수 모음

    // 성 등급별 유닛 보상 풀 딕셔너리
    private readonly Dictionary<UnitGrade, List<IGachaRewardItem>> _rewardPool = new Dictionary<UnitGrade, List<IGachaRewardItem>>();

    #endregion

    #region 라이프 사이클 및 초기화

    // 컴포넌트 시작 시 보상 풀 초기화
    private void Awake()
    {
        InitializePoolFromCatalog();
    }

    // UnitCatalog SO를 분석하여 성 등급별로 유닛 풀 자동 분류 및 적재
    public void InitializePoolFromCatalog()
    {
        _rewardPool.Clear();
        
        // 유효한 성 등급(1성~6성) 목록에 대한 빈 리스트 초기화
        foreach (UnitGrade grade in Enum.GetValues(typeof(UnitGrade)))
        {
            if (grade == UnitGrade.None) continue;
            _rewardPool[grade] = new List<IGachaRewardItem>();
        }

        // 인스펙터 바인딩이 누락된 경우 Resources 폴더에서 자동 로드 시도
        if (unitCatalog == null)
        {
            unitCatalog = Resources.Load<UnitCatalog>("Catalogs/UnitCatalog");
        }

        // 카탈로그 유효성 검사
        if (unitCatalog == null || unitCatalog.Units == null)
        {
            Debug.LogWarning("[GachaDataProvider] UnitCatalog를 찾을 수 없거나 데이터가 비어 있습니다.");
            return;
        }

        // 카탈로그에 등록된 모든 유닛을 성 등급별 풀에 자동 적재
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

    // [1단계: 성 등급 추첨] 천장 연산기에서 계산된 6성 확률을 기반으로 성 등급 결정 (비례 안분 배분 적용)
    public UnitGrade RollGrade(float sixStarProbability)
    {
        // 100회 도달 시 6성 100% 확정 지급
        if (sixStarProbability >= 1.0f)
        {
            return UnitGrade.SixStar;
        }

        // 0.0 ~ 100.0f 범위의 무작위 난수 추출
        float randomVal = UnityEngine.Random.Range(0f, 100.0f);
        float sixStarPercent = sixStarProbability * 100.0f;

        // 6성 당첨 여부 검사 (1~49회: 0.1%, 50~99회: 10.0%)
        if (randomVal < sixStarPercent)
        {
            return UnitGrade.SixStar;
        }

        // 6성을 제외한 나머지 1~5성에 할당될 잔여 총 확률 (기본 99.9% 또는 급증 시 90.0%)
        float remainingWeight = 100.0f - sixStarPercent;

        // 잔여 확률을 1성~5성 기본 가중치 비율대로 비례 안분 배분하여 등급 판정
        float accumulated = sixStarPercent;
        for (int i = 0; i < 5; i++)
        {
            float normalizedRate = (BaseGradeWeights[i] / BaseNonSixStarTotal) * remainingWeight;
            accumulated += normalizedRate;
            if (randomVal < accumulated)
            {
                return (UnitGrade)(i + 1); // 1성 ~ 5성 Enum 매핑
            }
        }

        return UnitGrade.OneStar;
    }

    // 하위 호환성을 위한 오버로딩 (isPity 플래그 기반)
    public UnitGrade RollGrade(bool isPity)
    {
        return RollGrade(isPity ? 1.0f : 0.001f);
    }

    // [2단계: 유닛 선택] 1단계에서 결정된 성 등급 풀에서 무작위 1종 추출
    public IGachaRewardItem GetRandomItemByGrade(UnitGrade grade)
    {
        // 요청된 등급의 풀에 유닛이 존재하는 경우 무작위 1종 선택
        if (_rewardPool.TryGetValue(grade, out List<IGachaRewardItem> itemList) && itemList.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, itemList.Count);
            return itemList[randomIndex];
        }

        // 해당 등급의 유닛이 비어있는 예외 상황 시 전체 풀에서 무작위 대체 추출
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

