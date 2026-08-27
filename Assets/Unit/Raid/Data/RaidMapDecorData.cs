using System;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Data
{
    public enum RaidMapDecorKind
    {
        [InspectorName("바닥")]
        Floor = 0,

        [InspectorName("파손 바닥 A")]
        FloorBrokenA = 1,

        [InspectorName("파손 바닥 B")]
        FloorBrokenB = 2,

        [InspectorName("절단 바닥")]
        FloorCut = 3,

        [InspectorName("기단")]
        Foundation = 4,

        [InspectorName("절단 기단")]
        FoundationCut = 5,

        [InspectorName("외벽")]
        Wall = 6,

        [InspectorName("파손 외벽")]
        WallBroken = 7,

        [InspectorName("난간 반칸")]
        RailHalf = 8,

        [InspectorName("난간 긴칸")]
        RailLong = 9,

        [InspectorName("난간 끝")]
        RailEnd = 10,

        [InspectorName("잔해")]
        Rubble = 11,

        [InspectorName("작은 바위")]
        Rocks = 12
    }

    [Serializable]
    public struct RaidMapDecorData
    {
        [SerializeField] private RaidMapDecorKind kind;
        [SerializeField] private Vector2Int coordinate;
        [SerializeField] private Vector2 tileOffset;
        [SerializeField] private float heightOffset;
        [SerializeField] private float yaw;
        [Min(0.01f)]
        [SerializeField] private float scale;

        public RaidMapDecorKind Kind => kind;
        public Vector2Int Coordinate => coordinate;
        public Vector2 TileOffset => tileOffset;
        public float HeightOffset => heightOffset;
        public float Yaw => yaw;
        public float Scale => scale > 0f ? scale : 1f;
    }
}
