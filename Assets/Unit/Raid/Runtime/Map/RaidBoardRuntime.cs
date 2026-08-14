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

        public RaidBoard Board { get; private set; }
        public RaidPhase Phase => phase;
        public RaidRouteGraph RouteGraph => map != null ? map.RouteGraph : null;
        public RaidLaneSet LaneSet => map != null ? map.LaneSet : null;
        public RaidEnemyPathSet EnemyPaths => map != null ? map.EnemyPaths : null;
        public IReadOnlyList<RaidRoutePlan> RoutePlans => map != null ? map.RoutePlans : Array.Empty<RaidRoutePlan>();
        public IReadOnlyList<RaidLanePath> LanePaths => map != null ? map.LanePaths : Array.Empty<RaidLanePath>();
        public IReadOnlyList<RaidLanePlan> LanePlans => map != null ? map.LanePlans : Array.Empty<RaidLanePlan>();
        public IReadOnlyList<RaidTravelPath> TravelPaths => map != null ? map.TravelPaths : Array.Empty<RaidTravelPath>();
        public string FamilyId => selectedFamily != null ? selectedFamily.FamilyId : string.Empty;
        public string FamilyName => selectedFamily != null ? selectedFamily.DisplayName : string.Empty;
        public string MapId => map != null ? map.MapId : string.Empty;
        public int PathSelectionKey => map != null ? map.VisualKey : 0;

        private void Start()
        {
            ValidateReferences();
            LoadFamily(config.DefaultFamily, phase);
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

        public void BuildPhase(RaidPhase nextPhase)
        {
            ValidateReferences();

            if (selectedFamily == null)
            {
                throw new InvalidOperationException("Raid Map Family가 선택되지 않았습니다.");
            }

            if (!selectedFamily.TryGetMap(nextPhase, out RaidMapSO mapData))
            {
                throw new InvalidOperationException($"선택된 Map Family에 해당 Phase가 없습니다. Family: {selectedFamily.FamilyId}, Phase: {nextPhase}");
            }

            phase = nextPhase;
            Board = new RaidBoard(boardRoot, mapData.Width, mapData.Height, config.GetCenteredOrigin(mapData.Width, mapData.Height), config.TileSize);
            map = RaidMapLoader.Load(Board, mapData);
            boardView.Build(Board, map.VisualKey);
        }

        public void RefreshVisuals()
        {
            if (Board == null || map == null)
            {
                throw new InvalidOperationException("Raid Board가 아직 준비되지 않았습니다.");
            }

            boardView.Build(Board, map.VisualKey);
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
