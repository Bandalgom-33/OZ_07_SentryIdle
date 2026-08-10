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

public class GameManager : SingletonBase<GameManager>
{

    #region 참조 및 변수

    [SerializeField] private GameObject optionPanel;

    private bool _optionPanelActive;

    #endregion


    #region 라이프 사이클

    protected override void Awake()
    {
        base.Awake();
        ChangeState(GameState.Title);
        _optionPanelActive = false;
    }

    private void OnEnable()
    {
        InGameUI.OnGameSpeedChange += SetGameSpeed;
    }

    private void OnDisable()
    {
        InGameUI.OnGameSpeedChange -= SetGameSpeed;
    }

    #endregion

    #region 외부 노출 메서드

    private GameState _currentState;

    public GameState CurrentState => _currentState;

    // 게임 상태 변경
    public void ChangeState(GameState newState)
    {
        GameState oldState = _currentState;
        _currentState = newState;

        switch (newState)
        {
            case GameState.Title:
                break;
            case GameState.Tutorial:
                //튜토리얼 상태 변환
                break;
            case GameState.InGame:
                ResumeGame();
                break;
            case GameState.Pause:
                PauseGame();
                break;
            case GameState.GameOver:
                //웨이브 실패시 호출 
                break;
        }

        EventBus.Publish(new GameStateChangedEvent(oldState, newState));
    }

    // 게임 속도 변경 
    // 이벤트로 속도 버튼 호출
    public void SetGameSpeed(int index)
    {
        Debug.Log(index);
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

    #region 내부 매서드 모음

    // 게임 일시 정지
    private void PauseGame()
    {
        SetGameSpeed(0);
        if (_optionPanelActive)
        {
            optionPanel.SetActive(true);
        }
    }

    // 게임 재개 및 초기 시작
    private void ResumeGame()
    {
        SetGameSpeed(1);
        if (optionPanel.activeSelf)
        {
            optionPanel.SetActive(false);
        }
    }

    #endregion
}
