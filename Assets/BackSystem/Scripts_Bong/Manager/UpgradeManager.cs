using System;
using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Runtime;
using UnityEngine;

// 골드/다이아/DP 및 아군 공통 전투 스탯 업그레이드 수치 관리와 CommonGrowthService 연동을 총괄하는 싱글톤 매니저
public class UpgradeManager : SingletonBase<UpgradeManager>
{
    #region 업그레이드 종류 열거형

    private enum UpgradeType
    {
        GoldBonus = 0,
        GoldMagnification = 1,
        DiamondBonus = 2,
        DiamondMagnification = 3,
        DpCostBonus = 4,
        MaxDpCost = 5,
        PhysicalAttack = 6,
        MagicalAttack = 7,
        MaxHp = 8,
        HpRegen = 9,
        PhysicalDefense = 10,
        MagicalDefense = 11,
        AttackSpeed = 12,
        Accuracy = 13,
        Evasion = 14,
        CriticalChance = 15,
        CriticalDamage = 16
    }

    #endregion

    #region 노출 변수

    [Header("--- 구매 상한 설정 ---")]
    [Tooltip("1회 구매 가능한 최대 강화 횟수 제한 (기본 9999)")]
    [SerializeField] private int maxPurchaseLimit = 9999;

    [Space(5f), Header("--- 골드 업그레이드 설정 값 ---")]
    [Tooltip("골드 보너스 1레벨 업그레이드 소모 비용")]
    [SerializeField] private int goldBonusUpgradeCost = 1000;

    [Tooltip("골드 배율 1레벨 업그레이드 소모 비용")]
    [SerializeField] private long goldMagnificationUpgradeCost = 1000;

    [Tooltip("골드 보너스 레벨당 비용 증가 가중치 배율")]
    [SerializeField] private float goldBonusIncreaseMultiplier = 1.5f;

    [Tooltip("골드 배율 레벨당 비용 증가 가중치 배율")]
    [SerializeField] private float goldMagnificationIncreaseMultiplier = 2.5f;

    [Space(5f), Header("--- 다이아 업그레이드 설정 값 ---")]
    [Tooltip("다이아 보너스 1레벨 업그레이드 소모 비용")]
    [SerializeField] private int diamondBonusUpgradeCost = 10;

    [Tooltip("다이아 배율 1레벨 업그레이드 소모 비용")]
    [SerializeField] private float diamondMagnificationUpgradeCost = 10;

    [Tooltip("다이아 보너스 레벨당 비용 증가 가중치 배율")]
    [SerializeField] private float diamondBonusIncreaseMultiplier = 1.5f;

    [Tooltip("다이아 배율 레벨당 비용 증가 가중치 배율")]
    [SerializeField] private float diamondMagnificationIncreaseMultiplier = 2.5f;

    [Space(5f), Header("--- 소환 코스트 업그레이드 설정 값 ---")]
    [Tooltip("DP 코스트 보너스 1레벨 업그레이드 소모 비용")]
    [SerializeField] private int dpCostBonusUpgradeCost = 5;

    [Tooltip("최대 DP 상한 1레벨 업그레이드 소모 비용")]
    [SerializeField] private float maxDpCostUpgradeCost = 5;

    [Tooltip("DP 코스트 보너스 레벨당 비용 증가 가중치 배율")]
    [SerializeField] private float dpCostBonusIncreaseMultiplier = 1.5f;

    [Tooltip("최대 DP 상한 레벨당 비용 증가 가중치 배율")]
    [SerializeField] private float maxDpCostIncreaseMultiplier = 2.5f;

    [Space(5f), Header("--- 아군 공통 스탯 기본 비용 ---")]
    [Tooltip("공통 스탯 1레벨 기본 소모 골드 비용")]
    [SerializeField] private long baseStatUpgradeCost = 500;

    [Tooltip("공통 스탯 레벨당 비용 증가 배율")]
    [SerializeField] private float statCostMultiplier = 1.3f;

    [Space(5f), Header("--- 공통 스탯 1레벨당 수치 증가 세팅 ---")]
    [Tooltip("물리 공격력 1레벨당 수치 증가량")]
    [SerializeField] private float physicalAttackIncrease = 10f;

