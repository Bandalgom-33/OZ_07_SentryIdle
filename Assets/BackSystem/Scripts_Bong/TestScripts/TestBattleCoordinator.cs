using System;
using EndlessGuard.Unit.Runtime;
using UnityEngine;

namespace EndlessGuard.TestBattle
{
    /// <summary>
    /// 맵 생성(TestMapGenerator), 아군 소환(TestUnitSummonManager), 웨이브 진행(TestWaveManager),
    /// 전투 루프(CombatLoop) 및 패배/게임오버(GameManager)를 총괄 오케스트레이션하는 마스터 전투 코디네이터 클래스
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CombatLoop))]
    public class TestBattleCoordinator : MonoBehaviour
    {
        #region 인스펙터 직렬화 필드

        [Header("--- 하위 시스템 컴포넌트 참조 ---")]
        [Tooltip("맵 격자 및 경로 생성 매니저")]
        [SerializeField] private TestMapGenerator mapGenerator;

        [Tooltip("덱 연동 아군 유닛 자동 소환 매니저")]
        [SerializeField] private TestUnitSummonManager unitSummonManager;

        [Tooltip("웨이브 몬스터 스폰 및 스테이지 제어 매니저")]
        [SerializeField] private TestWaveManager waveManager;

        [Header("--- 전투 시작 옵션 ---")]
        [Tooltip("씬 시작 시 자동으로 전체 전투 시퀀스를 시작할지 여부")]
        [SerializeField] private bool autoStartBattleOnStart = true;

        [Header("--- 디버그 핫키 활성화 ---")]
        [Tooltip("R(재시작), U(유닛소환), M(맵재생성) 등 키보드 디버그 활성화 여부")]
        [SerializeField] private bool enableDebugHotkeys = true;

        #endregion

        #region 내부 런타임 필드

        private CombatLoop _combatLoop;
        private bool _isBattleActive;

        public bool IsBattleActive => _isBattleActive;

        #endregion

        #region 라이프사이클

        private void Awake()
        {
            // CombatLoop 컴포넌트 캐싱
            _combatLoop = GetComponent<CombatLoop>();

            // 하위 매니저 자동 탐색 및 검증
            if (mapGenerator == null)
            {
                mapGenerator = GetComponentInChildren<TestMapGenerator>();
                if (mapGenerator == null) mapGenerator = FindFirstObjectByType<TestMapGenerator>();
            }

            if (unitSummonManager == null)
            {
                unitSummonManager = GetComponentInChildren<TestUnitSummonManager>();
                if (unitSummonManager == null) unitSummonManager = FindFirstObjectByType<TestUnitSummonManager>();
            }

            if (waveManager == null)
            {
                waveManager = GetComponentInChildren<TestWaveManager>();
                if (waveManager == null) waveManager = FindFirstObjectByType<TestWaveManager>();
            }
        }

        private void OnEnable()
        {
            // 1. 게임 상태 변경 이벤트 구독 (라운드 패배 및 게임오버 감지)
            EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);

            // 2. 스테이지 클리어 이벤트 구독
            if (waveManager != null)
            {
                waveManager.OnStageCleared += HandleStageCleared;
            }

            // 3. 맵 생성 완료 이벤트 구독
            if (mapGenerator != null)
            {
                mapGenerator.OnMapGenerated += HandleMapGenerated;
            }
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);

            if (waveManager != null)
            {
                waveManager.OnStageCleared -= HandleStageCleared;
            }

            if (mapGenerator != null)
            {
                mapGenerator.OnMapGenerated -= HandleMapGenerated;
            }

            StopBattle();
        }

        private void Start()
        {
            // 씬 진입 시 자동 전투 시작
            if (autoStartBattleOnStart)
            {
                StartFullBattleSequence();
            }
        }

        private void Update()
        {
            // 디버그 단축키 처리
            if (enableDebugHotkeys)
            {
                HandleDebugInputs();
            }
        }

        #endregion

        #region 전투 시퀀스 총괄 제어

        /// <summary>
        /// 맵 생성 -> 아군 소환 -> 전투 루프 활성화 -> 웨이브 가동의 전체 전투 시퀀스를 시작합니다.
        /// </summary>
        public void StartFullBattleSequence()
        {
            Debug.Log("[TestBattleCoordinator] >>> 전체 전투 시퀀스 시작 <<<");

            // 1. 기존 전투 정리
            StopBattle();

            // 2. 맵 생성 요청 (생성 완료 시 HandleMapGenerated 콜백이 호출됨)
            if (mapGenerator != null)
            {
                mapGenerator.GenerateMap();
            }
        }

        // 맵 생성 완료 콜백 -> 아군 배치 및 전투 루프/웨이브 가동
        private void HandleMapGenerated()
        {
            // 1. 전투 루프 컴포넌트 가동 (매 프레임 Step 활성화)
            if (_combatLoop != null)
            {
                _combatLoop.StartLoop();
            }

            // 2. 아군 유닛 초기 배치 및 자동 소환 루프 가동
            if (unitSummonManager != null)
            {
                unitSummonManager.InitializeSummoning();
            }

            // 3. 적 웨이브 코루틴 가동
            if (waveManager != null)
            {
                waveManager.StartStageWaves();
            }

            _isBattleActive = true;
        }

        /// <summary>
        /// 진행 중인 전투를 안전하게 정지하고 모든 필드 오브젝트를 정리합니다.
        /// </summary>
        public void StopBattle()
        {
            _isBattleActive = false;

            // 1. 웨이브 코루틴 중단 및 적 삭제
            if (waveManager != null)
            {
                waveManager.StopWaveSystem();
                waveManager.ClearAllEnemies();
            }

            // 2. 아군 소환 중단 및 아군 유닛 삭제
            if (unitSummonManager != null)
            {
                unitSummonManager.StopAutoSpawn();
                unitSummonManager.ClearAllUnits();
            }

            // 3. 전투 프레임 틱 루프 정지
            if (_combatLoop != null)
            {
                _combatLoop.StopLoop();
            }

            // 4. 잔여 이펙트(AidEffect / ReadyEffect) 일괄 정리
            AidEffect.Shutdown();
            ReadyEffect.Shutdown();
        }

        /// <summary>
        /// 필드 오브젝트를 청소하고 현재 스테이지 웨이브를 1웨이브부터 재시작합니다.
        /// </summary>
        public void RestartBattle(bool keepMap = true)
        {
            Debug.Log("[TestBattleCoordinator] 전투 재시작 (RestartBattle)");

            if (!keepMap)
            {
                StartFullBattleSequence();
            }
            else
            {
                // 1. 필드 유닛 및 적 청소
                if (waveManager != null) waveManager.ClearAllEnemies();
                if (unitSummonManager != null) unitSummonManager.ClearAllUnits();

                // 2. 아군 재소환 및 웨이브 재가동
                if (unitSummonManager != null) unitSummonManager.InitializeSummoning();
                if (waveManager != null) waveManager.RestartCurrentStage();
                if (_combatLoop != null) _combatLoop.StartLoop();

                _isBattleActive = true;
            }
        }

        #endregion

        #region 스테이지 클리어 및 패배(GameOver) 처리

        // 스테이지 완료 시 콜백 -> 맵 재생성 및 다음 스테이지 시작
        private void HandleStageCleared(int clearedStage)
        {
            Debug.Log($"[TestBattleCoordinator] Stage {clearedStage} 클리어 확인! 다음 스테이지 준비를 위해 맵을 재생성합니다.");

            // 맵을 새로 생성하고 새 지형/경로에서 다음 스테이지 가동
            StartFullBattleSequence();
        }

        // GameManager의 게임 상태 변경 이벤트 수신 (라운드 패배 처리)
        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            if (evt.newState == GameState.GameOver)
            {
                Debug.LogWarning("[TestBattleCoordinator] [GAME OVER] 라운드 패배 감지! 웨이브 및 아군을 재정비하고 라운드를 재시작합니다.");

                // 1. 현재 스테이지 1웨이브부터 재시작
                RestartBattle(keepMap: true);

                // 2. GameManager 라이프 복구 및 상태 복귀 연동
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.ResetLife();
                    GameManager.Instance.ChangeState(GameState.InGame);
                }
            }
        }

        #endregion

        #region 디버그 단축키 처리

        private void HandleDebugInputs()
        {
            // [R]: 전투 재시작
            if (Input.GetKeyDown(KeyCode.R))
            {
                Debug.Log("[Hotkey] 'R' 키 입력 -> 전투 재시작");
                RestartBattle(keepMap: true);
            }

            // [M]: 맵 전체 재생성
            if (Input.GetKeyDown(KeyCode.M))
            {
                Debug.Log("[Hotkey] 'M' 키 입력 -> 맵 전체 재생성");
                StartFullBattleSequence();
            }

            // [U]: 아군 1기 즉시 추가 소환 시도
            if (Input.GetKeyDown(KeyCode.U))
            {
                if (unitSummonManager != null)
                {
                    bool spawned = unitSummonManager.TrySpawnNextDeckUnit(ignoreDpCost: true);
                    Debug.Log($"[Hotkey] 'U' 키 입력 -> 아군 소환 결과: {(spawned ? "성공" : "실패")}");
                }
            }
        }

        #endregion
    }
}
