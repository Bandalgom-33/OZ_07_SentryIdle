using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Runtime;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    public readonly struct RaidContributionSnapshot
    {
        public string UnitId { get; }
        public string DisplayName { get; }
        public float Damage { get; }
        public float Ratio { get; }

        public RaidContributionSnapshot(string unitId, string displayName, float damage, float ratio)
        {
            UnitId = unitId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Damage = Mathf.Max(0f, damage);
            Ratio = Mathf.Clamp01(ratio);
        }
    }

    [DisallowMultipleComponent]
    public sealed class RaidContributionRuntime : MonoBehaviour
    {
        private sealed class Entry
        {
            public string UnitId;
            public string DisplayName;
            public float Damage;
        }

        private readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>(StringComparer.Ordinal);
        private readonly List<Entry> sortBuffer = new List<Entry>(RaidRosterRuntime.TotalSlots);
        private RaidBattleController battle;
        private float totalDamage;

        public event Action OnContributionChanged;

        public float TotalDamage => totalDamage;

        public static RaidContributionRuntime EnsureInstalled(GameObject host)
        {
            if (host == null)
            {
                return null;
            }

            RaidContributionRuntime runtime = host.GetComponent<RaidContributionRuntime>();

            if (runtime == null)
            {
                runtime = host.AddComponent<RaidContributionRuntime>();
            }

            return runtime;
        }

        private void Awake()
        {
            battle = GetComponent<RaidBattleController>();
        }

        private void OnEnable()
        {
            if (battle == null)
            {
                battle = GetComponent<RaidBattleController>();
            }

            if (battle != null)
            {
                battle.OnRaidStarted += ResetContribution;
            }

            CombatEvents.OnUnitDamageDealt += HandleUnitDamageDealt;
        }

        private void OnDisable()
        {
            if (battle != null)
            {
                battle.OnRaidStarted -= ResetContribution;
            }

            CombatEvents.OnUnitDamageDealt -= HandleUnitDamageDealt;
        }

        private void HandleUnitDamageDealt(UnitDamageDealtInfo info)
        {
            if (battle == null ||
                !battle.IsRunning ||
                info.Source == null ||
                info.AppliedDamage <= 0f)
            {
                return;
            }

            RecordDamage(info.Source, info.AppliedDamage);
        }

        public void RecordDamage(UnitRuntimeState attacker, float appliedDamage)
        {
            if (attacker == null ||
                attacker.IsSummon ||
                attacker.DataLink == null ||
                !attacker.DataLink.HasData ||
                appliedDamage <= 0f)
            {
                return;
            }

            string unitId = attacker.UnitId;

            if (string.IsNullOrWhiteSpace(unitId))
            {
                return;
            }

            if (!entries.TryGetValue(unitId, out Entry entry))
            {
                entry = new Entry
                {
                    UnitId = unitId,
                    DisplayName = attacker.DataLink.UnitData.DisplayName,
                    Damage = 0f
                };

                entries.Add(unitId, entry);
            }

            entry.Damage += appliedDamage;
            totalDamage += appliedDamage;
            OnContributionChanged?.Invoke();
        }

        public void FillSorted(List<RaidContributionSnapshot> destination)
        {
            if (destination == null)
            {
                return;
            }

            destination.Clear();
            sortBuffer.Clear();

            foreach (Entry entry in entries.Values)
            {
                if (entry != null && entry.Damage > 0f)
                {
                    sortBuffer.Add(entry);
                }
            }

            sortBuffer.Sort((a, b) =>
            {
                int damageCompare = b.Damage.CompareTo(a.Damage);
                return damageCompare != 0 ? damageCompare : string.CompareOrdinal(a.UnitId, b.UnitId);
            });

            float denominator = Mathf.Max(0.0001f, totalDamage);

            for (int i = 0; i < sortBuffer.Count; i++)
            {
                Entry entry = sortBuffer[i];
                destination.Add(new RaidContributionSnapshot(
                    entry.UnitId,
                    entry.DisplayName,
                    entry.Damage,
                    entry.Damage / denominator));
            }
        }

        public void ResetContribution()
        {
            entries.Clear();
            sortBuffer.Clear();
            totalDamage = 0f;
            OnContributionChanged?.Invoke();
        }
    }
}
