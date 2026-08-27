using System;
using System.Collections.Generic;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    internal sealed class RaidTravelPathBuilder
    {
        private const float SamePointSqrDistance = 0.000001f;

        private readonly RaidBoard board;
        private readonly RaidRouteGraph graph;
        private readonly RaidLaneSet laneSet;
        private readonly IReadOnlyList<RaidLanePath> lanePaths;
        private readonly int[] edgeStarts;
        private readonly List<Vector3> pointBuffer;
        private readonly List<RaidTravelPath> resultBuffer;

        public RaidTravelPathBuilder(RaidBoard board, RaidRouteGraph graph, RaidLaneSet laneSet, IReadOnlyList<RaidLanePath> lanePaths)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (graph == null)
            {
                throw new ArgumentNullException(nameof(graph));
            }

            if (laneSet == null)
            {
                throw new ArgumentNullException(nameof(laneSet));
            }

            if (lanePaths == null)
            {
                throw new ArgumentNullException(nameof(lanePaths));
            }

            if (graph.EdgeCount != laneSet.EdgeCount)
            {
                throw new InvalidOperationException("Route Graph와 Lane Set의 Edge 수가 일치하지 않습니다.");
            }

            if (lanePaths.Count != laneSet.LaneCount)
            {
                throw new InvalidOperationException("Lane Path 수가 Lane Set의 전체 Lane 수와 일치하지 않습니다.");
            }

            this.board = board;
            this.graph = graph;
            this.laneSet = laneSet;
            this.lanePaths = lanePaths;

            edgeStarts = new int[graph.EdgeCount + 1];
            pointBuffer = new List<Vector3>();
            resultBuffer = new List<RaidTravelPath>();

            BuildEdgeStarts();
            ValidateLanePaths();
        }

        public RaidTravelPath[] Build(IReadOnlyList<RaidRoutePlan> routePlans, IReadOnlyList<RaidLanePlan> lanePlans)
        {
            if (routePlans == null)
            {
                throw new ArgumentNullException(nameof(routePlans));
            }

            if (lanePlans == null)
            {
                throw new ArgumentNullException(nameof(lanePlans));
            }

            resultBuffer.Clear();

            for (int lanePlanIndex = 0; lanePlanIndex < lanePlans.Count; lanePlanIndex++)
            {
                RaidLanePlan lanePlan = lanePlans[lanePlanIndex];

                if (lanePlan == null)
                {
                    throw new InvalidOperationException($"Lane Plan이 null입니다. Index: {lanePlanIndex}");
                }

                if (lanePlan.RoutePlanIndex < 0 || lanePlan.RoutePlanIndex >= routePlans.Count)
                {
                    throw new InvalidOperationException($"Lane Plan의 Route Plan Index가 범위를 벗어났습니다. Index: {lanePlan.RoutePlanIndex}");
                }

                RaidRoutePlan routePlan = routePlans[lanePlan.RoutePlanIndex];

                if (lanePlan.StepCount != routePlan.EdgeIndices.Count)
                {
                    throw new InvalidOperationException($"Route Plan과 Lane Plan의 Step 수가 일치하지 않습니다. Route: {lanePlan.RoutePlanIndex}");
                }

                pointBuffer.Clear();

                for (int stepIndex = 0; stepIndex < routePlan.EdgeIndices.Count; stepIndex++)
                {
                    int edgeIndex = routePlan.EdgeIndices[stepIndex];
                    int laneIndex = lanePlan.GetLaneIndex(stepIndex);
                    RaidLanePath lanePath = GetLanePath(edgeIndex, laneIndex);

                    if (stepIndex > 0)
                    {
                        int previousEdgeIndex = routePlan.EdgeIndices[stepIndex - 1];
                        RaidRouteEdge previousEdge = graph.Edges[previousEdgeIndex];
                        RaidRouteEdge currentEdge = graph.Edges[edgeIndex];

                        if (previousEdge.ToNode != currentEdge.FromNode)
                        {
                            throw new InvalidOperationException($"Route Plan Edge가 Junction에서 연결되지 않습니다. Step: {stepIndex}");
                        }

                        AppendJunctionBridge(currentEdge.FromNode, lanePath.GetPoint(0));
                    }

                    AppendLanePath(lanePath);
                }

                if (pointBuffer.Count < 2)
                {
                    throw new InvalidOperationException($"Travel Path Waypoint가 부족합니다. Route: {lanePlan.RoutePlanIndex}, Variant: {lanePlan.VariantIndex}");
                }

                resultBuffer.Add(new RaidTravelPath(lanePlan.RoutePlanIndex, lanePlan.VariantIndex, routePlan.EntryNodeId, routePlan.GoalNodeId, pointBuffer));
            }

            return resultBuffer.ToArray();
        }

        private void BuildEdgeStarts()
        {
            int start = 0;

            for (int edgeIndex = 0; edgeIndex < graph.EdgeCount; edgeIndex++)
            {
                edgeStarts[edgeIndex] = start;
                start += laneSet.GetLaneCount(edgeIndex);
            }

            edgeStarts[graph.EdgeCount] = start;
        }

        private void ValidateLanePaths()
        {
            for (int edgeIndex = 0; edgeIndex < graph.EdgeCount; edgeIndex++)
            {
                int laneCount = laneSet.GetLaneCount(edgeIndex);

                for (int laneIndex = 0; laneIndex < laneCount; laneIndex++)
                {
                    RaidLanePath lanePath = GetLanePath(edgeIndex, laneIndex);

                    if (lanePath.EdgeIndex != edgeIndex)
                    {
                        throw new InvalidOperationException($"Lane Path의 Edge Index가 일치하지 않습니다. Expected: {edgeIndex}, Actual: {lanePath.EdgeIndex}");
                    }

                    if (lanePath.LaneIndex != laneIndex)
                    {
                        throw new InvalidOperationException($"Lane Path의 Lane Index가 일치하지 않습니다. Edge: {edgeIndex}, Expected: {laneIndex}, Actual: {lanePath.LaneIndex}");
                    }
                }
            }
        }

        private RaidLanePath GetLanePath(int edgeIndex, int laneIndex)
        {
            if (edgeIndex < 0 || edgeIndex >= graph.EdgeCount)
            {
                throw new ArgumentOutOfRangeException(nameof(edgeIndex));
            }

            int laneCount = laneSet.GetLaneCount(edgeIndex);

            if (laneIndex < 0 || laneIndex >= laneCount)
            {
                throw new ArgumentOutOfRangeException(nameof(laneIndex));
            }

            return lanePaths[edgeStarts[edgeIndex] + laneIndex];
        }

        private void AppendJunctionBridge(int junctionNodeId, Vector3 nextPoint)
        {
            if (pointBuffer.Count == 0)
            {
                return;
            }

            Vector3 previousPoint = pointBuffer[pointBuffer.Count - 1];

            if ((previousPoint - nextPoint).sqrMagnitude <= SamePointSqrDistance)
            {
                return;
            }

            if (IsCardinalConnection(previousPoint, nextPoint))
            {
                return;
            }

            RaidRouteNode junction = graph.GetNode(junctionNodeId);
            Vector3 junctionWorld = board.TileToWorld(junction.Coordinate);
            AppendPoint(junctionWorld);
        }

        private bool IsCardinalConnection(Vector3 from, Vector3 to)
        {
            if (!board.TryWorldToTile(from, out Vector2Int fromTile))
            {
                return false;
            }

            if (!board.TryWorldToTile(to, out Vector2Int toTile))
            {
                return false;
            }

            return fromTile.x == toTile.x || fromTile.y == toTile.y;
        }

        private void AppendLanePath(RaidLanePath lanePath)
        {
            for (int i = 0; i < lanePath.PointCount; i++)
            {
                AppendPoint(lanePath.GetPoint(i));
            }
        }

        private void AppendPoint(Vector3 point)
        {
            if (pointBuffer.Count > 0)
            {
                Vector3 previous = pointBuffer[pointBuffer.Count - 1];

                if ((previous - point).sqrMagnitude <= SamePointSqrDistance)
                {
                    return;
                }
            }

            pointBuffer.Add(point);
        }
    }
}