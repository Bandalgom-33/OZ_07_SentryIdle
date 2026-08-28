using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EndlessGuard.Unit.Raid.Runtime;
using EndlessGuard.Unit.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Title,
    Tutorial,
    InGame,
    Pause,
    GameOver
}

// 게임 전체 상태(Title, InGame, Pause, GameOver), 플레이어 라이프 및 게임 배속을 제어하는 싱글톤 매니저
public class GameManager : SingletonBase<GameManager>
{
    #region 참조 및 변수

    [Header("--- 팝업 UI 참조 ---")]
    [Tooltip("옵션/환경설정 팝업 패널 오브젝트")]
    [SerializeField] private GameObject optionPanel;

    [Header("--- 플레이어 라이프 설정 ---")]
    [Tooltip("플레이어 기지 최대 라이프 수치")]
    [SerializeField] private int maxLife = 20;

    [Tooltip("현재 남아있는 라이프 수치")]
    [SerializeField] private int currentLife = 20;

    private bool _optionPanelActive;
    private InGameUI _inGameUI;
    private int _currentSpeedIndex = 1;
    private GameState _currentState;

    public int CurrentLife => currentLife;
    public int MaxLife => maxLife;
    public int CurrentSpeedIndex => _currentSpeedIndex;
    public GameState CurrentState => _currentState;

    public static event Action<int, int> OnLifeChanged;

    #endregion

    #region 라이프 사이클

    // 인스턴스 및 게임 상태 초기화
    protected override void Awake()
    {
        base.Awake();
        ChangeState(GameState.Title);
        _optionPanelActive = false;
        _inGameUI = FindFirstObjectByType<InGameUI>();
        currentLife = maxLife;

        _ = SaveManager.Instance;
        _ = DeckManager.Instance;
        _ = ExperienceManager.Instance;
        _ = CollectionDataProvider.Instance;
        _ = StageProgressManager.Instance;
        _ = OfflineRewardManager.Instance;
        _ = SceneLoader.Instance;
        _ = InventoryGridManager.Instance;
        _ = EquipmentManager.Instance;
    }

    // 이벤트 버스 및 시스템 이벤트 구독
    private void OnEnable()
    {
        InGameUI.OnGameSpeedChange += SetGameSpeed;
        CombatEvents.OnEnemyReachedGoal += HandleEnemyReachedGoal;

        // 레이드 씬 로드 시 레이드 전투 컨트롤러 탐색 및 이벤트 구독 등록
        SceneManager.sceneLoaded += HandleSceneLoaded;
        BindActiveRaidBattles();
    }

    // 이벤트 구독 해제
    private void OnDisable()
    {
        InGameUI.OnGameSpeedChange -= SetGameSpeed;
        CombatEvents.OnEnemyReachedGoal -= HandleEnemyReachedGoal;

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        UnbindActiveRaidBattles();
    }

