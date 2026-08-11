using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Runtime;
using UnityEngine;

namespace EndlessGuard.Unit.Prototype
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CombatStatePrototypeController))]
    [RequireComponent(typeof(CombatLoop))]
    public sealed class AirTest : MonoBehaviour
    {
        private const float MoveCheckDistance = 0.05f;
        private const float RangeTolerance = 0.05f;

        [Header("검증 대상 연결")]
        [Tooltip("기존 검증 대상을 정리하기 위한 전투 상태 검증 컴포넌트입니다.")]
        [SerializeField] private CombatStatePrototypeController state;

        [Tooltip("기존 통합 전투 루프의 중복 실행을 막기 위해 연결합니다.")]
        [SerializeField] private CombatLoop combatLoop;

        [Tooltip("공중 몬스터의 공격 대상이 될 공식 캐릭터 프리팹입니다.")]
        [SerializeField] private GameObject unitPrefab;

        [Tooltip("Prototype 폴더에 만든 공중 InRange 검증 몬스터 프리팹입니다.")]
        [SerializeField] private GameObject enemyPrefab;

        [Header("검증 경로")]
        [Tooltip("격자 좌표 (0, 0)의 월드 기준 위치입니다.")]
        [SerializeField] private Vector3 worldOrigin;

        [Tooltip("격자 한 칸의 월드 크기입니다.")]
        [Min(0.01f)]
        [SerializeField] private float tileWorldSize = 1f;

        [Tooltip("지상 캐릭터가 위치할 타일입니다.")]
        [SerializeField] private Vector2Int unitTile = Vector2Int.zero;

        [Tooltip("공중 몬스터가 이동을 시작할 타일입니다.")]
        [SerializeField] private Vector2Int startTile = new Vector2Int(4, 0);

        [Tooltip("공중 몬스터가 향할 출구 타일입니다.")]
        [SerializeField] private Vector2Int goalTile = new Vector2Int(-4, 0);

        [Tooltip("공중 몬스터가 이동할 월드 높이입니다.")]
        [SerializeField] private float airHeight = 1.5f;

        [Header("검증 제한")]
        [Tooltip("이 시간 안에 공격과 출구 도달이 완료되지 않으면 실패합니다.")]
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
        [HideInInspector][SerializeField] private bool airLayerPassed;
        [HideInInspector][SerializeField] private bool blockViolation;
        [HideInInspector][SerializeField] private bool attackPauseViolation;
        [HideInInspector][SerializeField] private bool attackOccurred;
        [HideInInspector][SerializeField] private bool movedWhileAttacking;
        [HideInInspector][SerializeField] private bool passedUnit;
        [HideInInspector][SerializeField] private bool rangeExited;
        [HideInInspector][SerializeField] private bool noAttackAfterExit;
        [HideInInspector][SerializeField] private bool goalReached;
        [HideInInspector][SerializeField] private bool finalPassed;
        [HideInInspector][SerializeField] private int attackCount;
        [HideInInspector][SerializeField] private int attackCountAtExit;
        [HideInInspector][SerializeField] private float elapsedSeconds;
        [HideInInspector][SerializeField] private float unitStartHp;
        [HideInInspector][SerializeField] private float unitCurrentHp;
        [HideInInspector][SerializeField] private Vector3 firstAttackPosition;
        [HideInInspector][SerializeField] private Vector3 currentEnemyPosition;

        [HideInInspector]
        [TextArea(2, 4)]
        [SerializeField] private string message;

        public UnitRuntimeState Unit => unit;
        public EnemyRuntimeState Enemy => enemy;
        public bool IsReady => isReady;
        public bool IsRunning => isRunning;
        public bool AirLayerPassed => airLayerPassed;
        public bool NeverBlocked => !blockViolation;
        public bool NeverAttackPaused => !attackPauseViolation;
        public bool AttackOccurred => attackOccurred;
        public bool MovedWhileAttacking => movedWhileAttacking;
        public bool PassedUnit => passedUnit;
        public bool RangeExited => rangeExited;
        public bool NoAttackAfterExit => noAttackAfterExit;
        public bool GoalReached => goalReached;
        public bool FinalPassed => finalPassed;
        public int AttackCount => attackCount;
        public float ElapsedSeconds => elapsedSeconds;
        public float UnitStartHp => unitStartHp;
        public float UnitCurrentHp => unitCurrentHp;
        public float AppliedDamage => Mathf.Max(0f, unitStartHp - unitCurrentHp);
        public Vector3 FirstAttackPosition => firstAttackPosition;
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

                if (!attackOccurred)
                {
                    attackOccurred = true;
                    firstAttackPosition = enemy.transform.position;
                    Debug.Log($"공중 이동 공격 첫 성공: 위치 {firstAttackPosition}, 캐릭터 HP {unitCurrentHp:0.##}", enemy);
                }
            }

            if (attackOccurred && !movedWhileAttacking && IsTargetWithinWorldRange() && Vector3.Distance(firstAttackPosition, enemy.transform.position) > MoveCheckDistance)
            {
                movedWhileAttacking = true;
                Debug.Log("공중 몬스터가 공격 후에도 이동을 계속하는 것을 확인했습니다.", enemy);
            }

            if (!passedUnit && HasPassedUnit())
            {
                passedUnit = true;
                Debug.Log("공중 몬스터가 캐릭터 위치를 저지 없이 통과했습니다.", enemy);
            }

            if (attackOccurred && passedUnit && !rangeExited && !IsTargetWithinWorldRange())
            {
                rangeExited = true;
                attackCountAtExit = attackCount;
                Debug.Log($"공중 몬스터가 공격 사거리에서 이탈했습니다. 현재 공격 횟수 {attackCount}", enemy);
            }

            if (rangeExited)
            {
                noAttackAfterExit = attackCount == attackCountAtExit;
            }

            if (goalReached)
            {
                CompleteTest();
                return;
            }

            if (elapsedSeconds >= timeoutSeconds)
            {
                FailTest("제한 시간 안에 공중 이동 공격과 출구 도달 검증을 완료하지 못했습니다.");
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
                FailTest("State, CombatLoop, 캐릭터 프리팹 또는 공중 몬스터 프리팹이 연결되지 않았습니다.");
                return;
            }

            if (startTile.y != goalTile.y || unitTile.y != startTile.y)
            {
                FailTest("이번 공중 검증은 캐릭터와 시작·출구가 같은 가로 경로에 있어야 합니다.");
                return;
            }

            combatLoop.StopLoop();
            DisableStateActors();
            state.DespawnActors();
            CleanupActors();

            unitObject = Instantiate(unitPrefab, GetUnitWorldPosition(), Quaternion.identity, transform);
            enemyObject = Instantiate(enemyPrefab, GetEnemyWorldPosition(startTile), Quaternion.identity, transform);
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
                FailTest("공중 검증에 필요한 공통 런타임 컴포넌트가 없습니다.");
                CleanupActors();
                return;
            }

            if (enemy.DataLink == null || !enemy.DataLink.HasData)
            {
                FailTest("공중 몬스터 데이터가 연결되지 않았습니다.");
                CleanupActors();
                return;
            }

            EnemyDataSO enemyData = enemy.DataLink.EnemyData;

            if (enemyData.MovementType != EnemyMovementType.Air || enemyData.AttackRule != EnemyAttackRule.InRange || enemyData.AttackSettings == null || enemyData.AttackSettings.AttackMode == AttackMode.None)
            {
                FailTest("공중검증 데이터가 Air + InRange + 공격 가능 상태가 아닙니다.");
                CleanupActors();
                return;
            }

            unit.GridPosition.Initialize(unitTile, GridFacingDirection.East, CombatTargetLayer.Ground);
            move = enemy.Move;
            attack = enemy.Attack;
            CombatEvents.OnEnemyReachedGoal += HandleGoalReached;

            if (!move.SetPath(BuildPath()))
            {
                FailTest("공중 몬스터 경로를 설정하지 못했습니다.");
                CleanupActors();
                return;
            }

            move.SetPaused(true);
            airLayerPassed = enemy.GridPosition.TargetLayer == CombatTargetLayer.Air;
            unitStartHp = unit.Health.CurrentHp;
            unitCurrentHp = unitStartHp;
            currentEnemyPosition = enemy.transform.position;
            isReady = true;
            message = $"공중 이동 공격 검증 준비 완료: 시작 {startTile}, 캐릭터 {unitTile}, 출구 {goalTile}, 높이 {airHeight:0.##}";
            Debug.Log(message, this);
        }

        public void StartTest()
        {
            if (!isReady || unit == null || enemy == null || move == null || attack == null)
            {
                message = "먼저 공중 이동 공격 검증 준비를 실행하세요.";
                Debug.LogWarning(message, this);
                return;
            }

            elapsedSeconds = 0f;
            move.SetPaused(false);
            isRunning = true;
            message = "공중 몬스터 이동·공격 검증을 시작했습니다.";
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
            airLayerPassed = false;
            blockViolation = false;
            attackPauseViolation = false;
            attackOccurred = false;
            movedWhileAttacking = false;
            passedUnit = false;
            rangeExited = false;
            noAttackAfterExit = false;
            goalReached = false;
            finalPassed = false;
            attackCount = 0;
            attackCountAtExit = 0;
            elapsedSeconds = 0f;
            unitStartHp = 0f;
            unitCurrentHp = 0f;
            firstAttackPosition = Vector3.zero;
            currentEnemyPosition = Vector3.zero;
            message = string.Empty;
        }

        private void UpdateState()
        {
            unitCurrentHp = unit == null || unit.Health == null ? 0f : unit.Health.CurrentHp;
            currentEnemyPosition = enemy == null ? Vector3.zero : enemy.transform.position;
        }

        private bool IsTargetWithinWorldRange()
        {
            if (unit == null || enemy == null || enemy.DataLink == null || !enemy.DataLink.HasData || enemy.DataLink.EnemyData.AttackSettings == null)
            {
                return false;
            }

            float attackRange = enemy.DataLink.EnemyData.AttackSettings.AttackRange;
            return GetHorizontalDistance(enemy.transform.position, unit.transform.position) <= attackRange + RangeTolerance;
        }

        private bool HasPassedUnit()
        {
            if (enemy == null || unit == null)
            {
                return false;
            }

            if (goalTile.x < startTile.x)
            {
                return enemy.transform.position.x < unit.transform.position.x - MoveCheckDistance;
            }

            return enemy.transform.position.x > unit.transform.position.x + MoveCheckDistance;
        }

        private void CompleteTest()
        {
            isRunning = false;
            UpdateState();

            if (rangeExited)
            {
                noAttackAfterExit = attackCount == attackCountAtExit;
            }

            finalPassed = airLayerPassed && !blockViolation && !attackPauseViolation && attackOccurred && movedWhileAttacking && passedUnit && rangeExited && noAttackAfterExit && goalReached;

            if (finalPassed)
            {
                message = $"공중 이동 공격 검증 성공: {attackCount}회 공격, 저지·공격정지 없이 이동, 캐릭터 통과, 사거리 이탈 후 출구 도달";
                Debug.Log(message, this);
                return;
            }

            FailTest("공중 이동 공격 최종 결과가 예상 조건과 일치하지 않습니다.");
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
            return new PathNode(GetEnemyWorldPosition(tile), tile, facing);
        }

        private Vector3 GetUnitWorldPosition()
        {
            return new Vector3(worldOrigin.x + unitTile.x * tileWorldSize, worldOrigin.y, worldOrigin.z + unitTile.y * tileWorldSize);
        }

        private Vector3 GetEnemyWorldPosition(Vector2Int tile)
        {
            return new Vector3(worldOrigin.x + tile.x * tileWorldSize, worldOrigin.y + airHeight, worldOrigin.z + tile.y * tileWorldSize);
        }

        private GridFacingDirection GetFacing()
        {
            return goalTile.x >= startTile.x ? GridFacingDirection.East : GridFacingDirection.West;
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