using System;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    public enum UnitAnimationCue
    {
        Buff = 0,
        Summon = 1,
        Skill = 2
    }

    public readonly struct UnitAnimationCueInfo
    {
        public UnitRuntimeState Unit { get; }
        public UnitAnimationCue Cue { get; }

        public UnitAnimationCueInfo(UnitRuntimeState unit, UnitAnimationCue cue)
        {
            Unit = unit;
            Cue = cue;
        }
    }

    public static class UnitAnimationCueEvents
    {
        public static event Action<UnitAnimationCueInfo> OnRequested;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            OnRequested = null;
        }

        public static void NotifyBuff(UnitRuntimeState unit)
        {
            Publish(unit, UnitAnimationCue.Buff);
        }

        public static void NotifySummon(UnitRuntimeState unit)
        {
            Publish(unit, UnitAnimationCue.Summon);
        }

        public static void NotifySkill(UnitRuntimeState unit)
        {
            Publish(unit, UnitAnimationCue.Skill);
        }

        private static void Publish(UnitRuntimeState unit, UnitAnimationCue cue)
        {
            if (unit == null || !unit.IsInitialized || unit.Health == null || unit.Health.IsDead)
            {
                return;
            }

            OnRequested?.Invoke(new UnitAnimationCueInfo(unit, cue));
        }
    }
}
