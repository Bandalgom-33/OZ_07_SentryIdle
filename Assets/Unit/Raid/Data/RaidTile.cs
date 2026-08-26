using System;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Data
{
    public enum RaidTileSurface
    {
        [InspectorName("미설정")]
        None = 0,

        [InspectorName("지상")]
        Ground = 1,

        [InspectorName("언덕")]
        HighGround = 2,

        [InspectorName("비전투")]
        Void = 4
    }

    public enum RaidTileSurfaceVisual
    {
        [InspectorName("자동")]
        Auto = 0,

        [InspectorName("일반 바닥")]
        Floor = 1,

        [InspectorName("파손 바닥 A")]
        FloorBrokenA = 2,

        [InspectorName("파손 바닥 B")]
        FloorBrokenB = 3,

        [InspectorName("절단 바닥")]
        FloorCut = 4
    }

    public enum RaidTileRoute
    {
        [InspectorName("없음")]
        None = 0,

        [InspectorName("경로")]
        Path = 1
    }

    public enum RaidTileDeploy
    {
        [InspectorName("배치 불가")]
        None = 0,

        [InspectorName("지상")]
        Ground = 1,

        [InspectorName("언덕")]
        HighGround = 2
    }

    public enum RaidTileMarker
    {
        [InspectorName("없음")]
        None = 0,

        [InspectorName("입구")]
        Entry = 1,

        [InspectorName("목표")]
        Goal = 2
    }

    public enum RaidTileBridge
    {
        [InspectorName("없음")]
        None = 0,

        [InspectorName("가로")]
        Horizontal = 1,

        [InspectorName("세로")]
        Vertical = 2
    }

    public enum RaidTileBlock
    {
        [InspectorName("없음")]
        None = 0,

        [InspectorName("잔해")]
        Rubble = 1,

        [InspectorName("낮은 벽")]
        LowWall = 2,

        [InspectorName("기둥")]
        Column = 3
    }

    public enum RaidTileRotation
    {
        [InspectorName("0°")]
        North = 0,

        [InspectorName("90°")]
        East = 1,

        [InspectorName("180°")]
        South = 2,

        [InspectorName("270°")]
        West = 3
    }

    [Serializable]
    public struct RaidTile
    {
        [SerializeField] private RaidTileSurface surface;
        [SerializeField] private RaidTileRoute route;
        [SerializeField] private RaidTileDeploy deploy;
        [SerializeField] private RaidTileMarker marker;
        [SerializeField] private RaidTileBridge bridge;
        [SerializeField] private RaidTileBlock block;
        [SerializeField] private RaidTileRotation blockRotation;
        [SerializeField] private RaidTileSurfaceVisual surfaceVisual;
        [SerializeField] private RaidTileRotation surfaceRotation;

        public RaidTileSurface Surface => surface;
        public RaidTileRoute Route => route;
        public RaidTileDeploy Deploy => deploy;
        public RaidTileMarker Marker => marker;
        public RaidTileBridge Bridge => bridge;
        public RaidTileBlock Block => block;
        public RaidTileRotation BlockRotation => blockRotation;
        public RaidTileSurfaceVisual SurfaceVisual => surfaceVisual;
        public RaidTileRotation SurfaceRotation => surfaceRotation;

        public bool IsPath => route == RaidTileRoute.Path;
        public bool IsEntry => marker == RaidTileMarker.Entry;
        public bool IsGoal => marker == RaidTileMarker.Goal;
        public bool IsBridge => bridge != RaidTileBridge.None;
        public bool HasBlock => block != RaidTileBlock.None;
        public bool IsGroundDeployable => deploy == RaidTileDeploy.Ground && marker == RaidTileMarker.None && !HasBlock;
        public bool IsGroundCombatDeployable => surface == RaidTileSurface.Ground && marker == RaidTileMarker.None && !HasBlock && (deploy == RaidTileDeploy.Ground || route == RaidTileRoute.Path);
        public bool IsHighGroundDeployable => deploy == RaidTileDeploy.HighGround && marker == RaidTileMarker.None && !HasBlock;

        public RaidTile(RaidTileSurface surface, RaidTileRoute route, RaidTileDeploy deploy, RaidTileMarker marker) : this(surface, route, deploy, marker, RaidTileBridge.None, RaidTileBlock.None, RaidTileRotation.North)
        {
        }

        public RaidTile(RaidTileSurface surface, RaidTileRoute route, RaidTileDeploy deploy, RaidTileMarker marker, RaidTileBridge bridge, RaidTileBlock block, RaidTileRotation blockRotation)
        {
            this.surface = surface;
            this.route = route;
            this.deploy = deploy;
            this.marker = marker;
            this.bridge = bridge;
            this.block = block;
            this.blockRotation = blockRotation;
            surfaceVisual = RaidTileSurfaceVisual.Auto;
            surfaceRotation = RaidTileRotation.North;
        }
    }
}
