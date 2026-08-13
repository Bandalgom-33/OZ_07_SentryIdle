using System;
using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Runtime;
using UnityEngine;

// 스탯 및 재화 업그레이드 전담 싱글톤 매니저
public class UpgradeManager : SingletonBase<UpgradeManager>
{
    #region 노출 변수

    [Header("구매 상한 설정")]
    [SerializeField] private int maxPurchaseLimit = 9999;

    [Space(5f), Header("골드 업그레이드 설정 값")]
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
    [SerializeField] private float maxDpCostUpgradeCost = 5;
    [SerializeField] private float dpCostBonusIncreaseMultiplier = 1.5f;
    [SerializeField] private float maxDpCostIncreaseMultiplier = 2.5f;

    [Space(5f), Header("아군 공통 스탯 기본 비용")]
    [SerializeField] private long baseStatUpgradeCost = 500;
    [SerializeField] private float statCostMultiplier = 1.3f;

    [Space(5f), Header("공통 스탯 1레벨당 수치 증가 세팅")]
    [SerializeField] private float physicalAttackIncrease = 10f;
    [SerializeField] private float magicalAttackIncrease = 10f;
    [SerializeField] private float maxHpIncrease = 100f;
    [SerializeField] private float hpRegenIncrease = 1.0f;
    [SerializeField] private float physicalDefenseIncrease = 5f;
    [SerializeField] private float magicalDefenseIncrease = 5f;
    [SerializeField] private float attackSpeedIncrease = 0.05f;
    [SerializeField] private float accuracyIncrease = 5f;
    [SerializeField] private float evasionIncrease = 5f;
    [SerializeField] private float criticalChanceIncrease = 0.5f; // +0.5%
    [SerializeField] private float criticalDamageIncrease = 1.0f; // +1.0%

    [Space(5f), Header("공통 스탯 최대 레벨 상한 설정 (0은 무제한)")]
    [SerializeField] private int attackSpeedMaxLevel = 100;
    [SerializeField] private int criticalChanceMaxLevel = 100; // 최대 100레벨 (+50.0% MAX)
    [SerializeField] private int criticalDamageMaxLevel = 200;

    #endregion

    #region 프로퍼티

    public int GoldBonusLevel { get; private set; }
    public int GoldMagnificationLevel { get; private set; }
    public int DiamondBonusLevel { get; private set; }
    public int DiamondMagnificationLevel { get; private set; }
    public int DpCostBonusLevel { get; private set; }
    public int MaxDpCostLevel { get; private set; }

    public int PhysicalAttackLevel { get; private set; }
    public int MagicalAttackLevel { get; private set; }
    public int MaxHpLevel { get; private set; }
    public int HpRegenLevel { get; private set; }
    public int PhysicalDefenseLevel { get; private set; }
    public int MagicalDefenseLevel { get; private set; }
    public int AttackSpeedLevel { get; private set; }
    public int AccuracyLevel { get; private set; }
    public int EvasionLevel { get; private set; }
    public int CriticalChanceLevel { get; private set; }
    public int CriticalDamageLevel { get; private set; }

    public int DpCostMagnificationLevel => MaxDpCostLevel;

    #endregion

    #region 이벤트 선언

    public static event Action OnUpgradeCompleted;

    #endregion

    #region 라이프 사이클

    // 이벤트 구독 등록
    private void OnEnable()
    {
        UpgradeUi.OnCurrencyUpgrade += SelectUpgrade;
        EventBus.Subscribe<DataSaveEvent>(OnSave);
        EventBus.Subscribe<DataLoadEvent>(OnLoad);
        EventBus.Subscribe<DataResetEvent>(OnReset);
    }

    // 이벤트 구독 해제
    private void OnDisable()
    {
        UpgradeUi.OnCurrencyUpgrade -= SelectUpgrade;
        EventBus.Unsubscribe<DataSaveEvent>(OnSave);
        EventBus.Unsubscribe<DataLoadEvent>(OnLoad);
        EventBus.Unsubscribe<DataResetEvent>(OnReset);
    }

    #endregion

    #region 외부 호출용 계산 메서드

