using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Runtime;
using UnityEngine;

namespace EndlessGuard.Unit.Prototype
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CombatStatePrototypeController))]
    [RequireComponent(typeof(CombatLoop))]
    public sealed class LoopTest : MonoBehaviour
    {
        [Header("검증 대상 연결")]
        [Tooltip("검증용 캐릭터와 몬스터를 생성하는 컴포넌트입니다.")]
        [SerializeField] private CombatStatePrototypeController state;

        [Tooltip("캐릭터와 몬스터의 전투를 통합 갱신하는 실제 런타임 컴포넌트입니다.")]
        [SerializeField] private CombatLoop combatLoop;

        [Header("검증 경로")]
        [Tooltip("격자 좌표 (0, 0)의 월드 기준 위치입니다.")]
        [SerializeField] private Vector3 worldOrigin;

        [Tooltip("격자 한 칸의 월드 크기입니다.")]
        [Min(0.01f)]
        [SerializeField] private float tileWorldSize = 1f;

        [Tooltip("몬스터가 이동을 시작할 타일입니다.")]
        [SerializeField] private Vector2Int startTile = new Vector2Int(4, 0);

        [Tooltip("몬스터가 향할 출구 타일입니다.")]
        [SerializeField] private Vector2Int goalTile = new Vector2Int(-4, 0);

        [Tooltip("몬스터가 생성될 월드 Y 위치입니다.")]
        [SerializeField] private float spawnHeight;

        [Header("검증 제한")]
        [Tooltip("이 시간 안에 양방향 자동 공격이 발생하지 않으면 검증 실패로 처리합니다.")]
        [Min(0.1f)]
        [SerializeField] private float timeoutSeconds = 8f;

        [HideInInspector][SerializeField] private UnitRuntimeState unit;
        [HideInInspector][SerializeField] private EnemyRuntimeState enemy;
        [HideInInspector][SerializeField] private EnemyMove move;
        [HideInInspector][SerializeField] private bool isReady;
        [HideInInspector][SerializeField] private bool isRunning;
        [HideInInspector][SerializeField] private bool isBlocked;
        [HideInInspector][SerializeField] private bool unitAttacked;
        [HideInInspector][SerializeField] private bool enemyAttacked;
        [HideInInspector][SerializeField] private bool skillGaugeGained;
        [HideInInspector][SerializeField] private bool goalReached;
        [HideInInspector][SerializeField] private bool finalPassed;
        [HideInInspector][SerializeField] private float elapsedSeconds;
        [HideInInspector][SerializeField] private float unitStartHp;
        [HideInInspector][SerializeField] private float unitCurrentHp;
        [HideInInspector][SerializeField] private float enemyStartHp;
        [HideInInspector][SerializeField] private float enemyCurrentHp;
        [HideInInspector][SerializeField] private float startSkillGauge;
        [HideInInspector][SerializeField] private float currentSkillGauge;

        [HideInInspector]
        [TextArea(2, 4)]
        [SerializeField] private string message;

        public UnitRuntimeState Unit => unit;
        public EnemyRuntimeState Enemy => enemy;
        public bool IsReady => isReady;
        public bool IsRunning => isRunning;
        public bool IsBlocked => isBlocked;
        public bool UnitAttacked => unitAttacked;
        public bool EnemyAttacked => enemyAttacked;
        public bool SkillGaugeGained => skillGaugeGained;
        public bool GoalReached => goalReached;
        public bool FinalPassed => finalPassed;
        public float ElapsedSeconds => elapsedSeconds;
        public float UnitStartHp => unitStartHp;
        public float UnitCurrentHp => unitCurrentHp;
        public float EnemyStartHp => enemyStartHp;
        public float EnemyCurrentHp => enemyCurrentHp;
        public float UnitAppliedDamage => Mathf.Max(0f, enemyStartHp - enemyCurrentHp);
        public float EnemyAppliedDamage => Mathf.Max(0f, unitStartHp - unitCurrentHp);
        public float StartSkillGauge => startSkillGauge;
        public float CurrentSkillGauge => currentSkillGauge;
        public float GainedSkillGauge => Mathf.Max(0f, currentSkillGauge - startSkillGauge);
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
            if (!isRunning)
            {
                return;
            }

            elapsedSeconds += Time.deltaTime;
            UpdateState();

            if (isBlocked && unitAttacked && enemyAttacked && skillGaugeGained)
            {
                CompleteTest();
                return;
            }

            if (goalReached)
            {
                FailTest("몬스터가 양방향 전투를 완료하지 않고 출구에 도달했습니다.");
                return;
            }

            if (elapsedSeconds >= timeoutSeconds)
            {
                FailTest("제한 시간 안에 캐릭터와 몬스터의 양방향 자동 공격이 모두 발생하지 않았습니다.");
            }
        }

        private void OnDisable()
        {
            StopLoop();
            UnsubscribeGoal();
        }

        public void SetupTest()
        {
            ResetResult();

            if (state == null || combatLoop == null)
            {
                FailTest("CombatStatePrototypeController 또는 CombatLoop가 연결되지 않았습니다.");
                return;
            }

            combatLoop.StopLoop();
            state.SpawnActors();
            unit = state.SpawnedUnit;
            enemy = state.SpawnedEnemy;

            if (unit == null || enemy == null)
            {
                FailTest("검증용 캐릭터 또는 몬스터가 생성되지 않았습니다.");
                return;
            }

            if (unit.Health == null || unit.Attack == null || enemy.Health == null || enemy.Move == null || enemy.Block == null || enemy.Attack == null)
            {
                FailTest("검증 대상의 공통 런타임 컴포넌트가 완성되지 않았습니다.");
                return;
            }

            if (startTile == goalTile)
            {
                FailTest("시작 타일과 출구 타일이 같아 경로를 생성할 수 없습니다.");
                return;
            }

            move = enemy.Move;
            CombatEvents.OnEnemyReachedGoal += HandleGoalReached;

            if (!move.SetPath(BuildPath()))
            {
                FailTest("몬스터 경로를 설정하지 못했습니다.");
                UnsubscribeGoal();
                return;
            }

            move.SetPaused(true);

            unitStartHp = unit.Health.CurrentHp;
            unitCurrentHp = unitStartHp;
            enemyStartHp = enemy.Health.CurrentHp;
            enemyCurrentHp = enemyStartHp;
            startSkillGauge = unit.CurrentSkillGauge;
            currentSkillGauge = startSkillGauge;

            isReady = true;
            message = $"양방향 전투 검증 준비 완료: 캐릭터 HP {unitStartHp:0.##}, 몬스터 HP {enemyStartHp:0.##}, 스킬 게이지 {startSkillGauge:0.##}";
            Debug.Log(message, this);
        }

        public void StartTest()
        {
            if (!isReady || move == null || unit == null || enemy == null)
            {
                message = "먼저 양방향 전투 검증 준비를 실행하세요.";
                Debug.LogWarning(message, this);
                return;
            }

            elapsedSeconds = 0f;
            move.SetPaused(false);
            combatLoop.StartLoop();
            isRunning = true;
            message = "CombatLoop 양방향 자동 전투 검증을 시작했습니다.";
            Debug.Log(message, this);
        }

        public void StopTest()
        {
            StopLoop();
            UpdateState();
            message = "양방향 자동 전투 검증을 수동으로 정지했습니다.";
            Debug.Log(message, this);
        }

        public void ResetResult()
        {
            StopLoop();
            UnsubscribeGoal();

            if (enemy != null && enemy.Block != null)
            {
                BlockLink.Release(enemy.Block);
            }

            if (move != null)
            {
                move.ClearPath();
            }

            unit = null;
            enemy = null;
            move = null;
            isReady = false;
            isRunning = false;
            isBlocked = false;
            unitAttacked = false;
            enemyAttacked = false;
            skillGaugeGained = false;
            goalReached = false;
            finalPassed = false;
            elapsedSeconds = 0f;
            unitStartHp = 0f;
            unitCurrentHp = 0f;
            enemyStartHp = 0f;
            enemyCurrentHp = 0f;
            startSkillGauge = 0f;
            currentSkillGauge = 0f;
            message = string.Empty;
        }

        private void UpdateState()
        {
            isBlocked = enemy != null && enemy.Block != null && enemy.Block.IsBlocked;
            unitCurrentHp = unit == null || unit.Health == null ? 0f : unit.Health.CurrentHp;
            enemyCurrentHp = enemy == null || enemy.Health == null ? 0f : enemy.Health.CurrentHp;
            currentSkillGauge = unit == null ? 0f : unit.CurrentSkillGauge;
            unitAttacked = enemyCurrentHp < enemyStartHp;
            enemyAttacked = unitCurrentHp < unitStartHp;
            skillGaugeGained = currentSkillGauge > startSkillGauge;
        }

        private void CompleteTest()
        {
            StopLoop();
            UpdateState();

            finalPassed = isBlocked && unitAttacked && enemyAttacked && skillGaugeGained && !goalReached;

            if (finalPassed)
            {
                message = $"양방향 전투 검증 성공: 캐릭터 피해 {EnemyAppliedDamage:0.##}, 몬스터 피해 {UnitAppliedDamage:0.##}, 스킬 게이지 획득 {GainedSkillGauge:0.##}";
                Debug.Log(message, this);
                return;
            }

            FailTest("양방향 전투 검증 결과가 예상 조건과 일치하지 않습니다.");
        }

        private void FailTest(string failureMessage)
        {
            StopLoop();
            UpdateState();
            finalPassed = false;
            message = failureMessage;
            Debug.LogWarning(message, this);
        }

        private void StopLoop()
        {
            isRunning = false;

            if (combatLoop != null)
            {
                combatLoop.StopLoop();
            }

            if (move != null)
            {
                move.SetPaused(true);
            }
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
            Vector3 position = new Vector3(worldOrigin.x + tile.x * tileWorldSize, worldOrigin.y + spawnHeight, worldOrigin.z + tile.y * tileWorldSize);
            return new PathNode(position, tile, facing);
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

        private void HandleGoalReached(EnemyReachedGoalInfo info)
        {
            if (enemy != null && info.RuntimeId == enemy.RuntimeId)
            {
                goalReached = true;
            }
        }

        private void UnsubscribeGoal()
        {
            CombatEvents.OnEnemyReachedGoal -= HandleGoalReached;
        }
    }
}