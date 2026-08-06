using System;
using UnityEngine;

public class CurrencyUpgradeManager : MonoBehaviour
{
    #region 노출 변수
    
    [Header("골드 업그레이드 설정 값")]
    [SerializeField] private int goldBonusUpgradeCost = 1000;
    [SerializeField] private long goldMagnificationUpgradeCost = 1000;
    [SerializeField] private float goldBonusIncreaseMultiplier = 1.5f;
    [SerializeField] private float goldMagnificationIncreaseMultiplier = 2.5f;
    [Space(5f),Header("다이아 업그레이드 설정 값")]
    [SerializeField] private int diamondBonusUpgradeCost = 1000;
    [SerializeField] private float diamondMagnificationUpgradeCost = 1000;
    [SerializeField]  private float diamondBonusIncreaseMultiplier = 1.5f;
    [SerializeField] private float diamondMagnificationIncreaseMultiplier = 2.5f;
    [Space(5f),Header("소환 코스트 업그레이드 설정 값")]
    [SerializeField] private int dpCostBonusUpgradeCost = 1000;
    [SerializeField] private float dpCostMagnificationUpgradeCost = 1000;
    [SerializeField] private float dpCostBonusIncreaseMultiplier = 1.5f;
    [SerializeField] private float dpCostMagnificationIncreaseMultiplier = 2.5f;
    
    #endregion

    #region 프로퍼티

    public int GoldBonusLevel { get; private set; }
    public int GoldMagnificationLevel { get; private set; }
    public int DiamondBonusLevel { get; private set; }
    public int DiamondMagnificationLevel { get; private set; }
    public int DpCostBonusLevel { get; private set; }
    public int DpCostMagnificationLevel { get; private set; }

    #endregion

    #region 라이프 사이클

    private void OnEnable()
    {
        UpgradeUi.OnCurrencyUpgrade += SelectUpgrade;
    }

    private void OnDisable()
    {
        UpgradeUi.OnCurrencyUpgrade -= SelectUpgrade;
    }

    #endregion

    #region 재화 업그레이드 매서드

    private void SelectUpgrade(int type, int upgradeLevel)
    {
        switch (type)
        {
            case 0:
                GoldBonusUpgrade(upgradeLevel);
                break;
            case 1:
                GoldMagnificationUpgrade(upgradeLevel);
                break;
            case 2:
                DiamondBonusUpgrade(upgradeLevel);
                break;
            case 3:
                DiamondMagnificationUpgrade(upgradeLevel);
                break;
            case 4:
                DpCostBonusUpgrade(upgradeLevel);
                break;
            case 5:
                DpCostMagnificationUpgrade(upgradeLevel);
                break;
        }
    }
    
    //골드 보너스 업그레이드
    private void GoldBonusUpgrade(int upgradeLevel)
    {
        if (GoldUpgradeCostCalculation(true,upgradeLevel))
        {
            CurrencyManager.Instance.GoldBonusUpgrade(upgradeLevel);
            GoldBonusLevel++;
        }
        else
        {
            //실패
            Debug.Log("골드 보너스 업그레이드 실패");
        }
        
    }
    // 골드 획득배율 업그레이드
    private void GoldMagnificationUpgrade(int upgradeLevel)
    {
        if (GoldUpgradeCostCalculation(false,upgradeLevel))
        {
            CurrencyManager.Instance.GoldMagnificationUpgrade(upgradeLevel);
            GoldMagnificationLevel++;
        }
        else
        {
            //실패
            Debug.Log("골드 배율 업그레이드 실패");
        }
    }
    //다이아 보너스 업그레이드
    private void DiamondBonusUpgrade(int upgradeLevel)
    {
        if (DiamondUpgradeCostCalculation(true,upgradeLevel))
        {
            CurrencyManager.Instance.DiamondBonusUpgrade(upgradeLevel);
            DiamondBonusLevel++;
        }
        else
        {
            //실패
        }
    }
    // 다이아 획득배율 업그레이드
    private void DiamondMagnificationUpgrade(int upgradeLevel)
    {
        if (DiamondUpgradeCostCalculation(false, upgradeLevel))
        {
            CurrencyManager.Instance.DiamondMagnificationUpgrade(upgradeLevel);
            DiamondMagnificationLevel++;
        }
        else
        {
            //실패
        }
    }
    //소환 코스트 보너스 업그레이드
    private void DpCostBonusUpgrade(int upgradeLevel)
    {
        if (DpCostUpgradeCostCalculation(true, upgradeLevel))
        {
            CurrencyManager.Instance.DpCostBonusUpgrade(upgradeLevel);
            DpCostBonusLevel++;
        }
        else
        {
            
        }
    }
    // 소환 코스트 획득배율 업그레이드
    private void DpCostMagnificationUpgrade(int upgradeLevel)
    {
        if (DpCostUpgradeCostCalculation(false, upgradeLevel))
        {
            CurrencyManager.Instance.DpCostMagnificationUpgrade(upgradeLevel);
            DpCostMagnificationLevel++;
        }
        else
        {
            
        }
    }
    

