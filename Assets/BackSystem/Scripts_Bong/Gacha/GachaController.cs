using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using UnityEngine;

// 가챠 뽑기 트랜잭션, 천장 연산기 연동, 유닛 돌파 처리 및 세이브 동기화를 총괄하는 컨트롤러
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
    private PityEvaluator _pityEvaluator;
    private readonly HashSet<string> _ownedUnitIds = new HashSet<string>();
    private readonly List<UnitSaveData> _cachedOwnedUnits = new List<UnitSaveData>();

    // 현재 누적 천장 스택 프로퍼티
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

    // 세이브 / 로드 이벤트 버스 구독 등록
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

    // 가챠 뽑기 실행 (1회 또는 10회)
    public List<IGachaRewardItem> ExecuteGacha(int drawCount)
    {
        int requiredCost = (drawCount >= 10) ? tenDrawCost : singleDrawCost * drawCount;

        // 1. 다이아 재화 차감 검증
        if (!CurrencyManager.Instance.TrySpendDiamond(requiredCost))
        {
            Debug.LogWarning("[GachaController] 다이아가 부족하여 가챠를 실행할 수 없습니다.");
            return null;
        }

        List<IGachaRewardItem> results = new List<IGachaRewardItem>();

        // 2. 뽑기 횟수만큼 반복 추첨
        for (int i = 0; i < drawCount; i++)
        {
            // 천장 스택 1 증가
            _pityEvaluator.IncreasePity();

            // 현재 스택에 따른 6성 확률 계산 (1~49회: 0.1%, 50~99회: 10%, 100회: 100%)
            float sixStarProb = _pityEvaluator.GetTopGradeProbability();

            // [1단계: 성 등급 결정]
            UnitGrade rolledGrade = _dataProvider.RollGrade(sixStarProb);

            // [2단계: 해당 등급 유닛 무작위 추첨]
            IGachaRewardItem rewardItem = _dataProvider.GetRandomItemByGrade(rolledGrade);

            // 6성 캐릭터 획득 시 천장 스택 0 초기화
            if (rolledGrade == UnitGrade.SixStar)
            {
                _pityEvaluator.ResetPity();
            }

            if (rewardItem != null)
            {
                // [신규/돌파/풀돌 상태 판정 및 세이브 캐시 갱신]
                BreakthroughProcessor.ProcessGachaUnit(ref rewardItem, _cachedOwnedUnits, _ownedUnitIds);
                results.Add(rewardItem);
            }
        }

        // 가챠 완료 이벤트 발행 (UI 및 타 시스템 실시간 연동)
        EventBus.Publish(new GachaDrawCompletedEvent(results, CurrentPityStack));

        // 가챠 직후 자동 세이브 트리거 실행
        SaveManager.Instance.SaveGameData();

        return results;
    }

    #endregion

    #region 데이터 세이브/로드 연동

    // 가챠 및 유닛 보유/돌파 세이브 데이터 저장
    private void OnSave(DataSaveEvent evt)
    {
        if (evt.saveData == null) return;

        evt.saveData.gacha.pityStackCount = CurrentPityStack;
        evt.saveData.unitDeck.ownedUnits = new List<UnitSaveData>(_cachedOwnedUnits);
    }

    // 가챠 및 유닛 보유/돌파 세이브 데이터 로드
    private void OnLoad(DataLoadEvent evt)
    {
        if (evt.saveData == null) return;

        int loadedPity = evt.saveData.gacha.pityStackCount;
        if (_pityEvaluator != null)
        {
            _pityEvaluator.SetPityStack(loadedPity);
        }
        else
        {
            _pityEvaluator = new PityEvaluator(loadedPity);
        }

        _ownedUnitIds.Clear();
        _cachedOwnedUnits.Clear();

        if (evt.saveData.unitDeck.ownedUnits != null)
        {
            foreach (var unitSave in evt.saveData.unitDeck.ownedUnits)
            {
                if (unitSave == null) continue;

                _cachedOwnedUnits.Add(unitSave);
                string formattedId = $"UNIT_{unitSave.unitId:D4}";
                _ownedUnitIds.Add(formattedId);
                _ownedUnitIds.Add(unitSave.unitId.ToString());
            }
        }
    }

    // 가챠 데이터 초기화 연산
    private void OnReset(DataResetEvent evt)
    {
        if (_pityEvaluator != null)
        {
            _pityEvaluator.ResetPity();
        }
        _ownedUnitIds.Clear();
        _cachedOwnedUnits.Clear();
    }

    #endregion
}

