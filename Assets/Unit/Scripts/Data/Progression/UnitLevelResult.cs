namespace EndlessGuard.Unit.Data
{
    public struct UnitLevelResult
    {
        public int PreviousLevel { get; }
        public int CurrentLevel { get; }
        public int LevelsGained { get; }
        public long PreviousExp { get; }
        public long CurrentExp { get; }
        public long GainedExp { get; }
        public long ConsumedExp { get; }
        public long DiscardedExp { get; }
        public bool ReachedMaxLevel { get; }
        public bool DidLevelUp => LevelsGained > 0;

        public UnitLevelResult(int previousLevel, int currentLevel, long previousExp, long currentExp, long gainedExp, long consumedExp, long discardedExp, bool reachedMaxLevel)
        {
            PreviousLevel = previousLevel;
            CurrentLevel = currentLevel;
            LevelsGained = currentLevel - previousLevel;
            PreviousExp = previousExp;
            CurrentExp = currentExp;
            GainedExp = gainedExp;
            ConsumedExp = consumedExp;
            DiscardedExp = discardedExp;
            ReachedMaxLevel = reachedMaxLevel;
        }
    }
}