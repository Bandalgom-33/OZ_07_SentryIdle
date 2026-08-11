using System;
using EndlessGuard.Unit.Data;

namespace EndlessGuard.Unit.Runtime
{
    internal sealed class BlockAttackHandler : IUnitAttackAllBlockedPassiveHandler
    {
        public Type DataType => typeof(BlockAttackSO);

        public bool IsEnabled(UnitRuntimeState owner, PassiveDataSO passive, PassiveTuning tuning)
        {
            return owner != null && owner.Block != null;
        }
    }
}
