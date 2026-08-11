using System;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    public static class CommonGrowthService
    {
        private const int StatCount = 11;
        private static readonly float[] values = new float[StatCount];

        internal static event Action<GrowthStatMask> OnChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            Array.Clear(values, 0, values.Length);
            OnChanged = null;
        }

        public static float Get(GrowthStatMask stat)
        {
            int index = GetIndex(stat);
            return index >= 0 ? values[index] : 0f;
        }

        public static bool Set(GrowthStatMask stat, float value)
        {
            int index = GetIndex(stat);

            if (index < 0)
            {
                return false;
            }

            float sanitizedValue = Sanitize(value);
            float previousValue = values[index];

            if (Mathf.Approximately(previousValue, sanitizedValue))
            {
                return false;
            }

            values[index] = sanitizedValue;
            OnChanged?.Invoke(stat);
            UnitProgressEvents.NotifyGrowthChanged(new UnitGrowthChangedInfo(string.Empty, stat, previousValue, sanitizedValue));
            return true;
        }

        public static bool Add(GrowthStatMask stat, float amount)
        {
            if (amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount))
            {
                return false;
            }

            return Set(stat, Get(stat) + amount);
        }

        public static void Clear()
        {
            bool changed = false;

            for (int i = 0; i < values.Length; i++)
            {
                if (!Mathf.Approximately(values[i], 0f))
                {
                    values[i] = 0f;
                    changed = true;
                }
            }

            if (!changed)
            {
                return;
            }

            OnChanged?.Invoke(GrowthStatMask.All);
            UnitProgressEvents.NotifyGrowthChanged(new UnitGrowthChangedInfo(string.Empty, GrowthStatMask.All, 0f, 0f));
        }

        private static int GetIndex(GrowthStatMask stat)
        {
            switch (stat)
            {
                case GrowthStatMask.MaxHp: return 0;
                case GrowthStatMask.HpRegenPerSecond: return 1;
                case GrowthStatMask.PhysicalAttack: return 2;
                case GrowthStatMask.MagicalAttack: return 3;
                case GrowthStatMask.AttacksPerSecond: return 4;
                case GrowthStatMask.PhysicalDefense: return 5;
                case GrowthStatMask.MagicalDefense: return 6;
                case GrowthStatMask.Accuracy: return 7;
                case GrowthStatMask.Evasion: return 8;
                case GrowthStatMask.CriticalChancePercent: return 9;
                case GrowthStatMask.CriticalDamageBonusPercent: return 10;
                default: return -1;
            }
        }

        private static float Sanitize(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Max(0f, value);
        }
    }
}