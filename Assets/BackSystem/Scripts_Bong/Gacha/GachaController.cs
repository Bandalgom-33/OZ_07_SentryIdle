using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using UnityEngine;

// 가챠 추첨 프로세스, 다이아 차감, 6단계 등급 추첨 및 유저 캐릭터 보유(IsOwned) 여부 업데이트 총괄 컨트롤러
[RequireComponent(typeof(GachaDataProvider))]
public class GachaController : SingletonBase<GachaController>
{
    #region 직렬화 필드 (인스펙터 바인딩)

    [Header("가챠 비용 및 천장 설정")]
    [SerializeField] private int singleDrawCost = 300;   // 1회 가챠 소모 다이아 수량
    [SerializeField] private int tenDrawCost = 3000;     // 10회 가챠 소모 다이아 수량
    [SerializeField] private int pityThreshold = 100;    // 6성 확정 천장 횟수 (100회)

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

    protected override void Awake()
    {
        base.Awake();
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

    #endregion

    #region 가챠 실행 핵심 메서드

    // 1회 또는 10회 가챠 실행 로직
    // 이유: 재화(다이아) 소모 검증 후 6단계 등급 추첨, 천장 스택 계산 및 보유 여부(IsOwned) 업데이트를 일괄 처리함
    public List<IGachaRewardItem> ExecuteGacha(int drawCount)
    {
        int requiredCost = (drawCount >= 10) ? tenDrawCost : singleDrawCost * drawCount;

        // 재화(다이아) 차감 검증
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

            // 1. 6단계 등급(OneStar~SixStar) 추첨
            UnitGrade rolledGrade = _dataProvider.RollGrade(isPity);

            // 2. 등급 풀에서 무작위 유닛 1종 획득
            IGachaRewardItem rewardItem = _dataProvider.GetRandomItemByGrade(rolledGrade);

            // 3. 천장 달성 시 스택 초기화
            if (isPity || rolledGrade == UnitGrade.SixStar)
            {
                CurrentPityStack = 0;
            }

            if (rewardItem != null)
            {
                // 4. 유저 보유 여부(IsOwned) 판별 및 갱신
                bool alreadyOwned = _ownedUnitIds.Contains(rewardItem.RewardId);
                rewardItem.IsOwned = alreadyOwned;

                if (!alreadyOwned)
                {
                    // 최초 획득 시 보유 목록에 등록
                    _ownedUnitIds.Add(rewardItem.RewardId);
                }

                results.Add(rewardItem);
            }
        }

        // 가챠 뽑기 완료 이벤트 전파 (UI 연동)
        EventBus.Publish(new GachaDrawCompletedEvent(results, CurrentPityStack));

        return results;
    }

    #endregion

    #region 데이터 세이브/로드 연동

    private void OnSave(DataSaveEvent evt)
    {
        evt.saveData.gacha.pityStackCount = CurrentPityStack;

        // 보유 유닛 목록 보존
        evt.saveData.unitDeck.ownedUnits.Clear();
        foreach (string unitId in _ownedUnitIds)
        {
            evt.saveData.unitDeck.ownedUnits.Add(new UnitSaveData
            {
                unitId = unitId.GetHashCode(), // 임시 해시 매핑 또는 String ID 저장 지원
                level = 1
            });
        }
    }

    private void OnLoad(DataLoadEvent evt)
    {
        CurrentPityStack = evt.saveData.gacha.pityStackCount;
        _ownedUnitIds.Clear();

        if (evt.saveData.unitDeck.ownedUnits != null)
        {
            foreach (var unitSave in evt.saveData.unitDeck.ownedUnits)
            {
                // 보유 세이브 데이터를 기반으로 습득 유닛 ID 복원
                _ownedUnitIds.Add(unitSave.unitId.ToString());
            }
        }
    }

    private void OnReset(DataResetEvent evt)
    {
        CurrentPityStack = 0;
        _ownedUnitIds.Clear();
    }

    #endregion
}
