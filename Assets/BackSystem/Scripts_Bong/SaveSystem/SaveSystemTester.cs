using UnityEngine;

public class SaveSystemTester : MonoBehaviour
{
    #region 라이프 사이클
    
    // 키 입력을 통한 테스트 명령 감지
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

    // 재화 획득 및 세이브 테스트
    private void TestSave()
    {
        if (CurrencyManager.Instance == null || SaveManager.Instance == null)
        {
            Debug.LogError("[SaveTest] Manager 인스턴스 미존재");
            return;
        }
        CurrencyManager.Instance.GetGold(1000);
        CurrencyManager.Instance.GetDiamond(10);
        
        SaveManager.Instance.SaveGameData();

        Debug.Log(
            $"[SaveTest] 저장 완료 - Gold: {CurrencyManager.Instance.Gold}, Diamond: {CurrencyManager.Instance.Diamond}");
    }

    // 게임 데이터 로드 테스트
    private void TestLoad()
    {
        SaveManager.Instance.LoadGameData();

        Debug.Log(
            $"[SaveTest] 로드 완료 - Gold: {CurrencyManager.Instance.Gold}, Diamond: {CurrencyManager.Instance.Diamond}");
    }

    // 게임 데이터 초기화 테스트
    private void TestReset()
    {
        SaveManager.Instance.ResetGameData();

        Debug.Log(
            $"[SaveTest] 초기화 완료 - Gold: {CurrencyManager.Instance.Gold}, Diamond: {CurrencyManager.Instance.Diamond}");
    }

    #endregion
}

