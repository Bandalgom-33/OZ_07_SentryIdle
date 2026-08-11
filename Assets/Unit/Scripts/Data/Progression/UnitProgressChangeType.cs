using System;

namespace EndlessGuard.Unit.Data
{
    [Flags]
    public enum UnitProgressChangeType
    {
        None = 0,
        Experience = 1 << 0,
        Level = 1 << 1,
        Promotion = 1 << 2
    }
}
