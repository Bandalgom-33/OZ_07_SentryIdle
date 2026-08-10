using System.Collections.Generic;
using UnityEngine;

// 가챠 뽑기 풀 데이터 관리 및 희귀도별 무작위 추첨 제공자
public class GachaDataProvider : MonoBehaviour
{
    private Dictionary<TestRarityGrade, List<IGachaRewardItem>> _rewardPool 
        = new Dictionary<TestRarityGrade, List<IGachaRewardItem>>();

    private void Awake()
    {
        InitializeDummyPool();
    }

    // 테스트용 더미 캐릭터 데이터 풀 초기화
    public void InitializeDummyPool()
    {
        _rewardPool.Clear();
        foreach (TestRarityGrade grade in System.Enum.GetValues(typeof(TestRarityGrade)))
        {
            _rewardPool[grade] = new List<IGachaRewardItem>();
        }

        // Grade_N (일반) 더미 데이터
        _rewardPool[TestRarityGrade.GradeN].Add(new DummyGachaItem("UNIT_001", TestRarityGrade.GradeN, "Trainee Asad"));
        _rewardPool[TestRarityGrade.GradeN].Add(new DummyGachaItem("UNIT_002", TestRarityGrade.GradeN, "Guard Ryan"));

        // Grade_R (희귀) 더미 데이터
        _rewardPool[TestRarityGrade.GradeR].Add(new DummyGachaItem("UNIT_101", TestRarityGrade.GradeR, "Wind Archer Aiden"));
        _rewardPool[TestRarityGrade.GradeR].Add(new DummyGachaItem("UNIT_102", TestRarityGrade.GradeR, "Iron Knight Rohan"));

        // Grade_SR (슈퍼 희귀) 더미 데이터
        _rewardPool[TestRarityGrade.GradeSR].Add(new DummyGachaItem("UNIT_201", TestRarityGrade.GradeSR, "Fire Mage Valentina"));
        _rewardPool[TestRarityGrade.GradeSR].Add(new DummyGachaItem("UNIT_202", TestRarityGrade.GradeSR, "Healer Elena"));

        // Grade_SSR (최상위 등급) 더미 데이터
        _rewardPool[TestRarityGrade.GradeSSR].Add(new DummyGachaItem("UNIT_301", TestRarityGrade.GradeSSR, "Holy Knight Arthur"));
        _rewardPool[TestRarityGrade.GradeSSR].Add(new DummyGachaItem("UNIT_302", TestRarityGrade.GradeSSR, "Dragon Slayer Lucifer"));
    }

    // SSR 당첨 여부 및 가중치 확률 기반 희귀도 등급 결정 연산
    public TestRarityGrade RollGrade(bool isSSR)
    {
        if (isSSR)
        {
            return TestRarityGrade.GradeSSR;
        }

        // SSR 미당첨 시 등급별 확률 가중치 연산 (N: 70%, R: 24.9%, SR: 5.0%)
        float randomVal = Random.Range(0f, 1f);
        if (randomVal < 0.70f)
        {
            return TestRarityGrade.GradeN;
        }
        else if (randomVal < 0.949f)
        {
            return TestRarityGrade.GradeR;
        }
        else
        {
            return TestRarityGrade.GradeSR;
        }
    }

    // 해당 희귀도 풀 내 무작위 캐릭터 1종 추출 연산
    public IGachaRewardItem GetRandomItemByGrade(TestRarityGrade grade)
    {
        if (!_rewardPool.ContainsKey(grade) || _rewardPool[grade].Count == 0)
        {
            // 예외 처리용 기본 캐릭터 반환
            return new DummyGachaItem("UNIT_DEFAULT", grade, "Default Unit");
        }

        List<IGachaRewardItem> list = _rewardPool[grade];
        int randomIndex = Random.Range(0, list.Count);
        return list[randomIndex];
    }
}