    // 업그레이드 타입별 최대 레벨 상한 반환
    public int GetMaxLevelByType(int type)
    {
        return type switch
        {
            12 => attackSpeedMaxLevel,
            15 => criticalChanceMaxLevel,
            16 => criticalDamageMaxLevel,
            _ => 0 // 0이면 무제한
        };
    }

    // 업그레이드 타입 및 레벨별 동적 수치 표현 문자열 반환
    public string GetStatValue(int type, int level)
    {
        int maxLvl = GetMaxLevelByType(type);
        if (maxLvl > 0 && level >= maxLvl)
        {
            return type switch
            {
                12 => $"+{level * attackSpeedIncrease:F2} (MAX)",
                15 => $"+{level * criticalChanceIncrease:F1}% (MAX)",
                16 => $"+{level * criticalDamageIncrease:F1}% (MAX)",
                _ => $"MAX"
            };
        }

        return type switch
        {
            0 => $"+{level * 10}",                                              // 골드 보너스 (+10)
            1 => $"x{1.0f + (level * 0.1f):F1} (+{level * 10}%)",               // 골드 배율
            2 => $"+{level * 3}",                                               // 다이아 보너스 (+3)
            3 => $"x{1.0f + (level * 0.1f):F1} (+{level * 10}%)",               // 다이아 배율
            4 => $"+{level * 1} DP",                                            // DP 보너스
            5 => $"Max DP {100 + (level * 10)}",                                 // 최대 DP 상한
            6 => $"+{level * physicalAttackIncrease:N0}",                        // 물리 공격력
            7 => $"+{level * magicalAttackIncrease:N0}",                         // 마법 공격력
            8 => $"+{level * maxHpIncrease:N0}",                                 // 최대 체력
            9 => $"+{level * hpRegenIncrease:F1}/sec",                           // 초당 HP 재생
            10 => $"+{level * physicalDefenseIncrease:N0}",                      // 물리 방어력
            11 => $"+{level * magicalDefenseIncrease:N0}",                       // 마법 방어력
            12 => $"+{level * attackSpeedIncrease:F2}",                          // 공격 속도
            13 => $"+{level * accuracyIncrease:N0}",                             // 명중력
            14 => $"+{level * evasionIncrease:N0}",                              // 회피력
            15 => $"+{level * criticalChanceIncrease:F1}%",                      // 치명타 확률 (+0.5%)
            16 => $"+{level * criticalDamageIncrease:F1}%",                      // 치명타 피해량 (+1.0%)
            _ => $"+{level}"
        };
    }

    // 업그레이드 총 비용 계산
    public double GetUpgradeCost(int type, int count)
    {
        if (count <= 0) return 0;

        int currentLevel = GetLevelByType(type);
        int maxLvl = GetMaxLevelByType(type);

        if (maxLvl > 0 && currentLevel >= maxLvl)
        {
            return double.MaxValue; // 만렙 시 구매 불가 처리
        }

        double totalCost = 0;
        int targetCount = count;
        if (maxLvl > 0 && currentLevel + count > maxLvl)
        {
            targetCount = maxLvl - currentLevel;
        }

        for (int i = 0; i < targetCount; i++)
        {
            totalCost += CalculateSingleStepCost(type, currentLevel + i);
        }

        return totalCost;
    }

    // 보유 재화 기준 최대 구매 가능 횟수 계산
    public int GetMaxPurchasableCount(int type, long availableCurrency)
    {
        int currentLevel = GetLevelByType(type);
        int maxLvl = GetMaxLevelByType(type);

        if (maxLvl > 0 && currentLevel >= maxLvl)
        {
            return 0; // 이미 만렙이면 0회 리턴
        }

        int count = 0;
        double accumulatedCost = 0;

        while (true)
        {
            if (maxLvl > 0 && currentLevel + count >= maxLvl) break;

            double stepCost = CalculateSingleStepCost(type, currentLevel + count);
            if (accumulatedCost + stepCost > availableCurrency)
            {
                break;
            }
            accumulatedCost += stepCost;
            count++;

            if (count >= maxPurchaseLimit) break;
        }

        return count;
    }

