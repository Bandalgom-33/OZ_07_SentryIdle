using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class CurrencyManager : SingletonBase<CurrencyManager>
{
#region 노출 변수 모음
    
    [Header("재화 베이스 정보")]
    [Tooltip("기본 생성/초기 보유 골드 수량")]
    [SerializeField] private long baseGold = 10;

    [Tooltip("기본 생성/초기 보유 다이아 수량")]
    [SerializeField] private long baseDiamond = 3;

    [Tooltip("기본 생성/초기 보유 DP 코스트 수량")]
    [SerializeField] private int baseDpCost = 0;

    [Tooltip("골드 고정 보너스 획득량")]
    [SerializeField] private long goldBonus = 0;

    [Tooltip("다이아 고정 보너스 획득량")]
    [SerializeField] private long diamondBonus = 0;

    [Tooltip("DP 코스트 고정 보너스 획득량")]
    [SerializeField] private int dpCostBonus = 0;

    [Tooltip("골드 획득 배율")]
    [SerializeField] private float goldMagnification = 1.0f;

    [Tooltip("다이아 획득 배율")]
    [SerializeField] private float diamondMagnification = 1.0f;

    [Tooltip("DP 코스트 1 회복에 소요되는 시간(초)")]
    [SerializeField] private float dpCostRegenTime = 1.0f;

    [Space(5f), Header("업그레이드 세팅 값")]
    [Tooltip("골드 보너스 1레벨당 증가 수량")]
    [SerializeField] private int goldBonusIncrease = 10;

    [Tooltip("골드 배율 1레벨당 증가 비율")]
    [SerializeField] private float goldMagnificationIncrease = 0.1f;

    [Tooltip("다이아 보너스 1레벨당 증가 수량")]
    [SerializeField] private int diamondBonusIncrease = 3;

    [Tooltip("다이아 배율 1레벨당 증가 비율")]
    [SerializeField] private float diamondMagnificationIncrease = 0.1f;

    [Tooltip("DP 코스트 보너스 1레벨당 증가 수량")]
    [SerializeField] private int dpCostBonusIncrease = 1;

    [Tooltip("최대 DP 상한 1레벨당 증가 수량")]
    [SerializeField] private int maxDpCostIncrease = 10;
    
#endregion

#region 프로퍼티
    
    public long Gold { get; private set; }
    public long Diamond { get; private set; }
    public int DpCost { get; private set; }
    public int MaxDpCost { get; private set; } = 100;
    public long WaveStone { get; private set; }
    public long StageStone { get; private set; }
    public long RaidStone { get; private set; }

    // 골드 보유 여부 검증
    public bool HasGold(long inputGold) => Gold >= inputGold;

    // 다이아 보유 여부 검증
    public bool HasDiamond(long inputDiamond) => Diamond >= inputDiamond;

    // DP 코스트 보유 여부 검증
    public bool HasDpCost(int inputDpCost) => DpCost >= inputDpCost;

    // 웨이브 마석 보유 여부 검증
    public bool HasWaveStone(long amount) => WaveStone >= amount;

    // 스테이지 마석 보유 여부 검증
    public bool HasStageStone(long amount) => StageStone >= amount;

    // 레이드 마석 보유 여부 검증
    public bool HasRaidStone(long amount) => RaidStone >= amount;

    // 통합 재화 잔액 보유 검사
    public bool HasEnoughCurrency(CurrencyType type, long amount)
    {
        return type switch
        {
            CurrencyType.Gold => HasGold(amount),
            CurrencyType.Diamond => HasDiamond(amount),
            CurrencyType.DpCost => HasDpCost((int)amount),
            CurrencyType.WaveStone => HasWaveStone(amount),
            CurrencyType.StageStone => HasStageStone(amount),
            CurrencyType.RaidStone => HasRaidStone(amount),
            _ => false
        };
    }

#endregion

#region 비공개 변수 모음
    
    private bool _isPaused = false;
    private float _currentRegenTime;
    
#endregion

#region 이벤트

    public static event Action<long> OnGoldChange;
    public static event Action<long> OnDiamondChange;
    public static event Action<int> OnDpCostChange;
    public static event Action<float> OnDpCostSliderChange; 
    public static event Action<long> OnWaveStoneChange;
    public static event Action<long> OnStageStoneChange;
    public static event Action<long> OnRaidStoneChange;

#endregion

#region 라이프 사이클

    // 이벤트 버스 구독 연산
    private void OnEnable()
    {
        EventBus.Subscribe<GameSpeedChangedEvent>(GameSpeedChange);
        EventBus.Subscribe<DataSaveEvent>(OnSave);
        EventBus.Subscribe<DataLoadEvent>(OnLoad);
        EventBus.Subscribe<DataResetEvent>(OnReset);
        EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
    }

    // DP 회복 루프 시작 연산
    private void Start()
    {
        _currentRegenTime = dpCostRegenTime;
        RegenDpCost(this.GetCancellationTokenOnDestroy()).Forget();
    }

    // 이벤트 버스 구독 해제 연산
    private void OnDisable()
    {
        EventBus.Unsubscribe<GameSpeedChangedEvent>(GameSpeedChange);
        EventBus.Unsubscribe<DataSaveEvent>(OnSave);
        EventBus.Unsubscribe<DataLoadEvent>(OnLoad);
        EventBus.Unsubscribe<DataResetEvent>(OnReset);
        EventBus.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
    }

    // 적 사망 시 골드 보상 획득 처리
    private void OnEnemyDied(EnemyDiedEvent eventMessage)
    {
        if (eventMessage.rewardGold > 0)
        {
            GetGold(eventMessage.rewardGold, applyModifiers: true);
        }
    }

#endregion

#region 재화 획득 및 차감 메서드

    // 통합 재화 수급 처리
    public void AddCurrency(CurrencyType type, long amount, bool applyModifiers = true)
    {
        switch (type)
        {
            case CurrencyType.Gold:
                GetGold(amount, applyModifiers);
                break;
            case CurrencyType.Diamond:
                GetDiamond(amount, applyModifiers);
                break;
            case CurrencyType.DpCost:
                GetDpCost((int)amount);
                break;
            case CurrencyType.WaveStone:
                GetWaveStone(amount);
                break;
            case CurrencyType.StageStone:
                GetStageStone(amount);
                break;
            case CurrencyType.RaidStone:
                GetRaidStone(amount);
                break;
        }
    }

    // 골드 획득 연산
    public void GetGold(long baseAmount, bool applyModifiers = true)
    {
        long finalGold = baseAmount;
        if (applyModifiers)
        {
            double calculated = (baseAmount + goldBonus) * (double)goldMagnification;
            finalGold = (long)Math.Round(calculated);
        }

        Gold += finalGold;
        OnGoldChange?.Invoke(Gold);
        EventBus.Publish(new CurrencyChangedEvent(CurrencyType.Gold, Gold, finalGold));
        Debug.Log("Gold: " + finalGold);
    }

    // 골드 획득 테스트 연산
    public void TestGetGold()
    {
        GetGold(1000000000000000, applyModifiers: false);
    }

    // 골드 소모 연산
    public bool TrySpendGold(long gold)
    {
        if (Gold < gold) return false;
        Gold -= gold;
        OnGoldChange?.Invoke(Gold);
        EventBus.Publish(new CurrencyChangedEvent(CurrencyType.Gold, Gold, -gold));
        return true;
    }

    // 다이아 획득 연산
    public void GetDiamond(long baseAmount, bool applyModifiers = true)
    {
        long finalDiamond = baseAmount;
        if (applyModifiers)
        {
            double calculated = (baseAmount + diamondBonus) * (double)diamondMagnification;
            finalDiamond = (long)Math.Round(calculated);
        }

        Diamond += finalDiamond;
        OnDiamondChange?.Invoke(Diamond);
        EventBus.Publish(new CurrencyChangedEvent(CurrencyType.Diamond, Diamond, finalDiamond));
    }

    // 다이아 소모 연산
    public bool TrySpendDiamond(long diamond)
    {
        if (Diamond < diamond) return false;
        Diamond -= diamond;
        OnDiamondChange?.Invoke(Diamond);
        EventBus.Publish(new CurrencyChangedEvent(CurrencyType.Diamond, Diamond, -diamond));
        return true;
    }

    // DP 코스트 획득 연산
    public void GetDpCost(int dpCost)
    {
        int prevDp = DpCost;
        DpCost = Mathf.Min(DpCost + dpCost, MaxDpCost);
        int change = DpCost - prevDp;
        OnDpCostChange?.Invoke(DpCost);
        EventBus.Publish(new CurrencyChangedEvent(CurrencyType.DpCost, DpCost, change));

        if (DpCost >= MaxDpCost)
        {
            _isPaused = true;
        }
    }

    // DP 코스트 소모 연산
    public bool TrySpendDpCost(int dpCost)
    {
        if (DpCost < dpCost) return false;

        DpCost -= dpCost;
        OnDpCostChange?.Invoke(DpCost);
        EventBus.Publish(new CurrencyChangedEvent(CurrencyType.DpCost, DpCost, -dpCost));

        if (DpCost < MaxDpCost)
        {
            _isPaused = false;
        }

        return true;
    }

    // DP 코스트 설정 연산
    public void SetDpCost(int dpCost)
    {
        DpCost = Mathf.Clamp(dpCost, 0, MaxDpCost);
        OnDpCostChange?.Invoke(DpCost);
        EventBus.Publish(new CurrencyChangedEvent(CurrencyType.DpCost, DpCost, 0));

        _isPaused = (DpCost >= MaxDpCost);
    }

    // 라운드 시작 시 DP 코스트 초기화 연산
    public void ResetDpCostOnRoundStart()
    {
        SetDpCost(baseDpCost + dpCostBonus);
    }

    // 웨이브 마석 획득 연산
    public void GetWaveStone(long amount)
    {
        WaveStone += amount;
        OnWaveStoneChange?.Invoke(WaveStone);
        EventBus.Publish(new CurrencyChangedEvent(CurrencyType.WaveStone, WaveStone, amount));
    }

    // 웨이브 마석 소모 연산
    public bool TrySpendWaveStone(long amount)
    {
        if (WaveStone < amount) return false;
        WaveStone -= amount;
        OnWaveStoneChange?.Invoke(WaveStone);
        EventBus.Publish(new CurrencyChangedEvent(CurrencyType.WaveStone, WaveStone, -amount));
        return true;
    }

    // 스테이지 마석 획득 연산
    public void GetStageStone(long amount)
    {
        StageStone += amount;
        OnStageStoneChange?.Invoke(StageStone);
        EventBus.Publish(new CurrencyChangedEvent(CurrencyType.StageStone, StageStone, amount));
    }

    // 스테이지 마석 소모 연산
    public bool TrySpendStageStone(long amount)
    {
        if (StageStone < amount) return false;
        StageStone -= amount;
        OnStageStoneChange?.Invoke(StageStone);
        EventBus.Publish(new CurrencyChangedEvent(CurrencyType.StageStone, StageStone, -amount));
        return true;
    }

    // 레이드 마석 획득 연산
    public void GetRaidStone(long amount)
    {
        RaidStone += amount;
        OnRaidStoneChange?.Invoke(RaidStone);
        EventBus.Publish(new CurrencyChangedEvent(CurrencyType.RaidStone, RaidStone, amount));
    }

    // 레이드 마석 소모 연산
    public bool TrySpendRaidStone(long amount)
    {
        if (RaidStone < amount) return false;
        RaidStone -= amount;
        OnRaidStoneChange?.Invoke(RaidStone);
        EventBus.Publish(new CurrencyChangedEvent(CurrencyType.RaidStone, RaidStone, -amount));
        return true;
    }

    // 통합 재화 소모 연산
    public bool ConsumeCurrency(CurrencyType type, long amount)
    {
        return type switch
        {
            CurrencyType.Gold => TrySpendGold(amount),
            CurrencyType.Diamond => TrySpendDiamond(amount),
            CurrencyType.DpCost => TrySpendDpCost((int)amount),
            CurrencyType.WaveStone => TrySpendWaveStone(amount),
            CurrencyType.StageStone => TrySpendStageStone(amount),
            CurrencyType.RaidStone => TrySpendRaidStone(amount),
            _ => false
        };
    }
    
#endregion

#region 재화 관련 업그레이드 메서드

    // 골드 보너스 업그레이드 연산
    public void GoldBonusUpgrade(int level)
    {
        goldBonus = level * goldBonusIncrease;
    }

    // 골드 배율 업그레이드 연산
    public void GoldMagnificationUpgrade(int level)
    {
        goldMagnification = 1.0f + (level * goldMagnificationIncrease);
    }

    // 다이아 보너스 업그레이드 연산
    public void DiamondBonusUpgrade(int level)
    {
        diamondBonus = level * diamondBonusIncrease;
    }

    // 다이아 배율 업그레이드 연산
    public void DiamondMagnificationUpgrade(int level)
    {
        diamondMagnification = 1.0f + (level * diamondMagnificationIncrease);
    }

    // DP 코스트 보너스 업그레이드 연산
    public void DpCostBonusUpgrade(int level)
    {
        dpCostBonus = level * dpCostBonusIncrease;
    }

    // 최대 DP 코스트 업그레이드 연산
    public void MaxDpCostUpgrade(int level)
    {
        MaxDpCost = 100 + (level * maxDpCostIncrease);

        if (DpCost < MaxDpCost)
        {
            _isPaused = false;
        }
    }

#endregion

#region 계산 메서드

    // 게임 속도 변경 처리
    private void GameSpeedChange(GameSpeedChangedEvent evt)
    {
        if (evt.timeScale == 0)
        {
            _isPaused = true;
        }
        else
        {
            _isPaused = false;
            _currentRegenTime = dpCostRegenTime / evt.timeScale;
        }
    }
    
    // DP 코스트 자동 회복 연산
    private async UniTaskVoid RegenDpCost(CancellationToken token)
    {
        float timer = 0;
        while (true)
        {
            token.ThrowIfCancellationRequested();
            if (!_isPaused)
            {
                timer += Time.deltaTime;
                float sliderValue = Mathf.Lerp(0, 1, timer / _currentRegenTime);
                OnDpCostSliderChange?.Invoke(sliderValue);
                if (sliderValue >= 1)
                {
                    int dpCost = baseDpCost + dpCostBonus;
                    if (dpCost <= 0) dpCost = 1;
                    GetDpCost(dpCost);
                    timer = 0;
                }
            }
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }
    }
     
#endregion

#region 재화 저장 관리

    // 세이브 데이터 저장 연산
    private void OnSave(DataSaveEvent evt)
    {
        evt.saveData.currency.gold = Gold;
        evt.saveData.currency.diamond = Diamond;
        evt.saveData.currency.waveStone = WaveStone;
        evt.saveData.currency.stageStone = StageStone;
        evt.saveData.currency.raidStone = RaidStone;
    }

    // 세이브 데이터 로드 연산
    private void OnLoad(DataLoadEvent evt)
    {
        Gold = evt.saveData.currency.gold;
        Diamond = evt.saveData.currency.diamond;
        WaveStone = evt.saveData.currency.waveStone;
        StageStone = evt.saveData.currency.stageStone;
        RaidStone = evt.saveData.currency.raidStone;
        DpCost = 5;

        OnGoldChange?.Invoke(Gold);
        OnDiamondChange?.Invoke(Diamond);
        OnDpCostChange?.Invoke(DpCost);
        OnWaveStoneChange?.Invoke(WaveStone);
        OnStageStoneChange?.Invoke(StageStone);
        OnRaidStoneChange?.Invoke(RaidStone);

        EventBus.Publish(new CurrencyChangedEvent(CurrencyType.Gold, Gold, 0));
        EventBus.Publish(new CurrencyChangedEvent(CurrencyType.Diamond, Diamond, 0));
        EventBus.Publish(new CurrencyChangedEvent(CurrencyType.DpCost, DpCost, 0));
        EventBus.Publish(new CurrencyChangedEvent(CurrencyType.WaveStone, WaveStone, 0));
        EventBus.Publish(new CurrencyChangedEvent(CurrencyType.StageStone, StageStone, 0));
        EventBus.Publish(new CurrencyChangedEvent(CurrencyType.RaidStone, RaidStone, 0));
    }

    // 데이터 초기화 연산
    private void OnReset(DataResetEvent evt)
    {
        Gold = baseGold;
        Diamond = baseDiamond;
        DpCost = baseDpCost;
        WaveStone = 0;
        StageStone = 0;
        RaidStone = 0;

        OnGoldChange?.Invoke(Gold);
        OnDiamondChange?.Invoke(Diamond);
        OnDpCostChange?.Invoke(DpCost);
        OnWaveStoneChange?.Invoke(WaveStone);
        OnStageStoneChange?.Invoke(StageStone);
        OnRaidStoneChange?.Invoke(RaidStone);

        EventBus.Publish(new CurrencyChangedEvent(CurrencyType.Gold, Gold, 0));
        EventBus.Publish(new CurrencyChangedEvent(CurrencyType.Diamond, Diamond, 0));
        EventBus.Publish(new CurrencyChangedEvent(CurrencyType.DpCost, DpCost, 0));
        EventBus.Publish(new CurrencyChangedEvent(CurrencyType.WaveStone, WaveStone, 0));
        EventBus.Publish(new CurrencyChangedEvent(CurrencyType.StageStone, StageStone, 0));
        EventBus.Publish(new CurrencyChangedEvent(CurrencyType.RaidStone, RaidStone, 0));
    }

#endregion
}


