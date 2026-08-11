using System;
using EndlessGuard.Unit.Data;
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
        private const float HealthRegenTickSeconds = 0.1f;
        private const float SkillGaugeRegenTickSeconds = 0.1f;

        [Header("캐릭터 런타임 상태")]
        [Tooltip("기본 공격 실행을 위해 누적되는 공격 진행도입니다.")]
        [SerializeField] private AttackProgressState attackProgress = new AttackProgressState();

        [Tooltip("기준 전투 능력치에 성장, 패시브와 전투 효과를 반영하여 사용하는 런타임 능력치입니다.")]
        [SerializeField] private RuntimeStats runtimeStats = new RuntimeStats();

        [Tooltip("UnitDataSO에 설정된 패시브를 현재 전투에서 실행·관리하는 런타임 상태입니다.")]
        [SerializeField] private UnitPassiveRuntime passiveRuntime = new UnitPassiveRuntime();

        [Tooltip("레벨/승급 진행도에 따라 상위 분류 성장 프로필을 RuntimeStats에 적용하는 상태입니다.")]
        [SerializeField] private UnitGrowthRuntime growthRuntime = new UnitGrowthRuntime();

        [Tooltip("다른 담당의 공통 강화 결과를 RuntimeStats에 적용하는 상태입니다.")]
        [SerializeField] private CommonGrowthRuntime commonGrowthRuntime = new CommonGrowthRuntime();

        private PassiveStatusRuntime passiveStatuses = new PassiveStatusRuntime();
        private UnitProgressData progressData;

        [Tooltip("현재 보유 중인 스킬게이지입니다. 캐릭터 생성 시 0으로 시작합니다.")]
        [Min(0f)]
        [SerializeField] private float currentSkillGauge;

        private bool isInitialized;
        private int runtimeId;

        private UnitDataLink dataLink;
        private CombatEntityAnchors anchors;
        private CombatHealth health;
        private CombatGridPosition gridPosition;
        private UnitBlock block;
        private UnitAttack attack;
        private bool deathPublished;
        private bool isSummon;
        private UnitSummonRuntime summonRuntime;
        private float healthRegenElapsed;
        private float skillGaugeRegenElapsed;

        public event Action<UnitRuntimeState> OnSkillGaugeChanged;

        public UnitDataLink DataLink => dataLink;
        public CombatEntityAnchors Anchors => anchors;
        public CombatHealth Health => health;
        public CombatGridPosition GridPosition => gridPosition;
        public UnitBlock Block => block;
        public UnitAttack Attack => attack;
        public RuntimeStats Stats => runtimeStats;
        public UnitPassiveRuntime Passives => passiveRuntime;
        public UnitGrowthRuntime Growth => growthRuntime;
        public CommonGrowthRuntime CommonGrowth => commonGrowthRuntime;
        public PassiveStatusRuntime Statuses => passiveStatuses;
        public UnitProgressData Progress => progressData;
        public string UnitId => dataLink == null ? string.Empty : dataLink.UnitId;
        public int CurrentLevel => progressData != null ? progressData.CurrentLevel : (dataLink != null && dataLink.HasData ? dataLink.UnitData.InitialLevel : 1);
        public int PromotionStage => progressData != null ? progressData.PromotionStage : 0;
        public int MaxLevel => dataLink != null && dataLink.HasData ? UnitProgressionService.GetMaxLevel(dataLink.UnitData, progressData) : 1;
        public float CurrentSkillGauge => currentSkillGauge;
        public float MaxSkillGauge => dataLink != null && dataLink.HasData ? dataLink.UnitData.MaxSkillGauge : 0f;
        public float NormalizedSkillGauge => MaxSkillGauge > 0f ? currentSkillGauge / MaxSkillGauge : 0f;
        public float AttackProgress => attackProgress.Progress;
        public int ReadyAttackCount => attackProgress.ReadyAttackCount;
        public bool IsInitialized => isInitialized;
        public int RuntimeId => runtimeId;
        public bool IsSummon => isSummon;
        public UnitSummonRuntime SummonRuntime => summonRuntime;
        public Vector3 EffectPosition => anchors != null && anchors.EffectPoint != null ? anchors.EffectPoint.position : transform.position;

        private void Awake()
        {
            dataLink = GetComponent<UnitDataLink>();
            anchors = GetComponent<CombatEntityAnchors>();
            health = GetComponent<CombatHealth>();
            gridPosition = GetComponent<CombatGridPosition>();
            block = GetComponent<UnitBlock>();
            attack = GetComponent<UnitAttack>();
            summonRuntime = GetComponent<UnitSummonRuntime>();
            isSummon = summonRuntime != null;

            health.OnDied += HandleDied;

            if (!isSummon)
            {
                UnitProgressEvents.OnUnitProgressChanged += HandleProgressChanged;
                CommonGrowthService.OnChanged += HandleCommonGrowthChanged;
            }
        }

        private void OnEnable()
        {
            runtimeId = CombatEvents.AllocateRuntimeId();
            InitializeRuntime();

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

            if (!isSummon)
            {
                UnitProgressEvents.OnUnitProgressChanged -= HandleProgressChanged;
                CommonGrowthService.OnChanged -= HandleCommonGrowthChanged;
            }
        }

        public void InitializeRuntime()
        {
            if (dataLink == null || !dataLink.HasData || dataLink.UnitData.BaseStats == null || block == null || attack == null)
            {
                isInitialized = false;
                Debug.LogError($"{name} 캐릭터의 런타임 상태를 초기화할 데이터, UnitBlock 또는 UnitAttack이 없습니다.", this);
                return;
            }

            if (runtimeStats == null)
            {
                runtimeStats = new RuntimeStats();
            }

            if (passiveRuntime == null)
            {
                passiveRuntime = new UnitPassiveRuntime();
            }

            if (growthRuntime == null)
            {
                growthRuntime = new UnitGrowthRuntime();
            }

            if (commonGrowthRuntime == null)
            {
                commonGrowthRuntime = new CommonGrowthRuntime();
            }

            if (passiveStatuses == null)
            {
                passiveStatuses = new PassiveStatusRuntime();
            }

            if (!runtimeStats.Initialize(dataLink.UnitData.BaseStats))
            {
                isInitialized = false;
                Debug.LogError($"{name} 캐릭터의 RuntimeStats를 초기화하지 못했습니다.", this);
                return;
            }

            runtimeStats.SetHpRegenPerSecond(dataLink.UnitData.HpRegenPerSecond);
            runtimeStats.SetCriticalChancePercent(dataLink.UnitData.CriticalChancePercent);
            runtimeStats.SetCriticalDamageBonusPercent(dataLink.UnitData.CriticalDamageBonusPercent);

            if (!isSummon)
            {
                if (progressData == null || !progressData.Matches(dataLink.UnitData))
                {
                    progressData = UnitProgressData.Create(dataLink.UnitData);
                }

                growthRuntime.Apply(runtimeStats, dataLink.UnitData, progressData);
                commonGrowthRuntime.Reset();
                commonGrowthRuntime.ApplyAll(runtimeStats);
            }
            else
            {
                progressData = null;
                commonGrowthRuntime.Reset();
            }

            deathPublished = false;
            healthRegenElapsed = 0f;
            skillGaugeRegenElapsed = 0f;
            attackProgress.Reset();
            currentSkillGauge = 0f;

            health.Initialize(runtimeStats.MaxHp);
            passiveStatuses.Initialize(runtimeStats);

            isInitialized = true;

            passiveRuntime.Initialize(this, dataLink.UnitData.Passives);

            OnSkillGaugeChanged?.Invoke(this);
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

        public bool AddExperience(long gainedExp, out UnitLevelResult result)
        {
            result = default;

            if (isSummon || dataLink == null || !dataLink.HasData)
            {
                return false;
            }

            if (progressData == null || !progressData.Matches(dataLink.UnitData))
            {
                progressData = UnitProgressData.Create(dataLink.UnitData);
            }

            return UnitProgressionService.TryAddExperience(dataLink.UnitData, progressData, gainedExp, out result);
        }

        public bool ApplyApprovedPromotion()
        {
            if (isSummon || dataLink == null || !dataLink.HasData)
            {
                return false;
            }

            if (progressData == null || !progressData.Matches(dataLink.UnitData))
            {
                progressData = UnitProgressData.Create(dataLink.UnitData);
            }

            return UnitProgressionService.ApplyApprovedPromotion(dataLink.UnitData, progressData);
        }

        public bool ApplyProgression(UnitProgressData progress)
        {
            if (isSummon || dataLink == null || !dataLink.HasData || progress == null || !progress.Matches(dataLink.UnitData))
            {
                return false;
            }

            progressData = progress;

            if (!isInitialized || runtimeStats == null || !runtimeStats.IsInitialized)
            {
                return true;
            }

            if (growthRuntime == null)
            {
                growthRuntime = new UnitGrowthRuntime();
            }

            bool applied = growthRuntime.Apply(runtimeStats, dataLink.UnitData, progressData);

            if (applied)
            {
                SyncHealthMaxHpFromStats();
            }

            return applied;
        }

        public bool SetMaxHp(float value)
        {
            if (!isInitialized || runtimeStats == null || !runtimeStats.IsInitialized || health == null || !health.IsInitialized)
            {
                return false;
            }

            float previousMaxHp = runtimeStats.MaxHp;

            runtimeStats.SetMaxHp(value);

            bool statsChanged = !Mathf.Approximately(previousMaxHp, runtimeStats.MaxHp);
            bool healthChanged = health.SetMaxHp(runtimeStats.MaxHp);

            return statsChanged || healthChanged;
        }

        internal bool SyncHealthMaxHpFromStats()
        {
            if (!isInitialized || runtimeStats == null || !runtimeStats.IsInitialized || health == null || !health.IsInitialized)
            {
                return false;
            }

            return health.SetMaxHp(runtimeStats.MaxHp);
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

        internal void StepHealthRegeneration(float deltaTime)
        {
            if (!isInitialized || deltaTime <= 0f || runtimeStats == null || !runtimeStats.IsInitialized || health == null || health.IsDead)
            {
                return;
            }

            if (runtimeStats.HpRegenPerSecond <= 0f || health.CurrentHp >= health.MaxHp)
            {
                healthRegenElapsed = 0f;
                return;
            }

            healthRegenElapsed += deltaTime;

            if (healthRegenElapsed < HealthRegenTickSeconds)
            {
                return;
            }

            int tickCount = Mathf.FloorToInt(healthRegenElapsed / HealthRegenTickSeconds);
            float regenDuration = tickCount * HealthRegenTickSeconds;

            healthRegenElapsed -= regenDuration;

            Heal(runtimeStats.HpRegenPerSecond * regenDuration);
        }

        internal void StepSkillGaugeRegeneration(float deltaTime)
        {
            if (!isInitialized || deltaTime <= 0f || health == null || health.IsDead || dataLink == null || !dataLink.HasData)
            {
                return;
            }

            float regenPerSecond = dataLink.UnitData.SkillGaugeRegenPerSecond;

            if (regenPerSecond <= 0f || MaxSkillGauge <= 0f || currentSkillGauge >= MaxSkillGauge)
            {
                skillGaugeRegenElapsed = 0f;
                return;
            }

            skillGaugeRegenElapsed += deltaTime;

            if (skillGaugeRegenElapsed < SkillGaugeRegenTickSeconds)
            {
                return;
            }

            int tickCount = Mathf.FloorToInt(skillGaugeRegenElapsed / SkillGaugeRegenTickSeconds);
            float regenDuration = tickCount * SkillGaugeRegenTickSeconds;

            skillGaugeRegenElapsed -= regenDuration;

            AddSkillGauge(regenPerSecond * regenDuration);
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

        private void HandleCommonGrowthChanged(GrowthStatMask changedStats)
        {
            if (isSummon || !isInitialized || runtimeStats == null || !runtimeStats.IsInitialized || commonGrowthRuntime == null)
            {
                return;
            }

            commonGrowthRuntime.Apply(runtimeStats, changedStats);

            if ((changedStats & GrowthStatMask.MaxHp) != 0)
            {
                SyncHealthMaxHpFromStats();
            }
        }

        private void HandleProgressChanged(UnitProgressChangedInfo info)
        {
            if (isSummon || dataLink == null || !dataLink.HasData || info.Progress == null || !info.Progress.Matches(dataLink.UnitData))
            {
                return;
            }

            ApplyProgression(info.Progress);
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
                passiveRuntime.Deactivate();
            }

            if (!isSummon)
            {
                CombatEvents.PublishUnitDied(new UnitDiedInfo(runtimeId, UnitId, transform.position));
            }

            if (summonRuntime != null)
            {
                summonRuntime.Release();
            }
        }
    }
}