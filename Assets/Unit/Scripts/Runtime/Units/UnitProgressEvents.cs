using System;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    public static class UnitProgressEvents
    {
        public static event Action<UnitProgressChangedInfo> OnUnitProgressChanged;
        public static event Action<UnitGrowthChangedInfo> OnUnitGrowthChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            OnUnitProgressChanged = null;
            OnUnitGrowthChanged = null;
        }

        internal static void PublishProgressChanged(UnitProgressChangedInfo info)
        {
            OnUnitProgressChanged?.Invoke(info);
        }

        public static void NotifyGrowthChanged(UnitGrowthChangedInfo info)
        {
            OnUnitGrowthChanged?.Invoke(info);
        }
    }
}
