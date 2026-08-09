using UnityEngine;


public class SaveSystemTester : MonoBehaviour
{
    #region 라이프 사이클
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
            TestSave();
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            TestLoad();
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            TestReset();
        }
    }

    #endregion

    #region 내부 테스트 매서드 모음

    // 재화 획득 및 데이터 저장 테스트
    private void TestSave()
    {
        if (CurrencyManager.Instance == null || SaveManager.Instance == null)
        {
            Debug.LogError("[SaveTest] Manager 인스턴스 미존재");
            return;
        }
        CurrencyManager.Instance.GetGold(1000);
        CurrencyManager.Instance.GetDiamond(10);
        
        SaveManager.Instance.Save();

        Debug.Log(
            $"[SaveTest] 저장 완료 - Gold: {CurrencyManager.Instance.Gold}, Diamond: {CurrencyManager.Instance.Diamond}");
    }

    // 파일 역직렬화 및 데이터 복원 테스트
    private void TestLoad()
    {
        // 파일 읽기 실행
        SaveManager.Instance.Load();

        Debug.Log(
            $"[SaveTest] 로드 완료 - Gold: {CurrencyManager.Instance.Gold}, Diamond: {CurrencyManager.Instance.Diamond}");
    }

    // 세이브 파일 삭제 및 초기값 리셋 테스트
    private void TestReset()
    {
        // 데이터 완전 리셋 실행
        SaveManager.Instance.ResetSaveData();

        Debug.Log(
            $"[SaveTest] 초기화 완료 - Gold: {CurrencyManager.Instance.Gold}, Diamond: {CurrencyManager.Instance.Diamond}");
    }

    #endregion
}

