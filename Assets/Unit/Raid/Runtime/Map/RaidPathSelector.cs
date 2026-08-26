using System;
using System.Collections.Generic;

namespace EndlessGuard.Unit.Raid.Runtime
{
    internal sealed class RaidPathSelector
    {
        private readonly RaidRouteGraph graph;
        private readonly IReadOnlyList<RaidTravelPath> paths;
        private readonly IRaidPathStrategy strategy;
        private readonly int routePlanCount;

        private readonly int[] pathCounts;

        private readonly int[] routeStarts;
        private readonly int[] routeCounts;
        private readonly int[] routeIndices;

        private readonly int[] laneStarts;
        private readonly int[] laneCounts;
        private readonly int[] laneIndices;

        private readonly int[] representatives;

        public RaidPathSelector(RaidRouteGraph graph, IReadOnlyList<RaidTravelPath> paths, int routePlanCount, IRaidPathStrategy strategy)
        {
            if (graph == null)
            {
                throw new ArgumentNullException(nameof(graph));
            }

            if (paths == null)
            {
                throw new ArgumentNullException(nameof(paths));
            }

            if (routePlanCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(routePlanCount));
            }

            if (strategy == null)
            {
                throw new ArgumentNullException(nameof(strategy));
            }

            this.graph = graph;
            this.paths = paths;
            this.routePlanCount = routePlanCount;
            this.strategy = strategy;

            pathCounts = new int[graph.NodeCount];

            routeStarts = new int[graph.NodeCount + 1];
            routeCounts = new int[graph.NodeCount];
            routeIndices = new int[routePlanCount];

            laneStarts = new int[routePlanCount + 1];
            laneCounts = new int[routePlanCount];
            laneIndices = new int[paths.Count];

            representatives = new int[routePlanCount];

            for (int i = 0; i < representatives.Length; i++)
            {
                representatives[i] = -1;
            }

            InspectPaths();
            ValidateRoutePlans();
            BuildStarts();
            FillRouteIndices();
            FillLaneIndices();
            ValidateEntries();
        }

        public int GetPathCount(int entryNodeId)
        {
            ValidateEntry(entryNodeId);
            return pathCounts[entryNodeId];
        }

        public bool TrySelect(int entryNodeId, out int pathIndex)
        {
            ValidateEntry(entryNodeId);

            int routeCount = routeCounts[entryNodeId];

            if (routeCount == 0)
            {
                pathIndex = -1;
                return false;
            }

            RaidPathCandidates routeCandidates = new RaidPathCandidates(entryNodeId, routeIndices, routeStarts[entryNodeId], routeCount, paths);
            int routeCandidateIndex = SelectCandidate(in routeCandidates, "Route");
            int representativePathIndex = routeCandidates.GetIndex(routeCandidateIndex);
            RaidTravelPath representativePath = paths[representativePathIndex];
            int routePlanIndex = representativePath.RoutePlanIndex;

            int laneCount = laneCounts[routePlanIndex];

            if (laneCount == 0)
            {
                throw new InvalidOperationException($"선택된 Route Plan에 Lane Path가 없습니다. Route: {routePlanIndex}");
            }

            int laneKey = checked(graph.NodeCount + routePlanIndex);
            RaidPathCandidates laneCandidates = new RaidPathCandidates(laneKey, laneIndices, laneStarts[routePlanIndex], laneCount, paths);
            int laneCandidateIndex = SelectCandidate(in laneCandidates, "Lane");
            pathIndex = laneCandidates.GetIndex(laneCandidateIndex);

            RaidTravelPath selectedPath = paths[pathIndex];

            if (selectedPath.EntryNodeId != entryNodeId)
            {
                throw new InvalidOperationException($"선택된 Path의 Entry가 요청 Entry와 일치하지 않습니다. Entry: {entryNodeId}, Path: {pathIndex}");
            }

            if (selectedPath.RoutePlanIndex != routePlanIndex)
            {
                throw new InvalidOperationException($"선택된 Path의 Route Plan이 일치하지 않습니다. Route: {routePlanIndex}, Path: {pathIndex}");
            }

            return true;
        }

        public void Reset()
        {
            strategy.Reset();
        }

