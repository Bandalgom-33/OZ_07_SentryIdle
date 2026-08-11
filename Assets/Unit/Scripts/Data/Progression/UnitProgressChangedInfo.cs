namespace EndlessGuard.Unit.Data
{
    public readonly struct UnitProgressChangedInfo
    {
        public UnitDataSO UnitData { get; }
        public UnitProgressData Progress { get; }
        public UnitProgressChangeType ChangeType { get; }
        public int PreviousLevel { get; }
        public int CurrentLevel { get; }
        public long PreviousExp { get; }
        public long CurrentExp { get; }
        public int PreviousPromotionStage { get; }
        public int CurrentPromotionStage { get; }
        public int PreviousMaxLevel { get; }
        public int CurrentMaxLevel { get; }

        public string UnitId => UnitData != null ? UnitData.UnitId : Progress?.UnitId ?? string.Empty;

        public UnitProgressChangedInfo(
            UnitDataSO unitData,
            UnitProgressData progress,
            UnitProgressChangeType changeType,
            int previousLevel,
            int currentLevel,
            long previousExp,
            long currentExp,
            int previousPromotionStage,
            int currentPromotionStage,
            int previousMaxLevel,
            int currentMaxLevel)
        {
            UnitData = unitData;
            Progress = progress;
            ChangeType = changeType;
            PreviousLevel = previousLevel;
            CurrentLevel = currentLevel;
            PreviousExp = previousExp;
            CurrentExp = currentExp;
            PreviousPromotionStage = previousPromotionStage;
            CurrentPromotionStage = currentPromotionStage;
            PreviousMaxLevel = previousMaxLevel;
            CurrentMaxLevel = currentMaxLevel;
        }
    }
}
