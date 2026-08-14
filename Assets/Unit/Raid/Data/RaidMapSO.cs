using System;
using System.Collections.Generic;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Data
{
    public enum RaidMapNodeType
    {
        Entry = 0,
        Junction = 1,
        Goal = 2
    }

    [Serializable]
    public sealed class RaidMapNodeData
    {
        [SerializeField] private Vector2Int coordinate;
        [SerializeField] private RaidMapNodeType type;

        public Vector2Int Coordinate => coordinate;
        public RaidMapNodeType Type => type;
    }

    [Serializable]
    public sealed class RaidMapEdgeData
    {
        [SerializeField] private int fromNode;
        [SerializeField] private int toNode;
        [SerializeField] private int width;
        [SerializeField] private RaidRouteEdgeKind kind;
        [SerializeField] private Vector2Int[] centerLine = Array.Empty<Vector2Int>();

        public int FromNode => fromNode;
        public int ToNode => toNode;
        public int Width => width;
        public RaidRouteEdgeKind Kind => kind;
        public IReadOnlyList<Vector2Int> CenterLine => centerLine;
    }

    [Serializable]
    public sealed class RaidMapRouteData
    {
        [SerializeField] private int entryNodeId;
        [SerializeField] private int goalNodeId;
        [SerializeField] private int stepCount;
        [SerializeField] private int[] edgeIndices = Array.Empty<int>();

        public int EntryNodeId => entryNodeId;
        public int GoalNodeId => goalNodeId;
        public int StepCount => stepCount;
        public IReadOnlyList<int> EdgeIndices => edgeIndices;
    }

    [CreateAssetMenu(fileName = "RaidMap", menuName = "Endless Guard/Raid/Map")]
    public sealed class RaidMapSO : ScriptableObject
    {
        [Header("식별")]
        [SerializeField] private string mapId;
        [SerializeField] private RaidPhase phase;
        [SerializeField] private int visualKey;

        [Header("보드")]
        [Min(1)]
        [SerializeField] private int width;

        [Min(1)]
        [SerializeField] private int height;

        [SerializeField] private RaidTile[] tiles = Array.Empty<RaidTile>();

        [Header("경로")]
        [SerializeField] private RaidMapNodeData[] nodes = Array.Empty<RaidMapNodeData>();
        [SerializeField] private RaidMapEdgeData[] edges = Array.Empty<RaidMapEdgeData>();
        [SerializeField] private RaidMapRouteData[] routes = Array.Empty<RaidMapRouteData>();

        public string MapId => mapId;
        public RaidPhase Phase => phase;
        public int VisualKey => visualKey;
        public int Width => width;
        public int Height => height;
        public int TileCount => tiles != null ? tiles.Length : 0;
        public int NodeCount => nodes != null ? nodes.Length : 0;
        public int EdgeCount => edges != null ? edges.Length : 0;
        public int RouteCount => routes != null ? routes.Length : 0;
        public bool HasData => !string.IsNullOrWhiteSpace(mapId) && width > 0 && height > 0 && TileCount == width * height && NodeCount > 0 && EdgeCount > 0 && RouteCount > 0;

        public RaidTile GetTile(int index)
        {
            ValidateIndex(index, TileCount, nameof(index));
            return tiles[index];
        }

        public RaidMapNodeData GetNode(int index)
        {
            ValidateIndex(index, NodeCount, nameof(index));
            return nodes[index];
        }

        public RaidMapEdgeData GetEdge(int index)
        {
            ValidateIndex(index, EdgeCount, nameof(index));
            return edges[index];
        }

        public RaidMapRouteData GetRoute(int index)
        {
            ValidateIndex(index, RouteCount, nameof(index));
            return routes[index];
        }

        private static void ValidateIndex(int index, int count, string paramName)
        {
            if (index < 0 || index >= count)
            {
                throw new ArgumentOutOfRangeException(paramName, index, "Raid Map 데이터 범위를 벗어났습니다.");
            }
        }
    }
}
