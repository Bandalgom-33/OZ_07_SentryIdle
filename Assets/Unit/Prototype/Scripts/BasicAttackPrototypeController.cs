using EndlessGuard.Unit.Runtime;
using UnityEngine;

namespace EndlessGuard.Unit.Prototype
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CombatStatePrototypeController))]
    public sealed class BasicAttackPrototypeController : MonoBehaviour
    {
        [Header("검증 대상 연결")]
        [Tooltip("캐릭터와 몬스터 인스턴스를 생성하고 보관하는 기존 전투 상태 검증 컴포넌트입니다.")]
        [SerializeField] private CombatStatePrototypeController stateController;

        [HideInInspector]
        [SerializeField] private BasicAttackResult lastResult;

        [HideInInspector]
        [SerializeField] private bool hasResult;

        [HideInInspector]
        [SerializeField] private int unitAttackSuccessCount;

        [HideInInspector]
        [SerializeField] private int enemyAttackSuccessCount;

        [HideInInspector]
        [TextArea(2, 4)]
        [SerializeField] private string lastMessage;

        public CombatStatePrototypeController StateController => stateController;
        public BasicAttackResult LastResult => lastResult;
        public bool HasResult => hasResult;
        public int UnitAttackSuccessCount => unitAttackSuccessCount;
        public int EnemyAttackSuccessCount => enemyAttackSuccessCount;
        public string LastMessage => lastMessage;
        public UnitRuntimeState Unit => stateController == null ? null : stateController.SpawnedUnit;
        public EnemyRuntimeState Enemy => stateController == null ? null : stateController.SpawnedEnemy;

        private void Reset()
        {
            stateController = GetComponent<CombatStatePrototypeController>();
        }

        private void OnValidate()
        {
            if (stateController == null)
            {
                stateController = GetComponent<CombatStatePrototypeController>();
            }
        }

        public bool TryCreateUnitAttackContext(out BasicAttackContext context)
        {
            return BasicAttackContextFactory.TryCreate(Unit, Enemy, out context);
        }

        public bool TryCreateEnemyAttackContext(out BasicAttackContext context)
        {
            return BasicAttackContextFactory.TryCreate(Enemy, Unit, out context);
        }

        public void PrepareUnitAttack()
        {
            UnitRuntimeState unit = Unit;

            if (!CanPrepare(unit))
            {
                lastMessage = "공격을 준비할 수 있는 캐릭터 인스턴스가 없습니다.";
                return;
            }

            float attacksPerSecond = unit.DataLink.UnitData.BaseStats.BaseAttacksPerSecond;

            if (attacksPerSecond <= 0f)
            {
                lastMessage = "캐릭터 기본 공격 빈도가 0입니다.";
                return;
            }

            float requiredSeconds = 1f / attacksPerSecond;
            unit.AdvanceAttackProgress(attacksPerSecond, requiredSeconds);
            lastMessage = $"캐릭터 공격 1회 준비: 진행도 {unit.AttackProgress:0.###}, 준비 공격 {unit.ReadyAttackCount}회";
            Debug.Log(lastMessage, unit);
        }

        public void ExecuteUnitAttack()
        {
            if (!TryCreateUnitAttackContext(out BasicAttackContext context))
            {
                lastResult = BasicAttackResult.Failed(BasicAttackFailureReason.GridContextUnavailable);
                hasResult = true;
                lastMessage = "캐릭터 기본 공격 실패: 격자 공격 상황을 자동 생성하지 못했습니다.";
                Debug.Log(lastMessage, this);
                return;
            }

            bool succeeded = BasicAttackExecutor.TryExecute(Unit, Enemy, context, out lastResult);
            hasResult = true;

            if (succeeded)
            {
                unitAttackSuccessCount++;
                lastMessage = $"캐릭터 기본 공격 성공: 자동 타일 {context.RelativeTargetTile}, 자동 거리 {context.HorizontalWorldDistance:0.###}, 피해 {lastResult.AppliedDamage:0.##}, SP 획득 {lastResult.SkillGaugeGained:0.##}, 몬스터 HP {Enemy.Health.CurrentHp:0.##}";
                Debug.Log(lastMessage, Unit);
                return;
            }

            lastMessage = $"캐릭터 기본 공격 실패: {lastResult.FailureReason}, 자동 타일 {context.RelativeTargetTile}, 자동 거리 {context.HorizontalWorldDistance:0.###}, 방향 {context.FacingDirection}, 대상 {context.TargetLayer}";
            Debug.Log(lastMessage, this);
        }

        public void PrepareEnemyAttack()
        {
            EnemyRuntimeState enemy = Enemy;

            if (!CanPrepare(enemy))
            {
                lastMessage = "공격을 준비할 수 있는 몬스터 인스턴스가 없습니다.";
                return;
            }

            float attacksPerSecond = enemy.DataLink.EnemyData.BaseStats.BaseAttacksPerSecond;

            if (attacksPerSecond <= 0f)
            {
                lastMessage = "몬스터 기본 공격 빈도가 0입니다.";
                return;
            }

            float requiredSeconds = 1f / attacksPerSecond;
            enemy.AdvanceAttackProgress(attacksPerSecond, requiredSeconds);
            lastMessage = $"몬스터 공격 1회 준비: 진행도 {enemy.AttackProgress:0.###}, 준비 공격 {enemy.ReadyAttackCount}회";
            Debug.Log(lastMessage, enemy);
        }

        public void ExecuteEnemyAttack()
        {
            if (!TryCreateEnemyAttackContext(out BasicAttackContext context))
            {
                lastResult = BasicAttackResult.Failed(BasicAttackFailureReason.GridContextUnavailable);
                hasResult = true;
                lastMessage = "몬스터 기본 공격 실패: 격자 공격 상황을 자동 생성하지 못했습니다.";
                Debug.Log(lastMessage, this);
                return;
            }

            bool succeeded = BasicAttackExecutor.TryExecute(Enemy, Unit, context, out lastResult);
            hasResult = true;

            if (succeeded)
            {
                enemyAttackSuccessCount++;
                lastMessage = $"몬스터 기본 공격 성공: 자동 타일 {context.RelativeTargetTile}, 자동 거리 {context.HorizontalWorldDistance:0.###}, 피해 {lastResult.AppliedDamage:0.##}, 캐릭터 HP {Unit.Health.CurrentHp:0.##}";
                Debug.Log(lastMessage, Enemy);
                return;
            }

            lastMessage = $"몬스터 기본 공격 실패: {lastResult.FailureReason}, 자동 타일 {context.RelativeTargetTile}, 자동 거리 {context.HorizontalWorldDistance:0.###}, 방향 {context.FacingDirection}, 대상 {context.TargetLayer}";
            Debug.Log(lastMessage, this);
        }

        public void ResetResults()
        {
            lastResult = default;
            hasResult = false;
            unitAttackSuccessCount = 0;
            enemyAttackSuccessCount = 0;
            lastMessage = string.Empty;
        }

        private static bool CanPrepare(UnitRuntimeState unit)
        {
            return unit != null && unit.IsInitialized && unit.Health != null && !unit.Health.IsDead;
        }

        private static bool CanPrepare(EnemyRuntimeState enemy)
        {
            return enemy != null && enemy.IsInitialized && enemy.Health != null && !enemy.Health.IsDead;
        }
    }
}