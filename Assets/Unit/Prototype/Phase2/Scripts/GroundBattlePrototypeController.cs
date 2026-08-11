using System.Collections;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EndlessGuard.Unit.Prototype.Phase2
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CombatLoop))]
    public sealed class GroundBattlePrototypeController : MonoBehaviour, ISummonTileProvider, IAttackRangeTileProvider
    {
        private const int GrowthStatCount = 11;
        private const float PlacementDragDeadZone = 0.35f;
        private const int CritSummonNeighborRadius = 1;

        [Header("정식 데이터")]
        [SerializeField] private UnitCatalog unitCatalog;
        [SerializeField] private EnemyCatalog enemyCatalog;

        [Header("캐릭터 배치")]
        [Range(1, 8)]
        [SerializeField] private int initialUnitCount = 8;
        [Range(0, 3)]
        [SerializeField] private int initialHighGroundUnitCount = 3;

        [Tooltip("다른 담당의 자동배치를 흉내 내는 옵션입니다. 기본은 수동 배치입니다.")]
        [SerializeField] private bool autoDeployEnabled;

        [Header("Prototype 배치 코스트")]
        [Tooltip("다른 담당의 코스트 시스템을 흉내 내는 검증용 시작 코스트입니다.")]
        [Min(0)]
        [SerializeField] private int startingCost = 25;
        [Tooltip("다른 담당의 코스트 시스템을 흉내 내는 검증용 최대 코스트입니다.")]
        [Min(1)]
        [SerializeField] private int maxCost = 99;
        [Tooltip("시간 경과로 자동 획득하는 초당 코스트입니다.")]
        [Min(0f)]
        [SerializeField] private float costRegenPerSecond = 1f;

        [Header("Prototype 골드 강화")]
        [Tooltip("다른 담당의 골드 시스템을 흉내 내는 검증용 시작 골드입니다.")]
        [Min(0)]
        [SerializeField] private int startingGold;
        [Min(1)]
        [SerializeField] private int upgradeBaseCost = 50;
        [Min(0)]
        [SerializeField] private int upgradeCostIncrease = 25;

        [Header("공통 강화 1회 증가량")]
        [Min(0f)][SerializeField] private float maxHpUpgradeAmount = 1000f;
        [Min(0f)][SerializeField] private float hpRegenUpgradeAmount = 25f;
        [Min(0f)][SerializeField] private float physicalAttackUpgradeAmount = 100f;
        [Min(0f)][SerializeField] private float magicalAttackUpgradeAmount = 100f;
        [Min(0f)][SerializeField] private float physicalDefenseUpgradeAmount = 100f;
        [Min(0f)][SerializeField] private float magicalDefenseUpgradeAmount = 100f;
        [Min(0f)][SerializeField] private float attackSpeedUpgradeAmount = 0.5f;
        [Min(0f)][SerializeField] private float accuracyUpgradeAmount = 10f;
        [Min(0f)][SerializeField] private float evasionUpgradeAmount = 10f;
        [Min(0f)][SerializeField] private float criticalChanceUpgradeAmount = 10f;
        [Min(0f)][SerializeField] private float criticalDamageUpgradeAmount = 25f;

        [Header("몬스터 생성")]
        [Tooltip("다른 담당의 웨이브를 흉내 내는 옵션입니다. 기본은 원하는 몬스터를 직접 소환합니다.")]
        [SerializeField] private bool autoEnemySpawnEnabled;
        [Min(0.1f)]
        [SerializeField] private float enemySpawnInterval = 2f;

        [Header("캐릭터 교체")]
        [Min(0f)]
        [SerializeField] private float replacementDelay = 0.5f;

        [Header("출구")]
        [Min(1)]
        [SerializeField] private int maxExitHp = 10;

        [Header("공중 몬스터")]
        [Min(0.1f)]
        [SerializeField] private float airHeight = 2f;

        [HideInInspector][SerializeField] private int currentCost;
        [HideInInspector][SerializeField] private int totalCostSpent;
        [HideInInspector][SerializeField] private int totalCostRegenerated;
        [HideInInspector][SerializeField] private int totalPassiveCostGained;
        [HideInInspector][SerializeField] private int passiveCostRequestCount;
        [HideInInspector][SerializeField] private int currentGold;
        [HideInInspector][SerializeField] private int totalGoldEarned;
        [HideInInspector][SerializeField] private int totalGoldSpent;
        [HideInInspector][SerializeField] private int currentExitHp;
        [HideInInspector][SerializeField] private int enemySpawnCount;
        [HideInInspector][SerializeField] private int enemyDeathCount;
        [HideInInspector][SerializeField] private int enemyReachedGoalCount;
        [HideInInspector][SerializeField] private int replacementCount;
        [HideInInspector][SerializeField] private int progressEventCount;
        [HideInInspector][SerializeField] private bool battleRunning;
        [HideInInspector][TextArea(2, 5)][SerializeField] private string lastCostMessage;
        [HideInInspector][TextArea(2, 5)][SerializeField] private string lastProgressMessage;
        [HideInInspector][TextArea(3, 8)][SerializeField] private string lastMessage;

        private readonly List<Phase2GroundTile> groundTiles = new List<Phase2GroundTile>();
        private readonly List<Phase2GroundTile> highGroundTiles = new List<Phase2GroundTile>();
        private readonly List<UnitRuntimeState> activeUnits = new List<UnitRuntimeState>();
        private readonly List<UnitDataSO> reserveUnits = new List<UnitDataSO>();
        private readonly List<GameObject> spawnedObjects = new List<GameObject>();
        private readonly HashSet<Vector2Int> occupiedTiles = new HashSet<Vector2Int>();
        private readonly Dictionary<UnitRuntimeState, Vector2Int> activeUnitTiles = new Dictionary<UnitRuntimeState, Vector2Int>();
        private readonly Dictionary<Vector2Int, Phase2GroundTile> tilesByCoordinate = new Dictionary<Vector2Int, Phase2GroundTile>();
        private readonly List<Phase2GroundTile> summonTileCandidates = new List<Phase2GroundTile>();
        private readonly int[] goldUpgradeLevels = new int[GrowthStatCount];

        private CombatLoop combatLoop;
        private Phase2EnemyRoute enemyRoute;
        private Coroutine waveRoutine;
        private UnitRuntimeState selectedUnit;
        private UnitDataSO selectedReserveUnit;
        private GridFacingDirection selectedPlacementFacing = GridFacingDirection.West;
        private Phase2GroundTile pendingPlacementTile;
        private bool placementDragging;
        private float costRegenAccumulator;
        private bool autoDeployRequested;
        private Vector2 unitScroll;
        private Vector2 growthScroll;
        private Vector2 reserveScroll;
        private Vector2 enemyScroll;

        public int CurrentExitHp => currentExitHp;
        public int ActiveUnitCount => activeUnits.Count;
        public int ReserveUnitCount => reserveUnits.Count;
        public int EnemySpawnCount => enemySpawnCount;
        public int EnemyDeathCount => enemyDeathCount;
        public int EnemyReachedGoalCount => enemyReachedGoalCount;
        public int ReplacementCount => replacementCount;
        public int CurrentCost => currentCost;
        public int MaxCost => Mathf.Max(1, maxCost);
        public int TotalCostSpent => totalCostSpent;
        public int TotalCostRegenerated => totalCostRegenerated;
        public int TotalPassiveCostGained => totalPassiveCostGained;
        public int PassiveCostRequestCount => passiveCostRequestCount;
        public int CurrentGold => currentGold;
        public int TotalGoldEarned => totalGoldEarned;
        public int TotalGoldSpent => totalGoldSpent;
        public int ProgressEventCount => progressEventCount;
        public bool AutoDeployEnabled => autoDeployEnabled;
        public bool AutoEnemySpawnEnabled => autoEnemySpawnEnabled;
        public bool BattleRunning => battleRunning;
        public string LastMessage => lastMessage;
        public string LastCostMessage => lastCostMessage;
        public string LastProgressMessage => lastProgressMessage;

        private void Awake()
        {
            combatLoop = GetComponent<CombatLoop>();
        }

        private void OnEnable()
        {
            CombatEvents.OnUnitDied += HandleUnitDied;
            CombatEvents.OnEnemyDied += HandleEnemyDied;
            CombatEvents.OnEnemyReachedGoal += HandleEnemyReachedGoal;
            PassiveRuntimeEvents.OnSummonCostGainRequested += HandleSummonCostGainRequested;
            UnitProgressEvents.OnUnitProgressChanged += HandleProgressChanged;
            SummonTileService.Register(this);
            AttackRangeTileService.Register(this);
        }

        private void OnDisable()
        {
            CombatEvents.OnUnitDied -= HandleUnitDied;
            CombatEvents.OnEnemyDied -= HandleEnemyDied;
            CombatEvents.OnEnemyReachedGoal -= HandleEnemyReachedGoal;
            PassiveRuntimeEvents.OnSummonCostGainRequested -= HandleSummonCostGainRequested;
            UnitProgressEvents.OnUnitProgressChanged -= HandleProgressChanged;
            SummonTileService.Unregister(this);
            AttackRangeTileService.Unregister(this);
            ResetBattle();
        }

        private void Update()
        {
            if (!battleRunning)
            {
                return;
            }

            TickCostRegeneration(Time.deltaTime);

            if (autoDeployEnabled && autoDeployRequested)
            {
                autoDeployRequested = false;
                TryAutoDeployAvailableUnits();
            }

            if (!autoDeployEnabled)
            {
                HandleManualPlacement();
                HandleManualUnitSelection();
            }

            EnsureSelectedUnit();
        }

        public void StartBattle()
        {
            ResetBattle();

            if (!PrepareBattle())
            {
                return;
            }

            ResetBattleEconomy();

            if (!PrepareRoster())
            {
                Fail("배치 가능한 정식 캐릭터가 없습니다.");
                return;
            }

            battleRunning = true;
            combatLoop.StartLoop();

            if (autoDeployEnabled)
            {
                TryAutoDeployAvailableUnits();
            }

            if (autoEnemySpawnEnabled)
            {
                StartAutoEnemySpawn();
            }

            lastMessage = $"Ground 통합 전투 시작: 배치 {activeUnits.Count}명 / 대기 {reserveUnits.Count}명 / Cost {currentCost}/{MaxCost} / Gold {currentGold} / 자동배치 {(autoDeployEnabled ? "ON" : "OFF")} / 자동몬스터 {(autoEnemySpawnEnabled ? "ON" : "OFF")}";
            Debug.Log(lastMessage, this);
        }

        public void ReplaceOneUnit()
        {
            if (!battleRunning)
            {
                Fail("먼저 Ground 전투를 시작하세요.");
                return;
            }

            if (reserveUnits.Count <= 0)
            {
                Fail("교체할 대기 캐릭터가 없습니다.");
                return;
            }

            for (int i = 0; i < activeUnits.Count; i++)
            {
                UnitRuntimeState current = activeUnits[i];

                if (!CanUseUnit(current))
                {
                    continue;
                }

                string previousName = GetUnitDisplayName(current);
                RemoveUnit(current);
                replacementCount++;
                autoDeployRequested = autoDeployEnabled;
                lastMessage = autoDeployEnabled ? $"{previousName} 전투 이탈. 자동배치가 현재 Cost로 대기 캐릭터를 확인합니다." : $"{previousName} 전투 이탈. 원하는 대기 캐릭터를 직접 선택해 다시 배치하세요.";
                Debug.Log(lastMessage, this);
                return;
            }

            Fail("교체 가능한 캐릭터가 없습니다.");
        }

        public void ResetBattle()
        {
            battleRunning = false;
            autoDeployRequested = false;
            CancelPlacementDrag();
            AttackRangeDisplay.Hide();
            costRegenAccumulator = 0f;

            if (waveRoutine != null)
            {
                StopCoroutine(waveRoutine);
                waveRoutine = null;
            }

            if (combatLoop != null)
            {
                combatLoop.StopLoop();
            }

            for (int i = spawnedObjects.Count - 1; i >= 0; i--)
            {
                GameObject target = spawnedObjects[i];

                if (target != null)
                {
                    target.SetActive(false);
                    Destroy(target);
                }
            }

            spawnedObjects.Clear();
            activeUnits.Clear();
            reserveUnits.Clear();
            occupiedTiles.Clear();
            activeUnitTiles.Clear();

            for (int i = 0; i < goldUpgradeLevels.Length; i++)
            {
                goldUpgradeLevels[i] = 0;
            }

            CommonGrowthService.Clear();
            selectedUnit = null;
            selectedReserveUnit = null;
            tilesByCoordinate.Clear();
            currentExitHp = maxExitHp;
            enemySpawnCount = 0;
            enemyDeathCount = 0;
            enemyReachedGoalCount = 0;
            replacementCount = 0;
            progressEventCount = 0;
            currentCost = Mathf.Clamp(startingCost, 0, MaxCost);
            totalCostSpent = 0;
            totalCostRegenerated = 0;
            totalPassiveCostGained = 0;
            passiveCostRequestCount = 0;
            currentGold = Mathf.Max(0, startingGold);
            totalGoldEarned = 0;
            totalGoldSpent = 0;
            lastCostMessage = string.Empty;
            lastProgressMessage = string.Empty;
            lastMessage = "Ground Prototype 대기.";
        }

        private void ResetBattleEconomy()
        {
            currentExitHp = maxExitHp;
            currentCost = Mathf.Clamp(startingCost, 0, MaxCost);
            currentGold = Mathf.Max(0, startingGold);
            totalCostSpent = 0;
            totalCostRegenerated = 0;
            totalPassiveCostGained = 0;
            passiveCostRequestCount = 0;
            totalGoldEarned = 0;
            totalGoldSpent = 0;
            costRegenAccumulator = 0f;
            lastCostMessage = $"시작 Cost {currentCost}/{MaxCost} / 자동 회복 {costRegenPerSecond:0.##}/초";
        }

        private bool PrepareBattle()
        {
            if (unitCatalog == null)
            {
                Fail("UnitCatalog이 연결되지 않았습니다.");
                return false;
            }

            if (enemyCatalog == null)
            {
                Fail("EnemyCatalog이 연결되지 않았습니다.");
                return false;
            }

            enemyRoute = GetComponentInChildren<Phase2EnemyRoute>(true);

            if (enemyRoute == null)
            {
                Fail("EnemyRoute를 찾지 못했습니다.");
                return false;
            }

            if (!enemyRoute.ValidateGroundRoute(out string routeMessage))
            {
                Fail($"EnemyRoute가 올바르지 않습니다. {routeMessage}");
                return false;
            }

            Phase2GroundTile[] tiles = GetComponentsInChildren<Phase2GroundTile>(true);
            groundTiles.Clear();
            highGroundTiles.Clear();
            tilesByCoordinate.Clear();

            for (int i = 0; i < tiles.Length; i++)
            {
                Phase2GroundTile tile = tiles[i];

                if (tile == null)
                {
                    continue;
                }

                if (!tilesByCoordinate.ContainsKey(tile.Coordinate))
                {
                    tilesByCoordinate.Add(tile.Coordinate, tile);
                }

                if (tile.Surface == Phase2TileSurface.HighGround)
                {
                    highGroundTiles.Add(tile);
                }
                else
                {
                    groundTiles.Add(tile);
                }
            }

            groundTiles.Sort(ComparePlacementTiles);
            highGroundTiles.Sort(ComparePlacementTiles);

            if (groundTiles.Count == 0 || highGroundTiles.Count == 0)
            {
                Fail("Ground 또는 HighGround 타일이 없습니다.");
                return false;
            }

            return true;
        }

        private bool PrepareRoster()
        {
            List<UnitDataSO> groundPool = new List<UnitDataSO>();
            List<UnitDataSO> highGroundPool = new List<UnitDataSO>();
            List<UnitDataSO> flexiblePool = new List<UnitDataSO>();
            IReadOnlyList<UnitDataSO> catalogUnits = unitCatalog.Units;

            for (int i = 0; i < catalogUnits.Count; i++)
            {
                UnitDataSO data = catalogUnits[i];

                if (data == null || data.UnitPrefab == null)
                {
                    continue;
                }

                switch (data.Placement)
                {
                    case UnitPlacement.Ground:
                        groundPool.Add(data);
                        break;

                    case UnitPlacement.HighGround:
                        highGroundPool.Add(data);
                        break;

                    case UnitPlacement.GroundAndHighGround:
                        flexiblePool.Add(data);
                        break;
                }
            }

            groundPool.Sort(CompareUnitDeploymentPriority);
            highGroundPool.Sort(CompareUnitDeploymentPriority);
            flexiblePool.Sort(CompareUnitDeploymentPriority);

            int requestedCount = Mathf.Min(initialUnitCount, 8);
            int requestedHighGroundCount = Mathf.Min(initialHighGroundUnitCount, requestedCount);
            int requestedGroundCount = requestedCount - requestedHighGroundCount;
            List<UnitDataSO> preferredUnits = new List<UnitDataSO>(requestedCount);
            HashSet<UnitDataSO> selected = new HashSet<UnitDataSO>();

            AddPreferredUnits(groundPool, requestedGroundCount, preferredUnits, selected);
            AddPreferredUnits(highGroundPool, requestedHighGroundCount, preferredUnits, selected);

            for (int i = 0; i < flexiblePool.Count && preferredUnits.Count < requestedCount; i++)
            {
                UnitDataSO data = flexiblePool[i];

                if (selected.Add(data))
                {
                    preferredUnits.Add(data);
                }
            }

            for (int i = 0; i < groundPool.Count && preferredUnits.Count < requestedCount; i++)
            {
                UnitDataSO data = groundPool[i];

                if (selected.Add(data))
                {
                    preferredUnits.Add(data);
                }
            }

            for (int i = 0; i < highGroundPool.Count && preferredUnits.Count < requestedCount; i++)
            {
                UnitDataSO data = highGroundPool[i];

                if (selected.Add(data))
                {
                    preferredUnits.Add(data);
                }
            }

            reserveUnits.Clear();

            for (int i = 0; i < preferredUnits.Count; i++)
            {
                reserveUnits.Add(preferredUnits[i]);
            }

            List<UnitDataSO> benchUnits = new List<UnitDataSO>();

            for (int i = 0; i < catalogUnits.Count; i++)
            {
                UnitDataSO data = catalogUnits[i];

                if (data != null && data.UnitPrefab != null && !selected.Contains(data))
                {
                    benchUnits.Add(data);
                }
            }

            benchUnits.Sort(CompareUnitDeploymentPriority);

            for (int i = 0; i < benchUnits.Count; i++)
            {
                reserveUnits.Add(benchUnits[i]);
            }

            return reserveUnits.Count > 0;
        }

        private static void AddPreferredUnits(List<UnitDataSO> source, int count, List<UnitDataSO> result, HashSet<UnitDataSO> selected)
        {
            int added = 0;

            for (int i = 0; i < source.Count && added < count; i++)
            {
                UnitDataSO data = source[i];

                if (data != null && selected.Add(data))
                {
                    result.Add(data);
                    added++;
                }
            }
        }

        private void TickCostRegeneration(float deltaTime)
        {
            if (deltaTime <= 0f || costRegenPerSecond <= 0f || currentCost >= MaxCost)
            {
                return;
            }

            costRegenAccumulator += deltaTime * costRegenPerSecond;
            int gain = Mathf.FloorToInt(costRegenAccumulator);

            if (gain <= 0)
            {
                return;
            }

            costRegenAccumulator -= gain;
            int applied = AddCost(gain);

            if (applied > 0)
            {
                totalCostRegenerated += applied;
                lastCostMessage = $"자동 Cost +{applied} / 현재 {currentCost}/{MaxCost}";
            }
        }

        private void HandleSummonCostGainRequested(UnitRuntimeState source, int amount, PassiveDataSO passive)
        {
            if (!battleRunning || !CanUseUnit(source) || !activeUnits.Contains(source) || amount <= 0)
            {
                return;
            }

            passiveCostRequestCount++;
            int applied = AddCost(amount);
            totalPassiveCostGained += applied;
            string passiveName = passive != null ? passive.DisplayName : "미지정 패시브";
            lastCostMessage = $"패시브 Cost #{passiveCostRequestCount}: {GetUnitDisplayName(source)} / {passiveName} / 요청 +{amount} / 실제 +{applied} / 현재 {currentCost}/{MaxCost}";
            Debug.Log(lastCostMessage, source);
        }

        private int AddCost(int amount)
        {
            if (amount <= 0)
            {
                return 0;
            }

            int previous = currentCost;
            currentCost = Mathf.Min(MaxCost, currentCost + amount);
            int applied = currentCost - previous;

            if (applied > 0 && autoDeployEnabled)
            {
                autoDeployRequested = autoDeployEnabled;
            }

            return applied;
        }

        private bool TrySpendDeploymentCost(UnitDataSO data, out int spentCost)
        {
            spentCost = data != null ? Mathf.Max(0, data.SummonCost) : 0;

            if (spentCost > currentCost)
            {
                return false;
            }

            currentCost -= spentCost;
            totalCostSpent += spentCost;
            return true;
        }

        private void RefundDeploymentCost(int amount)
        {
            int refund = Mathf.Max(0, amount);

            if (refund <= 0)
            {
                return;
            }

            currentCost = Mathf.Min(MaxCost, currentCost + refund);
            totalCostSpent = Mathf.Max(0, totalCostSpent - refund);
        }

        private int TryAutoDeployAvailableUnits()
        {
            int deployedCount = 0;

            while (battleRunning && activeUnits.Count < initialUnitCount)
            {
                int reserveIndex = FindAffordableDeployableReserveIndex();

                if (reserveIndex < 0)
                {
                    break;
                }

                UnitDataSO data = reserveUnits[reserveIndex];
                reserveUnits.RemoveAt(reserveIndex);

                if (!TryDeployUnit(data))
                {
                    reserveUnits.Insert(reserveIndex, data);
                    break;
                }

                deployedCount++;
            }

            return deployedCount;
        }

        private int FindAffordableDeployableReserveIndex()
        {
            for (int i = 0; i < reserveUnits.Count; i++)
            {
                UnitDataSO data = reserveUnits[i];

                if (data == null || data.SummonCost > currentCost)
                {
                    continue;
                }

                if (TryFindPlacementTile(data.Placement, out _))
                {
                    return i;
                }
            }

            return -1;
        }

        private bool TryDeployUnit(UnitDataSO data)
        {
            if (data == null || data.UnitPrefab == null)
            {
                return false;
            }

            if (!TryFindPlacementTile(data.Placement, out Phase2GroundTile tile))
            {
                return false;
            }

            return TryDeployUnitAtTile(data, tile, GetAutoFacingToRoute(tile.Coordinate));
        }

        private void HandleManualPlacement()
        {
            if (Mouse.current == null)
            {
                return;
            }

            if (Mouse.current.rightButton.wasPressedThisFrame && placementDragging)
            {
                CancelPlacementDrag();
                return;
            }

            if (selectedReserveUnit == null || !reserveUnits.Contains(selectedReserveUnit))
            {
                CancelPlacementDrag();
                selectedReserveUnit = null;
                return;
            }

            if (!placementDragging && Mouse.current.leftButton.wasPressedThisFrame && !IsPointerOverPrototypeUI())
            {
                BeginPlacementDrag();
            }

            if (placementDragging && Mouse.current.leftButton.isPressed)
            {
                UpdatePlacementDrag();
            }

            if (placementDragging && Mouse.current.leftButton.wasReleasedThisFrame)
            {
                ConfirmPlacementDrag();
            }
        }

        private void HandleManualUnitSelection()
        {
            if (Mouse.current == null || selectedReserveUnit != null || placementDragging || !Mouse.current.leftButton.wasPressedThisFrame || IsPointerOverPrototypeUI())
            {
                return;
            }

            if (!TryGetTileUnderPointer(out Phase2GroundTile tile))
            {
                ClearSelectedUnit();
                return;
            }

            UnitRuntimeState unit = FindActiveUnitAtTile(tile.Coordinate);

            if (CanUseUnit(unit))
            {
                SelectUnit(unit);
            }
            else
            {
                ClearSelectedUnit();
            }
        }

        private UnitRuntimeState FindActiveUnitAtTile(Vector2Int tileCoordinate)
        {
            for (int i = 0; i < activeUnits.Count; i++)
            {
                UnitRuntimeState unit = activeUnits[i];

                if (CanUseUnit(unit) && activeUnitTiles.TryGetValue(unit, out Vector2Int unitTile) && unitTile == tileCoordinate)
                {
                    return unit;
                }
            }

            return null;
        }

        private void SelectUnit(UnitRuntimeState unit)
        {
            if (!CanUseUnit(unit) || !activeUnits.Contains(unit))
            {
                ClearSelectedUnit();
                return;
            }

            CancelPlacementDrag();
            selectedReserveUnit = null;
            selectedUnit = unit;
            AttackRangeDisplay.ShowSelected(unit);
        }

        private void ClearSelectedUnit()
        {
            selectedUnit = null;
            AttackRangeDisplay.Hide();
        }

        private void BeginPlacementDrag()
        {
            if (selectedReserveUnit == null)
            {
                return;
            }

            if (activeUnits.Count >= initialUnitCount)
            {
                lastMessage = $"동시 배치 한도 {initialUnitCount}명입니다.";
                return;
            }

            if (currentCost < selectedReserveUnit.SummonCost)
            {
                lastCostMessage = $"{selectedReserveUnit.DisplayName} 배치 Cost 부족: {currentCost}/{selectedReserveUnit.SummonCost}";
                return;
            }

            if (!TryGetTileUnderPointer(out Phase2GroundTile tile) || !CanPlaceSelectedUnit(tile))
            {
                return;
            }

            pendingPlacementTile = tile;
            selectedPlacementFacing = GetAutoFacingToRoute(tile.Coordinate);
            placementDragging = true;
            AttackRangeDisplay.ShowPlacement(selectedReserveUnit, pendingPlacementTile.Coordinate, selectedPlacementFacing);
            lastMessage = $"{selectedReserveUnit.DisplayName} 배치 방향 선택 중: {selectedPlacementFacing}. 드래그하지 않고 놓으면 경로 방향으로 자동 배치합니다.";
        }

        private void UpdatePlacementDrag()
        {
            if (!placementDragging || pendingPlacementTile == null || selectedReserveUnit == null || !TryGetPointerWorldPoint(out Vector3 worldPoint))
            {
                return;
            }

            Vector3 delta = worldPoint - pendingPlacementTile.WorldPosition;
            delta.y = 0f;

            if (delta.sqrMagnitude < PlacementDragDeadZone * PlacementDragDeadZone)
            {
                return;
            }

            GridFacingDirection newFacing = GetFacing(delta);

            if (newFacing == selectedPlacementFacing)
            {
                return;
            }

            selectedPlacementFacing = newFacing;
            AttackRangeDisplay.ShowPlacement(selectedReserveUnit, pendingPlacementTile.Coordinate, selectedPlacementFacing);
            lastMessage = $"{selectedReserveUnit.DisplayName} 배치 방향: {selectedPlacementFacing}";
        }

        private void ConfirmPlacementDrag()
        {
            if (!placementDragging || pendingPlacementTile == null || selectedReserveUnit == null)
            {
                CancelPlacementDrag();
                return;
            }

            UnitDataSO data = selectedReserveUnit;
            Phase2GroundTile tile = pendingPlacementTile;
            GridFacingDirection facing = selectedPlacementFacing;
            CancelPlacementDrag();

            if (!TryDeployUnitAtTile(data, tile, facing))
            {
                return;
            }

            reserveUnits.Remove(data);
            selectedReserveUnit = null;
        }

        private void CancelPlacementDrag()
        {
            bool wasDragging = placementDragging;
            placementDragging = false;
            pendingPlacementTile = null;

            if (wasDragging)
            {
                AttackRangeDisplay.Hide();
            }
        }

        private bool CanPlaceSelectedUnit(Phase2GroundTile tile)
        {
            if (selectedReserveUnit == null || tile == null)
            {
                return false;
            }

            if (occupiedTiles.Contains(tile.Coordinate))
            {
                lastMessage = $"배치 실패: {tile.Coordinate} 타일은 이미 사용 중입니다.";
                return false;
            }

            if (IsRouteEndpoint(tile.Coordinate))
            {
                lastMessage = "배치 실패: 입구/출구 타일에는 배치할 수 없습니다.";
                return false;
            }

            if (!IsPlacementAllowed(selectedReserveUnit.Placement, tile.Surface))
            {
                lastMessage = $"배치 실패: {selectedReserveUnit.DisplayName}은 {selectedReserveUnit.Placement} 배치이며 선택 타일은 {tile.Surface}입니다.";
                return false;
            }

            return true;
        }

        private bool TryGetPointerWorldPoint(out Vector3 worldPoint)
        {
            worldPoint = Vector3.zero;
            Camera camera = Camera.main;

            if (camera == null || Mouse.current == null)
            {
                return false;
            }

            Vector2 pointerPosition = Mouse.current.position.ReadValue();
            Ray ray = camera.ScreenPointToRay(pointerPosition);
            Plane mapPlane = new Plane(Vector3.up, Vector3.zero);

            if (!mapPlane.Raycast(ray, out float distance))
            {
                return false;
            }

            worldPoint = ray.GetPoint(distance);
            return true;
        }

        private static GridFacingDirection GetFacing(Vector3 delta)
        {
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.z))
            {
                return delta.x >= 0f ? GridFacingDirection.East : GridFacingDirection.West;
            }

            return delta.z >= 0f ? GridFacingDirection.North : GridFacingDirection.South;
        }

        private bool TryDeployUnitAtTile(UnitDataSO data, Phase2GroundTile tile, GridFacingDirection facing)
        {
            if (data == null || data.UnitPrefab == null || tile == null)
            {
                return false;
            }

            if (activeUnits.Count >= initialUnitCount)
            {
                lastMessage = $"동시 배치 한도 {initialUnitCount}명입니다.";
                return false;
            }

            if (occupiedTiles.Contains(tile.Coordinate))
            {
                lastMessage = $"배치 실패: {tile.Coordinate} 타일은 이미 사용 중입니다.";
                return false;
            }

            if (IsRouteEndpoint(tile.Coordinate))
            {
                lastMessage = $"배치 실패: 입구/출구 타일 {tile.Coordinate}에는 배치할 수 없습니다.";
                return false;
            }

            if (!IsPlacementAllowed(data.Placement, tile.Surface))
            {
                lastMessage = $"배치 실패: {data.DisplayName}은 {data.Placement} 전용이며 선택 타일은 {tile.Surface}입니다.";
                return false;
            }

            if (!TrySpendDeploymentCost(data, out int spentCost))
            {
                lastCostMessage = $"{data.DisplayName} 배치 Cost 부족: {currentCost}/{data.SummonCost}";
                return false;
            }

            GameObject instance = Instantiate(data.UnitPrefab, tile.WorldPosition, data.UnitPrefab.transform.rotation, transform);
            UnitRuntimeState state = instance.GetComponent<UnitRuntimeState>();

            if (state == null || state.DataLink == null || !state.DataLink.HasData || state.DataLink.UnitData != data)
            {
                RefundDeploymentCost(spentCost);
                Destroy(instance);
                return false;
            }

            state.GridPosition.Initialize(tile.Coordinate, facing, CombatTargetLayer.Ground);
            occupiedTiles.Add(tile.Coordinate);
            activeUnits.Add(state);
            activeUnitTiles[state] = tile.Coordinate;
            spawnedObjects.Add(instance);

            lastCostMessage = $"{data.DisplayName} 배치 -{spentCost} Cost / 현재 {currentCost}/{MaxCost} / {tile.Surface} {tile.Coordinate} / Facing {facing}";
            Debug.Log(lastCostMessage, state);
            return true;
        }

        public bool TryGetAttackRangeTile(Vector2Int tileCoordinate, out AttackRangeTile tile)
        {
            tile = default;

            if (!tilesByCoordinate.TryGetValue(tileCoordinate, out Phase2GroundTile mapTile) || mapTile == null)
            {
                return false;
            }

            tile = new AttackRangeTile(mapTile.WorldPosition + Vector3.up * 0.22f, new Vector3(0.82f, 0.035f, 0.82f));
            return true;
        }

        public bool TryGetTile(SummonTileRequest request, out SummonTile tile)
        {
            tile = default;

            if (!battleRunning || request.Owner == null || request.SummonData == null || request.Owner.GridPosition == null || !request.Owner.GridPosition.IsInitialized)
            {
                return false;
            }

            summonTileCandidates.Clear();

            if (request.Source is CritSummonSO)
            {
                Vector2Int ownerTile = request.Owner.GridPosition.TileCoordinate;
                AddNearbySummonTileCandidates(request.SummonData.Placement, groundTiles, Phase2TileSurface.Ground, ownerTile, CritSummonNeighborRadius);
                AddNearbySummonTileCandidates(request.SummonData.Placement, highGroundTiles, Phase2TileSurface.HighGround, ownerTile, CritSummonNeighborRadius);
            }
            else
            {
                AddSummonTileCandidates(request.SummonData.Placement, groundTiles, Phase2TileSurface.Ground);
                AddSummonTileCandidates(request.SummonData.Placement, highGroundTiles, Phase2TileSurface.HighGround);
            }

            if (summonTileCandidates.Count == 0)
            {
                lastMessage = request.Source is CritSummonSO ? $"{request.SummonData.DisplayName} 소환 실패: 소환자 주변에 배치 가능한 타일이 없습니다." : $"{request.SummonData.DisplayName} 소환 실패: 배치 가능한 타일이 없습니다.";
                return false;
            }

            Phase2GroundTile selectedTile = summonTileCandidates[Random.Range(0, summonTileCandidates.Count)];
            tile = new SummonTile(selectedTile.WorldPosition, selectedTile.Coordinate);
            lastMessage = request.Source is CritSummonSO ? $"{request.SummonData.DisplayName} 주변 소환 위치: {selectedTile.Surface} {selectedTile.Coordinate}" : $"{request.SummonData.DisplayName} 랜덤 소환 위치: {selectedTile.Surface} {selectedTile.Coordinate}";
            return true;
        }

        private void AddSummonTileCandidates(UnitPlacement placement, List<Phase2GroundTile> source, Phase2TileSurface surface)
        {
            if (!IsPlacementAllowed(placement, surface))
            {
                return;
            }

            for (int i = 0; i < source.Count; i++)
            {
                Phase2GroundTile candidate = source[i];

                if (candidate == null || IsRouteEndpoint(candidate.Coordinate) || IsSummonTileOccupied(candidate.Coordinate))
                {
                    continue;
                }

                summonTileCandidates.Add(candidate);
            }
        }

        private void AddNearbySummonTileCandidates(UnitPlacement placement, List<Phase2GroundTile> source, Phase2TileSurface surface, Vector2Int ownerTile, int radius)
        {
            if (!IsPlacementAllowed(placement, surface))
            {
                return;
            }

            int safeRadius = Mathf.Max(1, radius);

            for (int i = 0; i < source.Count; i++)
            {
                Phase2GroundTile candidate = source[i];

                if (candidate == null || candidate.Coordinate == ownerTile || IsRouteEndpoint(candidate.Coordinate) || IsSummonTileOccupied(candidate.Coordinate))
                {
                    continue;
                }

                Vector2Int delta = candidate.Coordinate - ownerTile;
                int distance = Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y));

                if (distance <= 0 || distance > safeRadius)
                {
                    continue;
                }

                summonTileCandidates.Add(candidate);
            }
        }

        private static bool IsSummonTileOccupied(Vector2Int coordinate)
        {
            foreach (UnitRuntimeState unit in CombatRegistry.Units)
            {
                if (unit == null || !unit.gameObject.activeInHierarchy || !unit.IsInitialized || unit.Health == null || unit.Health.IsDead || unit.GridPosition == null || !unit.GridPosition.IsInitialized)
                {
                    continue;
                }

                if (unit.GridPosition.TileCoordinate == coordinate)
                {
                    return true;
                }
            }

            foreach (EnemyRuntimeState enemy in CombatRegistry.Enemies)
            {
                if (enemy == null || !enemy.gameObject.activeInHierarchy || !enemy.IsInitialized || enemy.Health == null || enemy.Health.IsDead || enemy.GridPosition == null || !enemy.GridPosition.IsInitialized)
                {
                    continue;
                }

                if (enemy.GridPosition.TileCoordinate == coordinate)
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryGetTileUnderPointer(out Phase2GroundTile tile)
        {
            tile = null;
            Camera camera = Camera.main;

            if (camera == null || Mouse.current == null)
            {
                return false;
            }

            Vector2 pointerPosition = Mouse.current.position.ReadValue();
            Ray ray = camera.ScreenPointToRay(pointerPosition);
            Plane mapPlane = new Plane(Vector3.up, Vector3.zero);

            if (!mapPlane.Raycast(ray, out float distance))
            {
                return false;
            }

            Vector3 worldPoint = ray.GetPoint(distance);
            Vector2Int coordinate = new Vector2Int(Mathf.RoundToInt(worldPoint.x), Mathf.RoundToInt(worldPoint.z));
            return tilesByCoordinate.TryGetValue(coordinate, out tile);
        }

        private bool TryFindPlacementTile(UnitPlacement placement, out Phase2GroundTile tile)
        {
            tile = null;

            switch (placement)
            {
                case UnitPlacement.Ground:
                    return TryFindFreeTile(groundTiles, out tile);

                case UnitPlacement.HighGround:
                    return TryFindFreeTile(highGroundTiles, out tile);

                case UnitPlacement.GroundAndHighGround:
                    return TryFindFreeTile(groundTiles, out tile) || TryFindFreeTile(highGroundTiles, out tile);

                default:
                    return false;
            }
        }

        private bool TryFindFreeTile(List<Phase2GroundTile> candidates, out Phase2GroundTile tile)
        {
            tile = null;

            for (int i = 0; i < candidates.Count; i++)
            {
                Phase2GroundTile current = candidates[i];

                if (current == null || occupiedTiles.Contains(current.Coordinate) || IsRouteEndpoint(current.Coordinate))
                {
                    continue;
                }

                tile = current;
                return true;
            }

            return false;
        }

        private static bool IsPlacementAllowed(UnitPlacement placement, Phase2TileSurface surface)
        {
            if (placement == UnitPlacement.Ground)
            {
                return surface == Phase2TileSurface.Ground;
            }

            if (placement == UnitPlacement.HighGround)
            {
                return surface == Phase2TileSurface.HighGround;
            }

            return placement == UnitPlacement.GroundAndHighGround;
        }

        private void StartAutoEnemySpawn()
        {
            if (!battleRunning || waveRoutine != null)
            {
                return;
            }

            waveRoutine = StartCoroutine(SpawnWave());
        }

        private void StopAutoEnemySpawn()
        {
            if (waveRoutine == null)
            {
                return;
            }

            StopCoroutine(waveRoutine);
            waveRoutine = null;
        }

        private void SetAutoEnemySpawn(bool enabled)
        {
            autoEnemySpawnEnabled = enabled;

            if (!battleRunning)
            {
                return;
            }

            if (enabled)
            {
                StartAutoEnemySpawn();
            }
            else
            {
                StopAutoEnemySpawn();
            }
        }

        private IEnumerator SpawnWave()
        {
            List<EnemyDataSO> wave = new List<EnemyDataSO>();
            IReadOnlyList<EnemyDataSO> catalogEnemies = enemyCatalog.Enemies;

            for (int i = 0; i < catalogEnemies.Count; i++)
            {
                EnemyDataSO data = catalogEnemies[i];

                if (data != null && data.EnemyPrefab != null)
                {
                    wave.Add(data);
                }
            }

            wave.Sort(CompareEnemyData);

            for (int i = 0; i < wave.Count; i++)
            {
                if (!battleRunning)
                {
                    yield break;
                }

                TrySpawnEnemy(wave[i]);
                float elapsed = 0f;

                while (battleRunning && elapsed < enemySpawnInterval)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }

            waveRoutine = null;
            lastMessage = $"정식 몬스터 {enemySpawnCount}마리 생성 완료. 남은 전투를 계속합니다.";
            Debug.Log(lastMessage, this);
        }

        private bool TrySpawnEnemy(EnemyDataSO data)
        {
            if (data == null || data.EnemyPrefab == null || enemyRoute == null)
            {
                return false;
            }

            PathNode[] path;
            bool pathReady = data.MovementType == EnemyMovementType.Air ? enemyRoute.BuildAirPath(airHeight, out path) : enemyRoute.BuildGroundPath(out path);

            if (!pathReady || path == null || path.Length == 0)
            {
                Fail($"{data.DisplayName} 경로 생성에 실패했습니다.");
                return false;
            }

            GameObject instance = Instantiate(data.EnemyPrefab, path[0].Position, data.EnemyPrefab.transform.rotation, transform);
            EnemyRuntimeState state = instance.GetComponent<EnemyRuntimeState>();

            if (state == null || state.DataLink == null || !state.DataLink.HasData || state.DataLink.EnemyData != data || state.Move == null || !state.Move.SetPath(path))
            {
                Destroy(instance);
                Fail($"{data.DisplayName} 생성 또는 경로 연결에 실패했습니다.");
                return false;
            }

            spawnedObjects.Add(instance);
            enemySpawnCount++;
            Debug.Log($"몬스터 등장 #{enemySpawnCount}: {data.EnemyId} {data.DisplayName} / EXP {data.RewardExp} / Gold {data.RewardGold}", state);
            return true;
        }

        private void HandleUnitDied(UnitDiedInfo info)
        {
            UnitRuntimeState unit = FindActiveUnit(info.RuntimeId);

            if (!battleRunning || unit == null || unit.IsSummon)
            {
                return;
            }

            string unitName = GetUnitDisplayName(unit);
            RemoveUnit(unit);
            lastMessage = $"{unitName} 전투 이탈. {replacementDelay:0.##}초 후 현재 Cost로 배치 가능한 대기 캐릭터를 다시 확인합니다.";
            Debug.Log(lastMessage, this);
            StartCoroutine(DeployReplacementAfterDelay());
        }

        private IEnumerator DeployReplacementAfterDelay()
        {
            float elapsed = 0f;

            while (battleRunning && elapsed < replacementDelay)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!battleRunning)
            {
                yield break;
            }

            autoDeployRequested = autoDeployEnabled;
        }

        private void RemoveUnit(UnitRuntimeState unit)
        {
            if (unit == null)
            {
                return;
            }

            if (activeUnitTiles.TryGetValue(unit, out Vector2Int tile))
            {
                occupiedTiles.Remove(tile);
                activeUnitTiles.Remove(unit);
            }

            activeUnits.Remove(unit);

            if (selectedUnit == unit)
            {
                ClearSelectedUnit();
            }

            GameObject target = unit.gameObject;
            spawnedObjects.Remove(target);
            target.SetActive(false);
            Destroy(target);
        }

        private void HandleEnemyDied(EnemyDiedInfo info)
        {
            EnemyRuntimeState enemy = FindSpawnedEnemy(info.RuntimeId);

            if (!battleRunning || enemy == null || enemy.IsSummon)
            {
                return;
            }

            enemyDeathCount++;

            if (enemy.DataLink != null && enemy.DataLink.HasData)
            {
                int rewardGold = Mathf.Max(0, enemy.DataLink.EnemyData.RewardGold);
                currentGold += rewardGold;
                totalGoldEarned += rewardGold;
                lastMessage = $"{enemy.DataLink.EnemyData.DisplayName} 처치: EXP {enemy.DataLink.EnemyData.RewardExp} / Gold +{rewardGold} / 보유 Gold {currentGold}";
            }

            StartCoroutine(RemoveEnemyNextFrame(enemy));
        }

        private void HandleEnemyReachedGoal(EnemyReachedGoalInfo info)
        {
            EnemyRuntimeState enemy = FindSpawnedEnemy(info.RuntimeId);

            if (!battleRunning || enemy == null || enemy.IsSummon)
            {
                return;
            }

            enemyReachedGoalCount++;
            currentExitHp = Mathf.Max(0, currentExitHp - 1);
            Debug.Log($"몬스터 출구 도달: {info.EnemyId} / 출구 HP {currentExitHp}/{maxExitHp}", enemy);
            StartCoroutine(RemoveEnemyNextFrame(enemy));

            if (currentExitHp <= 0)
            {
                battleRunning = false;

                if (waveRoutine != null)
                {
                    StopCoroutine(waveRoutine);
                    waveRoutine = null;
                }

                combatLoop.StopLoop();
                lastMessage = "출구 HP가 0이 되어 Ground 전투가 종료되었습니다.";
                Debug.LogWarning(lastMessage, this);
            }
        }

        private IEnumerator RemoveEnemyNextFrame(EnemyRuntimeState enemy)
        {
            yield return null;

            if (enemy == null)
            {
                yield break;
            }

            GameObject target = enemy.gameObject;
            spawnedObjects.Remove(target);
            target.SetActive(false);
            Destroy(target);
        }

        private void HandleProgressChanged(UnitProgressChangedInfo info)
        {
            if (!battleRunning || info.Progress == null)
            {
                return;
            }

            UnitRuntimeState target = FindActiveUnit(info.UnitId);

            if (target == null)
            {
                return;
            }

            progressEventCount++;

            if ((info.ChangeType & UnitProgressChangeType.Level) != 0)
            {
                lastProgressMessage = $"{GetUnitDisplayName(target)} LEVEL UP: Lv.{info.PreviousLevel} -> Lv.{info.CurrentLevel} / EXP {info.CurrentExp}";
                return;
            }

            if ((info.ChangeType & UnitProgressChangeType.Promotion) != 0)
            {
                lastProgressMessage = $"{GetUnitDisplayName(target)} 승급: Stage {info.PreviousPromotionStage} -> {info.CurrentPromotionStage} / MaxLv {info.PreviousMaxLevel} -> {info.CurrentMaxLevel}";
                return;
            }

            if ((info.ChangeType & UnitProgressChangeType.Experience) != 0)
            {
                lastProgressMessage = $"{GetUnitDisplayName(target)} EXP: {info.PreviousExp} -> {info.CurrentExp}";
            }
        }

        private UnitRuntimeState FindActiveUnit(int runtimeId)
        {
            for (int i = 0; i < activeUnits.Count; i++)
            {
                UnitRuntimeState unit = activeUnits[i];

                if (unit != null && unit.RuntimeId == runtimeId)
                {
                    return unit;
                }
            }

            return null;
        }

        private EnemyRuntimeState FindSpawnedEnemy(int runtimeId)
        {
            for (int i = 0; i < spawnedObjects.Count; i++)
            {
                GameObject spawned = spawnedObjects[i];

                if (spawned == null)
                {
                    continue;
                }

                EnemyRuntimeState enemy = spawned.GetComponent<EnemyRuntimeState>();

                if (enemy != null && enemy.RuntimeId == runtimeId)
                {
                    return enemy;
                }
            }

            return null;
        }

        private UnitRuntimeState FindActiveUnit(string unitId)
        {
            for (int i = 0; i < activeUnits.Count; i++)
            {
                UnitRuntimeState unit = activeUnits[i];

                if (unit != null && unit.UnitId == unitId)
                {
                    return unit;
                }
            }

            return null;
        }

        private void TryUpgradeGoldStat(int statIndex)
        {
            if (statIndex < 0 || statIndex >= GrowthStatCount)
            {
                return;
            }

            int cost = GetUpgradeCost(statIndex);

            if (currentGold < cost)
            {
                lastMessage = $"{GetGrowthStatName(statIndex)} 강화 실패: Gold {currentGold}/{cost}";
                return;
            }

            currentGold -= cost;
            totalGoldSpent += cost;
            goldUpgradeLevels[statIndex]++;

            GrowthStatMask stat = GetGrowthStatMask(statIndex);
            float totalBonus = GetGrowthAmount(statIndex) * goldUpgradeLevels[statIndex];
            CommonGrowthService.Set(stat, totalBonus);

            lastMessage = $"{GetGrowthStatName(statIndex)} 공통 강화 Lv.{goldUpgradeLevels[statIndex]} / Gold -{cost} / 현재 {currentGold}";
        }

        private int GetUpgradeCost(int statIndex)
        {
            return Mathf.Max(1, upgradeBaseCost + goldUpgradeLevels[statIndex] * upgradeCostIncrease);
        }

        private float GetGrowthAmount(int statIndex)
        {
            switch (statIndex)
            {
                case 0: return maxHpUpgradeAmount;
                case 1: return hpRegenUpgradeAmount;
                case 2: return physicalAttackUpgradeAmount;
                case 3: return magicalAttackUpgradeAmount;
                case 4: return physicalDefenseUpgradeAmount;
                case 5: return magicalDefenseUpgradeAmount;
                case 6: return attackSpeedUpgradeAmount;
                case 7: return accuracyUpgradeAmount;
                case 8: return evasionUpgradeAmount;
                case 9: return criticalChanceUpgradeAmount;
                case 10: return criticalDamageUpgradeAmount;
                default: return 0f;
            }
        }

        private static GrowthStatMask GetGrowthStatMask(int statIndex)
        {
            switch (statIndex)
            {
                case 0: return GrowthStatMask.MaxHp;
                case 1: return GrowthStatMask.HpRegenPerSecond;
                case 2: return GrowthStatMask.PhysicalAttack;
                case 3: return GrowthStatMask.MagicalAttack;
                case 4: return GrowthStatMask.PhysicalDefense;
                case 5: return GrowthStatMask.MagicalDefense;
                case 6: return GrowthStatMask.AttacksPerSecond;
                case 7: return GrowthStatMask.Accuracy;
                case 8: return GrowthStatMask.Evasion;
                case 9: return GrowthStatMask.CriticalChancePercent;
                case 10: return GrowthStatMask.CriticalDamageBonusPercent;
                default: return GrowthStatMask.None;
            }
        }

        private static string GetGrowthStatName(int statIndex)
        {
            switch (statIndex)
            {
                case 0: return "최대 HP";
                case 1: return "초당 HP 재생";
                case 2: return "물리 공격";
                case 3: return "마법 공격";
                case 4: return "물리 방어";
                case 5: return "마법 방어";
                case 6: return "공격속도";
                case 7: return "명중";
                case 8: return "회피";
                case 9: return "치명타 확률";
                case 10: return "치명타 피해";
                default: return "미지정";
            }
        }

        private string GetExperienceText(UnitRuntimeState unit)
        {
            if (!CanUseUnit(unit) || unit.Progress == null)
            {
                return "EXP -";
            }

            if (unit.CurrentLevel >= unit.MaxLevel)
            {
                return $"EXP {unit.Progress.CurrentExp} / MAX";
            }

            UnitClassGrowthTableSO growthTable = unit.DataLink.UnitData.GrowthTable;
            UnitLevelCurveSO levelCurve = growthTable != null ? growthTable.LevelCurve : null;

            if (levelCurve == null)
            {
                return $"EXP {unit.Progress.CurrentExp} / ?";
            }

            long required = levelCurve.GetRequiredExp(unit.CurrentLevel);
            return $"EXP {unit.Progress.CurrentExp} / {required}";
        }

        private void ApplyPromotionToSelectedUnit()
        {
            if (!CanUseUnit(selectedUnit))
            {
                lastMessage = "승급 검증할 캐릭터를 선택하세요.";
                return;
            }

            int previousStage = selectedUnit.PromotionStage;
            int previousMaxLevel = selectedUnit.MaxLevel;
            bool success = selectedUnit.ApplyApprovedPromotion();

            if (success)
            {
                lastMessage = $"{GetUnitDisplayName(selectedUnit)} 승급 승인 결과 적용: Stage {previousStage}->{selectedUnit.PromotionStage}, MaxLv {previousMaxLevel}->{selectedUnit.MaxLevel}";
            }
            else
            {
                lastMessage = $"{GetUnitDisplayName(selectedUnit)} 승급 실패: 현재 Stage {previousStage}, MaxLv {previousMaxLevel}";
            }
        }

        private void EnsureSelectedUnit()
        {
            if (CanUseUnit(selectedUnit) && activeUnits.Contains(selectedUnit))
            {
                return;
            }

            if (selectedUnit != null)
            {
                ClearSelectedUnit();
            }
        }

        private int ComparePlacementTiles(Phase2GroundTile left, Phase2GroundTile right)
        {
            int leftDistance = GetDistanceToRoute(left);
            int rightDistance = GetDistanceToRoute(right);

            if (leftDistance != rightDistance)
            {
                return leftDistance.CompareTo(rightDistance);
            }

            int yCompare = left.Coordinate.y.CompareTo(right.Coordinate.y);

            if (yCompare != 0)
            {
                return yCompare;
            }

            return left.Coordinate.x.CompareTo(right.Coordinate.x);
        }

        private int GetDistanceToRoute(Phase2GroundTile tile)
        {
            if (tile == null || enemyRoute == null || enemyRoute.RouteTiles == null)
            {
                return int.MaxValue;
            }

            int best = int.MaxValue;

            for (int i = 0; i < enemyRoute.RouteTiles.Count; i++)
            {
                Phase2GroundTile routeTile = enemyRoute.RouteTiles[i];

                if (routeTile == null)
                {
                    continue;
                }

                Vector2Int delta = tile.Coordinate - routeTile.Coordinate;
                int distance = Mathf.Abs(delta.x) + Mathf.Abs(delta.y);

                if (distance < best)
                {
                    best = distance;
                }
            }

            return best;
        }

        private bool IsRouteEndpoint(Vector2Int coordinate)
        {
            if (enemyRoute == null || enemyRoute.RouteTiles == null || enemyRoute.RouteTiles.Count < 2)
            {
                return false;
            }

            Phase2GroundTile start = enemyRoute.RouteTiles[0];
            Phase2GroundTile goal = enemyRoute.RouteTiles[enemyRoute.RouteTiles.Count - 1];
            return start != null && start.Coordinate == coordinate || goal != null && goal.Coordinate == coordinate;
        }

        private static int CompareUnitDeploymentPriority(UnitDataSO left, UnitDataSO right)
        {
            int leftCost = left != null ? left.SummonCost : int.MaxValue;
            int rightCost = right != null ? right.SummonCost : int.MaxValue;

            if (leftCost != rightCost)
            {
                return leftCost.CompareTo(rightCost);
            }

            string leftId = left != null ? left.UnitId : string.Empty;
            string rightId = right != null ? right.UnitId : string.Empty;
            return string.CompareOrdinal(leftId, rightId);
        }

        private static int CompareEnemyData(EnemyDataSO left, EnemyDataSO right)
        {
            string leftId = left != null ? left.EnemyId : string.Empty;
            string rightId = right != null ? right.EnemyId : string.Empty;
            return string.CompareOrdinal(leftId, rightId);
        }

        private static bool CanUseUnit(UnitRuntimeState unit)
        {
            return unit != null && unit.gameObject.activeInHierarchy && unit.IsInitialized && !unit.IsSummon && unit.Health != null && !unit.Health.IsDead && unit.Stats != null && unit.Stats.IsInitialized && unit.DataLink != null && unit.DataLink.HasData && unit.GridPosition != null && unit.GridPosition.IsInitialized;
        }

        private static string GetUnitDisplayName(UnitRuntimeState unit)
        {
            return unit != null && unit.DataLink != null && unit.DataLink.HasData ? unit.DataLink.UnitData.DisplayName : "미지정";
        }

        private void Fail(string message)
        {
            lastMessage = message;
            Debug.LogError(message, this);
        }

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            DrawTopBar();

            if (!battleRunning)
            {
                GUILayout.BeginArea(new Rect(10f, 60f, 300f, 110f), GUI.skin.box);

                if (GUILayout.Button("Ground 전투 준비 시작", GUILayout.Height(32f)))
                {
                    StartBattle();
                }

                GUILayout.Label("전투 시작 후 캐릭터와 몬스터를 직접 선택할 수 있습니다.");
                GUILayout.Label(lastMessage);
                GUILayout.EndArea();
                return;
            }

            DrawUnitPanel();
            DrawGrowthPanel();
            DrawEnemySpawnPanel();
            DrawReserveDeploymentPanel();
        }

        private void DrawTopBar()
        {
            float width = Mathf.Max(500f, Screen.width - 20f);
            GUILayout.BeginArea(new Rect(10f, 10f, width, 44f), GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"COST {currentCost}/{MaxCost}", GUILayout.Width(105f));
            GUILayout.Label($"자동 +{costRegenPerSecond:0.##}/초", GUILayout.Width(95f));
            GUILayout.Label($"패시브 +{totalPassiveCostGained} ({passiveCostRequestCount}회)", GUILayout.Width(145f));
            GUILayout.Label($"GOLD {currentGold}", GUILayout.Width(95f));
            GUILayout.Label($"출구 HP {currentExitHp}/{maxExitHp}", GUILayout.Width(115f));
            GUILayout.Label($"배치 {activeUnits.Count} / 대기 {reserveUnits.Count}", GUILayout.Width(115f));

            bool newAutoDeploy = GUILayout.Toggle(autoDeployEnabled, "자동배치", GUILayout.Width(85f));

            if (newAutoDeploy != autoDeployEnabled)
            {
                autoDeployEnabled = newAutoDeploy;
                autoDeployRequested = autoDeployEnabled;
            }

            bool newAutoEnemySpawn = GUILayout.Toggle(autoEnemySpawnEnabled, "자동몬스터", GUILayout.Width(95f));

            if (newAutoEnemySpawn != autoEnemySpawnEnabled)
            {
                SetAutoEnemySpawn(newAutoEnemySpawn);
            }

            GUILayout.Label($"적 사망 {enemyDeathCount}/{enemySpawnCount}");
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawUnitPanel()
        {
            float height = Mathf.Max(220f, Screen.height - 280f);
            GUILayout.BeginArea(new Rect(10f, 60f, 330f, height), GUI.skin.box);
            GUILayout.Label("전투 캐릭터 / Lv / EXP");
            unitScroll = GUILayout.BeginScrollView(unitScroll, GUILayout.Height(Mathf.Min(180f, height * 0.38f)));

            for (int i = 0; i < activeUnits.Count; i++)
            {
                UnitRuntimeState unit = activeUnits[i];

                if (!CanUseUnit(unit))
                {
                    continue;
                }

                string mark = unit == selectedUnit ? "▶ " : string.Empty;
                string label = $"{mark}{GetUnitDisplayName(unit)}  Lv.{unit.CurrentLevel}/{unit.MaxLevel}  {GetExperienceText(unit)}";

                if (GUILayout.Button(label, GUILayout.Height(26f)))
                {
                    SelectUnit(unit);
                }
            }

            GUILayout.EndScrollView();
            GUILayout.Space(5f);
            DrawSelectedUnitDetails();
            GUILayout.EndArea();
        }

        private void DrawSelectedUnitDetails()
        {
            EnsureSelectedUnit();

            if (!CanUseUnit(selectedUnit))
            {
                GUILayout.Label("선택 캐릭터 없음");
                return;
            }

            RuntimeStats stats = selectedUnit.Stats;
            GUILayout.Label($"{GetUnitDisplayName(selectedUnit)} ({selectedUnit.UnitId})");
            GUILayout.Label($"배치: {selectedUnit.DataLink.UnitData.Placement} / Cost {selectedUnit.DataLink.UnitData.SummonCost}");

            if (selectedUnit.GridPosition != null)
            {
                AttackRangeRotationMode rotationMode = selectedUnit.DataLink.UnitData.AttackSettings != null ? selectedUnit.DataLink.UnitData.AttackSettings.RangeRotationMode : AttackRangeRotationMode.Fixed;
                string rotationLabel = rotationMode == AttackRangeRotationMode.FollowFacing ? "Facing 연동" : "방향 고정";
                GUILayout.Label($"Facing {selectedUnit.GridPosition.FacingDirection} / 공격범위 {rotationLabel}");
            }

            GUILayout.Label($"Lv.{selectedUnit.CurrentLevel}/{selectedUnit.MaxLevel}  {GetExperienceText(selectedUnit)}");
            GUILayout.Label($"Promotion Stage {selectedUnit.PromotionStage}");
            GUILayout.Label($"레벨 성장 +{selectedUnit.Growth.AppliedLevelGrowthPercent:0.##}% / 승급 성장 +{selectedUnit.Growth.AppliedPromotionGrowthPercent:0.##}%");
            GUILayout.Space(4f);
            GUILayout.Label($"HP {selectedUnit.Health.CurrentHp:0}/{stats.MaxHp:0} | HP재생 {stats.HpRegenPerSecond:0.##}");
            GUILayout.Label($"물공 {stats.PhysicalAttack:0.##} | 마공 {stats.MagicalAttack:0.##} | 공속 {stats.AttacksPerSecond:0.###}");
            GUILayout.Label($"물방 {stats.PhysicalDefense:0.##} | 마방 {stats.MagicalDefense:0.##}");
            GUILayout.Label($"명중 {stats.Accuracy:0.##} | 회피 {stats.Evasion:0.##}");
            GUILayout.Label($"치확 {stats.CriticalChancePercent:0.##}% | 치피 +{stats.CriticalDamageBonusPercent:0.##}%");
            GUILayout.Space(5f);

            if (GUILayout.Button($"승급 승인 테스트 Stage {selectedUnit.PromotionStage} -> {selectedUnit.PromotionStage + 1}", GUILayout.Height(28f)))
            {
                ApplyPromotionToSelectedUnit();
            }

            if (GUILayout.Button("현재 선택 캐릭터 퇴장"))
            {
                string unitName = GetUnitDisplayName(selectedUnit);
                RemoveUnit(selectedUnit);
                replacementCount++;
                lastMessage = $"{unitName}을 Prototype에서 수동 퇴장시켰습니다.";
            }

            GUILayout.Label(lastProgressMessage);
            GUILayout.Label(lastCostMessage);
        }

        private void DrawGrowthPanel()
        {
            float width = 300f;
            float height = Mathf.Max(220f, Screen.height - 280f);
            float x = Mathf.Max(350f, Screen.width - width - 10f);
            GUILayout.BeginArea(new Rect(x, 60f, width, height), GUI.skin.box);
            GUILayout.Label($"공통 Gold 강화 / 보유 {currentGold}");
            GUILayout.Label($"획득 {totalGoldEarned} / 소비 {totalGoldSpent}");
            growthScroll = GUILayout.BeginScrollView(growthScroll);

            for (int i = 0; i < GrowthStatCount; i++)
            {
                int cost = GetUpgradeCost(i);
                float amount = GetGrowthAmount(i);
                string suffix = i == 6 ? "회/초" : i == 9 || i == 10 ? "%p" : string.Empty;
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label($"{GetGrowthStatName(i)} Lv.{goldUpgradeLevels[i]} / 누적 +{amount * goldUpgradeLevels[i]:0.###}{suffix}");

                if (GUILayout.Button($"+{amount:0.###}{suffix} / {cost} Gold"))
                {
                    TryUpgradeGoldStat(i);
                }

                GUILayout.EndVertical();
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawEnemySpawnPanel()
        {
            float y = Mathf.Max(270f, Screen.height - 205f);
            GUILayout.BeginArea(new Rect(10f, y, Screen.width - 20f, 85f), GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label("몬스터 직접 소환", GUILayout.Width(110f));
            GUILayout.Label("원하는 몬스터를 반복해서 눌러 같은 종류만 테스트할 수 있습니다.");
            GUILayout.EndHorizontal();
            enemyScroll = GUILayout.BeginScrollView(enemyScroll, true, false, GUILayout.Height(55f));
            GUILayout.BeginHorizontal();

            if (enemyCatalog != null)
            {
                IReadOnlyList<EnemyDataSO> enemies = enemyCatalog.Enemies;

                for (int i = 0; i < enemies.Count; i++)
                {
                    EnemyDataSO data = enemies[i];

                    if (data == null || data.EnemyPrefab == null)
                    {
                        continue;
                    }

                    if (GUILayout.Button($"{data.EnemyId}\n{data.DisplayName} / {data.MovementType}", GUILayout.Width(130f), GUILayout.Height(45f)))
                    {
                        TrySpawnEnemy(data);
                    }
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawReserveDeploymentPanel()
        {
            float y = Mathf.Max(360f, Screen.height - 115f);
            GUILayout.BeginArea(new Rect(10f, y, Screen.width - 20f, 105f), GUI.skin.box);
            GUILayout.BeginHorizontal();
            string selectedName = selectedReserveUnit != null ? selectedReserveUnit.DisplayName : "없음";
            GUILayout.Label($"배치 선택: {selectedName}", GUILayout.Width(145f));
            GUILayout.Label("캐릭터 선택 → 타일에서 마우스를 누른 채 방향으로 드래그 → 놓으면 배치. 드래그하지 않으면 경로 방향 자동 설정.", GUILayout.Width(720f));
            GUILayout.EndHorizontal();
            reserveScroll = GUILayout.BeginScrollView(reserveScroll, true, false, GUILayout.Height(60f));
            GUILayout.BeginHorizontal();

            for (int i = 0; i < reserveUnits.Count; i++)
            {
                UnitDataSO data = reserveUnits[i];

                if (data == null)
                {
                    continue;
                }

                string mark = data == selectedReserveUnit ? "▶ " : string.Empty;
                string affordable = currentCost >= data.SummonCost ? "가능" : "부족";

                if (GUILayout.Button($"{mark}{data.DisplayName}\nCost {data.SummonCost} / {data.Placement} / {affordable}", GUILayout.Width(145f), GUILayout.Height(48f)))
                {
                    CancelPlacementDrag();
                    ClearSelectedUnit();
                    selectedReserveUnit = data;
                    lastMessage = $"{data.DisplayName} 선택. {data.Placement} 타일에서 누른 채 방향으로 드래그하세요. Cost {currentCost}/{data.SummonCost}";
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private GridFacingDirection GetAutoFacingToRoute(Vector2Int unitTile)
        {
            if (enemyRoute == null || enemyRoute.RouteTiles == null || enemyRoute.RouteTiles.Count == 0)
            {
                return GridFacingDirection.North;
            }

            Vector2Int nearestTile = unitTile;
            int nearestDistance = int.MaxValue;

            for (int i = 0; i < enemyRoute.RouteTiles.Count; i++)
            {
                Phase2GroundTile routeTile = enemyRoute.RouteTiles[i];

                if (routeTile == null)
                {
                    continue;
                }

                Vector2Int delta = routeTile.Coordinate - unitTile;
                int distance = Mathf.Abs(delta.x) + Mathf.Abs(delta.y);

                if (distance >= nearestDistance)
                {
                    continue;
                }

                nearestDistance = distance;
                nearestTile = routeTile.Coordinate;
            }

            Vector2Int direction = nearestTile - unitTile;

            if (direction == Vector2Int.zero)
            {
                return GridFacingDirection.North;
            }

            if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
            {
                return direction.x >= 0 ? GridFacingDirection.East : GridFacingDirection.West;
            }

            return direction.y >= 0 ? GridFacingDirection.North : GridFacingDirection.South;
        }

        private bool IsPointerOverPrototypeUI()
        {
            if (Mouse.current == null)
            {
                return false;
            }

            Vector2 pointerPosition = Mouse.current.position.ReadValue();
            Vector2 point = new Vector2(pointerPosition.x, Screen.height - pointerPosition.y);
            Rect top = new Rect(10f, 10f, Screen.width - 20f, 44f);
            Rect left = new Rect(10f, 60f, 330f, Mathf.Max(220f, Screen.height - 280f));
            Rect right = new Rect(Mathf.Max(350f, Screen.width - 310f), 60f, 300f, Mathf.Max(220f, Screen.height - 280f));
            Rect enemy = new Rect(10f, Mathf.Max(270f, Screen.height - 205f), Screen.width - 20f, 85f);
            Rect reserve = new Rect(10f, Mathf.Max(360f, Screen.height - 115f), Screen.width - 20f, 105f);
            return top.Contains(point) || left.Contains(point) || right.Contains(point) || enemy.Contains(point) || reserve.Contains(point);
        }
    }
}