    // 업그레이드 타입별 보유 재화 잔액 반환
    public long GetAvailableCurrencyForType(int type)
    {
        if (CurrencyManager.Instance == null) return 0;

        return type switch
        {
            0 or 1 => CurrencyManager.Instance.Gold,
            2 or 3 => CurrencyManager.Instance.Diamond,
            4 or 5 => CurrencyManager.Instance.DpCost,
            _ => CurrencyManager.Instance.Gold // 6~16번 공통 스탯은 모두 골드로 처리
        };
    }

    // 업그레이드 타입별 현재 레벨 반환
    public int GetLevelByType(int type)
    {
        return type switch
        {
            0 => GoldBonusLevel,
            1 => GoldMagnificationLevel,
            2 => DiamondBonusLevel,
            3 => DiamondMagnificationLevel,
            4 => DpCostBonusLevel,
            5 => MaxDpCostLevel,
            6 => PhysicalAttackLevel,
            7 => MagicalAttackLevel,
            8 => MaxHpLevel,
            9 => HpRegenLevel,
            10 => PhysicalDefenseLevel,
            11 => MagicalDefenseLevel,
            12 => AttackSpeedLevel,
            13 => AccuracyLevel,
            14 => EvasionLevel,
            15 => CriticalChanceLevel,
            16 => CriticalDamageLevel,
            _ => 0
        };
    }

    // 단일 단계 소모 비용 연산
    private double CalculateSingleStepCost(int type, int targetLevel)
    {
        return type switch
        {
            0 => goldBonusUpgradeCost * Mathf.Pow(goldBonusIncreaseMultiplier, targetLevel),
            1 => goldMagnificationUpgradeCost * Mathf.Pow(goldMagnificationIncreaseMultiplier, targetLevel),
            2 => diamondBonusUpgradeCost * Mathf.Pow(diamondBonusIncreaseMultiplier, targetLevel),
            3 => diamondMagnificationUpgradeCost * Mathf.Pow(diamondMagnificationIncreaseMultiplier, targetLevel),
            4 => dpCostBonusUpgradeCost * Mathf.Pow(dpCostBonusIncreaseMultiplier, targetLevel),
            5 => maxDpCostUpgradeCost * Mathf.Pow(maxDpCostIncreaseMultiplier, targetLevel),
            _ => baseStatUpgradeCost * Mathf.Pow(statCostMultiplier, targetLevel) // 6~16번 공통 스탯 비용
        };
    }

    #endregion

    #region 업그레이드 실행 및 CommonGrowthService 연동

    // 업그레이드 요청 분기 처리
    private void SelectUpgrade(int type, int count)
    {
        int actualCount = count;

        if (count == -1)
        {
            long currentCurrency = GetAvailableCurrencyForType(type);
            actualCount = GetMaxPurchasableCount(type, currentCurrency);
            if (actualCount <= 0) return;
        }

        int maxLvl = GetMaxLevelByType(type);
        int currentLvl = GetLevelByType(type);
        if (maxLvl > 0 && currentLvl + actualCount > maxLvl)
        {
            actualCount = maxLvl - currentLvl;
            if (actualCount <= 0) return;
        }

        if (type <= 5)
        {
            // 0~5번 재화 업그레이드 실행
            switch (type)
            {
                case 0: GoldBonusUpgrade(actualCount); break;
                case 1: GoldMagnificationUpgrade(actualCount); break;
                case 2: DiamondBonusUpgrade(actualCount); break;
                case 3: DiamondMagnificationUpgrade(actualCount); break;
                case 4: DpCostBonusUpgrade(actualCount); break;
                case 5: MaxDpCostUpgrade(actualCount); break;
            }
        }
        else
        {
            // 6~16번 공통 스탯 업그레이드 실행 (골드 소비)
            ExecuteStatUpgrade(type, actualCount);
        }
    }

    // 골드 보너스 업그레이드 실행
    private void GoldBonusUpgrade(int count)
    {
        if (ExecuteGoldUpgradeTransaction(0, count))
        {
            GoldBonusLevel += count;
            CurrencyManager.Instance.GoldBonusUpgrade(GoldBonusLevel);
            OnUpgradeCompleted?.Invoke();
        }
    }

