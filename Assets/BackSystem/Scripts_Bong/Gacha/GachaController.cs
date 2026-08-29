using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using UnityEngine;

// 가챠 뽑기 실행, 천장 연산기 연동, 트랜잭션 안전성 보장 및 보상 지급 이벤트를 총괄하는 컨트롤러
[RequireComponent(typeof(GachaDataProvider))]
public class GachaController : SingletonBase<GachaController>
{
    #region 직렬화 필드 (인스펙터 바인딩)

    [Header("--- 가챠 설정 에셋 ---")]
    [Tooltip("가챠 비용, 등급별 가중치, 천장 기준값을 관리하는 설정 SO")]
    [SerializeField] private GachaConfigSO gachaConfig;

    #endregion

    #region 비공개 필드 및 프로퍼티

    private GachaDataProvider _dataProvider;
    private PityEvaluator _pityEvaluator;

    // 가챠 연타 및 중복 실행 방지를 위한 조작 락 플래그
    private bool _isDrawing = false;

    // 실제 가챠 뽑기 발생 여부를 추적하여 팝업 닫힘 시점에만 디스크 세이브를 유도하는 더티 플래그
    private bool _isGachaDirty = false;

    // 100개 고정 크기 링 버퍼 기반 가챠 로그 시스템 (GC Alloc 0 Byte 달성을 위해 인스턴스를 사전 할당하여 재사용)
    private const int MaxLogCount = 100;
    private readonly GachaLogEntry[] _drawLogBuffer = new GachaLogEntry[MaxLogCount];
    private int _logHead = 0;   // 가장 최신 로그가 기록될 인덱스
    private int _logCount = 0;  // 현재 저장된 유효 로그 총 개수 (최대 100)

    public int CurrentPityStack => _pityEvaluator != null ? _pityEvaluator.CurrentPityStack : 0;
    public int SingleDrawCost => gachaConfig != null ? gachaConfig.SingleDrawCost : 300;
    public int TenDrawCost => gachaConfig != null ? gachaConfig.TenDrawCost : 3000;
    public int PityThreshold => gachaConfig != null ? gachaConfig.HardPityThreshold : 100;
    public bool IsDrawing => _isDrawing;
    public bool IsGachaDirty => _isGachaDirty;

    #endregion

    #region 라이프 사이클

