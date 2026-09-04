using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 업그레이드 슬롯 UI 요소 제어 및 강화 요청 중계 컴포넌트
public class UIUpgradeItemSlot : MonoBehaviour
{
    #region 직렬화 변수

    [Header("--- UI 아이콘 및 텍스트 ---")]
    [Tooltip("업그레이드 스탯 대표 아이콘 이미지")]
    [SerializeField] private Image statIcon;

    [Tooltip("업그레이드 항목 명칭 텍스트 (예: Gold Bonus)")]
    [SerializeField] private TMP_Text statNameText;

    [Tooltip("현재 업그레이드 레벨 텍스트 (예: LEVEL 1)")]
    [SerializeField] private TMP_Text levelText;

    [Tooltip("현재 적용 수치 텍스트 (예: CURRENT 100)")]
    [SerializeField] private TMP_Text currentValueText;

    [Tooltip("다음 레벨 적용 수치 텍스트 (예: NEXT 110)")]
    [SerializeField] private TMP_Text nextValueText;

    [Tooltip("업그레이드 소모 비용 및 부족액 통합 텍스트")]
    [SerializeField] private TMP_Text costText;

    [Header("--- 강화 횟수별 버튼 ---")]
    [Tooltip("+1 회 강화 버튼")]
    [SerializeField] private Button btnUpgradeOne;

    [Tooltip("+10 회 연속 강화 버튼")]
    [SerializeField] private Button btnUpgradeTen;

    [Tooltip("보유 재화로 가능한 최대 강화(MAX) 버튼")]
    [SerializeField] private Button btnUpgradeMax;

    [Header("--- 버튼 상태 피드백 색상 ---")]
    [Tooltip("구매 가능할 때 버튼 배경 색상")]
    [SerializeField] private Color purchasableColor = new Color(0.1f, 0.58f, 0.82f, 1f);

    [Tooltip("구매 불가능할 때 버튼 배경 색상")]
    [SerializeField] private Color unpurchasableColor = new Color(0.29f, 0.32f, 0.36f, 1f);

    #endregion

    #region 내부 필드 및 프로퍼티

    public int UpgradeTypeIndex { get; private set; }

    #endregion

    #region 라이프 사이클 및 초기화

    // 강화 버튼 클릭 이벤트 리스너 바인딩
    private void Awake()
    {
        if (btnUpgradeOne != null)
        {
            btnUpgradeOne.onClick.AddListener(() => RequestUpgrade(1));
        }

        if (btnUpgradeTen != null)
        {
            btnUpgradeTen.onClick.AddListener(() => RequestUpgrade(10));
        }

        if (btnUpgradeMax != null)
        {
            btnUpgradeMax.onClick.AddListener(() => RequestUpgrade(-1));
        }
    }

    // 업그레이드 슬롯 타입 인덱스 초기화
    public void Initialize(int typeIndex)
    {
        UpgradeTypeIndex = typeIndex;
    }

    #endregion

    #region 외부 데이터 연동 및 버튼 상태 갱신

    // 슬롯 텍스트 및 버튼 활성화 상태 갱신
    public void UpdateSlotUI(
        string statName,
        int currentLevel,
        string currentValueStr,
        string nextValueStr,
        double costOne,
        double costTen,
        double costMax,
        int maxPurchasableCount,
        long currentAvailableCurrency,
        bool isMaxLevel)
    {
        if (statNameText != null) statNameText.text = statName;
        if (levelText != null) levelText.text = $"Lv.{currentLevel}";
        if (currentValueText != null) currentValueText.text = $"현재 {currentValueStr}";
        if (nextValueText != null) nextValueText.text = isMaxLevel ? "최대 레벨" : $"다음 {nextValueStr}";

        if (costText != null)
        {
            if (isMaxLevel)
            {
                costText.text = "<color=#FFD700>최대 레벨 달성</color>";
            }
            else
            {
                string oneCostStr = FormatCostWithDeficit(costOne, currentAvailableCurrency);
                string tenCostStr = FormatCostWithDeficit(costTen, currentAvailableCurrency);
                string maxCostStr = maxPurchasableCount > 0
                    ? $"<color=#FFA500>최대({maxPurchasableCount})</color>: <color=#55FF55>{FormatNumber(costMax)}</color>"
                    : $"<color=#FFA500>최대(0)</color> <color=#FF4444>(-{FormatNumber(costOne - currentAvailableCurrency)})</color>";

                costText.text = $"<color=#FFA500>+1</color>: {oneCostStr} | <color=#FFA500>+10</color>: {tenCostStr} | {maxCostStr}";
            }
        }

        bool canAffordOne = !isMaxLevel && currentAvailableCurrency >= costOne;
        UpdateButtonState(btnUpgradeOne, canAffordOne);

        bool canAffordTen = !isMaxLevel && currentAvailableCurrency >= costTen;
        UpdateButtonState(btnUpgradeTen, canAffordTen);

        bool canAffordMax = !isMaxLevel && maxPurchasableCount > 0;
        UpdateButtonState(btnUpgradeMax, canAffordMax);
    }

    // 단일 비용 및 부족액 포맷팅 문자열 반환
    private string FormatCostWithDeficit(double cost, long availableCurrency)
    {
        if (availableCurrency >= cost)
        {
            return $"<color=#55FF55>{FormatNumber(cost)}</color>";
        }

        double deficit = cost - availableCurrency;
        return $"<color=#55FF55>{FormatNumber(cost)}</color> <color=#FF4444>(-{FormatNumber(deficit)})</color>";
    }

    // 버튼 상호작용 및 배경 색상 갱신
    private void UpdateButtonState(Button targetButton, bool isPurchasable)
    {
        if (targetButton == null) return;

        targetButton.interactable = isPurchasable;

        Image buttonImage = targetButton.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = isPurchasable ? purchasableColor : unpurchasableColor;
        }
    }

    #endregion

    #region 내부 업그레이드 이벤트 요청

    // 업그레이드 실행 요청 중계
    private void RequestUpgrade(int count)
    {
        UpgradeUi.TriggerUpgradeRequest(UpgradeTypeIndex, count);
    }

    // 대용량 숫자 단위 축약 포맷팅
    private string FormatNumber(double value)
    {
        if (value < 1000) return value.ToString("N0");

        string[] formats = { "", "K", "M", "B", "T", "Qa", "Qi" };
        int index = 0;
        while (value >= 1000 && index < formats.Length - 1)
        {
            value /= 1000;
            index++;
        }
        return value.ToString("N1") + formats[index];
    }

    #endregion
}
