using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Runtime;
using UnityEngine;

namespace EndlessGuard.Unit.Prototype
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CombatStatePrototypeController))]
    public sealed class MoveTest : MonoBehaviour
    {
        [Header("검증 대상 연결")]
        [SerializeField] private CombatStatePrototypeController state;

        [Header("직선 경로")]
        [SerializeField] private Vector3 worldOrigin;
        [Min(0.01f)]
        [SerializeField] private float tileWorldSize = 1f;
        [SerializeField] private Vector2Int startTile = new Vector2Int(4, 0);
        [SerializeField] private Vector2Int goalTile = new Vector2Int(-4, 0);
        [SerializeField] private float spawnHeight;

        [HideInInspector]
        [SerializeField] private EnemyMove enemyMove;

        [HideInInspector]
        [SerializeField] private bool isRunning;

        [HideInInspector]
        [SerializeField] private bool reachedGoal;

        [HideInInspector]
        [TextArea(2, 4)]
        [SerializeField] private string lastMessage;

        public EnemyMove EnemyMove => enemyMove;
        public bool IsRunning => isRunning;
        public bool ReachedGoal => reachedGoal;
        public string LastMessage => lastMessage;

        private void Reset()
        {
            state = GetComponent<CombatStatePrototypeController>();
        }

        private void OnValidate()
        {
            if (state == null)
            {
                state = GetComponent<CombatStatePrototypeController>();
            }
        }

        private void Update()
        {
            if (isRunning && enemyMove != null)
            {
                enemyMove.Step(Time.deltaTime);
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Setup()
        {
            Unsubscribe();

            if (state == null)
            {
                lastMessage = "CombatStatePrototypeController가 연결되지 않았습니다.";
                Debug.LogError(lastMessage, this);
                return;
            }

            state.SpawnActors();

            if (state.SpawnedEnemy == null)
            {
                lastMessage = "검증할 몬스터가 생성되지 않았습니다.";
                Debug.LogError(lastMessage, this);
                return;
            }

            enemyMove = state.SpawnedEnemy.Move;

            if (enemyMove == null)
            {
                lastMessage = "생성된 몬스터에서 EnemyMove를 찾지 못했습니다.";
                Debug.LogError(lastMessage, state.SpawnedEnemy);
                return;
            }

            PathNode[] path = BuildPath();
            enemyMove.OnGoalReached += HandleGoalReached;
            reachedGoal = false;
            isRunning = false;

            if (!enemyMove.SetPath(path))
            {
                lastMessage = "검증 경로를 설정하지 못했습니다.";
                Debug.LogError(lastMessage, enemyMove);
                return;
            }

            lastMessage = $"이동 검증 준비 완료: 시작 {startTile}, 출구 {goalTile}, 경로 {path.Length}개";
            Debug.Log(lastMessage, this);
        }

        public void StartMove()
        {
            if (enemyMove == null || !enemyMove.HasPath)
            {
                lastMessage = "먼저 이동 검증 준비를 실행하세요.";
                return;
            }

            enemyMove.SetPaused(false);
            isRunning = true;
            lastMessage = "몬스터 이동 시작";
            Debug.Log(lastMessage, enemyMove);
        }

        public void StopMove()
        {
            isRunning = false;

            if (enemyMove != null)
            {
                enemyMove.SetPaused(true);
            }

            lastMessage = "몬스터 이동 정지";
            Debug.Log(lastMessage, this);
        }

        private PathNode[] BuildPath()
        {
            int xCount = Mathf.Abs(goalTile.x - startTile.x);
            int yCount = Mathf.Abs(goalTile.y - startTile.y);
            PathNode[] path = new PathNode[xCount + yCount + 1];
            Vector2Int current = startTile;
            int index = 0;
            GridFacingDirection startFacing = GetFirstFacing();

            path[index++] = CreateNode(current, startFacing);

            while (current.x != goalTile.x)
            {
                int step = goalTile.x > current.x ? 1 : -1;
                current = new Vector2Int(current.x + step, current.y);
                path[index++] = CreateNode(current, step > 0 ? GridFacingDirection.East : GridFacingDirection.West);
            }

            while (current.y != goalTile.y)
            {
                int step = goalTile.y > current.y ? 1 : -1;
                current = new Vector2Int(current.x, current.y + step);
                path[index++] = CreateNode(current, step > 0 ? GridFacingDirection.North : GridFacingDirection.South);
            }

            return path;
        }

        private PathNode CreateNode(Vector2Int tile, GridFacingDirection facing)
        {
            float worldX = worldOrigin.x + tile.x * tileWorldSize;
            float worldY = worldOrigin.y + spawnHeight;
            float worldZ = worldOrigin.z + tile.y * tileWorldSize;
            return new PathNode(new Vector3(worldX, worldY, worldZ), tile, facing);
        }

        private GridFacingDirection GetFirstFacing()
        {
            if (goalTile.x > startTile.x)
            {
                return GridFacingDirection.East;
            }

            if (goalTile.x < startTile.x)
            {
                return GridFacingDirection.West;
            }

            return goalTile.y >= startTile.y ? GridFacingDirection.North : GridFacingDirection.South;
        }

        private void HandleGoalReached(EnemyMove move)
        {
            isRunning = false;
            reachedGoal = true;
            lastMessage = $"몬스터 출구 도달: 타일 {move.GetComponent<CombatGridPosition>().TileCoordinate}";
            Debug.Log(lastMessage, move);
        }

        private void Unsubscribe()
        {
            if (enemyMove != null)
            {
                enemyMove.OnGoalReached -= HandleGoalReached;
            }

            isRunning = false;
            enemyMove = null;
        }
    }
}