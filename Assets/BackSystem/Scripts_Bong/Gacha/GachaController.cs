using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using UnityEngine;

[RequireComponent(typeof(GachaDataProvider))]
public class GachaController : SingletonBase<GachaController>
{
    #region 직렬화 필드 (인스펙터 바인딩)

    [Header("가챠 비용 및 천장 설정")]
    [Tooltip("1회 가챠 소모 다이아 수량")]
    [SerializeField] private int singleDrawCost = 300;

    [Tooltip("10회 가챠 소모 다이아 수량")]
    [SerializeField] private int tenDrawCost = 3000;

    [Tooltip("6성 확정 천장 횟수 (100회)")]
    [SerializeField] private int pityThreshold = 100;

    #endregion

    #region 비공개 필드 및 프로퍼티

    private GachaDataProvider _dataProvider;
    private readonly HashSet<string> _ownedUnitIds = new HashSet<string>();

    public int CurrentPityStack { get; private set; }
    public int SingleDrawCost => singleDrawCost;
    public int TenDrawCost => tenDrawCost;
    public int PityThreshold => pityThreshold;

    #endregion

    #region 라이프 사이클

    // 인스턴스 초기화 및 컴포넌트 참조 연산
    protected override void Awake()
    {
        base.Awake();
        _dataProvider = GetComponent<GachaDataProvider>();
    }

    // 이벤트 버스 구독 연산
    private void OnEnable()
    {
        EventBus.Subscribe<DataSaveEvent>(OnSave);
        EventBus.Subscribe<DataLoadEvent>(OnLoad);
        EventBus.Subscribe<DataResetEvent>(OnReset);
    }

    // 이벤트 버스 구독 해제 연산
    private void OnDisable()
    {
        EventBus.Unsubscribe<DataSaveEvent>(OnSave);
        EventBus.Unsubscribe<DataLoadEvent>(OnLoad);
        EventBus.Unsubscribe<DataResetEvent>(OnReset);
    }

    #endregion

    #region 가챠 실행 핵심 메서드

    // 가챠 뽑기 실행 및 결과 반환 연산
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
            CurrentPityStack++;
            bool isPity = (CurrentPityStack >= pityThreshold);

            UnitGrade rolledGrade = _dataProvider.RollGrade(isPity);

            IGachaRewardItem rewardItem = _dataProvider.GetRandomItemByGrade(rolledGrade);

            if (isPity || rolledGrade == UnitGrade.SixStar)
            {
                CurrentPityStack = 0;
            }

            if (rewardItem != null)
            {
                bool alreadyOwned = _ownedUnitIds.Contains(rewardItem.RewardId);
                rewardItem.IsOwned = alreadyOwned;

                if (!alreadyOwned)
                {
                    _ownedUnitIds.Add(rewardItem.RewardId);
                }

                results.Add(rewardItem);
            }
        }

        EventBus.Publish(new GachaDrawCompletedEvent(results, CurrentPityStack));

        return results;
    }

    #endregion

    #region 데이터 세이브/로드 연동

    // 가챠 관련 세이브 데이터 저장 연산
    private void OnSave(DataSaveEvent evt)
    {
        evt.saveData.gacha.pityStackCount = CurrentPityStack;
        evt.saveData.unitDeck.ownedUnits.Clear();

        foreach (string unitId in _ownedUnitIds)
        {
            if (int.TryParse(unitId.Replace("UNIT_", ""), out int parsedId))
            {
                evt.saveData.unitDeck.ownedUnits.Add(new UnitSaveData
                {
                    unitId = parsedId,
                    level = 1,
                    breakThroughStep = 0
                });
            }
            else
            {
                evt.saveData.unitDeck.ownedUnits.Add(new UnitSaveData
                {
                    unitId = unitId.GetHashCode(),
                    level = 1,
                    breakThroughStep = 0
                });
            }
        }
    }

    // 가챠 관련 세이브 데이터 로드 연산
    private void OnLoad(DataLoadEvent evt)
    {
        CurrentPityStack = evt.saveData.gacha.pityStackCount;
        _ownedUnitIds.Clear();

        if (evt.saveData.unitDeck.ownedUnits != null)
        {
            foreach (var unitSave in evt.saveData.unitDeck.ownedUnits)
            {
                if (unitSave == null) continue;
                string formattedId = $"UNIT_{unitSave.unitId:D4}";
                _ownedUnitIds.Add(formattedId);
                _ownedUnitIds.Add(unitSave.unitId.ToString());
            }
        }
    }

    // 가챠 진행 데이터 초기화 연산
    private void OnReset(DataResetEvent evt)
    {
        CurrentPityStack = 0;
        _ownedUnitIds.Clear();
    }

    #endregion
}
