using System.IO;
using UnityEngine;

// 로컬 JSON 세이브/로드 파일 암복호화 및 저장 관리 총괄 싱글톤
public class SaveManager : SingletonBase<SaveManager>
{
    private string _savePath;

    // 세이브 파일 저장 경로 지정 초기화 연산
    protected override void Awake()
    {
        base.Awake();
        _savePath = Path.Combine(Application.persistentDataPath, "SaveData.json");
    }

    // 메모리 세이브 객체 생성, 시스템 데이터 수집 및 로컬 JSON 파일 기록 연산
    public void SaveGameData()
    {
        SaveData data = new SaveData();

        // 1. 등록된 시스템들에 세이브 데이터 수집 이벤트 발행
        EventBus.Publish(new DataSaveEvent(data));

        // 타임스탬프 기록 연산
        data.lastSaveTimestamp = System.DateTime.Now.ToString("o");

        // 2. JSON 직렬화 연산
        string json = JsonUtility.ToJson(data, true);

        // 3. 로컬 파일 쓰기 연산
        File.WriteAllText(_savePath, json);
        Debug.Log($"[SaveManager] 데이터 저장 완료: {_savePath}");
    }

    // 로컬 JSON 세이브 파일 읽기, 역직렬화 및 시스템 데이터 복원 연산
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
            // 1. 로컬 파일 읽기 연산
            string json = File.ReadAllText(_savePath);

            // 2. JSON 역직렬화 연산
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            // 3. 수신 시스템들에 데이터 복원 이벤트 발행
            EventBus.Publish(new DataLoadEvent(data));
            Debug.Log($"[SaveManager] 데이터 로드 완료: {_savePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] 데이터 로드 중 오류 발생: {e.Message}");
        }
    }

    // 게임 데이터 전체 초기화 이벤트 발행 연산
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
