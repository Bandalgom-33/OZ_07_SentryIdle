using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Raid.Data;
using EndlessGuard.Unit.Runtime;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    public enum RaidFieldBuffChangeKind
    {
        Activated = 0,
        StackIncreased = 1,
        Refreshed = 2,
        Expired = 3,
        Cleared = 4
    }

    public readonly struct RaidFieldBuffState
    {
        public RaidItemType Type { get; }
        public int Stack { get; }
        public int MaxStack { get; }
        public float RemainingSeconds { get; }
        public float DurationSeconds { get; }
        public float EffectPercentPerStack { get; }
        public bool IsActive => Stack > 0 && RemainingSeconds > 0f;
        public float NormalizedRemaining => DurationSeconds > 0f ? Mathf.Clamp01(RemainingSeconds / DurationSeconds) : 0f;
        public float TotalEffectPercent => EffectPercentPerStack * Stack;

        public RaidFieldBuffState(RaidItemType type, int stack, int maxStack, float remainingSeconds, float durationSeconds, float effectPercentPerStack)
        {
            Type = type;
            Stack = Mathf.Max(0, stack);
            MaxStack = Mathf.Max(1, maxStack);
            RemainingSeconds = Mathf.Max(0f, remainingSeconds);
            DurationSeconds = Mathf.Max(0f, durationSeconds);
            EffectPercentPerStack = Mathf.Max(0f, effectPercentPerStack);
        }
    }

    public readonly struct RaidFieldBuffChangedInfo
    {
        public RaidFieldBuffState State { get; }
        public int PreviousStack { get; }
        public RaidFieldBuffChangeKind Kind { get; }

        public RaidFieldBuffChangedInfo(RaidFieldBuffState state, int previousStack, RaidFieldBuffChangeKind kind)
        {
            State = state;
            PreviousStack = Mathf.Max(0, previousStack);
            Kind = kind;
        }
    }

    [DisallowMultipleComponent]
    public sealed class RaidFieldBuffRuntime : MonoBehaviour
    {
        private const float HealTickIntervalSeconds = 1f;
        private readonly Dictionary<UnitRuntimeState, UnitBuffModifiers> modifiersByUnit = new Dictionary<UnitRuntimeState, UnitBuffModifiers>(RaidRosterRuntime.TotalSlots);
        private readonly List<UnitRuntimeState> staleUnits = new List<UnitRuntimeState>(RaidRosterRuntime.TotalSlots);
        private RaidBattleController battle;
        private RaidDeploymentRuntime deployment;
        private BuffState attack;
        private BuffState attackSpeed;
        private BuffState heal;
        private float healTickAccumulator;

        public event Action<RaidFieldBuffChangedInfo> OnBuffChanged;

        public RaidItemConfigSO Config => battle != null && battle.Config != null ? battle.Config.ItemConfig : null;

        private void Awake()
        {
            battle = GetComponent<RaidBattleController>();
            deployment = GetComponent<RaidDeploymentRuntime>();
        }

        private void OnEnable()
        {
            ResolveDependencies();

            if (battle == null || deployment == null)
            {
                Debug.LogError("RaidFieldBuffRuntime은 RaidBattleController와 RaidDeploymentRuntime이 필요합니다.", this);
                enabled = false;
                return;
            }

            battle.OnRaidPreparing += HandleRaidPreparing;
            battle.OnRaidEnded += HandleRaidEnded;
            deployment.OnUnitRemoved += HandleUnitRemoved;
        }

        private void OnDisable()
        {
            if (battle != null)
            {
                battle.OnRaidPreparing -= HandleRaidPreparing;
                battle.OnRaidEnded -= HandleRaidEnded;
            }

            if (deployment != null)
            {
                deployment.OnUnitRemoved -= HandleUnitRemoved;
            }

            ClearAll(false);
        }

        private void Update()
        {
            if (battle == null || battle.State != RaidBattleState.Running)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            float activeHealDelta = heal.Stack > 0 && heal.RemainingSeconds > 0f ? Mathf.Min(deltaTime, heal.RemainingSeconds) : 0f;
            TickHeal(activeHealDelta);
            TickState(ref attack, RaidItemType.Attack, deltaTime);
            TickState(ref attackSpeed, RaidItemType.AttackSpeed, deltaTime);
            TickState(ref heal, RaidItemType.Heal, deltaTime);
        }

        public RaidFieldBuffState GetState(RaidItemType type)
        {
            RaidItemConfigSO config = Config;
            int maxStack = config != null ? config.MaxBuffStacks : 10;
            BuffState state = GetInternalState(type);
            float duration = state.DurationSeconds;
            if (duration <= 0f && config != null && battle != null)
            {
                duration = config.GetBuffDurationSeconds(type, battle.CurrentPhase);
            }

            float effectPercent = config != null ? config.GetBuffEffectPercent(type) : 0f;
            return new RaidFieldBuffState(type, state.Stack, maxStack, state.RemainingSeconds, duration, effectPercent);
        }

        public bool Apply(RaidItemType type)
        {
            if (type != RaidItemType.Attack && type != RaidItemType.AttackSpeed && type != RaidItemType.Heal)
            {
                return false;
            }

            RaidItemConfigSO config = Config;
            RaidItemDefinition definition = config != null ? config.GetDefinition(type) : null;
            float effectPercent = config != null ? config.GetBuffEffectPercent(type) : 0f;
            float durationSeconds = config != null && battle != null ? config.GetBuffDurationSeconds(type, battle.CurrentPhase) : 0f;
            if (definition == null || effectPercent <= 0f || durationSeconds <= 0f)
            {
                return false;
            }

            ref BuffState state = ref GetStateRef(type);
            int previousStack = state.Stack;
            int maxStack = config.MaxBuffStacks;
            state.Stack = Mathf.Min(maxStack, Mathf.Max(1, state.Stack + 1));
            state.RemainingSeconds = durationSeconds;
            state.DurationSeconds = durationSeconds;

            if (type == RaidItemType.Heal && previousStack == 0)
            {
                healTickAccumulator = 0f;
            }

            ApplyStateToField(type, state.Stack, definition);

            RaidFieldBuffChangeKind kind;
            if (previousStack == 0)
            {
                kind = RaidFieldBuffChangeKind.Activated;
            }
            else if (state.Stack > previousStack)
            {
                kind = RaidFieldBuffChangeKind.StackIncreased;
            }
            else
            {
                kind = RaidFieldBuffChangeKind.Refreshed;
            }

            OnBuffChanged?.Invoke(new RaidFieldBuffChangedInfo(GetState(type), previousStack, kind));
            return true;
        }

        private void TickState(ref BuffState state, RaidItemType type, float deltaTime)
        {
            if (state.Stack <= 0 || state.RemainingSeconds <= 0f)
            {
                return;
            }

            state.RemainingSeconds -= deltaTime;
            if (state.RemainingSeconds > 0f)
            {
                return;
            }

            int previousStack = state.Stack;
            state = default;
            if (type == RaidItemType.Heal)
            {
                healTickAccumulator = 0f;
            }

            RemoveTypeFromAllUnits(type);
            OnBuffChanged?.Invoke(new RaidFieldBuffChangedInfo(GetState(type), previousStack, RaidFieldBuffChangeKind.Expired));
        }

        private void TickHeal(float deltaTime)
        {
            if (deltaTime <= 0f || heal.Stack <= 0)
            {
                return;
            }

            healTickAccumulator += deltaTime;
            while (healTickAccumulator >= HealTickIntervalSeconds)
            {
                healTickAccumulator -= HealTickIntervalSeconds;
                ApplyHealTick();
            }
        }

        private void ApplyHealTick()
        {
            RaidItemConfigSO config = Config;
            if (config == null || heal.Stack <= 0)
            {
                return;
            }

            float healPercentPerSecond = config.GetBuffEffectPercent(RaidItemType.Heal) * heal.Stack;
            if (healPercentPerSecond <= 0f)
            {
                return;
            }

            float ratio = healPercentPerSecond * 0.01f;
            foreach (KeyValuePair<UnitRuntimeState, UnitBuffModifiers> pair in modifiersByUnit)
            {
                UnitRuntimeState unit = pair.Key;
                UnitBuffModifiers modifiers = pair.Value;
                if (!modifiers.HealActive || !IsLiveDeployedUnit(unit))
                {
                    continue;
                }

                float healedAmount = unit.Heal(unit.Stats.MaxHp * ratio);
                if (healedAmount <= 0f)
                {
                    continue;
                }

                if (modifiers.Bars != null)
                {
                    modifiers.Bars.PlayHealItemFeedback(healedAmount);
                }

                if (ShouldShowHealNumber(unit))
                {
                    Vector3 numberPosition = unit.Anchors != null && unit.Anchors.EffectPoint != null ? unit.Anchors.EffectPoint.position : unit.transform.position;
                    float numberScale = battle != null && battle.Config != null ? battle.Config.RaidCombatNumberScale : 0.72f;
                    DamageNumberPool.ShowHeal(unit.Health, healedAmount, numberPosition, numberScale);
                }
            }
        }

        private static bool ShouldShowHealNumber(UnitRuntimeState unit)
        {
            if (!IsLiveDeployedUnit(unit) || unit.Health == null || unit.Health.NormalizedHp >= 0.9999f)
            {
                return false;
            }

            if (unit.Block != null && unit.Block.Count > 0)
            {
                return false;
            }

            return unit.Attack == null || !unit.Attack.HasCombatTarget;
        }

        private void ApplyStateToField(RaidItemType type, int stack, RaidItemDefinition definition)
        {
            foreach (UnitRuntimeState unit in CombatRegistry.Units)
            {
                if (IsLiveDeployedUnit(unit))
                {
                    ApplyStateToUnit(unit, type, stack, definition);
                }
            }
        }

        private void ApplyStateToUnit(UnitRuntimeState unit, RaidItemType type, int stack, RaidItemDefinition definition)
        {
            if (!IsLiveDeployedUnit(unit) || definition == null || stack <= 0)
            {
                return;
            }

            if (!modifiersByUnit.TryGetValue(unit, out UnitBuffModifiers modifiers))
            {
                modifiers = new UnitBuffModifiers();
                modifiersByUnit.Add(unit, modifiers);
            }

            float percent = Config != null ? Config.GetBuffEffectPercent(type) * stack : 0f;
            switch (type)
            {
                case RaidItemType.Attack:
                    modifiers.AttackPhysicalId = SetModifier(unit, PassiveStatType.PhysicalAttack, modifiers.AttackPhysicalId, percent);
                    modifiers.AttackMagicalId = SetModifier(unit, PassiveStatType.MagicalAttack, modifiers.AttackMagicalId, percent);
                    break;
                case RaidItemType.AttackSpeed:
                    if (unit.Attack != null)
                    {
                        unit.Attack.SetBasicAttackRepeatMultiplier(1f + percent * 0.01f);
                        modifiers.AttackSpeedActive = true;
                    }
                    break;
                case RaidItemType.Heal:
                    modifiers.HealActive = true;
                    if (modifiers.Bars == null)
                    {
                        modifiers.Bars = unit.GetComponentInChildren<UnitBars>(true);
                    }
                    break;
            }

            RefreshUnitVisual(unit, type, GetInternalState(type).RemainingSeconds, stack, Config != null ? Config.MaxBuffStacks : 10, definition);
        }

        private static int SetModifier(UnitRuntimeState unit, PassiveStatType statType, int modifierId, float percent)
        {
            if (unit == null || unit.Stats == null || !unit.Stats.IsInitialized)
            {
                return 0;
            }

            if (modifierId > 0 && unit.Stats.UpdateModifier(modifierId, 0f, percent))
            {
                return modifierId;
            }

            return unit.Stats.AddModifier(statType, 0f, percent);
        }

        private void HandleUnitRemoved(UnitRuntimeState unit)
        {
            if (unit == null)
            {
                return;
            }

            RemoveAllModifiersFromUnit(unit, true);
        }

        private void RefreshUnitVisual(UnitRuntimeState unit, RaidItemType type, float remainingSeconds, int stack, int maxStack, RaidItemDefinition definition)
        {
            if (unit == null || definition == null)
            {
                return;
            }

            RaidItemBuffView view = unit.GetComponent<RaidItemBuffView>();
            if (view == null)
            {
                view = unit.gameObject.AddComponent<RaidItemBuffView>();
            }

            GameObject visualPrefab = definition.BuffVisualPrefab;
            if (visualPrefab == null || stack <= 0 || remainingSeconds <= 0f)
            {
                view.Hide(type);
                return;
            }

            view.Show(type, visualPrefab, remainingSeconds, stack, maxStack);
        }

        private void RemoveTypeFromAllUnits(RaidItemType type)
        {
            if (modifiersByUnit.Count == 0)
            {
                return;
            }

            foreach (KeyValuePair<UnitRuntimeState, UnitBuffModifiers> pair in modifiersByUnit)
            {
                UnitRuntimeState unit = pair.Key;
                UnitBuffModifiers modifiers = pair.Value;
                if (unit == null)
                {
                    continue;
                }

                switch (type)
                {
                    case RaidItemType.Attack:
                        RemoveModifier(unit, modifiers.AttackPhysicalId);
                        RemoveModifier(unit, modifiers.AttackMagicalId);
                        modifiers.AttackPhysicalId = 0;
                        modifiers.AttackMagicalId = 0;
                        break;
                    case RaidItemType.AttackSpeed:
                        if (unit.Attack != null)
                        {
                            unit.Attack.ResetBasicAttackRepeatMultiplier();
                        }

                        modifiers.AttackSpeedActive = false;
                        break;
                    case RaidItemType.Heal:
                        modifiers.HealActive = false;
                        break;
                }

                RaidItemBuffView view = unit.GetComponent<RaidItemBuffView>();
                if (view != null)
                {
                    view.Hide(type);
                }
            }

            CleanupEmptyModifierEntries();
        }

        private void RemoveAllModifiersFromUnit(UnitRuntimeState unit, bool clearVisual)
        {
            if (unit == null || !modifiersByUnit.TryGetValue(unit, out UnitBuffModifiers modifiers))
            {
                return;
            }

            RemoveModifier(unit, modifiers.AttackPhysicalId);
            RemoveModifier(unit, modifiers.AttackMagicalId);
            if (unit.Attack != null)
            {
                unit.Attack.ResetBasicAttackRepeatMultiplier();
            }

            modifiersByUnit.Remove(unit);

            if (clearVisual)
            {
                RaidItemBuffView view = unit.GetComponent<RaidItemBuffView>();
                if (view != null)
                {
                    view.Clear();
                }
            }
        }

        private static void RemoveModifier(UnitRuntimeState unit, int modifierId)
        {
            if (unit != null && unit.Stats != null && modifierId > 0)
            {
                unit.Stats.RemoveModifier(modifierId);
            }
        }

        private void CleanupEmptyModifierEntries()
        {
            if (modifiersByUnit.Count == 0)
            {
                return;
            }

            staleUnits.Clear();
            foreach (KeyValuePair<UnitRuntimeState, UnitBuffModifiers> pair in modifiersByUnit)
            {
                if (!pair.Value.HasAny && pair.Key != null)
                {
                    staleUnits.Add(pair.Key);
                }
            }

            for (int i = 0; i < staleUnits.Count; i++)
            {
                modifiersByUnit.Remove(staleUnits[i]);
            }

            staleUnits.Clear();
        }

        private void HandleRaidPreparing()
        {
            ClearAll(true);
        }

        private void HandleRaidEnded(RaidBattleResult result)
        {
            ClearAll(true);
        }

        private void ClearAll(bool notify)
        {
            int previousAttack = attack.Stack;
            int previousAttackSpeed = attackSpeed.Stack;
            int previousHeal = heal.Stack;

            if (modifiersByUnit.Count > 0)
            {
                staleUnits.Clear();
                foreach (UnitRuntimeState unit in modifiersByUnit.Keys)
                {
                    staleUnits.Add(unit);
                }

                for (int i = 0; i < staleUnits.Count; i++)
                {
                    UnitRuntimeState unit = staleUnits[i];
                    if (unit != null)
                    {
                        RemoveAllModifiersFromUnit(unit, true);
                    }
                }

                staleUnits.Clear();
            }

            modifiersByUnit.Clear();
            attack = default;
            attackSpeed = default;
            heal = default;
            healTickAccumulator = 0f;

            if (!notify)
            {
                return;
            }

            if (previousAttack > 0)
            {
                OnBuffChanged?.Invoke(new RaidFieldBuffChangedInfo(GetState(RaidItemType.Attack), previousAttack, RaidFieldBuffChangeKind.Cleared));
            }

            if (previousAttackSpeed > 0)
            {
                OnBuffChanged?.Invoke(new RaidFieldBuffChangedInfo(GetState(RaidItemType.AttackSpeed), previousAttackSpeed, RaidFieldBuffChangeKind.Cleared));
            }

            if (previousHeal > 0)
            {
                OnBuffChanged?.Invoke(new RaidFieldBuffChangedInfo(GetState(RaidItemType.Heal), previousHeal, RaidFieldBuffChangeKind.Cleared));
            }
        }

        private BuffState GetInternalState(RaidItemType type)
        {
            switch (type)
            {
                case RaidItemType.Attack:
                    return attack;
                case RaidItemType.AttackSpeed:
                    return attackSpeed;
                case RaidItemType.Heal:
                    return heal;
                default:
                    return default;
            }
        }

        private ref BuffState GetStateRef(RaidItemType type)
        {
            if (type == RaidItemType.Attack)
            {
                return ref attack;
            }

            if (type == RaidItemType.AttackSpeed)
            {
                return ref attackSpeed;
            }

            return ref heal;
        }

        private void ResolveDependencies()
        {
            if (battle == null)
            {
                battle = GetComponent<RaidBattleController>();
            }

            if (deployment == null)
            {
                deployment = GetComponent<RaidDeploymentRuntime>();
            }
        }

        private static bool IsLiveDeployedUnit(UnitRuntimeState unit)
        {
            return unit != null && !unit.IsSummon && unit.IsInitialized && unit.Health != null && !unit.Health.IsDead && unit.GridPosition != null && unit.GridPosition.IsInitialized && unit.Stats != null && unit.Stats.IsInitialized;
        }

        private struct BuffState
        {
            public int Stack;
            public float RemainingSeconds;
            public float DurationSeconds;
        }

        private sealed class UnitBuffModifiers
        {
            public int AttackPhysicalId;
            public int AttackMagicalId;
            public bool AttackSpeedActive;
            public bool HealActive;
            public UnitBars Bars;
            public bool HasAny => AttackPhysicalId > 0 || AttackMagicalId > 0 || AttackSpeedActive || HealActive;
        }
    }
}
