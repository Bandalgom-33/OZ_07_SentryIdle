using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Runtime;
using UnityEngine;

namespace EndlessGuard.Unit.Prototype.Phase2
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CombatLoop))]
    public sealed class GroundMapPrototypeController : MonoBehaviour
    {
        [Header("Prototype Ground Map")]
        [Tooltip("Phase2GroundTile들이 들어 있는 부모를 연결합니다.")]
        [SerializeField] private Transform tileRoot;
        [SerializeField] private Phase2EnemyRoute enemyRoute;

        [Header("정식 Prefab")]
        [SerializeField] private GameObject unitPrefab;
        [SerializeField] private GameObject enemyPrefab;

        [Header("배치 검증 좌표")]
        [SerializeField] private Vector2Int groundPlacementTile;
        [SerializeField] private Vector2Int highGroundPlacementTile = new Vector2Int(1, 0);

        [Header("공중 몬스터")]
        [Min(0.1f)]
        [SerializeField] private float airHeight = 2f;

        [HideInInspector]
        [SerializeField] private int tileCount;
        [HideInInspector]
        [SerializeField] private int groundTileCount;
        [HideInInspector]
        [SerializeField] private int highGroundTileCount;
        [HideInInspector]
        [SerializeField] private bool lastPlacementPassed;
        [HideInInspector]
        [SerializeField] private bool lastGroundRoutePassed;
        [HideInInspector]
        [TextArea(3, 8)]
        [SerializeField] private string lastMessage;

        private readonly Dictionary<Vector2Int, Phase2GroundTile> tileLookup = new Dictionary<Vector2Int, Phase2GroundTile>();
        private readonly List<GameObject> spawnedObjects = new List<GameObject>(8);
        private readonly List<ScriptableObject> runtimeDataObjects = new List<ScriptableObject>(8);

        private CombatLoop combatLoop;

        public int TileCount => tileCount;
        public int GroundTileCount => groundTileCount;
        public int HighGroundTileCount => highGroundTileCount;
        public bool LastPlacementPassed => lastPlacementPassed;
        public bool LastGroundRoutePassed => lastGroundRoutePassed;
        public string LastMessage => lastMessage;

        private void Awake()
        {
            combatLoop = GetComponent<CombatLoop>();
        }

        private void OnDisable()
        {
            ResetActors();
        }

        public bool RefreshAndValidateMap()
        {
            tileLookup.Clear();
            tileCount = 0;
            groundTileCount = 0;
            highGroundTileCount = 0;

            if (tileRoot == null)
            {
                lastMessage = "Tile Root가 연결되지 않았습니다.";
                Debug.LogError(lastMessage, this);
                return false;
            }

            Phase2GroundTile[] tiles = tileRoot.GetComponentsInChildren<Phase2GroundTile>(true);

            for (int i = 0; i < tiles.Length; i++)
            {
                Phase2GroundTile tile = tiles[i];

                if (tile == null)
                {
                    continue;
                }

                if (tileLookup.ContainsKey(tile.Coordinate))
                {
                    lastMessage = $"중복 타일 좌표가 있습니다: {tile.Coordinate}";
                    Debug.LogError(lastMessage, tile);
                    return false;
                }

                tileLookup.Add(tile.Coordinate, tile);
                tileCount++;

                if (tile.Surface == Phase2TileSurface.HighGround)
                {
                    highGroundTileCount++;
                }
                else
                {
                    groundTileCount++;
                }
            }

            string routeMessage = "EnemyRoute 없음";
            bool routePassed = enemyRoute != null && enemyRoute.ValidateGroundRoute(out routeMessage);
            lastGroundRoutePassed = routePassed;
            bool passed = tileCount > 0 && groundTileCount > 0 && highGroundTileCount > 0 && routePassed;

            lastMessage = passed
                ? $"Ground Map PASS: 전체 {tileCount}, Ground {groundTileCount}, HighGround {highGroundTileCount}. {routeMessage}"
                : $"Ground Map FAIL: 전체 {tileCount}, Ground {groundTileCount}, HighGround {highGroundTileCount}. {routeMessage}";

            if (passed)
            {
                Debug.Log(lastMessage, this);
            }
            else
            {
                Debug.LogWarning(lastMessage, this);
            }

            return passed;
        }

        public void TestGroundUnitOnGround()
        {
            lastPlacementPassed = TrySpawnPlacementUnit(UnitPlacement.Ground, groundPlacementTile, true);
        }

        public void TestGroundUnitOnHighGroundShouldFail()
        {
            TestRejectedPlacement(UnitPlacement.Ground, highGroundPlacementTile, "지상 전용 캐릭터의 HighGround 배치");
        }

        public void TestHighGroundUnitOnHighGround()
        {
            lastPlacementPassed = TrySpawnPlacementUnit(UnitPlacement.HighGround, highGroundPlacementTile, true);
        }

        public void TestHighGroundUnitOnGroundShouldFail()
        {
            TestRejectedPlacement(UnitPlacement.HighGround, groundPlacementTile, "언덕 전용 캐릭터의 Ground 배치");
        }

        public void SpawnGroundEnemy()
        {
            if (!EnsureMapReady() || !TryGetEnemySource(out EnemyDataSO source) || !enemyRoute.BuildGroundPath(out PathNode[] path))
            {
                return;
            }

            EnemyDataSO runtimeData = Phase2PrototypeDataFactory.CloneEnemyData(source, null, null, 0f, null, EnemyMovementType.Ground);
            runtimeDataObjects.Add(runtimeData);

            EnemyRuntimeState enemy = Phase2PrototypeSpawnUtility.SpawnEnemy(enemyPrefab, runtimeData, transform, path[0].Position);

            if (enemy == null || enemy.Move == null || !enemy.Move.SetPath(path))
            {
                lastMessage = "지상 몬스터 생성/경로 설정에 실패했습니다.";
                Debug.LogError(lastMessage, this);
                return;
            }

            spawnedObjects.Add(enemy.gameObject);
            combatLoop?.StartLoop();
            lastMessage = $"지상 몬스터 생성 PASS: Ground 경로 {path.Length}개 노드를 따라 이동합니다.";
            Debug.Log(lastMessage, enemy);
        }

        public void SpawnAirEnemy()
        {
            if (!EnsureMapReady() || !TryGetEnemySource(out EnemyDataSO source) || !enemyRoute.BuildAirPath(airHeight, out PathNode[] path))
            {
                return;
            }

            EnemyDataSO runtimeData = Phase2PrototypeDataFactory.CloneEnemyData(source, null, null, 0f, null, EnemyMovementType.Air);
            runtimeDataObjects.Add(runtimeData);

            EnemyRuntimeState enemy = Phase2PrototypeSpawnUtility.SpawnEnemy(enemyPrefab, runtimeData, transform, path[0].Position);

            if (enemy == null || enemy.Move == null || !enemy.Move.SetPath(path))
            {
                lastMessage = "공중 몬스터 생성/경로 설정에 실패했습니다.";
                Debug.LogError(lastMessage, this);
                return;
            }

            spawnedObjects.Add(enemy.gameObject);
            combatLoop?.StartLoop();
            lastMessage = "공중 몬스터 생성 PASS: 중간 지형을 사용하지 않고 시작점->출구 직선 경로로 이동합니다.";
            Debug.Log(lastMessage, enemy);
        }

        public void StartCombat()
        {
            combatLoop?.StartLoop();
            lastMessage = "Ground Map 통합 전투 시작";
            Debug.Log(lastMessage, this);
        }

        public void StopCombat()
        {
            combatLoop?.StopLoop();
            lastMessage = "Ground Map 통합 전투 정지";
            Debug.Log(lastMessage, this);
        }

        public void ResetActors()
        {
            combatLoop?.StopLoop();

            for (int i = spawnedObjects.Count - 1; i >= 0; i--)
            {
                if (spawnedObjects[i] != null)
                {
                    Destroy(spawnedObjects[i]);
                }
            }

            for (int i = runtimeDataObjects.Count - 1; i >= 0; i--)
            {
                if (runtimeDataObjects[i] != null)
                {
                    Destroy(runtimeDataObjects[i]);
                }
            }

            spawnedObjects.Clear();
            runtimeDataObjects.Clear();
        }

        private void TestRejectedPlacement(UnitPlacement placement, Vector2Int coordinate, string label)
        {
            if (!EnsureMapReady())
            {
                lastPlacementPassed = false;
                return;
            }

            if (!tileLookup.TryGetValue(coordinate, out Phase2GroundTile tile))
            {
                lastPlacementPassed = false;
                lastMessage = $"배치 검증 타일 {coordinate}를 찾지 못했습니다.";
                Debug.LogError(lastMessage, this);
                return;
            }

            lastPlacementPassed = !CanPlace(placement, tile.Surface);
            lastMessage = lastPlacementPassed ? $"PASS: {label}를 정상 거부했습니다." : $"FAIL: {label}가 허용되었습니다.";
            Log(lastPlacementPassed, lastMessage);
        }

        private bool TrySpawnPlacementUnit(UnitPlacement placement, Vector2Int coordinate, bool spawnWhenAllowed)
        {
            if (!EnsureMapReady() || !TryGetUnitSource(out UnitDataSO source))
            {
                return false;
            }

            if (!tileLookup.TryGetValue(coordinate, out Phase2GroundTile tile))
            {
                lastMessage = $"배치 검증 타일 {coordinate}를 찾지 못했습니다.";
                Debug.LogError(lastMessage, this);
                return false;
            }

            bool allowed = CanPlace(placement, tile.Surface);

            if (!allowed || !spawnWhenAllowed)
            {
                lastMessage = allowed ? $"배치 규칙상 허용됨: {placement} -> {tile.Surface} {coordinate}" : $"배치 규칙상 거부됨: {placement} -> {tile.Surface} {coordinate}";
                Debug.Log(lastMessage, this);
                return allowed;
            }

            UnitDataSO runtimeData = Phase2PrototypeDataFactory.CloneUnitData(source, null, null, placement);
            runtimeDataObjects.Add(runtimeData);

            UnitRuntimeState unit = Phase2PrototypeSpawnUtility.SpawnUnit(unitPrefab, runtimeData, transform, tile.WorldPosition);

            if (unit == null)
            {
                lastMessage = "배치 검증 캐릭터 생성에 실패했습니다.";
                Debug.LogError(lastMessage, this);
                return false;
            }

            unit.GridPosition.Initialize(tile.Coordinate, GridFacingDirection.North, CombatTargetLayer.Ground);
            spawnedObjects.Add(unit.gameObject);
            lastMessage = $"배치 PASS: {placement} 캐릭터 -> {tile.Surface} {coordinate}";
            Debug.Log(lastMessage, unit);
            return true;
        }

        private bool EnsureMapReady()
        {
            return tileLookup.Count > 0 || RefreshAndValidateMap();
        }

        private bool TryGetUnitSource(out UnitDataSO data)
        {
            data = null;

            if (unitPrefab == null)
            {
                lastMessage = "Unit Prefab이 연결되지 않았습니다.";
                Debug.LogError(lastMessage, this);
                return false;
            }

            UnitDataLink link = unitPrefab.GetComponent<UnitDataLink>();
            data = link != null ? link.UnitData : null;

            if (data == null)
            {
                lastMessage = "Unit Prefab의 UnitDataLink에 데이터가 없습니다.";
                Debug.LogError(lastMessage, this);
                return false;
            }

            return true;
        }

        private bool TryGetEnemySource(out EnemyDataSO data)
        {
            data = null;

            if (enemyPrefab == null)
            {
                lastMessage = "Enemy Prefab이 연결되지 않았습니다.";
                Debug.LogError(lastMessage, this);
                return false;
            }

            EnemyDataLink link = enemyPrefab.GetComponent<EnemyDataLink>();
            data = link != null ? link.EnemyData : null;

            if (data == null)
            {
                lastMessage = "Enemy Prefab의 EnemyDataLink에 데이터가 없습니다.";
                Debug.LogError(lastMessage, this);
                return false;
            }

            return true;
        }

        private static bool CanPlace(UnitPlacement placement, Phase2TileSurface surface)
        {
            switch (placement)
            {
                case UnitPlacement.Ground:
                    return surface == Phase2TileSurface.Ground;

                case UnitPlacement.HighGround:
                    return surface == Phase2TileSurface.HighGround;

                case UnitPlacement.GroundAndHighGround:
                    return true;

                default:
                    return false;
            }
        }

        private void Log(bool success, string message)
        {
            if (success)
            {
                Debug.Log(message, this);
            }
            else
            {
                Debug.LogWarning(message, this);
            }
        }
    }
}