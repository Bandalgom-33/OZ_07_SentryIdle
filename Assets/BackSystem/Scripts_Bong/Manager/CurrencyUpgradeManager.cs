using System;
using UnityEngine;

// 재화 및 스탯 업그레이드 단계, 필요 재화 계산, 차감 트랜잭션을 총괄하는 매니저 클래스
public class CurrencyUpgradeManager : SingletonBase<CurrencyUpgradeManager>
{
    #region 노출 변수

    [Header("골드 업그레이드 설정 값")]
    [SerializeField] private int goldBonusUpgradeCost = 1000;
    [SerializeField] private long goldMagnificationUpgradeCost = 1000;
    [SerializeField] private float goldBonusIncreaseMultiplier = 1.5f;
    [SerializeField] private float goldMagnificationIncreaseMultiplier = 2.5f;

    [Space(5f), Header("다이아 업그레이드 설정 값")]
    [SerializeField] private int diamondBonusUpgradeCost = 10;
    [SerializeField] private float diamondMagnificationUpgradeCost = 10;
    [SerializeField] private float diamondBonusIncreaseMultiplier = 1.5f;
    [SerializeField] private float diamondMagnificationIncreaseMultiplier = 2.5f;

    [Space(5f), Header("소환 코스트 업그레이드 설정 값")]
    [SerializeField] private int dpCostBonusUpgradeCost = 5;
    [SerializeField] private float dpCostMagnificationUpgradeCost = 5;
    [SerializeField] private float dpCostBonusIncreaseMultiplier = 1.5f;
    [SerializeField] private float dpCostMagnificationIncreaseMultiplier = 2.5f;

    #endregion

    #region 프로퍼티 (각 업그레이드 레벨)

    public int GoldBonusLevel { get; private set; }
    public int GoldMagnificationLevel { get; private set; }
    public int DiamondBonusLevel { get; private set; }
    public int DiamondMagnificationLevel { get; private set; }
    public int DpCostBonusLevel { get; private set; }
    public int DpCostMagnificationLevel { get; private set; }

    #endregion

    #region 이벤트 선언

    // 업그레이드가 성공적으로 수행되었을 때 UI 갱신을 트리거하기 위한 C# 이벤트
    public static event Action OnUpgradeCompleted;

    #endregion

    #region 라이프 사이클

    private void OnEnable()
    {
        UpgradeUi.OnCurrencyUpgrade += SelectUpgrade;
        EventBus.Subscribe<DataSaveEvent>(OnSave);
        EventBus.Subscribe<DataLoadEvent>(OnLoad);
        EventBus.Subscribe<DataResetEvent>(OnReset);
    }

    private void OnDisable()
    {
        UpgradeUi.OnCurrencyUpgrade -= SelectUpgrade;
        EventBus.Unsubscribe<DataSaveEvent>(OnSave);
        EventBus.Unsubscribe<DataLoadEvent>(OnLoad);
        EventBus.Unsubscribe<DataResetEvent>(OnReset);
    }

    #endregion

    #region 외부 호출용 계산 메서드 (비용 및 최대 구매 수량)

    // 지정된 업그레이드 타입 및 횟수(count)에 필요한 총 비용 계산
    // 이유: UI에서 +1, +10 버튼 활성화 여부를 판별하기 위한 비용 미리보기 제공
    public double GetUpgradeCost(int type, int count)
    {
        if (count <= 0) return 0;

        double totalCost = 0;
        int currentLevel = GetLevelByType(type);

        for (int i = 0; i < count; i++)
        {
            totalCost += CalculateSingleStepCost(type, currentLevel + i);
        }

        return totalCost;
    }

    // 현재 보유 재화 기준으로 구매 가능한 최대 강화 횟수 계산
    // 이유: MAX 버튼 누를 시 실행할 수 있는 최대 횟수 계산 및 MAX 버튼 활성화 상태 판별
    public int GetMaxPurchasableCount(int type, long availableCurrency)
    {
        int count = 0;
        double accumulatedCost = 0;
        int currentLevel = GetLevelByType(type);

        while (true)
        {
            double stepCost = CalculateSingleStepCost(type, currentLevel + count);
            if (accumulatedCost + stepCost > availableCurrency)
            {
                break;
            }
            accumulatedCost += stepCost;
            count++;

            // 무한 루프 방지 상한값
            if (count >= 1000) break;
        }

        return count;
    }

