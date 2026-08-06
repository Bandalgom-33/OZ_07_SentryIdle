using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Manager;
using UnityEngine;

public class CurrencyManager : SingletonBase<CurrencyManager>
{
#region 노출 변수 모음
    
    [Header("재화 베이스 정보")]
    [SerializeField] private int baseGold = 10;
    [SerializeField] private int baseDiamond = 3;
    [SerializeField] private int baseDpCost = 1;
    [SerializeField] private long goldBonus ;
    [SerializeField] private int diamondBonus ;
    [SerializeField] private int dpCostBonus ;
    [SerializeField] private float goldMagnification = 1.0f;
    [SerializeField] private float diamondMagnification = 1.0f;
    [SerializeField] private float dpCostMagnification = 1.0f;
    [SerializeField] private float dpCostRegenTime = 1.0f;
    [Space(5f), Header("업그레이드 세팅 값")]
    [SerializeField] private int goldBonusIncrease = 10;
    [SerializeField] private float goldMagnificationIncrease = 0.1f;
    [SerializeField] private int diamondBonusIncrease = 3;
    [SerializeField] private float diamondMagnificationIncrease = 0.1f;
    [SerializeField] private int dpCostBonusIncrease = 1;
    [SerializeField] private float dpCostMagnificationIncrease = 1f;
    
#endregion

#region 프로퍼티
    
    public long Gold { get; private set; }
    public int Diamond { get; private set; }
    public int DpCost { get; private set; }
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

#endregion

#region 라이프 사이클

    protected override void Awake()
    {
        base.Awake();
        LoadCurrency();
    }

    private void OnEnable()
    {
        GameManager.OnGameSpeedChange += GameSpeedChange;
    }

    private void Start()
    {
        _currentRegenTime = dpCostRegenTime;
        //StartCoroutine(RegenDpCostCo());
        RegenDpCost(this.GetCancellationTokenOnDestroy()).Forget();
    }

    private void OnDisable()
    {
        SaveCurrency();
        GameManager.OnGameSpeedChange -= GameSpeedChange;
    }

#endregion

#region 재화 획득/차감 매서드 모음

    //몬스터 랭크에 맞춰서 재화 일괄 획득
    private void AddCurrency()
    {
    }
    //골드 획득
    public void GetGold(long gold)
    {
        
        Gold += gold;
        OnGoldChange?.Invoke(Gold);
        
    }
    //골드 소모
    public bool TrySpendGold(long gold)
    {
        if (Gold < gold) return false;
        Gold -= gold;
        OnGoldChange?.Invoke(Gold);
        return true;
    }
    //다이아 획득
    public void GetDiamond(int diamond)
    {
        Diamond += diamond;
        OnDiamondChange?.Invoke(Diamond);
    }
    //다이아 소모
    public bool TrySpendDiamond(int diamond)
    {
        if (Diamond < diamond) return false;
        Diamond -= diamond;
        OnDiamondChange?.Invoke(Diamond);
        return true;
    }
    //소환 코스트 획득
    public void GetDpCost(int dpCost)
    {
        DpCost += dpCost;
        OnDpCostChange?.Invoke(DpCost);
    }
    //소환 코스트 소모
    public bool TrySpendDpCost(int dpCost)
    {
        if (DpCost < dpCost) return false;
        DpCost -= dpCost;
        OnDpCostChange?.Invoke(DpCost);
        return true;
    }
    
#endregion

#region 재화 관련 업그레이드 메서드 모음



    // 골드 보너스 업그레이드
    public void GoldBonusUpgrade(int level)
    {
        for (int i = 0; i < level; i++)
        {
            goldBonus += goldBonusIncrease;
        }
    }
    // 골드 배율 업그레이드 
    public void GoldMagnificationUpgrade(int level)
    {
        for (int i = 0; i < level; i++)
        {
            goldMagnification += goldMagnificationIncrease;
        }
    }
    // 다이아 보너스 업그레이드
    public void DiamondBonusUpgrade(int level)
    {
        for (int i = 0; i < level; i++)
        {
            diamondBonus += diamondBonusIncrease;
        }
    }
    // 다이아 배율 업그레이드 
    public void DiamondMagnificationUpgrade(int level)
    {
        for (int i = 0; i < level; i++)
        {
            diamondMagnification += diamondMagnificationIncrease;
        }
    }
    //소환 코스트 보너스 업그레이드
    public void DpCostBonusUpgrade(int level)
    {
        for (int i = 0; i < level; i++)
        {
            dpCostBonus +=  dpCostBonusIncrease;
        }
    }
    // 소환 코스트 배율 업그레이드
    public void DpCostMagnificationUpgrade(int level)
    {
        for (int i = 0; i < level; i++)
        {
            dpCostMagnification += dpCostMagnificationIncrease;
        }
    }

#endregion

#region 계산 메서드

    private void CalculateOfflineReward()
    {
        // 저장 시스템에서 시간 저장 후 계산 
    }

    // 게임 속도 변경시 Dp코스트 리젠 속도 변경
    private void GameSpeedChange()
    {
        if (Time.timeScale == 0)
        {
            _isPaused = true;
        }
        else
        {
            _isPaused = false;
            _currentRegenTime = dpCostRegenTime/ Time.timeScale;
        }
    }
    // Dp코스트 리젠 코루틴
    /*IEnumerator RegenDpCostCo()
    {
        float timer = 0;
        while (true)
        {
            if (!_isPaused)
            {
                timer += Time.deltaTime;
                float sliderValue = Mathf.Lerp(0, 1, timer / _currentRegenTime);
                if (sliderValue >= 1)
                {
                    int dpCost = (int)((baseDpCost + dpCostBonus) * dpCostMagnification);
                    GetDpCost(dpCost);
                    long gold = (long)((baseGold + goldBonus) * goldMagnification);
                    GetGold(gold);
                    int diamond = (int)((baseDiamond + diamondBonus) * diamondMagnification);
                    GetDiamond(diamond);
                    timer = 0;
                }
            }
            yield return null;
        }
    }*/
    // DpCost 비동기 리젠
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
                if (sliderValue >= 1)
                {
                    int dpCost = (int)((baseDpCost + dpCostBonus) * dpCostMagnification);
                    GetDpCost(dpCost);
                    long gold = (long)((baseGold + goldBonus) * goldMagnification);
                    GetGold(gold);
                    int diamond = (int)((baseDiamond + diamondBonus) * diamondMagnification);
                    GetDiamond(diamond);
                    timer = 0;
                }
            }
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }
    }
     
    
#endregion

#region 재화 저장 관리

    private void SaveCurrency()
    {
        
    }

    private void LoadCurrency()
    {
        Gold = 0;
        Diamond = 0;
        DpCost = 5;
    }


#endregion

}
