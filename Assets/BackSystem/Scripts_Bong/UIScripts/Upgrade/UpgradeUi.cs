using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 업그레이드 화면 표시, 슬롯별 레벨/비용 갱신 및 강화 요청을 중계하는 UI 컨트롤러
public class UpgradeUi : MonoBehaviour
{
    #region 직렬화 변수

    [Header("--- 상단 UI 요소 ---")]
    [Tooltip("상단 보유 골드 표시 텍스트")]
    [SerializeField] private TMP_Text availableGoldText;

    [Tooltip("상단 보유 다이아 표시 텍스트")]
    [SerializeField] private TMP_Text availableDiamondText;

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

    // 전역 이벤트 구독 및 슬롯 UI 전체 갱신
    private void OnEnable()
    {
        CurrencyManager.OnGoldChange += OnGoldAmountChanged;
        CurrencyManager.OnDiamondChange += OnDiamondAmountChanged;
        CurrencyManager.OnDpCostChange += OnDpCostAmountChanged;
        UpgradeManager.OnUpgradeCompleted += RefreshAllSlots;

        RefreshAllSlots();
    }

    // 전역 이벤트 구독 해제
    private void OnDisable()
    {
        CurrencyManager.OnGoldChange -= OnGoldAmountChanged;
        CurrencyManager.OnDiamondChange -= OnDiamondAmountChanged;
        CurrencyManager.OnDpCostChange -= OnDpCostAmountChanged;
        UpgradeManager.OnUpgradeCompleted -= RefreshAllSlots;
    }

    // 다이아 수량 변경 이벤트 콜백
    private void OnDiamondAmountChanged(long diamond)
    {
        UpdateAvailableDiamondText(diamond);
        RefreshAllSlots();
    }

    // DP 코스트 변경 이벤트 콜백
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

    // 골드 수량 변경 이벤트 콜백
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
            availableGoldText.text = FormatNumber(gold);
        }
    }

    // 보유 다이아 텍스트 갱신
    private void UpdateAvailableDiamondText(long diamond)
    {
        if (availableDiamondText != null)
        {
            availableDiamondText.text = FormatNumber(diamond);
        }
    }

    // 전체 슬롯 UI 및 상단 재화 일괄 동기화
    public void RefreshAllSlots()
    {
        long currentGold = CurrencyManager.Instance != null ? CurrencyManager.Instance.Gold : 0;
        long currentDiamond = CurrencyManager.Instance != null ? CurrencyManager.Instance.Diamond : 0;

        UpdateAvailableGoldText(currentGold);
        UpdateAvailableDiamondText(currentDiamond);

        if (upgradeSlots == null || UpgradeManager.Instance == null) return;

        string[] statNames = {
            "골드 보너스",
            "골드 배율",
            "다이아 보너스",
            "다이아 배율",
            "DP 코스트 보너스",
            "최대 DP 제한",
            "물리 공격력",
            "마법 공격력",
            "최대 체력",
            "초당 HP 재생",
            "물리 방어력",
            "마법 방어력",
            "공격 속도",
            "명중률",
            "회피율",
            "치명타 확률",
            "치명타 피해"
        };

        for (int i = 0; i < upgradeSlots.Length; i++)
        {
            UIUpgradeItemSlot slot = upgradeSlots[i];
            if (slot == null) continue;

            int typeIndex = slot.UpgradeTypeIndex;
            int currentLevel = UpgradeManager.Instance.GetLevelByType(typeIndex);
            int maxLevel = UpgradeManager.Instance.GetMaxLevelByType(typeIndex);
            bool isMaxLevel = maxLevel > 0 && currentLevel >= maxLevel;

            long availableCurrency = UpgradeManager.Instance.GetAvailableCurrencyForType(typeIndex);

            double costOne = UpgradeManager.Instance.GetUpgradeCost(typeIndex, 1);
            double costTen = UpgradeManager.Instance.GetUpgradeCost(typeIndex, 10);
            int maxCount = UpgradeManager.Instance.GetMaxPurchasableCount(typeIndex, availableCurrency);
            double costMax = maxCount > 0 ? UpgradeManager.Instance.GetUpgradeCost(typeIndex, maxCount) : 0;

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
                costMax,
                maxCount,
                availableCurrency,
                isMaxLevel
            );
        }
    }

    // 패널 닫기 처리
    public void ClosePanel()
    {
        gameObject.SetActive(false);
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
