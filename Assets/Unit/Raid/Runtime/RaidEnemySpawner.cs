using System;
using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Runtime;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RaidBattleController))]
    [RequireComponent(typeof(RaidBoardRuntime))]
    public sealed class RaidEnemySpawner : MonoBehaviour
    {
        [Header("생성")]
        [SerializeField] private Transform enemyRoot;

        [Header("경로 선택")]
        [SerializeField] private RaidPathMode pathMode = RaidPathMode.RoundRobin;

        private RaidBattleController battle;
        private RaidBoardRuntime board;
        private RaidPathSelector selector;
        private RaidRouteGraph selectorGraph;
        private int selectorKey;
        private int selectorRoutePlanCount;
        private RaidPathMode selectorMode;

        public RaidPathMode PathMode => pathMode;
        public int SpawnCount { get; private set; }

        private void Awake()
        {
            battle = GetComponent<RaidBattleController>();
            board = GetComponent<RaidBoardRuntime>();
        }

        private void OnEnable()
        {
            battle.OnRaidStarted += HandleRaidStarted;
        }

        private void OnDisable()
        {
            battle.OnRaidStarted -= HandleRaidStarted;
            ClearSelector();
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
            GameObject instance = Instantiate(data.EnemyPrefab, spawnPosition, data.EnemyPrefab.transform.rotation, enemyRoot);
            EnemyRuntimeState state = instance.GetComponent<EnemyRuntimeState>();

            if (!ValidateSpawnedEnemy(state, data) || !board.EnemyPaths.ApplyTo(pathIndex, state.Move))
            {
                instance.SetActive(false);
                Destroy(instance);
                return false;
            }

            SpawnCount++;
            spawn = new RaidSpawnInfo(state, entryNodeId, pathIndex);
            return true;
        }

        private void HandleRaidStarted()
        {
            SpawnCount = 0;
            ClearSelector();
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
            int key = board.PathSelectionKey;
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

        private static bool ValidateSpawnedEnemy(EnemyRuntimeState state, EnemyDataSO data)
        {
            return state != null &&
                   state.IsInitialized &&
                   state.DataLink != null &&
                   state.DataLink.HasData &&
                   state.DataLink.EnemyData == data &&
                   state.Move != null;
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