using System.Collections.Generic;
using EndlessGuard.Unit.Data;

// 가챠 획득 결과의 세부 상태를 구분하는 열거형 (신규 해금, 한계돌파 상승, 풀돌 도달/유지)
public enum GachaResultType
{
    // 신규 미보유 캐릭터 최초 획득 (해금)
    NewUnlock,
    // 보유 캐릭터 중복 획득으로 인한 한계돌파 단계 상승
    Breakthrough,
    // 이미 최대 한계돌파(6단계 풀돌)에 도달한 캐릭터 중복 획득
    MaxBreakthroughReached
}

// 가챠 추첨 보상 아이템의 표준 인터페이스
public interface IGachaRewardItem
{
    // 보상 고유 식별 ID
    string RewardId { get; }
    // 유닛 성 등급 (1성 ~ 6성)
    UnitGrade Grade { get; }
    // 화면 표시 이름
    string DisplayName { get; }
    // 기존 보유 여부 플래그
    bool IsOwned { get; set; }
    // 중복 획득 시 처리 결과 타입 (신규/돌파/풀돌)
    GachaResultType ResultType { get; set; }
    // 돌파 적용 이전 단계 (0 ~ 6)
    int PreviousBreakthroughStep { get; set; }
    // 돌파 적용 이후 현재 단계 (0 ~ 6)
    int CurrentBreakthroughStep { get; set; }
    // 중복 획득 시 부여 조각 수량
    int DuplicatePieceAmount { get; }
    // 대상 유닛 ScriptableObject 원본 데이터
    UnitDataSO TargetUnitData { get; }
}

// UnitDataSO를 가챠 보상 인터페이스로 래핑하는 어댑터 구조체
public struct UnitGachaItemAdapter : IGachaRewardItem
{
    // 원본 유닛 데이터 참조
    public UnitDataSO TargetUnitData { get; }
    // 유닛 ID 반환
    public string RewardId => TargetUnitData != null ? TargetUnitData.UnitId : string.Empty;
    // 성 등급 반환
    public UnitGrade Grade => TargetUnitData != null ? TargetUnitData.Grade : UnitGrade.None;
    // 유닛 표시 이름 반환
    public string DisplayName => TargetUnitData != null ? TargetUnitData.DisplayName : string.Empty;
    
    // 보유 여부
    public bool IsOwned { get; set; }
    // 가챠 결과 타입
    public GachaResultType ResultType { get; set; }
    // 이전 돌파 단계
    public int PreviousBreakthroughStep { get; set; }
    // 현재 돌파 단계
    public int CurrentBreakthroughStep { get; set; }
    // 중복 조각 수량
    public int DuplicatePieceAmount { get; private set; }

    // 가챠 보상 아이템 어댑터 생성자 (초기화 시 기본값 설정)
    public UnitGachaItemAdapter(UnitDataSO unitData, bool isOwned = false, int duplicatePieceAmount = 10)
    {
        TargetUnitData = unitData;
        IsOwned = isOwned;
        ResultType = isOwned ? GachaResultType.Breakthrough : GachaResultType.NewUnlock;
        PreviousBreakthroughStep = 0;
        CurrentBreakthroughStep = 0;
        DuplicatePieceAmount = duplicatePieceAmount;
    }
}

