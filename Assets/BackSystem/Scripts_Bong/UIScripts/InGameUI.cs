using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 인게임 HUD(상단 정보 바, 하단 DP/배치 정보 바, 배속 버튼 시각화) 전체 제어 스크립트
public class InGameUI : MonoBehaviour
{
    #region 직렬화 변수 (인스펙터 바인딩)

    [Header("--- 상단 HUD 정보 바 ---")]
    [Tooltip("스테이지 및 웨이브 통합 표시 텍스트 (예: STAGE 01 | WAVE 03 / 10)")]
    [SerializeField] private Text stageWaveText;

    [Tooltip("라이프 현황 표시 텍스트 (예: LIFE 20 / 20)")]
    [SerializeField] private Text lifeText;

    [Tooltip("상단 재화 1: 골드 텍스트")]
    [SerializeField] private Text goldText;

    [Tooltip("상단 재화 2: 다이아 텍스트")]
    [SerializeField] private Text diamondText;

    [Header("--- 하단 배치 & DP 정보 바 ---")]
    [Tooltip("하단 재화 3: DP 코스트 텍스트 (예: 45 / 100)")]
    [SerializeField] private TMP_Text dpCostText;

    [Tooltip("DP 코스트 자동 회복(리젠) 타이머 슬라이더")]
    [SerializeField] private Slider dpRegenSlider;

    [Tooltip("필드에 배치된 유닛 수 표시 텍스트 (예: FIELD UNITS 0 / 10)")]
    [SerializeField] private TMP_Text fieldUnitsText;

    [Header("--- 배속 조절 버튼 및 색상 연동 ---")]
    [Tooltip("게임 속도 변경 버튼 배열 (0: Pause, 1: 1x, 2: 2x, 3: 3x)")]
    [SerializeField] private Button[] speedButtons;

    [Tooltip("선택된 배속 버튼의 배경 색상 (파란색)")]
    [SerializeField] private Color selectedSpeedColor = new Color(0.1f, 0.58f, 0.82f, 1f);

    [Tooltip("선택되지 않은 배속 버튼의 배경 색상 (회색)")]
    [SerializeField] private Color unselectedSpeedColor = new Color(0.29f, 0.32f, 0.36f, 1f);

    #endregion

    #region 내부 변수 및 상수

    // 대용량 재화 표기를 위한 단위 문자열 배열 (1,000 단위마다 표기 변경)
    private static readonly string[] NumFormats = { "", "K", "M", "B", "T", "Qa", "Qi" };

    #endregion

    #region 이벤트 선언

    // 배속 변경 시 외부 시스템(GameManager 등)으로 변경된 속도 인덱스를 전파하기 위한 C# 이벤트
    public static event Action<int> OnGameSpeedChange;

    #endregion

    #region 라이프 사이클

    private void Awake()
    {
        // 배속 버튼 클릭 이벤트 동적 바인딩 처리
        // 이유: 인스펙터에서 UnityEvent로 개별 지정 시 누락 위험이 있어 스크립트에서 속도 인덱스(0~3)를 안전하게 람다로 연결함
        if (speedButtons != null)
        {
            for (int i = 0; i < speedButtons.Length; i++)
            {
                if (speedButtons[i] == null) continue;

                int speedIndex = i;
                speedButtons[i].onClick.AddListener(() => OnSpeedButtonClicked(speedIndex));
            }
        }

        // 기본 1x 배속(인덱스 1) 시각화 적용
        // 이유: 게임 초기화 시 기본 속도 상태(1x 파란색, 나머지 회색)를 시각적으로 명확히 노출함
        SetSpeedButtonVisual(1);
    }

    private void OnEnable()
    {
        // CurrencyManager C# 액션 이벤트 구독
        // 이유: 싱글톤 또는 관리자 클래스의 재화 변경 발생 시 느슨한 결합(Loose Coupling)으로 UI를 자동 갱신하기 위함
        CurrencyManager.OnGoldChange += UpdateGoldUI;
        CurrencyManager.OnDiamondChange += UpdateDiamondUI;
        CurrencyManager.OnDpCostChange += UpdateDpCostUI;
        CurrencyManager.OnDpCostSliderChange += UpdateDpRegenSlider;
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제
        // 이유: 객체 파괴 또는 비활성화 시 dangling reference로 인한 메모리 누수 및 NullReferenceException 방지
        CurrencyManager.OnGoldChange -= UpdateGoldUI;
        CurrencyManager.OnDiamondChange -= UpdateDiamondUI;
        CurrencyManager.OnDpCostChange -= UpdateDpCostUI;
        CurrencyManager.OnDpCostSliderChange -= UpdateDpRegenSlider;
    }

