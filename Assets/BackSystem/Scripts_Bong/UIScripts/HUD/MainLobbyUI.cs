using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 메인 로비 씬의 상단 정보 표시, 씬 전환, 6개 서브 패널 토글 및 게임 종료를 총괄 제어하는 UI 컨트롤러
public class MainLobbyUI : MonoBehaviour
{
    #region 직렬화 변수 (인스펙터 바인딩)

    [Header("--- 상단 HUD 정보 텍스트 ---")]
    [Tooltip("현재 스테이지 정보 표시 TMP 텍스트")]
    [SerializeField] private TMP_Text stageInfoText;

    [Tooltip("현재 보유 골드 표시 TMP 텍스트")]
    [SerializeField] private TMP_Text goldText;

    [Tooltip("현재 보유 다이아 표시 TMP 텍스트")]
    [SerializeField] private TMP_Text diamondText;

    [Header("--- 씬 전환 버튼 ---")]
    [Tooltip("메인 게임플레이(전투) 씬 전환 버튼")]
    [SerializeField] private Button enterGamePlayButton;

    [Tooltip("레이드 씬 전환 버튼")]
    [SerializeField] private Button enterRaidButton;

    [Header("--- 서브 시스템 진입 버튼 (6종) ---")]
    [Tooltip("영웅 컬렉션(보관함) 패널 오픈 버튼")]
    [SerializeField] private Button collectionButton;

    [Tooltip("가챠(뽑기) 패널 오픈 버튼")]
    [SerializeField] private Button gachaButton;

    [Tooltip("업그레이드(강화) 패널 오픈 버튼")]
    [SerializeField] private Button upgradeButton;

    [Tooltip("인벤토리(소모품) 패널 오픈 버튼")]
    [SerializeField] private Button inventoryButton;

    [Tooltip("공방(제작) 패널 오픈 버튼")]
    [SerializeField] private Button workshopButton;

    [Tooltip("던전(파견) 패널 오픈 버튼")]
    [SerializeField] private Button dungeonButton;

    [Header("--- 서브 시스템 패널 오브젝트 매핑 (6종) ---")]
    [Tooltip("영웅 컬렉션 윈도우 패널 오브젝트")]
    [SerializeField] private GameObject collectionWindowPanel;

    [Tooltip("가챠 윈도우 패널 오브젝트")]
    [SerializeField] private GameObject gachaWindowPanel;

    [Tooltip("업그레이드 윈도우 패널 오브젝트")]
    [SerializeField] private GameObject upgradeWindowPanel;

    [Tooltip("인벤토리 윈도우 패널 오브젝트")]
    [SerializeField] private GameObject inventoryWindowPanel;

    [Tooltip("공방 윈도우 패널 오브젝트")]
    [SerializeField] private GameObject workshopWindowPanel;

    [Tooltip("던전 윈도우 패널 오브젝트")]
    [SerializeField] private GameObject dungeonWindowPanel;

    [Header("--- 게임 종료 버튼 ---")]
    [Tooltip("게임 시작/타이틀 화면의 게임 종료 버튼")]
    [SerializeField] private Button titleQuitButton;

    [Tooltip("인게임/옵션 팝업 내의 게임 종료 버튼")]
    [SerializeField] private Button optionQuitButton;

    #endregion

    #region 내부 변수 및 상수

    private static readonly string[] NumFormats = { "", "K", "M", "B", "T", "Qa", "Qi" };

    #endregion

    #region 라이프 사이클

    // 버튼 클릭 이벤트 자동 바인딩
    private void Awake()
    {
        InitializeButtonListeners();
    }

    // 전역 이벤트 및 재화 변경 액션 구독
    private void OnEnable()
    {
        CurrencyManager.OnGoldChange += UpdateGoldUI;
        CurrencyManager.OnDiamondChange += UpdateDiamondUI;
        EventBus.Subscribe<StageWaveChangedEvent>(OnStageWaveChanged);
        EventBus.Subscribe<DataLoadEvent>(OnDataLoaded);

        RefreshAllHUD();
    }

    // 이벤트 구독 해제
    private void OnDisable()
    {
        CurrencyManager.OnGoldChange -= UpdateGoldUI;
        CurrencyManager.OnDiamondChange -= UpdateDiamondUI;
        EventBus.Unsubscribe<StageWaveChangedEvent>(OnStageWaveChanged);
        EventBus.Unsubscribe<DataLoadEvent>(OnDataLoaded);
    }

    // 데이터 로드 완료 이벤트 수신 처리
    private void OnDataLoaded(DataLoadEvent evt)
    {
        RefreshAllHUD();
    }

    // UI 활성화 초기 갱신
    private void Start()
    {
        RefreshAllHUD();
    }

    #endregion

    #region 초기화 보조 메서드

