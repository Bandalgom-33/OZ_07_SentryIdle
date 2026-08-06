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

        [Tooltip("정적 몬스터 데이터로 런타임 상태가 초기화됐는지 표시합니다.")]
        [SerializeField] private bool isInitialized;

        private EnemyDataLink dataLink;
        private CombatEntityAnchors anchors;
        private CombatHealth health;
        private CombatGridPosition gridPosition;
        private EnemyMove move;
        private bool deathPublished;

        public EnemyDataLink DataLink => dataLink;
        public CombatEntityAnchors Anchors => anchors;
        public CombatHealth Health => health;
        public CombatGridPosition GridPosition => gridPosition;
        public EnemyMove Move => move;
        public string EnemyId => dataLink == null ? string.Empty : dataLink.EnemyId;
        public float AttackProgress => attackProgress.Progress;
        public int ReadyAttackCount => attackProgress.ReadyAttackCount;
        public bool IsInitialized => isInitialized;
        public Vector3 DamageNumberPosition => anchors != null && anchors.EffectPoint != null ? anchors.EffectPoint.position : transform.position;

        private void Awake()
        {
            dataLink = GetComponent<EnemyDataLink>();
            anchors = GetComponent<CombatEntityAnchors>();
            health = GetComponent<CombatHealth>();
            gridPosition = GetComponent<CombatGridPosition>();
            move = GetComponent<EnemyMove>();
            health.OnDied += HandleDied;
            InitializeRuntime();
        }

        private void OnEnable()
        {
            CombatRegistry.Register(this);
        }

        private void OnDisable()
        {
            CombatRegistry.Unregister(this);
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.OnDied -= HandleDied;
            }
        }

        public void InitializeRuntime()
        {
            if (dataLink == null || !dataLink.HasData || dataLink.EnemyData.BaseStats == null)
            {
                isInitialized = false;
                Debug.LogError($"{name} 몬스터의 런타임 상태를 초기화할 데이터가 없습니다.", this);
                return;
            }

            deathPublished = false;
            attackProgress.Reset();
            health.Initialize(dataLink.EnemyData.BaseStats.MaxHp);
            isInitialized = true;
        }

        public float ApplyDamage(float finalDamage)
        {
            return isInitialized ? health.ApplyDamage(finalDamage) : 0f;
        }

        public float Heal(float amount)
        {
            return isInitialized ? health.Heal(amount) : 0f;
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
            CombatEvents.PublishEnemyDied(this);
        }
    }
}