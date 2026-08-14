using System;
using EndlessGuard.Unit.Raid.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    internal static class RaidMapLoader
    {
        public static RaidMapResult Load(RaidBoard board, RaidMapSO mapData)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (mapData == null)
            {
                throw new ArgumentNullException(nameof(mapData));
            }

            if (!mapData.HasData)
            {
                throw new InvalidOperationException($"Raid Map 데이터가 유효하지 않습니다. Map: {mapData.name}");
            }

            if (board.Width != mapData.Width || board.Height != mapData.Height || board.Count != mapData.TileCount)
            {
                throw new InvalidOperationException($"Raid Board 크기와 Map 크기가 다릅니다. Board: {board.Width}x{board.Height}, Map: {mapData.Width}x{mapData.Height}");
            }

            ApplyTiles(board, mapData);
            RaidRouteGraph graph = BuildGraph(mapData);
            RaidRoutePlan[] routePlans = BuildRoutes(mapData);
            RaidLaneSet laneSet = new RaidLaneSet(graph);
            RaidLaneBuilder laneBuilder = new RaidLaneBuilder(board, laneSet);
            RaidLanePath[] lanePaths = laneBuilder.Build(graph);
            RaidLaneRouter laneRouter = new RaidLaneRouter(graph, laneSet);
            RaidLanePlan[] lanePlans = laneRouter.Build(routePlans);
            RaidTravelPathBuilder travelBuilder = new RaidTravelPathBuilder(board, graph, laneSet, lanePaths);
            RaidTravelPath[] travelPaths = travelBuilder.Build(routePlans, lanePlans);

            if (!RaidEnemyPathSet.TryCreate(board, graph, travelPaths, out RaidEnemyPathSet enemyPaths))
            {
                throw new InvalidOperationException($"Raid Map에서 Enemy Path를 복원하지 못했습니다. Map: {mapData.MapId}");
            }

            return new RaidMapResult(mapData.Phase, mapData.MapId, mapData.VisualKey, graph, routePlans, laneSet, lanePaths, lanePlans, travelPaths, enemyPaths);
        }

        private static void ApplyTiles(RaidBoard board, RaidMapSO mapData)
        {
            int index = 0;

            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    board.SetTile(new Vector2Int(x, y), mapData.GetTile(index++));
                }
            }
        }

        private static RaidRouteGraph BuildGraph(RaidMapSO mapData)
        {
            RaidRouteGraph graph = new RaidRouteGraph();

            for (int i = 0; i < mapData.NodeCount; i++)
            {
                RaidMapNodeData node = mapData.GetNode(i);
                int nodeId = graph.AddNode(node.Coordinate, ToRuntimeType(node.Type));

                if (nodeId != i)
                {
                    throw new InvalidOperationException($"Raid Route Node 순서가 깨졌습니다. Expected: {i}, Actual: {nodeId}");
                }
            }

            for (int i = 0; i < mapData.EdgeCount; i++)
            {
                RaidMapEdgeData edge = mapData.GetEdge(i);
                graph.AddEdge(edge.FromNode, edge.ToNode, edge.Width, edge.CenterLine, edge.Kind);
            }

            return graph;
        }

        private static RaidRoutePlan[] BuildRoutes(RaidMapSO mapData)
        {
            RaidRoutePlan[] routes = new RaidRoutePlan[mapData.RouteCount];

            for (int i = 0; i < routes.Length; i++)
            {
                RaidMapRouteData route = mapData.GetRoute(i);
                routes[i] = new RaidRoutePlan(route.EntryNodeId, route.GoalNodeId, route.EdgeIndices, route.StepCount);
            }

            return routes;
        }

        private static RaidRouteNodeType ToRuntimeType(RaidMapNodeType type)
        {
            switch (type)
            {
                case RaidMapNodeType.Entry:
                    return RaidRouteNodeType.Entry;
                case RaidMapNodeType.Junction:
                    return RaidRouteNodeType.Junction;
                case RaidMapNodeType.Goal:
                    return RaidRouteNodeType.Goal;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, "지원하지 않는 Raid Route Node Type입니다.");
            }
        }
    }
}
