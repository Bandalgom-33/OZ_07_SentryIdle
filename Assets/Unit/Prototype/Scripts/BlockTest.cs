using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Runtime;
using UnityEngine;

namespace EndlessGuard.Unit.Prototype
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CombatStatePrototypeController))]
    public sealed class BlockTest : MonoBehaviour
    {
        private const int AutoEnemyCount = 3;

        [Header("검증 대상 연결")]
        [Tooltip("캐릭터와 첫 번째 몬스터를 생성하는 기존 검증 컴포넌트입니다.")]
        [SerializeField] private CombatStatePrototypeController state;

        [Header("추가 몬스터 배치")]
        [Tooltip("두 번째 몬스터가 첫 번째 몬스터로부터 떨어질 월드 위치입니다.")]
        [SerializeField] private Vector3 secondEnemyOffset = new Vector3(1.5f, 0f, 0f);

        [Tooltip("세 번째 몬스터가 첫 번째 몬스터로부터 떨어질 월드 위치입니다.")]
        [SerializeField] private Vector3 thirdEnemyOffset = new Vector3(3f, 0f, 0f);

        [Header("자동 이동 경로")]
        [Tooltip("격자 좌표 (0, 0)의 월드 기준 위치입니다.")]
        [SerializeField] private Vector3 worldOrigin;

        [Tooltip("격자 한 칸의 월드 크기입니다.")]
        [Min(0.01f)]
        [SerializeField] private float tileWorldSize = 1f;

        [Tooltip("첫 번째 몬스터의 시작 타일입니다.")]
        [SerializeField] private Vector2Int firstStartTile = new Vector2Int(4, 0);

        [Tooltip("몬스터가 이동할 출구 타일입니다.")]
        [SerializeField] private Vector2Int goalTile = new Vector2Int(-4, 0);

        [Tooltip("뒤에 생성되는 몬스터 사이의 타일 간격입니다.")]
        [Min(1)]
        [SerializeField] private int enemySpacingTiles = 1;

        [Tooltip("몬스터가 생성될 월드 Y 위치입니다.")]
        [SerializeField] private float spawnHeight;

        [HideInInspector]
        [SerializeField] private UnitBlock unitBlock;

        [HideInInspector]
        [SerializeField] private EnemyBlock firstBlock;

        [HideInInspector]
        [SerializeField] private EnemyBlock secondBlock;

        [HideInInspector]
        [SerializeField] private EnemyBlock thirdBlock;

        [HideInInspector]
        [SerializeField] private GameObject secondEnemyObject;

        [HideInInspector]
        [SerializeField] private GameObject thirdEnemyObject;

        [HideInInspector]
        [SerializeField] private bool lastResult;

        [HideInInspector]
        [TextArea(2, 4)]
        [SerializeField] private string lastMessage;

        [HideInInspector]
        [SerializeField] private EnemyMove[] autoMoves = new EnemyMove[AutoEnemyCount];

        [HideInInspector]
        [SerializeField] private bool[] autoReachedGoals = new bool[AutoEnemyCount];

        [HideInInspector]
        [SerializeField] private bool isAutoMoveRunning;

        [HideInInspector]
        [SerializeField] private int autoGoalReachedCount;

        [HideInInspector]
        [TextArea(2, 4)]
        [SerializeField] private string autoMoveMessage;

        public UnitBlock UnitBlock => unitBlock;
        public EnemyBlock FirstBlock => firstBlock;
        public EnemyBlock SecondBlock => secondBlock;
        public EnemyBlock ThirdBlock => thirdBlock;
        public bool LastResult => lastResult;
        public string LastMessage => lastMessage;

        public bool IsAutoMoveRunning => isAutoMoveRunning;
        public int AutoGoalReachedCount => autoGoalReachedCount;
        public string AutoMoveMessage => autoMoveMessage;
        public int AutoBlockedCount => unitBlock == null ? 0 : unitBlock.Count;
        public int ExpectedAutoBlockedCount => unitBlock == null ? 0 : Mathf.Min(AutoEnemyCount, unitBlock.MaxCount);
        public int ExpectedAutoGoalCount => Mathf.Max(0, AutoEnemyCount - ExpectedAutoBlockedCount);
        public bool AutoMovePassed => IsAutoMovePassed();

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

            tileWorldSize = Mathf.Max(0.01f, tileWorldSize);
            enemySpacingTiles = Mathf.Max(1, enemySpacingTiles);
        }

        private void Update()
        {
            if (!isAutoMoveRunning)
            {
                return;
            }

            for (int i = 0; i < autoMoves.Length; i++)
            {
                if (autoMoves[i] != null)
                {
                    autoMoves[i].Step(Time.deltaTime);
                }
            }

            CompleteAutoMoveIfSettled();
        }

        private void OnDisable()
        {
            if (Application.isPlaying)
            {
                CleanupExtras();
            }
        }

        public void Setup()
        {
            CleanupExtras();

            if (state == null)
            {
                lastMessage = "CombatStatePrototypeController가 연결되지 않았습니다.";
                Debug.LogError(lastMessage, this);
                return;
            }

            state.SpawnActors();

            if (state.SpawnedUnit == null || state.SpawnedEnemy == null)
            {
                lastMessage = "검증용 캐릭터 또는 첫 번째 몬스터가 생성되지 않았습니다.";
                Debug.LogError(lastMessage, this);
                return;
            }

            unitBlock = state.SpawnedUnit.GetComponent<UnitBlock>();
            firstBlock = state.SpawnedEnemy.GetComponent<EnemyBlock>();

            if (unitBlock == null || firstBlock == null)
            {
                lastMessage = "생성된 프리팹에서 UnitBlock 또는 EnemyBlock을 찾지 못했습니다.";
                Debug.LogError(lastMessage, this);
                return;
            }

            GameObject enemyPrefab = state.SpawnedEnemy.DataLink.EnemyData.EnemyPrefab;

            if (enemyPrefab == null)
            {
                lastMessage = "첫 번째 몬스터 데이터에 연결된 프리팹이 없습니다.";
                Debug.LogError(lastMessage, this);
                return;
            }

            Vector3 firstPosition = state.SpawnedEnemy.transform.position;
            secondEnemyObject = Instantiate(enemyPrefab, firstPosition + secondEnemyOffset, Quaternion.identity, transform);
            thirdEnemyObject = Instantiate(enemyPrefab, firstPosition + thirdEnemyOffset, Quaternion.identity, transform);
            secondBlock = secondEnemyObject.GetComponent<EnemyBlock>();
            thirdBlock = thirdEnemyObject.GetComponent<EnemyBlock>();

            if (secondBlock == null || thirdBlock == null)
            {
                lastMessage = "추가 몬스터에서 EnemyBlock을 찾지 못했습니다.";
                Debug.LogError(lastMessage, this);
                CleanupExtras();
                return;
            }

            lastResult = true;
            lastMessage = $"저지 검증 준비 완료: 최대 {unitBlock.MaxCount}마리, 현재 {unitBlock.Count}마리";
            Debug.Log(lastMessage, this);
        }

        public void SetupAutoMove()
        {
            Setup();

            if (unitBlock == null || firstBlock == null || secondBlock == null || thirdBlock == null)
            {
                autoMoveMessage = "자동 이동 검증 대상을 준비하지 못했습니다.";
                Debug.LogError(autoMoveMessage, this);
                return;
            }

            Vector2Int movementDirection = GetInitialStepDirection();

            if (movementDirection == Vector2Int.zero)
            {
                autoMoveMessage = "시작 타일과 출구 타일이 같아 경로를 만들 수 없습니다.";
                Debug.LogError(autoMoveMessage, this);
                return;
            }

            autoMoves = new[]
            {
                firstBlock.GetComponent<EnemyMove>(),
                secondBlock.GetComponent<EnemyMove>(),
                thirdBlock.GetComponent<EnemyMove>()
            };

            autoReachedGoals = new bool[AutoEnemyCount];
            autoGoalReachedCount = 0;
            isAutoMoveRunning = false;
            CombatEvents.OnEnemyReachedGoal += HandleAutoGoalReached;

            for (int i = 0; i < autoMoves.Length; i++)
            {
                EnemyMove move = autoMoves[i];

                if (move == null)
                {
                    autoMoveMessage = $"{i + 1}번째 몬스터에서 EnemyMove를 찾지 못했습니다.";
                    Debug.LogError(autoMoveMessage, this);
                    ClearAutoMoveState();
                    return;
                }

                Vector2Int startTile = firstStartTile - movementDirection * (i * enemySpacingTiles);
                PathNode[] path = BuildPath(startTile);
                if (!move.SetPath(path))
                {
                    autoMoveMessage = $"{i + 1}번째 몬스터의 경로를 설정하지 못했습니다.";
                    Debug.LogError(autoMoveMessage, move);
                    ClearAutoMoveState();
                    return;
                }

                move.SetPaused(true);
            }

            autoMoveMessage =
                $"자동 이동 준비 완료: 예상 저지 {ExpectedAutoBlockedCount}마리, " +
                $"예상 출구 도달 {ExpectedAutoGoalCount}마리";

            Debug.Log(autoMoveMessage, this);
        }

        public void StartAutoMove()
        {
            if (autoMoves == null || autoMoves.Length != AutoEnemyCount || autoMoves[0] == null)
            {
                autoMoveMessage = "먼저 자동 이동 검증 준비를 실행하세요.";
                return;
            }

            for (int i = 0; i < autoMoves.Length; i++)
            {
                autoMoves[i].SetPaused(false);
            }

            isAutoMoveRunning = true;
            autoMoveMessage = "몬스터 3마리 자동 이동 시작";
            Debug.Log(autoMoveMessage, this);
        }

        public void StopAutoMove()
        {
            isAutoMoveRunning = false;

            for (int i = 0; i < autoMoves.Length; i++)
            {
                if (autoMoves[i] != null)
                {
                    autoMoves[i].SetPaused(true);
                }
            }

            autoMoveMessage = "몬스터 3마리 자동 이동 정지";
            Debug.Log(autoMoveMessage, this);
        }

        public EnemyMove GetAutoMove(int index)
        {
            return autoMoves != null && index >= 0 && index < autoMoves.Length
                ? autoMoves[index]
                : null;
        }

        public bool HasAutoReachedGoal(int index)
        {
            return autoReachedGoals != null &&
                   index >= 0 &&
                   index < autoReachedGoals.Length &&
                   autoReachedGoals[index];
        }

        public void BindFirst()
        {
            Bind(firstBlock, "첫 번째 몬스터");
        }

        public void BindSecond()
        {
            Bind(secondBlock, "두 번째 몬스터");
        }

        public void BindThird()
        {
            Bind(thirdBlock, "세 번째 몬스터");
        }

        public void ReleaseFirst()
        {
            Release(firstBlock, "첫 번째 몬스터");
        }

        public void ReleaseSecond()
        {
            Release(secondBlock, "두 번째 몬스터");
        }

        public void ReleaseThird()
        {
            Release(thirdBlock, "세 번째 몬스터");
        }

        public void ReleaseAll()
        {
            if (unitBlock == null)
            {
                lastMessage = "저지 검증이 준비되지 않았습니다.";
                return;
            }

            unitBlock.ReleaseAll();
            lastResult = true;
            lastMessage = $"전체 저지 해제 완료: 현재 {unitBlock.Count}마리";
            Debug.Log(lastMessage, this);
        }

        public void KillFirst()
        {
            KillEnemy(firstBlock, "첫 번째 몬스터");
        }

        public void KillSecond()
        {
            KillEnemy(secondBlock, "두 번째 몬스터");
        }

        public void KillUnit()
        {
            if (state == null || state.SpawnedUnit == null || state.SpawnedUnit.Health == null)
            {
                lastMessage = "사망시킬 캐릭터가 없습니다.";
                return;
            }

            state.SpawnedUnit.ApplyDamage(state.SpawnedUnit.Health.CurrentHp);
            lastResult = state.SpawnedUnit.Health.IsDead;
            lastMessage = $"캐릭터 사망 처리: 현재 저지 {GetBlockCount()}마리";
            Debug.Log(lastMessage, state.SpawnedUnit);
        }

        public void CleanupExtras()
        {
            ClearAutoMoveState();

            if (unitBlock != null)
            {
                unitBlock.ReleaseAll();
            }

            if (secondEnemyObject != null)
            {
                Destroy(secondEnemyObject);
            }

            if (thirdEnemyObject != null)
            {
                Destroy(thirdEnemyObject);
            }

            unitBlock = null;
            firstBlock = null;
            secondBlock = null;
            thirdBlock = null;
            secondEnemyObject = null;
            thirdEnemyObject = null;
        }

        private PathNode[] BuildPath(Vector2Int startTile)
        {
            int xCount = Mathf.Abs(goalTile.x - startTile.x);
            int yCount = Mathf.Abs(goalTile.y - startTile.y);
            PathNode[] path = new PathNode[xCount + yCount + 1];
            Vector2Int current = startTile;
            int index = 0;

            path[index++] = CreateNode(current, GetFirstFacing(startTile));

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
            Vector3 position = new Vector3(
                worldOrigin.x + tile.x * tileWorldSize,
                worldOrigin.y + spawnHeight,
                worldOrigin.z + tile.y * tileWorldSize);

            return new PathNode(position, tile, facing);
        }

        private GridFacingDirection GetFirstFacing(Vector2Int startTile)
        {
            if (goalTile.x > startTile.x)
            {
                return GridFacingDirection.East;
            }

            if (goalTile.x < startTile.x)
            {
                return GridFacingDirection.West;
            }

            return goalTile.y >= startTile.y
                ? GridFacingDirection.North
                : GridFacingDirection.South;
        }

        private Vector2Int GetInitialStepDirection()
        {
            if (goalTile.x != firstStartTile.x)
            {
                return new Vector2Int(goalTile.x > firstStartTile.x ? 1 : -1, 0);
            }

            if (goalTile.y != firstStartTile.y)
            {
                return new Vector2Int(0, goalTile.y > firstStartTile.y ? 1 : -1);
            }

            return Vector2Int.zero;
        }

        private void HandleAutoGoalReached(EnemyReachedGoalInfo info)
        {
            int index = -1;

            for (int i = 0; i < autoMoves.Length; i++)
            {
                EnemyMove move = autoMoves[i];
                EnemyRuntimeState enemy = move != null ? move.GetComponent<EnemyRuntimeState>() : null;

                if (enemy != null && enemy.RuntimeId == info.RuntimeId)
                {
                    index = i;
                    break;
                }
            }

            if (index < 0 || autoReachedGoals[index])
            {
                return;
            }

            autoReachedGoals[index] = true;
            autoGoalReachedCount++;
            Debug.Log($"{index + 1}번째 몬스터 출구 도달: {info.EnemyId}", this);
        }

        private void CompleteAutoMoveIfSettled()
        {
            if (!AreAutoMovesSettled())
            {
                return;
            }

            isAutoMoveRunning = false;
            bool passed = IsAutoMovePassed();

            autoMoveMessage = passed
                ? $"자동 이동 검증 성공: {AutoBlockedCount}마리 저지, {autoGoalReachedCount}마리 출구 도달"
                : $"자동 이동 검증 실패: 실제 저지 {AutoBlockedCount}, 실제 출구 {autoGoalReachedCount}, " +
                  $"예상 저지 {ExpectedAutoBlockedCount}, 예상 출구 {ExpectedAutoGoalCount}";

            if (passed)
            {
                Debug.Log(autoMoveMessage, this);
            }
            else
            {
                Debug.LogWarning(autoMoveMessage, this);
            }
        }

        private bool AreAutoMovesSettled()
        {
            if (autoMoves == null || autoMoves.Length != AutoEnemyCount)
            {
                return false;
            }

            for (int i = 0; i < autoMoves.Length; i++)
            {
                EnemyMove move = autoMoves[i];

                if (move == null || (!move.IsBlocked && !HasAutoReachedGoal(i)))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsAutoMovePassed()
        {
            return AreAutoMovesSettled() &&
                   AutoBlockedCount == ExpectedAutoBlockedCount &&
                   autoGoalReachedCount == ExpectedAutoGoalCount;
        }

        private void ClearAutoMoveState()
        {
            isAutoMoveRunning = false;

            CombatEvents.OnEnemyReachedGoal -= HandleAutoGoalReached;

            autoMoves = new EnemyMove[AutoEnemyCount];
            autoReachedGoals = new bool[AutoEnemyCount];
            autoGoalReachedCount = 0;
        }

        private void Bind(EnemyBlock enemy, string targetName)
        {
            if (unitBlock == null || enemy == null)
            {
                lastResult = false;
                lastMessage = $"{targetName} 저지 실패: 검증 대상이 준비되지 않았습니다.";
                return;
            }

            lastResult = BlockLink.TryBind(unitBlock, enemy);
            lastMessage = $"{targetName} 저지 {(lastResult ? "성공" : "실패")}: 현재 {unitBlock.Count} / {unitBlock.MaxCount}";
            Debug.Log(lastMessage, this);
        }

        private void Release(EnemyBlock enemy, string targetName)
        {
            if (enemy == null)
            {
                lastResult = false;
                lastMessage = $"{targetName} 해제 실패: 검증 대상이 없습니다.";
                return;
            }

            lastResult = BlockLink.Release(enemy);
            lastMessage = $"{targetName} 저지 해제 {(lastResult ? "성공" : "실패")}: 현재 {GetBlockCount()}마리";
            Debug.Log(lastMessage, this);
        }

        private void KillEnemy(EnemyBlock enemy, string targetName)
        {
            if (enemy == null)
            {
                lastResult = false;
                lastMessage = $"{targetName} 사망 실패: 검증 대상이 없습니다.";
                return;
            }

            EnemyRuntimeState enemyState = enemy.GetComponent<EnemyRuntimeState>();

            if (enemyState == null || enemyState.Health == null)
            {
                lastResult = false;
                lastMessage = $"{targetName}에서 EnemyRuntimeState를 찾지 못했습니다.";
                return;
            }

            enemyState.ApplyDamage(enemyState.Health.CurrentHp);
            lastResult = enemyState.Health.IsDead;
            lastMessage = $"{targetName} 사망 처리: 현재 저지 {GetBlockCount()}마리";
            Debug.Log(lastMessage, enemyState);
        }

        private int GetBlockCount()
        {
            return unitBlock == null ? 0 : unitBlock.Count;
        }
    }
}