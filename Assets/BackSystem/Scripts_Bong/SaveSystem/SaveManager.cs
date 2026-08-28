using System;
using System.IO;
using UnityEngine;

// 게임 전체 데이터의 파일 입출력(JSON) 및 종료/이탈 시 자동 저장을 총괄하는 싱글톤 매니저
public class SaveManager : SingletonBase<SaveManager>
{
    #region 내부 필드 및 프로퍼티

    private string _savePath;
    private float _lastSaveTime = -10f;
    private const float SaveCooldownSeconds = 0.5f; // 중복 저장 방지 쿨다운 (초)

    // 최초 로드 시 실제 저장 파일이 존재했는지 여부 (신규 유저 판별용: true=기존 유저, false=신규 유저)
    public bool HasExistingSaveFile { get; private set; } = false;

    #endregion

    #region 라이프 사이클

    // 세이브 파일 경로 초기화
    protected override void Awake()
    {
        base.Awake();
        _savePath = Path.Combine(Application.persistentDataPath, "SaveData.json");
    }

    // 게임 최초 시작 시 세이브 데이터 자동 로드 실행
    private void Start()
    {
        LoadGameData();
    }

    #endregion

    #region 애플리케이션 라이프사이클 훅 (종료/이탈 대비 자동 저장)

    // 애플리케이션 정상 및 강제 종료 시 자동 저장 (PC 빌드 및 에디터 플레이모드 중지)
    private void OnApplicationQuit()
    {
        SaveGameData(force: true);
    }

    // 모바일 백그라운드 전환 및 일시정지 시 자동 저장 (홈 화면 나가기, 강제종료 대비)
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveGameData(force: true);
        }
    }

    // 윈도우 포커스 이탈 시 자동 저장
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            SaveGameData(force: false);
        }
    }

    #endregion

    #region 저장 및 로드 연산

    // 게임 데이터 직렬화 및 저장 (중복 호출 쿨다운 및 강제 저장 지원)
    public void SaveGameData(bool force = false)
    {
        // 강제 저장이 아닌 경우 짧은 시간 내 연쇄 호출 시 디스크 I/O 병목 방지
        if (!force && Time.unscaledTime - _lastSaveTime < SaveCooldownSeconds)
        {
            return;
        }

        _lastSaveTime = Time.unscaledTime;

        try
        {
            SaveData data = new SaveData();

            EventBus.Publish(new DataSaveEvent(data));

            data.lastSaveTimestamp = DateTime.UtcNow.ToString("o");

            string json = JsonUtility.ToJson(data, true);

            File.WriteAllText(_savePath, json);

            string timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            long curGold = (data.currency != null) ? data.currency.gold : 0L;
            long curDia = (data.currency != null) ? data.currency.diamond : 0L;
            int curStage = (data.stage != null) ? data.stage.currentStage : 1;

            Debug.Log($"[SaveManager] [{timeStamp}] [SAVE] 저장 완료 - Gold: {curGold:N0}, Dia: {curDia:N0}, Stage: {curStage}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [SAVE ERROR] 저장 중 오류 발생: {ex.Message}");
        }
    }

    // 게임 데이터 읽기 및 로드
    public void LoadGameData()
    {
        string timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        if (!File.Exists(_savePath))
        {
            HasExistingSaveFile = false;
            Debug.LogWarning($"[SaveManager] [{timeStamp}] [LOAD] 세이브 파일이 존재하지 않아 신규 데이터로 초기화합니다.");
            ResetGameData();
            return;
        }

        try
        {
            HasExistingSaveFile = true;
            string json = File.ReadAllText(_savePath);

            SaveData data = JsonUtility.FromJson<SaveData>(json);

            EventBus.Publish(new DataLoadEvent(data));

            long curGold = (data.currency != null) ? data.currency.gold : 0L;
            long curDia = (data.currency != null) ? data.currency.diamond : 0L;
            int curStage = (data.stage != null) ? data.stage.currentStage : 1;

            Debug.Log($"[SaveManager] [{timeStamp}] [LOAD] 로드 완료 - Gold: {curGold:N0}, Dia: {curDia:N0}, Stage: {curStage}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] [{timeStamp}] [LOAD ERROR] 로드 중 오류 발생: {e.Message}");
        }
    }

    // 게임 데이터 초기화
    public void ResetGameData()
    {
        string timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        if (File.Exists(_savePath))
        {
            File.Delete(_savePath);
        }

        HasExistingSaveFile = false;
        EventBus.Publish(new DataResetEvent());
        Debug.Log($"[SaveManager] [{timeStamp}] [RESET] 데이터 초기화 완료");
    }

    #endregion
}