    // 골드 배율 업그레이드 실행
    private void GoldMagnificationUpgrade(int count)
    {
        if (ExecuteGoldUpgradeTransaction(1, count))
        {
            GoldMagnificationLevel += count;
            CurrencyManager.Instance.GoldMagnificationUpgrade(GoldMagnificationLevel);
            OnUpgradeCompleted?.Invoke();
        }
    }

    // 다이아 보너스 업그레이드 실행
    private void DiamondBonusUpgrade(int count)
    {
        if (ExecuteDiamondUpgradeTransaction(2, count))
        {
            DiamondBonusLevel += count;
            CurrencyManager.Instance.DiamondBonusUpgrade(DiamondBonusLevel);
            OnUpgradeCompleted?.Invoke();
        }
    }

    // 다이아 배율 업그레이드 실행
    private void DiamondMagnificationUpgrade(int count)
    {
        if (ExecuteDiamondUpgradeTransaction(3, count))
        {
            DiamondMagnificationLevel += count;
            CurrencyManager.Instance.DiamondMagnificationUpgrade(DiamondMagnificationLevel);
            OnUpgradeCompleted?.Invoke();
        }
    }

    // DP 코스트 보너스 업그레이드 실행
    private void DpCostBonusUpgrade(int count)
    {
        if (ExecuteDpCostUpgradeTransaction(4, count))
        {
            DpCostBonusLevel += count;
            CurrencyManager.Instance.DpCostBonusUpgrade(DpCostBonusLevel);
            OnUpgradeCompleted?.Invoke();
        }
    }

    // 최대 DP 상한 업그레이드 실행
    private void MaxDpCostUpgrade(int count)
    {
        if (ExecuteDpCostUpgradeTransaction(5, count))
        {
            MaxDpCostLevel += count;
            CurrencyManager.Instance.MaxDpCostUpgrade(MaxDpCostLevel);
            OnUpgradeCompleted?.Invoke();
        }
    }

    // 공통 스탯 업그레이드 실행 및 CommonGrowthService 전파
    private void ExecuteStatUpgrade(int type, int count)
    {
        double totalCost = GetUpgradeCost(type, count);
        if (!CurrencyManager.Instance.TrySpendGold((long)totalCost)) return;

        switch (type)
        {
            case 6:
                PhysicalAttackLevel += count;
                CommonGrowthService.Set(GrowthStatMask.PhysicalAttack, PhysicalAttackLevel * physicalAttackIncrease);
                break;
            case 7:
                MagicalAttackLevel += count;
                CommonGrowthService.Set(GrowthStatMask.MagicalAttack, MagicalAttackLevel * magicalAttackIncrease);
                break;
            case 8:
                MaxHpLevel += count;
                CommonGrowthService.Set(GrowthStatMask.MaxHp, MaxHpLevel * maxHpIncrease);
                break;
            case 9:
                HpRegenLevel += count;
                CommonGrowthService.Set(GrowthStatMask.HpRegenPerSecond, HpRegenLevel * hpRegenIncrease);
                break;
            case 10:
                PhysicalDefenseLevel += count;
                CommonGrowthService.Set(GrowthStatMask.PhysicalDefense, PhysicalDefenseLevel * physicalDefenseIncrease);
                break;
            case 11:
                MagicalDefenseLevel += count;
                CommonGrowthService.Set(GrowthStatMask.MagicalDefense, MagicalDefenseLevel * magicalDefenseIncrease);
                break;
            case 12:
                AttackSpeedLevel += count;
                CommonGrowthService.Set(GrowthStatMask.AttacksPerSecond, AttackSpeedLevel * attackSpeedIncrease);
                break;
            case 13:
                AccuracyLevel += count;
                CommonGrowthService.Set(GrowthStatMask.Accuracy, AccuracyLevel * accuracyIncrease);
                break;
            case 14:
                EvasionLevel += count;
                CommonGrowthService.Set(GrowthStatMask.Evasion, EvasionLevel * evasionIncrease);
                break;
            case 15:
                CriticalChanceLevel += count;
                CommonGrowthService.Set(GrowthStatMask.CriticalChancePercent, CriticalChanceLevel * criticalChanceIncrease);
                break;
            case 16:
                CriticalDamageLevel += count;
                CommonGrowthService.Set(GrowthStatMask.CriticalDamageBonusPercent, CriticalDamageLevel * criticalDamageIncrease);
                break;
        }

        OnUpgradeCompleted?.Invoke();
    }

