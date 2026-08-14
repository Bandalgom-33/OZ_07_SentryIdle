using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyDataLink))]
    [RequireComponent(typeof(CombatEntityAnchors))]
    [RequireComponent(typeof(CombatHealth))]
    [RequireComponent(typeof(CombatGridPosition))]
    [RequireComponent(typeof(EnemyMove))]
    public sealed class EnemyRuntimeState : MonoBehaviour
    {
        [Header("몬스터 런타임 상태")]
        [Tooltip("기본 공격 실행을 위해 누적되는 공격 진행도입니다.")]
        [SerializeField] private AttackProgressState attackProgress = new AttackProgressState();

        [Tooltip("기준 전투 능력치에 성장, 패시브와 전투 효과를 반영하여 사용하는 런타임 능력치입니다.")]
        [SerializeField] private RuntimeStats runtimeStats = new RuntimeStats();

        [Tooltip("EnemyDataSO에 설정된 패시브를 현재 전투에서 실행·관리하는 런타임 상태입니다.")]
        [SerializeField] private EnemyPassiveRuntime passiveRuntime = new EnemyPassiveRuntime();

        private PassiveStatusRuntime passiveStatuses = new PassiveStatusRuntime();

        private bool isInitialized;
        private int runtimeId;
        private EnemyDataLink dataLink;
        private CombatEntityAnchors anchors;
        private CombatHealth health;
        private CombatGridPosition gridPosition;
        private EnemyMove move;
        private EnemyBlock block;
        private EnemyAttack attack;
        private bool deathPublished;
        private bool isSummon;
        private EnemySummonRuntime summonRuntime;

        public EnemyDataLink DataLink => dataLink;
        public CombatEntityAnchors Anchors => anchors;
        public CombatHealth Health => health;
        public CombatGridPosition GridPosition => gridPosition;
        public EnemyMove Move => move;
        public EnemyBlock Block => block;
        public EnemyAttack Attack => attack;
        public RuntimeStats Stats => runtimeStats;
        public EnemyPassiveRuntime Passives => passiveRuntime;
        public PassiveStatusRuntime Statuses => passiveStatuses;
        public string EnemyId => dataLink == null ? string.Empty : dataLink.EnemyId;
        public float AttackProgress => attackProgress.Progress;
        public int ReadyAttackCount => attackProgress.ReadyAttackCount;
        public bool IsInitialized => isInitialized;
        public int RuntimeId => runtimeId;
        public bool IsSummon => isSummon;
        public EnemySummonRuntime SummonRuntime => summonRuntime;
        public Vector3 DamageNumberPosition => anchors != null && anchors.EffectPoint != null ? anchors.EffectPoint.position : transform.position;

        private void Awake()
        {
            dataLink = GetComponent<EnemyDataLink>();
            anchors = GetComponent<CombatEntityAnchors>();
            health = GetComponent<CombatHealth>();
            gridPosition = GetComponent<CombatGridPosition>();
            move = GetComponent<EnemyMove>();
            block = GetComponent<EnemyBlock>();
            attack = GetComponent<EnemyAttack>();
            summonRuntime = GetComponent<EnemySummonRuntime>();
            isSummon = summonRuntime != null;

            health.OnDied += HandleDied;
        }

        private void OnEnable()
        {
            runtimeId = CombatEvents.AllocateRuntimeId();
            PrepareForSpawn();

            if (isInitialized)
            {
                CombatRegistry.Register(this);
            }
        }

        private void OnDisable()
        {
            if (passiveRuntime != null)
            {
                passiveRuntime.Deactivate();
            }

            if (passiveStatuses != null)
            {
                passiveStatuses.Clear();
            }

            CombatRegistry.Unregister(this);

            if (gridPosition != null)
            {
                gridPosition.Clear();
            }

            isInitialized = false;
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.OnDied -= HandleDied;
            }
        }

        internal void PrepareForSpawn()
        {
            isInitialized = false;

            if (block != null)
            {
                BlockLink.Release(block);
            }

            if (move != null)
            {
                move.PrepareForSpawn();
            }

            if (gridPosition != null)
            {
                gridPosition.Clear();
            }

            InitializeRuntime();
        }

        public void InitializeRuntime()
        {
            if (dataLink == null || !dataLink.HasData || dataLink.EnemyData.BaseStats == null || move == null || block == null || attack == null)
            {
                isInitialized = false;
                Debug.LogError($"{name} 몬스터의 런타임 상태를 초기화할 데이터, EnemyMove, EnemyBlock 또는 EnemyAttack이 없습니다.", this);
                return;
            }

            if (runtimeStats == null)
            {
                runtimeStats = new RuntimeStats();
            }

            if (passiveRuntime == null)
            {
                passiveRuntime = new EnemyPassiveRuntime();
            }

            if (passiveStatuses == null)
            {
                passiveStatuses = new PassiveStatusRuntime();
            }

            if (!runtimeStats.Initialize(dataLink.EnemyData.BaseStats))
            {
                isInitialized = false;
                Debug.LogError($"{name} 몬스터의 RuntimeStats를 초기화하지 못했습니다.", this);
                return;
            }

            deathPublished = false;
            attackProgress.Reset();

            health.Initialize(runtimeStats.MaxHp);
            passiveStatuses.Initialize(runtimeStats);

            isInitialized = true;

            passiveRuntime.Initialize(this, dataLink.EnemyData.Passives);
        }

        public float ApplyDamage(float finalDamage)
        {
            return isInitialized ? health.ApplyDamage(finalDamage) : 0f;
        }

        public float ApplyDamage(DamageInfo damageInfo)
        {
            return isInitialized ? health.ApplyDamage(damageInfo) : 0f;
        }

        public float Heal(float amount)
        {
            return isInitialized ? health.Heal(amount) : 0f;
        }

        internal void StepPassiveRuntime(float deltaTime)
        {
            if (!isInitialized || deltaTime <= 0f || health == null || health.IsDead)
            {
                return;
            }

            passiveStatuses?.Step(deltaTime);
            passiveRuntime?.Step(this, deltaTime);
        }

        public void AdvanceAttackProgress(float finalAttacksPerSecond, float deltaTime)
        {
            if (!isInitialized || health.IsDead)
            {
                return;
            }

            attackProgress.Advance(finalAttacksPerSecond, deltaTime);
        }

        public int ConsumeReadyAttacks(int maxAttackCount)
        {
            return attackProgress.ConsumeReadyAttacks(maxAttackCount);
        }

        private void HandleDied(CombatHealth sender)
        {
            if (deathPublished)
            {
                return;
            }

            deathPublished = true;

            if (passiveRuntime != null)
            {
                passiveRuntime.NotifyDied(this);
                passiveRuntime.Deactivate();
            }

            int rewardGold = 0;
            int rewardExp = 0;

            if (dataLink != null && dataLink.HasData && dataLink.EnemyData != null)
            {
                rewardGold = dataLink.EnemyData.RewardGold;
                rewardExp = dataLink.EnemyData.RewardExp;
            }

            if (!isSummon)
            {
                EnemySize enemySize = dataLink != null && dataLink.HasData ? dataLink.EnemyData.Size : EnemySize.None;
                CombatEvents.PublishEnemyDied(new EnemyDiedInfo(runtimeId, EnemyId, enemySize, transform.position));
            }

            // 중앙 EventBus로 사망 정보 및 보상 발행
            EventBus.Publish(new EnemyDiedEvent(gameObject, EnemyId, rewardGold, rewardExp, transform.position));

            if (summonRuntime != null)
            {
                summonRuntime.Release();
            }
        }
    }
}