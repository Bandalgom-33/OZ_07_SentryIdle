using System;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeUi : MonoBehaviour
{
    #region 직렬화 변수 (인스펙터 바인딩)

    [Header("--- 상단 UI 요소 ---")]
    [Tooltip("상단 보유 골드 표시 텍스트")]
    [SerializeField] private Text availableGoldText;

    [Tooltip("업그레이드 패널 닫기 버튼")]
    [SerializeField] private Button exitButton;

    [Header("--- 업그레이드 슬롯 리스트 ---")]
    [Tooltip("화면에 노출할 업그레이드 슬롯 컴포넌트 배열")]
    [SerializeField] private UIUpgradeItemSlot[] upgradeSlots;

    #endregion

    #region 이벤트 선언

    public static event Action<int, int> OnCurrencyUpgrade;

    #endregion

    #region 라이프 사이클

    // 버튼 및 슬롯 초기화 연산
    private void Awake()
    {
        if (exitButton != null)
        {
            exitButton.onClick.AddListener(ClosePanel);
        }

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

    // 이벤트 액션 구독 및 슬롯 갱신
    private void OnEnable()
    {
        CurrencyManager.OnGoldChange += OnGoldAmountChanged;
        CurrencyManager.OnDiamondChange += OnOtherCurrencyChanged;
        CurrencyManager.OnDpCostChange += OnDpCostAmountChanged;
        UpgradeManager.OnUpgradeCompleted += RefreshAllSlots;

        RefreshAllSlots();
    }

    // 이벤트 구독 해제 연산
    private void OnDisable()
    {
        CurrencyManager.OnGoldChange -= OnGoldAmountChanged;
        CurrencyManager.OnDiamondChange -= OnOtherCurrencyChanged;
        CurrencyManager.OnDpCostChange -= OnDpCostAmountChanged;
        UpgradeManager.OnUpgradeCompleted -= RefreshAllSlots;
    }

    // 다이아 변경 시 슬롯 UI 갱신
    private void OnOtherCurrencyChanged(long amount)
    {
        RefreshAllSlots();
    }

    // DP 코스트 변경 시 슬롯 UI 갱신
    private void OnDpCostAmountChanged(int amount)
    {
        RefreshAllSlots();
    }

    #endregion

    #region 슬롯 강화 요청 중계

    // 슬롯 강화 요청 중계
    public static void TriggerUpgradeRequest(int typeIndex, int count)
    {
        OnCurrencyUpgrade?.Invoke(typeIndex, count);
    }

    #endregion

    #region UI 갱신 로직

    // 골드 잔액 변경 시 UI 갱신
    private void OnGoldAmountChanged(long gold)
    {
        UpdateAvailableGoldText(gold);
        RefreshAllSlots();
    }

    // 보유 골드 텍스트 갱신
    private void UpdateAvailableGoldText(long gold)
    {
        if (availableGoldText != null)
        {
            availableGoldText.text = $"AVAILABLE GOLD {FormatNumber(gold)}";
        }
    }

    // 전체 슬롯 UI 갱신 연산
    public void RefreshAllSlots()
    {
        long currentGold = CurrencyManager.Instance != null ? CurrencyManager.Instance.Gold : 0;
        UpdateAvailableGoldText(currentGold);

        if (upgradeSlots == null || UpgradeManager.Instance == null) return;

        string[] statNames = {
            "Gold Bonus",
            "Gold Multiplier",
            "Diamond Bonus",
            "Diamond Multiplier",
            "DP Cost Bonus",
            "Max DP Limit",
            "Physical Attack",
            "Magical Attack",
            "Max HP",
            "HP Regen / sec",
            "Physical Defense",
            "Magical Defense",
            "Attack Speed",
            "Accuracy",
            "Evasion",
            "Critical Chance",
            "Critical Damage"
        };

        for (int i = 0; i < upgradeSlots.Length; i++)
        {
            UIUpgradeItemSlot slot = upgradeSlots[i];
            if (slot == null) continue;

            int typeIndex = slot.UpgradeTypeIndex;
            int currentLevel = UpgradeManager.Instance.GetLevelByType(typeIndex);
            
            long availableCurrency = UpgradeManager.Instance.GetAvailableCurrencyForType(typeIndex);

            double costOne = UpgradeManager.Instance.GetUpgradeCost(typeIndex, 1);
            double costTen = UpgradeManager.Instance.GetUpgradeCost(typeIndex, 10);
            int maxCount = UpgradeManager.Instance.GetMaxPurchasableCount(typeIndex, availableCurrency);

            string name = typeIndex < statNames.Length ? statNames[typeIndex] : $"STAT {typeIndex + 1:D2}";
            string currentVal = UpgradeManager.Instance.GetStatValue(typeIndex, currentLevel);
            string nextVal = UpgradeManager.Instance.GetStatValue(typeIndex, currentLevel + 1);

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

    // 패널 닫기 연산
    public void ClosePanel()
    {
        gameObject.SetActive(false);
    }

    // 숫자 축약 포맷팅 처리
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
