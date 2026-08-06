using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InGameUI : MonoBehaviour
{
    #region 참조 및 변수
    [SerializeField] private Button[] speedButtons;
    [SerializeField] private TMP_Text[] currencyTexts;

    private static readonly string[] NumFormats = {"","K","M","B","T","Qa","Qi"};
    
    #endregion

    #region 이벤트

    public static event Action<int> OnGameSpeedChange;

    #endregion

    #region 라이프 사이클
    private void Awake()
    {
        for (int i = 0; i < speedButtons.Length ; i++)
        {
            int index = i;
            speedButtons[i].onClick.AddListener((() => OnGameSpeedChange?.Invoke(index)));
        }
    }

    private void OnEnable()
    {
        CurrencyManager.OnGoldChange += UpdateGold;
        CurrencyManager.OnDiamondChange += UpdateDiamond;
        CurrencyManager.OnDpCostChange += UpdateDpCost;
    }

    private void OnDisable()
    {
        CurrencyManager.OnGoldChange -= UpdateGold;
        CurrencyManager.OnDiamondChange -= UpdateDiamond;
        CurrencyManager.OnDpCostChange -= UpdateDpCost;
    }

    #endregion

    #region 재화 텍스트 업데이트

    private void UpdateGold(long gold)
    {
        if (gold < 1000)
        {
            currencyTexts[0].text = gold.ToString("N0");
            return;
        }

        int formatIndex = 0;
        double currencyValue = gold;
        while (currencyValue >=1000 && formatIndex <NumFormats.Length -1)
        {
            currencyValue /= 1000;
            formatIndex++;
        }
        currencyTexts[0].text = currencyValue.ToString("N1") + NumFormats[formatIndex];
    }

    private void UpdateDiamond(int diamond)
    {
        if (diamond < 1000)
        {
            currencyTexts[1].text = diamond.ToString("N0");
            return;
        }

        int formatIndex = 0;
        double currencyValue = diamond;
        while (currencyValue >=1000 && formatIndex <NumFormats.Length -1)
        {
            currencyValue /= 1000;
            formatIndex++;
        }
        currencyTexts[1].text = currencyValue.ToString("N1") + NumFormats[formatIndex];
    }

    private void UpdateDpCost(int dpCost)
    {
        if (dpCost < 1000)
        {
            currencyTexts[2].text = dpCost.ToString("N0");
            return;
        }

        int formatIndex = 0;
        double currencyValue = dpCost;
        while (currencyValue >=1000 && formatIndex <NumFormats.Length -1)
        {
            currencyValue /= 1000;
            formatIndex++;
        }
        currencyTexts[2].text = currencyValue.ToString("N1") + NumFormats[formatIndex];
    }
    

    #endregion
}
