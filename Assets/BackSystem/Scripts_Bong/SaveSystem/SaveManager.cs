using System;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;

public class SaveManager : SingletonBase<SaveManager>
{
    #region 인스펙터 설정

    [Header("--- 세이브 버전 관리 ---")]
    [Tooltip("현재 요구하는 세이브 데이터 버전. 저장된 버전과 다르면 자동 초기화")]
    [SerializeField] private int requiredSaveVersion = 1;

    [Header("--- 저장 쿨다운 ---")]
    [Tooltip("강제 저장이 아닌 경우 최소 저장 간격(초)")]
    [SerializeField] private float saveCooldownSeconds = 0.5f;

    #endregion

    #region 내부 필드

    private string _savePath;
    private string _backupPath;
    private string _tempPath;
    private string _legacyPath;

    private float _lastSaveTime = -99f;

    public bool HasExistingSaveFile { get; private set; } = false;

    #endregion

    #region 라이프 사이클

    // 세이브 파일 경로 초기화
    protected override void Awake()
    {
        base.Awake();

        string dir = Application.persistentDataPath;
        _savePath = Path.Combine(dir, "SaveData.sav");
        _backupPath = Path.Combine(dir, "SaveData.bak");
        _tempPath = Path.Combine(dir, "SaveData.tmp");
        _legacyPath = Path.Combine(dir, "SaveData.json");
    }

    // 게임 시작 시 세이브 데이터 자동 로드
    private void Start()
    {
        TryMigrateLegacyJson();
        LoadGameData();
    }

    // 전역 이벤트 구독 등록
    private void OnEnable()
    {
        EventBus.Subscribe<RequestSaveGameEvent>(OnRequestSaveGame);
    }

    // 전역 이벤트 구독 해제
    private void OnDisable()
    {
        EventBus.Unsubscribe<RequestSaveGameEvent>(OnRequestSaveGame);
    }

    // 외부 저장 요청 이벤트 수신 처리
    private void OnRequestSaveGame(RequestSaveGameEvent evt)
    {
        SaveGameData(evt.force);
    }

    #endregion

    #region 애플리케이션 라이프사이클 훅

    // 애플리케이션 종료 시 단일 강제 저장
    private void OnApplicationQuit()
    {
        SaveGameData(force: true);
    }

