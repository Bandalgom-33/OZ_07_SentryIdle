using System.Collections.Generic;
using EndlessGuard.Unit.Data;

// 캐릭터 데이터 연동 및 가챠 결과 디커플링을 위한 표준 보상 인터페이스
public interface IGachaRewardItem
{
    // 캐릭터 고유 식별 ID (예: "UNIT_0001")
    string RewardId { get; }

    // 캐릭터 희귀도 등급 (팀원의 UnitGrade: OneStar ~ SixStar)
    UnitGrade Grade { get; }

    // UI 표시용 캐릭터 이름
    string DisplayName { get; }

    // 유저가 이미 해당 캐릭터를 소유하고 있는지 여부 (NEW 마크 표기용)
    bool IsOwned { get; set; }

    // 중복 획득 시 변환/제공할 조각/재화 수량 (확장성 고려)
    int DuplicatePieceAmount { get; }

    // 실제 참조하는 원본 UnitDataSO 객체 (팀원 데이터 연동용)
    UnitDataSO TargetUnitData { get; }
}

// 팀원의 UnitDataSO를 가챠 보상 인터페이스(IGachaRewardItem)로 바인딩하는 어댑터 구조체
// 이유: 팀원 UnitDataSO 코드를 수정하지 않고 가챠 시스템에서 요구하는 인터페이스 규격을 맞추기 위한 어댑터 패턴 적용
public struct UnitGachaItemAdapter : IGachaRewardItem
{
    public UnitDataSO TargetUnitData { get; }
    public string RewardId => TargetUnitData != null ? TargetUnitData.UnitId : string.Empty;
    public UnitGrade Grade => TargetUnitData != null ? TargetUnitData.Grade : UnitGrade.None;
    public string DisplayName => TargetUnitData != null ? TargetUnitData.DisplayName : string.Empty;
    
    public bool IsOwned { get; set; }
    public int DuplicatePieceAmount { get; private set; }

    public UnitGachaItemAdapter(UnitDataSO unitData, bool isOwned = false, int duplicatePieceAmount = 10)
    {
        TargetUnitData = unitData;
        IsOwned = isOwned;
        DuplicatePieceAmount = duplicatePieceAmount;
    }
}
