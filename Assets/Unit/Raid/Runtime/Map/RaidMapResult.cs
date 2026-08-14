using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Raid.Data;

namespace EndlessGuard.Unit.Raid.Runtime
{
    public sealed class RaidMapResult
    {
        private readonly RaidRoutePlan[] routePlans;
        private readonly RaidLanePath[] lanePaths;
        private readonly RaidLanePlan[] lanePlans;
        private readonly RaidTravelPath[] travelPaths;

        public RaidPhase Phase { get; }
        public string MapId { get; }
        public int VisualKey { get; }
        public RaidRouteGraph RouteGraph { get; }
        public RaidLaneSet LaneSet { get; }
        public RaidEnemyPathSet EnemyPaths { get; }
        public IReadOnlyList<RaidRoutePlan> RoutePlans => routePlans;
        public IReadOnlyList<RaidLanePath> LanePaths => lanePaths;
        public IReadOnlyList<RaidLanePlan> LanePlans => lanePlans;
        public IReadOnlyList<RaidTravelPath> TravelPaths => travelPaths;

        internal RaidMapResult(RaidPhase phase, string mapId, int visualKey, RaidRouteGraph routeGraph, RaidRoutePlan[] routePlans, RaidLaneSet laneSet, RaidLanePath[] lanePaths, RaidLanePlan[] lanePlans, RaidTravelPath[] travelPaths, RaidEnemyPathSet enemyPaths)
        {
            if (string.IsNullOrWhiteSpace(mapId))
            {
                throw new ArgumentException("Raid Map ID가 비어 있습니다.", nameof(mapId));
            }

            RouteGraph = routeGraph ?? throw new ArgumentNullException(nameof(routeGraph));
            this.routePlans = routePlans ?? throw new ArgumentNullException(nameof(routePlans));
            LaneSet = laneSet ?? throw new ArgumentNullException(nameof(laneSet));
            this.lanePaths = lanePaths ?? throw new ArgumentNullException(nameof(lanePaths));
            this.lanePlans = lanePlans ?? throw new ArgumentNullException(nameof(lanePlans));
            this.travelPaths = travelPaths ?? throw new ArgumentNullException(nameof(travelPaths));
            EnemyPaths = enemyPaths ?? throw new ArgumentNullException(nameof(enemyPaths));

            if (routePlans.Length == 0)
            {
                throw new ArgumentException("Route Plan이 없습니다.", nameof(routePlans));
            }

            if (travelPaths.Length == 0 || travelPaths.Length != enemyPaths.Count)
            {
                throw new ArgumentException("Travel Path와 Enemy Path 구성이 유효하지 않습니다.");
            }

            Phase = phase;
            MapId = mapId;
            VisualKey = visualKey;
        }
    }
}
