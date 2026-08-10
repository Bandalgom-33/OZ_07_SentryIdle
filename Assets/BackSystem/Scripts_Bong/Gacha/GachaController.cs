using System.Collections.Generic;
using UnityEngine;

// 가챠 추첨 프로세스, 다이아 차감 및 세이브 연동 총괄 컨트롤러 싱글톤
[RequireComponent(typeof(GachaDataProvider))]
public class GachaController : SingletonBase<GachaController>
{
    #region SerializeFields (인스펙터 바인딩)

    [Header("가챠 비용 설정")]
    [SerializeField] private int singleDrawCost = 300;   // 1회 가챠 소모 다이아 수량
    [SerializeField] private int tenDrawCost = 3000;     // 10회 가챠 소모 다이아 수량

    #endregion

    #region 비공개 필드 및 프로퍼티

    private PityEvaluator _pityEvaluator;
    private GachaDataProvider _dataProvider;

    public int CurrentPityStack => _pityEvaluator != null ? _pityEvaluator.CurrentPityStack : 0;
    public int SingleDrawCost => singleDrawCost;
    public int TenDrawCost => tenDrawCost;

    #endregion

    #region 라이프 사이클

    // 싱글톤 패턴 초기화 및 컴포넌트 참조 연동
    protected override void Awake()
    {
        base.Awake();
        _pityEvaluator = new PityEvaluator();
        _dataProvider = GetComponent<GachaDataProvider>();
    }

    // 중앙 EventBus 세이브/로드 이벤트 구독 등록
    private void OnEnable()
    {
        EventBus.Subscribe<DataSaveEvent>(OnSave);
        EventBus.Subscribe<DataLoadEvent>(OnLoad);
        EventBus.Subscribe<DataResetEvent>(OnReset);
    }

    // 중앙 EventBus 이벤트 구독 해제
    private void OnDisable()
    {
        EventBus.Unsubscribe<DataSaveEvent>(OnSave);
        EventBus.Unsubscribe<DataLoadEvent>(OnLoad);
        EventBus.Unsubscribe<DataResetEvent>(OnReset);
    }

    #endregion

    #region 가챠 핵심 실행 메서드

    // 다이아 소유량 기반 가챠 진행 가능 여부 검증
    public bool CanAffordGacha(int drawCount)
    {
        int requiredCost = (drawCount >= 10) ? tenDrawCost : singleDrawCost * drawCount;
        return CurrencyManager.Instance != null && CurrencyManager.Instance.HasDiamond(requiredCost);
    }

    // 다이아 차감, 등급/캐릭터 추첨, 천장 갱신 및 완료 이벤트 발행 연산
    public List<IGachaRewardItem> ExecuteGacha(int drawCount)
    {
        int requiredCost = (drawCount >= 10) ? tenDrawCost : singleDrawCost * drawCount;

        // 1. 다이아 소유 확인 및 차감 검증
        if (CurrencyManager.Instance == null || !CurrencyManager.Instance.TrySpendDiamond(requiredCost))
        {
            Debug.LogWarning($"[GachaController] 다이아가 부족하여 가챠를 진행할 수 없습니다. (필요 다이아: {requiredCost})");
            return null;
        }

        List<IGachaRewardItem> results = new List<IGachaRewardItem>();

        // 2. 가챠 횟수만큼 확률 추첨 연산 실행
        for (int i = 0; i < drawCount; i++)
        {
            // 구간별 천장 확률 산출
            float ssrProbability = _pityEvaluator.GetTopGradeProbability();
            bool isSSR = Random.value <= ssrProbability;

            // 희귀도 등급 결정 및 무작위 캐릭터 추출
            TestRarityGrade rolledGrade = _dataProvider.RollGrade(isSSR);
            IGachaRewardItem rewardItem = _dataProvider.GetRandomItemByGrade(rolledGrade);
            results.Add(rewardItem);

            // SSR 당첨 여부에 따른 천장 스택 리셋 또는 1 증가 처리
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

        // 3. 가챠 완료 이벤트 중앙 EventBus 발행
        EventBus.Publish(new GachaDrawCompletedEvent(results, _pityEvaluator.CurrentPityStack));

        return results;
    }

    #endregion

    #region 세이브 및 데이터 복원 연동

    // 세이브 데이터에 현재 천장 스택 수치 기록 연산
    private void OnSave(DataSaveEvent evt)
    {
        if (evt.saveData != null && evt.saveData.gacha != null)
        {
            evt.saveData.gacha.pityStackCount = _pityEvaluator.CurrentPityStack;
        }
    }

    // 로드된 세이브 데이터 기반 천장 스택 수치 복원 연산
    private void OnLoad(DataLoadEvent evt)
    {
        if (evt.saveData != null && evt.saveData.gacha != null)
        {
            _pityEvaluator.SetPityStack(evt.saveData.gacha.pityStackCount);
        }
    }

    // 데이터 리셋 이벤트 수신 시 천장 스택 초기화 처리
    private void OnReset(DataResetEvent evt)
    {
        _pityEvaluator.ResetPity();
    }

    #endregion
}
