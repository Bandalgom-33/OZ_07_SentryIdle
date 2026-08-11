using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;

namespace EndlessGuard.Unit.Runtime
{
    internal static class PassiveRegistry
    {
        private static readonly Dictionary<Type, IPassiveHandler> handlers = new Dictionary<Type, IPassiveHandler>
        {
            { typeof(AirAttackSO), new AirAttackHandler() },
            { typeof(AllyAidSO), new AllyAidHandler() },
            { typeof(AttackSlowSO), new AttackSlowHandler() },
            { typeof(AttackSpeedSO), new AttackSpeedHandler() },
            { typeof(BerserkSO), new BerserkHandler() },
            { typeof(BlockAttackSO), new BlockAttackHandler() },
            { typeof(BlockGaugeSO), new BlockGaugeHandler() },
            { typeof(CleanseSO), new CleanseHandler() },
            { typeof(CommandSO), new CommandHandler() },
            { typeof(CostGainPassiveSO), new CostGainHandler() },
            { typeof(CritBuffSO), new CritBuffHandler() },
            { typeof(CritSummonSO), new CritSummonHandler() },
            { typeof(DefenseAuraSO), new DefenseAuraHandler() },
            { typeof(DefenseBuffSO), new DefenseBuffHandler() },
            { typeof(ExplosionSO), new ExplosionHandler() },
            { typeof(HealSO), new HealHandler() },
            { typeof(HeavyArmorSO), new HeavyArmorHandler() },
            { typeof(LifeStealSO), new LifeStealHandler() },
            { typeof(LostHpAttackSO), new LostHpAttackHandler() },
            { typeof(MasterSO), new MasterHandler() },
            { typeof(RandomAttackSO), new RandomAttackHandler() },
            { typeof(ReflectSO), new ReflectHandler() },
            { typeof(RushSO), new RushHandler() },
            { typeof(SizeAttackSO), new SizeAttackHandler() },
            { typeof(SizeDamagePassiveSO), new SizeDamageHandler() },
            { typeof(SlowSO), new SlowHandler() },
            { typeof(SnipeBurstSO), new SnipeBurstHandler() },
            { typeof(SnipeSO), new SnipeHandler() },
            { typeof(SummonDefenseSO), new SummonDefenseHandler() },
            { typeof(SummonSO), new SummonHandler() },
            { typeof(WeakSO), new WeakHandler() }
        };

        public static bool TryGet(PassiveDataSO passive, out IPassiveHandler handler)
        {
            handler = null;

            if (passive == null)
            {
                return false;
            }

            return handlers.TryGetValue(passive.GetType(), out handler);
        }
    }
}
