using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    [Serializable]
    public readonly struct RaidRuntimeStatModifier
    {
        public PassiveStatType StatType { get; }
        public float FlatBonus { get; }
        public float PercentBonus { get; }

        public RaidRuntimeStatModifier(PassiveStatType statType, float flatBonus, float percentBonus)
        {
            StatType = statType;
            FlatBonus = float.IsNaN(flatBonus) || float.IsInfinity(flatBonus) ? 0f : flatBonus;
            PercentBonus = float.IsNaN(percentBonus) || float.IsInfinity(percentBonus) ? 0f : percentBonus;
        }
    }

    public sealed class RaidRosterSelection
    {
        private readonly RaidRuntimeStatModifier[] runtimeModifiers;

        public UnitDataSO UnitData { get; }
        public UnitProgressData Progress { get; }
        public IReadOnlyList<RaidRuntimeStatModifier> RuntimeModifiers => runtimeModifiers;

        public RaidRosterSelection(
            UnitDataSO unitData,
            UnitProgressData progress = null,
            IReadOnlyList<RaidRuntimeStatModifier> runtimeModifiers = null)
        {
            UnitData = unitData;
            Progress = progress;

            if (runtimeModifiers == null || runtimeModifiers.Count == 0)
            {
                this.runtimeModifiers = Array.Empty<RaidRuntimeStatModifier>();
                return;
            }

            this.runtimeModifiers = new RaidRuntimeStatModifier[runtimeModifiers.Count];

            for (int i = 0; i < runtimeModifiers.Count; i++)
            {
                this.runtimeModifiers[i] = runtimeModifiers[i];
            }
        }
    }

    public static class RaidRosterTransferService
    {
        private static readonly List<RaidRosterSelection> pendingRoster = new List<RaidRosterSelection>(RaidRosterRuntime.TotalSlots);

        // Enter Play Mode Options에서 Domain Reload가 꺼져 있어도 이전 Play의 Unity Object 참조를 남기지 않습니다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            pendingRoster.Clear();
        }

        public static bool HasPendingRoster => pendingRoster.Count == RaidRosterRuntime.TotalSlots;

        public static bool SetPendingRoster(IReadOnlyList<RaidRosterSelection> selections)
        {
            if (selections == null || selections.Count != RaidRosterRuntime.TotalSlots)
            {
                return false;
            }

            HashSet<string> unitIds = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < selections.Count; i++)
            {
                RaidRosterSelection selection = selections[i];

                if (selection == null ||
                    selection.UnitData == null ||
                    string.IsNullOrWhiteSpace(selection.UnitData.UnitId) ||
                    !unitIds.Add(selection.UnitData.UnitId))
                {
                    return false;
                }

                if (selection.Progress != null && !selection.Progress.Matches(selection.UnitData))
                {
                    return false;
                }
            }

            pendingRoster.Clear();

            for (int i = 0; i < selections.Count; i++)
            {
                pendingRoster.Add(selections[i]);
            }

            return true;
        }

        public static bool CopyPendingRoster(List<RaidRosterSelection> destination)
        {
            if (!HasPendingRoster || destination == null)
            {
                return false;
            }

            destination.Clear();
            destination.AddRange(pendingRoster);
            return true;
        }

        public static void Clear()
        {
            pendingRoster.Clear();
        }
    }
}
