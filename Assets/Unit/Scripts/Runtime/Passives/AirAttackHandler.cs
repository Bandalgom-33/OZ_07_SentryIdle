using System;
using EndlessGuard.Unit.Data;

namespace EndlessGuard.Unit.Runtime
{
    internal sealed class AirAttackHandler : IUnitTargetLayerPassiveHandler
    {
        public Type DataType => typeof(AirAttackSO);

        public bool AllowsTargetLayer(UnitRuntimeState owner, PassiveDataSO passive, PassiveTuning tuning, CombatTargetLayer targetLayer)
        {
            return targetLayer == CombatTargetLayer.Air;
        }
    }
}