    // 골드 수급 안전 차감 처리
    private bool ExecuteGoldUpgradeTransaction(int type, int count)
    {
        double totalCost = GetUpgradeCost(type, count);
        return CurrencyManager.Instance.TrySpendGold((long)totalCost);
    }

    // 다이아 수급 안전 차감 처리
    private bool ExecuteDiamondUpgradeTransaction(int type, int count)
    {
        double totalCost = GetUpgradeCost(type, count);
        return CurrencyManager.Instance.TrySpendDiamond((long)totalCost);
    }

    // DP 코스트 수급 안전 차감 처리
    private bool ExecuteDpCostUpgradeTransaction(int type, int count)
    {
        double totalCost = GetUpgradeCost(type, count);
        return CurrencyManager.Instance.TrySpendDpCost((int)totalCost);
    }

    #endregion

    #region 저장 관리

    // 세이브 데이터에 업그레이드 레벨 저장 처리
    private void OnSave(DataSaveEvent evt)
    {
        evt.saveData.statUpgrade.goldBonusLevel = GoldBonusLevel;
        evt.saveData.statUpgrade.goldMagnificationLevel = GoldMagnificationLevel;
        evt.saveData.statUpgrade.diamondBonusLevel = DiamondBonusLevel;
        evt.saveData.statUpgrade.diamondMagnificationLevel = DiamondMagnificationLevel;
        evt.saveData.statUpgrade.dpCostBonusLevel = DpCostBonusLevel;
        evt.saveData.statUpgrade.maxDpCostLevel = MaxDpCostLevel;

        evt.saveData.statUpgrade.physicalAttackLevel = PhysicalAttackLevel;
        evt.saveData.statUpgrade.magicalAttackLevel = MagicalAttackLevel;
        evt.saveData.statUpgrade.maxHpLevel = MaxHpLevel;
        evt.saveData.statUpgrade.hpRegenLevel = HpRegenLevel;
        evt.saveData.statUpgrade.physicalDefenseLevel = PhysicalDefenseLevel;
        evt.saveData.statUpgrade.magicalDefenseLevel = MagicalDefenseLevel;
        evt.saveData.statUpgrade.attackSpeedLevel = AttackSpeedLevel;
        evt.saveData.statUpgrade.accuracyLevel = AccuracyLevel;
        evt.saveData.statUpgrade.evasionLevel = EvasionLevel;
        evt.saveData.statUpgrade.criticalChanceLevel = CriticalChanceLevel;
        evt.saveData.statUpgrade.criticalDamageLevel = CriticalDamageLevel;
    }

