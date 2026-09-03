using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using UnityEngine;

[RequireComponent(typeof(GachaDataProvider))]
public class GachaController : SingletonBase<GachaController>
{
    #region 직렬화 필드

    [Header("--- 가챠 설정 에셋 ---")]
    [Tooltip("가챠 비용, 등급별 가중치, 천장 기준값을 관리하는 설정 SO")]
    [SerializeField] private GachaConfigSO gachaConfig;

    #endregion

    #region 비공개 필드 및 프로퍼티

    private GachaDataProvider _dataProvider;
    private PityEvaluator _pityEvaluator;

    private bool _isDrawing = false;
    private bool _isGachaDirty = false;

    private const int MaxLogCount = 100;
    private readonly GachaLogEntry[] _drawLogBuffer = new GachaLogEntry[MaxLogCount];
    private int _logHead = 0;
    private int _logCount = 0;

    public int CurrentPityStack => _pityEvaluator != null ? _pityEvaluator.CurrentPityStack : 0;
    public int SingleDrawCost => gachaConfig != null ? gachaConfig.SingleDrawCost : 300;
    public int TenDrawCost => gachaConfig != null ? gachaConfig.TenDrawCost : 3000;
    public int PityThreshold => gachaConfig != null ? gachaConfig.HardPityThreshold : 100;
    public int SoftPityThreshold => gachaConfig != null ? gachaConfig.SoftPityThreshold : 50;
    public bool IsDrawing => _isDrawing;
    public bool IsGachaDirty => _isGachaDirty;

    public float CurrentSixStarProbability => _pityEvaluator != null ? _pityEvaluator.GetTopGradeProbability() : 0.001f;
    public float CurrentSixStarRatePercent => CurrentSixStarProbability * 100.0f;
    public GachaConfigSO GachaConfig => gachaConfig;

    #endregion

    #region 라이프 사이클

    // 인스턴스 초기화 및 링 버퍼 생성
    protected override void Awake()
    {
        base.Awake();
        _dataProvider = GetComponent<GachaDataProvider>();

        for (int i = 0; i < MaxLogCount; i++)
        {
            _drawLogBuffer[i] = new GachaLogEntry();
        }

        if (gachaConfig == null && _dataProvider != null)
        {
            gachaConfig = _dataProvider.GachaConfig;
        }

        if (gachaConfig == null)
        {
            Debug.LogError("[GachaController] GachaConfigSO 참조가 누락되었습니다! 인스펙터에서 GachaConfigSO를 할당해주세요.");
        }

        _pityEvaluator = new PityEvaluator(gachaConfig, 0);
    }

    // 전역 이벤트 구독 등록
    private void OnEnable()
    {
        EventBus.Subscribe<DataSaveEvent>(OnSave);
        EventBus.Subscribe<DataLoadEvent>(OnLoad);
        EventBus.Subscribe<DataResetEvent>(OnReset);
    }

    // 전역 이벤트 구독 해제
    private void OnDisable()
    {
        EventBus.Unsubscribe<DataSaveEvent>(OnSave);
        EventBus.Unsubscribe<DataLoadEvent>(OnLoad);
        EventBus.Unsubscribe<DataResetEvent>(OnReset);
    }

    #endregion

    #region 가챠 실행 및 확률 조회 연산

    // 현재 스택 기준 전 등급 실시간 확률표(%) 반환
    public Dictionary<UnitGrade, float> GetCurrentGradeProbabilities()
    {
        if (_dataProvider != null)
        {
            return _dataProvider.CalculateGradeProbabilities(CurrentSixStarProbability);
        }
        return new Dictionary<UnitGrade, float>();
    }