    // 인스턴스 초기화, 링 버퍼 사전 할당 및 천장 연산기 생성
    protected override void Awake()
    {
        base.Awake();
        _dataProvider = GetComponent<GachaDataProvider>();

        // GC Allocation을 방지하기 위해 100개의 GachaLogEntry 객체를 미리 생성
        for (int i = 0; i < MaxLogCount; i++)
        {
            _drawLogBuffer[i] = new GachaLogEntry();
        }

        // GachaConfigSO 에셋이 누락되었을 경우 명시적 에러 경고 출력
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

    // 앱 종료 시 더티 플래그가 켜져 있으면 데이터 유실 방지를 위해 자동 저장
    private void OnApplicationQuit()
    {
        SaveIfDirty();
    }

    // 모바일 백그라운드 전환 시 자동 저장 방어
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveIfDirty();
        }
    }

    #endregion

    #region 가챠 실행 핵심 메서드 (후불식 안전 트랜잭션 파이프라인)

    // 가챠 뽑기 실행 연산 (1회 또는 10회 고정 검증)
    public List<GachaRewardItem> ExecuteGacha(int drawCount)
    {
        // 1. 유효하지 않은 인수(음수, 0, 1회/10회가 아닌 임의의 수치) 입력 방어
        if (drawCount != 1 && drawCount != 10)
        {
            Debug.LogWarning($"[GachaController] 허용되지 않은 가챠 뽑기 수량({drawCount})입니다. 1회 또는 10회만 가능합니다.");
            return null;
        }

        // 2. 가챠 중복 실행(연타/스팸) 방어
        if (_isDrawing)
        {
            Debug.LogWarning("[GachaController] 이미 가챠 연출 또는 뽑기가 진행 중입니다.");
            return null;
        }

        // 3. 필수 참조 무결성 검증 (카탈로그, 설정, 프로바이더, 재화 매니저)
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

        // 4. 추첨 전 재화 보유량 사전 확인 (선(先) 차감하지 않고 보유 여부만 확인하여 트랜잭션 안전성 확보)
        if (!CurrencyManager.Instance.HasDiamond(requiredCost))
        {
            Debug.LogWarning("[GachaController] 보유 다이아가 부족하여 가챠를 실행할 수 없습니다.");
            return null;
        }

        _isDrawing = true;

        List<GachaRewardItem> results = new List<GachaRewardItem>(drawCount);
        string currentTimestamp = System.DateTime.Now.ToString("HH:mm:ss");

        // 10연차 내에서 동일 유닛 중복 등장 시 실시간 돌파 단계를 누적 추적하기 위한 배치 컨텍스트
        Dictionary<int, UnitSaveData> batchContext = new Dictionary<int, UnitSaveData>();

        // 5. 단차별 순차 추첨 루프
        for (int i = 0; i < drawCount; i++)
        {
            // 천장 스택 1 증가
            _pityEvaluator.IncreasePity();

            // 현재 스택에 따른 6성 확률 계산 및 등급 추첨
            float sixStarProb = _pityEvaluator.GetTopGradeProbability();
            UnitGrade rolledGrade = _dataProvider.RollGrade(sixStarProb);
            int rolledUnitId = _dataProvider.GetRandomUnitIdByGrade(rolledGrade);

            // 6성 최고 등급 획득 시 천장 스택 0으로 즉시 리셋 (남은 연차는 1부터 다시 스택 누적)
            if (rolledGrade == UnitGrade.SixStar)
            {
                _pityEvaluator.ResetPity();
            }

            if (rolledUnitId > 0)
            {
                UnitDataSO unitSO = _dataProvider.GetUnitData(rolledUnitId);
                GachaRewardItem rewardItem = new GachaRewardItem(unitSO, rolledUnitId);

                // 배치 컨텍스트를 주입하여 10연차 내 실시간 누적 돌파 상태 판정
                BreakthroughProcessor.ProcessGachaUnit(rewardItem, batchContext);
                results.Add(rewardItem);

                // 링 버퍼에 가챠 로그 기록 (GC Alloc 0 Byte)
                AddDrawLogToRingBuffer(rolledUnitId, currentTimestamp);
            }
        }

        // 6. 추첨이 완전히 성공적으로 끝난 직후 실제 재화 차감 (후불식 안전 트랜잭션 완료)
        if (!CurrencyManager.Instance.TrySpendDiamond(requiredCost))
        {
            Debug.LogError("[GachaController] 추첨 완료 후 다이아 차감에 실패하였습니다!");
            _isDrawing = false;
            return null;
        }

        // 7. 가챠 변경사항 발생 플래그 On (팝업 닫힘 시점에 디스크 저장 유도)
        _isGachaDirty = true;

        // 8. 연출 완료 상태로 락 선(先) 해제 (이벤트 수신 시 UI에서 interactable 상태를 올바르게 계산할 수 있도록 보장)
        _isDrawing = false;

        // 9. 가챠 완료 이벤트 브로드캐스트 (CollectionDataProvider 및 UI 뷰어 동기화)
        EventBus.Publish(new GachaDrawCompletedEvent(results, CurrentPityStack));

        return results;
    }

    // 외부 연출 시스템 연동 시 드로잉 락 상태를 수동 제어하는 메서드
    public void SetDrawingState(bool isDrawing)
    {
        _isDrawing = isDrawing;
    }

    #endregion

    #region 링 버퍼 로그 관리 연산 (GC Alloc 0 Byte)

    // 100개 고정 배열 링 버퍼에 가챠 로그 기록 덮어쓰기
    private void AddDrawLogToRingBuffer(int unitId, string timestamp)
    {
        // 새로 객체를 생성하지 않고 기존 버퍼 슬롯의 값만 교체
        GachaLogEntry entry = _drawLogBuffer[_logHead];
        entry.unitId = unitId;
        entry.timestamp = timestamp;

        _logHead = (_logHead + 1) % MaxLogCount;
        if (_logCount < MaxLogCount)
        {
            _logCount++;
        }
    }

    // UI 및 세이브 시스템을 위해 시간 순서대로 정렬된 로그 목록 반환
    public List<GachaLogEntry> GetOrderedDrawLogs()
    {
        List<GachaLogEntry> list = new List<GachaLogEntry>(_logCount);

        if (_logCount < MaxLogCount)
        {
            // 아직 100개가 다 안 찬 경우 0부터 _logCount - 1까지 순차 복사
            for (int i = 0; i < _logCount; i++)
            {
                list.Add(_drawLogBuffer[i]);
            }
        }
        else
        {
            // 100개가 꽉 찬 경우 가장 오래된 인덱스(_logHead)부터 100개 순환 복사
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

    #region 디스크 I/O 최적화 및 세이브/로드 연동

    // 가챠 뽑기로 인한 변경사항이 있을 때만 안전하게 디스크에 저장하는 디바운싱 메서드
    public void SaveIfDirty()
    {
        if (_isGachaDirty)
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.SaveGameData();
            }
            _isGachaDirty = false;
        }
    }

    // 가챠 천장 및 로그 세이브 데이터 저장 처리 (DataSaveEvent 수신)
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

    // 가챠 천장 및 로그 세이브 데이터 로드 처리 (DataLoadEvent 수신)
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

        // 세이브된 가챠 이력 로그를 링 버퍼로 복원
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

    // 가챠 데이터 초기화 처리 (DataResetEvent 수신)
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