    // 세이브 데이터 기반 레벨 복원 및 전파 처리
    private void OnLoad(DataLoadEvent evt)
    {
        GoldBonusLevel = evt.saveData.statUpgrade.goldBonusLevel;
        GoldMagnificationLevel = evt.saveData.statUpgrade.goldMagnificationLevel;
        DiamondBonusLevel = evt.saveData.statUpgrade.diamondBonusLevel;
        DiamondMagnificationLevel = evt.saveData.statUpgrade.diamondMagnificationLevel;
        DpCostBonusLevel = evt.saveData.statUpgrade.dpCostBonusLevel;
        MaxDpCostLevel = evt.saveData.statUpgrade.maxDpCostLevel;

        PhysicalAttackLevel = evt.saveData.statUpgrade.physicalAttackLevel;
        MagicalAttackLevel = evt.saveData.statUpgrade.magicalAttackLevel;
        MaxHpLevel = evt.saveData.statUpgrade.maxHpLevel;
        HpRegenLevel = evt.saveData.statUpgrade.hpRegenLevel;
        PhysicalDefenseLevel = evt.saveData.statUpgrade.physicalDefenseLevel;
        MagicalDefenseLevel = evt.saveData.statUpgrade.magicalDefenseLevel;
        AttackSpeedLevel = evt.saveData.statUpgrade.attackSpeedLevel;
        AccuracyLevel = evt.saveData.statUpgrade.accuracyLevel;
        EvasionLevel = evt.saveData.statUpgrade.evasionLevel;
        CriticalChanceLevel = evt.saveData.statUpgrade.criticalChanceLevel;
        CriticalDamageLevel = evt.saveData.statUpgrade.criticalDamageLevel;

        // 재화 시스템 적용
        CurrencyManager.Instance.GoldBonusUpgrade(GoldBonusLevel);
        CurrencyManager.Instance.GoldMagnificationUpgrade(GoldMagnificationLevel);
        CurrencyManager.Instance.DiamondBonusUpgrade(DiamondBonusLevel);
        CurrencyManager.Instance.DiamondMagnificationUpgrade(DiamondMagnificationLevel);
        CurrencyManager.Instance.DpCostBonusUpgrade(DpCostBonusLevel);
        CurrencyManager.Instance.MaxDpCostUpgrade(MaxDpCostLevel);

        // 아군 공통 스탯 전역 적용
        CommonGrowthService.Set(GrowthStatMask.PhysicalAttack, PhysicalAttackLevel * physicalAttackIncrease);
        CommonGrowthService.Set(GrowthStatMask.MagicalAttack, MagicalAttackLevel * magicalAttackIncrease);
        CommonGrowthService.Set(GrowthStatMask.MaxHp, MaxHpLevel * maxHpIncrease);
        CommonGrowthService.Set(GrowthStatMask.HpRegenPerSecond, HpRegenLevel * hpRegenIncrease);
        CommonGrowthService.Set(GrowthStatMask.PhysicalDefense, PhysicalDefenseLevel * physicalDefenseIncrease);
        CommonGrowthService.Set(GrowthStatMask.MagicalDefense, MagicalDefenseLevel * magicalDefenseIncrease);
        CommonGrowthService.Set(GrowthStatMask.AttacksPerSecond, AttackSpeedLevel * attackSpeedIncrease);
        CommonGrowthService.Set(GrowthStatMask.Accuracy, AccuracyLevel * accuracyIncrease);
        CommonGrowthService.Set(GrowthStatMask.Evasion, EvasionLevel * evasionIncrease);
        CommonGrowthService.Set(GrowthStatMask.CriticalChancePercent, CriticalChanceLevel * criticalChanceIncrease);
        CommonGrowthService.Set(GrowthStatMask.CriticalDamageBonusPercent, CriticalDamageLevel * criticalDamageIncrease);
    }

    // 업그레이드 레벨 및 전역 스탯 초기화 처리
    private void OnReset(DataResetEvent evt)
    {
        GoldBonusLevel = 0;
        GoldMagnificationLevel = 0;
        DiamondBonusLevel = 0;
        DiamondMagnificationLevel = 0;
        DpCostBonusLevel = 0;
        MaxDpCostLevel = 0;

        PhysicalAttackLevel = 0;
        MagicalAttackLevel = 0;
        MaxHpLevel = 0;
        HpRegenLevel = 0;
        PhysicalDefenseLevel = 0;
        MagicalDefenseLevel = 0;
        AttackSpeedLevel = 0;
        AccuracyLevel = 0;
        EvasionLevel = 0;
        CriticalChanceLevel = 0;
        CriticalDamageLevel = 0;

        CurrencyManager.Instance.GoldBonusUpgrade(0);
        CurrencyManager.Instance.GoldMagnificationUpgrade(0);
        CurrencyManager.Instance.DiamondBonusUpgrade(0);
        CurrencyManager.Instance.DiamondMagnificationUpgrade(0);
        CurrencyManager.Instance.DpCostBonusUpgrade(0);
        CurrencyManager.Instance.MaxDpCostUpgrade(0);

        CommonGrowthService.Clear();
    }

    #endregion
}
