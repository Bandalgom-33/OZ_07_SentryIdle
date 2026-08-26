using System;
using System.Collections.Generic;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Data
{
    [CreateAssetMenu(fileName = "RaidTileVisualSet", menuName = "Endless Guard/Raid/Tile Visual Set")]
    public sealed class RaidTileVisualSetSO : ScriptableObject
    {
        [Header("표면")]
        [SerializeField] private GameObject[] groundPrefabs = Array.Empty<GameObject>();
        [SerializeField] private GameObject[] highGroundPrefabs = Array.Empty<GameObject>();

        [Header("전술 구조물")]
        [SerializeField] private GameObject[] rubblePrefabs = Array.Empty<GameObject>();
        [SerializeField] private GameObject[] lowWallPrefabs = Array.Empty<GameObject>();
        [SerializeField] private GameObject[] columnPrefabs = Array.Empty<GameObject>();

        [Header("경계")]
        [SerializeField] private GameObject boundaryBasePrefab;
        [SerializeField] private GameObject boundaryWallPrefab;
        [SerializeField] private GameObject boundaryBrokenPrefab;

        [Header("고정 장식")]
        [SerializeField] private GameObject[] fixedFloorPrefabs = Array.Empty<GameObject>();
        [SerializeField] private GameObject fixedBasePrefab;
        [SerializeField] private GameObject fixedCutFloorPrefab;
        [SerializeField] private GameObject fixedCutBasePrefab;
        [SerializeField] private GameObject fixedRailHalfPrefab;
        [SerializeField] private GameObject fixedRailLongPrefab;
        [SerializeField] private GameObject fixedRailEndPrefab;

        [Header("다리")]
        [SerializeField] private GameObject bridgePrefab;

        [Header("균열")]
        [SerializeField] private Material collapseCrackMaterial;
        [SerializeField] private Material collapseScarMaterial;
        [SerializeField] private Material collapseBeamMaterial;

        [Header("표식")]
        [SerializeField] private GameObject[] entryPrefabs = Array.Empty<GameObject>();
        [SerializeField] private GameObject[] goalPrefabs = Array.Empty<GameObject>();

        public GameObject BoundaryBasePrefab => boundaryBasePrefab;
        public GameObject BoundaryWallPrefab => boundaryWallPrefab;
        public GameObject BoundaryBrokenPrefab => boundaryBrokenPrefab;
        public GameObject FixedCutBasePrefab => fixedCutBasePrefab;
        public GameObject BridgePrefab => bridgePrefab;
        public Material CollapseCrackMaterial => collapseCrackMaterial;
        public Material CollapseScarMaterial => collapseScarMaterial;
        public Material CollapseBeamMaterial => collapseBeamMaterial;

        public IReadOnlyList<GameObject> GetSurfacePrefabs(RaidTileSurface surface)
        {
            switch (surface)
            {
                case RaidTileSurface.Ground:
                    return groundPrefabs;
                case RaidTileSurface.HighGround:
                    return highGroundPrefabs;
                default:
                    return Array.Empty<GameObject>();
            }
        }

        public GameObject GetSurfaceVisualPrefab(RaidTileSurfaceVisual visual)
        {
            switch (visual)
            {
                case RaidTileSurfaceVisual.Floor:
                    return GetFixedFloor(0);
                case RaidTileSurfaceVisual.FloorBrokenA:
                    return GetFixedFloor(1);
                case RaidTileSurfaceVisual.FloorBrokenB:
                    return GetFixedFloor(2);
                case RaidTileSurfaceVisual.FloorCut:
                    return fixedCutFloorPrefab;
                default:
                    return null;
            }
        }

        public IReadOnlyList<GameObject> GetBlockPrefabs(RaidTileBlock block)
        {
            switch (block)
            {
                case RaidTileBlock.Rubble:
                    return rubblePrefabs;
                case RaidTileBlock.LowWall:
                    return lowWallPrefabs;
                case RaidTileBlock.Column:
                    return columnPrefabs;
                default:
                    return Array.Empty<GameObject>();
            }
        }

        public GameObject GetDecorPrefab(RaidMapDecorKind kind)
        {
            switch (kind)
            {
                case RaidMapDecorKind.Floor:
                    return GetFixedFloor(0);
                case RaidMapDecorKind.FloorBrokenA:
                    return GetFixedFloor(1);
                case RaidMapDecorKind.FloorBrokenB:
                    return GetFixedFloor(2);
                case RaidMapDecorKind.FloorCut:
                    return fixedCutFloorPrefab;
                case RaidMapDecorKind.Foundation:
                    return fixedBasePrefab;
                case RaidMapDecorKind.FoundationCut:
                    return fixedCutBasePrefab;
                case RaidMapDecorKind.Wall:
                    return boundaryWallPrefab;
                case RaidMapDecorKind.WallBroken:
                    return boundaryBrokenPrefab;
                case RaidMapDecorKind.RailHalf:
                    return fixedRailHalfPrefab;
                case RaidMapDecorKind.RailLong:
                    return fixedRailLongPrefab;
                case RaidMapDecorKind.RailEnd:
                    return fixedRailEndPrefab;
                case RaidMapDecorKind.Rubble:
                    return Get(rubblePrefabs, 0);
                case RaidMapDecorKind.Rocks:
                    return Get(rubblePrefabs, 1);
                default:
                    return null;
            }
        }

        public IReadOnlyList<GameObject> GetMarkerPrefabs(RaidTileMarker marker)
        {
            switch (marker)
            {
                case RaidTileMarker.Entry:
                    return entryPrefabs;
                case RaidTileMarker.Goal:
                    return goalPrefabs;
                default:
                    return Array.Empty<GameObject>();
            }
        }

        private GameObject GetFixedFloor(int index)
        {
            return Get(fixedFloorPrefabs, index);
        }

        private static GameObject Get(IReadOnlyList<GameObject> prefabs, int index)
        {
            return prefabs != null && index >= 0 && index < prefabs.Count ? prefabs[index] : null;
        }
    }
}
