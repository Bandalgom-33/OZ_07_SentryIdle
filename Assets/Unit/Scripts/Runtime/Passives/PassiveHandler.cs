using System;
using EndlessGuard.Unit.Data;

namespace EndlessGuard.Unit.Runtime
{
    internal interface IPassiveHandler
    {
        Type DataType { get; }
    }

    internal interface IPassiveRuntimeBinding
    {
        void Activate();
        void Deactivate();
    }

    internal interface IPassiveTickBinding
    {
        void Step(float deltaTime);
    }

    internal interface IEnemyInitializePassiveHandler : IPassiveHandler
    {
        void Apply(EnemyRuntimeState owner, PassiveDataSO passive, PassiveTuning tuning);
    }

    internal interface IEnemyRuntimePassiveHandler : IPassiveHandler
    {
        IPassiveRuntimeBinding CreateBinding(EnemyRuntimeState owner, PassiveDataSO passive, PassiveTuning tuning);
    }

    internal interface IEnemyBasicAttackResolvedPassiveHandler : IPassiveHandler
    {
        void OnBasicAttackResolved(EnemyRuntimeState owner, UnitRuntimeState target, PassiveDataSO passive, PassiveTuning tuning, BasicAttackResult result);
    }

    internal interface IEnemyBasicAttackReceivedPassiveHandler : IPassiveHandler
    {
        void OnBasicAttackReceived(EnemyRuntimeState owner, UnitRuntimeState attacker, PassiveDataSO passive, PassiveTuning tuning, BasicAttackResult result);
    }

    internal interface IEnemyBlockedPassiveHandler : IPassiveHandler
    {
        void OnBlocked(EnemyRuntimeState owner, UnitRuntimeState blocker, PassiveDataSO passive, PassiveTuning tuning);
    }

    internal interface IEnemyDiedPassiveHandler : IPassiveHandler
    {
        void OnDied(EnemyRuntimeState owner, PassiveDataSO passive, PassiveTuning tuning);
    }

    internal interface IEnemyRandomTargetPassiveHandler : IPassiveHandler
    {
        int GetRandomTargetCount(EnemyRuntimeState owner, PassiveDataSO passive, PassiveTuning tuning);
    }

    internal interface IEnemySnipeBurstPassiveHandler : IPassiveHandler
    {
        int GetBurstAttackCount(EnemyRuntimeState owner, PassiveDataSO passive, PassiveTuning tuning);
        float GetForcedMoveSeconds(EnemyRuntimeState owner, PassiveDataSO passive, PassiveTuning tuning);
    }

    internal interface IUnitOutgoingDamagePassiveHandler : IPassiveHandler
    {
        float ModifyDamage(UnitRuntimeState owner, EnemyRuntimeState target, PassiveDataSO passive, PassiveTuning tuning, float damage);
    }

    internal interface IUnitAttackPowerPassiveHandler : IPassiveHandler
    {
        float ModifyAttackPower(UnitRuntimeState owner, EnemyRuntimeState target, PassiveDataSO passive, PassiveTuning tuning, float attackPower);
    }

    internal interface IUnitTargetLayerPassiveHandler : IPassiveHandler
    {
        bool AllowsTargetLayer(UnitRuntimeState owner, PassiveDataSO passive, PassiveTuning tuning, CombatTargetLayer targetLayer);
    }

    internal interface IUnitAttackAllBlockedPassiveHandler : IPassiveHandler
    {
        bool IsEnabled(UnitRuntimeState owner, PassiveDataSO passive, PassiveTuning tuning);
    }

    internal interface IUnitRuntimePassiveHandler : IPassiveHandler
    {
        IPassiveRuntimeBinding CreateBinding(UnitRuntimeState owner, PassiveDataSO passive, PassiveTuning tuning);
    }

    internal interface IUnitBasicAttackResolvedPassiveHandler : IPassiveHandler
    {
        void OnBasicAttackResolved(UnitRuntimeState owner, EnemyRuntimeState target, PassiveDataSO passive, PassiveTuning tuning, BasicAttackResult result);
    }

    internal interface IUnitBasicAttackReceivedPassiveHandler : IPassiveHandler
    {
        void OnBasicAttackReceived(UnitRuntimeState owner, EnemyRuntimeState attacker, PassiveDataSO passive, PassiveTuning tuning, BasicAttackResult result);
    }

    internal interface IUnitBlockStartedPassiveHandler : IPassiveHandler
    {
        void OnBlockStarted(UnitRuntimeState owner, EnemyRuntimeState enemy, PassiveDataSO passive, PassiveTuning tuning);
    }

    internal interface IUnitBlockEndedPassiveHandler : IPassiveHandler
    {
        void OnBlockEnded(UnitRuntimeState owner, EnemyRuntimeState enemy, PassiveDataSO passive, PassiveTuning tuning);
    }
}
