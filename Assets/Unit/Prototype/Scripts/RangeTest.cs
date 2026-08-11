using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Runtime;
using UnityEngine;

namespace EndlessGuard.Unit.Prototype
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CombatStatePrototypeController))]
    [RequireComponent(typeof(CombatLoop))]
    public sealed class RangeTest : MonoBehaviour
    {
        private const float ResumeMoveDistance = 0.05f;

        private enum LossMode
        {
            None,
            Manual,
            Death
        }

        [Header("검증 대상 연결")]
        [Tooltip("기존 검증 대상을 정리하기 위한 전투 상태 검증 컴포넌트입니다.")]
        [SerializeField] private CombatStatePrototypeController state;

        [Tooltip("기존 통합 전투 루프의 중복 실행을 막기 위해 연결합니다.")]
        [SerializeField] private CombatLoop combatLoop;

        [Tooltip("원거리 몬스터의 공격 대상이 될 공식 캐릭터 프리팹입니다.")]
        [SerializeField] private GameObject unitPrefab;

        [Tooltip("Prototype 폴더에 만든 InRange 원거리 검증 몬스터 프리팹입니다.")]
        [SerializeField] private GameObject enemyPrefab;

        [Header("검증 경로")]
        [Tooltip("격자 좌표 (0, 0)의 월드 기준 위치입니다.")]
        [SerializeField] private Vector3 worldOrigin;

        [Tooltip("격자 한 칸의 월드 크기입니다.")]
        [Min(0.01f)]
        [SerializeField] private float tileWorldSize = 1f;

        [Tooltip("공격 대상 캐릭터가 위치할 타일입니다.")]
        [SerializeField] private Vector2Int unitTile = Vector2Int.zero;

        [Tooltip("원거리 몬스터가 이동을 시작할 타일입니다.")]
        [SerializeField] private Vector2Int startTile = new Vector2Int(4, 0);

        [Tooltip("원거리 몬스터가 향할 출구 타일입니다.")]
        [SerializeField] private Vector2Int goalTile = new Vector2Int(-4, 0);

        [Tooltip("검증 대상이 생성될 월드 Y 위치입니다.")]
        [SerializeField] private float spawnHeight;

        [Header("검증 제한")]
        [Tooltip("이 시간 안에 사거리 진입과 첫 공격이 발생하지 않으면 실패합니다.")]
        [Min(0.1f)]
        [SerializeField] private float attackTimeoutSeconds = 6f;

        [Tooltip("대상이 사라진 뒤 이 시간 안에 출구에 도달하지 않으면 실패합니다.")]
        [Min(0.1f)]
        [SerializeField] private float resumeTimeoutSeconds = 8f;

        [HideInInspector][SerializeField] private GameObject unitObject;
        [HideInInspector][SerializeField] private GameObject enemyObject;
        [HideInInspector][SerializeField] private UnitRuntimeState unit;
        [HideInInspector][SerializeField] private EnemyRuntimeState enemy;
        [HideInInspector][SerializeField] private EnemyMove move;
        [HideInInspector][SerializeField] private EnemyAttack attack;
        [HideInInspector][SerializeField] private LossMode lossMode;
        [HideInInspector][SerializeField] private bool isReady;
        [HideInInspector][SerializeField] private bool isRunning;
        [HideInInspector][SerializeField] private bool attackPauseDetected;
        [HideInInspector][SerializeField] private bool attackOccurred;
        [HideInInspector][SerializeField] private bool notBlockedAtPause;
        [HideInInspector][SerializeField] private bool targetRemoved;
        [HideInInspector][SerializeField] private bool targetDied;
        [HideInInspector][SerializeField] private bool targetLost;
        [HideInInspector][SerializeField] private bool attackPauseReleased;
        [HideInInspector][SerializeField] private bool movementResumed;
        [HideInInspector][SerializeField] private bool goalReached;
        [HideInInspector][SerializeField] private bool finalPassed;
        [HideInInspector][SerializeField] private float phaseElapsedSeconds;
        [HideInInspector][SerializeField] private float unitStartHp;
        [HideInInspector][SerializeField] private float unitCurrentHp;
        [HideInInspector][SerializeField] private float pauseWorldDistance;
        [HideInInspector][SerializeField] private Vector3 pauseWorldPosition;
        [HideInInspector][SerializeField] private Vector3 resumeStartPosition;
        [HideInInspector][SerializeField] private Vector3 currentEnemyPosition;

        [HideInInspector]
        [TextArea(2, 4)]
        [SerializeField] private string message;

        public UnitRuntimeState Unit => unit;
        public EnemyRuntimeState Enemy => enemy;
        public bool IsReady => isReady;
        public bool IsRunning => isRunning;
        public bool IsManualMode => lossMode == LossMode.Manual;
        public bool IsDeathMode => lossMode == LossMode.Death;
        public string LossModeName => IsManualMode ? "수동 제거" : IsDeathMode ? "자연 사망" : "없음";
        public bool AttackPauseDetected => attackPauseDetected;
        public bool AttackOccurred => attackOccurred;
        public bool NotBlockedAtPause => notBlockedAtPause;
        public bool TargetRemoved => targetRemoved;
        public bool TargetDied => targetDied;
        public bool TargetLost => targetLost;
        public bool AttackPauseReleased => attackPauseReleased;
        public bool MovementResumed => movementResumed;
        public bool GoalReached => goalReached;
        public bool FinalPassed => finalPassed;
        public float PhaseElapsedSeconds => phaseElapsedSeconds;
        public float UnitStartHp => unitStartHp;
        public float UnitCurrentHp => unitCurrentHp;
        public float AppliedDamage => Mathf.Max(0f, unitStartHp - unitCurrentHp);
        public float PauseWorldDistance => pauseWorldDistance;
        public Vector3 PauseWorldPosition => pauseWorldPosition;
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
            attackTimeoutSeconds = Mathf.Max(0.1f, attackTimeoutSeconds);
            resumeTimeoutSeconds = Mathf.Max(0.1f, resumeTimeoutSeconds);
        }

        private void Update()
        {
            if (!isRunning || enemy == null || move == null || attack == null)
            {
                return;
            }

            phaseElapsedSeconds += Time.deltaTime;
            move.Step(Time.deltaTime);
            attack.Step(Time.deltaTime);
            UpdateState();

            if (!attackPauseDetected && move.IsAttackPaused)
            {
                attackPauseDetected = true;
                pauseWorldPosition = enemy.transform.position;
                pauseWorldDistance = GetHorizontalDistance(enemy.transform.position, unit.transform.position);
                notBlockedAtPause = enemy.Block != null && !enemy.Block.IsBlocked;
                Debug.Log($"InRange 공격 정지 감지: 월드 위치 {pauseWorldPosition}, 캐릭터 거리 {pauseWorldDistance:0.##}, 저지 여부 {!notBlockedAtPause}", enemy);
            }

            if (!attackOccurred && unitCurrentHp < unitStartHp)
            {
                attackOccurred = true;
                Debug.Log($"InRange 원거리 공격 성공: 캐릭터 HP {unitStartHp:0.##} → {unitCurrentHp:0.##}", enemy);
            }

            if (!targetLost && unit != null && unit.Health != null && unit.Health.IsDead)
            {
                targetDied = true;

                if (IsDeathMode)
                {
                    BeginResumePhase("자연 사망");
                }
                else
                {
                    FailTest("수동 제거 검증 중 캐릭터가 먼저 사망했습니다. 첫 공격 확인 후 대상 제거 버튼을 눌러야 합니다.");
                    return;
                }
            }

            if (!targetLost)
            {
                if (!attackOccurred && phaseElapsedSeconds >= attackTimeoutSeconds)
                {
                    FailTest("제한 시간 안에 InRange 사거리 진입과 원거리 공격이 발생하지 않았습니다.");
                }

                return;
            }

            attackPauseReleased = !move.IsAttackPaused;

            if (!movementResumed && Vector3.Distance(resumeStartPosition, enemy.transform.position) > ResumeMoveDistance)
            {
                movementResumed = true;
                Debug.Log("대상 소실 후 원거리 몬스터 이동 재개 확인", enemy);
            }

            if (goalReached)
            {
                CompleteTest();
                return;
            }

            if (phaseElapsedSeconds >= resumeTimeoutSeconds)
            {
                FailTest("대상 소실 후 제한 시간 안에 이동을 재개하고 출구에 도달하지 못했습니다.");
            }
        }

        private void OnDisable()
        {
            StopTest();
            CleanupActors();
        }

        public void SetupManualTest()
        {
            SetupTest(LossMode.Manual);
        }

        public void SetupDeathTest()
        {
            SetupTest(LossMode.Death);
        }

        public void StartTest()
        {
            if (!isReady || unit == null || enemy == null || move == null || attack == null)
            {
                message = "먼저 InRange 검증 준비를 실행하세요.";
                Debug.LogWarning(message, this);
                return;
            }

            phaseElapsedSeconds = 0f;
            move.SetPaused(false);
            isRunning = true;
            message = $"InRange {LossModeName} 검증을 시작했습니다.";
            Debug.Log(message, this);
        }

        public void RemoveTarget()
        {
            if (!IsManualMode)
            {
                message = "현재 검증 방식은 자연 사망입니다. 대상 제거 버튼을 사용하지 않습니다.";
                Debug.LogWarning(message, this);
                return;
            }

            if (!isRunning || !attackOccurred || targetLost || unitObject == null)
            {
                message = "원거리 공격이 확인된 뒤 한 번만 대상을 제거할 수 있습니다.";
                Debug.LogWarning(message, this);
                return;
            }

            targetRemoved = true;
            BeginResumePhase("수동 제거");
            unitObject.SetActive(false);
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

            lossMode = LossMode.None;
            isReady = false;
            isRunning = false;
            attackPauseDetected = false;
            attackOccurred = false;
            notBlockedAtPause = false;
            targetRemoved = false;
            targetDied = false;
            targetLost = false;
            attackPauseReleased = false;
            movementResumed = false;
            goalReached = false;
            finalPassed = false;
            phaseElapsedSeconds = 0f;
            unitStartHp = 0f;
            unitCurrentHp = 0f;
            pauseWorldDistance = 0f;
            pauseWorldPosition = Vector3.zero;
            resumeStartPosition = Vector3.zero;
            currentEnemyPosition = Vector3.zero;
            message = string.Empty;
        }

        private void SetupTest(LossMode mode)
        {
            ResetResult();
            lossMode = mode;

            if (state == null || combatLoop == null || unitPrefab == null || enemyPrefab == null)
            {
                FailTest("State, CombatLoop, 캐릭터 프리팹 또는 원거리 몬스터 프리팹이 연결되지 않았습니다.");
                return;
            }

            combatLoop.StopLoop();
            DisableStateActors();
            state.DespawnActors();
            CleanupActors();

            Vector3 unitPosition = GetWorldPosition(unitTile);
            Vector3 enemyPosition = GetWorldPosition(startTile);

            unitObject = Instantiate(unitPrefab, unitPosition, Quaternion.identity, transform);
            enemyObject = Instantiate(enemyPrefab, enemyPosition, Quaternion.identity, transform);
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
                FailTest("원거리 검증에 필요한 공통 런타임 컴포넌트가 없습니다.");
                CleanupActors();
                return;
            }

            if (enemy.DataLink == null || !enemy.DataLink.HasData || enemy.DataLink.EnemyData.AttackRule != EnemyAttackRule.InRange)
            {
                FailTest("연결된 몬스터 데이터의 공격 시작 규칙이 InRange가 아닙니다.");
                CleanupActors();
                return;
            }

            unit.GridPosition.Initialize(unitTile, GridFacingDirection.East, CombatTargetLayer.Ground);
            move = enemy.Move;
            attack = enemy.Attack;
            CombatEvents.OnEnemyReachedGoal += HandleGoalReached;

            if (!move.SetPath(BuildPath()))
            {
                FailTest("원거리 몬스터 경로를 설정하지 못했습니다.");
                CleanupActors();
                return;
            }

            move.SetPaused(true);
            unitStartHp = unit.Health.CurrentHp;
            unitCurrentHp = unitStartHp;
            currentEnemyPosition = enemy.transform.position;
            isReady = true;
            message = $"InRange {LossModeName} 검증 준비 완료: 몬스터 시작 {startTile}, 캐릭터 {unitTile}, 출구 {goalTile}";
            Debug.Log(message, this);
        }

        private void BeginResumePhase(string reason)
        {
            if (targetLost)
            {
                return;
            }

            targetLost = true;
            resumeStartPosition = enemy.transform.position;
            phaseElapsedSeconds = 0f;
            message = $"공격 대상 소실 확인: {reason}. 공격 정지 해제와 출구 이동 재개를 확인합니다.";
            Debug.Log(message, this);
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
            attackPauseReleased = !move.IsAttackPaused;

            bool lossModePassed = IsManualMode ? targetRemoved && !targetDied : IsDeathMode && targetDied && !targetRemoved;
            finalPassed = attackPauseDetected && attackOccurred && notBlockedAtPause && targetLost && lossModePassed && attackPauseReleased && movementResumed && goalReached;

            if (finalPassed)
            {
                message = $"InRange {LossModeName} 검증 성공: 거리 {pauseWorldDistance:0.##}에서 정지·공격, 대상 소실 후 이동 재개, 출구 도달 완료";
                Debug.Log(message, this);
                return;
            }

            FailTest("InRange 최종 검증 결과가 예상 조건과 일치하지 않습니다.");
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
            int yCount = Mathf.Abs(goalTile.y - startTile.y);
            PathNode[] path = new PathNode[xCount + yCount + 1];
            Vector2Int current = startTile;
            int index = 0;

            path[index++] = CreateNode(current, GetFirstFacing());

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
            return new PathNode(GetWorldPosition(tile), tile, facing);
        }

        private Vector3 GetWorldPosition(Vector2Int tile)
        {
            return new Vector3(worldOrigin.x + tile.x * tileWorldSize, worldOrigin.y + spawnHeight, worldOrigin.z + tile.y * tileWorldSize);
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