using System;
using EndlessGuard.Unit.Runtime;
using UnityEngine;

namespace EndlessGuard.TestBattle
{
    // 맵 생성, 아군 소환, 적 웨이브 및 전투 루프를 총괄 오케스트레이션하는 코디네이터 컴포넌트
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

        // 하위 시스템 컴포넌트 자동 탐색 및 캐싱
        private void Awake()
        {
            _combatLoop = GetComponent<CombatLoop>();

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

        // 전역 이벤트 및 맵/웨이브 이벤트 리스너 등록
        private void OnEnable()
        {
            EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);

            if (waveManager != null)
            {
                waveManager.OnStageCleared += HandleStageCleared;
            }

            if (mapGenerator != null)
            {
                mapGenerator.OnMapGenerated += HandleMapGenerated;
            }
        }

        // 이벤트 구독 해제 및 전투 정지
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

        // 시작 옵션에 따른 전투 시퀀스 가동
        private void Start()
        {
            if (autoStartBattleOnStart)
            {
                StartFullBattleSequence();
            }
        }

        // 디버그 키보드 입력 감지
        private void Update()
        {
            if (enableDebugHotkeys)
            {
                HandleDebugInputs();
            }
        }

        #endregion

        #region 전투 시퀀스 총괄 제어

        // 전체 전투 시퀀스 시작 (맵 생성 -> 아군 소환 -> 웨이브 가동)
        public void StartFullBattleSequence()
        {
            Debug.Log("[TestBattleCoordinator] >>> 전체 전투 시퀀스 시작 <<<");

            StopBattle();

            if (mapGenerator != null)
            {
                mapGenerator.GenerateMap();
            }
        }

        // 맵 생성 완료 이벤트 콜백
        private void HandleMapGenerated()
        {
            if (_combatLoop != null)
            {
                _combatLoop.StartLoop();
            }

            if (unitSummonManager != null)
            {
                unitSummonManager.InitializeSummoning();
            }

            if (waveManager != null)
            {
                waveManager.StartStageWaves();
            }

            _isBattleActive = true;
        }

        // 전투 시퀀스 안전 정지 및 필드 오브젝트 일괄 정리
        public void StopBattle()
        {
            _isBattleActive = false;

            if (waveManager != null)
            {
                waveManager.StopWaveSystem();
                waveManager.ClearAllEnemies();
            }

            if (unitSummonManager != null)
            {
                unitSummonManager.StopAutoSpawn();
                unitSummonManager.ClearAllUnits();
            }

            if (_combatLoop != null)
            {
                _combatLoop.StopLoop();
            }

            AidEffect.Shutdown();
            ReadyEffect.Shutdown();
        }

        // 전투 시퀀스 재시작
        public void RestartBattle(bool keepMap = true)
        {
            Debug.Log("[TestBattleCoordinator] 전투 재시작 (RestartBattle)");

            if (!keepMap)
            {
                StartFullBattleSequence();
            }
            else
            {
                if (waveManager != null) waveManager.ClearAllEnemies();
                if (unitSummonManager != null) unitSummonManager.ClearAllUnits();

                if (unitSummonManager != null) unitSummonManager.InitializeSummoning();
                if (waveManager != null) waveManager.RestartCurrentStage();
                if (_combatLoop != null) _combatLoop.StartLoop();

                _isBattleActive = true;
            }
        }

        #endregion

        #region 스테이지 클리어 및 패배(GameOver) 처리

        // 스테이지 클리어 이벤트 콜백
        private void HandleStageCleared(int clearedStage)
        {
            Debug.Log($"[TestBattleCoordinator] Stage {clearedStage} 클리어 확인! 다음 스테이지 준비를 위해 맵을 재생성합니다.");

            StartFullBattleSequence();
        }

        // 게임 상태 변경 이벤트 콜백
        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            if (evt.newState == GameState.GameOver)
            {
                Debug.LogWarning("[TestBattleCoordinator] [GAME OVER] 라운드 패배 감지! 웨이브 및 아군을 재정비하고 라운드를 재시작합니다.");

                RestartBattle(keepMap: true);

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.ResetLife();
                    GameManager.Instance.ChangeState(GameState.InGame);
                }
            }
        }

        #endregion

        #region 디버그 단축키 처리

        // 디버그 키보드 입력 처리
        private void HandleDebugInputs()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                Debug.Log("[Hotkey] 'R' 키 입력 -> 전투 재시작");
                RestartBattle(keepMap: true);
            }

            if (Input.GetKeyDown(KeyCode.M))
            {
                Debug.Log("[Hotkey] 'M' 키 입력 -> 맵 전체 재생성");
                StartFullBattleSequence();
            }

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