    [Tooltip("마법 공격력 1레벨당 수치 증가량")]
    [SerializeField] private float magicalAttackIncrease = 10f;

    [Tooltip("최대 체력 1레벨당 수치 증가량")]
    [SerializeField] private float maxHpIncrease = 100f;

    [Tooltip("초당 HP 재생 1레벨당 수치 증가량")]
    [SerializeField] private float hpRegenIncrease = 1.0f;

    [Tooltip("물리 방어력 1레벨당 수치 증가량")]
    [SerializeField] private float physicalDefenseIncrease = 5f;

    [Tooltip("마법 방어력 1레벨당 수치 증가량")]
    [SerializeField] private float magicalDefenseIncrease = 5f;

    [Tooltip("공격 속도 1레벨당 수치 증가량")]
    [SerializeField] private float attackSpeedIncrease = 0.05f;

    [Tooltip("명중력 1레벨당 수치 증가량")]
    [SerializeField] private float accuracyIncrease = 5f;

    [Tooltip("회피력 1레벨당 수치 증가량")]
    [SerializeField] private float evasionIncrease = 5f;

    [Tooltip("치명타 확률 1레벨당 수치 증가량 (%)")]
    [SerializeField] private float criticalChanceIncrease = 0.5f;

    [Tooltip("치명타 피해량 1레벨당 수치 증가량 (%)")]
    [SerializeField] private float criticalDamageIncrease = 1.0f;

    [Space(5f), Header("--- 공통 스탯 최대 레벨 상한 설정 (0은 무제한) ---")]
    [Tooltip("공격 속도 최대 레벨 상한 (0은 무제한)")]
    [SerializeField] private int attackSpeedMaxLevel = 100;

    [Tooltip("치명타 확률 최대 레벨 상한 (0은 무제한)")]
    [SerializeField] private int criticalChanceMaxLevel = 100;

    [Tooltip("치명타 피해량 최대 레벨 상한 (0은 무제한)")]
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

    // 업그레이드 타입별 최대 레벨 상한 조회
    public int GetMaxLevelByType(int type)
    {
        return (UpgradeType)type switch
        {
            UpgradeType.AttackSpeed => attackSpeedMaxLevel,
            UpgradeType.CriticalChance => criticalChanceMaxLevel,
            UpgradeType.CriticalDamage => criticalDamageMaxLevel,
            _ => 0
        };
    }

    // 업그레이드 수치 텍스트 생성
    public string GetStatValue(int type, int level)
    {
        int maxLvl = GetMaxLevelByType(type);
        if (maxLvl > 0 && level >= maxLvl)
        {
            return (UpgradeType)type switch
            {
                UpgradeType.AttackSpeed => $"+{level * attackSpeedIncrease:F2} (MAX)",
                UpgradeType.CriticalChance => $"+{level * criticalChanceIncrease:F1}% (MAX)",
                UpgradeType.CriticalDamage => $"+{level * criticalDamageIncrease:F1}% (MAX)",
                _ => "MAX"
            };
        }

        return (UpgradeType)type switch
        {
            UpgradeType.GoldBonus => $"+{level * 10}",
            UpgradeType.GoldMagnification => $"x{1.0f + (level * 0.1f):F1} (+{level * 10}%)",
            UpgradeType.DiamondBonus => $"+{level * 3}",
            UpgradeType.DiamondMagnification => $"x{1.0f + (level * 0.1f):F1} (+{level * 10}%)",
            UpgradeType.DpCostBonus => $"+{level * 1} DP",
            UpgradeType.MaxDpCost => $"Max DP {100 + (level * 10)}",
            UpgradeType.PhysicalAttack => $"+{level * physicalAttackIncrease:N0}",
            UpgradeType.MagicalAttack => $"+{level * magicalAttackIncrease:N0}",
            UpgradeType.MaxHp => $"+{level * maxHpIncrease:N0}",
            UpgradeType.HpRegen => $"+{level * hpRegenIncrease:F1}/sec",
            UpgradeType.PhysicalDefense => $"+{level * physicalDefenseIncrease:N0}",
            UpgradeType.MagicalDefense => $"+{level * magicalDefenseIncrease:N0}",
            UpgradeType.AttackSpeed => $"+{level * attackSpeedIncrease:F2}",
            UpgradeType.Accuracy => $"+{level * accuracyIncrease:N0}",
            UpgradeType.Evasion => $"+{level * evasionIncrease:N0}",
            UpgradeType.CriticalChance => $"+{level * criticalChanceIncrease:F1}%",
            UpgradeType.CriticalDamage => $"+{level * criticalDamageIncrease:F1}%",
            _ => $"+{level}"
        };
    }

