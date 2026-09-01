using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Raid.Data;
using EndlessGuard.Unit.Runtime;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    public readonly struct RaidUnitDeployedInfo
    {
        public RaidRosterSlotState Slot { get; }
        public UnitRuntimeState Unit { get; }
        public Vector2Int Tile { get; }
        public GridFacingDirection Facing { get; }
        public bool Automatic { get; }

        public RaidUnitDeployedInfo(RaidRosterSlotState slot, UnitRuntimeState unit, Vector2Int tile, GridFacingDirection facing, bool automatic)
        {
            Slot = slot;
            Unit = unit;
            Tile = tile;
            Facing = facing;
            Automatic = automatic;
        }
    }

    [DisallowMultipleComponent]
    public sealed class RaidDeploymentRuntime : MonoBehaviour
    {
        private readonly Dictionary<Vector2Int, UnitRuntimeState> deployedByTile = new Dictionary<Vector2Int, UnitRuntimeState>();
        private readonly List<Vector2Int> staleTiles = new List<Vector2Int>(8);
        private RaidBattleController battle;
        private RaidBoardRuntime boardRuntime;
        private RaidRosterRuntime roster;
        private Transform unitRoot;
        private float reconcileElapsed;
        private bool preparedForRaidStart;

        public event Action<RaidUnitDeployedInfo> OnUnitDeployed;
        public event Action<UnitRuntimeState> OnUnitRemoved;

        public int DeployedCount => deployedByTile.Count;

        public int MaxDeployedUnits => battle != null && battle.Config != null ? battle.Config.MaxDeployedUnits : 16;
        public bool HasCapacity => DeployedCount < MaxDeployedUnits;
        public RaidBoard Board => boardRuntime != null ? boardRuntime.Board : null;

        private void Awake()
        {
            battle = GetComponent<RaidBattleController>();
            boardRuntime = GetComponent<RaidBoardRuntime>();
            roster = GetComponent<RaidRosterRuntime>();
        }

        private void OnEnable()
        {
            if (battle == null)
            {
                battle = GetComponent<RaidBattleController>();
            }

            if (boardRuntime == null)
            {
                boardRuntime = GetComponent<RaidBoardRuntime>();
            }

            if (roster == null)
            {
                roster = GetComponent<RaidRosterRuntime>();
            }

            if (battle == null || boardRuntime == null || roster == null)
            {
                Debug.LogError("RaidDeploymentRuntime은 RaidBattleController, RaidBoardRuntime, RaidRosterRuntime이 필요합니다.", this);
                enabled = false;
                return;
            }

            battle.OnRaidPreparing += HandleRaidPreparing;
            battle.OnRaidStarted += HandleRaidStarted;
            battle.OnRaidEnded += HandleRaidEnded;
            battle.OnPhaseTransitionCompleted += HandlePhaseTransitionCompleted;
            battle.OnUnitForcedRetreat += HandleForcedRetreat;
            CombatEvents.OnUnitDied += HandleUnitDied;
        }

        private void OnDisable()
        {
            if (battle != null)
            {
                battle.OnRaidPreparing -= HandleRaidPreparing;
                battle.OnRaidStarted -= HandleRaidStarted;
                battle.OnRaidEnded -= HandleRaidEnded;
                battle.OnPhaseTransitionCompleted -= HandlePhaseTransitionCompleted;
                battle.OnUnitForcedRetreat -= HandleForcedRetreat;
            }

            CombatEvents.OnUnitDied -= HandleUnitDied;
        }

        private void Update()
        {
            reconcileElapsed += Time.deltaTime;
            if (reconcileElapsed < 0.5f)
            {
                return;
            }

            reconcileElapsed = 0f;
            Reconcile();
        }

        public bool IsTileOccupied(Vector2Int tile)
        {
            if (deployedByTile.TryGetValue(tile, out UnitRuntimeState deployedUnit))
            {
                if (IsLiveUnit(deployedUnit) && deployedUnit.GridPosition != null && deployedUnit.GridPosition.IsInitialized && deployedUnit.GridPosition.TileCoordinate == tile)
                {
                    return true;
                }

                deployedByTile.Remove(tile);

                if (deployedUnit != null)
                {
                    OnUnitRemoved?.Invoke(deployedUnit);
                }
            }

            if (CombatRegistry.TryGetUnitsAt(tile, out HashSet<UnitRuntimeState> units))
            {
                foreach (UnitRuntimeState unit in units)
                {
                    if (IsLiveUnit(unit))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool TryGetDeployedUnitAt(Vector2Int tile, out UnitRuntimeState unit)
        {
            if (deployedByTile.TryGetValue(tile, out unit) && IsLiveUnit(unit) && unit.GridPosition != null && unit.GridPosition.IsInitialized && unit.GridPosition.TileCoordinate == tile)
            {
                return true;
            }

            if (unit != null)
            {
                deployedByTile.Remove(tile);
                OnUnitRemoved?.Invoke(unit);
            }

            unit = null;
            return false;
        }

        public bool IsTileDeployable(UnitDataSO data, Vector2Int tile)
        {
            RaidBoard board = Board;
            if (data == null || board == null || !board.TryGetTile(tile, out RaidTile raidTile) || IsTileOccupied(tile))
            {
                return false;
            }

            switch (data.Placement)
            {
                case UnitPlacement.Ground:
                    return IsGroundCombatDeployable(raidTile);
                case UnitPlacement.HighGround:
                    return raidTile.IsHighGroundDeployable;
                case UnitPlacement.GroundAndHighGround:
                    return IsGroundCombatDeployable(raidTile) || raidTile.IsHighGroundDeployable;
                default:
                    return false;
            }
        }

        public static bool IsGroundCombatDeployable(RaidTile raidTile)
        {
            return raidTile.IsGroundCombatDeployable;
        }

        public bool TryDeploy(RaidRosterSlotState slot, Vector2Int tile, GridFacingDirection facing, bool automatic, out UnitRuntimeState deployedUnit)
        {
            deployedUnit = null;

            if (battle == null || battle.State != RaidBattleState.Running || battle.IsTransitioning || slot == null || !slot.CanDeploy || slot.UnitData == null || !HasCapacity)
            {
                return false;
            }

            UnitDataSO data = slot.UnitData;
            if (data.UnitPrefab == null || !IsTileDeployable(data, tile))
            {
                return false;
            }

            int cost = Mathf.Max(0, data.SummonCost);
            if (!battle.TrySpendCost(cost))
            {
                return false;
            }

            RaidBoard board = Board;
            RaidTile raidTile = board.GetTile(tile);
            float height = raidTile.IsHighGroundDeployable ? battle.Config.HighGroundDeployHeight : battle.Config.GroundDeployHeight;
            Vector3 worldPosition = board.TileToWorld(tile, height);
            GameObject instance = null;

            try
            {
                instance = Instantiate(data.UnitPrefab, worldPosition, Quaternion.identity, GetUnitRoot());
                instance.name = $"Raid_{data.UnitId}";
                deployedUnit = instance.GetComponent<UnitRuntimeState>();

                if (deployedUnit == null || !deployedUnit.IsInitialized || deployedUnit.GridPosition == null)
                {
                    throw new InvalidOperationException($"{data.UnitId} Prefab에 정상 초기화된 UnitRuntimeState/CombatGridPosition이 없습니다.");
                }

                deployedUnit.GridPosition.Initialize(tile, facing, CombatTargetLayer.Ground);

                if (slot != null && !slot.ApplyBuild(deployedUnit))
                {
                    throw new InvalidOperationException($"{data.UnitId}의 Raid 성장/장비 Build Snapshot 적용에 실패했습니다.");
                }

                if (deployedUnit.Passives != null)
                {
                    deployedUnit.Passives.Initialize(deployedUnit, data.Passives);
                    ValidatePassiveRuntime(deployedUnit, data);
                }

                deployedByTile[tile] = deployedUnit;

                if (roster != null)
                {
                    roster.MarkDeployed(deployedUnit);
                }

                OnUnitDeployed?.Invoke(new RaidUnitDeployedInfo(slot, deployedUnit, tile, facing, automatic));
                return true;
            }
            catch (Exception exception)
            {
                if (instance != null)
                {
                    Destroy(instance);
                }

                battle.AddCost(cost);
                deployedUnit = null;
                Debug.LogError($"Raid 캐릭터 배치 실패: {data.UnitId} / Tile {tile}\n{exception.Message}", this);
                return false;
            }
        }

        private static void ValidatePassiveRuntime(UnitRuntimeState unit, UnitDataSO data)
        {
            if (unit == null || data == null || unit.Passives == null)
            {
                return;
            }

            int expected = data.Passives != null ? data.Passives.Count : 0;
            UnitPassiveRuntime runtime = unit.Passives;

            if (runtime.AssignedPassiveCount != expected || runtime.AppliedPassiveCount != expected || runtime.RejectedPassiveCount > 0 || runtime.UnsupportedPassiveCount > 0)
            {
                Debug.LogError(
                    $"Raid Passive 연결 실패: {data.UnitId} / Expected {expected} / Assigned {runtime.AssignedPassiveCount} / Applied {runtime.AppliedPassiveCount} / Rejected {runtime.RejectedPassiveCount} / Unsupported {runtime.UnsupportedPassiveCount}",
                    unit);
            }
        }

        public void Reconcile()
        {
            staleTiles.Clear();

            foreach (KeyValuePair<Vector2Int, UnitRuntimeState> pair in deployedByTile)
            {
                UnitRuntimeState unit = pair.Value;
                if (!IsLiveUnit(unit) || unit.GridPosition == null || !unit.GridPosition.IsInitialized || unit.GridPosition.TileCoordinate != pair.Key)
                {
                    staleTiles.Add(pair.Key);
                }
            }

            for (int i = 0; i < staleTiles.Count; i++)
            {
                Vector2Int tile = staleTiles[i];
                if (deployedByTile.TryGetValue(tile, out UnitRuntimeState removed))
                {
                    deployedByTile.Remove(tile);
                    OnUnitRemoved?.Invoke(removed);
                }
            }

            foreach (UnitRuntimeState unit in CombatRegistry.Units)
            {
                if (!IsLiveUnit(unit) || unit.IsSummon || unit.GridPosition == null || !unit.GridPosition.IsInitialized)
                {
                    continue;
                }

                RaidRosterSlotState slot = FindRosterSlot(unit.UnitId);
                if (slot == null)
                {
                    continue;
                }

                deployedByTile[unit.GridPosition.TileCoordinate] = unit;
            }
        }

        private RaidRosterSlotState FindRosterSlot(string unitId)
        {
            if (roster == null || string.IsNullOrWhiteSpace(unitId))
            {
                return null;
            }

            for (int team = 0; team < RaidRosterRuntime.TeamCount; team++)
            {
                for (int slot = 0; slot < RaidRosterRuntime.SlotsPerTeam; slot++)
                {
                    RaidRosterSlotState state = roster.GetSlot(team, slot);
                    if (state != null && state.UnitData != null && string.Equals(state.UnitData.UnitId, unitId, StringComparison.Ordinal))
                    {
                        return state;
                    }
                }
            }

            return null;
        }

        private Transform GetUnitRoot()
        {
            if (unitRoot != null)
            {
                return unitRoot;
            }

            Transform raidRoot = battle.transform.parent;
            Transform runtimeRoot = raidRoot != null ? raidRoot.Find("Runtime") : null;
            if (runtimeRoot == null)
            {
                runtimeRoot = battle.transform;
            }

            Transform existing = runtimeRoot.Find("Units");
            if (existing != null)
            {
                unitRoot = existing;
                return unitRoot;
            }

            GameObject root = new GameObject("Units");
            unitRoot = root.transform;
            unitRoot.SetParent(runtimeRoot, false);
            return unitRoot;
        }

        private void HandleRaidPreparing()
        {
            deployedByTile.Clear();
            reconcileElapsed = 0f;
            AttackRangeDisplay.Hide();
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
            AttackRangeDisplay.Hide();
        }

        private void HandlePhaseTransitionCompleted(RaidPhaseTransitionInfo info)
        {
            Reconcile();
            AttackRangeDisplay.Hide();
        }

        private void HandleForcedRetreat(RaidForcedRetreatInfo info)
        {
            if (info.Unit == null)
            {
                return;
            }

            RemoveUnit(info.Unit);
            Destroy(info.Unit.gameObject);
        }

        private void HandleUnitDied(UnitDiedInfo info)
        {
            UnitRuntimeState deadUnit = null;
            Vector2Int deadTile = default;

            foreach (KeyValuePair<Vector2Int, UnitRuntimeState> pair in deployedByTile)
            {
                UnitRuntimeState unit = pair.Value;
                if (unit != null && unit.RuntimeId == info.RuntimeId)
                {
                    deadUnit = unit;
                    deadTile = pair.Key;
                    break;
                }
            }

            if (deadUnit == null)
            {
                return;
            }

            deployedByTile.Remove(deadTile);
            OnUnitRemoved?.Invoke(deadUnit);

            float despawnDelay = 0.05f;
            UnitAnimationBridge animationBridge = deadUnit.GetComponent<UnitAnimationBridge>();
            if (animationBridge != null)
            {
                despawnDelay = Mathf.Max(despawnDelay, animationBridge.DeathPresentationDuration);
            }

            Destroy(deadUnit.gameObject, despawnDelay);
        }

        private void RemoveUnit(UnitRuntimeState unit)
        {
            if (unit == null)
            {
                return;
            }

            staleTiles.Clear();
            foreach (KeyValuePair<Vector2Int, UnitRuntimeState> pair in deployedByTile)
            {
                if (pair.Value == unit)
                {
                    staleTiles.Add(pair.Key);
                }
            }

            for (int i = 0; i < staleTiles.Count; i++)
            {
                deployedByTile.Remove(staleTiles[i]);
            }

            OnUnitRemoved?.Invoke(unit);
        }

        private static bool IsLiveUnit(UnitRuntimeState unit)
        {
            return unit != null && unit.gameObject.activeInHierarchy && unit.IsInitialized && !unit.IsSummon && unit.Health != null && !unit.Health.IsDead;
        }
    }
}
