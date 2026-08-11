using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Runtime;
using UnityEngine;

namespace EndlessGuard.Unit.Prototype
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CombatStatePrototypeController))]
    [RequireComponent(typeof(CombatLoop))]
    public sealed class NoAttackTest : MonoBehaviour
    {
        private const float PassDistance = 0.05f;

        [Header("검증 대상 연결")]
        [Tooltip("기존 검증 대상을 정리하기 위한 전투 상태 검증 컴포넌트입니다.")]
        [SerializeField] private CombatStatePrototypeController state;

        [Tooltip("기존 통합 전투 루프의 중복 실행을 막기 위해 연결합니다.")]
        [SerializeField] private CombatLoop combatLoop;

        [Tooltip("비공격 몬스터 근처에 배치할 공식 캐릭터 프리팹입니다.")]
        [SerializeField] private GameObject unitPrefab;

        [Tooltip("Prototype 폴더에 만든 비공격 검증 몬스터 프리팹입니다.")]
        [SerializeField] private GameObject enemyPrefab;

        [Header("검증 경로")]
        [Tooltip("격자 좌표 (0, 0)의 월드 기준 위치입니다.")]
        [SerializeField] private Vector3 worldOrigin;

        [Tooltip("격자 한 칸의 월드 크기입니다.")]
        [Min(0.01f)]
        [SerializeField] private float tileWorldSize = 1f;

        [Tooltip("캐릭터를 배치할 타일입니다. 몬스터 경로 옆 한 칸을 사용합니다.")]
        [SerializeField] private Vector2Int unitTile = new Vector2Int(0, 1);

        [Tooltip("몬스터가 이동을 시작할 타일입니다.")]
        [SerializeField] private Vector2Int startTile = new Vector2Int(4, 0);

        [Tooltip("몬스터가 향할 출구 타일입니다.")]
        [SerializeField] private Vector2Int goalTile = new Vector2Int(-4, 0);

        [Tooltip("검증 대상이 생성될 월드 Y 위치입니다.")]
        [SerializeField] private float spawnHeight;

        [Header("검증 제한")]
        [Tooltip("이 시간 안에 캐릭터 근처 통과와 출구 도달이 완료되지 않으면 실패합니다.")]
        [Min(0.1f)]
        [SerializeField] private float timeoutSeconds = 8f;

        [HideInInspector][SerializeField] private GameObject unitObject;
        [HideInInspector][SerializeField] private GameObject enemyObject;
        [HideInInspector][SerializeField] private UnitRuntimeState unit;
        [HideInInspector][SerializeField] private EnemyRuntimeState enemy;
        [HideInInspector][SerializeField] private EnemyMove move;
        [HideInInspector][SerializeField] private EnemyAttack attack;
        [HideInInspector][SerializeField] private bool isReady;
        [HideInInspector][SerializeField] private bool isRunning;
        [HideInInspector][SerializeField] private bool groundLayerPassed;
        [HideInInspector][SerializeField] private bool enteredRange;
        [HideInInspector][SerializeField] private bool blockViolation;
        [HideInInspector][SerializeField] private bool attackPauseViolation;
        [HideInInspector][SerializeField] private bool attackViolation;
        [HideInInspector][SerializeField] private bool hpViolation;
        [HideInInspector][SerializeField] private bool passedUnit;
        [HideInInspector][SerializeField] private bool goalReached;
        [HideInInspector][SerializeField] private bool finalPassed;
        [HideInInspector][SerializeField] private int attackCount;
        [HideInInspector][SerializeField] private float elapsedSeconds;
        [HideInInspector][SerializeField] private float unitStartHp;
        [HideInInspector][SerializeField] private float unitCurrentHp;
        [HideInInspector][SerializeField] private float minimumDistance;
        [HideInInspector][SerializeField] private Vector3 currentEnemyPosition;

        [HideInInspector]
        [TextArea(2, 4)]
        [SerializeField] private string message;

        public UnitRuntimeState Unit => unit;
        public EnemyRuntimeState Enemy => enemy;
        public bool IsReady => isReady;
        public bool IsRunning => isRunning;
        public bool GroundLayerPassed => groundLayerPassed;
        public bool EnteredRange => enteredRange;
        public bool NeverBlocked => !blockViolation;
        public bool NeverAttackPaused => !attackPauseViolation;
        public bool NeverAttacked => !attackViolation;
        public bool HpUnchanged => !hpViolation;
        public bool PassedUnit => passedUnit;
        public bool GoalReached => goalReached;
        public bool FinalPassed => finalPassed;
        public int AttackCount => attackCount;
        public float ElapsedSeconds => elapsedSeconds;
        public float UnitStartHp => unitStartHp;
        public float UnitCurrentHp => unitCurrentHp;
        public float MinimumDistance => minimumDistance;
        public Vector3 CurrentEnemyPosition => currentEnemyPosition;
        public string Message => message;

        private void Reset()
        {
            state = GetComponent<CombatStatePrototypeController>();
            combatLoop = GetComponent<CombatLoop>();
        }

        private void Awake()
        {
            if (combatLoop == null)
            {
                combatLoop = GetComponent<CombatLoop>();
            }

            combatLoop.StopLoop();
        }

        private void OnValidate()
        {
            if (state == null)
            {
                state = GetComponent<CombatStatePrototypeController>();
            }

            if (combatLoop == null)
            {
                combatLoop = GetComponent<CombatLoop>();
            }

            tileWorldSize = Mathf.Max(0.01f, tileWorldSize);
            timeoutSeconds = Mathf.Max(0.1f, timeoutSeconds);
        }

        private void Update()
        {
            if (!isRunning || unit == null || enemy == null || move == null || attack == null)
            {
                return;
            }

            elapsedSeconds += Time.deltaTime;

            move.Step(Time.deltaTime);
            bool attackedThisFrame = attack.Step(Time.deltaTime);
            UpdateState();

            if (enemy.Block != null && enemy.Block.IsBlocked)
            {
                blockViolation = true;
            }

            if (move.IsAttackPaused)
            {
                attackPauseViolation = true;
            }

            if (attackedThisFrame)
            {
                attackCount++;
                attackViolation = true;
            }

            if (unitCurrentHp < unitStartHp)
            {
                hpViolation = true;
            }

            float currentDistance = GetHorizontalDistance(enemy.transform.position, unit.transform.position);
            minimumDistance = Mathf.Min(minimumDistance, currentDistance);

            if (!enteredRange && currentDistance <= enemy.DataLink.EnemyData.AttackSettings.AttackRange)
            {
                enteredRange = true;
                Debug.Log($"비공격 몬스터가 공격 사거리 안에 진입했습니다. 캐릭터 거리 {currentDistance:0.##}", enemy);
            }

            if (!passedUnit && HasPassedUnit())
            {
                passedUnit = true;
                Debug.Log("비공격 몬스터가 캐릭터 옆을 통과했습니다.", enemy);
            }

            if (goalReached)
            {
                CompleteTest();
                return;
            }

            if (elapsedSeconds >= timeoutSeconds)
            {
                FailTest("제한 시간 안에 비공격 몬스터가 출구에 도달하지 못했습니다.");
            }
        }

        private void OnDisable()
        {
            StopTest();
            CleanupActors();
        }

        public void SetupTest()
        {
            ResetResult();

            if (state == null || combatLoop == null || unitPrefab == null || enemyPrefab == null)
            {
                FailTest("State, CombatLoop, 캐릭터 프리팹 또는 비공격 몬스터 프리팹이 연결되지 않았습니다.");
                return;
            }

            if (startTile.y != goalTile.y || unitTile.y == startTile.y)
            {
                FailTest("이번 검증은 직선 경로와 경로 옆 캐릭터 배치가 필요합니다.");
                return;
            }

            combatLoop.StopLoop();
            DisableStateActors();
            state.DespawnActors();
            CleanupActors();

            unitObject = Instantiate(unitPrefab, GetWorldPosition(unitTile), Quaternion.identity, transform);
            enemyObject = Instantiate(enemyPrefab, GetWorldPosition(startTile), Quaternion.identity, transform);
            unit = unitObject.GetComponent<UnitRuntimeState>();
            enemy = enemyObject.GetComponent<EnemyRuntimeState>();

            if (unit == null || enemy == null)
            {
                FailTest("검증 프리팹에서 UnitRuntimeState 또는 EnemyRuntimeState를 찾지 못했습니다.");
                CleanupActors();
                return;
            }

            if (unit.GridPosition == null || enemy.GridPosition == null || enemy.Move == null || enemy.Attack == null || enemy.Block == null)
            {
                FailTest("비공격 검증에 필요한 공통 런타임 컴포넌트가 없습니다.");
                CleanupActors();
                return;
            }

            if (enemy.DataLink == null || !enemy.DataLink.HasData || enemy.DataLink.EnemyData.AttackSettings == null)
            {
                FailTest("비공격 몬스터 데이터가 올바르게 연결되지 않았습니다.");
                CleanupActors();
                return;
            }

            EnemyDataSO enemyData = enemy.DataLink.EnemyData;

            if (enemyData.MovementType != EnemyMovementType.Ground || enemyData.AttackRule != EnemyAttackRule.InRange || enemyData.AttackSettings.AttackMode != AttackMode.None)
            {
                FailTest("비공격검증 데이터가 Ground + InRange + 공격하지 않음 상태가 아닙니다.");
                CleanupActors();
                return;
            }

            unit.GridPosition.Initialize(unitTile, GridFacingDirection.East, CombatTargetLayer.Ground);
            move = enemy.Move;
            attack = enemy.Attack;
            CombatEvents.OnEnemyReachedGoal += HandleGoalReached;

            if (!move.SetPath(BuildPath()))
            {
                FailTest("비공격 몬스터 경로를 설정하지 못했습니다.");
                CleanupActors();
                return;
            }

            move.SetPaused(true);
            groundLayerPassed = enemy.GridPosition.TargetLayer == CombatTargetLayer.Ground;
            unitStartHp = unit.Health.CurrentHp;
            unitCurrentHp = unitStartHp;
            minimumDistance = float.MaxValue;
            currentEnemyPosition = enemy.transform.position;
            isReady = true;
            message = $"비공격 검증 준비 완료: 시작 {startTile}, 캐릭터 {unitTile}, 출구 {goalTile}";
            Debug.Log(message, this);
        }

        public void StartTest()
        {
            if (!isReady || unit == null || enemy == null || move == null || attack == null)
            {
                message = "먼저 비공격 검증 준비를 실행하세요.";
                Debug.LogWarning(message, this);
                return;
            }

            elapsedSeconds = 0f;
            move.SetPaused(false);
            isRunning = true;
            message = "비공격 몬스터 이동 검증을 시작했습니다.";
            Debug.Log(message, this);
        }

        public void StopTest()
        {
            isRunning = false;

            if (move != null)
            {
                move.SetPaused(true);
            }
        }

        public void ResetResult()
        {
            StopTest();
            CleanupActors();

            isReady = false;
            isRunning = false;
            groundLayerPassed = false;
            enteredRange = false;
            blockViolation = false;
            attackPauseViolation = false;
            attackViolation = false;
            hpViolation = false;
            passedUnit = false;
            goalReached = false;
            finalPassed = false;
            attackCount = 0;
            elapsedSeconds = 0f;
            unitStartHp = 0f;
            unitCurrentHp = 0f;
            minimumDistance = float.MaxValue;
            currentEnemyPosition = Vector3.zero;
            message = string.Empty;
        }

        private void UpdateState()
        {
            unitCurrentHp = unit == null || unit.Health == null ? 0f : unit.Health.CurrentHp;
            currentEnemyPosition = enemy == null ? Vector3.zero : enemy.transform.position;
        }

        private void CompleteTest()
        {
            isRunning = false;
            UpdateState();

            finalPassed = groundLayerPassed && enteredRange && !blockViolation && !attackPauseViolation && !attackViolation && !hpViolation && passedUnit && goalReached;

            if (finalPassed)
            {
                message = $"비공격 검증 성공: 최소 거리 {minimumDistance:0.##}, 공격 0회, HP 변화 없음, 저지·공격정지 없이 출구 도달";
                Debug.Log(message, this);
                return;
            }

            FailTest("비공격 몬스터 최종 결과가 예상 조건과 일치하지 않습니다.");
        }

        private void FailTest(string failureMessage)
        {
            isRunning = false;
            finalPassed = false;

            if (move != null)
            {
                move.SetPaused(true);
            }

            message = failureMessage;
            Debug.LogWarning(message, this);
        }

        private void DisableStateActors()
        {
            if (state.SpawnedUnit != null)
            {
                state.SpawnedUnit.gameObject.SetActive(false);
            }

            if (state.SpawnedEnemy != null)
            {
                state.SpawnedEnemy.gameObject.SetActive(false);
            }
        }

        private void CleanupActors()
        {
            CombatEvents.OnEnemyReachedGoal -= HandleGoalReached;

            if (unitObject != null)
            {
                unitObject.SetActive(false);
                Destroy(unitObject);
            }

            if (enemyObject != null)
            {
                enemyObject.SetActive(false);
                Destroy(enemyObject);
            }

            unitObject = null;
            enemyObject = null;
            unit = null;
            enemy = null;
            move = null;
            attack = null;
        }

        private PathNode[] BuildPath()
        {
            int xCount = Mathf.Abs(goalTile.x - startTile.x);
            PathNode[] path = new PathNode[xCount + 1];
            Vector2Int current = startTile;
            int index = 0;

            path[index++] = CreateNode(current, GetFacing());

            while (current.x != goalTile.x)
            {
                int step = goalTile.x > current.x ? 1 : -1;
                current = new Vector2Int(current.x + step, current.y);
                path[index++] = CreateNode(current, step > 0 ? GridFacingDirection.East : GridFacingDirection.West);
            }

            return path;
        }

        private PathNode CreateNode(Vector2Int tile, GridFacingDirection facing)
        {
            return new PathNode(GetWorldPosition(tile), tile, facing);
        }

        private Vector3 GetWorldPosition(Vector2Int tile)
        {
            return new Vector3(worldOrigin.x + tile.x * tileWorldSize, worldOrigin.y + spawnHeight, worldOrigin.z + tile.y * tileWorldSize);
        }

        private GridFacingDirection GetFacing()
        {
            return goalTile.x >= startTile.x ? GridFacingDirection.East : GridFacingDirection.West;
        }

        private bool HasPassedUnit()
        {
            if (enemy == null || unit == null)
            {
                return false;
            }

            if (goalTile.x < startTile.x)
            {
                return enemy.transform.position.x < unit.transform.position.x - PassDistance;
            }

            return enemy.transform.position.x > unit.transform.position.x + PassDistance;
        }

        private static float GetHorizontalDistance(Vector3 first, Vector3 second)
        {
            float deltaX = first.x - second.x;
            float deltaZ = first.z - second.z;
            return Mathf.Sqrt(deltaX * deltaX + deltaZ * deltaZ);
        }

        private void HandleGoalReached(EnemyReachedGoalInfo info)
        {
            if (enemy != null && info.RuntimeId == enemy.RuntimeId)
            {
                goalReached = true;
            }
        }
    }
}