    // 업그레이드 누적 비용 연산
    public double GetUpgradeCost(int type, int count)
    {
        if (count <= 0) return 0;

        int currentLevel = GetLevelByType(type);
        int maxLvl = GetMaxLevelByType(type);

        if (maxLvl > 0 && currentLevel >= maxLvl)
        {
            return double.MaxValue;
        }

        double totalCost = 0;
        int targetCount = count;
        if (maxLvl > 0 && currentLevel + count > maxLvl)
        {
            targetCount = maxLvl - currentLevel;
        }

        for (int i = 0; i < targetCount; i++)
        {
            totalCost += CalculateSingleStepCost((UpgradeType)type, currentLevel + i);
        }

        return totalCost;
    }

    // 최대 구매 가능 횟수 연산
    public int GetMaxPurchasableCount(int type, long availableCurrency)
    {
        int currentLevel = GetLevelByType(type);
        int maxLvl = GetMaxLevelByType(type);

        if (maxLvl > 0 && currentLevel >= maxLvl)
        {
            return 0;
        }

        int count = 0;
        double accumulatedCost = 0;

        while (true)
        {
            if (maxLvl > 0 && currentLevel + count >= maxLvl) break;

            double stepCost = CalculateSingleStepCost((UpgradeType)type, currentLevel + count);
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

    // 업그레이드 타입별 보유 재화 조회
    public long GetAvailableCurrencyForType(int type)
    {
        if (CurrencyManager.Instance == null) return 0;

        return (UpgradeType)type switch
        {
            UpgradeType.GoldBonus or UpgradeType.GoldMagnification => CurrencyManager.Instance.Gold,
            UpgradeType.DiamondBonus or UpgradeType.DiamondMagnification => CurrencyManager.Instance.Diamond,
            UpgradeType.DpCostBonus or UpgradeType.MaxDpCost => CurrencyManager.Instance.DpCost,
            _ => CurrencyManager.Instance.Gold
        };
    }

    // 업그레이드 타입별 현재 레벨 조회
    public int GetLevelByType(int type)
    {
        return (UpgradeType)type switch
        {
            UpgradeType.GoldBonus => GoldBonusLevel,
            UpgradeType.GoldMagnification => GoldMagnificationLevel,
            UpgradeType.DiamondBonus => DiamondBonusLevel,
            UpgradeType.DiamondMagnification => DiamondMagnificationLevel,
            UpgradeType.DpCostBonus => DpCostBonusLevel,
            UpgradeType.MaxDpCost => MaxDpCostLevel,
            UpgradeType.PhysicalAttack => PhysicalAttackLevel,
            UpgradeType.MagicalAttack => MagicalAttackLevel,
            UpgradeType.MaxHp => MaxHpLevel,
            UpgradeType.HpRegen => HpRegenLevel,
            UpgradeType.PhysicalDefense => PhysicalDefenseLevel,
            UpgradeType.MagicalDefense => MagicalDefenseLevel,
            UpgradeType.AttackSpeed => AttackSpeedLevel,
            UpgradeType.Accuracy => AccuracyLevel,
            UpgradeType.Evasion => EvasionLevel,
            UpgradeType.CriticalChance => CriticalChanceLevel,
            UpgradeType.CriticalDamage => CriticalDamageLevel,
            _ => 0
        };
    }

    // 단일 단계 소모 비용 연산
    private double CalculateSingleStepCost(UpgradeType type, int targetLevel)
    {
        return type switch
        {
            UpgradeType.GoldBonus => goldBonusUpgradeCost * Mathf.Pow(goldBonusIncreaseMultiplier, targetLevel),
            UpgradeType.GoldMagnification => goldMagnificationUpgradeCost * Mathf.Pow(goldMagnificationIncreaseMultiplier, targetLevel),
            UpgradeType.DiamondBonus => diamondBonusUpgradeCost * Mathf.Pow(diamondBonusIncreaseMultiplier, targetLevel),
            UpgradeType.DiamondMagnification => diamondMagnificationUpgradeCost * Mathf.Pow(diamondMagnificationIncreaseMultiplier, targetLevel),
            UpgradeType.DpCostBonus => dpCostBonusUpgradeCost * Mathf.Pow(dpCostBonusIncreaseMultiplier, targetLevel),
            UpgradeType.MaxDpCost => maxDpCostUpgradeCost * Mathf.Pow(maxDpCostIncreaseMultiplier, targetLevel),
            _ => baseStatUpgradeCost * Mathf.Pow(statCostMultiplier, targetLevel)
        };
    }

    #endregion

    #region 업그레이드 실행 및 CommonGrowthService 연동

    // 업그레이드 실행 요청 처리
    private void SelectUpgrade(int typeInt, int count)
    {
        UpgradeType type = (UpgradeType)typeInt;
        int actualCount = count;

        if (count == -1)
        {
            long currentCurrency = GetAvailableCurrencyForType(typeInt);
            actualCount = GetMaxPurchasableCount(typeInt, currentCurrency);
            if (actualCount <= 0) return;
        }

        int maxLvl = GetMaxLevelByType(typeInt);
        int currentLvl = GetLevelByType(typeInt);
        if (maxLvl > 0 && currentLvl + actualCount > maxLvl)
        {
            actualCount = maxLvl - currentLvl;
            if (actualCount <= 0) return;
        }

        if (typeInt <= 5)
        {
            ExecuteCurrencyUpgrade(type, actualCount);
        }
        else
        {
            ExecuteStatUpgrade(type, actualCount);
        }
    }

    // 재화 보너스/배율 업그레이드 실행
    private void ExecuteCurrencyUpgrade(UpgradeType type, int count)
    {
        double totalCost = GetUpgradeCost((int)type, count);

        CurrencyType currencyType = type switch
        {
            UpgradeType.GoldBonus or UpgradeType.GoldMagnification => CurrencyType.Gold,
            UpgradeType.DiamondBonus or UpgradeType.DiamondMagnification => CurrencyType.Diamond,
            _ => CurrencyType.DpCost
        };

        if (!CurrencyManager.Instance.ConsumeCurrency(currencyType, (long)totalCost)) return;

        switch (type)
        {
            case UpgradeType.GoldBonus:
                GoldBonusLevel += count;
                CurrencyManager.Instance.GoldBonusUpgrade(GoldBonusLevel);
                break;
            case UpgradeType.GoldMagnification:
                GoldMagnificationLevel += count;
                CurrencyManager.Instance.GoldMagnificationUpgrade(GoldMagnificationLevel);
                break;
            case UpgradeType.DiamondBonus:
                DiamondBonusLevel += count;
                CurrencyManager.Instance.DiamondBonusUpgrade(DiamondBonusLevel);
                break;
            case UpgradeType.DiamondMagnification:
                DiamondMagnificationLevel += count;
                CurrencyManager.Instance.DiamondMagnificationUpgrade(DiamondMagnificationLevel);
                break;
            case UpgradeType.DpCostBonus:
                DpCostBonusLevel += count;
                CurrencyManager.Instance.DpCostBonusUpgrade(DpCostBonusLevel);
                break;
            case UpgradeType.MaxDpCost:
                MaxDpCostLevel += count;
                CurrencyManager.Instance.MaxDpCostUpgrade(MaxDpCostLevel);
                break;
        }

        OnUpgradeCompleted?.Invoke();
    }

    // 공통 스탯 업그레이드 연산 및 전파
    private void ExecuteStatUpgrade(UpgradeType type, int count)
    {
        double totalCost = GetUpgradeCost((int)type, count);
        if (!CurrencyManager.Instance.TrySpendGold((long)totalCost)) return;

        switch (type)
        {
            case UpgradeType.PhysicalAttack:
                PhysicalAttackLevel += count;
                CommonGrowthService.Set(GrowthStatMask.PhysicalAttack, PhysicalAttackLevel * physicalAttackIncrease);
                break;
            case UpgradeType.MagicalAttack:
                MagicalAttackLevel += count;
                CommonGrowthService.Set(GrowthStatMask.MagicalAttack, MagicalAttackLevel * magicalAttackIncrease);
                break;
            case UpgradeType.MaxHp:
                MaxHpLevel += count;
                CommonGrowthService.Set(GrowthStatMask.MaxHp, MaxHpLevel * maxHpIncrease);
                break;
            case UpgradeType.HpRegen:
                HpRegenLevel += count;
                CommonGrowthService.Set(GrowthStatMask.HpRegenPerSecond, HpRegenLevel * hpRegenIncrease);
                break;
            case UpgradeType.PhysicalDefense:
                PhysicalDefenseLevel += count;
                CommonGrowthService.Set(GrowthStatMask.PhysicalDefense, PhysicalDefenseLevel * physicalDefenseIncrease);
                break;
            case UpgradeType.MagicalDefense:
                MagicalDefenseLevel += count;
                CommonGrowthService.Set(GrowthStatMask.MagicalDefense, MagicalDefenseLevel * magicalDefenseIncrease);
                break;
            case UpgradeType.AttackSpeed:
                AttackSpeedLevel += count;
                CommonGrowthService.Set(GrowthStatMask.AttacksPerSecond, AttackSpeedLevel * attackSpeedIncrease);
                break;
            case UpgradeType.Accuracy:
                AccuracyLevel += count;
                CommonGrowthService.Set(GrowthStatMask.Accuracy, AccuracyLevel * accuracyIncrease);
                break;
            case UpgradeType.Evasion:
                EvasionLevel += count;
                CommonGrowthService.Set(GrowthStatMask.Evasion, EvasionLevel * evasionIncrease);
                break;
            case UpgradeType.CriticalChance:
                CriticalChanceLevel += count;
                CommonGrowthService.Set(GrowthStatMask.CriticalChancePercent, CriticalChanceLevel * criticalChanceIncrease);
                break;
            case UpgradeType.CriticalDamage:
                CriticalDamageLevel += count;
                CommonGrowthService.Set(GrowthStatMask.CriticalDamageBonusPercent, CriticalDamageLevel * criticalDamageIncrease);
                break;
        }

        OnUpgradeCompleted?.Invoke();
    }

    // 재화 업그레이드 값 일괄 적용 헬퍼
    private void ApplyAllCurrencyUpgrades()
    {
        if (CurrencyManager.Instance == null) return;

        CurrencyManager.Instance.GoldBonusUpgrade(GoldBonusLevel);
        CurrencyManager.Instance.GoldMagnificationUpgrade(GoldMagnificationLevel);
        CurrencyManager.Instance.DiamondBonusUpgrade(DiamondBonusLevel);
        CurrencyManager.Instance.DiamondMagnificationUpgrade(DiamondMagnificationLevel);
        CurrencyManager.Instance.DpCostBonusUpgrade(DpCostBonusLevel);
        CurrencyManager.Instance.MaxDpCostUpgrade(MaxDpCostLevel);
    }

    // 공통 스탯 수치 일괄 반영 헬퍼
    private void ApplyAllCommonGrowthStats()
    {
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

    #endregion

    #region 저장 관리

    // 세이브 데이터 저장 연산
    private void OnSave(DataSaveEvent evt)
    {
        if (evt.saveData == null) return;
        if (evt.saveData.statUpgrade == null)
        {
            evt.saveData.statUpgrade = new CurrencyUpgradeData();
        }

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

    // 세이브 데이터 로드 연산
    private void OnLoad(DataLoadEvent evt)
    {
        if (evt.saveData == null || evt.saveData.statUpgrade == null) return;

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

        ApplyAllCurrencyUpgrades();
        ApplyAllCommonGrowthStats();
    }

    // 업그레이드 데이터 초기화 연산
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

        ApplyAllCurrencyUpgrades();
        CommonGrowthService.Clear();
    }

    #endregion
}
