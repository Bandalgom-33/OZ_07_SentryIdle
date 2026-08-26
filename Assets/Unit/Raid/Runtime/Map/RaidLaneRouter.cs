using System;
using System.Collections.Generic;

namespace EndlessGuard.Unit.Raid.Runtime
{
    internal sealed class RaidLaneRouter
    {
        private readonly RaidRouteGraph graph;
        private readonly RaidLaneSet laneSet;
        private readonly List<RaidLanePlan> resultBuffer;

        public RaidLaneRouter(RaidRouteGraph graph, RaidLaneSet laneSet)
        {
            if (graph == null)
            {
                throw new ArgumentNullException(nameof(graph));
            }

            if (laneSet == null)
            {
                throw new ArgumentNullException(nameof(laneSet));
            }

            if (graph.EdgeCount != laneSet.EdgeCount)
            {
                throw new InvalidOperationException("Route Graph와 Lane Set의 Edge 수가 일치하지 않습니다.");
            }

            this.graph = graph;
            this.laneSet = laneSet;
            resultBuffer = new List<RaidLanePlan>();
        }

        public RaidLanePlan[] Build(IReadOnlyList<RaidRoutePlan> routePlans)
        {
            if (routePlans == null)
            {
                throw new ArgumentNullException(nameof(routePlans));
            }

            resultBuffer.Clear();

            for (int routePlanIndex = 0; routePlanIndex < routePlans.Count; routePlanIndex++)
            {
                RaidRoutePlan routePlan = routePlans[routePlanIndex];

                if (routePlan == null)
                {
                    throw new InvalidOperationException($"Route Plan이 null입니다. Index: {routePlanIndex}");
                }

                ValidateRoutePlan(routePlan);

                int variantCount = GetVariantCount(routePlan);

                for (int variantIndex = 0; variantIndex < variantCount; variantIndex++)
                {
                    int[] laneIndices = new int[routePlan.EdgeIndices.Count];

                    for (int stepIndex = 0; stepIndex < routePlan.EdgeIndices.Count; stepIndex++)
                    {
                        int edgeIndex = routePlan.EdgeIndices[stepIndex];
                        int laneCount = laneSet.GetLaneCount(edgeIndex);
                        laneIndices[stepIndex] = MapLaneIndex(variantIndex, variantCount, laneCount);
                    }

                    resultBuffer.Add(new RaidLanePlan(routePlanIndex, variantIndex, variantCount, laneIndices));
                }
            }

            return resultBuffer.ToArray();
        }

        private void ValidateRoutePlan(RaidRoutePlan routePlan)
        {
            IReadOnlyList<int> edgeIndices = routePlan.EdgeIndices;

            if (edgeIndices.Count == 0)
            {
                throw new InvalidOperationException("Route Plan에 Edge가 없습니다.");
            }

            RaidRouteEdge firstEdge = GetEdge(edgeIndices[0]);

            if (firstEdge.FromNode != routePlan.EntryNodeId)
            {
                throw new InvalidOperationException("Route Plan의 첫 Edge가 Entry Node에서 시작하지 않습니다.");
            }

            for (int i = 1; i < edgeIndices.Count; i++)
            {
                RaidRouteEdge previousEdge = GetEdge(edgeIndices[i - 1]);
                RaidRouteEdge currentEdge = GetEdge(edgeIndices[i]);

                if (previousEdge.ToNode != currentEdge.FromNode)
                {
                    throw new InvalidOperationException($"Route Plan의 Edge가 Junction에서 연결되지 않습니다. Step: {i}");
                }
            }

            RaidRouteEdge lastEdge = GetEdge(edgeIndices[edgeIndices.Count - 1]);

            if (lastEdge.ToNode != routePlan.GoalNodeId)
            {
                throw new InvalidOperationException("Route Plan의 마지막 Edge가 Goal Node에서 끝나지 않습니다.");
            }
        }

        private int GetVariantCount(RaidRoutePlan routePlan)
        {
            int variantCount = 1;

            for (int i = 0; i < routePlan.EdgeIndices.Count; i++)
            {
                int edgeIndex = routePlan.EdgeIndices[i];
                int laneCount = laneSet.GetLaneCount(edgeIndex);

                if (laneCount > variantCount)
                {
                    variantCount = laneCount;
                }
            }

            return variantCount;
        }

        private static int MapLaneIndex(int variantIndex, int variantCount, int laneCount)
        {
            if (laneCount == 1)
            {
                return 0;
            }

            if (variantCount == 1)
            {
                return (laneCount - 1) / 2;
            }

            double normalized = (double)variantIndex / (variantCount - 1);
            double target = normalized * (laneCount - 1);
            int laneIndex = (int)Math.Round(target, MidpointRounding.AwayFromZero);
            return Math.Clamp(laneIndex, 0, laneCount - 1);
        }

        private RaidRouteEdge GetEdge(int edgeIndex)
        {
            if (edgeIndex < 0 || edgeIndex >= graph.EdgeCount)
            {
                throw new InvalidOperationException($"Route Plan에 존재하지 않는 Edge가 있습니다. Edge: {edgeIndex}");
            }

            return graph.Edges[edgeIndex];
        }
    }
}