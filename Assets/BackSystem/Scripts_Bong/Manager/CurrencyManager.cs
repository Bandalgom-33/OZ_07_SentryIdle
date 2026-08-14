using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

// 게임 내 3대 핵심 재화(Gold, Diamond, DpCost) 통합 관리 및 트랜잭션 전담 싱글톤
public enum CurrencyType
{
    Gold,
    Diamond,
    DpCost
}

public class CurrencyManager : SingletonBase<CurrencyManager>
{
#region 노출 변수 모음
    
    [Header("재화 베이스 정보")]
    [SerializeField] private int baseGold = 10;
    [SerializeField] private int baseDiamond = 3;
    [SerializeField] private int baseDpCost = 0;
    [SerializeField] private long goldBonus ;
    [SerializeField] private int diamondBonus ;
    [SerializeField] private int dpCostBonus ;
    [SerializeField] private float goldMagnification = 1.0f;
    [SerializeField] private float diamondMagnification = 1.0f;
    [SerializeField] private float dpCostRegenTime = 1.0f;
    [Space(5f), Header("업그레이드 세팅 값")]
    [SerializeField] private int goldBonusIncrease = 10;
    [SerializeField] private float goldMagnificationIncrease = 0.1f;
    [SerializeField] private int diamondBonusIncrease = 3;
    [SerializeField] private float diamondMagnificationIncrease = 0.1f;
    [SerializeField] private int dpCostBonusIncrease = 1;
    [SerializeField] private int maxDpCostIncrease = 10;
    
#endregion

#region 프로퍼티
    
    public long Gold { get; private set; }
    public int Diamond { get; private set; }
    public int DpCost { get; private set; }
    public int MaxDpCost { get; private set; } = 100;

    // 재화 잔액 보유 여부 검증 프로퍼티
    public bool HasGold(long inputGold) => Gold >= inputGold;
    public bool HasDiamond(int inputDiamond) => Diamond >= inputDiamond;
    public bool HasDpCost(int inputDpCost) => DpCost >= inputDpCost;

#endregion

#region 비공개 변수 모음
    
    private bool _isPaused = false;
    private float _currentRegenTime;
    
#endregion

#region 이벤트

    public static event Action<long> OnGoldChange;
    public static event Action<int> OnDiamondChange;
    public static event Action<int> OnDpCostChange;
    public static event Action<float> OnDpCostSliderChange; 

#endregion

#region 라이프 사이클

    // 중앙 EventBus 이벤트 및 재화 연동 레지스터 구독 등록
    private void OnEnable()
    {
        EventBus.Subscribe<GameSpeedChangedEvent>(GameSpeedChange);
        EventBus.Subscribe<DataSaveEvent>(OnSave);
        EventBus.Subscribe<DataLoadEvent>(OnLoad);
        EventBus.Subscribe<DataResetEvent>(OnReset);
        EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
    }

    // 비동기 DP 회복 루프 시작 및 기본 리젠 시간 초기화 연산
    private void Start()
    {
        _currentRegenTime = dpCostRegenTime;
        RegenDpCost(this.GetCancellationTokenOnDestroy()).Forget();
    }

    // 중앙 EventBus 이벤트 구독 해제 (메모리 누수 방지)
    private void OnDisable()
    {
        EventBus.Unsubscribe<GameSpeedChangedEvent>(GameSpeedChange);
        EventBus.Unsubscribe<DataSaveEvent>(OnSave);
        EventBus.Unsubscribe<DataLoadEvent>(OnLoad);
        EventBus.Unsubscribe<DataResetEvent>(OnReset);
        EventBus.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
    }

    // 적 처치 시 보상 골드 획득 핸들러
    private void OnEnemyDied(EnemyDiedEvent eventMessage)
    {
        if (eventMessage.rewardGold > 0)
        {
            GetGold(eventMessage.rewardGold);
        }
    }

#endregion

#region 재화 획득/차감 매서드 모음

    // 몬스터 랭크별 재화 일괄 수급 연산
    private void AddCurrency()
    {
    }

    // 골드 수급 및 변경 이벤트 발행 처리
    public void GetGold(long gold)
    {
        Gold += gold;
        OnGoldChange?.Invoke(Gold);
    }

    public void TestGetGold()
    {
        Gold += 1000000;
        OnGoldChange?.Invoke(Gold);
    }

    // 골드 소모 검증 및 안전 차감 처리
    public bool TrySpendGold(long gold)
    {
        if (Gold < gold) return false;
        Gold -= gold;
        OnGoldChange?.Invoke(Gold);
        return true;
    }

    // 다이아 수급 및 변경 이벤트 발행 처리
    public void GetDiamond(int diamond)
    {
        Diamond += diamond;
        OnDiamondChange?.Invoke(Diamond);
    }

