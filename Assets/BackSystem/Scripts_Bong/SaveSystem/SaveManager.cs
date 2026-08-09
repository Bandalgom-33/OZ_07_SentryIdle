using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;



public class SaveManager : SingletonBase<SaveManager>
{

    #region 비공개 변수
    
    private string _saveFilePath;
    private SaveData _currentSaveData;
    private readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
    {
        MissingMemberHandling = MissingMemberHandling.Ignore,
        Formatting = Formatting.Indented
    };

    #endregion

    #region 라이프 사이클

    protected override void Awake()
    {
        base.Awake();
        _saveFilePath = Path.Combine(Application.persistentDataPath, "save.json");
        Load();
    }

    #endregion

    #region 외부 노출 메서드
    
    public SaveData GetSaveData() => _currentSaveData;

    // 데이터 저장
    public void Save()
    {
        EventBus.Publish(new DataSaveEvent(_currentSaveData));
        // 오프라인 계산을 위한 UTC 시간 저장
        _currentSaveData.lastSaveTimestamp = DateTime.UtcNow.ToString("o");
        string json = JsonConvert.SerializeObject(_currentSaveData, _jsonSettings);
        File.WriteAllText(_saveFilePath, json);
        Debug.Log($"[SaveManager] 저장 완료 → {_saveFilePath}");
    }

    // 저장 데이터 로드
    public void Load()
    {
        if (File.Exists(_saveFilePath))
        {
            string json = File.ReadAllText(_saveFilePath);
            _currentSaveData = JsonConvert.DeserializeObject<SaveData>(json, _jsonSettings);
            Debug.Log($"[SaveManager] 로드 완료 → {_saveFilePath}");
        }
        else
        {
            _currentSaveData = CreateDefaultSaveData();
            Debug.Log("[SaveManager] 세이브 파일 없음 - 기본값 데이터 생성");
        }
        EventBus.Publish(new DataLoadEvent(_currentSaveData));
    }

    // 세이브 파일 삭제 및 전체 데이터 초기화
    public void ResetSaveData()
    {
        if (File.Exists(_saveFilePath))
        {
            File.Delete(_saveFilePath);
            Debug.Log("[SaveManager] 세이브 파일 삭제 완료");
        }
        _currentSaveData = CreateDefaultSaveData();
        EventBus.Publish(new DataResetEvent());
        Debug.Log("[SaveManager] 데이터 초기화 완료");
    }

    #endregion

    #region 내부 메서드

    // 새 데이터 생성
    private SaveData CreateDefaultSaveData()
    {
        return new SaveData();
    }

    #endregion
}

