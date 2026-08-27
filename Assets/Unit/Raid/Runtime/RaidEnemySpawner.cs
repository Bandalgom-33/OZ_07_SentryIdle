using System;
using System.Collections;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Raid.Data;
using EndlessGuard.Unit.Runtime;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    public enum RaidMapSelectionMode
    {
        [InspectorName("Map 01")]
        Map01 = 0,

        [InspectorName("Map 02")]
        Map02 = 1,

        [InspectorName("랜덤")]
        Random = 2
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(RaidBattleController))]
    [RequireComponent(typeof(RaidBoardRuntime))]
    public sealed class RaidEnemySpawner : MonoBehaviour
    {
        private const string Map01FamilyId = "RAID_MAP_01";
        private const string Map02FamilyId = "RAID_MAP_02";
        private const int MaxPrewarmCountPerEnemyType = 8;

        [Header("맵 선택")]
        [Tooltip("검증 중에는 Map 01/02를 직접 선택합니다. 검증 완료 후 랜덤으로 바꾸면 레이드 시작마다 완성된 맵 중 하나를 선택합니다.")]
        [SerializeField] private RaidMapSelectionMode mapSelection = RaidMapSelectionMode.Map02;

        [Header("생성")]
        [SerializeField] private Transform enemyRoot;

        [Header("경로 선택")]
        [SerializeField] private RaidPathMode pathMode = RaidPathMode.RoundRobin;

        private readonly List<int> automaticEntryIds = new List<int>(4);
        private readonly List<EnemyDataSO> automaticEnemyPool = new List<EnemyDataSO>(8);

        private RaidBattleController battle;
        private RaidBoardRuntime board;
        private SpawnedEnemyManager cleanupManager;
        private RaidPathSelector selector;
        private RaidRouteGraph selectorGraph;
        private int selectorKey;
        private int selectorRoutePlanCount;
        private RaidPathMode selectorMode;
        private Coroutine automaticSpawnRoutine;
        private float automaticRaidStartTime;
        private RaidRouteGraph automaticSpawnGraph;
        private RaidPhase automaticSpawnPhase;
        private int automaticEntryCursor;
        private int automaticEnemyCursor;
        private const float MaxGuideCountdownDeltaTime = 0.05f;

        private bool preparedForRaidStart;
        private bool spawnCountdownStartedByRouteGuide;
        private float spawnCountdownElapsedByRouteGuide;

        public RaidMapSelectionMode MapSelection => mapSelection;
        public RaidPathMode PathMode => pathMode;
        public int SpawnCount { get; private set; }
        public int AutomaticSpawnCount { get; private set; }
        public int AutomaticBurstCount { get; private set; }
        public int AirSpawnCount { get; private set; }
        public int SpeedAdjustedSpawnCount { get; private set; }
        public int FailedAutomaticSpawnCount { get; private set; }
        public bool IsAutomaticSpawnRunning => automaticSpawnRoutine != null;
        public int AutomaticEntryCount => automaticEntryIds.Count;
        public int AutomaticEnemyPoolCount => automaticEnemyPool.Count;
        public int ActiveEnemyCount => CombatRegistry.EnemyCount;
        public int PooledEnemyCreatedCount => RaidEnemyPool.CreatedCount;
        public int PooledEnemyReusedCount => RaidEnemyPool.ReusedCount;
        public int PooledEnemyReleasedCount => RaidEnemyPool.ReleasedCount;
        public float LastBaseMoveSpeed { get; private set; }
        public float LastRaidMoveSpeed { get; private set; }
        public string LastSpawnedEnemyId { get; private set; } = string.Empty;
        public bool LastSpawnWasAir { get; private set; }
        public int LastAirCorridorVariant { get; private set; } = -1;
        public int PassiveValidationFailureCount { get; private set; }
        public int LastPassiveAssignedCount { get; private set; }
        public int LastPassiveAppliedCount { get; private set; }
        public int LastPassiveRejectedCount { get; private set; }
        public int LastPassiveUnsupportedCount { get; private set; }

        private void Awake()
        {
            battle = GetComponent<RaidBattleController>();
            board = GetComponent<RaidBoardRuntime>();
            ConfigureStartupMap();

            cleanupManager = SpawnedEnemyManager.Instance;
            if (cleanupManager == null)
            {
                Debug.LogError("Raid 몬스터 사망 제거를 담당할 SpawnedEnemyManager를 준비하지 못했습니다.", this);
            }
        }

        private void ConfigureStartupMap()
        {
            if (board == null)
            {
                throw new InvalidOperationException("RaidEnemySpawner에 RaidBoardRuntime이 없습니다.");
            }

            switch (mapSelection)
            {
                case RaidMapSelectionMode.Map01:
                    board.SetStartupFamily(Map01FamilyId);
                    break;
                case RaidMapSelectionMode.Map02:
                    board.SetStartupFamily(Map02FamilyId);
                    break;
                case RaidMapSelectionMode.Random:
                    board.SetStartupRandomFamily();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mapSelection), mapSelection, "지원하지 않는 Raid Map 선택입니다.");
            }
        }

        private void OnEnable()
        {
            battle.OnRaidPreparing += HandleRaidPreparing;
            battle.OnRaidStarted += HandleRaidStarted;
            battle.OnRaidEnded += HandleRaidEnded;
            battle.OnPhaseTransitionCompleted += HandlePhaseTransitionCompleted;
        }

        private void OnDisable()
        {
            battle.OnRaidPreparing -= HandleRaidPreparing;
            battle.OnRaidStarted -= HandleRaidStarted;
            battle.OnRaidEnded -= HandleRaidEnded;
            battle.OnPhaseTransitionCompleted -= HandlePhaseTransitionCompleted;
            StopAutomaticSpawn();
            RaidEnemyPool.ReleaseAll();
            ClearSelector();
            ClearAutomaticSpawnCache();
            preparedForRaidStart = false;
            spawnCountdownStartedByRouteGuide = false;
            spawnCountdownElapsedByRouteGuide = 0f;
        }

        private void Update()
        {
            if (!preparedForRaidStart || !spawnCountdownStartedByRouteGuide || battle == null || !battle.IsRouteGuidePlaying)
            {
                return;
            }

            float deltaTime = Mathf.Min(Mathf.Max(0f, Time.unscaledDeltaTime), MaxGuideCountdownDeltaTime);
            spawnCountdownElapsedByRouteGuide += deltaTime;
        }

        public void SetPathMode(RaidPathMode mode)
        {
            if (pathMode == mode)
            {
                return;
            }

            pathMode = mode;
            ClearSelector();
        }

        public void ResetPathSelection()
        {
            selector?.Reset();
        }

        public bool TrySpawn(EnemyDataSO data, int entryNodeId, out RaidSpawnInfo spawn)
        {
            spawn = default;

            if (!CanSpawn(data, entryNodeId))
            {
                return false;
            }

            EnsureSelector();

            if (!selector.TrySelect(entryNodeId, out int pathIndex))
            {
                return false;
            }

            if (pathIndex < 0 || pathIndex >= board.TravelPaths.Count)
            {
                throw new InvalidOperationException($"선택된 Path Index가 범위를 벗어났습니다. Path: {pathIndex}");
            }

            RaidTravelPath travelPath = board.TravelPaths[pathIndex];

            if (travelPath == null || travelPath.EntryNodeId != entryNodeId)
            {
                throw new InvalidOperationException($"선택된 Path의 Entry가 요청 Entry와 일치하지 않습니다. Entry: {entryNodeId}, Path: {pathIndex}");
            }

            RaidRouteNode entryNode = board.RouteGraph.GetNode(entryNodeId);
            Vector3 spawnPosition = board.Board.TileToWorld(entryNode.Coordinate);
            GameObject instance = RaidEnemyPool.Get(data.EnemyPrefab, spawnPosition, data.EnemyPrefab.transform.rotation, enemyRoot);
            EnemyRuntimeState state = instance != null ? instance.GetComponent<EnemyRuntimeState>() : null;

            if (!ValidateSpawnedEnemy(state, data))
            {
                RaidEnemyPool.Release(instance);
                return false;
            }

            ApplyRaidMoveSpeed(state, data);

            RaidBattleConfigSO config = battle.Config;
            bool isAir = data.MovementType == EnemyMovementType.Air;
            float airFlightHeight = config != null ? config.AirFlightHeight : 0f;
            int airVariantCount = config != null ? config.AirCorridorVariantCount : 1;
            int airVariant = isAir ? (AirSpawnCount + pathIndex) % Mathf.Max(1, airVariantCount) : -1;
            float airLateralOffsetTiles = config != null ? config.AirCorridorLateralOffsetTiles : 2.4f;
            float airNodeSpacingTiles = config != null ? config.AirCorridorNodeSpacingTiles : 1.25f;

            if (!board.EnemyPaths.ApplyTo(pathIndex, state.Move, data.MovementType, airFlightHeight, airVariant, airVariantCount, airLateralOffsetTiles, airNodeSpacingTiles))
            {
                RaidEnemyPool.Release(instance);
                return false;
            }

            RebindAndValidateEnemyPassives(state, data);
            cleanupManager?.RegisterEnemy(state);

            SpawnCount++;
            LastSpawnedEnemyId = data.EnemyId;
            LastSpawnWasAir = isAir;
            LastAirCorridorVariant = airVariant;

            if (LastSpawnWasAir)
            {
                AirSpawnCount++;
            }

            spawn = new RaidSpawnInfo(state, entryNodeId, pathIndex);
            return true;
        }

        private void HandleRaidPreparing()
        {
            SpawnCount = 0;
            AutomaticSpawnCount = 0;
            AutomaticBurstCount = 0;
            AirSpawnCount = 0;
            SpeedAdjustedSpawnCount = 0;
            FailedAutomaticSpawnCount = 0;
            LastBaseMoveSpeed = 0f;
            LastRaidMoveSpeed = 0f;
            LastSpawnedEnemyId = string.Empty;
            LastSpawnWasAir = false;
            LastAirCorridorVariant = -1;
            PassiveValidationFailureCount = 0;
            LastPassiveAssignedCount = 0;
            LastPassiveAppliedCount = 0;
            LastPassiveRejectedCount = 0;
            LastPassiveUnsupportedCount = 0;
            automaticEntryCursor = 0;
            automaticEnemyCursor = 0;
            ClearSelector();
            StopAutomaticSpawn();
            RefreshAutomaticSpawnCache(true);
            spawnCountdownStartedByRouteGuide = battle != null && battle.IsRouteGuidePlaying;
            spawnCountdownElapsedByRouteGuide = 0f;
            preparedForRaidStart = true;
        }

        private void HandleRaidStarted()
        {
            if (!preparedForRaidStart)
            {
                HandleRaidPreparing();
            }

            float routeGuideCountdownElapsed = spawnCountdownStartedByRouteGuide
                ? Mathf.Max(0f, spawnCountdownElapsedByRouteGuide)
                : 0f;

            preparedForRaidStart = false;
            spawnCountdownStartedByRouteGuide = false;
            spawnCountdownElapsedByRouteGuide = 0f;
            automaticRaidStartTime = Time.time;
            StopAutomaticSpawn();

            RaidBattleConfigSO config = battle.Config;

            if (config != null && config.EnableAutomaticSpawn)
            {
                RaidSpawnRhythmSO rhythm = config.SpawnRhythm;
                float initialWaitSeconds = rhythm != null ? rhythm.StartDelaySeconds : Mathf.Max(0f, config.SpawnStartDelay - routeGuideCountdownElapsed);
                automaticSpawnRoutine = StartCoroutine(RunAutomaticSpawnLoop(initialWaitSeconds));
            }
        }

        private void HandleRaidEnded(RaidBattleResult result)
        {
            preparedForRaidStart = false;
            spawnCountdownStartedByRouteGuide = false;
            spawnCountdownElapsedByRouteGuide = 0f;
            StopAutomaticSpawn();
            RaidEnemyPool.ReleaseAll();
        }

        private void HandlePhaseTransitionCompleted(RaidPhaseTransitionInfo info)
        {
            ClearSelector();
            RefreshAutomaticSpawnCache(true);
        }

        private IEnumerator RunAutomaticSpawnLoop(float initialWaitSeconds)
        {
            RaidBattleConfigSO config = battle.Config;

            if (config == null)
            {
                automaticSpawnRoutine = null;
                yield break;
            }

            if (config.SpawnRhythm != null)
            {
                yield return RunBeatSpawnLoop(config, config.SpawnRhythm, initialWaitSeconds);
                automaticSpawnRoutine = null;
                yield break;
            }

            float waitSeconds = Mathf.Max(0f, initialWaitSeconds);

            while (battle != null && (battle.State == RaidBattleState.Running || battle.State == RaidBattleState.Transitioning))
            {
                if (!battle.IsRunning)
                {
                    yield return null;
                    continue;
                }

                if (waitSeconds > 0f)
                {
                    waitSeconds = Mathf.Max(0f, waitSeconds - Time.deltaTime);
                    yield return null;
                    continue;
                }

                if (!RefreshAutomaticSpawnCache(false))
                {
                    waitSeconds = 0.5f;
                    yield return null;
                    continue;
                }

                int spawnCountBeforeBurst = AutomaticSpawnCount;
                yield return RunAutomaticSpawnBurst(config);

                bool spawnedAny = AutomaticSpawnCount > spawnCountBeforeBurst;
                waitSeconds = spawnedAny ? GetAutomaticSpawnInterval(config, board.Phase) : 0.5f;
                yield return null;
            }

            automaticSpawnRoutine = null;
        }

        private IEnumerator RunBeatSpawnLoop(RaidBattleConfigSO config, RaidSpawnRhythmSO rhythm, float initialWaitSeconds)
        {
            float beatSeconds = rhythm.SecondsPerBeat;
            float beatCountdown = Mathf.Max(0f, initialWaitSeconds);
            RaidPhase rhythmPhase = board.Phase;
            int beatIndex = 0;

            while (battle != null && (battle.State == RaidBattleState.Running || battle.State == RaidBattleState.Transitioning))
            {
                if (!battle.IsRunning)
                {
                    yield return null;
                    continue;
                }

                if (board.Phase != rhythmPhase)
                {
                    rhythmPhase = board.Phase;
                    beatIndex = 0;
                    beatCountdown = 0f;
                }

                if (!RefreshAutomaticSpawnCache(false))
                {
                    yield return null;
                    continue;
                }

                beatCountdown -= Time.deltaTime;
                int processedBeatCount = 0;

                while (beatCountdown <= 0f && processedBeatCount < 4)
                {
                    int requestedSpawnCount = rhythm.GetSpawnCount(rhythmPhase, beatIndex);

                    if (requestedSpawnCount > 0)
                    {
                        SpawnAutomaticRhythmPulse(config, rhythmPhase, requestedSpawnCount);
                    }

                    beatIndex++;
                    beatCountdown += beatSeconds;
                    processedBeatCount++;
                }

                if (beatCountdown <= 0f)
                {
                    beatCountdown = beatSeconds;
                }

                yield return null;
            }
        }

        private void SpawnAutomaticRhythmPulse(RaidBattleConfigSO config, RaidPhase phase, int requestedSpawnCount)
        {
            if (requestedSpawnCount <= 0 || automaticEntryIds.Count == 0 || automaticEnemyPool.Count == 0)
            {
                return;
            }

            int maxActiveEnemies = config.GetMaxActiveEnemies(phase);
            int availableSlots = maxActiveEnemies - CombatRegistry.EnemyCount;
            int spawnTarget = Mathf.Min(requestedSpawnCount, availableSlots);

            if (spawnTarget <= 0)
            {
                return;
            }

            int spawnedInPulse = 0;

            for (int i = 0; i < spawnTarget; i++)
            {
                if (battle == null || !battle.IsRunning || board.Phase != phase || CombatRegistry.EnemyCount >= maxActiveEnemies)
                {
                    break;
                }

                if (TrySpawnAutomaticEnemy(phase))
                {
                    spawnedInPulse++;
                }
            }

            if (spawnedInPulse > 0)
            {
                AutomaticBurstCount++;
            }
        }

        private bool TrySpawnAutomaticEnemy(RaidPhase phase)
        {
            if (automaticEntryIds.Count == 0 || automaticEnemyPool.Count == 0)
            {
                return false;
            }

            int entryNodeId = automaticEntryIds[automaticEntryCursor % automaticEntryIds.Count];
            EnemyDataSO data = automaticEnemyPool[automaticEnemyCursor % automaticEnemyPool.Count];
            automaticEntryCursor++;
            automaticEnemyCursor++;

            if (TrySpawn(data, entryNodeId, out _))
            {
                AutomaticSpawnCount++;
                return true;
            }

            FailedAutomaticSpawnCount++;
            Debug.LogWarning($"자동 Raid Spawn에 실패했습니다. Phase: {phase}, Enemy: {data.EnemyId}, Entry: {entryNodeId}", this);
            return false;
        }

        private float GetAutomaticRaidElapsed()
        {
            return Mathf.Max(0f, Time.time - automaticRaidStartTime);
        }

        private bool IsPhase1OpeningRamp(RaidBattleConfigSO config, RaidPhase phase)
        {
            return config != null &&
                   phase == RaidPhase.Phase1 &&
                   GetAutomaticRaidElapsed() < config.Phase1OpeningDuration;
        }

        private float GetAutomaticSpawnInterval(RaidBattleConfigSO config, RaidPhase phase)
        {
            return IsPhase1OpeningRamp(config, phase)
                ? config.Phase1OpeningSpawnInterval
                : config.GetSpawnInterval(phase);
        }

        private IEnumerator RunAutomaticSpawnBurst(RaidBattleConfigSO config)
        {
            if (config == null || automaticEntryIds.Count == 0 || automaticEnemyPool.Count == 0)
            {
                yield break;
            }

            RaidPhase burstPhase = board.Phase;
            int maxActiveEnemies = config.GetMaxActiveEnemies(burstPhase);
            int availableSlots = maxActiveEnemies - CombatRegistry.EnemyCount;

            if (availableSlots <= 0)
            {
                yield break;
            }

            bool openingRamp = IsPhase1OpeningRamp(config, burstPhase);
            int requestedSpawnCount = openingRamp ? config.Phase1OpeningSpawnPerPulse : config.GetSpawnPerPulse(burstPhase);
            int spawnTarget = Mathf.Min(requestedSpawnCount, availableSlots);
            float spacing = openingRamp ? 0f : config.GetSpawnSpacing(burstPhase);
            int spawnedInBurst = 0;

            for (int i = 0; i < spawnTarget; i++)
            {
                if (battle == null || !battle.IsRunning || board.Phase != burstPhase || CombatRegistry.EnemyCount >= maxActiveEnemies)
                {
                    break;
                }

                if (TrySpawnAutomaticEnemy(burstPhase))
                {
                    spawnedInBurst++;
                }

                if (i >= spawnTarget - 1 || spacing <= 0f)
                {
                    continue;
                }

                float elapsed = 0f;

                while (elapsed < spacing)
                {
                    if (battle == null || !battle.IsRunning || board.Phase != burstPhase)
                    {
                        break;
                    }

                    elapsed += Time.deltaTime;
                    yield return null;
                }

                if (battle == null || !battle.IsRunning || board.Phase != burstPhase)
                {
                    break;
                }
            }

            if (spawnedInBurst > 0)
            {
                AutomaticBurstCount++;
            }
        }

        private bool RefreshAutomaticSpawnCache(bool force)
        {
            if (battle == null || board == null || board.RouteGraph == null || battle.Config == null)
            {
                return false;
            }

            RaidRouteGraph graph = board.RouteGraph;
            RaidPhase phase = board.Phase;

            if (!force && ReferenceEquals(automaticSpawnGraph, graph) && automaticSpawnPhase == phase && automaticEntryIds.Count > 0 && automaticEnemyPool.Count > 0)
            {
                return true;
            }

            automaticSpawnGraph = graph;
            automaticSpawnPhase = phase;
            automaticEntryIds.Clear();
            automaticEnemyPool.Clear();
            automaticEntryCursor = 0;
            automaticEnemyCursor = 0;

            for (int nodeId = 0; nodeId < graph.NodeCount; nodeId++)
            {
                if (graph.GetNode(nodeId).Type == RaidRouteNodeType.Entry)
                {
                    automaticEntryIds.Add(nodeId);
                }
            }

            RaidBattleConfigSO config = battle.Config;
            EnemyCatalog catalog = config.EnemyCatalog;
            string[] enemyIds = config.GetSpawnEnemyIds(phase);

            if (catalog == null)
            {
                Debug.LogError("RaidBattleConfig에 EnemyCatalog가 연결되지 않아 자동 Spawn을 시작할 수 없습니다.", this);
                return false;
            }

            if (enemyIds != null)
            {
                for (int i = 0; i < enemyIds.Length; i++)
                {
                    string enemyId = enemyIds[i];

                    if (catalog.TryGetById(enemyId, out EnemyDataSO data) && data != null && data.EnemyPrefab != null)
                    {
                        automaticEnemyPool.Add(data);
                    }
                    else
                    {
                        Debug.LogWarning($"Raid Spawn Pool의 Enemy ID를 찾지 못했습니다. Phase: {phase}, Enemy: {enemyId}", this);
                    }
                }
            }

            if (automaticEntryIds.Count == 0)
            {
                Debug.LogError($"자동 Spawn에 사용할 Entry가 없습니다. Phase: {phase}", this);
                return false;
            }

            if (automaticEnemyPool.Count == 0)
            {
                Debug.LogError($"자동 Spawn에 사용할 몬스터가 없습니다. Phase: {phase}", this);
                return false;
            }

            PrewarmAutomaticEnemyPool();
            return true;
        }

        private void PrewarmAutomaticEnemyPool()
        {
            if (enemyRoot == null || automaticEnemyPool.Count == 0 || battle == null || battle.Config == null)
            {
                return;
            }

            int maxActiveEnemies = battle.Config.GetMaxActiveEnemies(board.Phase);
            int perTypeCount = Mathf.Clamp(Mathf.CeilToInt((float)maxActiveEnemies / automaticEnemyPool.Count), 1, MaxPrewarmCountPerEnemyType);

            for (int i = 0; i < automaticEnemyPool.Count; i++)
            {
                EnemyDataSO data = automaticEnemyPool[i];
                if (data != null && data.EnemyPrefab != null)
                {
                    RaidEnemyPool.Prewarm(data.EnemyPrefab, perTypeCount, enemyRoot);
                }
            }
        }

        private void RebindAndValidateEnemyPassives(EnemyRuntimeState state, EnemyDataSO data)
        {
            if (state == null || data == null || state.Passives == null)
            {
                PassiveValidationFailureCount++;
                return;
            }

            state.Passives.Initialize(state, data.Passives);

            int expected = data.Passives != null ? data.Passives.Count : 0;
            LastPassiveAssignedCount = state.Passives.AssignedPassiveCount;
            LastPassiveAppliedCount = state.Passives.AppliedPassiveCount;
            LastPassiveRejectedCount = state.Passives.RejectedPassiveCount;
            LastPassiveUnsupportedCount = state.Passives.UnsupportedPassiveCount;

            if (LastPassiveAssignedCount == expected && LastPassiveAppliedCount == expected && LastPassiveRejectedCount == 0 && LastPassiveUnsupportedCount == 0)
            {
                return;
            }

            PassiveValidationFailureCount++;
            Debug.LogError(
                $"Raid Enemy Passive 연결 실패: {data.EnemyId} / Expected {expected} / Assigned {LastPassiveAssignedCount} / Applied {LastPassiveAppliedCount} / Rejected {LastPassiveRejectedCount} / Unsupported {LastPassiveUnsupportedCount}",
                state);
        }

        private void ApplyRaidMoveSpeed(EnemyRuntimeState state, EnemyDataSO data)
        {
            if (state == null || state.Stats == null || !state.Stats.IsInitialized || data == null || data.BaseStats == null)
            {
                return;
            }

            RaidBattleConfigSO config = battle != null ? battle.Config : null;
            float baseMoveSpeed = Mathf.Max(0f, data.BaseStats.MoveSpeed);
            float raidMoveSpeed = config != null ? config.GetRaidMoveSpeed(baseMoveSpeed, board.Phase, data.MovementType) : baseMoveSpeed;

            LastBaseMoveSpeed = baseMoveSpeed;
            state.Stats.SetMoveSpeed(raidMoveSpeed);
            LastRaidMoveSpeed = state.Stats.MoveSpeed;

            if (!Mathf.Approximately(baseMoveSpeed, raidMoveSpeed))
            {
                SpeedAdjustedSpawnCount++;
            }
        }

        private bool CanSpawn(EnemyDataSO data, int entryNodeId)
        {
            if (battle == null || !battle.IsRunning || board == null || board.Board == null || board.RouteGraph == null || board.EnemyPaths == null || enemyRoot == null)
            {
                return false;
            }

            if (data == null || data.EnemyPrefab == null)
            {
                return false;
            }

            if (entryNodeId < 0 || entryNodeId >= board.RouteGraph.NodeCount)
            {
                return false;
            }

            return board.RouteGraph.GetNode(entryNodeId).Type == RaidRouteNodeType.Entry;
        }

        private void EnsureSelector()
        {
            RaidRouteGraph graph = board.RouteGraph;
            int key = GetStablePathKey(board.MapId);
            int routePlanCount = board.RoutePlans.Count;

            if (routePlanCount < 1)
            {
                throw new InvalidOperationException("사용 가능한 Raid Route Plan이 없습니다.");
            }

            if (selector != null && ReferenceEquals(selectorGraph, graph) && selectorKey == key && selectorRoutePlanCount == routePlanCount && selectorMode == pathMode)
            {
                return;
            }

            int strategyKeyCount = checked(graph.NodeCount + routePlanCount);
            IRaidPathStrategy strategy = CreateStrategy(pathMode, strategyKeyCount, key);

            selector = new RaidPathSelector(graph, board.TravelPaths, routePlanCount, strategy);
            selectorGraph = graph;
            selectorKey = key;
            selectorRoutePlanCount = routePlanCount;
            selectorMode = pathMode;
        }

        private static IRaidPathStrategy CreateStrategy(RaidPathMode mode, int keyCount, int key)
        {
            switch (mode)
            {
                case RaidPathMode.RoundRobin:
                    return new RaidRoundRobinStrategy(keyCount);
                case RaidPathMode.Random:
                    return new RaidRandomStrategy(key, keyCount);
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, "지원하지 않는 Raid Path 선택 방식입니다.");
            }
        }

        private static int GetStablePathKey(string mapId)
        {
            if (string.IsNullOrEmpty(mapId))
            {
                return 0;
            }

            unchecked
            {
                uint hash = 2166136261u;

                for (int i = 0; i < mapId.Length; i++)
                {
                    hash ^= mapId[i];
                    hash *= 16777619u;
                }

                return (int)hash;
            }
        }

        private static bool ValidateSpawnedEnemy(EnemyRuntimeState state, EnemyDataSO data)
        {
            return state != null && state.IsInitialized && state.DataLink != null && state.DataLink.HasData && state.DataLink.EnemyData == data && state.Move != null;
        }

        private void StopAutomaticSpawn()
        {
            if (automaticSpawnRoutine == null)
            {
                return;
            }

            StopCoroutine(automaticSpawnRoutine);
            automaticSpawnRoutine = null;
        }

        private void ClearAutomaticSpawnCache()
        {
            automaticSpawnGraph = null;
            automaticEntryIds.Clear();
            automaticEnemyPool.Clear();
            automaticEntryCursor = 0;
            automaticEnemyCursor = 0;
        }

        private void ClearSelector()
        {
            selector = null;
            selectorGraph = null;
            selectorKey = 0;
            selectorRoutePlanCount = 0;
        }
    }
}
