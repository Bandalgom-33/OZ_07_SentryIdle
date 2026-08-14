using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using UnityEngine;

public class GachaDataProvider : MonoBehaviour
{
    #region 직렬화 변수 (인스펙터 바인딩)

    [Header("--- 팀원 캐릭터 카탈로그 데이터 ---")]
    [Tooltip("팀원 캐릭터 카탈로그 SO 참조")]
    [SerializeField] private UnitCatalog unitCatalog;

    #endregion

    #region 내부 변수 모음

    private readonly Dictionary<UnitGrade, List<IGachaRewardItem>> _rewardPool = new Dictionary<UnitGrade, List<IGachaRewardItem>>();

    #endregion

    #region 라이프 사이클 및 초기화

    // 인스턴스 초기화 연산
    private void Awake()
    {
        InitializePoolFromCatalog();
    }

    // 카탈로그 기반 등급별 보상 풀 초기화
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
            unitCatalog = Resources.Load<UnitCatalog>("Catalogs/UnitCatalog");
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

    #region 추첨 로직 메서드

    // 등급 확률 추첨 연산
    public UnitGrade RollGrade(bool isPity)
    {
        if (isPity)
        {
            return UnitGrade.SixStar;
        }

        float randomVal = UnityEngine.Random.Range(0f, 1f);

        if (randomVal < 0.45f)       return UnitGrade.OneStar;
        else if (randomVal < 0.75f)  return UnitGrade.TwoStar;
        else if (randomVal < 0.90f)  return UnitGrade.ThreeStar;
        else if (randomVal < 0.96f)  return UnitGrade.FourStar;
        else if (randomVal < 0.99f)  return UnitGrade.FiveStar;
        else                         return UnitGrade.SixStar;
    }

    // 등급별 무작위 보상 아이템 추출
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
