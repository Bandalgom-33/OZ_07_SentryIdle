using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainLobbyUI : MonoBehaviour
{
    #region 싱글톤 인스턴스

    public static MainLobbyUI Instance { get; private set; }

    #endregion

    #region 직렬화 변수 (인스펙터 바인딩)

    [Header("--- 메인 화면 패널 구성 ---")]
    [Tooltip("게임 최초 실행 시 표시되는 시작/타이틀 패널")]
    [SerializeField] private GameObject startPanel;

    [Tooltip("기본 메인 로비 화면 패널")]
    [SerializeField] private GameObject mainLobbyPanel;

    [Tooltip("시작 패널 내 게임 시작 버튼")]
    [SerializeField] private Button startGameButton;

    [Header("--- 오프라인 보상 팝업 패널 ---")]
    [Tooltip("오프라인 보상 획득 시 활성화되는 보상 팝업 패널")]
    [SerializeField] private GameObject offlineRewardPanel;

    [Tooltip("오프라인 보상 팝업 닫기/수령 확인 버튼")]
    [SerializeField] private Button closeOfflineRewardButton;

    [Header("--- 상단 HUD 정보 텍스트 ---")]
    [Tooltip("현재 스테이지 정보 표시 TMP 텍스트")]
    [SerializeField] private TMP_Text stageInfoText;

    [Tooltip("현재 보유 골드 표시 TMP 텍스트")]
    [SerializeField] private TMP_Text goldText;

    [Tooltip("현재 보유 다이아 표시 TMP 텍스트")]
    [SerializeField] private TMP_Text diamondText;

    [Header("--- 씬 전환 버튼 ---")]
    [Tooltip("메인 게임플레이(일반 스테이지) 씬 전환 버튼")]
    [SerializeField] private Button enterGamePlayButton;

    [Tooltip("레이드 씬 전환 버튼")]
    [SerializeField] private Button enterRaidButton;

    [Header("--- 서브 시스템 메인 메뉴 버튼 (3개 그룹 + 던전) ---")]
    [Tooltip("유닛 보관함 / 덱 편성 선택 패널 오픈 버튼")]
    [SerializeField] private Button unitDeckMenuButton;

    [Tooltip("가챠 / 업그레이드 선택 패널 오픈 버튼")]
    [SerializeField] private Button gachaUpgradeMenuButton;

    [Tooltip("인벤토리 / 공방 선택 패널 오픈 버튼")]
    [SerializeField] private Button inventoryWorkshopMenuButton;

    [Tooltip("던전(파견) 패널 오픈 버튼")]
    [SerializeField] private Button dungeonButton;

    [Header("--- 1. 유닛 & 덱 편성 중간 선택 패널 ---")]
    [Tooltip("유닛/덱 편성 2선택 중간 패널 오브젝트")]
    [SerializeField] private GameObject unitDeckSelectPanel;

    [Tooltip("유닛 보관함 윈도우 오픈 버튼")]
    [SerializeField] private Button openCollectionButton;

    [Tooltip("덱 편성 윈도우 오픈 버튼")]
    [SerializeField] private Button openDeckButton;

    [Tooltip("유닛/덱 편성 선택 패널 닫기 버튼")]
    [SerializeField] private Button closeUnitDeckSelectButton;

    [Header("--- 2. 가챠 & 업그레이드 중간 선택 패널 ---")]
    [Tooltip("가챠/업그레이드 2선택 중간 패널 오브젝트")]
    [SerializeField] private GameObject gachaUpgradeSelectPanel;

    [Tooltip("가챠 윈도우 오픈 버튼")]
    [SerializeField] private Button openGachaButton;

    [Tooltip("업그레이드 윈도우 오픈 버튼")]
    [SerializeField] private Button openUpgradeButton;

    [Tooltip("가챠/업그레이드 선택 패널 닫기 버튼")]
    [SerializeField] private Button closeGachaUpgradeSelectButton;

    [Header("--- 3. 인벤토리 & 공방 중간 선택 패널 ---")]
    [Tooltip("인벤토리/공방 2선택 중간 패널 오브젝트")]
    [SerializeField] private GameObject inventoryWorkshopSelectPanel;

    [Tooltip("인벤토리 윈도우 오픈 버튼")]
    [SerializeField] private Button openInventoryButton;

    [Tooltip("공방 윈도우 오픈 버튼")]
    [SerializeField] private Button openWorkshopButton;

    [Tooltip("인벤토리/공방 선택 패널 닫기 버튼")]
    [SerializeField] private Button closeInventoryWorkshopSelectButton;

    [Header("--- 최종 서브 시스템 윈도우 패널 오브젝트 ---")]
    [Tooltip("유닛 보관함(컬렉션) 윈도우 패널")]
    [SerializeField] private GameObject collectionWindowPanel;

    [Tooltip("덱 편성 윈도우 패널")]
    [SerializeField] private GameObject deckFormationWindowPanel;

    [Tooltip("가챠(소환) 윈도우 패널")]
    [SerializeField] private GameObject gachaWindowPanel;

    [Tooltip("업그레이드(강화) 윈도우 패널")]
    [SerializeField] private GameObject upgradeWindowPanel;

    [Tooltip("인벤토리 윈도우 패널")]
    [SerializeField] private GameObject inventoryWindowPanel;

    [Tooltip("공방(제작) 윈도우 패널")]
    [SerializeField] private GameObject workshopWindowPanel;

    [Tooltip("던전 윈도우 패널")]
    [SerializeField] private GameObject dungeonWindowPanel;

    [Header("--- 게임 종료 버튼 ---")]
    [Tooltip("게임 시작/타이틀 화면의 게임 종료 버튼")]
    [SerializeField] private Button titleQuitButton;

    [Tooltip("인게임/옵션 팝업 내의 게임 종료 버튼")]
    [SerializeField] private Button optionQuitButton;

    #endregion

    #region 내부 변수 및 상수

    private static bool _isFirstLaunch = true;
    private static readonly string[] NumFormats = { "", "K", "M", "B", "T", "Qa", "Qi" };

    #endregion

    #region 라이프 사이클

    // 싱글톤 인스턴스 등록 및 버튼 리스너 바인딩
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        InitializeButtonListeners();
    }

    // 전역 이벤트 구독 및 패널 초기 상태 설정
    private void OnEnable()
    {
        CurrencyManager.OnGoldChange += UpdateGoldUI;
        CurrencyManager.OnDiamondChange += UpdateDiamondUI;
        EventBus.Subscribe<StageWaveChangedEvent>(OnStageWaveChanged);
        EventBus.Subscribe<DataLoadEvent>(OnDataLoaded);

        RefreshAllHUD();
    }

    // 이벤트 버스 구독 해제
    private void OnDisable()
    {
        CurrencyManager.OnGoldChange -= UpdateGoldUI;
        CurrencyManager.OnDiamondChange -= UpdateDiamondUI;
        EventBus.Unsubscribe<StageWaveChangedEvent>(OnStageWaveChanged);
        EventBus.Unsubscribe<DataLoadEvent>(OnDataLoaded);
    }

    // 첫 실행 여부에 따른 초기 메인 패널 표시 및 HUD 갱신
    private void Start()
    {
        ApplyInitialPanelState();
        RefreshAllHUD();
    }

    // 인스턴스 파괴 시 싱글톤 참조 해제
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    #endregion

    #region 시작 패널 및 메인 패널 제어

    // 첫 진입 여부에 따른 시작 패널 및 메인 로비 패널 상태 적용
    private void ApplyInitialPanelState()
    {
        CloseAllSelectPanels();
        CloseAllSubPanels();

        if (offlineRewardPanel != null)
        {
            offlineRewardPanel.SetActive(false);
        }

        if (_isFirstLaunch)
        {
            if (startPanel != null) startPanel.SetActive(true);
            if (mainLobbyPanel != null) mainLobbyPanel.SetActive(false);
        }
        else
        {
            if (startPanel != null) startPanel.SetActive(false);
            if (mainLobbyPanel != null) mainLobbyPanel.SetActive(true);
        }
    }

    // 시작 패널에서 메인 로비 패널로 전환
    public void OnStartGameClicked()
    {
        _isFirstLaunch = false;

        if (startPanel != null) startPanel.SetActive(false);
        if (mainLobbyPanel != null) mainLobbyPanel.SetActive(true);
    }

    #endregion

    #region 오프라인 보상 팝업 제어

    // 오프라인 보상 팝업 패널 오픈
    public void ShowOfflineRewardPopup()
    {
        SetOfflineRewardPanelActive(true);
    }

    // 오프라인 보상 팝업 패널 닫기
    public void CloseOfflineRewardPopup()
    {
        SetOfflineRewardPanelActive(false);
    }

    // 오프라인 보상 패널 활성화 상태 설정
    public void SetOfflineRewardPanelActive(bool active)
    {
        if (offlineRewardPanel != null)
        {
            offlineRewardPanel.SetActive(active);
        }
    }

    #endregion

    #region 초기화 보조 메서드

    // 버튼 클릭 리스너 일괄 바인딩
    private void InitializeButtonListeners()
    {
        // 1. 시작 패널 및 오프라인 보상 버튼
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(OnStartGameClicked);
        }

        if (closeOfflineRewardButton != null)
        {
            closeOfflineRewardButton.onClick.AddListener(CloseOfflineRewardPopup);
        }

        // 2. 씬 전환 버튼
        if (enterGamePlayButton != null)
        {
            enterGamePlayButton.onClick.AddListener(OnEnterGamePlayClicked);
        }

        if (enterRaidButton != null)
        {
            enterRaidButton.onClick.AddListener(OnEnterRaidClicked);
        }

        // 3. 메인 로비 서브 시스템 그룹 버튼
        if (unitDeckMenuButton != null)
        {
            unitDeckMenuButton.onClick.AddListener(() => ToggleSelectPanel(unitDeckSelectPanel));
        }

        if (gachaUpgradeMenuButton != null)
        {
            gachaUpgradeMenuButton.onClick.AddListener(() => ToggleSelectPanel(gachaUpgradeSelectPanel));
        }

        if (inventoryWorkshopMenuButton != null)
        {
            inventoryWorkshopMenuButton.onClick.AddListener(() => ToggleSelectPanel(inventoryWorkshopSelectPanel));
        }

        if (dungeonButton != null)
        {
            dungeonButton.onClick.AddListener(() => OpenSubPanel(dungeonWindowPanel));
        }

        // 4. 중간 선택 패널 내 서브 시스템 진입 버튼
        if (openCollectionButton != null)
        {
            openCollectionButton.onClick.AddListener(() => OpenSubPanel(collectionWindowPanel));
        }

        if (openDeckButton != null)
        {
            openDeckButton.onClick.AddListener(() => OpenSubPanel(deckFormationWindowPanel));
        }

        if (openGachaButton != null)
        {
            openGachaButton.onClick.AddListener(() => OpenSubPanel(gachaWindowPanel));
        }

        if (openUpgradeButton != null)
        {
            openUpgradeButton.onClick.AddListener(() => OpenSubPanel(upgradeWindowPanel));
        }

        if (openInventoryButton != null)
        {
            openInventoryButton.onClick.AddListener(() => OpenSubPanel(inventoryWindowPanel));
        }

        if (openWorkshopButton != null)
        {
            openWorkshopButton.onClick.AddListener(() => OpenSubPanel(workshopWindowPanel));
        }

        // 5. 중간 선택 패널 닫기 버튼
        if (closeUnitDeckSelectButton != null)
        {
            closeUnitDeckSelectButton.onClick.AddListener(() => CloseSelectPanel(unitDeckSelectPanel));
        }

        if (closeGachaUpgradeSelectButton != null)
        {
            closeGachaUpgradeSelectButton.onClick.AddListener(() => CloseSelectPanel(gachaUpgradeSelectPanel));
        }

        if (closeInventoryWorkshopSelectButton != null)
        {
            closeInventoryWorkshopSelectButton.onClick.AddListener(() => CloseSelectPanel(inventoryWorkshopSelectPanel));
        }

        // 6. 게임 종료 버튼
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

    // 일반 스테이지 게임플레이 씬 전환
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

    // 레이드 씬 전환
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

    #region 서브 패널 및 중간 선택 패널 관리

    // 지정 중간 선택 패널 토글
    public void ToggleSelectPanel(GameObject targetPanel)
    {
        if (targetPanel == null) return;

        bool isActive = targetPanel.activeSelf;
        CloseAllSelectPanels();
        CloseAllSubPanels();
        targetPanel.SetActive(!isActive);
    }

    // 지정 중간 선택 패널 오픈
    public void OpenSelectPanel(GameObject targetPanel)
    {
        if (targetPanel == null) return;

        CloseAllSelectPanels();
        CloseAllSubPanels();
        targetPanel.SetActive(true);
    }

    // 지정 중간 선택 패널 닫기
    public void CloseSelectPanel(GameObject targetPanel)
    {
        if (targetPanel != null)
        {
            targetPanel.SetActive(false);
        }
    }

    // 모든 중간 선택 패널 일괄 닫기
    public void CloseAllSelectPanels()
    {
        if (unitDeckSelectPanel != null) unitDeckSelectPanel.SetActive(false);
        if (gachaUpgradeSelectPanel != null) gachaUpgradeSelectPanel.SetActive(false);
        if (inventoryWorkshopSelectPanel != null) inventoryWorkshopSelectPanel.SetActive(false);
    }

    // 지정 최종 서브 윈도우 패널 단독 오픈
    public void OpenSubPanel(GameObject targetPanel)
    {
        if (targetPanel == null) return;

        CloseAllSelectPanels();
        CloseAllSubPanels();
        targetPanel.SetActive(true);
    }

    // 지정 최종 서브 윈도우 패널 토글
    public void ToggleSubPanel(GameObject targetPanel)
    {
        if (targetPanel == null) return;

        bool isActive = targetPanel.activeSelf;
        CloseAllSelectPanels();
        CloseAllSubPanels();
        targetPanel.SetActive(!isActive);
    }

    // 모든 최종 서브 윈도우 패널 일괄 닫기
    public void CloseAllSubPanels()
    {
        if (collectionWindowPanel != null) collectionWindowPanel.SetActive(false);
        if (deckFormationWindowPanel != null) deckFormationWindowPanel.SetActive(false);
        if (gachaWindowPanel != null) gachaWindowPanel.SetActive(false);
        if (upgradeWindowPanel != null) upgradeWindowPanel.SetActive(false);
        if (inventoryWindowPanel != null) inventoryWindowPanel.SetActive(false);
        if (workshopWindowPanel != null) workshopWindowPanel.SetActive(false);
        if (dungeonWindowPanel != null) dungeonWindowPanel.SetActive(false);
    }

    #endregion

    #region 게임 종료

    // 애플리케이션 종료
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

    // 전체 HUD 텍스트 정보 일괄 갱신
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

    // 데이터 로드 완료 이벤트 처리
    private void OnDataLoaded(DataLoadEvent evt)
    {
        RefreshAllHUD();
    }

    // 스테이지 변경 이벤트 처리
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

    // 대용량 재화 단위 축약 포맷팅
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
