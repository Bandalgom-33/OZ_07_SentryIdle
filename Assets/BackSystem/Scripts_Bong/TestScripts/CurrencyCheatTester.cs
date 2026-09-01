using UnityEngine;

// 테스트 환경 치트 단축키 기반 재화 일괄 지급 테스터 컴포넌트
public class CurrencyCheatTester : MonoBehaviour
{
    #region 인스펙터 바인딩 필드

    [Header("1. 치트 단축키 설정")]
    [Tooltip("모든 재화를 일괄 지급할 트리거 단축키")]
    [SerializeField] private KeyCode cheatKey = KeyCode.M;

    [Header("2. 1회 지급 수량 설정")]
    [Tooltip("1회 지급할 골드 수량")]
    [SerializeField] private long addGoldAmount = 1_000_000L;

    [Tooltip("1회 지급할 다이아 수량")]
    [SerializeField] private long addDiamondAmount = 10_000L;

    [Tooltip("1회 지급할 웨이브 마석 수량")]
    [SerializeField] private long addWaveStoneAmount = 1_000L;

    [Tooltip("1회 지급할 던전 마석 수량")]
    [SerializeField] private long addDungeonStoneAmount = 1_000L;

    [Tooltip("1회 지급할 레이드 마석 수량")]
    [SerializeField] private long addRaidStoneAmount = 1_000L;

    #endregion

    #region 라이프사이클

    // 매 프레임 치트 단축키 입력 감지 및 지급 처리
    private void Update()
    {
        if (Input.GetKeyDown(cheatKey))
        {
            GrantAllCurrencies();
        }
    }

    #endregion

    #region 재화 지급 메서드

    // 5종 재화 일괄 지급 및 디버그 로그 출력
    public void GrantAllCurrencies()
    {
        CurrencyManager cm = CurrencyManager.Instance;
        if (cm == null)
        {
            Debug.LogWarning("[CurrencyCheatTester] CurrencyManager 인스턴스를 찾을 수 없어 재화를 지급하지 못했습니다.");
            return;
        }

        if (addGoldAmount > 0)
        {
            cm.GetGold(addGoldAmount, applyModifiers: false);
        }

        if (addDiamondAmount > 0)
        {
            cm.GetDiamond(addDiamondAmount, applyModifiers: false);
        }

        if (addWaveStoneAmount > 0)
        {
            cm.GetWaveStone(addWaveStoneAmount);
        }

        if (addDungeonStoneAmount > 0)
        {
            cm.GetDungeonStone(addDungeonStoneAmount);
        }

        if (addRaidStoneAmount > 0)
        {
            cm.GetRaidStone(addRaidStoneAmount);
        }

        Debug.Log($"<color=#00FF00>[CurrencyCheatTester] 치트 발동! 모든 재화 지급 완료: " +
                  $"Gold +{addGoldAmount:N0} (현재: {cm.Gold:N0}) | " +
                  $"Diamond +{addDiamondAmount:N0} (현재: {cm.Diamond:N0}) | " +
                  $"WaveStone +{addWaveStoneAmount:N0} (현재: {cm.WaveStone:N0}) | " +
                  $"DungeonStone +{addDungeonStoneAmount:N0} (현재: {cm.DungeonStone:N0}) | " +
                  $"RaidStone +{addRaidStoneAmount:N0} (현재: {cm.RaidStone:N0})</color>");
    }

    #endregion
}