    // 모바일 백그라운드 전환 시 단일 강제 저장
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveGameData(force: true);
        }
    }

    // 윈도우 포커스 이탈 시 쿨다운 준수 저장
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            SaveGameData(force: false);
        }
    }

    #endregion

    #region 저장 및 로드 연산

    // 게임 데이터 직렬화 및 원자적 백업 저장
    public void SaveGameData(bool force = false)
    {
        if (!force && Time.unscaledTime - _lastSaveTime < saveCooldownSeconds)
        {
            return;
        }
        _lastSaveTime = Time.unscaledTime;

        try
        {
            SaveData data = new SaveData();
            EventBus.Publish(new DataSaveEvent(data));

            data.saveVersion = requiredSaveVersion;
            data.lastSaveTimestamp = DateTime.UtcNow.ToString("o");

            string json = JsonUtility.ToJson(data, true);
            byte[] encrypted = SaveEncryptor.Encrypt(json);

            File.WriteAllBytes(_tempPath, encrypted);

            if (File.Exists(_savePath))
            {
                File.Copy(_savePath, _backupPath, overwrite: true);
            }

            if (File.Exists(_savePath))
            {
                File.Delete(_savePath);
            }
            File.Move(_tempPath, _savePath);

            string ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            long curGold = data.currency != null ? data.currency.gold : 0L;
            long curDia = data.currency != null ? data.currency.diamond : 0L;
            int curStage = data.stage != null ? data.stage.currentStage : 1;

            Debug.Log($"[SaveManager] [{ts}] [SAVE] 저장 완료 (v{requiredSaveVersion}) - Gold:{curGold:N0}, Dia:{curDia:N0}, Stage:{curStage}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [SAVE ERROR] 저장 중 오류: {ex.GetType().Name} - {ex.Message}");
            TryDeleteFile(_tempPath);
        }
    }

    // 게임 데이터 로드 및 손상 복구 시도
    public void LoadGameData()
    {
        string ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        if (File.Exists(_savePath))
        {
            SaveData data = TryLoadFromFile(_savePath, "메인(.sav)");
            if (data != null)
            {
                if (!CheckVersion(data, ts)) return;

                data.Validate();

                HasExistingSaveFile = true;
                EventBus.Publish(new DataLoadEvent(data));

                long gold = data.currency != null ? data.currency.gold : 0L;
                long dia = data.currency != null ? data.currency.diamond : 0L;
                int stage = data.stage != null ? data.stage.currentStage : 1;
                Debug.Log($"[SaveManager] [{ts}] [LOAD] 메인 파일 로드 완료 - Gold:{gold:N0}, Dia:{dia:N0}, Stage:{stage}");
                return;
            }
            Debug.LogWarning($"[SaveManager] [{ts}] [LOAD] 메인 파일 로드 실패 → 백업 파일 시도");
        }
        else
        {
            Debug.LogWarning($"[SaveManager] [{ts}] [LOAD] 메인 파일 없음 → 백업 파일 시도");
        }

        if (File.Exists(_backupPath))
        {
            SaveData data = TryLoadFromFile(_backupPath, "백업(.bak)");
            if (data != null)
            {
                if (!CheckVersion(data, ts)) return;

                data.Validate();

                HasExistingSaveFile = true;
                EventBus.Publish(new DataLoadEvent(data));

                long gold = data.currency != null ? data.currency.gold : 0L;
                long dia = data.currency != null ? data.currency.diamond : 0L;
                int stage = data.stage != null ? data.stage.currentStage : 1;
                Debug.LogWarning($"[SaveManager] [{ts}] [LOAD] 백업 파일로 복원 완료 - Gold:{gold:N0}, Dia:{dia:N0}, Stage:{stage}");
                return;
            }
            Debug.LogError($"[SaveManager] [{ts}] [LOAD] 백업 파일도 손상됨 → 자동 초기화");
        }
        else
        {
            Debug.LogWarning($"[SaveManager] [{ts}] [LOAD] 백업 파일도 없음 → 신규 데이터로 초기화");
        }

        HasExistingSaveFile = false;
        ResetGameData();
    }

    // 세이브 파일 및 백업 파일 전체 삭제 및 초기화
    public void ResetGameData()
    {
        string ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        TryDeleteFile(_savePath);
        TryDeleteFile(_backupPath);
        TryDeleteFile(_tempPath);

        HasExistingSaveFile = false;
        EventBus.Publish(new DataResetEvent());

        Debug.Log($"[SaveManager] [{ts}] [RESET] 데이터 초기화 완료");
    }

    #endregion

    #region 내부 헬퍼

    // 세이브 파일 복호화 및 데이터 역직렬화
    private SaveData TryLoadFromFile(string filePath, string label)
    {
        try
        {
            byte[] encrypted = File.ReadAllBytes(filePath);
            string json = SaveEncryptor.Decrypt(encrypted);
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            if (data == null)
            {
                Debug.LogError($"[SaveManager] {label} JSON 파싱 결과가 null입니다.");
                return null;
            }
            return data;
        }
        catch (CryptographicException ex)
        {
            Debug.LogError($"[SaveManager] {label} 복호화 실패: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] {label} 로드 오류: {ex.GetType().Name} - {ex.Message}");
            return null;
        }
    }

    // 세이브 데이터 버전 일치 여부 검증
    private bool CheckVersion(SaveData data, string ts)
    {
        if (data.saveVersion == requiredSaveVersion) return true;

        Debug.LogWarning(
            $"[SaveManager] [{ts}] [VERSION MISMATCH] 저장 버전={data.saveVersion}, 요구 버전={requiredSaveVersion} → 자동 초기화");

        HasExistingSaveFile = false;
        ResetGameData();
        return false;
    }

    // 레거시 JSON 세이브 파일 마이그레이션
    private void TryMigrateLegacyJson()
    {
        if (!File.Exists(_legacyPath)) return;

        Debug.Log("[SaveManager] [MIGRATE] 구버전 SaveData.json 감지 → 새 형식으로 변환 중...");

        try
        {
            string json = File.ReadAllText(_legacyPath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            if (data != null)
            {
                data.saveVersion = requiredSaveVersion;
                data.Validate();

                string newJson = JsonUtility.ToJson(data, true);
                byte[] encrypted = SaveEncryptor.Encrypt(newJson);

                File.WriteAllBytes(_tempPath, encrypted);
                if (File.Exists(_savePath)) File.Delete(_savePath);
                File.Move(_tempPath, _savePath);

                Debug.Log("[SaveManager] [MIGRATE] 마이그레이션 완료 → SaveData.json 삭제");
            }

            TryDeleteFile(_legacyPath);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] [MIGRATE] 마이그레이션 중 오류: {ex.Message} → 구버전 파일 삭제 후 신규 시작");
            TryDeleteFile(_legacyPath);
        }
    }

    // 대상 파일 안전 삭제
    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SaveManager] 파일 삭제 실패: {path} - {ex.Message}");
        }
    }

    #endregion
}
