using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using UnityEngine;

// 가챠 뽑기 실행, 천장 연산기 연동 및 보상 지급 이벤트를 총괄하는 컨트롤러
[RequireComponent(typeof(GachaDataProvider))]
public class GachaController : SingletonBase<GachaController>
{
    #region 직렬화 필드 (인스펙터 바인딩)

    [Header("--- 가챠 비용 및 천장 설정 ---")]
    [Tooltip("1회 가챠 소모 다이아 수량")]
    [SerializeField] private int singleDrawCost = 300;

    [Tooltip("10회 가챠 소모 다이아 수량")]
    [SerializeField] private int tenDrawCost = 3000;

    [Tooltip("6성 확정 천장 횟수 (100회)")]
    [SerializeField] private int pityThreshold = 100;

    #endregion

    #region 비공개 필드 및 프로퍼티

    private GachaDataProvider _dataProvider;
    private PityEvaluator _pityEvaluator;

    public int CurrentPityStack => _pityEvaluator != null ? _pityEvaluator.CurrentPityStack : 0;
    public int SingleDrawCost => singleDrawCost;
    public int TenDrawCost => tenDrawCost;
    public int PityThreshold => pityThreshold;

    #endregion

    #region 라이프 사이클

    // 인스턴스 초기화 및 천장 연산기 생성
    protected override void Awake()
    {
        base.Awake();
        _dataProvider = GetComponent<GachaDataProvider>();
        _pityEvaluator = new PityEvaluator(0);
    }

    // 이벤트 버스 구독 등록
    private void OnEnable()
    {
        EventBus.Subscribe<DataSaveEvent>(OnSave);
        EventBus.Subscribe<DataLoadEvent>(OnLoad);
        EventBus.Subscribe<DataResetEvent>(OnReset);
    }

    // 이벤트 버스 구독 해제
    private void OnDisable()
    {
        EventBus.Unsubscribe<DataSaveEvent>(OnSave);
        EventBus.Unsubscribe<DataLoadEvent>(OnLoad);
        EventBus.Unsubscribe<DataResetEvent>(OnReset);
    }

    #endregion

    #region 가챠 실행 핵심 메서드

    // 가챠 뽑기 실행 연산 (1회 또는 10회)
    public List<IGachaRewardItem> ExecuteGacha(int drawCount)
    {
        int requiredCost = (drawCount >= 10) ? tenDrawCost : singleDrawCost * drawCount;

        if (!CurrencyManager.Instance.TrySpendDiamond(requiredCost))
        {
            Debug.LogWarning("[GachaController] 다이아가 부족하여 가챠를 실행할 수 없습니다.");
            return null;
        }

        List<IGachaRewardItem> results = new List<IGachaRewardItem>();

        for (int i = 0; i < drawCount; i++)
        {
            _pityEvaluator.IncreasePity();

            float sixStarProb = _pityEvaluator.GetTopGradeProbability();
            UnitGrade rolledGrade = _dataProvider.RollGrade(sixStarProb);
            IGachaRewardItem rewardItem = _dataProvider.GetRandomItemByGrade(rolledGrade);

            if (rolledGrade == UnitGrade.SixStar)
            {
                _pityEvaluator.ResetPity();
            }

            if (rewardItem != null)
            {
                BreakthroughProcessor.ProcessGachaUnit(ref rewardItem);
                results.Add(rewardItem);
            }
        }

        EventBus.Publish(new GachaDrawCompletedEvent(results, CurrentPityStack));
        SaveManager.Instance.SaveGameData();

        return results;
    }

    #endregion

    #region 데이터 세이브/로드 연동

    // 가챠 천장 세이브 데이터 저장 처리
    private void OnSave(DataSaveEvent evt)
    {
        if (evt.saveData == null) return;
        if (evt.saveData.gacha == null)
        {
            evt.saveData.gacha = new GachaData();
        }

        evt.saveData.gacha.pityStackCount = CurrentPityStack;
    }

    // 가챠 천장 세이브 데이터 로드 처리
    private void OnLoad(DataLoadEvent evt)
    {
        if (evt.saveData == null || evt.saveData.gacha == null) return;

        int loadedPity = evt.saveData.gacha.pityStackCount;
        if (_pityEvaluator != null)
        {
            _pityEvaluator.SetPityStack(loadedPity);
        }
        else
        {
            _pityEvaluator = new PityEvaluator(loadedPity);
        }
    }

    // 가챠 데이터 초기화 처리
    private void OnReset(DataResetEvent evt)
    {
        if (_pityEvaluator != null)
        {
            _pityEvaluator.ResetPity();
        }
    }

    #endregion
}