    // 다이아 소모 검증 및 안전 차감 처리
    public bool TrySpendDiamond(int diamond)
    {
        if (Diamond < diamond) return false;
        Diamond -= diamond;
        OnDiamondChange?.Invoke(Diamond);
        return true;
    }

    // 소환 코스트(DP) 수급 및 변경 이벤트 발행 처리 (MaxDpCost 상한 제한 처리)
    // 이유: DpCost가 MaxDpCost를 초과하지 않도록 Clamp 처리하고 꽉 차면 리젠을 정지함
    public void GetDpCost(int dpCost)
    {
        DpCost = Mathf.Min(DpCost + dpCost, MaxDpCost);
        OnDpCostChange?.Invoke(DpCost);

        if (DpCost >= MaxDpCost)
        {
            _isPaused = true;
        }
    }

    // 소환 코스트(DP) 소모 검증 및 안전 차감 처리
    // 이유: DP 소모 시 MaxDpCost 미만이 되므로 _isPaused = false로 설정하여 자동 리젠을 재개함
    public bool TrySpendDpCost(int dpCost)
    {
        if (DpCost < dpCost) return false;

        DpCost -= dpCost;
        OnDpCostChange?.Invoke(DpCost);

        if (DpCost < MaxDpCost)
        {
            _isPaused = false;
        }

        return true;
    }
    
#endregion

#region 재화 관련 업그레이드 메서드 모음

    // 골드 보너스 수량 업그레이드 연산
    public void GoldBonusUpgrade(int level)
    {
        for (int i = 0; i < level; i++)
        {
            goldBonus += goldBonusIncrease;
        }
    }

    // 골드 수급 배율 업그레이드 연산
    public void GoldMagnificationUpgrade(int level)
    {
        for (int i = 0; i < level; i++)
        {
            goldMagnification += goldMagnificationIncrease;
        }
    }

    // 다이아 보너스 수량 업그레이드 연산
    public void DiamondBonusUpgrade(int level)
    {
        for (int i = 0; i < level; i++)
        {
            diamondBonus += diamondBonusIncrease;
        }
    }

    // 다이아 수급 배율 업그레이드 연산
    public void DiamondMagnificationUpgrade(int level)
    {
        for (int i = 0; i < level; i++)
        {
            diamondMagnification += diamondMagnificationIncrease;
        }
    }

    // 소환 코스트(DP) 보너스 업그레이드 연산
    public void DpCostBonusUpgrade(int level)
    {
        for (int i = 0; i < level; i++)
        {
            dpCostBonus += dpCostBonusIncrease;
        }
    }

    // 소환 코스트(DP) 수급 배율 업그레이드 연산 (MaxDpCost 증가)
    // 이유: 최대 코스트가 확장되었으므로 DpCost < MaxDpCost일 때 리젠을 즉시 재개함
    public void MaxDpCostUpgrade(int level)
    {
        for (int i = 0; i < level; i++)
        {
            MaxDpCost += maxDpCostIncrease;
        }

        if (DpCost < MaxDpCost)
        {
            _isPaused = false;
        }
    }

#endregion

#region 계산 메서드

    // 오프라인 방치 24시간 재화 수급 보상 연산
    private void CalculateOfflineReward()
    {
        // 저장 시스템 시각 기록 기반 오프라인 보상 계산 연산
    }

    // 게임 속도 변경 이벤트 수신 시 DP 회복 속도 변환 연산
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
    
    // 비동기(UniTask) 기반 DP 코스트 및 초당 기본 재화 자동 지속 회복 연산
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
                    GetDpCost(dpCost);
                    timer = 0;
                }
            }
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }
    }
     
#endregion

#region 재화 저장 관리

    // 보유 재화 데이터 세이브 객체 저장 연산
    private void OnSave(DataSaveEvent evt)
    {
        evt.saveData.currency.gold    = Gold;
        evt.saveData.currency.diamond = Diamond;
    }

    // 세이브 데이터 기반 보유 재화 복원 및 UI 갱신 이벤트 발행 처리
    private void OnLoad(DataLoadEvent evt)
    {
        Gold    = evt.saveData.currency.gold;
        Diamond = evt.saveData.currency.diamond;
        DpCost  = 5; 
        OnGoldChange?.Invoke(Gold);
        OnDiamondChange?.Invoke(Diamond);
        OnDpCostChange?.Invoke(DpCost);
    }

    // 재화 데이터 초기화 및 UI 갱신 이벤트 발행 처리
    private void OnReset(DataResetEvent evt)
    {
        Gold    = baseGold;
        Diamond = baseDiamond;
        DpCost  = baseDpCost;
        OnGoldChange?.Invoke(Gold);
        OnDiamondChange?.Invoke(Diamond);
        OnDpCostChange?.Invoke(DpCost);
    }

#endregion
}
