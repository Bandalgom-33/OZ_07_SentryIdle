namespace EndlessGuard.Unit.Raid.Runtime
{
    internal interface IRaidPathStrategy
    {
        int Select(in RaidPathCandidates candidates);
        void Reset();
    }
}