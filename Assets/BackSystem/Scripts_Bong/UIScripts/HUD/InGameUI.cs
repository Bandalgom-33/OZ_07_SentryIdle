using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InGameUI : MonoBehaviour
{
    #region 직렬화 변수 (인스펙터 바인딩)

    [Header("--- 상단 HUD 정보 바 ---")]
    [Tooltip("스테이지 및 웨이브 통합 표시 텍스트 (예: STAGE 01 | WAVE 03 / 10)")]
    [SerializeField] private TMP_Text stageWaveText;

    [Tooltip("라이프 현황 표시 텍스트 (예: LIFE 20 / 20)")]
    [SerializeField] private TMP_Text lifeText;

    [Tooltip("상단 재화 1: 골드 텍스트")]
    [SerializeField] private TMP_Text goldText;

    [Tooltip("상단 재화 2: 다이아 텍스트")]
    [SerializeField] private TMP_Text diamondText;

    [Header("--- 하단 배치 & DP 정보 바 ---")]
    [Tooltip("하단 재화 3: DP 코스트 텍스트 (예: 45 / 100)")]
    [SerializeField] private TMP_Text dpCostText;

    [Tooltip("DP 코스트 자동 회복(리젠) 타이머 슬라이더")]
    [SerializeField] private Slider dpRegenSlider;

    [Tooltip("필드에 배치된 유닛 수 표시 텍스트 (예: FIELD UNITS 0 / 10)")]
    [SerializeField] private TMP_Text fieldUnitsText;

    [Header("--- 씬 전환 제어 버튼 ---")]
    [Tooltip("메인 로비 씬으로 복귀하는 버튼")]
    [SerializeField] private Button returnToLobbyButton;

    [Header("--- 배속 조절 버튼 및 색상 연동 ---")]
    [Tooltip("게임 속도 변경 버튼 배열 (0: Pause, 1: 1x, 2: 2x, 3: 3x)")]
    [SerializeField] private Button[] speedButtons;

    [Tooltip("선택된 배속 버튼의 배경 색상")]
    [SerializeField] private Color selectedSpeedColor = new Color(0.1f, 0.58f, 0.82f, 1f);

    [Tooltip("선택되지 않은 배속 버튼의 배경 색상")]
    [SerializeField] private Color unselectedSpeedColor = new Color(0.29f, 0.32f, 0.36f, 1f);

    #endregion

    #region 내부 변수 및 상수

    private static readonly string[] NumFormats = { "", "K", "M", "B", "T", "Qa", "Qi" };

    #endregion

    #region 이벤트 선언

    public static event Action<int> OnGameSpeedChange;

    #endregion

    #region 라이프 사이클

    // 버튼 이벤트 바인딩 및 속도 비주얼 초기화
    private void Awake()
    {
        // 로비 씬 복귀 버튼 리스너 바인딩
        if (returnToLobbyButton != null)
        {
            returnToLobbyButton.onClick.AddListener(OnReturnToLobbyClicked);
        }

        // 배속 조절 버튼 리스너 바인딩
        if (speedButtons != null)
        {
            for (int i = 0; i < speedButtons.Length; i++)
            {
                if (speedButtons[i] == null) continue;

                int speedIndex = i;
                speedButtons[i].onClick.AddListener(() => OnSpeedButtonClicked(speedIndex));
            }
        }

        SetSpeedButtonVisual(1);
    }

    #endregion

    #region 씬 전환 처리

    // 메인 로비 씬으로 복귀 요청 처리
    public void OnReturnToLobbyClicked()
    {
        Debug.Log("[InGameUI] 메인 로비 씬 복귀를 요청합니다.");

        // SceneLoader를 통해 자동 세이브 및 페이드 아웃 연출과 함께 안전하게 로비 씬 전환
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadScene(SceneType.Lobby);
        }
        else
        {
            // SceneLoader가 없는 독립 씬 테스트 환경을 위한 폴백 처리
            Debug.LogWarning("[InGameUI] SceneLoader 인스턴스가 존재하지 않아 기본 씬 매니저로 로비 씬을 로드합니다.");
            UnityEngine.SceneManagement.SceneManager.LoadScene("TestBuild2MainLobby");
        }
    }

    // 이벤트 버스 및 시스템 액션 구독
    private void OnEnable()
    {
        CurrencyManager.OnGoldChange += UpdateGoldUI;
        CurrencyManager.OnDiamondChange += UpdateDiamondUI;
        CurrencyManager.OnDpCostChange += UpdateDpCostUI;
        CurrencyManager.OnDpCostSliderChange += UpdateDpRegenSlider;
        GameManager.OnLifeChanged += UpdateLifeUI;

        // 스테이지 및 웨이브 변경 이벤트 구독
        EventBus.Subscribe<StageWaveChangedEvent>(OnStageWaveChanged);
        EventBus.Subscribe<SceneLoadCompletedEvent>(OnSceneLoadCompleted);
        EventBus.Subscribe<DataLoadEvent>(OnDataLoaded);

        RefreshAllHUD();
    }

    // 씬 시작 시 최신 데이터로 HUD 즉시 갱신
    private void Start()
    {
        RefreshAllHUD();
    }

    // 인게임 전체 HUD 정보 일괄 갱신 연산
    public void RefreshAllHUD()
    {
        if (CurrencyManager.Instance != null)
        {
            UpdateGoldUI(CurrencyManager.Instance.Gold);
            UpdateDiamondUI(CurrencyManager.Instance.Diamond);
            UpdateDpCostUI(CurrencyManager.Instance.DpCost);
        }

        if (GameManager.Instance != null)
        {
            UpdateLifeUI(GameManager.Instance.CurrentLife, GameManager.Instance.MaxLife);
        }

        if (StageProgressManager.Instance != null)
        {
            UpdateStageWaveUI(StageProgressManager.Instance.CurrentStage, StageProgressManager.Instance.CurrentWave, StageProgressManager.Instance.WavesPerStage);
        }
    }

    // 이벤트 구독 해제 연산
    private void OnDisable()
    {
        CurrencyManager.OnGoldChange -= UpdateGoldUI;
        CurrencyManager.OnDiamondChange -= UpdateDiamondUI;
        CurrencyManager.OnDpCostChange -= UpdateDpCostUI;
        CurrencyManager.OnDpCostSliderChange -= UpdateDpRegenSlider;
        GameManager.OnLifeChanged -= UpdateLifeUI;

        EventBus.Unsubscribe<StageWaveChangedEvent>(OnStageWaveChanged);
        EventBus.Unsubscribe<SceneLoadCompletedEvent>(OnSceneLoadCompleted);
        EventBus.Unsubscribe<DataLoadEvent>(OnDataLoaded);
    }

    // 씬 로드 완료 이벤트 핸들러
    private void OnSceneLoadCompleted(SceneLoadCompletedEvent evt)
    {
        RefreshAllHUD();
    }

    // 데이터 로드 완료 이벤트 핸들러
    private void OnDataLoaded(DataLoadEvent evt)
    {
        RefreshAllHUD();
    }

    // 스테이지 및 웨이브 변경 이벤트 핸들러 (5웨이브 기준 표시)
    private void OnStageWaveChanged(StageWaveChangedEvent evt)
    {
        int maxWave = (StageProgressManager.Instance != null) ? StageProgressManager.Instance.WavesPerStage : 5;
        UpdateStageWaveUI(evt.stageNumber, evt.waveNumber, maxWave);
    }

    #endregion

    #region 배속 버튼 처리 및 시각 효과

    // 배속 버튼 클릭 이벤트 처리
    private void OnSpeedButtonClicked(int speedIndex)
    {
        SetSpeedButtonVisual(speedIndex);
        OnGameSpeedChange?.Invoke(speedIndex);
    }

    // 배속 버튼 하이라이트 색상 설정
    public void SetSpeedButtonVisual(int selectedIndex)
    {
        if (speedButtons == null) return;

        for (int i = 0; i < speedButtons.Length; i++)
        {
            if (speedButtons[i] == null) continue;

            Image btnImage = speedButtons[i].GetComponent<Image>();
            if (btnImage != null)
            {
                btnImage.color = (i == selectedIndex) ? selectedSpeedColor : unselectedSpeedColor;
            }
        }
    }

    #endregion

    #region UI 갱신 메서드 모음

    // 스테이지 및 웨이브 텍스트 갱신
    public void UpdateStageWaveUI(int stage, int currentWave, int maxWave)
    {
        if (stageWaveText != null)
        {
            stageWaveText.text = $"STAGE {stage:D2} | WAVE {currentWave:D2} / {maxWave:D2}";
        }
    }

    // 라이프 수치 텍스트 갱신
    public void UpdateLifeUI(int currentLife, int maxLife)
    {
        if (lifeText != null)
        {
            lifeText.text = $"LIFE {currentLife} / {maxLife}";
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

    // DP 코스트 텍스트 갱신
    private void UpdateDpCostUI(int dpCost)
    {
        if (dpCostText != null)
        {
            dpCostText.text = FormatCurrencyNumber(dpCost) +$"/{CurrencyManager.Instance.MaxDpCost}" ;
        }
    }

    // DP 회복 슬라이더 갱신
    public void UpdateDpRegenSlider(float progress)
    {
        if (dpRegenSlider != null)
        {
            dpRegenSlider.value = Mathf.Clamp01(progress);
        }
    }

    // 필드 유닛 수 텍스트 갱신
    public void UpdateFieldUnitsUI(int currentUnits, int maxUnits)
    {
        if (fieldUnitsText != null)
        {
            fieldUnitsText.text = $"FIELD UNITS {currentUnits} / {maxUnits}";
        }
    }

    #endregion

    #region 유틸리티 메서드

    // 대용량 단위 축약 포맷팅 처리
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