    #endregion

    #region 배속 버튼 처리 및 시각 효과

    // 배속 버튼 클릭 시 처리 로직
    private void OnSpeedButtonClicked(int speedIndex)
    {
        // 선택한 배속 버튼 색상 하이라이트 변경
        SetSpeedButtonVisual(speedIndex);

        // 게임 속도 변경 이벤트를 발행하여 GameManager 및 기타 시스템이 속도를 전환하도록 유도
        OnGameSpeedChange?.Invoke(speedIndex);
    }

    // 선택된 배속 버튼은 파란색, 선택되지 않은 버튼은 회색으로 색상 상태 변경
    // 이유: 유저가 현재 활성화된 게임 배속 상태를 즉각 파악할 수 있도록 시각적 피드백을 제공함
    public void SetSpeedButtonVisual(int selectedIndex)
    {
        if (speedButtons == null) return;

        for (int i = 0; i < speedButtons.Length; i++)
        {
            if (speedButtons[i] == null) continue;

            Image btnImage = speedButtons[i].GetComponent<Image>();
            if (btnImage != null)
            {
                // 선택된 인덱스는 파란색(selectedSpeedColor), 그 외에는 회색(unselectedSpeedColor) 적용
                btnImage.color = (i == selectedIndex) ? selectedSpeedColor : unselectedSpeedColor;
            }
        }
    }

    #endregion

    #region UI 갱신 메서드 모음

    // 1. 스테이지 및 웨이브 통합 텍스트 갱신
    // 이유: 상단 HUD 중앙/좌측에 현재 진행 중인 스테이지와 웨이브 상태를 합쳐서 가독성 높게 표시
    public void UpdateStageWaveUI(int stage, int currentWave, int maxWave)
    {
        if (stageWaveText != null)
        {
            stageWaveText.text = $"STAGE {stage:D2} | WAVE {currentWave:D2} / {maxWave:D2}";
        }
    }

    // 2. 라이프 수치 텍스트 갱신
    // 이유: 플레이어의 기지/캐릭터 남은 라이프 현황을 갱신
    public void UpdateLifeUI(int currentLife, int maxLife)
    {
        if (lifeText != null)
        {
            lifeText.text = $"LIFE {currentLife} / {maxLife}";
        }
    }

    // 3. 상단 재화 1 (골드) 텍스트 갱신
    private void UpdateGoldUI(long gold)
    {
        if (goldText != null)
        {
            goldText.text = FormatCurrencyNumber(gold);
        }
    }

    // 4. 상단 재화 2 (다이아) 텍스트 갱신
    private void UpdateDiamondUI(int diamond)
    {
        if (diamondText != null)
        {
            diamondText.text = FormatCurrencyNumber(diamond);
        }
    }

    // 5. 하단 재화 3 (DP 코스트) 텍스트 갱신
    private void UpdateDpCostUI(int dpCost)
    {
        if (dpCostText != null)
        {
            dpCostText.text = FormatCurrencyNumber(dpCost) +$"/{CurrencyManager.Instance.MaxDpCost}" ;
        }
    }

    // 6. DP 코스트 리젠 슬라이더 갱신 (progress: 0.0f ~ 1.0f)
    // 이유: DP 코스트가 차오르는 타이머/게이지를 시각적으로 실시간 표현
    public void UpdateDpRegenSlider(float progress)
    {
        if (dpRegenSlider != null)
        {
            dpRegenSlider.value = Mathf.Clamp01(progress);
        }
    }

    // 7. 필드 유닛 수 텍스트 갱신
    // 이유: 현재 맵 상에 배치된 소환 유닛 수와 최대 배치 가능 유닛 제한을 안내
    public void UpdateFieldUnitsUI(int currentUnits, int maxUnits)
    {
        if (fieldUnitsText != null)
        {
            fieldUnitsText.text = $"FIELD UNITS {currentUnits} / {maxUnits}";
        }
    }

    #endregion

    #region 유틸리티 메서드

    // 단위 포맷팅 처리 함수 (K, M, B 등)
    // 이유: 방치형 게임 특성상 숫자가 매우 커지므로 1,000 단위마다 약어로 환산하여 UI 텍스트 오버플로우 방지
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
