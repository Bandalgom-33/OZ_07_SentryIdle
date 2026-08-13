using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 개별 스탯/재화 업그레이드 슬롯 UI 단위를 관리하는 컨트롤러 클래스
public class UIUpgradeItemSlot : MonoBehaviour
{
    #region 직렬화 변수 (인스펙터 바인딩)

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

    [Tooltip("업그레이드 소모 비용 텍스트 (예: COST 500)")]
    [SerializeField] private TMP_Text costText;

    [Header("--- 강화 횟수별 버튼 ---")]
    [Tooltip("+1 회 강화 버튼")]
    [SerializeField] private Button btnUpgradeOne;

    [Tooltip("+10 회 연속 강화 버튼")]
    [SerializeField] private Button btnUpgradeTen;

    [Tooltip("보유 재화로 가능한 최대 강화(MAX) 버튼")]
    [SerializeField] private Button btnUpgradeMax;

    [Header("--- 버튼 상태 피드백 색상 ---")]
    [Tooltip("재화가 충분하여 구매 가능할 때의 버튼 배경 색상 (파란색 계열)")]
    [SerializeField] private Color purchasableColor = new Color(0.1f, 0.58f, 0.82f, 1f);

    [Tooltip("재화가 부족하여 구매 불가능할 때의 버튼 배경 색상 (회색 계열)")]
    [SerializeField] private Color unpurchasableColor = new Color(0.29f, 0.32f, 0.36f, 1f);

    #endregion

    #region 내부 필드 및 프로퍼티

    // 현재 슬롯이 담당하는 업그레이드 인덱스 타입 (0: 골드 보너스, 1: 골드 배율, 2: 다이아 보너스 등)
    public int UpgradeTypeIndex { get; private set; }

    #endregion

    #region 라이프 사이클 및 초기화

    private void Awake()
    {
        // 1회, 10회, MAX 강화 버튼 클릭 이벤트 동적 바인딩
        // 이유: 인스펙터 버튼 이벤트 수동 바인딩 시 발생할 수 있는 참조 누락을 방지하고 슬롯 인덱스와 강화 수량을 전달함
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
            btnUpgradeMax.onClick.AddListener(() => RequestUpgrade(-1)); // -1은 MAX 강화를 의미함
        }
    }

    // 슬롯 식별 타입 초기 설정 메서드
    public void Initialize(int typeIndex)
    {
        UpgradeTypeIndex = typeIndex;
    }

    #endregion

    #region 외부 데이터 연동 및 버튼 상태 갱신

    // 슬롯 텍스트 정보 및 재화 수량 판별에 따른 버튼 시각적 상태(활성화/색상) 일괄 갱신
    // 이유: 재화 변경 또는 레벨 변경 시 플레이어에게 명확한 구매 가능 여부 피드백을 주기 위함
    public void UpdateSlotUI(
        string statName,
        int currentLevel,
        string currentValueStr,
        string nextValueStr,
        double costOne,
        double costTen,
        int maxPurchasableCount,
        long currentAvailableCurrency)
    {
        // 1. 텍스트 정보 바인딩
        if (statNameText != null) statNameText.text = statName;
        if (levelText != null) levelText.text = $"LEVEL {currentLevel}";
        if (currentValueText != null) currentValueText.text = $"CURRENT {currentValueStr}";
        if (nextValueText != null) nextValueText.text = $"NEXT {nextValueStr}";
        if (costText != null) costText.text = $"COST {FormatNumber(costOne)}";

        // 2. +1 회 강화 버튼 상태 및 색상 갱신
        bool canAffordOne = currentAvailableCurrency >= costOne;
        UpdateButtonState(btnUpgradeOne, canAffordOne);

        // 3. +10 회 강화 버튼 상태 및 색상 갱신
        bool canAffordTen = currentAvailableCurrency >= costTen;
        UpdateButtonState(btnUpgradeTen, canAffordTen);

        // 4. MAX 강화 버튼 상태 및 색상 갱신 (구매 가능 수량이 1개 이상일 때만 활성화)
        bool canAffordMax = maxPurchasableCount > 0;
        UpdateButtonState(btnUpgradeMax, canAffordMax);
    }

    // 버튼 활성화 여부 및 배경 색상 변경 헬퍼 메서드
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

    // 강화를 요청할 때 매니저로 이벤트 발행
    private void RequestUpgrade(int count)
    {
        // UpgradeUi 스크립트 또는 UpgradeManager 이벤트에 전달
        UpgradeUi.TriggerUpgradeRequest(UpgradeTypeIndex, count);
    }

    // 대용량 숫자 포맷팅 유틸리티
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