    // 씬 로드 완료 시 레이드 전투 컨트롤러 바인딩
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindActiveRaidBattles();
    }

    // 현재 씬의 활성 레이드 전투 컨트롤러 탐색 및 OnRaidEnded 구독
    private void BindActiveRaidBattles()
    {
        RaidBattleController[] battles = FindObjectsByType<RaidBattleController>(FindObjectsSortMode.None);
        for (int i = 0; i < battles.Length; i++)
        {
            if (battles[i] != null)
            {
                battles[i].OnRaidEnded -= HandleRaidEnded;
                battles[i].OnRaidEnded += HandleRaidEnded;
            }
        }
    }

    // 레이드 전투 컨트롤러 이벤트 구독 해제
    private void UnbindActiveRaidBattles()
    {
        RaidBattleController[] battles = FindObjectsByType<RaidBattleController>(FindObjectsSortMode.None);
        for (int i = 0; i < battles.Length; i++)
        {
            if (battles[i] != null)
            {
                battles[i].OnRaidEnded -= HandleRaidEnded;
            }
        }
    }

    // 레이드 종료 시 승리 판정 및 로비 복귀 루틴 분기
    private void HandleRaidEnded(RaidBattleResult result)
    {
        if (result == RaidBattleResult.Victory)
        {
            Debug.Log("[GameManager] 레이드 보스 처치 승리 확인! 2초 후 로비 씬으로 비동기 전환합니다.");
            ReturnToLobbyAfterDelayAsync().Forget();
        }
    }

    // 보스 사망 연출을 위해 2초 대기 후 로비 씬으로 비동기 페이드 전환
    private async UniTaskVoid ReturnToLobbyAfterDelayAsync()
    {
        // 1. 보스 사망 연출 및 승리 여운을 위해 2초간 비동기 대기
        await UniTask.Delay(TimeSpan.FromSeconds(2.0f), cancellationToken: this.GetCancellationTokenOnDestroy());

        // 2. SceneLoader 싱글톤을 통해 로비 씬으로 페이드아웃 효과와 함께 전환
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadScene(SceneType.Lobby, useFade: true);
        }
        else
        {
            _ = SceneManager.LoadSceneAsync("TestBuild2MainLobby");
        }
    }

    // 라이프 UI 초기 표시 연산
    private void Start()
    {
        UpdateLifeUI();
    }

    #endregion

    #region 라이프 및 적 도달 처리

    // 적 목표 지점 도달 시 라이프 차감 및 적 제거 처리
    private void HandleEnemyReachedGoal(EnemyReachedGoalInfo info)
    {
        DecreaseLife(1);

        EnemyRuntimeState targetEnemy = null;
        foreach (var enemy in SpawnedEnemyManager.Instance.ActiveEnemies)
        {
            if (enemy != null && enemy.RuntimeId == info.RuntimeId)
            {
                targetEnemy = enemy;
                break;
            }
        }

        if (targetEnemy != null)
        {
            SpawnedEnemyManager.Instance.UnregisterEnemy(targetEnemy);
            Destroy(targetEnemy.gameObject);
        }
        else
        {
            var activeEnemies = new List<EnemyRuntimeState>(SpawnedEnemyManager.Instance.ActiveEnemies);
            foreach (var enemy in activeEnemies)
            {
                if (enemy != null && Vector3.Distance(enemy.transform.position, info.Position) < 0.5f)
                {
                    SpawnedEnemyManager.Instance.UnregisterEnemy(enemy);
                    Destroy(enemy.gameObject);
                    break;
                }
            }
        }
    }

    // 라이프 차감 연산
    public void DecreaseLife(int amount)
    {
        if (currentLife <= 0) return;

        currentLife = Mathf.Max(0, currentLife - amount);
        UpdateLifeUI();

        if (currentLife <= 0)
        {
            Debug.LogWarning("[GameManager] 라이프가 0이 되어 게임 오버 상태로 전환합니다.");
            ChangeState(GameState.GameOver);
        }
    }

    // 라이프 UI 갱신 연산
    private void UpdateLifeUI()
    {
        if (_inGameUI == null)
        {
            _inGameUI = FindFirstObjectByType<InGameUI>();
        }

        if (_inGameUI != null)
        {
            _inGameUI.UpdateLifeUI(currentLife, maxLife);
        }

        OnLifeChanged?.Invoke(currentLife, maxLife);
    }

    #endregion

    #region 외부 노출 메서드

    // 게임 상태 전환 연산
    public void ChangeState(GameState newState)
    {
        if (_currentState == newState) return;

        GameState oldState = _currentState;
        _currentState = newState;

        // 상태 변경 이벤트를 먼저 발행하여 시스템 간 동기화 순서 보장
        EventBus.Publish(new GameStateChangedEvent(oldState, newState));

        switch (newState)
        {
            case GameState.Title:
            case GameState.Tutorial:
                break;
            case GameState.InGame:
                ResumeGame();
                break;
            case GameState.Pause:
                PauseGame();
                break;
            case GameState.GameOver:
                HandleGameOver();
                break;
        }
    }

    // 라이프 수치 초기화
    public void ResetLife()
    {
        currentLife = maxLife;
        UpdateLifeUI();
    }

    // 게임 오버 처리 및 재시작
    private void HandleGameOver()
    {
        MapGenerator mapGenerator = FindFirstObjectByType<MapGenerator>();
        if (mapGenerator != null)
        {
            mapGenerator.RestartWave();
        }

        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.SetDpCost(50);
        }

        ResetLife();
        ChangeState(GameState.InGame);
    }

    // 게임 속도 설정 및 배속 변경
    public void SetGameSpeed(int index)
    {
        if (index > 0)
        {
            _currentSpeedIndex = index;
        }

        Time.timeScale = index switch
        {
            0 => 0f,
            1 => 1f,
            2 => 2f,
            3 => 3f,
            _ => 1f
        };

        EventBus.Publish(new GameSpeedChangedEvent(index, Time.timeScale));
    }

    #endregion

    #region 내부 메서드

    // 게임 일시 정지 처리
    private void PauseGame()
    {
        SetGameSpeed(0);
        if (_optionPanelActive && optionPanel != null)
        {
            optionPanel.SetActive(true);
        }
    }

    // 게임 일시 정지 해제 및 재개
    private void ResumeGame()
    {
        SetGameSpeed(_currentSpeedIndex);
        if (_inGameUI == null)
        {
            _inGameUI = FindFirstObjectByType<InGameUI>();
        }

        if (_inGameUI != null)
        {
            _inGameUI.SetSpeedButtonVisual(_currentSpeedIndex);
        }

        if (optionPanel != null && optionPanel.activeSelf)
        {
            optionPanel.SetActive(false);
        }
    }

    #endregion
}
