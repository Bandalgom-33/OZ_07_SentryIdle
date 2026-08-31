using EndlessGuard.Unit.Data;

public enum GachaResultType
{
    NewUnlock,
    Breakthrough,
    MaxBreakthroughReached
}

public class GachaRewardItem
{
    public UnitDataSO TargetUnitData { get; private set; }
    public int UnitId { get; private set; }
    public string RewardId => TargetUnitData != null ? TargetUnitData.UnitId : string.Empty;
    public UnitGrade Grade => TargetUnitData != null ? TargetUnitData.Grade : UnitGrade.None;
    public string DisplayName => TargetUnitData != null ? TargetUnitData.DisplayName : "Unknown";
    public bool IsOwned { get; set; }
    public GachaResultType ResultType { get; set; }
    public int PreviousBreakthroughStep { get; set; }
    public int CurrentBreakthroughStep { get; set; }

    // 가챠 보상 DTO 객체 생성
    public GachaRewardItem(UnitDataSO unitData, int unitId, bool isOwned = false)
    {
        TargetUnitData = unitData;
        UnitId = unitId;
        IsOwned = isOwned;
        ResultType = isOwned ? GachaResultType.Breakthrough : GachaResultType.NewUnlock;
        PreviousBreakthroughStep = 0;
        CurrentBreakthroughStep = 0;
    }
}
