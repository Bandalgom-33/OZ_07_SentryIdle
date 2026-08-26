using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Runtime;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    public enum RaidRosterSlotStatus
    {
        Empty = 0,
        Ready = 1,
        Deployed = 2,
        RedeployCooldown = 3
    }

    public sealed class RaidRosterSlotState
    {
        public int TeamIndex { get; }
        public int SlotIndex { get; }
        private readonly List<RaidRuntimeStatModifier> runtimeModifiers = new List<RaidRuntimeStatModifier>(8);

        public UnitDataSO UnitData { get; private set; }
        public UnitProgressData Progress { get; private set; }
        public IReadOnlyList<RaidRuntimeStatModifier> RuntimeModifiers => runtimeModifiers;
        public RaidRosterSlotStatus Status { get; private set; }
        public float RedeployRemaining { get; private set; }
        public float RedeployDuration { get; private set; }
        public UnitRuntimeState DeployedUnit { get; private set; }

        public string UnitId => UnitData != null ? UnitData.UnitId : string.Empty;
        public bool HasUnit => UnitData != null;
        public bool CanDeploy => HasUnit && Status == RaidRosterSlotStatus.Ready;
        public float RedeployReadyRatio => RedeployDuration > 0f ? 1f - Mathf.Clamp01(RedeployRemaining / RedeployDuration) : 1f;

        internal RaidRosterSlotState(int teamIndex, int slotIndex)
        {
            TeamIndex = teamIndex;
            SlotIndex = slotIndex;
            Status = RaidRosterSlotStatus.Empty;
        }

        internal void Assign(UnitDataSO unitData)
        {
            Assign(unitData != null ? new RaidRosterSelection(unitData) : null);
        }

        internal void Assign(RaidRosterSelection selection)
        {
            UnitData = selection != null ? selection.UnitData : null;
            Progress = selection != null ? selection.Progress : null;
            runtimeModifiers.Clear();

            if (selection != null && selection.RuntimeModifiers != null)
            {
                for (int i = 0; i < selection.RuntimeModifiers.Count; i++)
                {
                    RaidRuntimeStatModifier modifier = selection.RuntimeModifiers[i];

                    if (modifier.StatType != PassiveStatType.None)
                    {
                        runtimeModifiers.Add(modifier);
                    }
                }
            }

            Status = UnitData != null ? RaidRosterSlotStatus.Ready : RaidRosterSlotStatus.Empty;
            RedeployRemaining = 0f;
            RedeployDuration = 0f;
            DeployedUnit = null;
        }

        internal bool ApplyBuild(UnitRuntimeState unit)
        {
            if (UnitData == null ||
                unit == null ||
                unit.DataLink == null ||
                !unit.DataLink.HasData ||
                !string.Equals(unit.UnitId, UnitData.UnitId, StringComparison.Ordinal) ||
                unit.Stats == null ||
                !unit.Stats.IsInitialized)
            {
                return false;
            }

            if (Progress != null && !unit.ApplyProgression(Progress))
            {
                return false;
            }

            for (int i = 0; i < runtimeModifiers.Count; i++)
            {
                RaidRuntimeStatModifier modifier = runtimeModifiers[i];
                unit.Stats.AddModifier(modifier.StatType, modifier.FlatBonus, modifier.PercentBonus);
            }

            if (unit.Health != null && unit.Health.IsInitialized)
            {
                unit.Health.Initialize(unit.Stats.MaxHp);
            }

            return true;
        }

        internal void SetReady()
        {
            if (UnitData == null)
            {
                Status = RaidRosterSlotStatus.Empty;
                return;
            }

            Status = RaidRosterSlotStatus.Ready;
            RedeployRemaining = 0f;
            RedeployDuration = 0f;
            DeployedUnit = null;
        }

        internal void SetDeployed(UnitRuntimeState unit)
        {
            if (UnitData == null || unit == null)
            {
                return;
            }

            Status = RaidRosterSlotStatus.Deployed;
            RedeployRemaining = 0f;
            RedeployDuration = 0f;
            DeployedUnit = unit;
        }

        internal void StartRedeployCooldown(float duration)
        {
            if (UnitData == null)
            {
                Status = RaidRosterSlotStatus.Empty;
                return;
            }

            RedeployDuration = Mathf.Max(0f, duration);
            RedeployRemaining = RedeployDuration;
            DeployedUnit = null;
            Status = RedeployDuration > 0f ? RaidRosterSlotStatus.RedeployCooldown : RaidRosterSlotStatus.Ready;
        }

        internal bool StepCooldown(float deltaTime)
        {
            if (Status != RaidRosterSlotStatus.RedeployCooldown)
            {
                return false;
            }

            float previous = RedeployRemaining;
            RedeployRemaining = Mathf.Max(0f, RedeployRemaining - Mathf.Max(0f, deltaTime));

            if (RedeployRemaining <= 0f)
            {
                SetReady();
                return true;
            }

            return !Mathf.Approximately(previous, RedeployRemaining);
        }
    }

    [DisallowMultipleComponent]
    public sealed class RaidRosterRuntime : MonoBehaviour
    {
        public const int TeamCount = 2;
        public const int SlotsPerTeam = 8;
        public const int TotalSlots = TeamCount * SlotsPerTeam;

        private readonly RaidRosterSlotState[] slots = new RaidRosterSlotState[TotalSlots];
        private readonly List<RaidRosterSelection> externalRoster = new List<RaidRosterSelection>(TotalSlots);
        private RaidBattleController battle;
        private bool useExternalRoster;
        private float registrySyncElapsed;
        private float cooldownPublishElapsed;
        private bool preparedForRaidStart;

        public event Action OnRosterRebuilt;
        public event Action<RaidRosterSlotState> OnSlotChanged;

        public IReadOnlyList<RaidRosterSlotState> Slots => slots;
        public bool UsesExternalRoster => useExternalRoster;

        private void Awake()
        {
            battle = GetComponent<RaidBattleController>();

            for (int team = 0; team < TeamCount; team++)
            {
                for (int slot = 0; slot < SlotsPerTeam; slot++)
                {
                    int index = team * SlotsPerTeam + slot;
                    slots[index] = new RaidRosterSlotState(team, slot);
                }
            }
        }

        private void OnEnable()
        {
            if (battle == null)
            {
                battle = GetComponent<RaidBattleController>();
            }

            if (battle == null)
            {
                Debug.LogError("RaidRosterRuntime은 RaidBattleController와 같은 오브젝트에 있어야 합니다.", this);
                enabled = false;
                return;
            }

            battle.OnRaidPreparing += HandleRaidPreparing;
            battle.OnRaidStarted += HandleRaidStarted;
            battle.OnRaidEnded += HandleRaidEnded;
            battle.OnUnitForcedRetreat += HandleForcedRetreat;
            CombatEvents.OnUnitDied += HandleUnitDied;

            if (!useExternalRoster)
            {
                List<RaidRosterSelection> pending = new List<RaidRosterSelection>(TotalSlots);

                if (RaidRosterTransferService.CopyPendingRoster(pending))
                {
                    SetExternalRoster(pending);
                    return;
                }
            }

            RebuildRoster();
        }

        private void OnDisable()
        {
            if (battle != null)
            {
                battle.OnRaidPreparing -= HandleRaidPreparing;
                battle.OnRaidStarted -= HandleRaidStarted;
                battle.OnRaidEnded -= HandleRaidEnded;
                battle.OnUnitForcedRetreat -= HandleForcedRetreat;
            }

            CombatEvents.OnUnitDied -= HandleUnitDied;
        }

        private void Update()
        {
            if (battle == null || battle.State != RaidBattleState.Running)
            {
                return;
            }

            float deltaTime = Mathf.Max(0f, Time.deltaTime);
            cooldownPublishElapsed += deltaTime;
            bool publishCooldown = cooldownPublishElapsed >= 0.1f;

            if (publishCooldown)
            {
                cooldownPublishElapsed = 0f;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                RaidRosterSlotState slot = slots[i];
                bool changed = slot.StepCooldown(deltaTime);

                if (changed && (publishCooldown || slot.Status == RaidRosterSlotStatus.Ready))
                {
                    OnSlotChanged?.Invoke(slot);
                }
            }

            registrySyncElapsed += deltaTime;

            if (registrySyncElapsed >= 0.25f)
            {
                registrySyncElapsed = 0f;
                SyncDeployedUnits();
            }
        }

        public RaidRosterSlotState GetSlot(int teamIndex, int slotIndex)
        {
            if (teamIndex < 0 || teamIndex >= TeamCount || slotIndex < 0 || slotIndex >= SlotsPerTeam)
            {
                return null;
            }

            return slots[teamIndex * SlotsPerTeam + slotIndex];
        }

        public bool SetExternalRoster(IReadOnlyList<UnitDataSO> units)
        {
            if (!ValidateRoster(units, true))
            {
                return false;
            }

            List<RaidRosterSelection> selections = new List<RaidRosterSelection>(TotalSlots);

            for (int i = 0; i < TotalSlots; i++)
            {
                selections.Add(new RaidRosterSelection(units[i]));
            }

            return SetExternalRoster(selections);
        }

        public bool SetExternalRoster(IReadOnlyList<RaidRosterSelection> selections)
        {
            if (!ValidateSelectionRoster(selections, true))
            {
                return false;
            }

            externalRoster.Clear();

            for (int i = 0; i < TotalSlots; i++)
            {
                externalRoster.Add(selections[i]);
            }

            useExternalRoster = true;
            RebuildRoster();
            return true;
        }

        public void ClearExternalRoster()
        {
            externalRoster.Clear();
            useExternalRoster = false;
            RebuildRoster();
        }

        public bool CanDeploy(UnitDataSO unitData)
        {
            RaidRosterSlotState slot = FindSlot(unitData != null ? unitData.UnitId : string.Empty);
            return slot != null && slot.CanDeploy;
        }

        public bool MarkDeployed(UnitRuntimeState unit)
        {
            if (unit == null || unit.IsSummon || unit.Health == null || unit.Health.IsDead)
            {
                return false;
            }

            RaidRosterSlotState slot = FindSlot(unit.UnitId);

            if (slot == null || slot.Status == RaidRosterSlotStatus.RedeployCooldown)
            {
                return false;
            }

            if (slot.Status == RaidRosterSlotStatus.Deployed && slot.DeployedUnit == unit)
            {
                return true;
            }

            slot.SetDeployed(unit);
            OnSlotChanged?.Invoke(slot);
            return true;
        }

        private void HandleRaidPreparing()
        {
            RebuildRoster();
            registrySyncElapsed = 0f;
            cooldownPublishElapsed = 0f;
            preparedForRaidStart = true;
        }

        private void HandleRaidStarted()
        {
            if (!preparedForRaidStart)
            {
                HandleRaidPreparing();
            }

            preparedForRaidStart = false;
        }

        private void HandleRaidEnded(RaidBattleResult result)
        {
            preparedForRaidStart = false;
            SyncDeployedUnits();
        }

        private void HandleUnitDied(UnitDiedInfo info)
        {
            if (battle == null || battle.State != RaidBattleState.Running)
            {
                return;
            }

            RaidRosterSlotState slot = FindSlot(info.UnitId);

            if (slot == null || slot.UnitData == null)
            {
                return;
            }

            slot.StartRedeployCooldown(slot.UnitData.RedeployTime);
            OnSlotChanged?.Invoke(slot);
        }

        private void HandleForcedRetreat(RaidForcedRetreatInfo info)
        {
            if (info.IsSummon)
            {
                return;
            }

            RaidRosterSlotState slot = FindSlot(info.UnitId);

            if (slot != null)
            {
                slot.SetReady();
                OnSlotChanged?.Invoke(slot);
            }

            if (info.RefundCost > 0 && battle != null && battle.State == RaidBattleState.Running)
            {
                battle.AddCost(info.RefundCost);
            }
        }

        private void SyncDeployedUnits()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                RaidRosterSlotState slot = slots[i];

                if (slot.Status == RaidRosterSlotStatus.Deployed &&
                    (slot.DeployedUnit == null || !slot.DeployedUnit.isActiveAndEnabled || slot.DeployedUnit.Health == null || slot.DeployedUnit.Health.IsDead))
                {
                    slot.SetReady();
                    OnSlotChanged?.Invoke(slot);
                }
            }

            foreach (UnitRuntimeState unit in CombatRegistry.Units)
            {
                if (unit == null || unit.IsSummon || unit.Health == null || unit.Health.IsDead)
                {
                    continue;
                }

                MarkDeployed(unit);
            }
        }

        private void RebuildRoster()
        {
            if (useExternalRoster)
            {
                for (int i = 0; i < TotalSlots; i++)
                {
                    RaidRosterSelection selection = i < externalRoster.Count ? externalRoster[i] : null;
                    slots[i].Assign(selection);
                }

                if (!ValidateSelectionRoster(externalRoster, false))
                {
                    Debug.LogWarning("Raid 외부 Roster가 16명의 유효한 고유 캐릭터/성장 데이터로 구성되지 않았습니다.", this);
                }

                OnRosterRebuilt?.Invoke();
                return;
            }

            IReadOnlyList<UnitDataSO> source = battle != null && battle.Config != null ? battle.Config.DevelopmentRoster : null;

            for (int i = 0; i < TotalSlots; i++)
            {
                UnitDataSO unitData = source != null && i < source.Count ? source[i] : null;
                slots[i].Assign(unitData);
            }

            if (!ValidateRoster(source, false))
            {
                Debug.LogWarning("Raid 개발용 Roster가 16명의 유효한 고유 캐릭터로 구성되지 않았습니다. 빈 슬롯은 UI에 EMPTY로 표시됩니다.", this);
            }

            OnRosterRebuilt?.Invoke();
        }

        private RaidRosterSlotState FindSlot(string unitId)
        {
            if (string.IsNullOrWhiteSpace(unitId))
            {
                return null;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                RaidRosterSlotState slot = slots[i];

                if (slot.UnitData != null && string.Equals(slot.UnitData.UnitId, unitId, StringComparison.Ordinal))
                {
                    return slot;
                }
            }

            return null;
        }

        private static bool ValidateSelectionRoster(IReadOnlyList<RaidRosterSelection> selections, bool logError)
        {
            if (selections == null || selections.Count != TotalSlots)
            {
                if (logError)
                {
                    Debug.LogError($"Raid Roster에는 정확히 {TotalSlots}명의 캐릭터 Build Snapshot이 필요합니다.");
                }

                return false;
            }

            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < TotalSlots; i++)
            {
                RaidRosterSelection selection = selections[i];
                UnitDataSO unit = selection != null ? selection.UnitData : null;

                if (unit == null ||
                    string.IsNullOrWhiteSpace(unit.UnitId) ||
                    !ids.Add(unit.UnitId) ||
                    (selection.Progress != null && !selection.Progress.Matches(unit)))
                {
                    if (logError)
                    {
                        Debug.LogError($"Raid Roster {i + 1}번 Build Snapshot의 캐릭터/Progress가 비어 있거나 ID가 중복/불일치합니다.");
                    }

                    return false;
                }
            }

            return true;
        }

        private static bool ValidateRoster(IReadOnlyList<UnitDataSO> units, bool logError)
        {
            if (units == null || units.Count < TotalSlots)
            {
                if (logError)
                {
                    Debug.LogError($"Raid Roster에는 정확히 {TotalSlots}명의 캐릭터가 필요합니다.");
                }

                return false;
            }

            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < TotalSlots; i++)
            {
                UnitDataSO unit = units[i];

                if (unit == null || string.IsNullOrWhiteSpace(unit.UnitId) || !ids.Add(unit.UnitId))
                {
                    if (logError)
                    {
                        Debug.LogError($"Raid Roster {i + 1}번 슬롯의 캐릭터가 비어 있거나 ID가 중복되었습니다.");
                    }

                    return false;
                }
            }

            return true;
        }
    }
}
