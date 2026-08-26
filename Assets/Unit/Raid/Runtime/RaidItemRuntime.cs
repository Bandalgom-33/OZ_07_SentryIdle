using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Raid.Data;
using EndlessGuard.Unit.Runtime;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    [DisallowMultipleComponent]
    public sealed class RaidItemRuntime : MonoBehaviour
    {
        private readonly List<ActiveItem> activeItems = new List<ActiveItem>(8);
        private RaidBattleController battle;
        private RaidBoardRuntime boardRuntime;
        private RaidDeploymentRuntime deployment;
        private RaidRosterRuntime roster;
        private RaidFieldBuffRuntime fieldBuffs;
        private Transform itemRoot;
        private bool missingVisualLogged;
        private bool canUseGroundItemTiles;
        private bool canUseHighGroundItemTiles;

        public event Action<RaidItemType, Vector2Int> OnItemDropped;
        public event Action<RaidItemType, UnitRuntimeState, Vector2Int> OnItemConsumed;

        public int ActiveItemCount => activeItems.Count;
        public RaidItemConfigSO Config => battle != null && battle.Config != null ? battle.Config.ItemConfig : null;

        private void Awake()
        {
            battle = GetComponent<RaidBattleController>();
            boardRuntime = GetComponent<RaidBoardRuntime>();
            deployment = GetComponent<RaidDeploymentRuntime>();
            roster = GetComponent<RaidRosterRuntime>();
            fieldBuffs = GetComponent<RaidFieldBuffRuntime>();
        }

        private void OnEnable()
        {
            ResolveDependencies();

            if (battle == null || boardRuntime == null || deployment == null || roster == null || fieldBuffs == null)
            {
                Debug.LogError("RaidItemRuntime은 RaidBattleController, RaidBoardRuntime, RaidDeploymentRuntime, RaidRosterRuntime, RaidFieldBuffRuntime이 필요합니다.", this);
                enabled = false;
                return;
            }

            battle.OnRaidPreparing += HandleRaidPreparing;
            battle.OnRaidEnded += HandleRaidEnded;
            battle.OnPhaseTransitionStarted += HandlePhaseTransitionStarted;
            deployment.OnUnitDeployed += HandleUnitDeployed;
            CombatEvents.OnEnemyDied += HandleEnemyDied;
        }

        private void OnDisable()
        {
            if (battle != null)
            {
                battle.OnRaidPreparing -= HandleRaidPreparing;
                battle.OnRaidEnded -= HandleRaidEnded;
                battle.OnPhaseTransitionStarted -= HandlePhaseTransitionStarted;
            }

            if (deployment != null)
            {
                deployment.OnUnitDeployed -= HandleUnitDeployed;
            }

            CombatEvents.OnEnemyDied -= HandleEnemyDied;
            ClearItems();
        }

        private void Update()
        {
            TickItems(Time.deltaTime);
        }

        public bool HasItemAt(Vector2Int tile)
        {
            for (int i = 0; i < activeItems.Count; i++)
            {
                if (activeItems[i].Tile == tile)
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryGetItemType(Vector2Int tile, out RaidItemType type)
        {
            for (int i = 0; i < activeItems.Count; i++)
            {
                ActiveItem item = activeItems[i];
                if (item.Tile == tile)
                {
                    type = item.Type;
                    return true;
                }
            }

            type = default;
            return false;
        }

        public float GetDeploymentBonus(UnitDataSO unitData, Vector2Int tile)
        {
            if (unitData == null || !TryGetItemType(tile, out RaidItemType type))
            {
                return 0f;
            }

            RaidItemConfigSO config = Config;
            RaidItemDefinition definition = config != null ? config.GetDefinition(type) : null;
            if (definition == null)
            {
                return 0f;
            }

            switch (type)
            {
                case RaidItemType.Attack:
                    return ScoreAttackItem(unitData, definition);
                case RaidItemType.AttackSpeed:
                    return ScoreAttackSpeedItem(unitData, definition);
                case RaidItemType.Heal:
                    return ScoreHealItem();
                default:
                    return 0f;
            }
        }

        private void HandleRaidPreparing()
        {
            ClearItems();
            missingVisualLogged = false;
            RefreshDropPlacementSupport();
        }

        private void HandleRaidEnded(RaidBattleResult result)
        {
            ClearItems();
        }

        private void HandlePhaseTransitionStarted(RaidPhaseTransitionInfo info)
        {
            ClearItems();
        }

        private void HandleEnemyDied(EnemyDiedInfo info)
        {
            RaidItemConfigSO config = Config;
            if (config == null || battle == null || battle.State != RaidBattleState.Running || battle.IsTransitioning || battle.CurrentPhase == RaidPhase.Phase3 || activeItems.Count >= config.MaxActiveItems)
            {
                return;
            }

            if (UnityEngine.Random.value > config.GetDropChance(battle.CurrentPhase))
            {
                return;
            }

            if (!TryFindRandomDropTile(out Vector2Int tile))
            {
                return;
            }

            RaidItemType type = (RaidItemType)UnityEngine.Random.Range(0, 3);
            SpawnItem(type, tile, config);
        }

        private void HandleUnitDeployed(RaidUnitDeployedInfo info)
        {
            if (info.Unit == null)
            {
                return;
            }

            for (int i = activeItems.Count - 1; i >= 0; i--)
            {
                ActiveItem item = activeItems[i];
                if (item.Tile != info.Tile)
                {
                    continue;
                }

                RaidItemConfigSO config = Config;
                RaidItemDefinition definition = config != null ? config.GetDefinition(item.Type) : null;
                if (definition == null || !ApplyItem(item.Type, info.Unit, item.Tile, definition))
                {
                    return;
                }

                PlayConsumeVisual(item.Type, info.Unit, definition);
                OnItemConsumed?.Invoke(item.Type, info.Unit, item.Tile);
                RemoveItemAt(i);
                return;
            }
        }

        private bool SpawnItem(RaidItemType type, Vector2Int tile, RaidItemConfigSO config)
        {
            if (boardRuntime == null || boardRuntime.Board == null || HasItemAt(tile))
            {
                return false;
            }

            RaidItemDefinition definition = config.GetDefinition(type);
            if (definition == null || definition.VisualPrefab == null)
            {
                if (!missingVisualLogged)
                {
                    missingVisualLogged = true;
                    Debug.LogError($"Raid 아이템 {type}의 Visual Prefab이 연결되지 않았습니다.", this);
                }

                return false;
            }

            RaidBoard board = boardRuntime.Board;
            if (!board.TryGetTile(tile, out RaidTile raidTile))
            {
                return false;
            }

            float surfaceHeight = raidTile.IsHighGroundDeployable ? battle.Config.HighGroundDeployHeight : battle.Config.GroundDeployHeight;
            Vector3 position = board.TileToWorld(tile, surfaceHeight + config.VisualHeightOffset);
            GameObject visual = Instantiate(definition.VisualPrefab, position, Quaternion.identity, GetItemRoot());
            visual.name = $"RaidItem_{type}_{tile.x}_{tile.y}";
            activeItems.Add(new ActiveItem(type, tile, visual, config.GetActiveLifetimeSeconds(battle.CurrentPhase)));
            OnItemDropped?.Invoke(type, tile);
            return true;
        }

        private bool ApplyItem(RaidItemType type, UnitRuntimeState unit, Vector2Int tile, RaidItemDefinition definition)
        {
            RaidItemConfigSO config = Config;
            if (config == null || definition == null || !IsLiveDeployedUnit(unit))
            {
                return false;
            }

            switch (type)
            {
                case RaidItemType.Attack:
                case RaidItemType.AttackSpeed:
                case RaidItemType.Heal:
                    return fieldBuffs != null && fieldBuffs.Apply(type);
                default:
                    return false;
            }
        }

        private void PlayConsumeVisual(RaidItemType type, UnitRuntimeState unit, RaidItemDefinition definition)
        {
            if (unit == null || definition == null || definition.ConsumeVisualPrefab == null)
            {
                return;
            }

            GameObject visual = Instantiate(definition.ConsumeVisualPrefab, unit.transform.position, Quaternion.identity, unit.transform);
            visual.name = $"ItemConsume_{type}";
            RaidTimedVFX timedVfx = visual.GetComponent<RaidTimedVFX>();
            if (timedVfx != null)
            {
                timedVfx.Play(definition.ConsumeVisualLifetime);
            }
            else
            {
                Destroy(visual, definition.ConsumeVisualLifetime);
            }
        }

        public bool TryGetActiveItem(int index, out RaidItemType type, out Vector2Int tile, out float remainingSeconds)
        {
            if (index < 0 || index >= activeItems.Count)
            {
                type = default;
                tile = default;
                remainingSeconds = 0f;
                return false;
            }

            ActiveItem item = activeItems[index];
            type = item.Type;
            tile = item.Tile;
            remainingSeconds = Mathf.Max(0f, item.RemainingSeconds);
            return item.Visual != null;
        }

        public bool EnsureReservationWindow(Vector2Int tile, float secondsToAfford)
        {
            RaidItemConfigSO config = Config;
            if (config == null || secondsToAfford < 0f)
            {
                return false;
            }

            float requiredRemaining = Mathf.Max(0f, secondsToAfford) + config.ReservationGraceSeconds;
            for (int i = 0; i < activeItems.Count; i++)
            {
                ActiveItem item = activeItems[i];
                if (item.Tile != tile || item.Visual == null)
                {
                    continue;
                }

                if (item.RemainingSeconds < requiredRemaining)
                {
                    item.RemainingSeconds = requiredRemaining;
                }

                return true;
            }

            return false;
        }

        private bool TryFindRandomDropTile(out Vector2Int tile)
        {
            tile = default;
            RaidBoard board = boardRuntime != null ? boardRuntime.Board : null;
            if (board == null)
            {
                return false;
            }

            int validCount = 0;
            Vector2Int selected = default;

            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    Vector2Int candidate = new Vector2Int(x, y);
                    if (!IsValidDropTile(candidate))
                    {
                        continue;
                    }

                    validCount++;
                    if (UnityEngine.Random.Range(0, validCount) == 0)
                    {
                        selected = candidate;
                    }
                }
            }

            if (validCount <= 0)
            {
                return false;
            }

            tile = selected;
            return true;
        }

        private bool IsValidDropTile(Vector2Int tile)
        {
            RaidBoard board = boardRuntime != null ? boardRuntime.Board : null;
            if (board == null || !board.TryGetTile(tile, out RaidTile raidTile) || deployment == null || roster == null || deployment.IsTileOccupied(tile) || HasItemAt(tile))
            {
                return false;
            }

            bool groundValid = raidTile.IsGroundCombatDeployable && canUseGroundItemTiles;
            bool highGroundValid = raidTile.IsHighGroundDeployable && canUseHighGroundItemTiles;
            return groundValid || highGroundValid;
        }

        private void RefreshDropPlacementSupport()
        {
            canUseGroundItemTiles = false;
            canUseHighGroundItemTiles = false;

            if (roster == null)
            {
                return;
            }

            for (int team = 0; team < RaidRosterRuntime.TeamCount; team++)
            {
                for (int slotIndex = 0; slotIndex < RaidRosterRuntime.SlotsPerTeam; slotIndex++)
                {
                    RaidRosterSlotState slot = roster.GetSlot(team, slotIndex);
                    UnitDataSO data = slot != null ? slot.UnitData : null;
                    if (data == null || data.UnitPrefab == null)
                    {
                        continue;
                    }

                    switch (data.Placement)
                    {
                        case UnitPlacement.Ground:
                            canUseGroundItemTiles = true;
                            break;
                        case UnitPlacement.HighGround:
                            canUseHighGroundItemTiles = true;
                            break;
                        case UnitPlacement.GroundAndHighGround:
                            canUseGroundItemTiles = true;
                            canUseHighGroundItemTiles = true;
                            break;
                    }

                    if (canUseGroundItemTiles && canUseHighGroundItemTiles)
                    {
                        return;
                    }
                }
            }
        }

        private void TickItems(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            for (int i = activeItems.Count - 1; i >= 0; i--)
            {
                ActiveItem item = activeItems[i];
                if (item.Visual == null)
                {
                    RemoveItemAt(i);
                    continue;
                }

                item.RemainingSeconds -= deltaTime;
                if (item.RemainingSeconds <= 0f)
                {
                    RemoveItemAt(i);
                }
            }
        }

        private void ClearItems()
        {
            for (int i = activeItems.Count - 1; i >= 0; i--)
            {
                RemoveItemAt(i);
            }
        }

        private void RemoveItemAt(int index)
        {
            ActiveItem item = activeItems[index];
            if (item.Visual != null)
            {
                Destroy(item.Visual);
            }

            int last = activeItems.Count - 1;
            if (index != last)
            {
                activeItems[index] = activeItems[last];
            }

            activeItems.RemoveAt(last);
        }

        private float ScoreHealItem()
        {
            int fieldCount = 0;
            float missingHp = 0f;
            float maxHp = 0f;

            foreach (UnitRuntimeState unit in CombatRegistry.Units)
            {
                if (!IsLiveDeployedUnit(unit))
                {
                    continue;
                }

                fieldCount++;
                float unitMaxHp = Mathf.Max(1f, unit.Stats.MaxHp);
                maxHp += unitMaxHp;
                missingHp += Mathf.Max(0f, unitMaxHp - unit.Health.CurrentHp);
            }

            if (fieldCount <= 0)
            {
                return 0.5f;
            }

            float missingRatio = maxHp > 0f ? Mathf.Clamp01(missingHp / maxHp) : 0f;
            float stackValue = 1f;
            if (fieldBuffs != null)
            {
                RaidFieldBuffState state = fieldBuffs.GetState(RaidItemType.Heal);
                if (state.Stack < state.MaxStack)
                {
                    stackValue += (state.MaxStack - state.Stack) * 0.20f;
                }
                else if (state.IsActive)
                {
                    stackValue = Mathf.Lerp(0.65f, 1.10f, 1f - state.NormalizedRemaining);
                }
            }

            float sustainValue = 5.5f + Mathf.Min(7f, fieldCount * 0.45f) + missingRatio * 14f;
            return sustainValue * stackValue;
        }

        private float ScoreAttackItem(UnitDataSO unitData, RaidItemDefinition definition)
        {
            return ScoreGlobalBuffItem(RaidItemType.Attack, unitData, definition);
        }

        private float ScoreAttackSpeedItem(UnitDataSO unitData, RaidItemDefinition definition)
        {
            return ScoreGlobalBuffItem(RaidItemType.AttackSpeed, unitData, definition);
        }

        private float ScoreGlobalBuffItem(RaidItemType type, UnitDataSO unitData, RaidItemDefinition definition)
        {
            int fieldCount = deployment != null ? deployment.DeployedCount : 0;
            float effectScale = Mathf.Clamp(definition.EffectPercent / 30f, 0.5f, 1.75f);
            float globalValue = 7f + Mathf.Min(7f, fieldCount * 0.55f);
            float collectorBonus = HasOffensiveAttack(unitData) ? 1.5f : 0f;
            float stackValue = 1f;

            if (fieldBuffs != null)
            {
                RaidFieldBuffState state = fieldBuffs.GetState(type);
                if (state.Stack < state.MaxStack)
                {
                    stackValue += (state.MaxStack - state.Stack) * 0.22f;
                }
                else if (state.IsActive)
                {
                    stackValue = Mathf.Lerp(0.6f, 1.05f, 1f - state.NormalizedRemaining);
                }
            }

            return (globalValue + collectorBonus) * effectScale * stackValue;
        }

        private static bool HasOffensiveAttack(UnitDataSO unitData)
        {
            return unitData != null && unitData.BaseStats != null && unitData.AttackSettings != null && unitData.AttackSettings.AttackMode != AttackMode.None && unitData.AttackSettings.TargetCount > 0;
        }

        private Transform GetItemRoot()
        {
            if (itemRoot != null)
            {
                return itemRoot;
            }

            Transform raidRoot = battle != null ? battle.transform.parent : null;
            Transform runtimeRoot = raidRoot != null ? raidRoot.Find("Runtime") : null;
            if (runtimeRoot == null && battle != null)
            {
                runtimeRoot = battle.transform;
            }

            Transform existing = runtimeRoot != null ? runtimeRoot.Find("Items") : null;
            if (existing != null)
            {
                itemRoot = existing;
                return itemRoot;
            }

            GameObject root = new GameObject("Items");
            itemRoot = root.transform;
            if (runtimeRoot != null)
            {
                itemRoot.SetParent(runtimeRoot, false);
            }

            return itemRoot;
        }

        private void ResolveDependencies()
        {
            if (battle == null)
            {
                battle = GetComponent<RaidBattleController>();
            }

            if (boardRuntime == null)
            {
                boardRuntime = GetComponent<RaidBoardRuntime>();
            }

            if (deployment == null)
            {
                deployment = GetComponent<RaidDeploymentRuntime>();
            }

            if (roster == null)
            {
                roster = GetComponent<RaidRosterRuntime>();
            }

            if (fieldBuffs == null)
            {
                fieldBuffs = GetComponent<RaidFieldBuffRuntime>();
            }
        }

        private static bool IsLiveDeployedUnit(UnitRuntimeState unit)
        {
            return unit != null && !unit.IsSummon && unit.IsInitialized && unit.Health != null && !unit.Health.IsDead && unit.GridPosition != null && unit.GridPosition.IsInitialized && unit.Stats != null && unit.Stats.IsInitialized;
        }

        private sealed class ActiveItem
        {
            public RaidItemType Type { get; }
            public Vector2Int Tile { get; }
            public GameObject Visual { get; }
            public float RemainingSeconds { get; set; }

            public ActiveItem(RaidItemType type, Vector2Int tile, GameObject visual, float remainingSeconds)
            {
                Type = type;
                Tile = tile;
                Visual = visual;
                RemainingSeconds = remainingSeconds;
            }
        }


    }
}
