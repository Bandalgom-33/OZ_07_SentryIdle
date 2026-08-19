using System.IO;
using UnityEngine;

public class SaveManager : SingletonBase<SaveManager>
{
    private string _savePath;

    // 세이브 파일 경로 초기화
    protected override void Awake()
    {
        base.Awake();
        _savePath = Path.Combine(Application.persistentDataPath, "SaveData.json");
    }

    // 게임 데이터 직렬화 및 저장
    public void SaveGameData()
    {
        SaveData data = new SaveData();

        EventBus.Publish(new DataSaveEvent(data));

        data.lastSaveTimestamp = System.DateTime.Now.ToString("o");

        string json = JsonUtility.ToJson(data, true);

        File.WriteAllText(_savePath, json);
        Debug.Log($"[SaveManager] 데이터 저장 완료: {_savePath}");
    }

    // 게임 데이터 읽기 및 로드
    public void LoadGameData()
    {
        if (!File.Exists(_savePath))
        {
            Debug.LogWarning("[SaveManager] 세이브 파일이 존재하지 않아 신규 데이터로 초기화합니다.");
            ResetGameData();
            return;
        }

        try
        {
            string json = File.ReadAllText(_savePath);

            SaveData data = JsonUtility.FromJson<SaveData>(json);

            EventBus.Publish(new DataLoadEvent(data));
            Debug.Log($"[SaveManager] 데이터 로드 완료: {_savePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] 데이터 로드 중 오류 발생: {e.Message}");
        }
    }

    // 게임 데이터 초기화
    public void ResetGameData()
    {
        if (File.Exists(_savePath))
        {
            File.Delete(_savePath);
        }

        EventBus.Publish(new DataResetEvent());
        Debug.Log("[SaveManager] 데이터 초기화 완료");
    }
}
