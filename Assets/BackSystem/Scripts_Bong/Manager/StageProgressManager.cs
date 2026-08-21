using System;
using UnityEngine;

// 스테이지 및 웨이브 진행 데이터의 단일 진실 공급원(SSOT) 및 세이브/로드를 총괄 관리하는 싱글톤 매니저
public class StageProgressManager : SingletonBase<StageProgressManager>
{
    #region 직렬화 변수 (인스펙터 설정)

    [Header("--- 스테이지 기본 설정 ---")]
    [Tooltip("기본/최초 시작 스테이지 번호")]
    [SerializeField, Min(1)] private int defaultStage = 1;

    [Tooltip("기본/최초 시작 웨이브 번호")]
    [SerializeField, Min(1)] private int defaultWave = 1;

    [Tooltip("스테이지당 총 웨이브 수")]
    [SerializeField, Min(1)] private int wavesPerStage = 5;

    #endregion

    #region 프로퍼티

    // 현재 진행 중인 스테이지 번호
    public int CurrentStage { get; private set; } = 1;

    // 현재 진행 중인 웨이브 번호
    public int CurrentWave { get; private set; } = 1;

    // 도달한 최고 웨이브 번호
    public int MaxWave { get; private set; } = 1;

    // 스테이지당 총 웨이브 수
    public int WavesPerStage => wavesPerStage;

    #endregion

    #region 라이프 사이클

    // 기본 스테이지 초기화
    protected override void Awake()
    {
        base.Awake();
        CurrentStage = defaultStage;
        CurrentWave = defaultWave;
        MaxWave = defaultWave;
    }

    // 전역 세이브/로드 이벤트 구독
    private void OnEnable()
    {
        EventBus.Subscribe<DataSaveEvent>(OnSave);
        EventBus.Subscribe<DataLoadEvent>(OnLoad);
        EventBus.Subscribe<DataResetEvent>(OnReset);
    }

    // 이벤트 구독 해제
    private void OnDisable()
    {
        EventBus.Unsubscribe<DataSaveEvent>(OnSave);
        EventBus.Unsubscribe<DataLoadEvent>(OnLoad);
        EventBus.Unsubscribe<DataResetEvent>(OnReset);
    }

    #endregion

    #region 스테이지 진행 제어 API

    // 스테이지 및 웨이브 수치 동시 설정 연산
    public void SetStageAndWave(int stage, int wave, bool autoSave = false)
    {
        CurrentStage = Mathf.Max(1, stage);
        CurrentWave = Mathf.Max(1, wave);
        if (CurrentWave > MaxWave)
        {
            MaxWave = CurrentWave;
        }

        PublishStageWaveChanged();

        if (autoSave && SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGameData();
        }
    }

    // 다음 스테이지 진입 연산 (스테이지 1 증가, 1웨이브로 리셋 및 자동 세이브)
    public void AdvanceToNextStage()
    {
        CurrentStage++;
        CurrentWave = 1;
        Debug.Log($"[StageProgressManager] 다음 스테이지 진입: Stage {CurrentStage}");

        PublishStageWaveChanged();

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGameData();
        }
    }

    // 현재 웨이브 진행도 갱신 연산
    public void SetCurrentWave(int wave)
    {
        CurrentWave = Mathf.Max(1, wave);
        if (CurrentWave > MaxWave)
        {
            MaxWave = CurrentWave;
        }

        PublishStageWaveChanged();
    }

    // 스테이지 진행 정보 초기화 연산
    public void ResetStage()
    {
        CurrentStage = defaultStage;
        CurrentWave = defaultWave;
        MaxWave = defaultWave;

        PublishStageWaveChanged();
    }

    // 스테이지/웨이브 변경 전역 이벤트 발행
    private void PublishStageWaveChanged()
    {
        EventBus.Publish(new StageWaveChangedEvent(CurrentStage, CurrentWave));
    }

    #endregion

    #region 세이브 / 로드 연산

    // 세이브 데이터 저장 처리
    private void OnSave(DataSaveEvent evt)
    {
        if (evt.saveData == null) return;
        if (evt.saveData.stage == null)
        {
            evt.saveData.stage = new StageData();
        }

        evt.saveData.stage.currentStage = CurrentStage;
        evt.saveData.stage.currentWave = CurrentWave;
        evt.saveData.stage.maxWave = MaxWave;
    }

    // 세이브 데이터 로드 처리
    private void OnLoad(DataLoadEvent evt)
    {
        if (evt.saveData == null || evt.saveData.stage == null)
        {
            ResetStage();
            return;
        }

        CurrentStage = Mathf.Max(1, evt.saveData.stage.currentStage);
        CurrentWave = Mathf.Max(1, evt.saveData.stage.currentWave);
        MaxWave = Mathf.Max(1, evt.saveData.stage.maxWave);

        PublishStageWaveChanged();
    }

    // 데이터 초기화 처리
    private void OnReset(DataResetEvent evt)
    {
        ResetStage();
    }

    #endregion
}