    // 업그레이드 타입별 보유 재화 수량 반환 (0,1: Gold / 2,3: Diamond / 4,5: DpCost)
    // 이유: 슬롯마다 소모하는 재화 종류가 다르므로 적절한 잔액을 가져와 구매 가능 여부를 판단함
    public long GetAvailableCurrencyForType(int type)
    {
        if (CurrencyManager.Instance == null) return 0;

        return type switch
        {
            0 or 1 => CurrencyManager.Instance.Gold,
            2 or 3 => CurrencyManager.Instance.Diamond,
            4 or 5 => CurrencyManager.Instance.DpCost,
            _ => 0
        };
    }

    // 업그레이드 타입별 현재 레벨 반환 헬퍼 함수
    public int GetLevelByType(int type)
    {
        return type switch
        {
            0 => GoldBonusLevel,
            1 => GoldMagnificationLevel,
            2 => DiamondBonusLevel,
            3 => DiamondMagnificationLevel,
            4 => DpCostBonusLevel,
            5 => DpCostMagnificationLevel,
            _ => 0
        };
    }

    // 특정 단계 레벨에서의 단일 소모 비용 계산
    private double CalculateSingleStepCost(int type, int targetLevel)
    {
        return type switch
        {
            0 => goldBonusUpgradeCost * Mathf.Pow(goldBonusIncreaseMultiplier, targetLevel),
            1 => goldMagnificationUpgradeCost * Mathf.Pow(goldMagnificationIncreaseMultiplier, targetLevel),
            2 => diamondBonusUpgradeCost * Mathf.Pow(diamondBonusIncreaseMultiplier, targetLevel),
            3 => diamondMagnificationUpgradeCost * Mathf.Pow(diamondMagnificationIncreaseMultiplier, targetLevel),
            4 => dpCostBonusUpgradeCost * Mathf.Pow(dpCostBonusIncreaseMultiplier, targetLevel),
            5 => dpCostMagnificationUpgradeCost * Mathf.Pow(dpCostMagnificationIncreaseMultiplier, targetLevel),
            _ => 0
        };
    }

    #endregion

    #region 재화 업그레이드 실행 처리

    // UI에서 요청한 업그레이드 타입과 횟수(1, 10, -1: MAX) 분기 처리
    private void SelectUpgrade(int type, int count)
    {
        int actualCount = count;

        // count가 -1이면 MAX 강화를 의미함
        if (count == -1)
        {
            long currentCurrency = GetAvailableCurrencyForType(type);
            actualCount = GetMaxPurchasableCount(type, currentCurrency);
            if (actualCount <= 0) return;
        }

        switch (type)
        {
            case 0:
                GoldBonusUpgrade(actualCount);
                break;
            case 1:
                GoldMagnificationUpgrade(actualCount);
                break;
            case 2:
                DiamondBonusUpgrade(actualCount);
                break;
            case 3:
                DiamondMagnificationUpgrade(actualCount);
                break;
            case 4:
                DpCostBonusUpgrade(actualCount);
                break;
            case 5:
                DpCostMagnificationUpgrade(actualCount);
                break;
        }
    }

    // 1. 골드 보너스 업그레이드 실행
    private void GoldBonusUpgrade(int count)
    {
        if (ExecuteGoldUpgradeTransaction(0, count))
        {
            CurrencyManager.Instance.GoldBonusUpgrade(count);
            GoldBonusLevel += count;
            OnUpgradeCompleted?.Invoke();
        }
    }

    // 2. 골드 획득 배율 업그레이드 실행
    private void GoldMagnificationUpgrade(int count)
    {
        if (ExecuteGoldUpgradeTransaction(1, count))
        {
            CurrencyManager.Instance.GoldMagnificationUpgrade(count);
            GoldMagnificationLevel += count;
            OnUpgradeCompleted?.Invoke();
        }
    }

    // 3. 다이아 보너스 업그레이드 실행
    private void DiamondBonusUpgrade(int count)
    {
        if (ExecuteDiamondUpgradeTransaction(2, count))
        {
            CurrencyManager.Instance.DiamondBonusUpgrade(count);
            DiamondBonusLevel += count;
            OnUpgradeCompleted?.Invoke();
        }
    }

