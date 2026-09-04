using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 메인 로비 UI 상태 제어 및 서브 시스템 패널 중계 컴포넌트
public class MainLobbyUI : MonoBehaviour
{
    #region 싱글톤 인스턴스

    public static MainLobbyUI Instance { get; private set; }

    #endregion

    #region 직렬화 변수

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

    [Tooltip("오프라인 방치 경과 시간 및 획득 보상 내역 표시 단일 통합 TMP 텍스트")]
    [SerializeField] private TMP_Text offlineRewardInfoText;

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

    [Header("--- 레이드 진입 버튼 상태 및 스프라이트 ---")]
    [Tooltip("레이드 진입 버튼의 Image 컴포넌트")]
    [SerializeField] private Image raidButtonImage;

    [Tooltip("레이드 덱 조건 충족(16명 완편성) 시 활성화 스프라이트")]
    [SerializeField] private Sprite raidButtonUnlockedSprite;

    [Tooltip("레이드 덱 조건 미충족(빈 슬롯 존재) 시 잠금 스프라이트")]
    [SerializeField] private Sprite raidButtonLockedSprite;

    [Header("--- 서브 시스템 직접 오픈 버튼 (7종) ---")]
    [Tooltip("유닛 보관함 윈도우 오픈 버튼")]
    [SerializeField] private Button openCollectionButton;

    [Tooltip("덱 편성 윈도우 오픈 버튼")]
    [SerializeField] private Button openDeckButton;

    [Tooltip("가챠 윈도우 오픈 버튼")]
    [SerializeField] private Button openGachaButton;

    [Tooltip("업그레이드 윈도우 오픈 버튼")]
    [SerializeField] private Button openUpgradeButton;

    [Tooltip("인벤토리 윈도우 오픈 버튼")]
    [SerializeField] private Button openInventoryButton;

    [Tooltip("공방 윈도우 오픈 버튼")]
    [SerializeField] private Button openWorkshopButton;

    [Tooltip("던전 윈도우 오픈 버튼")]
    [SerializeField] private Button openDungeonButton;

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

    [Tooltip("메인 로비 화면의 게임 종료 버튼")]
    [SerializeField] private Button lobbyQuitButton;

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
        EventBus.Subscribe<OfflineRewardReportEvent>(OnOfflineRewardReported);
        EventBus.Subscribe<RaidDeckChangedEvent>(OnRaidDeckChanged);

