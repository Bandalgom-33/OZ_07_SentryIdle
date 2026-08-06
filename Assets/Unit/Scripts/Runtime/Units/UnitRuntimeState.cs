using System;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UnitDataLink))]
    [RequireComponent(typeof(CombatEntityAnchors))]
    [RequireComponent(typeof(CombatHealth))]
    [RequireComponent(typeof(CombatGridPosition))]
    public sealed class UnitRuntimeState : MonoBehaviour
    {
        [Header("캐릭터 런타임 상태")]
        [Tooltip("기본 공격 실행을 위해 누적되는 공격 진행도입니다.")]
        [SerializeField] private AttackProgressState attackProgress = new AttackProgressState();

        [Tooltip("현재 보유 중인 스킬게이지입니다. 캐릭터 생성 시 0으로 시작합니다.")]
        [Min(0f)]
        [SerializeField] private float currentSkillGauge;

        [Tooltip("정적 캐릭터 데이터로 런타임 상태가 초기화됐는지 표시합니다.")]
        [SerializeField] private bool isInitialized;

        private UnitDataLink dataLink;
        private CombatEntityAnchors anchors;
        private CombatHealth health;
        private CombatGridPosition gridPosition;
        private UnitBlock block;
        private bool deathPublished;

        public event Action<UnitRuntimeState> OnSkillGaugeChanged;

        public UnitDataLink DataLink => dataLink;
        public CombatEntityAnchors Anchors => anchors;
        public CombatHealth Health => health;
        public CombatGridPosition GridPosition => gridPosition;
        public UnitBlock Block => block;
        public string UnitId => dataLink == null ? string.Empty : dataLink.UnitId;
        public float CurrentSkillGauge => currentSkillGauge;
        public float MaxSkillGauge => dataLink != null && dataLink.HasData ? dataLink.UnitData.MaxSkillGauge : 0f;
        public float NormalizedSkillGauge => MaxSkillGauge > 0f ? currentSkillGauge / MaxSkillGauge : 0f;
        public float AttackProgress => attackProgress.Progress;
        public int ReadyAttackCount => attackProgress.ReadyAttackCount;
        public bool IsInitialized => isInitialized;
        public Vector3 EffectPosition => anchors != null && anchors.EffectPoint != null ? anchors.EffectPoint.position : transform.position;

        private void Awake()
        {
            dataLink = GetComponent<UnitDataLink>();
            anchors = GetComponent<CombatEntityAnchors>();
            health = GetComponent<CombatHealth>();
            gridPosition = GetComponent<CombatGridPosition>();
            block = GetComponent<UnitBlock>();
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
            if (dataLink == null || !dataLink.HasData || dataLink.UnitData.BaseStats == null || block == null)
            {
                isInitialized = false;
                Debug.LogError($"{name} 캐릭터의 런타임 상태를 초기화할 데이터 또는 UnitBlock이 없습니다.", this);
                return;
            }

            deathPublished = false;
            attackProgress.Reset();
            currentSkillGauge = 0f;
            health.Initialize(dataLink.UnitData.BaseStats.MaxHp);
            isInitialized = true;
            OnSkillGaugeChanged?.Invoke(this);
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

        public float AddSkillGauge(float amount)
        {
            if (!isInitialized || health.IsDead || amount <= 0f || MaxSkillGauge <= 0f)
            {
                return 0f;
            }

            float previousGauge = currentSkillGauge;
            currentSkillGauge = Mathf.Min(MaxSkillGauge, currentSkillGauge + amount);
            float addedGauge = currentSkillGauge - previousGauge;

            if (addedGauge > 0f)
            {
                OnSkillGaugeChanged?.Invoke(this);
            }

            return addedGauge;
        }

        public bool TryConsumeSkillGauge(float amount)
        {
            if (!isInitialized || health.IsDead || amount < 0f || currentSkillGauge < amount)
            {
                return false;
            }

            currentSkillGauge -= amount;
            OnSkillGaugeChanged?.Invoke(this);
            return true;
        }

        private void HandleDied(CombatHealth sender)
        {
            if (deathPublished)
            {
                return;
            }

            deathPublished = true;
            CombatEvents.PublishUnitDied(this);
        }
    }
}