        private void InspectPaths()
        {
            for (int pathIndex = 0; pathIndex < paths.Count; pathIndex++)
            {
                RaidTravelPath path = paths[pathIndex];

                if (path == null)
                {
                    throw new InvalidOperationException($"Travel Path가 null입니다. Index: {pathIndex}");
                }

                ValidateNode(path.EntryNodeId);
                ValidateNode(path.GoalNodeId);

                if (graph.GetNode(path.EntryNodeId).Type != RaidRouteNodeType.Entry)
                {
                    throw new InvalidOperationException($"Travel Path 시작 Node가 Entry가 아닙니다. Path: {pathIndex}");
                }

                if (graph.GetNode(path.GoalNodeId).Type != RaidRouteNodeType.Goal)
                {
                    throw new InvalidOperationException($"Travel Path 도착 Node가 Goal이 아닙니다. Path: {pathIndex}");
                }

                if (path.RoutePlanIndex < 0 || path.RoutePlanIndex >= routePlanCount)
                {
                    throw new InvalidOperationException($"Travel Path의 Route Plan Index가 범위를 벗어났습니다. Path: {pathIndex}, Route: {path.RoutePlanIndex}");
                }

                pathCounts[path.EntryNodeId]++;
                laneCounts[path.RoutePlanIndex]++;

                int representativeIndex = representatives[path.RoutePlanIndex];

                if (representativeIndex < 0)
                {
                    representatives[path.RoutePlanIndex] = pathIndex;
                    routeCounts[path.EntryNodeId]++;
                    continue;
                }

                RaidTravelPath representative = paths[representativeIndex];

                if (representative.EntryNodeId != path.EntryNodeId || representative.GoalNodeId != path.GoalNodeId)
                {
                    throw new InvalidOperationException($"같은 Route Plan의 Travel Path가 서로 다른 Entry 또는 Goal을 가리킵니다. Route: {path.RoutePlanIndex}");
                }
            }
        }

        private void ValidateRoutePlans()
        {
            for (int routePlanIndex = 0; routePlanIndex < representatives.Length; routePlanIndex++)
            {
                if (representatives[routePlanIndex] < 0)
                {
                    throw new InvalidOperationException($"Travel Path가 없는 Route Plan이 있습니다. Route: {routePlanIndex}");
                }
            }
        }

        private void BuildStarts()
        {
            int routeStart = 0;

            for (int nodeId = 0; nodeId < graph.NodeCount; nodeId++)
            {
                routeStarts[nodeId] = routeStart;
                routeStart += routeCounts[nodeId];
            }

            routeStarts[graph.NodeCount] = routeStart;

            int laneStart = 0;

            for (int routePlanIndex = 0; routePlanIndex < routePlanCount; routePlanIndex++)
            {
                laneStarts[routePlanIndex] = laneStart;
                laneStart += laneCounts[routePlanIndex];
            }

            laneStarts[routePlanCount] = laneStart;
        }

        private void FillRouteIndices()
        {
            int[] writes = new int[graph.NodeCount];

            for (int nodeId = 0; nodeId < graph.NodeCount; nodeId++)
            {
                writes[nodeId] = routeStarts[nodeId];
            }

            for (int routePlanIndex = 0; routePlanIndex < routePlanCount; routePlanIndex++)
            {
                int representativeIndex = representatives[routePlanIndex];
                int entryNodeId = paths[representativeIndex].EntryNodeId;
                routeIndices[writes[entryNodeId]++] = representativeIndex;
            }
        }

        private void FillLaneIndices()
        {
            int[] writes = new int[routePlanCount];

            for (int routePlanIndex = 0; routePlanIndex < routePlanCount; routePlanIndex++)
            {
                writes[routePlanIndex] = laneStarts[routePlanIndex];
            }

            for (int pathIndex = 0; pathIndex < paths.Count; pathIndex++)
            {
                int routePlanIndex = paths[pathIndex].RoutePlanIndex;
                laneIndices[writes[routePlanIndex]++] = pathIndex;
            }
        }

        private int SelectCandidate(in RaidPathCandidates candidates, string stage)
        {
            int candidateIndex = strategy.Select(in candidates);

            if (candidateIndex < 0 || candidateIndex >= candidates.Count)
            {
                throw new InvalidOperationException($"{stage} Strategy가 잘못된 후보를 반환했습니다. Key: {candidates.Key}, Candidate: {candidateIndex}");
            }

            return candidateIndex;
        }

        private void ValidateEntries()
        {
            for (int nodeId = 0; nodeId < graph.NodeCount; nodeId++)
            {
                RaidRouteNode node = graph.GetNode(nodeId);

                if (node.Type == RaidRouteNodeType.Entry && routeCounts[nodeId] == 0)
                {
                    throw new InvalidOperationException($"사용 가능한 Route가 없는 Entry가 있습니다. Entry: {nodeId}");
                }
            }
        }

        private void ValidateEntry(int entryNodeId)
        {
            ValidateNode(entryNodeId);

            if (graph.GetNode(entryNodeId).Type != RaidRouteNodeType.Entry)
            {
                throw new ArgumentException($"지정한 Node가 Entry가 아닙니다. Node: {entryNodeId}", nameof(entryNodeId));
            }
        }

        private void ValidateNode(int nodeId)
        {
            if (nodeId < 0 || nodeId >= graph.NodeCount)
            {
                throw new ArgumentOutOfRangeException(nameof(nodeId), nodeId, "존재하지 않는 Route Node입니다.");
            }
        }
    }
}