    // 4. 다이아 배율 업그레이드 실행
    private void DiamondMagnificationUpgrade(int count)
    {
        if (ExecuteDiamondUpgradeTransaction(3, count))
        {
            CurrencyManager.Instance.DiamondMagnificationUpgrade(count);
            DiamondMagnificationLevel += count;
            OnUpgradeCompleted?.Invoke();
        }
    }

    // 5. 소환 코스트(DP) 보너스 업그레이드 실행
    private void DpCostBonusUpgrade(int count)
    {
        if (ExecuteDpCostUpgradeTransaction(4, count))
        {
            CurrencyManager.Instance.DpCostBonusUpgrade(count);
            DpCostBonusLevel += count;
            OnUpgradeCompleted?.Invoke();
        }
    }

    // 6. 소환 코스트(DP) 배율 업그레이드 실행
    private void DpCostMagnificationUpgrade(int count)
    {
        if (ExecuteDpCostUpgradeTransaction(5, count))
        {
            CurrencyManager.Instance.MaxDpCostUpgrade(count);
            DpCostMagnificationLevel += count;
            OnUpgradeCompleted?.Invoke();
        }
    }

    // 골드 소모 검증 및 시도
    private bool ExecuteGoldUpgradeTransaction(int type, int count)
    {
        double totalCost = GetUpgradeCost(type, count);
        return CurrencyManager.Instance.TrySpendGold((long)totalCost);
    }

    // 다이아 소모 검증 및 시도
    private bool ExecuteDiamondUpgradeTransaction(int type, int count)
    {
        double totalCost = GetUpgradeCost(type, count);
        return CurrencyManager.Instance.TrySpendDiamond((int)totalCost);
    }

    // DP 소모 검증 및 시도
    private bool ExecuteDpCostUpgradeTransaction(int type, int count)
    {
        double totalCost = GetUpgradeCost(type, count);
        return CurrencyManager.Instance.TrySpendDpCost((int)totalCost);
    }

    #endregion

    #region 저장 관리

    private void OnSave(DataSaveEvent evt)
    {
        evt.saveData.statUpgrade.goldBonusLevel = GoldBonusLevel;
        evt.saveData.statUpgrade.goldMagnificationLevel = GoldMagnificationLevel;
        evt.saveData.statUpgrade.diamondBonusLevel = DiamondBonusLevel;
        evt.saveData.statUpgrade.diamondMagnificationLevel = DiamondMagnificationLevel;
        evt.saveData.statUpgrade.dpCostBonusLevel = DpCostBonusLevel;
        evt.saveData.statUpgrade.dpCostMagnificationLevel = DpCostMagnificationLevel;
    }

    private void OnLoad(DataLoadEvent evt)
    {
        GoldBonusLevel = evt.saveData.statUpgrade.goldBonusLevel;
        GoldMagnificationLevel = evt.saveData.statUpgrade.goldMagnificationLevel;
        DiamondBonusLevel = evt.saveData.statUpgrade.diamondBonusLevel;
        DiamondMagnificationLevel = evt.saveData.statUpgrade.diamondMagnificationLevel;
        DpCostBonusLevel = evt.saveData.statUpgrade.dpCostBonusLevel;
        DpCostMagnificationLevel = evt.saveData.statUpgrade.dpCostMagnificationLevel;

        CurrencyManager.Instance.GoldBonusUpgrade(GoldBonusLevel);
        CurrencyManager.Instance.GoldMagnificationUpgrade(GoldMagnificationLevel);
        CurrencyManager.Instance.DiamondBonusUpgrade(DiamondBonusLevel);
        CurrencyManager.Instance.DiamondMagnificationUpgrade(DiamondMagnificationLevel);
        CurrencyManager.Instance.DpCostBonusUpgrade(DpCostBonusLevel);
        CurrencyManager.Instance.MaxDpCostUpgrade(DpCostMagnificationLevel);
    }

    private void OnReset(DataResetEvent evt)
    {
        GoldBonusLevel = 0;
        GoldMagnificationLevel = 0;
        DiamondBonusLevel = 0;
        DiamondMagnificationLevel = 0;
        DpCostBonusLevel = 0;
        DpCostMagnificationLevel = 0;
    }

    #endregion
}
