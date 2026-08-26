using System;
using System.Collections;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Raid.Data;
using EndlessGuard.Unit.Runtime;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    public enum RaidBattleState
    {
        Idle = 0,
        Running = 1,
        Transitioning = 2,
        Victory = 3,
        Defeat = 4
    }

    public enum RaidBattleResult
    {
        Victory = 0,
        Defeat = 1
    }

    public enum RaidBattleMode
    {
        Auto = 0,
        Manual = 1
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(CombatLoop))]
    [RequireComponent(typeof(RaidBoardRuntime))]
    [RequireComponent(typeof(RaidRouteGuideView))]
    public sealed class RaidBattleController : MonoBehaviour
    {
        private const int CurrentTransitionTuningVersion = 3;

        [Header("레이드 데이터")]
        [Tooltip("레이드 보스 HP, 제한시간, 두 게이지와 Phase HP 기준을 보관합니다. 현재 수치는 개발 튜닝값이며 최종 밸런스에서 조정합니다.")]
        [SerializeField] private RaidBattleConfigSO config;

        [Header("레이드 진행 상태")]
        [Tooltip("현재 레이드 전투의 진행 상태입니다.")]
        [SerializeField] private RaidBattleState state = RaidBattleState.Idle;
        [SerializeField] private RaidBattleMode mode = RaidBattleMode.Auto;

        [Header("런타임 상태")]
        [SerializeField] private float currentBossHp;
        [SerializeField] private float remainingTime;
        [SerializeField] private float bossSkillGauge;
        [SerializeField] private float raidAttackGauge;
        [SerializeField] private int currentCost;
        [SerializeField] private int selectedTeamIndex;

        [Header("페이즈 전환")]
        [Tooltip("Phase 붕괴 연출 전체 시간입니다. 전투와 타이머/게이지는 이 구간 동안 잠깁니다.")]
        [Min(0.5f)] [SerializeField] private float phaseTransitionDuration = 3.2f;
        [Tooltip("기존 Board를 다음 Phase Board로 확정하는 시점입니다.")]
        [Min(0.1f)] [SerializeField] private float phaseCommitTime = 2.55f;
        [Tooltip("전환 시작 순간의 짧고 강한 카메라 충격 세기입니다.")]
        [Min(0f)] [SerializeField] private float impactShake = 0.92f;
        [Tooltip("붕괴가 진행되는 동안 지속되는 약한 카메라 진동 세기입니다.")]
        [Min(0f)] [SerializeField] private float rumbleShake = 0.14f;
        [SerializeField, HideInInspector] private int phaseTransitionTuningVersion;

        private CombatLoop combatLoop;
        private RaidBoardRuntime board;
        private RaidRouteGuideView routeGuide;
        private RaidPhaseTransitionRuntime transition;
        private Coroutine transitionRoutine;
        private Coroutine raidStartRoutine;
        private Coroutine phaseRouteGuideRoutine;
        private RaidPhaseTransitionPlan activePlan;
        private bool bossSkillReadyRaised;
        private bool raidAttackReadyRaised;
        private bool raidAttackActive;
        private bool manualRaidAttackHeld;
        private int lastPublishedTimerSecond = -1;
        private float costRegenAccumulator;
        private Coroutine bossSkillRoutine;
        private readonly List<UnitRuntimeState> bossSkillTargets = new List<UnitRuntimeState>(16);

        [Header("Boss 표시")]
        [Tooltip("보스 피해 숫자를 표시할 기준 Transform입니다. 현재 씬에서는 RaidBoss/BossGuide를 연결합니다.")]
        [SerializeField] private Transform bossDamageAnchor;

        private Renderer bossDamageRenderer;
        private RaidContributionRuntime contribution;
        private int bossDamageNumberSequence;
        private readonly List<UnitRuntimeState> raidAttackParticipants = new List<UnitRuntimeState>(16);
        private readonly Dictionary<int, float> raidAttackUnitTimers = new Dictionary<int, float>(16);
        private readonly Dictionary<int, RaidAttackRepeatState> raidAttackRepeatStates = new Dictionary<int, RaidAttackRepeatState>(16);
        private float raidAttackSessionDamage;
        private int raidAttackSessionMaxParticipants;
        private bool raidCombatPrepared;

        public RaidBattleConfigSO Config => config;
        public RaidBattleState State => state;
        public RaidBattleMode Mode => mode;
        public bool IsRunning => state == RaidBattleState.Running;
        public bool IsPreparing => raidStartRoutine != null;
        public bool IsTransitioning => state == RaidBattleState.Transitioning;
        public string BossDisplayName => config != null ? config.BossDisplayName : "RAID BOSS";
        public float BossMaxHp => config != null ? config.BossMaxHp : 1f;
        public float CurrentBossHp => currentBossHp;
        public float BossHpRatio => BossMaxHp > 0f ? Mathf.Clamp01(currentBossHp / BossMaxHp) : 0f;
        public float TimeLimit => config != null ? config.TimeLimitSeconds : 0f;
        public float RemainingTime => remainingTime;
        public float BossSkillGauge => bossSkillGauge;
        public float BossSkillGaugeMax => config != null ? config.BossSkillGaugeMax : 1f;
        public float BossSkillGaugeRatio => BossSkillGaugeMax > 0f ? Mathf.Clamp01(bossSkillGauge / BossSkillGaugeMax) : 0f;
        public bool IsBossSkillReady => bossSkillGauge >= BossSkillGaugeMax - 0.0001f;
        public bool IsBossSkillCasting => bossSkillRoutine != null;
        public float RaidAttackGauge => raidAttackGauge;
        public float RaidAttackGaugeMax => config != null ? config.RaidAttackGaugeMax : 1f;
        public float RaidAttackGaugeRatio => RaidAttackGaugeMax > 0f ? Mathf.Clamp01(raidAttackGauge / RaidAttackGaugeMax) : 0f;
        public bool IsRaidAttackReady => raidAttackGauge >= RaidAttackGaugeMax - 0.0001f;
        public bool IsRaidAttackRequestPending => false;
        public bool IsRaidAttackCasting => raidAttackActive;
        public bool IsRaidAttackActive => raidAttackActive;
        public bool IsManualRaidAttackHeld => manualRaidAttackHeld;
        public int RaidAttackParticipantCount => CountEligibleRaidAttackParticipants();
        public bool CanRequestRaidAttack => CanStartManualRaidAttack;
        public bool CanStartManualRaidAttack => state == RaidBattleState.Running && mode == RaidBattleMode.Manual && raidAttackGauge > 0.0001f && !raidAttackActive && RaidAttackParticipantCount > 0;
        public int CurrentCost => currentCost;
        public int CostMax => config != null ? config.CostMax : 100;
        public int SelectedTeamIndex => selectedTeamIndex;
        public RaidPhase CurrentPhase => board != null ? board.Phase : RaidPhase.Phase1;

        public event Action OnRaidPreparing;
        public bool IsRouteGuidePlaying => routeGuide != null && routeGuide.IsPlaying;

        public event Action OnRaidStarted;
        public event Action<RaidBattleResult> OnRaidEnded;
        public event Action<RaidBattleState> OnStateChanged;
        public event Action<RaidBattleMode> OnModeChanged;
        public event Action<float, float> OnBossHpChanged;
        public event Action<float> OnTimeChanged;
        public event Action<float, float> OnBossSkillGaugeChanged;
        public event Action<float, float> OnRaidAttackGaugeChanged;
        public event Action<int, int> OnCostChanged;
        public event Action<int> OnSelectedTeamChanged;
        public event Action OnBossSkillReady;
        public event Action OnBossSkillCastStarted;
        public event Action<UnitRuntimeState, int, int> OnBossSkillUnitStrikeStarted;
        public event Action<UnitRuntimeState, int, int> OnBossSkillUnitStruck;
        public event Action<int, float> OnBossSkillCastResolved;
        public event Action OnRaidAttackReady;
        public event Action OnRaidAttackRequested;
        public event Action<int> OnRaidAttackCastStarted;
        public event Action<UnitRuntimeState, int, int> OnRaidAttackUnitFired;
        public event Action<int, float> OnRaidAttackCastResolved;
        public event Action<RaidPhaseTransitionInfo> OnPhaseTransitionStarted;
        public event Action<RaidPhaseTransitionInfo> OnPhaseTransitionCompleted;
        public event Action<RaidForcedRetreatInfo> OnUnitForcedRetreat;
        public event Action<RaidEnemyPhaseRemovalInfo> OnEnemyRemovedByPhase;

        private void Awake()
        {
            ApplyTransitionTuningMigration();
            combatLoop = GetComponent<CombatLoop>();
            board = GetComponent<RaidBoardRuntime>();
            routeGuide = GetComponent<RaidRouteGuideView>();
            contribution = RaidContributionRuntime.EnsureInstalled(gameObject);
            RaidBossLightningRuntime.EnsureInstalled(gameObject);

            RaidSummonTileProvider.EnsureInstalled(gameObject);
            Camera mainCamera = Camera.main;
            transition = new RaidPhaseTransitionRuntime(board, mainCamera);
            transition.CancelVisuals(false);
            ResolveBossDamageAnchor();

            combatLoop.StopLoop();
            state = RaidBattleState.Idle;
            InitializeRuntimeValues();
        }

        private void OnEnable()
        {
            CombatEvents.OnEnemyDied += HandleEnemyDied;
            CombatEvents.OnEnemyReachedGoal += HandleEnemyReachedGoal;
            PassiveRuntimeEvents.OnSummonCostGainRequested += HandleSummonCostGainRequested;
        }

        private void OnDisable()
        {
            CombatEvents.OnEnemyDied -= HandleEnemyDied;
            CombatEvents.OnEnemyReachedGoal -= HandleEnemyReachedGoal;
            PassiveRuntimeEvents.OnSummonCostGainRequested -= HandleSummonCostGainRequested;
            CancelRaidStartSequence();
            StopPhaseRouteGuide();
            StopBossSkillCast();
            StopRaidAttackCast();

            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
                transitionRoutine = null;
            }

            activePlan = null;
            transition?.CancelVisuals(false);

            if (combatLoop != null)
            {
                combatLoop.StopLoop();
            }
        }

        private void Update()
        {
            if (state != RaidBattleState.Running)
            {
                if (IsPreparing)
                {
                    TickCost(Time.deltaTime);
                }

                return;
            }

            if (remainingTime <= 0f)
            {
                PublishZeroTime();
                EndRaid(RaidBattleResult.Defeat);
                return;
            }

            TickCost(Time.deltaTime);
            TickRaidAttack(Time.deltaTime);

            float previousTime = remainingTime;
            remainingTime = Mathf.Max(0f, remainingTime - Time.deltaTime);

            int currentSecond = Mathf.CeilToInt(remainingTime);
            if (currentSecond != lastPublishedTimerSecond || previousTime > 0f && remainingTime <= 0f)
            {
                lastPublishedTimerSecond = currentSecond;
                OnTimeChanged?.Invoke(remainingTime);
            }

            if (remainingTime <= 0f)
            {
                PublishZeroTime();
                EndRaid(RaidBattleResult.Defeat);
            }
        }

        private void PublishZeroTime()
        {
            remainingTime = 0f;

            if (lastPublishedTimerSecond != 0)
            {
                lastPublishedTimerSecond = 0;
                OnTimeChanged?.Invoke(0f);
            }
        }

        private static void FreezeCombatActorsAtEnd()
        {
            foreach (EnemyRuntimeState enemy in CombatRegistry.Enemies)
            {
                if (enemy == null ||
                    !enemy.IsInitialized ||
                    enemy.Move == null)
                {
                    continue;
                }

                enemy.Move.SetPaused(true);
            }
        }

        private void OnValidate()
        {
            ApplyTransitionTuningMigration();
            phaseTransitionDuration = Mathf.Max(0.5f, phaseTransitionDuration);
            phaseCommitTime = Mathf.Clamp(phaseCommitTime, 0.1f, Mathf.Max(0.1f, phaseTransitionDuration - 0.05f));
            impactShake = Mathf.Max(0f, impactShake);
            rumbleShake = Mathf.Max(0f, rumbleShake);
        }

        private void ApplyTransitionTuningMigration()
        {
            if (phaseTransitionTuningVersion >= CurrentTransitionTuningVersion)
            {
                return;
            }

            phaseTransitionDuration = 3.2f;
            phaseCommitTime = 2.55f;
            impactShake = 0.92f;
            rumbleShake = 0.14f;
            phaseTransitionTuningVersion = CurrentTransitionTuningVersion;
        }

        public bool BeginRaid()
        {
            if (state == RaidBattleState.Running || state == RaidBattleState.Transitioning || raidStartRoutine != null || !ValidateConfig())
            {
                return false;
            }

            if (board == null || board.Board == null)
            {
                Debug.LogError("Raid Board가 준비되지 않아 레이드를 시작할 수 없습니다.", this);
                return false;
            }

            if (board.Phase != RaidPhase.Phase1)
            {
                board.BuildPhase(RaidPhase.Phase1);
            }

            InitializeRuntimeValues();
            raidCombatPrepared = false;
            combatLoop.StopLoop();
            SetState(RaidBattleState.Idle);
            PublishAllRuntimeValues();

            if (routeGuide != null && routeGuide.CanPlay(board))
            {
                raidStartRoutine = StartCoroutine(RunRaidStartSequence());
            }
            else
            {
                StartRaidCombat();
            }

            return true;
        }

        private IEnumerator RunRaidStartSequence()
        {
            yield return routeGuide.Play(board, PrepareRaidCombat);
            raidStartRoutine = null;

            if (!isActiveAndEnabled || state != RaidBattleState.Idle)
            {
                raidCombatPrepared = false;
                yield break;
            }

            StartRaidCombat();
        }

        private void PrepareRaidCombat()
        {
            if (raidCombatPrepared)
            {
                return;
            }

            raidCombatPrepared = true;
            OnRaidPreparing?.Invoke();
        }

        private void StartRaidCombat()
        {
            PrepareRaidCombat();
            combatLoop.StartLoop();
            SetState(RaidBattleState.Running);
            PublishAllRuntimeValues();
            OnRaidStarted?.Invoke();
            raidCombatPrepared = false;
        }

        private void CancelRaidStartSequence()
        {
            if (raidStartRoutine != null)
            {
                StopCoroutine(raidStartRoutine);
                raidStartRoutine = null;
            }

            if (routeGuide != null)
            {
                routeGuide.StopImmediate();
            }

            raidCombatPrepared = false;
        }

        public bool EndRaid(RaidBattleResult result)
        {
            if (state != RaidBattleState.Running)
            {
                return false;
            }

            if (result == RaidBattleResult.Defeat)
            {
                remainingTime = 0f;
                PublishZeroTime();
            }

            StopBossSkillCast();
            StopPhaseRouteGuide();
            routeGuide?.StopImmediate();
            StopRaidAttackCast();
            combatLoop.StopLoop();
            FreezeCombatActorsAtEnd();

            RaidBattleState endState = result == RaidBattleResult.Victory ? RaidBattleState.Victory : RaidBattleState.Defeat;
            SetState(endState);
            OnRaidEnded?.Invoke(result);
            return true;
        }

        public float ApplyBossDamage(float damage)
        {
            return ApplyBossDamage(new DamageInfo(damage, DamageType.None, false));
        }

        public float ApplyBossDamage(DamageInfo damageInfo)
        {
            if (state != RaidBattleState.Running || damageInfo.FinalDamage <= 0f || currentBossHp <= 0f)
            {
                return 0f;
            }

            float previousHp = currentBossHp;
            currentBossHp = Mathf.Max(0f, currentBossHp - damageInfo.FinalDamage);
            float appliedDamage = previousHp - currentBossHp;
            OnBossHpChanged?.Invoke(currentBossHp, BossMaxHp);
            ShowBossDamageNumber(damageInfo, appliedDamage);

            if (currentBossHp <= 0f)
            {
                EndRaid(RaidBattleResult.Victory);
                return appliedDamage;
            }

            TryStartHpPhaseTransition();
            return appliedDamage;
        }

        public float AddBossSkillGauge(float amount)
        {
            if (state != RaidBattleState.Running || amount <= 0f || IsBossSkillReady)
            {
                return 0f;
            }

            float previous = bossSkillGauge;
            bossSkillGauge = Mathf.Min(BossSkillGaugeMax, bossSkillGauge + amount);
            float added = bossSkillGauge - previous;
            OnBossSkillGaugeChanged?.Invoke(bossSkillGauge, BossSkillGaugeMax);

            if (IsBossSkillReady && !bossSkillReadyRaised)
            {
                bossSkillReadyRaised = true;
                OnBossSkillReady?.Invoke();
                TryStartBossSkillCast();
            }

            return added;
        }

        public bool ConsumeBossSkillGauge()
        {
            if (!IsBossSkillReady)
            {
                return false;
            }

            bossSkillGauge = 0f;
            bossSkillReadyRaised = false;
            OnBossSkillGaugeChanged?.Invoke(bossSkillGauge, BossSkillGaugeMax);
            return true;
        }

        public float AddRaidAttackGauge(float amount)
        {
            if (state != RaidBattleState.Running || amount <= 0f || raidAttackActive || raidAttackGauge >= RaidAttackGaugeMax)
            {
                return 0f;
            }

            float previous = raidAttackGauge;
            raidAttackGauge = Mathf.Min(RaidAttackGaugeMax, raidAttackGauge + amount);
            float added = raidAttackGauge - previous;

            if (added > 0f)
            {
                OnRaidAttackGaugeChanged?.Invoke(raidAttackGauge, RaidAttackGaugeMax);
            }

            if (IsRaidAttackReady && !raidAttackReadyRaised)
            {
                raidAttackReadyRaised = true;
                OnRaidAttackReady?.Invoke();
            }

            if (mode == RaidBattleMode.Auto && IsRaidAttackReady && !raidAttackActive)
            {
                StartRaidAttackSession(false);
            }

            return added;
        }

        public bool RequestRaidAttack()
        {
            return BeginManualRaidAttack();
        }

        public bool BeginManualRaidAttack()
        {
            manualRaidAttackHeld = true;

            if (!CanStartManualRaidAttack)
            {
                return false;
            }

            OnRaidAttackRequested?.Invoke();
            return StartRaidAttackSession(true);
        }

        public void EndManualRaidAttack()
        {
            manualRaidAttackHeld = false;

            if (mode == RaidBattleMode.Manual && raidAttackActive)
            {
                StopRaidAttackSession(true);
            }
        }

        public bool ConsumeRaidAttackGauge()
        {
            if (raidAttackGauge <= 0f)
            {
                return false;
            }

            raidAttackGauge = 0f;
            raidAttackReadyRaised = false;
            raidAttackActive = false;
            manualRaidAttackHeld = false;
            raidAttackUnitTimers.Clear();
            raidAttackRepeatStates.Clear();
            raidAttackSessionDamage = 0f;
            raidAttackSessionMaxParticipants = 0;
            OnRaidAttackGaugeChanged?.Invoke(raidAttackGauge, RaidAttackGaugeMax);
            return true;
        }

        public void SetMode(RaidBattleMode nextMode)
        {
            if (mode == nextMode)
            {
                return;
            }

            if (raidAttackActive)
            {
                StopRaidAttackSession(true);
            }

            manualRaidAttackHeld = false;
            mode = nextMode;
            OnModeChanged?.Invoke(mode);

            if (mode == RaidBattleMode.Auto && IsRaidAttackReady)
            {
                StartRaidAttackSession(false);
            }
        }

        public int AddCost(int amount)
        {
            if (state != RaidBattleState.Running)
            {
                return 0;
            }

            return ApplyCostGain(amount);
        }

        public bool TrySpendCost(int amount)
        {
            if (state != RaidBattleState.Running || amount <= 0 || currentCost < amount)
            {
                return false;
            }

            currentCost -= amount;
            OnCostChanged?.Invoke(currentCost, CostMax);
            return true;
        }

        public void SetSelectedTeam(int teamIndex)
        {
            int next = Mathf.Clamp(teamIndex, 0, 1);

            if (selectedTeamIndex == next)
            {
                return;
            }

            selectedTeamIndex = next;
            OnSelectedTeamChanged?.Invoke(selectedTeamIndex);
        }

        public bool TryTransitionToNextPhase()
        {
            if (board == null)
            {
                return false;
            }

            switch (board.Phase)
            {
                case RaidPhase.Phase1:
                    return TryTransitionTo(RaidPhase.Phase2);
                case RaidPhase.Phase2:
                    return TryTransitionTo(RaidPhase.Phase3);
                default:
                    return false;
            }
        }

        public bool TryTransitionTo(RaidPhase nextPhase)
        {
            if (state != RaidBattleState.Running || transitionRoutine != null || board == null || board.Board == null)
            {
                return false;
            }

            if (!RaidPhaseTransitionPlan.TryCreate(board, nextPhase, out RaidPhaseTransitionPlan plan, out string error))
            {
                Debug.LogError(error, this);
                return false;
            }

            activePlan = plan;
            transitionRoutine = StartCoroutine(RunPhaseTransition(plan));
            return true;
        }

        public void ResetRaid()
        {
            CancelRaidStartSequence();
            StopPhaseRouteGuide();
            StopBossSkillCast();
            StopRaidAttackCast();

            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
                transitionRoutine = null;
            }

            activePlan = null;
            transition?.CancelVisuals(true);
            combatLoop.StopLoop();

            if (board != null && board.Board != null && board.Phase != RaidPhase.Phase1)
            {
                board.BuildPhase(RaidPhase.Phase1);
            }

            InitializeRuntimeValues();
            SetState(RaidBattleState.Idle);
            PublishAllRuntimeValues();
        }

        private IEnumerator RunPhaseTransition(RaidPhaseTransitionPlan plan)
        {
            bool resumeAutoRaidAttack =
                mode == RaidBattleMode.Auto &&
                raidAttackActive &&
                raidAttackGauge > 0.0001f;

            StopPhaseRouteGuide();
            routeGuide?.StopImmediate();
            StopRaidAttackCast();
            combatLoop.StopLoop();
            SetState(RaidBattleState.Transitioning);
            transition.CaptureActors(plan);

            RaidMapFamilySO family = board != null ? board.Family : null;
            float transitionImpactScale = family != null ? family.GetTransitionImpactScale(plan.ToPhase) : 1f;
            float transitionRumbleScale = family != null ? family.GetTransitionRumbleScale(plan.ToPhase) : 1f;
            float transitionFxScale = family != null ? family.GetTransitionCollapseFxScale(plan.ToPhase) : 1f;
            RaidPhaseTransitionInfo info = new RaidPhaseTransitionInfo(plan.FromPhase, plan.ToPhase, plan.CollapsingTileCount, phaseTransitionDuration);
            OnPhaseTransitionStarted?.Invoke(info);

            yield return transition.Play(plan, board.Board, phaseTransitionDuration, phaseCommitTime, impactShake * transitionImpactScale, rumbleShake * transitionRumbleScale, transitionFxScale, CommitActivePhase);

            transitionRoutine = null;

            if (!transition.LastCommitSucceeded)
            {
                activePlan = null;
                combatLoop.StartLoop();
                SetState(RaidBattleState.Running);

                if (resumeAutoRaidAttack)
                {
                    StartRaidAttackSession(false, true);
                }

                Debug.LogError($"Raid Phase 전환을 완료하지 못했습니다. Current Phase: {board.Phase}", this);
                yield break;
            }

            activePlan = null;
            combatLoop.StartLoop();
            SetState(RaidBattleState.Running);

            if (resumeAutoRaidAttack)
            {
                StartRaidAttackSession(false, true);
            }

            OnPhaseTransitionCompleted?.Invoke(info);
            StartPhaseRouteGuide(plan.SourceMap);
            TryStartHpPhaseTransition();
        }

        private void StartPhaseRouteGuide(RaidMapSO previousMap)
        {
            if (board == null || board.Family == null || !board.Family.ShowRiftEntries || routeGuide == null || previousMap == null || !routeGuide.CanPlayNewEntries(board, previousMap))
            {
                return;
            }

            StopPhaseRouteGuide();
            phaseRouteGuideRoutine = StartCoroutine(RunPhaseRouteGuide(previousMap));
        }

        private IEnumerator RunPhaseRouteGuide(RaidMapSO previousMap)
        {
            yield return routeGuide.PlayNewEntries(board, previousMap);
            phaseRouteGuideRoutine = null;
        }

        private void StopPhaseRouteGuide()
        {
            if (phaseRouteGuideRoutine != null)
            {
                StopCoroutine(phaseRouteGuideRoutine);
                phaseRouteGuideRoutine = null;
            }

            if (routeGuide != null && state != RaidBattleState.Idle)
            {
                routeGuide.StopImmediate();
            }
        }

        private bool CommitActivePhase()
        {
            if (activePlan == null)
            {
                return false;
            }

            RaidPhase sourcePhase = activePlan.FromPhase;

            try
            {
                board.BuildPhase(activePlan.ToPhase);
                transition.CommitActors(activePlan, board.Board, PublishForcedRetreat, PublishEnemyRemoval);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);

                try
                {
                    if (board.Phase != sourcePhase)
                    {
                        board.BuildPhase(sourcePhase);
                    }
                }
                catch (Exception rollbackException)
                {
                    Debug.LogException(rollbackException, this);
                }

                return false;
            }
        }

        private void TryStartBossSkillCast()
        {
            if (state != RaidBattleState.Running || config == null || !IsBossSkillReady || bossSkillRoutine != null)
            {
                return;
            }

            bossSkillRoutine = StartCoroutine(RunBossSkillCast());
        }

        private IEnumerator RunBossSkillCast()
        {
            OnBossSkillCastStarted?.Invoke();
            float elapsed = 0f;
            float castDelay = config != null ? config.BossSkillCastDelay : 0f;

            while (elapsed < castDelay)
            {
                if (state == RaidBattleState.Victory || state == RaidBattleState.Defeat || state == RaidBattleState.Idle)
                {
                    bossSkillRoutine = null;
                    yield break;
                }

                if (state == RaidBattleState.Running)
                {
                    elapsed += Time.deltaTime;
                }

                yield return null;
            }

            bossSkillTargets.Clear();

            foreach (UnitRuntimeState unit in CombatRegistry.Units)
            {
                if (unit == null || !unit.IsInitialized || unit.Health == null || unit.Health.IsDead || unit.GridPosition == null || !unit.GridPosition.IsInitialized)
                {
                    continue;
                }

                bossSkillTargets.Add(unit);
            }

            ShuffleBossSkillTargets();

            int hitCount = 0;
            float totalDamage = 0f;
            float damageRatio = config != null ? config.BossSkillMaxHpDamageRatio : 0f;
            float minimumDamage = config != null ? config.BossSkillMinimumDamage : 0f;
            float strikeInterval = config != null ? config.BossSkillStrikeInterval : 0f;
            int strikeCount = bossSkillTargets.Count;

            for (int i = 0; i < strikeCount; i++)
            {
                UnitRuntimeState unit = bossSkillTargets[i];

                if (unit == null || !unit.IsInitialized || unit.Health == null || unit.Health.IsDead)
                {
                    continue;
                }

                OnBossSkillUnitStrikeStarted?.Invoke(unit, i, strikeCount);

                float strikeTelegraphDuration = config != null ? config.BossSkillStrikeTelegraphDuration : 0f;
                if (strikeTelegraphDuration > 0f)
                {
                    float strikeTelegraphElapsed = 0f;
                    while (strikeTelegraphElapsed < strikeTelegraphDuration)
                    {
                        if (state == RaidBattleState.Victory || state == RaidBattleState.Defeat || state == RaidBattleState.Idle)
                        {
                            bossSkillTargets.Clear();
                            bossSkillRoutine = null;
                            yield break;
                        }

                        if (state == RaidBattleState.Running)
                        {
                            strikeTelegraphElapsed += Time.deltaTime;
                        }

                        yield return null;
                    }
                }

                OnBossSkillUnitStruck?.Invoke(unit, i, strikeCount);

                float damage = Mathf.Max(minimumDamage, unit.Stats.MaxHp * damageRatio);
                float applied = unit.ApplyDamage(damage);

                if (applied > 0f)
                {
                    hitCount++;
                    totalDamage += applied;
                }

                if (i < strikeCount - 1 && strikeInterval > 0f)
                {
                    float strikeElapsed = 0f;
                    while (strikeElapsed < strikeInterval)
                    {
                        if (state == RaidBattleState.Victory || state == RaidBattleState.Defeat || state == RaidBattleState.Idle)
                        {
                            bossSkillTargets.Clear();
                            bossSkillRoutine = null;
                            yield break;
                        }

                        if (state == RaidBattleState.Running)
                        {
                            strikeElapsed += Time.deltaTime;
                        }

                        yield return null;
                    }
                }
            }

            bossSkillTargets.Clear();
            ConsumeBossSkillGauge();
            bossSkillRoutine = null;
            OnBossSkillCastResolved?.Invoke(hitCount, totalDamage);
        }

        private void ShuffleBossSkillTargets()
        {
            for (int i = bossSkillTargets.Count - 1; i > 0; i--)
            {
                int swapIndex = UnityEngine.Random.Range(0, i + 1);
                UnitRuntimeState temp = bossSkillTargets[i];
                bossSkillTargets[i] = bossSkillTargets[swapIndex];
                bossSkillTargets[swapIndex] = temp;
            }
        }

        private void StopBossSkillCast()
        {
            if (bossSkillRoutine != null)
            {
                StopCoroutine(bossSkillRoutine);
                bossSkillRoutine = null;
            }

            bossSkillTargets.Clear();
        }

        private void HandleEnemyDied(EnemyDiedInfo info)
        {
            if (config != null)
            {
                AddRaidAttackGauge(config.EnemyKillRaidGaugeGain);
            }
        }

        private void HandleEnemyReachedGoal(EnemyReachedGoalInfo info)
        {
            if (config != null)
            {
                AddBossSkillGauge(config.GoalSkillGaugeGain);
            }

            RemoveReachedGoalEnemy(info.RuntimeId);
        }

        private void RemoveReachedGoalEnemy(int runtimeId)
        {
            EnemyRuntimeState target = null;

            foreach (EnemyRuntimeState enemy in CombatRegistry.Enemies)
            {
                if (enemy != null && enemy.RuntimeId == runtimeId)
                {
                    target = enemy;
                    break;
                }
            }

            if (target == null)
            {
                return;
            }

            GameObject instance = target.gameObject;
            if (!RaidEnemyPool.Release(instance))
            {
                instance.SetActive(false);
                Destroy(instance);
            }
        }

        private void TickRaidAttack(float deltaTime)
        {
            if (state != RaidBattleState.Running || deltaTime <= 0f)
            {
                return;
            }

            if (!raidAttackActive)
            {
                if (mode == RaidBattleMode.Auto && IsRaidAttackReady && RaidAttackParticipantCount > 0)
                {
                    StartRaidAttackSession(false);
                }

                return;
            }

            if (mode == RaidBattleMode.Manual && !manualRaidAttackHeld)
            {
                StopRaidAttackSession(true);
                return;
            }

            raidAttackParticipants.Clear();
            CollectEligibleRaidAttackParticipants(raidAttackParticipants);

            if (raidAttackParticipants.Count <= 0)
            {
                StopRaidAttackSession(true);
                return;
            }

            raidAttackSessionMaxParticipants = Mathf.Max(raidAttackSessionMaxParticipants, raidAttackParticipants.Count);

            float fullDuration = config != null ? config.RaidAttackFullGaugeDuration : 10f;
            float drainPerSecond = RaidAttackGaugeMax / Mathf.Max(1f, fullDuration);
            float previousGauge = raidAttackGauge;
            raidAttackGauge = Mathf.Max(0f, raidAttackGauge - drainPerSecond * deltaTime);

            if (!Mathf.Approximately(previousGauge, raidAttackGauge))
            {
                OnRaidAttackGaugeChanged?.Invoke(raidAttackGauge, RaidAttackGaugeMax);
            }

            TickRaidAttackParticipants(deltaTime);

            if (raidAttackGauge <= 0.0001f)
            {
                raidAttackGauge = 0f;
                raidAttackReadyRaised = false;
                OnRaidAttackGaugeChanged?.Invoke(raidAttackGauge, RaidAttackGaugeMax);
                StopRaidAttackSession(false);
            }
        }

        private bool StartRaidAttackSession(bool manual, bool resumeAutoSession = false)
        {
            if (raidAttackActive || state != RaidBattleState.Running || raidAttackGauge <= 0.0001f || RaidAttackParticipantCount <= 0)
            {
                return false;
            }

            if (!manual)
            {
                if (mode != RaidBattleMode.Auto)
                {
                    return false;
                }

                if (!resumeAutoSession && !IsRaidAttackReady)
                {
                    return false;
                }
            }

            if (manual && mode != RaidBattleMode.Manual)
            {
                return false;
            }

            raidAttackActive = true;
            raidAttackSessionDamage = 0f;
            raidAttackUnitTimers.Clear();
            raidAttackRepeatStates.Clear();

            raidAttackParticipants.Clear();
            CollectEligibleRaidAttackParticipants(raidAttackParticipants);
            raidAttackSessionMaxParticipants = raidAttackParticipants.Count;

            for (int i = 0; i < raidAttackParticipants.Count; i++)
            {
                UnitRuntimeState unit = raidAttackParticipants[i];

                if (unit != null)
                {
                    raidAttackUnitTimers[unit.RuntimeId] = i * 0.055f;
                }
            }

            OnRaidAttackCastStarted?.Invoke(raidAttackSessionMaxParticipants);
            return true;
        }

        private void TickRaidAttackParticipants(float deltaTime)
        {
            for (int i = 0; i < raidAttackParticipants.Count && raidAttackActive; i++)
            {
                UnitRuntimeState unit = raidAttackParticipants[i];

                if (!IsEligibleRaidAttackParticipant(unit))
                {
                    continue;
                }

                int runtimeId = unit.RuntimeId;

                if (!raidAttackUnitTimers.TryGetValue(runtimeId, out float timer))
                {
                    timer = i * 0.055f;
                }

                timer -= deltaTime;
                float attacksPerSecond = unit.Stats != null ? Mathf.Max(0f, unit.Stats.AttacksPerSecond) : 0f;

                if (attacksPerSecond <= 0f)
                {
                    raidAttackUnitTimers[runtimeId] = 0.1f;
                    continue;
                }

                float interval = Mathf.Max(0.08f, 1f / attacksPerSecond);
                int catchUpSafety = 0;

                while (timer <= 0f && raidAttackActive && catchUpSafety < 3)
                {
                    int repeatCount = ResolveRaidAttackRepeatCount(unit, runtimeId);
                    for (int repeatIndex = 0; repeatIndex < repeatCount && raidAttackActive; repeatIndex++)
                    {
                        ExecuteRaidAttackHit(unit, i, raidAttackParticipants.Count);
                    }

                    timer += interval;
                    catchUpSafety++;
                }

                raidAttackUnitTimers[runtimeId] = timer;
            }
        }

        private int ResolveRaidAttackRepeatCount(UnitRuntimeState unit, int runtimeId)
        {
            float multiplier = unit != null && unit.Attack != null ? Mathf.Max(1f, unit.Attack.BasicAttackRepeatMultiplier) : 1f;
            if (!raidAttackRepeatStates.TryGetValue(runtimeId, out RaidAttackRepeatState repeatState) || !Mathf.Approximately(repeatState.Multiplier, multiplier))
            {
                repeatState = new RaidAttackRepeatState(multiplier, 0f);
            }

            float total = multiplier + repeatState.Carry;
            int repeatCount = Mathf.Max(1, Mathf.FloorToInt(total));
            repeatState.Multiplier = multiplier;
            repeatState.Carry = Mathf.Clamp(total - repeatCount, 0f, 0.999999f);
            raidAttackRepeatStates[runtimeId] = repeatState;
            return repeatCount;
        }

        private void ExecuteRaidAttackHit(UnitRuntimeState unit, int participantIndex, int participantCount)
        {
            if (!IsEligibleRaidAttackParticipant(unit) ||
                !RaidBossDamageCalculator.TryCalculate(unit, config, out DamageInfo damageInfo))
            {
                return;
            }

            OnRaidAttackUnitFired?.Invoke(unit, participantIndex, participantCount);

            float appliedDamage = ApplyBossDamage(damageInfo);

            if (appliedDamage > 0f)
            {
                if (bossDamageAnchor == null)
                {
                    ResolveBossDamageAnchor();
                }

                AttackHitSoundPool.ShowHit(unit.Attack != null ? unit.Attack.AttackHitSoundTemplate : null, null, bossDamageAnchor != null ? bossDamageAnchor : transform);
                raidAttackSessionDamage += appliedDamage;
                contribution?.RecordDamage(unit, appliedDamage);
            }
        }

        private void ShowBossDamageNumber(DamageInfo sourceDamage, float appliedDamage)
        {
            if (appliedDamage <= 0f)
            {
                return;
            }

            DamageInfo displayDamage = new DamageInfo(appliedDamage, sourceDamage.DamageType, sourceDamage.IsCritical);
            float numberScale = config != null ? config.RaidBossDamageNumberScale : 1.4f;
            DamageNumberPool.Show(
                GetInstanceID(),
                displayDamage,
                ResolveBossDamageNumberPoint(),
                DamageNumberTargetType.Enemy,
                numberScale);
        }

        private Vector3 ResolveBossDamageNumberPoint()
        {
            if (bossDamageAnchor == null)
            {
                ResolveBossDamageAnchor();
            }

            Vector3 point;
            float horizontalSpread = 0.9f;

            if (bossDamageAnchor == null)
            {
                point = transform.position + Vector3.up * 8f;
            }
            else
            {
                if (bossDamageRenderer == null)
                {
                    bossDamageRenderer = bossDamageAnchor.GetComponentInChildren<Renderer>();
                }

                if (bossDamageRenderer != null)
                {
                    Bounds bounds = bossDamageRenderer.bounds;
                    point = new Vector3(
                        bounds.center.x,
                        Mathf.Lerp(bounds.center.y, bounds.max.y, 0.72f),
                        bounds.center.z);
                    horizontalSpread = Mathf.Clamp(bounds.extents.x * 0.42f, 0.75f, 2.2f);
                }
                else
                {
                    point = bossDamageAnchor.position + Vector3.up * 2f;
                }
            }

            int slot = bossDamageNumberSequence++ % 5;

            switch (slot)
            {
                case 1:
                    point.x -= horizontalSpread * 0.55f;
                    point.y += 0.12f;
                    break;
                case 2:
                    point.x += horizontalSpread * 0.55f;
                    point.y += 0.05f;
                    break;
                case 3:
                    point.x -= horizontalSpread;
                    point.y += 0.28f;
                    break;
                case 4:
                    point.x += horizontalSpread;
                    point.y += 0.20f;
                    break;
            }

            return point;
        }

        private void ResolveBossDamageAnchor()
        {
            bossDamageRenderer = null;

            if (bossDamageAnchor == null)
            {
                Transform raidRoot = transform.parent;

                if (raidRoot != null)
                {
                    bossDamageAnchor = raidRoot.Find("RaidBoss/BossGuide");
                }
            }

            if (bossDamageAnchor != null)
            {
                bossDamageRenderer = bossDamageAnchor.GetComponentInChildren<Renderer>();
            }
        }

        private void StopRaidAttackSession(bool preserveGauge)
        {
            if (!raidAttackActive)
            {
                manualRaidAttackHeld = false;
                return;
            }

            int participantCount = raidAttackSessionMaxParticipants;
            float appliedDamage = raidAttackSessionDamage;
            raidAttackActive = false;
            raidAttackUnitTimers.Clear();
            raidAttackRepeatStates.Clear();
            raidAttackSessionDamage = 0f;
            raidAttackSessionMaxParticipants = 0;
            raidAttackParticipants.Clear();

            if (!preserveGauge)
            {
                raidAttackGauge = Mathf.Max(0f, raidAttackGauge);
            }

            OnRaidAttackCastResolved?.Invoke(participantCount, appliedDamage);
        }

        private void StopRaidAttackCast()
        {
            manualRaidAttackHeld = false;
            StopRaidAttackSession(true);
        }

        private static bool IsEligibleRaidAttackParticipant(UnitRuntimeState unit)
        {
            return unit != null &&
                   unit.IsInitialized &&
                   !unit.IsSummon &&
                   unit.Health != null &&
                   !unit.Health.IsDead &&
                   unit.GridPosition != null &&
                   unit.GridPosition.IsInitialized;
        }

        private static void CollectEligibleRaidAttackParticipants(List<UnitRuntimeState> target)
        {
            if (target == null)
            {
                return;
            }

            foreach (UnitRuntimeState unit in CombatRegistry.Units)
            {
                if (IsEligibleRaidAttackParticipant(unit))
                {
                    target.Add(unit);
                }
            }
        }

        private static int CountEligibleRaidAttackParticipants()
        {
            int count = 0;

            foreach (UnitRuntimeState unit in CombatRegistry.Units)
            {
                if (IsEligibleRaidAttackParticipant(unit))
                {
                    count++;
                }
            }

            return count;
        }

        private void TickCost(float deltaTime)
        {
            if (config == null || currentCost >= CostMax || config.CostRegenPerSecond <= 0f)
            {
                costRegenAccumulator = 0f;
                return;
            }

            costRegenAccumulator += Mathf.Max(0f, deltaTime) * config.CostRegenPerSecond;
            int wholeCost = Mathf.FloorToInt(costRegenAccumulator);

            if (wholeCost <= 0)
            {
                return;
            }

            costRegenAccumulator -= wholeCost;
            ApplyCostGain(wholeCost);
        }

        private int ApplyCostGain(int amount)
        {
            if (amount <= 0 || currentCost >= CostMax)
            {
                return 0;
            }

            int previous = currentCost;
            currentCost = Mathf.Min(CostMax, currentCost + amount);
            int added = currentCost - previous;

            if (added > 0)
            {
                OnCostChanged?.Invoke(currentCost, CostMax);
            }

            return added;
        }

        private void HandleSummonCostGainRequested(UnitRuntimeState source, int amount, PassiveDataSO passive)
        {
            if (source == null || amount <= 0)
            {
                return;
            }

            AddCost(amount);
        }

        private void TryStartHpPhaseTransition()
        {
            if (state != RaidBattleState.Running || board == null || config == null || currentBossHp <= 0f)
            {
                return;
            }

            float hpRatio = BossHpRatio;

            if (board.Phase == RaidPhase.Phase1 && hpRatio <= config.Phase2HpRatio)
            {
                TryTransitionTo(RaidPhase.Phase2);
            }
            else if (board.Phase == RaidPhase.Phase2 && hpRatio <= config.Phase3HpRatio)
            {
                TryTransitionTo(RaidPhase.Phase3);
            }
        }

        private void InitializeRuntimeValues()
        {
            currentBossHp = BossMaxHp;
            remainingTime = TimeLimit;
            bossSkillGauge = 0f;
            raidAttackGauge = 0f;
            currentCost = config != null ? config.StartingCost : 0;
            selectedTeamIndex = 0;
            costRegenAccumulator = 0f;
            bossSkillReadyRaised = false;
            raidAttackReadyRaised = false;
            raidAttackActive = false;
            manualRaidAttackHeld = false;
            raidAttackUnitTimers.Clear();
            raidAttackRepeatStates.Clear();
            raidAttackSessionDamage = 0f;
            raidAttackSessionMaxParticipants = 0;
            raidAttackParticipants.Clear();
            bossDamageNumberSequence = 0;
            lastPublishedTimerSecond = Mathf.CeilToInt(remainingTime);
        }

        private void PublishAllRuntimeValues()
        {
            OnBossHpChanged?.Invoke(currentBossHp, BossMaxHp);
            OnTimeChanged?.Invoke(remainingTime);
            OnBossSkillGaugeChanged?.Invoke(bossSkillGauge, BossSkillGaugeMax);
            OnRaidAttackGaugeChanged?.Invoke(raidAttackGauge, RaidAttackGaugeMax);
            OnCostChanged?.Invoke(currentCost, CostMax);
            OnSelectedTeamChanged?.Invoke(selectedTeamIndex);
            OnModeChanged?.Invoke(mode);
        }

        private struct RaidAttackRepeatState
        {
            public float Multiplier;
            public float Carry;

            public RaidAttackRepeatState(float multiplier, float carry)
            {
                Multiplier = multiplier;
                Carry = carry;
            }
        }

        private bool ValidateConfig()
        {
            if (config != null)
            {
                return true;
            }

            Debug.LogError("RaidBattleController에 RaidBattleConfigSO가 연결되지 않았습니다.", this);
            return false;
        }

        private void PublishForcedRetreat(RaidForcedRetreatInfo info)
        {
            OnUnitForcedRetreat?.Invoke(info);
        }

        private void PublishEnemyRemoval(RaidEnemyPhaseRemovalInfo info)
        {
            OnEnemyRemovedByPhase?.Invoke(info);
        }

        private void SetState(RaidBattleState nextState)
        {
            if (state == nextState)
            {
                return;
            }

            state = nextState;
            OnStateChanged?.Invoke(state);
        }
    }
}