    // 버튼 클릭 리스너 일괄 등록
    private void InitializeButtonListeners()
    {
        // 1. 씬 전환 버튼
        if (enterGamePlayButton != null)
        {
            enterGamePlayButton.onClick.AddListener(OnEnterGamePlayClicked);
        }

        if (enterRaidButton != null)
        {
            enterRaidButton.onClick.AddListener(OnEnterRaidClicked);
        }

        // 2. 서브 패널 토글 버튼
        if (collectionButton != null)
        {
            collectionButton.onClick.AddListener(() => TogglePanel(collectionWindowPanel));
        }

        if (gachaButton != null)
        {
            gachaButton.onClick.AddListener(() => TogglePanel(gachaWindowPanel));
        }

        if (upgradeButton != null)
        {
            upgradeButton.onClick.AddListener(() => TogglePanel(upgradeWindowPanel));
        }

        if (inventoryButton != null)
        {
            inventoryButton.onClick.AddListener(() => TogglePanel(inventoryWindowPanel));
        }

        if (workshopButton != null)
        {
            workshopButton.onClick.AddListener(() => TogglePanel(workshopWindowPanel));
        }

        if (dungeonButton != null)
        {
            dungeonButton.onClick.AddListener(() => TogglePanel(dungeonWindowPanel));
        }

        // 3. 게임 종료 버튼
        if (titleQuitButton != null)
        {
            titleQuitButton.onClick.AddListener(QuitGame);
        }

        if (optionQuitButton != null)
        {
            optionQuitButton.onClick.AddListener(QuitGame);
        }
    }

    #endregion

    #region 씬 전환 이벤트 핸들러

    // 메인 게임플레이 씬 전환 요청 처리
    public void OnEnterGamePlayClicked()
    {
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadScene(SceneType.GamePlay);
        }
        else
        {
            Debug.LogError("[MainLobbyUI] SceneLoader 인스턴스가 존재하지 않습니다.");
        }
    }

    // 레이드 씬 전환 요청 처리
    public void OnEnterRaidClicked()
    {
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadScene(SceneType.Raid);
        }
        else
        {
            Debug.LogError("[MainLobbyUI] SceneLoader 인스턴스가 존재하지 않습니다.");
        }
    }

    #endregion

    #region 서브 패널 관리 (토글 및 배타적 오픈)

    // 지정 패널 토글 연산 (열려있으면 닫고, 닫혀있으면 단독으로 엶)
    public void TogglePanel(GameObject targetPanel)
    {
        if (targetPanel == null) return;

        bool isActive = targetPanel.activeSelf;
        CloseAllSubPanels();
        targetPanel.SetActive(!isActive);
    }

    // 지정 패널 단독 오픈 연산
    public void OpenPanel(GameObject targetPanel)
    {
        if (targetPanel == null) return;

        CloseAllSubPanels();
        targetPanel.SetActive(true);
    }

    // 모든 서브 패널 일괄 닫기 연산
    public void CloseAllSubPanels()
    {
        if (collectionWindowPanel != null) collectionWindowPanel.SetActive(false);
        if (gachaWindowPanel != null) gachaWindowPanel.SetActive(false);
        if (upgradeWindowPanel != null) upgradeWindowPanel.SetActive(false);
        if (inventoryWindowPanel != null) inventoryWindowPanel.SetActive(false);
        if (workshopWindowPanel != null) workshopWindowPanel.SetActive(false);
        if (dungeonWindowPanel != null) dungeonWindowPanel.SetActive(false);
    }

    #endregion

    #region 게임 종료

    // 게임 애플리케이션 종료 연산
    public void QuitGame()
    {
        Debug.Log("[MainLobbyUI] 게임 종료를 요청합니다.");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion

    #region HUD 정보 갱신

    // 전체 HUD 정보 일괄 갱신 연산
    public void RefreshAllHUD()
    {
        if (CurrencyManager.Instance != null)
        {
            UpdateGoldUI(CurrencyManager.Instance.Gold);
            UpdateDiamondUI(CurrencyManager.Instance.Diamond);
        }

        if (StageProgressManager.Instance != null)
        {
            UpdateStageInfoUI(StageProgressManager.Instance.CurrentStage, StageProgressManager.Instance.CurrentWave);
        }
    }

    // 스테이지 변경 이벤트 핸들러
    private void OnStageWaveChanged(StageWaveChangedEvent evt)
    {
        UpdateStageInfoUI(evt.stageNumber, evt.waveNumber);
    }

    // 스테이지 정보 텍스트 갱신
    public void UpdateStageInfoUI(int stage, int wave)
    {
        if (stageInfoText != null)
        {
            stageInfoText.text = $"STAGE {stage:D2}";
        }
    }

    // 골드 잔액 텍스트 갱신
    private void UpdateGoldUI(long gold)
    {
        if (goldText != null)
        {
            goldText.text = FormatCurrencyNumber(gold);
        }
    }

    // 다이아 잔액 텍스트 갱신
    private void UpdateDiamondUI(long diamond)
    {
        if (diamondText != null)
        {
            diamondText.text = FormatCurrencyNumber(diamond);
        }
    }

    #endregion

    #region 유틸리티

    // 대용량 재화 단위 축약 포맷팅 처리
    private string FormatCurrencyNumber(double value)
    {
        if (value < 1000)
        {
            return value.ToString("N0");
        }

        int formatIndex = 0;
        while (value >= 1000 && formatIndex < NumFormats.Length - 1)
        {
            value /= 1000;
            formatIndex++;
        }

        return value.ToString("N1") + NumFormats[formatIndex];
    }

    #endregion
}
