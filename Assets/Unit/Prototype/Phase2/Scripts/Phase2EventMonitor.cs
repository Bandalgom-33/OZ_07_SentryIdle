using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Runtime;
using UnityEngine;

namespace EndlessGuard.Unit.Prototype.Phase2
{
    [DisallowMultipleComponent]
    public sealed class Phase2EventMonitor : MonoBehaviour
    {
        [Header("이벤트 수신 횟수")]
        [SerializeField] private int unitDiedCount;
        [SerializeField] private int enemyDiedCount;
        [SerializeField] private int enemyReachedGoalCount;
        [SerializeField] private int unitGrowthChangedCount;
        [SerializeField] private int unitProgressChangedCount;
        [SerializeField] private int summonRequestedCount;
        [SerializeField] private int summonCostRequestedCount;

        [Header("OnUnitGrowthChanged 연결 Probe")]
        [Tooltip("공통 성장 담당이 Unit 쪽에 성장 변경을 알릴 때 사용하는 공개 이벤트 연결을 검증합니다. 정식 데이터를 변경하지 않습니다.")]
        [SerializeField] private string growthProbeUnitId = "UNIT_0001";
        [SerializeField] private GrowthStatMask growthProbeStat = GrowthStatMask.PhysicalAttack;

        [Header("마지막 수신 내용")]
        [TextArea(3, 8)]
        [SerializeField] private string lastMessage;

        public int UnitDiedCount => unitDiedCount;
        public int EnemyDiedCount => enemyDiedCount;
        public int EnemyReachedGoalCount => enemyReachedGoalCount;
        public int UnitGrowthChangedCount => unitGrowthChangedCount;
        public int UnitProgressChangedCount => unitProgressChangedCount;
        public int SummonRequestedCount => summonRequestedCount;
        public int SummonCostRequestedCount => summonCostRequestedCount;
        public string LastMessage => lastMessage;

        private void OnEnable()
        {
            CombatEvents.OnUnitDied += HandleUnitDied;
            CombatEvents.OnEnemyDied += HandleEnemyDied;
            CombatEvents.OnEnemyReachedGoal += HandleEnemyReachedGoal;
            UnitProgressEvents.OnUnitGrowthChanged += HandleUnitGrowthChanged;
            UnitProgressEvents.OnUnitProgressChanged += HandleUnitProgressChanged;
            PassiveRuntimeEvents.OnSummonRequested += HandleSummonRequested;
            PassiveRuntimeEvents.OnSummonCostGainRequested += HandleSummonCostRequested;
        }

        private void OnDisable()
        {
            CombatEvents.OnUnitDied -= HandleUnitDied;
            CombatEvents.OnEnemyDied -= HandleEnemyDied;
            CombatEvents.OnEnemyReachedGoal -= HandleEnemyReachedGoal;
            UnitProgressEvents.OnUnitGrowthChanged -= HandleUnitGrowthChanged;
            UnitProgressEvents.OnUnitProgressChanged -= HandleUnitProgressChanged;
            PassiveRuntimeEvents.OnSummonRequested -= HandleSummonRequested;
            PassiveRuntimeEvents.OnSummonCostGainRequested -= HandleSummonCostRequested;
        }

        public void SendGrowthChangedProbe()
        {
            UnitProgressEvents.NotifyGrowthChanged(new UnitGrowthChangedInfo(growthProbeUnitId, growthProbeStat, 0f, 1f));
        }

        public void ResetCounts()
        {
            unitDiedCount = 0;
            enemyDiedCount = 0;
            enemyReachedGoalCount = 0;
            unitGrowthChangedCount = 0;
            unitProgressChangedCount = 0;
            summonRequestedCount = 0;
            summonCostRequestedCount = 0;
            lastMessage = string.Empty;
        }

        private void HandleUnitDied(UnitDiedInfo info)
        {
            unitDiedCount++;
            lastMessage = $"OnUnitDied #{unitDiedCount}: {info.UnitId}, Runtime {info.RuntimeId}, Position {info.Position}";
            Debug.Log(lastMessage, this);
        }

        private void HandleEnemyDied(EnemyDiedInfo info)
        {
            enemyDiedCount++;
            lastMessage = $"OnEnemyDied #{enemyDiedCount}: {info.EnemyId}, Runtime {info.RuntimeId}, Size {info.EnemySize}, Position {info.Position}";
            Debug.Log(lastMessage, this);
        }

        private void HandleEnemyReachedGoal(EnemyReachedGoalInfo info)
        {
            enemyReachedGoalCount++;
            lastMessage = $"OnEnemyReachedGoal #{enemyReachedGoalCount}: {info.EnemyId}, Runtime {info.RuntimeId}, Position {info.Position}";
            Debug.Log(lastMessage, this);
        }

        private void HandleUnitGrowthChanged(UnitGrowthChangedInfo info)
        {
            unitGrowthChangedCount++;
            lastMessage = $"OnUnitGrowthChanged #{unitGrowthChangedCount}: {info.UnitId}";
            Debug.Log(lastMessage, this);
        }

        private void HandleUnitProgressChanged(UnitProgressChangedInfo info)
        {
            unitProgressChangedCount++;
            lastMessage = $"OnUnitProgressChanged #{unitProgressChangedCount}: {info.UnitId}, Lv {info.PreviousLevel}->{info.CurrentLevel}, Promotion {info.PreviousPromotionStage}->{info.CurrentPromotionStage}";
            Debug.Log(lastMessage, this);
        }

        private void HandleSummonRequested(PassiveSummonRequest request)
        {
            summonRequestedCount++;
            lastMessage = $"Passive Summon 요청 #{summonRequestedCount}: {(request.Passive != null ? request.Passive.DisplayName : "미지정")}, Count {request.Count}";
            Debug.Log(lastMessage, this);
        }

        private void HandleSummonCostRequested(UnitRuntimeState unit, int amount, PassiveDataSO passive)
        {
            summonCostRequestedCount++;
            lastMessage = $"소환 코스트 획득 요청 #{summonCostRequestedCount}: {(unit != null ? unit.UnitId : "null")}, +{amount}, {(passive != null ? passive.DisplayName : "미지정")}";
            Debug.Log(lastMessage, this);
        }
    }
}
