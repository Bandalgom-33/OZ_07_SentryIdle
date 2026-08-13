using System;
using UnityEngine;


#region 게임 상태 관련 Enum

public enum GameState
{
    Title,
    Tutorial,
    InGame,
    Pause,
    GameOver
}

#endregion

// 게임 전체 상태 및 배속/라이프 관리 싱글톤
public class GameManager : SingletonBase<GameManager>
{

    #region 참조 및 변수

    [SerializeField] private GameObject optionPanel;

    [Header("--- 플레이어 라이프 설정 ---")]
    [SerializeField] private int maxLife = 20;
    [SerializeField] private int currentLife = 20;

    private bool _optionPanelActive;
    private InGameUI _inGameUI;
    private int _currentSpeedIndex = 1; // 기본 1배속 (0: Pause, 1: 1x, 2: 2x, 3: 3x)

    public int CurrentLife => currentLife;
    public int MaxLife => maxLife;
    public int CurrentSpeedIndex => _currentSpeedIndex;

    // 라이프 변경 알림 이벤트
    public static event Action<int, int> OnLifeChanged;

    #endregion


    #region 라이프 사이클

    protected override void Awake()
    {
        base.Awake();
        ChangeState(GameState.Title);
        _optionPanelActive = false;
        _inGameUI = FindFirstObjectByType<InGameUI>();
        currentLife = maxLife;
    }

    // 이벤트 구독 등록
    private void OnEnable()
    {
        InGameUI.OnGameSpeedChange += SetGameSpeed;
        EndlessGuard.Unit.Runtime.CombatEvents.OnEnemyReachedGoal += HandleEnemyReachedGoal;
    }

    // 이벤트 구독 해제
    private void OnDisable()
    {
        InGameUI.OnGameSpeedChange -= SetGameSpeed;
        EndlessGuard.Unit.Runtime.CombatEvents.OnEnemyReachedGoal -= HandleEnemyReachedGoal;
    }

    private void Start()
    {
        UpdateLifeUI();
    }

    #endregion

    #region 라이프 및 적 도달 처리

    // 적 출구 도달 처리
    private void HandleEnemyReachedGoal(EndlessGuard.Unit.Runtime.EnemyReachedGoalInfo info)
    {
        // 1. 라이프 차감 (최소 0)
        DecreaseLife(1);

        // 2. 도달한 적 탐색 및 파괴
        EndlessGuard.Unit.Runtime.EnemyRuntimeState targetEnemy = null;
        foreach (var enemy in EndlessGuard.Unit.Runtime.SpawnedEnemyManager.Instance.ActiveEnemies)
        {
            if (enemy != null && enemy.RuntimeId == info.RuntimeId)
            {
                targetEnemy = enemy;
                break;
            }
        }

        if (targetEnemy != null)
        {
            EndlessGuard.Unit.Runtime.SpawnedEnemyManager.Instance.UnregisterEnemy(targetEnemy);
            Destroy(targetEnemy.gameObject);
        }
        else
        {
            // RuntimeId 탐색 실패 시 위치 기반 예외 처리
            var activeEnemies = new System.Collections.Generic.List<EndlessGuard.Unit.Runtime.EnemyRuntimeState>(EndlessGuard.Unit.Runtime.SpawnedEnemyManager.Instance.ActiveEnemies);
            foreach (var enemy in activeEnemies)
            {
                if (enemy != null && Vector3.Distance(enemy.transform.position, info.Position) < 0.5f)
                {
                    EndlessGuard.Unit.Runtime.SpawnedEnemyManager.Instance.UnregisterEnemy(enemy);
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

    // 라이프 UI 갱신 알림 처리
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

    private GameState _currentState;

    public GameState CurrentState => _currentState;

    // 게임 상태 전환 처리
    public void ChangeState(GameState newState)
    {
        GameState oldState = _currentState;
        _currentState = newState;

        switch (newState)
        {
            case GameState.Title:
                break;
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

        EventBus.Publish(new GameStateChangedEvent(oldState, newState));
    }

    // 라이프 초기화 처리
    public void ResetLife()
    {
        currentLife = maxLife;
        UpdateLifeUI();
    }

    // 게임 오버 및 재시작 처리
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

    // 게임 속도 설정 처리
    public void SetGameSpeed(int index)
    {
        Debug.Log(index);
        if (index > 0)
        {
            _currentSpeedIndex = index;
        }

        switch (index)
        {
            case 0:
                Time.timeScale = 0;
                break;
            case 1:
                Time.timeScale = 1;
                break;
            case 2:
                Time.timeScale = 2;
                break;
            case 3:
                Time.timeScale = 3;
                break;
        }
        EventBus.Publish(new GameSpeedChangedEvent(index, Time.timeScale));
    }

    #endregion

    #region 내부 메서드 모음

    // 게임 일시 정지 처리
    private void PauseGame()
    {
        SetGameSpeed(0);
        if (_optionPanelActive && optionPanel != null)
        {
            optionPanel.SetActive(true);
        }
    }

    // 게임 재개 처리
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
