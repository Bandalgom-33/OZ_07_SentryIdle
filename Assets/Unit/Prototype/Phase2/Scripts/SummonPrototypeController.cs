using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Runtime;
using UnityEngine;

namespace EndlessGuard.Unit.Prototype.Phase2
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CombatLoop))]
    public sealed class SummonPrototypeController : MonoBehaviour
    {
        [Header("소환자 Prefab")]
        [SerializeField] private GameObject unitOwnerPrefab;
        [SerializeField] private GameObject enemyOwnerPrefab;

        [Header("소환물 Prefab")]
        [Tooltip("UnitSummonRuntime이 붙은 캐릭터 소환물 Prefab을 연결합니다.")]
        [SerializeField] private GameObject unitSummonPrefab;
        [Tooltip("EnemySummonRuntime이 붙은 몬스터 소환물 Prefab을 연결합니다.")]
        [SerializeField] private GameObject enemySummonPrefab;
        [Min(1)]
        [SerializeField] private int summonCount = 1;

        [Header("간단 배치")]
        [SerializeField] private Vector3 worldOrigin = Vector3.zero;
        [Min(0.01f)]
        [SerializeField] private float tileWorldSize = 1f;
        [SerializeField] private Vector2Int unitOwnerTile = Vector2Int.zero;
        [SerializeField] private Vector2Int enemyStartTile = new Vector2Int(2, 3);
        [SerializeField] private Vector2Int enemyGoalTile = new Vector2Int(2, -3);
        [SerializeField] private float unitHeight;
        [SerializeField] private float enemyHeight;

        [HideInInspector]
        [SerializeField] private int lastUnitSpawnedCount;
        [HideInInspector]
        [SerializeField] private int lastEnemySpawnedCount;
        [HideInInspector]
        [SerializeField] private int activeUnitSummonCount;
        [HideInInspector]
        [SerializeField] private int activeEnemySummonCount;
        [HideInInspector]
        [TextArea(3, 8)]
        [SerializeField] private string lastMessage;

        private CombatLoop combatLoop;
        private UnitRuntimeState unitOwner;
        private EnemyRuntimeState enemyOwner;

        public UnitRuntimeState UnitOwner => unitOwner;
        public EnemyRuntimeState EnemyOwner => enemyOwner;
        public int LastUnitSpawnedCount => lastUnitSpawnedCount;
        public int LastEnemySpawnedCount => lastEnemySpawnedCount;
        public int ActiveUnitSummonCount => activeUnitSummonCount;
        public int ActiveEnemySummonCount => activeEnemySummonCount;
        public string LastMessage => lastMessage;

        private void Awake()
        {
            combatLoop = GetComponent<CombatLoop>();
        }

        private void OnDisable()
        {
            ResetPrototype();
        }

        public void SpawnOwners()
        {
            ResetPrototype();

            if (!TryGetUnitData(unitOwnerPrefab, out UnitDataSO unitData) || !TryGetEnemyData(enemyOwnerPrefab, out EnemyDataSO enemyData))
            {
                return;
            }

            unitOwner = Phase2PrototypeSpawnUtility.SpawnUnit(unitOwnerPrefab, unitData, transform, ToWorld(unitOwnerTile, unitHeight));

            if (unitOwner != null)
            {
                unitOwner.GridPosition.Initialize(unitOwnerTile, GridFacingDirection.North, CombatTargetLayer.Ground);
            }

            enemyOwner = Phase2PrototypeSpawnUtility.SpawnEnemy(enemyOwnerPrefab, enemyData, transform, ToWorld(enemyStartTile, enemyHeight));

            if (enemyOwner != null && enemyOwner.Move != null)
            {
                enemyOwner.Move.SetPath(BuildStraightPath(enemyStartTile, enemyGoalTile, enemyHeight));
            }

            if (unitOwner == null || enemyOwner == null)
            {
                Fail("소환자 생성에 실패했습니다. 정식 Prefab 구성을 확인하세요.");
                return;
            }

            combatLoop?.StartLoop();
            RefreshCounts();
            lastMessage = "캐릭터/몬스터 소환자 준비 완료";
            Debug.Log(lastMessage, this);
        }

        public void SpawnUnitSummon()
        {
            if (unitOwner == null || unitSummonPrefab == null)
            {
                Fail("캐릭터 소환자와 UnitSummonRuntime Prefab을 먼저 준비하세요.");
                return;
            }

            bool success = SummonService.TrySpawn(new SummonRequest(unitOwner, unitSummonPrefab, summonCount, this), out int spawnedCount);
            lastUnitSpawnedCount = spawnedCount;
            RefreshCounts();
            lastMessage = $"캐릭터 소환 {(success ? "PASS" : "FAIL")}: 요청 {summonCount}, 생성 {spawnedCount}, 활성 {activeUnitSummonCount}";
            Log(success, lastMessage);
        }

        public void SpawnEnemySummon()
        {
            if (enemyOwner == null || enemySummonPrefab == null)
            {
                Fail("몬스터 소환자와 EnemySummonRuntime Prefab을 먼저 준비하세요.");
                return;
            }

            bool success = SummonService.TrySpawn(new SummonRequest(enemyOwner, enemySummonPrefab, summonCount, this), out int spawnedCount);
            lastEnemySpawnedCount = spawnedCount;
            RefreshCounts();
            lastMessage = $"몬스터 소환 {(success ? "PASS" : "FAIL")}: 요청 {summonCount}, 생성 {spawnedCount}, 활성 {activeEnemySummonCount}";
            Log(success, lastMessage);
        }

        public void RefreshCounts()
        {
            activeUnitSummonCount = 0;
            activeEnemySummonCount = 0;

            foreach (UnitRuntimeState unit in CombatRegistry.Units)
            {
                if (unit != null && unit.IsSummon)
                {
                    activeUnitSummonCount++;
                }
            }

            foreach (EnemyRuntimeState enemy in CombatRegistry.Enemies)
            {
                if (enemy != null && enemy.IsSummon)
                {
                    activeEnemySummonCount++;
                }
            }
        }

        public void ReleaseAllSummons()
        {
            List<GameObject> releaseTargets = new List<GameObject>();

            foreach (UnitRuntimeState unit in CombatRegistry.Units)
            {
                if (unit != null && unit.IsSummon)
                {
                    releaseTargets.Add(unit.gameObject);
                }
            }

            foreach (EnemyRuntimeState enemy in CombatRegistry.Enemies)
            {
                if (enemy != null && enemy.IsSummon)
                {
                    releaseTargets.Add(enemy.gameObject);
                }
            }

            for (int i = 0; i < releaseTargets.Count; i++)
            {
                SummonService.Release(releaseTargets[i]);
            }

            RefreshCounts();
            lastMessage = $"소환물 해제 요청: {releaseTargets.Count}개";
            Debug.Log(lastMessage, this);
        }

        public void ResetPrototype()
        {
            ReleaseAllSummons();
            combatLoop?.StopLoop();

            if (unitOwner != null)
            {
                Destroy(unitOwner.gameObject);
            }

            if (enemyOwner != null)
            {
                Destroy(enemyOwner.gameObject);
            }

            unitOwner = null;
            enemyOwner = null;
            lastUnitSpawnedCount = 0;
            lastEnemySpawnedCount = 0;
            activeUnitSummonCount = 0;
            activeEnemySummonCount = 0;
        }

        private bool TryGetUnitData(GameObject prefab, out UnitDataSO data)
        {
            data = null;

            if (prefab == null)
            {
                Fail("캐릭터 소환자 Prefab이 없습니다.");
                return false;
            }

            UnitDataLink link = prefab.GetComponent<UnitDataLink>();
            data = link != null ? link.UnitData : null;

            if (data == null)
            {
                Fail("캐릭터 소환자 Prefab의 UnitDataLink에 데이터가 없습니다.");
                return false;
            }

            return true;
        }

        private bool TryGetEnemyData(GameObject prefab, out EnemyDataSO data)
        {
            data = null;

            if (prefab == null)
            {
                Fail("몬스터 소환자 Prefab이 없습니다.");
                return false;
            }

            EnemyDataLink link = prefab.GetComponent<EnemyDataLink>();
            data = link != null ? link.EnemyData : null;

            if (data == null)
            {
                Fail("몬스터 소환자 Prefab의 EnemyDataLink에 데이터가 없습니다.");
                return false;
            }

            return true;
        }

        private PathNode[] BuildStraightPath(Vector2Int start, Vector2Int goal, float height)
        {
            int xCount = Mathf.Abs(goal.x - start.x);
            int yCount = Mathf.Abs(goal.y - start.y);
            PathNode[] path = new PathNode[xCount + yCount + 1];
            Vector2Int current = start;
            int index = 0;

            path[index++] = new PathNode(ToWorld(current, height), current, GetFacing(current, goal));

            while (current.x != goal.x)
            {
                int step = goal.x > current.x ? 1 : -1;
                current = new Vector2Int(current.x + step, current.y);
                path[index++] = new PathNode(ToWorld(current, height), current, step > 0 ? GridFacingDirection.East : GridFacingDirection.West);
            }

            while (current.y != goal.y)
            {
                int step = goal.y > current.y ? 1 : -1;
                current = new Vector2Int(current.x, current.y + step);
                path[index++] = new PathNode(ToWorld(current, height), current, step > 0 ? GridFacingDirection.North : GridFacingDirection.South);
            }

            return path;
        }

        private GridFacingDirection GetFacing(Vector2Int from, Vector2Int to)
        {
            if (to.x > from.x) return GridFacingDirection.East;
            if (to.x < from.x) return GridFacingDirection.West;
            return to.y >= from.y ? GridFacingDirection.North : GridFacingDirection.South;
        }

        private Vector3 ToWorld(Vector2Int tile, float height)
        {
            return new Vector3(
                worldOrigin.x + tile.x * tileWorldSize,
                worldOrigin.y + height,
                worldOrigin.z + tile.y * tileWorldSize);
        }

        private void Fail(string message)
        {
            lastMessage = message;
            Debug.LogError(message, this);
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
