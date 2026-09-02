using TMPro;
using UnityEngine;

// 인벤토리 패널 상단 5종 재화 텍스트 실시간 갱신 UI 컴포넌트
public class InventoryCurrencyBarUI : MonoBehaviour
{
    #region 직렬화 변수

    [Header("--- 재화 텍스트 UI ---")]
    [Tooltip("골드 수량 표시 TMP 텍스트")]
    [SerializeField] private TMP_Text goldText;

    [Tooltip("다이아 수량 표시 TMP 텍스트")]
    [SerializeField] private TMP_Text diamondText;

    [Tooltip("웨이브 마석 수량 표시 TMP 텍스트")]
    [SerializeField] private TMP_Text waveStoneText;

    [Tooltip("던전 마석 수량 표시 TMP 텍스트")]
    [SerializeField] private TMP_Text dungeonStoneText;

    [Tooltip("레이드 마석 수량 표시 TMP 텍스트")]
    [SerializeField] private TMP_Text raidStoneText;

    #endregion

    #region 라이프사이클

    // 전역 재화 변경 이벤트 구독
    private void OnEnable()
    {
        CurrencyManager.OnGoldChange += UpdateGoldUI;
        CurrencyManager.OnDiamondChange += UpdateDiamondUI;
        CurrencyManager.OnWaveStoneChange += UpdateWaveStoneUI;
        CurrencyManager.OnDungeonStoneChange += UpdateDungeonStoneUI;
        CurrencyManager.OnRaidStoneChange += UpdateRaidStoneUI;

        RefreshAllCurrencyUI();
    }

    // 전역 재화 변경 이벤트 구독 해제
    private void OnDisable()
    {
        CurrencyManager.OnGoldChange -= UpdateGoldUI;
        CurrencyManager.OnDiamondChange -= UpdateDiamondUI;
        CurrencyManager.OnWaveStoneChange -= UpdateWaveStoneUI;
        CurrencyManager.OnDungeonStoneChange -= UpdateDungeonStoneUI;
        CurrencyManager.OnRaidStoneChange -= UpdateRaidStoneUI;
    }

    #endregion

    #region 내부 변수 및 상수

    private static readonly string[] NumFormats = { "", "K", "M", "B", "T", "Qa", "Qi" };

    #endregion

    #region UI 갱신 메서드

    // 전체 재화 수량 일괄 동기화
    public void RefreshAllCurrencyUI()
    {
        if (CurrencyManager.Instance == null) return;

        UpdateGoldUI(CurrencyManager.Instance.Gold);
        UpdateDiamondUI(CurrencyManager.Instance.Diamond);
        UpdateWaveStoneUI(CurrencyManager.Instance.WaveStone);
        UpdateDungeonStoneUI(CurrencyManager.Instance.DungeonStone);
        UpdateRaidStoneUI(CurrencyManager.Instance.RaidStone);
    }

    // 골드 텍스트 갱신
    private void UpdateGoldUI(long gold)
    {
        if (goldText != null)
        {
            goldText.text = FormatCurrencyNumber(gold);
        }
    }

    // 다이아 텍스트 갱신
    private void UpdateDiamondUI(long diamond)
    {
        if (diamondText != null)
        {
            diamondText.text = FormatCurrencyNumber(diamond);
        }
    }

    // 웨이브 마석 텍스트 갱신
    private void UpdateWaveStoneUI(long waveStone)
    {
        if (waveStoneText != null)
        {
            waveStoneText.text = FormatCurrencyNumber(waveStone);
        }
    }

    // 던전 마석 텍스트 갱신
    private void UpdateDungeonStoneUI(long dungeonStone)
    {
        if (dungeonStoneText != null)
        {
            dungeonStoneText.text = FormatCurrencyNumber(dungeonStone);
        }
    }

    // 레이드 마석 텍스트 갱신
    private void UpdateRaidStoneUI(long raidStone)
    {
        if (raidStoneText != null)
        {
            raidStoneText.text = FormatCurrencyNumber(raidStone);
        }
    }

    // 1000 단위 축약 문자열 포맷팅 헬퍼
    private string FormatCurrencyNumber(double value)
    {
        if (value < 1000)
        {
            return value.ToString("N0");
        }

        int formatIndex = 0;
        while (value >= 1000 && formatIndex < NumFormats.Length - 1)
        {
            value /= 1000;
            formatIndex++;
        }

        return value.ToString("N1") + NumFormats[formatIndex];
    }

    #endregion
}
