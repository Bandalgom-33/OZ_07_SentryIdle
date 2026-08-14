using System.Collections.Generic;
using EndlessGuard.Unit.Data;

public interface IGachaRewardItem
{
    string RewardId { get; }
    UnitGrade Grade { get; }
    string DisplayName { get; }
    bool IsOwned { get; set; }
    int DuplicatePieceAmount { get; }
    UnitDataSO TargetUnitData { get; }
}

public struct UnitGachaItemAdapter : IGachaRewardItem
{
    public UnitDataSO TargetUnitData { get; }
    public string RewardId => TargetUnitData != null ? TargetUnitData.UnitId : string.Empty;
    public UnitGrade Grade => TargetUnitData != null ? TargetUnitData.Grade : UnitGrade.None;
    public string DisplayName => TargetUnitData != null ? TargetUnitData.DisplayName : string.Empty;
    
    public bool IsOwned { get; set; }
    public int DuplicatePieceAmount { get; private set; }

    // 가챠 보상 아이템 어댑터 생성자
    public UnitGachaItemAdapter(UnitDataSO unitData, bool isOwned = false, int duplicatePieceAmount = 10)
    {
        TargetUnitData = unitData;
        IsOwned = isOwned;
        DuplicatePieceAmount = duplicatePieceAmount;
    }
}
