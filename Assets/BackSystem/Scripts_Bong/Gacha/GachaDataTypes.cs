using System.Collections.Generic;

// 가챠 테스트 및 추첨용 희귀도 등급 열거형
public enum TestRarityGrade
{
    GradeN = 1,  // 일반 (Normal)
    GradeR = 2,  // 희귀 (Rare)
    GradeSR = 3, // 슈퍼 희귀 (Super Rare)
    GradeSSR = 4 // 최상위 희귀도 (Ultimate / 6성급)
}

// 캐릭터 데이터 연동 디커플링용 최소 표준 인터페이스
public interface IGachaRewardItem
{
    // 캐릭터 고유 식별 ID (예: "UNIT_0001")
    string RewardId { get; }
    
    // 캐릭터 희귀도 등급
    TestRarityGrade Grade { get; }
    
    // UI 표시용 캐릭터 이름
    string DisplayName { get; }
}

// 테스트용 더미 리워드 객체 구조체
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