    #endregion

    #region 내부 계산 매서드
    // 골드 업그레이드 소모 재화 계산
    private bool GoldUpgradeCostCalculation(bool isBonus,int upgradeLevel)
    {
        double cost = 0;
        for (int i = 0; i < upgradeLevel; i++)
        {
            if (isBonus)
            {
                double value = goldBonusUpgradeCost * Mathf.Pow(goldBonusIncreaseMultiplier, i+GoldBonusLevel);
                cost += value;
            }
            else
            {
                double value = goldMagnificationUpgradeCost * Mathf.Pow(goldMagnificationIncreaseMultiplier, i+GoldMagnificationLevel);
                cost += value;
            }
        }

        if (CurrencyManager.Instance.TrySpendGold((long)cost))
        {
            if (isBonus)
            {
                GoldBonusLevel += upgradeLevel;
            }
            else
            {
                GoldMagnificationLevel += upgradeLevel;
            }
            return true;
        }
        else
        {
            return false;
        }
    }
    // 다이어 업그레이드 소모 재화 계산
    private bool DiamondUpgradeCostCalculation(bool isBonus, int upgradeLevel)
    {
        double cost = 0;
        for (int i = 0; i < upgradeLevel; i++)
        {
            if (isBonus)
            {
                double value = diamondBonusUpgradeCost * Mathf.Pow(diamondBonusIncreaseMultiplier, i);
                cost += value;
            }
            else
            {
                double value = diamondMagnificationUpgradeCost * Mathf.Pow(diamondMagnificationIncreaseMultiplier, i);
                cost += value;
            }
        }

        if (CurrencyManager.Instance.TrySpendDiamond((int)cost))
        {
            if (isBonus)
            {
                DiamondBonusLevel += upgradeLevel;
            }
            else
            {
                DiamondMagnificationLevel += upgradeLevel;
            }
            return true;
        }
        else
        {
            return false;
        }
    }
    // 소환 코스트 업그레이드 재화 계산
    private bool DpCostUpgradeCostCalculation(bool isBonus, int upgradeLevel)
    {
        double cost = 0;
        for (int i = 0; i < upgradeLevel; i++)
        {
            if (isBonus)
            {
                double value = dpCostBonusUpgradeCost * Mathf.Pow(dpCostBonusIncreaseMultiplier, i);
                cost += value;
            }
            else
            {
                double value = dpCostMagnificationUpgradeCost * Mathf.Pow(dpCostMagnificationIncreaseMultiplier, i);
                cost += value;
            }
        }

        if (CurrencyManager.Instance.TrySpendDpCost((int)cost))
        {
            if (isBonus)
            {
                DpCostBonusLevel += upgradeLevel;
            }
            else
            {
                DpCostMagnificationLevel += upgradeLevel;
            }
            return true;
        }
        else
        {
            return false;
        }
    }

    #endregion
}