    // 가챠 추첨 실행 및 결과 반환
    public List<GachaRewardItem> ExecuteGacha(int drawCount)
    {
        if (drawCount != 1 && drawCount != 10)
        {
            Debug.LogWarning($"[GachaController] 허용되지 않은 가챠 뽑기 수량({drawCount})입니다. 1회 또는 10회만 가능합니다.");
            return null;
        }

        if (_isDrawing)
        {
            Debug.LogWarning("[GachaController] 이미 가챠 연출 또는 뽑기가 진행 중입니다.");
            return null;
        }

        if (gachaConfig == null)
        {
            Debug.LogError("[GachaController] GachaConfigSO 설정이 없어 가챠를 실행할 수 없습니다.");
            return null;
        }

        if (_dataProvider == null || !_dataProvider.IsInitialized)
        {
            Debug.LogError("[GachaController] GachaDataProvider가 정상 초기화되지 않아 가챠를 실행할 수 없습니다.");
            return null;
        }

        if (CurrencyManager.Instance == null)
        {
            Debug.LogError("[GachaController] CurrencyManager 인스턴스를 찾을 수 없습니다.");
            return null;
        }

        int requiredCost = (drawCount >= 10) ? TenDrawCost : SingleDrawCost * drawCount;

        if (!CurrencyManager.Instance.HasDiamond(requiredCost))
        {
            Debug.LogWarning("[GachaController] 보유 다이아가 부족하여 가챠를 실행할 수 없습니다.");
            return null;
        }

        _isDrawing = true;

        List<GachaRewardItem> results = new List<GachaRewardItem>(drawCount);
        string currentTimestamp = System.DateTime.Now.ToString("HH:mm:ss");
        Dictionary<int, UnitSaveData> batchContext = new Dictionary<int, UnitSaveData>();

        for (int i = 0; i < drawCount; i++)
        {
            _pityEvaluator.IncreasePity();

            float sixStarProb = _pityEvaluator.GetTopGradeProbability();
            UnitGrade rolledGrade = _dataProvider.RollGrade(sixStarProb);
            int rolledUnitId = _dataProvider.GetRandomUnitIdByGrade(rolledGrade);

            if (rolledGrade == UnitGrade.SixStar)
            {
                _pityEvaluator.ResetPity();
            }

            if (rolledUnitId > 0)
            {
                UnitDataSO unitSO = _dataProvider.GetUnitData(rolledUnitId);
                GachaRewardItem rewardItem = new GachaRewardItem(unitSO, rolledUnitId);

                BreakthroughProcessor.ProcessGachaUnit(rewardItem, batchContext);
                results.Add(rewardItem);

                AddDrawLogToRingBuffer(rolledUnitId, currentTimestamp);
            }
        }

        if (!CurrencyManager.Instance.TrySpendDiamond(requiredCost))
        {
            Debug.LogError("[GachaController] 추첨 완료 후 다이아 차감에 실패하였습니다!");
            _isDrawing = false;
            return null;
        }

        _isGachaDirty = true;
        _isDrawing = false;

        EventBus.Publish(new GachaDrawCompletedEvent(results, CurrentPityStack));

        return results;
    }

    // 가챠 연출 진행 상태 지정
    public void SetDrawingState(bool isDrawing)
    {
        _isDrawing = isDrawing;
    }

    #endregion

    #region 링 버퍼 로그 관리 연산

    // 링 버퍼에 단일 가챠 이력 추가
    private void AddDrawLogToRingBuffer(int unitId, string timestamp)
    {
        GachaLogEntry entry = _drawLogBuffer[_logHead];
        entry.unitId = unitId;
        entry.timestamp = timestamp;

        _logHead = (_logHead + 1) % MaxLogCount;
        if (_logCount < MaxLogCount)
        {
            _logCount++;
        }
    }

    // 시간 순서(과거 -> 최신)로 정렬된 로그 목록 반환
    public List<GachaLogEntry> GetOrderedDrawLogs()
    {
        List<GachaLogEntry> list = new List<GachaLogEntry>(_logCount);

        if (_logCount < MaxLogCount)
        {
            for (int i = 0; i < _logCount; i++)
            {
                list.Add(_drawLogBuffer[i]);
            }
        }
        else
        {
            for (int i = 0; i < MaxLogCount; i++)
            {
                int index = (_logHead + i) % MaxLogCount;
                list.Add(_drawLogBuffer[index]);
            }
        }

        return list;
    }

    // 링 버퍼 로그 전체 초기화
    public void ClearDrawLogs()
    {
        _logHead = 0;
        _logCount = 0;
    }

    #endregion

    #region 세이브 및 로드 연동

    // 가챠 변경사항 발생 시 저장 요청 발행
    public void SaveIfDirty()
    {
        if (_isGachaDirty)
        {
            EventBus.Publish(new RequestSaveGameEvent(force: false));
            _isGachaDirty = false;
        }
    }

    // 세이브 데이터에 가챠 정보 기록
    private void OnSave(DataSaveEvent evt)
    {
        if (evt.saveData == null) return;
        if (evt.saveData.gacha == null)
        {
            evt.saveData.gacha = new GachaData();
        }

        evt.saveData.gacha.pityStackCount = CurrentPityStack;
        evt.saveData.gacha.drawLogs = GetOrderedDrawLogs();
    }

    // 세이브 데이터로부터 가챠 정보 복원
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
            _pityEvaluator = new PityEvaluator(gachaConfig, loadedPity);
        }

        ClearDrawLogs();
        if (evt.saveData.gacha.drawLogs != null)
        {
            var loadedLogs = evt.saveData.gacha.drawLogs;
            for (int i = 0; i < loadedLogs.Count; i++)
            {
                if (loadedLogs[i] != null)
                {
                    AddDrawLogToRingBuffer(loadedLogs[i].unitId, loadedLogs[i].timestamp);
                }
            }
        }

        _isGachaDirty = false;
    }

    // 가챠 런타임 데이터 초기화
    private void OnReset(DataResetEvent evt)
    {
        if (_pityEvaluator != null)
        {
            _pityEvaluator.ResetPity();
        }
        ClearDrawLogs();
        _isGachaDirty = false;
    }

    #endregion
}
