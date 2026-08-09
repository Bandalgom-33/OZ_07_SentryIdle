using System.Collections.Generic;
using UnityEngine;

// 가챠 시스템 총괄 컨트롤러 싱글톤
[RequireComponent(typeof(GachaDataProvider))]
public class GachaController : SingletonBase<GachaController>
{
    [Header("가챠 비용 설정")]
    [SerializeField] private int singleDrawCost = 300;   // 1회 뽑기 다이아 비용
    [SerializeField] private int tenDrawCost = 3000;     // 10회 뽑기 다이아 비용

    //가챠 스택 관리
    private PityEvaluator _pityEvaluator;
    // 가챠 풀 관리 
    private GachaDataProvider _dataProvider;

    public int CurrentPityStack => _pityEvaluator != null ? _pityEvaluator.CurrentPityStack : 0;
    public int SingleDrawCost => singleDrawCost;
    public int TenDrawCost => tenDrawCost;

    protected override void Awake()
    {
        base.Awake();
        _pityEvaluator = new PityEvaluator();
        _dataProvider = GetComponent<GachaDataProvider>();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<DataSaveEvent>(OnSave);
        EventBus.Subscribe<DataLoadEvent>(OnLoad);
        EventBus.Subscribe<DataResetEvent>(OnReset);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<DataSaveEvent>(OnSave);
        EventBus.Unsubscribe<DataLoadEvent>(OnLoad);
        EventBus.Unsubscribe<DataResetEvent>(OnReset);
    }

    // 가챠 뽑기 가능 여부 확인
    public bool CanAffordGacha(int drawCount)
    {
        int requiredCost = (drawCount >= 10) ? tenDrawCost : singleDrawCost * drawCount;
        return CurrencyManager.Instance != null && CurrencyManager.Instance.HasDiamond(requiredCost);
    }

    // 가챠 뽑기 
    public List<IGachaRewardItem> ExecuteGacha(int drawCount)
    {
        int requiredCost = (drawCount >= 10) ? tenDrawCost : singleDrawCost * drawCount;

        
        if (CurrencyManager.Instance == null || !CurrencyManager.Instance.TrySpendDiamond(requiredCost))
        {
            Debug.LogWarning($"[GachaController] 다이아가 부족하여 가챠를 진행할 수 없습니다. (필요 다이아: {requiredCost})");
            return null;
        }

        List<IGachaRewardItem> results = new List<IGachaRewardItem>();

       // 가챠 연산
        for (int i = 0; i < drawCount; i++)
        {
            float ssrProbability = _pityEvaluator.GetTopGradeProbability();
            bool isSSR = Random.value <= ssrProbability;
            
            TestRarityGrade rolledGrade = _dataProvider.RollGrade(isSSR);
            IGachaRewardItem rewardItem = _dataProvider.GetRandomItemByGrade(rolledGrade);
            results.Add(rewardItem);
            
            if (rolledGrade == TestRarityGrade.GradeSSR)
            {
                Debug.Log($"<color=gold>[Gacha!] 최고 등급(SSR) 획득! [{rewardItem.DisplayName}] (천장 스택 리셋: 이전 {_pityEvaluator.CurrentPityStack}회)</color>");
                _pityEvaluator.ResetPity();
            }
            else
            {
                _pityEvaluator.IncreasePity();
            }
        }

        // 3. 가챠 완료 이벤트 발행
        EventBus.Publish(new GachaDrawCompletedEvent(results, _pityEvaluator.CurrentPityStack));

        return results;
    }

    #region 세이브 및 데이터 복원 연동

    private void OnSave(DataSaveEvent evt)
    {
        if (evt.saveData != null && evt.saveData.gacha != null)
        {
            evt.saveData.gacha.pityStackCount = _pityEvaluator.CurrentPityStack;
        }
    }

    private void OnLoad(DataLoadEvent evt)
    {
        if (evt.saveData != null && evt.saveData.gacha != null)
        {
            _pityEvaluator.SetPityStack(evt.saveData.gacha.pityStackCount);
        }
    }

    private void OnReset(DataResetEvent evt)
    {
        _pityEvaluator.ResetPity();
    }

    #endregion
}
