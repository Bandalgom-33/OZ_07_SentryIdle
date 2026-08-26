using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Raid.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    [DisallowMultipleComponent]
    public sealed class RaidBoardRuntime : MonoBehaviour
    {
        [Header("데이터")]
        [SerializeField] private RaidMapConfigSO config;

        [Header("참조")]
        [SerializeField] private Transform boardRoot;
        [SerializeField] private RaidBoardView boardView;

        [Header("시작 단계")]
        [SerializeField] private RaidPhase phase = RaidPhase.Phase1;

        private RaidMapResult map;
        private RaidMapFamilySO selectedFamily;
        private RaidMapSO currentMapData;
        private string startupFamilyId = string.Empty;
        private bool startupRandomFamily;

        public RaidBoard Board { get; private set; }
        public RaidPhase Phase => phase;
        public RaidRouteGraph RouteGraph => map != null ? map.RouteGraph : null;
        public RaidLaneSet LaneSet => map != null ? map.LaneSet : null;
        public RaidEnemyPathSet EnemyPaths => map != null ? map.EnemyPaths : null;
        public IReadOnlyList<RaidRoutePlan> RoutePlans => map != null ? map.RoutePlans : Array.Empty<RaidRoutePlan>();
        public IReadOnlyList<RaidLanePath> LanePaths => map != null ? map.LanePaths : Array.Empty<RaidLanePath>();
        public IReadOnlyList<RaidLanePlan> LanePlans => map != null ? map.LanePlans : Array.Empty<RaidLanePlan>();
        public IReadOnlyList<RaidTravelPath> TravelPaths => map != null ? map.TravelPaths : Array.Empty<RaidTravelPath>();
        public RaidMapFamilySO Family => selectedFamily;
        public string FamilyId => selectedFamily != null ? selectedFamily.FamilyId : string.Empty;
        public string FamilyName => selectedFamily != null ? selectedFamily.DisplayName : string.Empty;
        public string MapId => map != null ? map.MapId : string.Empty;
        public RaidMapSO CurrentMapData => currentMapData;
        internal RaidBoardView BoardView => boardView;

        private void Start()
        {
            ValidateReferences();
            LoadFamily(ResolveStartupFamily(), phase);
        }

        public void SetStartupFamily(string familyId)
        {
            if (string.IsNullOrWhiteSpace(familyId))
            {
                throw new ArgumentException("시작 Raid Map Family ID가 비어 있습니다.", nameof(familyId));
            }

            startupFamilyId = familyId;
            startupRandomFamily = false;
        }

        public void SetStartupRandomFamily()
        {
            startupFamilyId = string.Empty;
            startupRandomFamily = true;
        }

        public void LoadFamily(string familyId, RaidPhase startPhase = RaidPhase.Phase1)
        {
            ValidateReferences();
            LoadFamily(config.GetFamily(familyId), startPhase);
        }

        public void LoadFamily(RaidMapFamilySO family, RaidPhase startPhase = RaidPhase.Phase1)
        {
            ValidateReferences();

            if (family == null)
            {
                throw new ArgumentNullException(nameof(family));
            }

            if (!family.IsComplete)
            {
                throw new InvalidOperationException($"Raid Map Family에 Phase 1/2/3이 모두 연결되어 있지 않습니다. Family: {family.name}");
            }

            selectedFamily = family;
            BuildPhase(startPhase);
        }

        public bool TryGetMapData(RaidPhase targetPhase, out RaidMapSO mapData)
        {
            mapData = null;
            return selectedFamily != null && selectedFamily.TryGetMap(targetPhase, out mapData);
        }

        public void BuildPhase(RaidPhase nextPhase)
        {
            ValidateReferences();

            if (nextPhase == RaidPhase.Phase1)
            {
                boardView.ClearPersistentEffects();
            }

            if (selectedFamily == null)
            {
                throw new InvalidOperationException("Raid Map Family가 선택되지 않았습니다.");
            }

            if (!selectedFamily.TryGetMap(nextPhase, out RaidMapSO mapData))
            {
                throw new InvalidOperationException($"선택된 Map Family에 해당 Phase가 없습니다. Family: {selectedFamily.FamilyId}, Phase: {nextPhase}");
            }

            phase = nextPhase;
            currentMapData = mapData;
            Board = new RaidBoard(boardRoot, mapData.Width, mapData.Height, config.GetCenteredOrigin(mapData.Width, mapData.Height), config.TileSize);
            map = RaidMapLoader.Load(Board, mapData);
            boardView.Build(Board, currentMapData);
        }

        public void RefreshVisuals()
        {
            if (Board == null || map == null || currentMapData == null)
            {
                throw new InvalidOperationException("Raid Board가 아직 준비되지 않았습니다.");
            }

            boardView.Build(Board, currentMapData);
        }

        private RaidMapFamilySO ResolveStartupFamily()
        {
            if (startupRandomFamily)
            {
                return config.GetRandomFamily();
            }

            if (!string.IsNullOrWhiteSpace(startupFamilyId))
            {
                return config.GetFamily(startupFamilyId);
            }

            return config.DefaultFamily;
        }

        private void ValidateReferences()
        {
            if (config == null)
            {
                throw new InvalidOperationException("Raid Map Config가 연결되지 않았습니다.");
            }

            if (config.DefaultFamily == null)
            {
                throw new InvalidOperationException("Raid Map Config에 Default Family가 연결되지 않았습니다.");
            }

            if (boardRoot == null)
            {
                throw new InvalidOperationException("Board Root가 연결되지 않았습니다.");
            }

            if (boardView == null)
            {
                throw new InvalidOperationException("Raid Board View가 연결되지 않았습니다.");
            }
        }
    }
}
