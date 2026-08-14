using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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

        [Header("외곽 폐허")]
        [SerializeField] private GameObject[] outerRuinFloorPrefabs = Array.Empty<GameObject>();
        [SerializeField] private GameObject outerRuinBasePrefab;
        [FormerlySerializedAs("boundaryCutFloorPrefab")]
        [SerializeField] private GameObject outerRuinCutFloorPrefab;
        [FormerlySerializedAs("boundaryCutBasePrefab")]
        [SerializeField] private GameObject outerRuinCutBasePrefab;
        [Range(0f, 1f)]
        [SerializeField] private float outerRuinChance = 0.55f;
        [Range(0f, 1f)]
        [SerializeField] private float outerRuinWallChance = 0.55f;
        [Range(0f, 1f)]
        [SerializeField] private float outerRuinBrokenChance = 0.35f;
        [FormerlySerializedAs("boundaryRailPrefab")]
        [SerializeField] private GameObject outerRuinRailHalfPrefab;
        [SerializeField] private GameObject outerRuinRailLongPrefab;
        [SerializeField] private GameObject outerRuinRailEndPrefab;
        [Range(0f, 1f)]
        [SerializeField] private float outerRuinRailChance = 0.45f;

        [Header("다리")]
        [SerializeField] private GameObject bridgePrefab;

        [Header("경로")]
        [SerializeField] private GameObject[] pathPrefabs = Array.Empty<GameObject>();

        [Header("표식")]
        [SerializeField] private GameObject[] entryPrefabs = Array.Empty<GameObject>();
        [SerializeField] private GameObject[] goalPrefabs = Array.Empty<GameObject>();

        public GameObject BoundaryBasePrefab => boundaryBasePrefab;
        public GameObject BoundaryWallPrefab => boundaryWallPrefab;
        public GameObject BoundaryBrokenPrefab => boundaryBrokenPrefab;
        public IReadOnlyList<GameObject> OuterRuinFloorPrefabs => outerRuinFloorPrefabs;
        public GameObject OuterRuinBasePrefab => outerRuinBasePrefab;
        public GameObject OuterRuinCutFloorPrefab => outerRuinCutFloorPrefab;
        public GameObject OuterRuinCutBasePrefab => outerRuinCutBasePrefab;
        public float OuterRuinChance => outerRuinChance;
        public float OuterRuinWallChance => outerRuinWallChance;
        public float OuterRuinBrokenChance => outerRuinBrokenChance;
        public GameObject OuterRuinRailHalfPrefab => outerRuinRailHalfPrefab;
        public GameObject OuterRuinRailLongPrefab => outerRuinRailLongPrefab;
        public GameObject OuterRuinRailEndPrefab => outerRuinRailEndPrefab;
        public float OuterRuinRailChance => outerRuinRailChance;
        public GameObject BridgePrefab => bridgePrefab;

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

        public IReadOnlyList<GameObject> GetRoutePrefabs(RaidTileRoute route)
        {
            switch (route)
            {
                case RaidTileRoute.Path:
                    return pathPrefabs;
                default:
                    return Array.Empty<GameObject>();
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
    }
}
