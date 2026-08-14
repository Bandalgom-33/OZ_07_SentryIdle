using System;
using UnityEngine;
using UnityEngine.UI;

// 업그레이드 UI 전체 패널(상단 보유 골드, 닫기 버튼, 하위 슬롯 리스트 피드백)을 관장하는 UI 매니저
public class UpgradeUi : MonoBehaviour
{
    #region 직렬화 변수 (인스펙터 바인딩)

    [Header("--- 상단 UI 요소 ---")]
    [Tooltip("상단 보유 골드 표시 텍스트 (예: AVAILABLE GOLD 12,345)")]
    [SerializeField] private Text availableGoldText;

    [Tooltip("패널 닫기/종료 버튼")]
    [SerializeField] private Button exitButton;

    [Header("--- 업그레이드 슬롯 리스트 ---")]
    [Tooltip("패널 내에 등록된 스탯 업그레이드 슬롯 컴포넌트 목록")]
    [SerializeField] private UIUpgradeItemSlot[] upgradeSlots;

    #endregion

    #region 이벤트 선언

    // 슬롯에서 버튼 클릭 발생 시 (타입 인덱스, 구매 횟수(1, 10, -1))를 전달하기 위한 C# 이벤트
    public static event Action<int, int> OnCurrencyUpgrade;

    #endregion

    #region 라이프 사이클

    private void Awake()
    {
        // 닫기 버튼 클릭 시 패널 비활성화 처리
        if (exitButton != null)
        {
            exitButton.onClick.AddListener(ClosePanel);
        }

        // 슬롯들의 고유 타입 인덱스(0 ~ 5) 초기화
        if (upgradeSlots != null)
        {
            for (int i = 0; i < upgradeSlots.Length; i++)
            {
                if (upgradeSlots[i] != null)
                {
                    upgradeSlots[i].Initialize(i);
                }
            }
        }
    }

    private void OnEnable()
    {
        // 골드, 다이아, DP 코스트 등 재화 변경 및 업그레이드 성공 이벤트 구독
        // 이유: 슬롯마다 소모하는 재화가 다르므로 어떤 재화가 변경되어도 버튼 상태 피드백을 실시간 자동 갱신함
        CurrencyManager.OnGoldChange += OnGoldAmountChanged;
        CurrencyManager.OnDiamondChange += OnOtherCurrencyChanged;
        CurrencyManager.OnDpCostChange += OnOtherCurrencyChanged;
        CurrencyUpgradeManager.OnUpgradeCompleted += RefreshAllSlots;

        // 패널 열릴 때 전체 슬롯 및 골드 UI 초기 갱신
        RefreshAllSlots();
    }

    private void OnDisable()
    {
        // 메모리 누수 방지 이벤트 해제
        CurrencyManager.OnGoldChange -= OnGoldAmountChanged;
        CurrencyManager.OnDiamondChange -= OnOtherCurrencyChanged;
        CurrencyManager.OnDpCostChange -= OnOtherCurrencyChanged;
        CurrencyUpgradeManager.OnUpgradeCompleted -= RefreshAllSlots;
    }

    private void OnOtherCurrencyChanged(int amount)
    {
        RefreshAllSlots();
    }

    #endregion

    #region 슬롯 강화 요청 중계

    // UIUpgradeItemSlot에서 클릭 시 호출되는 정적 요청 메서드
    public static void TriggerUpgradeRequest(int typeIndex, int count)
    {
        OnCurrencyUpgrade?.Invoke(typeIndex, count);
    }

    #endregion

    #region UI 갱신 로직

    // 보유 골드 변경 시 자동 갱신 리스너
    private void OnGoldAmountChanged(long gold)
    {
        UpdateAvailableGoldText(gold);
        RefreshAllSlots();
    }

    // 상단 보유 골드 텍스트 표기
    private void UpdateAvailableGoldText(long gold)
    {
        if (availableGoldText != null)
        {
            availableGoldText.text = $"AVAILABLE GOLD {FormatNumber(gold)}";
        }
    }

    // 보유 골드 상태 및 레벨 변경에 따른 전체 슬롯 피드백(버튼 활성화/색상 변경) 갱신
    public void RefreshAllSlots()
    {
        long currentGold = CurrencyManager.Instance != null ? CurrencyManager.Instance.Gold : 0;
        UpdateAvailableGoldText(currentGold);

        if (upgradeSlots == null || CurrencyUpgradeManager.Instance == null) return;

        // 정의된 업그레이드 항목 이름 정의 (폰트 깨짐 방지를 위해 영문으로 표기)
        string[] statNames = {
            "Gold Bonus",
            "Gold Multiplier",
            "Diamond Bonus",
            "Diamond Multiplier",
            "DP Cost Bonus",
            "Max DP Cost"
        };

        for (int i = 0; i < upgradeSlots.Length; i++)
        {
            UIUpgradeItemSlot slot = upgradeSlots[i];
            if (slot == null) continue;

            int typeIndex = slot.UpgradeTypeIndex;
            int currentLevel = CurrencyUpgradeManager.Instance.GetLevelByType(typeIndex);
            
            // 해당 업그레이드 타입에 맞는 보유 재화 수량 조회 (Gold, Diamond, DpCost)
            long availableCurrency = CurrencyUpgradeManager.Instance.GetAvailableCurrencyForType(typeIndex);

            // 1회, 10회 소모 비용 및 MAX 구매 가능 수량 계산
            double costOne = CurrencyUpgradeManager.Instance.GetUpgradeCost(typeIndex, 1);
            double costTen = CurrencyUpgradeManager.Instance.GetUpgradeCost(typeIndex, 10);
            int maxCount = CurrencyUpgradeManager.Instance.GetMaxPurchasableCount(typeIndex, availableCurrency);

            string name = typeIndex < statNames.Length ? statNames[typeIndex] : $"STAT {typeIndex + 1:D2}";
            string currentVal = $"+{currentLevel * 10}"; // 예시 가공 수치
            string nextVal = $"+{(currentLevel + 1) * 10}";

            // 슬롯 UI 갱신 (해당 타입의 보유 재화에 따라 버튼 활성화/파란색/회색 피드백 자동 처리)
            slot.UpdateSlotUI(
                name,
                currentLevel,
                currentVal,
                nextVal,
                costOne,
                costTen,
                maxCount,
                availableCurrency
            );
        }
    }

    // 패널 닫기
    public void ClosePanel()
    {
        gameObject.SetActive(false);
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
