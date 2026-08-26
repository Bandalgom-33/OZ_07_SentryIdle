using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Raid.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    public enum RaidRouteNodeType
    {
        Entry = 0,
        Junction = 1,
        Goal = 2
    }

    public readonly struct RaidRouteNode
    {
        public int Id { get; }
        public Vector2Int Coordinate { get; }
        public RaidRouteNodeType Type { get; }

        public RaidRouteNode(int id, Vector2Int coordinate, RaidRouteNodeType type)
        {
            Id = id;
            Coordinate = coordinate;
            Type = type;
        }
    }

    public sealed class RaidRouteEdge
    {
        private readonly Vector2Int[] centerLine;

        public int FromNode { get; }
        public int ToNode { get; }
        public int Width { get; }
        public RaidRouteEdgeKind Kind { get; }
        public IReadOnlyList<Vector2Int> CenterLine => centerLine;

        public RaidRouteEdge(int fromNode, int toNode, int width, IReadOnlyList<Vector2Int> centerLine) : this(fromNode, toNode, width, centerLine, RaidRouteEdgeKind.Normal)
        {
        }

        public RaidRouteEdge(int fromNode, int toNode, int width, IReadOnlyList<Vector2Int> centerLine, RaidRouteEdgeKind kind)
        {
            if (fromNode < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(fromNode));
            }

            if (toNode < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(toNode));
            }

            if (width < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(width), width, "경로 폭은 1 이상이어야 합니다.");
            }

            if (centerLine == null)
            {
                throw new ArgumentNullException(nameof(centerLine));
            }

            if (centerLine.Count == 0)
            {
                throw new ArgumentException("경로 중심선에는 최소 한 개의 좌표가 필요합니다.", nameof(centerLine));
            }

            FromNode = fromNode;
            ToNode = toNode;
            Width = width;
            Kind = kind;
            this.centerLine = new Vector2Int[centerLine.Count];

            for (int i = 0; i < centerLine.Count; i++)
            {
                this.centerLine[i] = centerLine[i];
            }
        }
    }

    public sealed class RaidRouteGraph
    {
        private readonly List<RaidRouteNode> nodes = new List<RaidRouteNode>();
        private readonly List<RaidRouteEdge> edges = new List<RaidRouteEdge>();

        public IReadOnlyList<RaidRouteNode> Nodes => nodes;
        public IReadOnlyList<RaidRouteEdge> Edges => edges;
        public int NodeCount => nodes.Count;
        public int EdgeCount => edges.Count;

        public int AddNode(Vector2Int coordinate, RaidRouteNodeType type)
        {
            int id = nodes.Count;
            nodes.Add(new RaidRouteNode(id, coordinate, type));
            return id;
        }

        public void AddEdge(int fromNode, int toNode, int width, IReadOnlyList<Vector2Int> centerLine)
        {
            AddEdge(fromNode, toNode, width, centerLine, RaidRouteEdgeKind.Normal);
        }

        public void AddEdge(int fromNode, int toNode, int width, IReadOnlyList<Vector2Int> centerLine, RaidRouteEdgeKind kind)
        {
            ValidateNode(fromNode);
            ValidateNode(toNode);
            edges.Add(new RaidRouteEdge(fromNode, toNode, width, centerLine, kind));
        }

        internal bool TryMergeEdge(int fromNode, int toNode, int width, IReadOnlyList<Vector2Int> centerLine)
        {
            return TryMergeEdge(fromNode, toNode, width, centerLine, RaidRouteEdgeKind.Normal);
        }

        internal bool TryMergeEdge(int fromNode, int toNode, int width, IReadOnlyList<Vector2Int> centerLine, RaidRouteEdgeKind kind)
        {
            ValidateNode(fromNode);
            ValidateNode(toNode);

            if (width < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(width), width, "경로 폭은 1 이상이어야 합니다.");
            }

            if (centerLine == null)
            {
                throw new ArgumentNullException(nameof(centerLine));
            }

            if (centerLine.Count == 0)
            {
                throw new ArgumentException("경로 중심선에는 최소 한 개의 좌표가 필요합니다.", nameof(centerLine));
            }

            for (int i = 0; i < edges.Count; i++)
            {
                RaidRouteEdge edge = edges[i];

                if (edge.FromNode != fromNode || edge.ToNode != toNode || edge.Kind != kind)
                {
                    continue;
                }

                if (!SameLine(edge.CenterLine, centerLine))
                {
                    continue;
                }

                if (width > edge.Width)
                {
                    edges[i] = new RaidRouteEdge(fromNode, toNode, width, centerLine, kind);
                }

                return true;
            }

            return false;
        }

        public RaidRouteNode GetNode(int nodeId)
        {
            ValidateNode(nodeId);
            return nodes[nodeId];
        }

        private static bool SameLine(IReadOnlyList<Vector2Int> first, IReadOnlyList<Vector2Int> second)
        {
            if (first.Count != second.Count)
            {
                return false;
            }

            for (int i = 0; i < first.Count; i++)
            {
                if (first[i] != second[i])
                {
                    return false;
                }
            }

            return true;
        }

        private void ValidateNode(int nodeId)
        {
            if (nodeId < 0 || nodeId >= nodes.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(nodeId), nodeId, "존재하지 않는 경로 노드입니다.");
            }
        }
    }
}