        RefreshAllHUD();
        UpdateRaidButtonState();
    }

    // 전역 이벤트 구독 해제
    private void OnDisable()
    {
        CurrencyManager.OnGoldChange -= UpdateGoldUI;
        CurrencyManager.OnDiamondChange -= UpdateDiamondUI;
        EventBus.Unsubscribe<StageWaveChangedEvent>(OnStageWaveChanged);
        EventBus.Unsubscribe<DataLoadEvent>(OnDataLoaded);
        EventBus.Unsubscribe<OfflineRewardReportEvent>(OnOfflineRewardReported);
        EventBus.Unsubscribe<RaidDeckChangedEvent>(OnRaidDeckChanged);
    }

    // 첫 실행 여부에 따른 패널 상태 초기화 및 HUD/레이드 버튼 갱신
    private void Start()
    {
        ApplyInitialPanelState();
        RefreshAllHUD();
        UpdateRaidButtonState();
    }

    // 싱글톤 참조 해제
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    #endregion

    #region 오프라인 방치 보상 이벤트 핸들러 및 텍스트 빌더

    // 레이드 토벌 성공 보상 안내 텍스트 조립
    private string BuildRaidRewardText(long raidStone)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("<size=120%><color=#FFD700><b>[ 레이드 토벌 성공! ]</b></color></size>");
        sb.AppendLine();
        sb.AppendLine("강력한 레이드 보스를 성공적으로 격퇴했습니다!");
        sb.AppendLine();
        sb.AppendLine("[획득 보상]");
        sb.AppendLine($"• 레이드 마석: <color=#00FFFF>+{raidStone:N0}개</color>");
        sb.AppendLine();
        sb.AppendLine("<color=#AAAAAA>획득한 마석으로 공방에서 고급 아이템을 제작할 수 있습니다.</color>");
        return sb.ToString();
    }

    // 오프라인 방치 시간 및 보상 내역 문자열 조립
    private string BuildOfflineRewardText(OfflineRewardReportData report)
    {
        if (report == null)
        {
            return "오프라인 보상 데이터가 없습니다.";
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"방치 시간: <color=#00FFFF>{report.FormattedDuration}</color>");
        sb.AppendLine();

        if (report.HasAnyReward)
        {
            sb.AppendLine("[오프라인 획득 보상]");

            if (report.GainedGold > 0)
            {
                sb.AppendLine($"• 골드: <color=#FFD700>+{report.GainedGold:N0}</color>");
            }

            if (report.GainedWaveStone > 0)
            {
                sb.AppendLine($"• 웨이브 마석: <color=#00FFFF>+{report.GainedWaveStone:N0}</color>");
            }

            if (report.GainedConsumables != null && report.GainedConsumables.Count > 0)
            {
                foreach (var pair in report.GainedConsumables)
                {
                    if (pair.Value > 0)
                    {
                        sb.AppendLine($"• {pair.Key}: <color=#00FF00>+{pair.Value}개</color>");
                    }
                }
            }

            if (report.DungeonCompletedCycles != null && report.DungeonCompletedCycles.Count > 0)
            {
                foreach (var pair in report.DungeonCompletedCycles)
                {
                    if (pair.Value > 0)
                    {
                        sb.AppendLine($"• 던전 [{pair.Key}]: <color=#FFA500>{pair.Value}회 완료</color>");
                    }
                }
            }
        }
        else
        {
            sb.AppendLine("획득한 오프라인 보상이 없습니다.");
        }

        return sb.ToString();
    }

    // 오프라인 방치 보상 이벤트 수신 처리
    private void OnOfflineRewardReported(OfflineRewardReportEvent evt)
    {
        if (CurrencyManager.Instance != null && CurrencyManager.Instance.HasPendingRaidRewardReport)
        {
            return;
        }

        if (!_isFirstLaunch)
        {
            return;
        }

        if (offlineRewardInfoText != null && evt.reportData != null)
        {
            offlineRewardInfoText.text = BuildOfflineRewardText(evt.reportData);
        }
    }

    #endregion

    #region 시작 패널 및 메인 패널 제어

    // 첫 진입 여부에 따른 시작 패널 및 메인 로비 패널 상태 적용
    private void ApplyInitialPanelState()
    {
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

            if (CurrencyManager.Instance != null && CurrencyManager.Instance.HasPendingRaidRewardReport)
            {
                if (offlineRewardInfoText != null)
                {
                    offlineRewardInfoText.text = BuildRaidRewardText(CurrencyManager.Instance.LastRewardedRaidStone);
                }
                ShowOfflineRewardPopup();
            }
        }
    }

    // 게임 시작 버튼 클릭 이벤트 처리
    public void OnStartGameClicked()
    {
        _isFirstLaunch = false;

        if (startPanel != null) startPanel.SetActive(false);
        if (mainLobbyPanel != null) mainLobbyPanel.SetActive(true);

        bool isExistingUser = SaveManager.Instance != null && SaveManager.Instance.HasExistingSaveFile;
        if (isExistingUser && OfflineRewardManager.Instance != null && OfflineRewardManager.Instance.LastReportData != null)
        {
            if (offlineRewardInfoText != null)
            {
                offlineRewardInfoText.text = BuildOfflineRewardText(OfflineRewardManager.Instance.LastReportData);
            }
            ShowOfflineRewardPopup();
        }

        if (GuideManager.Instance != null)
        {
            GuideManager.Instance.StartGuideIfNeeded();
        }
    }

    #endregion

    #region 오프라인 / 레이드 보상 팝업 제어

    // 보상 팝업 패널 활성화
    public void ShowOfflineRewardPopup()
    {
        SetOfflineRewardPanelActive(true);
    }

    // 보상 팝업 패널 닫기 및 대기 리포트 초기화
    public void CloseOfflineRewardPopup()
    {
        SetOfflineRewardPanelActive(false);

        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.ClearPendingRaidRewardReport();
        }
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
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(OnStartGameClicked);
        }

        if (closeOfflineRewardButton != null)
        {
            closeOfflineRewardButton.onClick.AddListener(CloseOfflineRewardPopup);
        }

        if (enterGamePlayButton != null)
        {
            enterGamePlayButton.onClick.AddListener(OnEnterGamePlayClicked);
        }

        if (enterRaidButton != null)
        {
            enterRaidButton.onClick.AddListener(OnEnterRaidClicked);
        }

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
            openInventoryButton.onClick.AddListener(OpenInventoryOnly);
        }

        if (openWorkshopButton != null)
        {
            openWorkshopButton.onClick.AddListener(() => OpenSubPanel(workshopWindowPanel));
        }

        if (openDungeonButton != null)
        {
            openDungeonButton.onClick.AddListener(() => OpenSubPanel(dungeonWindowPanel));
        }

        if (titleQuitButton != null)
        {
            titleQuitButton.onClick.AddListener(QuitGame);
        }

        if (lobbyQuitButton != null)
        {
            lobbyQuitButton.onClick.AddListener(QuitGame);
        }
    }

    #endregion

    #region 씬 전환 및 레이드 잠금 제어

    // 일반 스테이지 게임플레이 씬 전환 요청
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

    // 레이드 씬 전환 요청 (16명 완편성 검증)
    public void OnEnterRaidClicked()
    {
        if (DeckManager.Instance != null && !DeckManager.Instance.IsRaidDeckFullyEquipped)
        {
            Debug.LogWarning($"[MainLobbyUI] 레이드 덱 미편성 슬롯이 {DeckManager.Instance.RaidTotalEmptySlotCount}개 존재하여 진입할 수 없습니다.");
            return;
        }

        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadScene(SceneType.Raid);
        }
        else
        {
            Debug.LogError("[MainLobbyUI] SceneLoader 인스턴스가 존재하지 않습니다.");
        }
    }

    // 레이드 버튼 활성화 및 잠금 스프라이트 상태 갱신
    public void UpdateRaidButtonState()
    {
        bool isRaidReady = DeckManager.Instance != null && DeckManager.Instance.IsRaidDeckFullyEquipped;

        if (enterRaidButton != null)
        {
            enterRaidButton.interactable = isRaidReady;
        }

        if (raidButtonImage != null)
        {
            if (isRaidReady && raidButtonUnlockedSprite != null)
            {
                raidButtonImage.sprite = raidButtonUnlockedSprite;
            }
            else if (!isRaidReady && raidButtonLockedSprite != null)
            {
                raidButtonImage.sprite = raidButtonLockedSprite;
            }
        }
    }

    // 레이드 덱 변경 이벤트 수신 콜백
    private void OnRaidDeckChanged(RaidDeckChangedEvent evt)
    {
        UpdateRaidButtonState();
    }

    #endregion

    #region 서브 패널 관리

    // 지정 최종 서브 윈도우 패널 활성화
    public void OpenSubPanel(GameObject targetPanel)
    {
        if (targetPanel == null) return;

        CloseAllSubPanels();
        targetPanel.SetActive(true);
    }

    // 인벤토리 전용 패널 오픈
    public void OpenInventoryOnly()
    {
        CloseAllSubPanels();

        if (inventoryWindowPanel != null)
        {
            inventoryWindowPanel.SetActive(true);
        }

        if (InventoryUI.Instance != null)
        {
            InventoryUI.Instance.OpenBagOnly();
        }
    }

    // 장비 관리 팝업 오픈
    public void OpenEquipmentPopup(string unitId)
    {
        if (inventoryWindowPanel != null)
        {
            inventoryWindowPanel.SetActive(true);
            inventoryWindowPanel.transform.SetAsLastSibling();
        }

        if (InventoryUI.Instance != null)
        {
            InventoryUI.Instance.OpenEquipment(unitId);
        }
    }

    // 지정 최종 서브 윈도우 패널 토글
    public void ToggleSubPanel(GameObject targetPanel)
    {
        if (targetPanel == null) return;

        bool isActive = targetPanel.activeSelf;
        CloseAllSubPanels();
        targetPanel.SetActive(!isActive);
    }

    // 모든 최종 서브 윈도우 패널 일괄 비활성화
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

    // 애플리케이션 종료 처리
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

    // 전체 HUD 텍스트 정보 및 레이드 버튼 상태 일괄 갱신
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

        UpdateRaidButtonState();
    }

    // 데이터 로드 완료 이벤트 수신 처리
    private void OnDataLoaded(DataLoadEvent evt)
    {
        RefreshAllHUD();
    }

    // 스테이지 변경 이벤트 수신 처리
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

    // 대용량 숫자 단위 축약 포맷팅
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
