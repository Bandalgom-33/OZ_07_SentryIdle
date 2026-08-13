using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using UnityEngine;

// 팀원의 UnitCatalog 데이터를 로드하여 6단계 등급(OneStar~SixStar)별 뽑기 풀을 구성하고 추첨을 담당하는 클래스
public class GachaDataProvider : MonoBehaviour
{
    #region 직렬화 변수 (인스펙터 바인딩)

    [Header("--- 팀원 캐릭터 카탈로그 데이터 ---")]
    [Tooltip("팀원이 작성한 UnitCatalog ScriptableObject 참조")]
    [SerializeField] private UnitCatalog unitCatalog;

    #endregion

    #region 내부 변수 모음

    // 6단계 등급(UnitGrade)별 가챠 뽑기 풀 사전 레지스터
    private readonly Dictionary<UnitGrade, List<IGachaRewardItem>> _rewardPool = new Dictionary<UnitGrade, List<IGachaRewardItem>>();

    #endregion

    #region 라이프 사이클 및 초기화

    private void Awake()
    {
        InitializePoolFromCatalog();
    }

    // 등급별 가챠 카탈로그 풀 동적 설정 처리
    public void InitializePoolFromCatalog()
    {
        _rewardPool.Clear();

        // 1성~6성 등급별 리스트 초기화
        foreach (UnitGrade grade in Enum.GetValues(typeof(UnitGrade)))
        {
            if (grade == UnitGrade.None) continue;
            _rewardPool[grade] = new List<IGachaRewardItem>();
        }

        // 인스펙터 참조가 누락되었을 경우 Resources 로딩 시도 (안전 장치)
        if (unitCatalog == null)
        {
            unitCatalog = Resources.Load<UnitCatalog>("Catalogs/UnitCatalog");
        }

        if (unitCatalog == null || unitCatalog.Units == null)
        {
            Debug.LogWarning("[GachaDataProvider] UnitCatalog를 찾을 수 없거나 데이터가 비어 있습니다.");
            return;
        }

        // 카탈로그의 모든 유닛을 등급(UnitGrade)별로 풀에 수집
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

    #region 추첨 로직 메서드

    // 등급별 가중치 기반 확률 추첨 연산
    public UnitGrade RollGrade(bool isPity)
    {
        // 천장 달성 시 최고 등급 6성(SixStar) 확정 리턴
        if (isPity)
        {
            return UnitGrade.SixStar;
        }

        // 6단계 등급 확률 분포 설정 (1성: 45%, 2성: 30%, 3성: 15%, 4성: 6%, 5성: 3%, 6성: 1%)
        float randomVal = UnityEngine.Random.Range(0f, 1f);

        if (randomVal < 0.45f)       return UnitGrade.OneStar;   // 0.00 ~ 0.45 (45%)
        else if (randomVal < 0.75f)  return UnitGrade.TwoStar;   // 0.45 ~ 0.75 (30%)
        else if (randomVal < 0.90f)  return UnitGrade.ThreeStar; // 0.75 ~ 0.90 (15%)
        else if (randomVal < 0.96f)  return UnitGrade.FourStar;  // 0.90 ~ 0.96 (6%)
        else if (randomVal < 0.99f)  return UnitGrade.FiveStar;  // 0.96 ~ 0.99 (3%)
        else                         return UnitGrade.SixStar;   // 0.99 ~ 1.00 (1%)
    }

    // 등급별 유닛 풀 무작위 1종 추출 연산
    public IGachaRewardItem GetRandomItemByGrade(UnitGrade grade)
    {
        if (_rewardPool.TryGetValue(grade, out List<IGachaRewardItem> itemList) && itemList.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, itemList.Count);
            return itemList[randomIndex];
        }

        // 폴백 조치: 해당 등급에 캐릭터가 없으면 전체 풀 중 임의 캐릭터 무작위 선정
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
