namespace EndlessGuard.Unit.Data
{
    public readonly struct UnitGrowthChangedInfo
    {
        public string UnitId { get; }
        public GrowthStatMask ChangedStat { get; }
        public float PreviousValue { get; }
        public float CurrentValue { get; }

        public UnitGrowthChangedInfo(string unitId, GrowthStatMask changedStat, float previousValue, float currentValue)
        {
            UnitId = unitId ?? string.Empty;
            ChangedStat = changedStat;
            PreviousValue = previousValue;
            CurrentValue = currentValue;
        }
    }
}
