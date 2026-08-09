
// 가챠 테스트 및 추첨용 데이터 희귀도 등급 (독립된 테스트 등급)
public enum TestRarityGrade
{
    GradeN = 1,  // 일반 (Normal)
    GradeR = 2,  // 희귀 (Rare)
    GradeSR = 3, // 슈퍼 희귀 (Super Rare)
    GradeSSR = 4 // 최상위 희귀도 (Ultimate / 6성급)
}

public interface IGachaRewardItem
{
    string RewardId { get; }
    TestRarityGrade Grade { get; }
    string DisplayName { get; }
}

// 테스트용 데이터 
public struct DummyGachaItem : IGachaRewardItem
{
    public string RewardId { get; private set; }
    public TestRarityGrade Grade { get; private set; }
    public string DisplayName { get; private set; }

    public DummyGachaItem(string rewardId, TestRarityGrade grade, string displayName)
    {
        RewardId = rewardId;
        Grade = grade;
        DisplayName = displayName;
    }
}
