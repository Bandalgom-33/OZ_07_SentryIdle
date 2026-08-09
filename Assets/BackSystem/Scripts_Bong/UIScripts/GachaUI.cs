using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 가챠 시스템 전용 UGUI 패널 및 실시간 획득 로그 뷰어 UI 컨트롤러
public class GachaUI : MonoBehaviour
{
    #region SerializeFields (인스펙터 바인딩)

    [Header("패널 제어")]
    [SerializeField] private GameObject gachaPanel;       // 가챠 전체 팝업 패널
    [SerializeField] private Button openPanelButton;      // 메인 로비의 가챠 오픈 버튼
    [SerializeField] private Button closePanelButton;     // 가챠 팝업 [X] 닫기 버튼

    [Header("가챠 제어 버튼")]
    [SerializeField] private Button drawSingleButton;     // 1회 뽑기 버튼 (300 다이아)
    [SerializeField] private Button drawTenButton;        // 10회 뽑기 버튼 (3,000 다이아)
    [SerializeField] private Button addCheatDiamondButton;// [테스트] 다이아 30,000개 충전 버튼

    [Header("상태 텍스트")]
    [SerializeField] private TMP_Text pityCountText;      // "현재 천장: N / 100" 텍스트

    [Header("로그 뷰어")]
    [SerializeField] private TMP_Text logContentText;     // ScrollView 내부의 Text (로그가 쌓이는 텍스트)
    [SerializeField] private ScrollRect logScrollRect;    // 스크롤뷰 (자동 하단 스크롤용)
    [SerializeField] private Button clearLogButton;       // 로그 삭제 버튼

    #endregion

    #region 비공개 필드

    private readonly StringBuilder _logBuilder = new StringBuilder();

    #endregion

    #region 라이프 사이클

    private void Awake()
    {
        // 버튼 클릭 이벤트 추가
        if (openPanelButton != null) openPanelButton.onClick.AddListener(() => SetPanelActive(true));
        if (closePanelButton != null) closePanelButton.onClick.AddListener(() => SetPanelActive(false));
        
        if (drawSingleButton != null) drawSingleButton.onClick.AddListener(OnClickDrawSingle);
        if (drawTenButton != null) drawTenButton.onClick.AddListener(OnClickDrawTen);
        if (addCheatDiamondButton != null) addCheatDiamondButton.onClick.AddListener(OnClickAddDiamond);
        if (clearLogButton != null) clearLogButton.onClick.AddListener(ClearLog);
    }

    private void OnEnable()
    {
        EventBus.Subscribe<GachaDrawCompletedEvent>(OnGachaCompleted);
        UpdatePityText();
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<GachaDrawCompletedEvent>(OnGachaCompleted);
    }

    #endregion

    #region 패널 제어 및 버튼 처리

    // 가챠 패널 활성화 / 비활성화
    public void SetPanelActive(bool active)
    {
        if (gachaPanel != null) gachaPanel.SetActive(active);
        if (active) UpdatePityText();
    }

    // 1회 뽑기 버튼 클릭
    private void OnClickDrawSingle()
    {
        if (GachaController.Instance == null) return;
        
        if (!GachaController.Instance.CanAffordGacha(1))
        {
            AppendLog("<color=#FF0000>[ERROR] Not enough Diamonds! (1 Draw: 300 Diamond)</color>");
            return;
        }

        GachaController.Instance.ExecuteGacha(1);
    }

    // 10회 뽑기 버튼 클릭
    private void OnClickDrawTen()
    {
        if (GachaController.Instance == null) return;

        if (!GachaController.Instance.CanAffordGacha(10))
        {
            AppendLog("<color=#FF0000>[ERROR] Not enough Diamonds! (10 Draw: 3,000 Diamond)</color>");
            return;
        }

        GachaController.Instance.ExecuteGacha(10);
    }

    // 치트 다이아 충전 버튼 클릭
    private void OnClickAddDiamond()
    {
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.GetDiamond(30000);
            AppendLog("<color=#00FFFF>[CHEAT] +30,000 Diamonds Added!</color>");
        }
    }

    #endregion

    #region 이벤트 수신 및 로그 뷰어 처리

    // 가챠 완료 이벤트 수신 시 로그 생성 및 천장 텍스트 갱신
    private void OnGachaCompleted(GachaDrawCompletedEvent evt)
    {
        UpdatePityText();

        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        _logBuilder.AppendLine($"<color=#AAAAAA>[{timestamp}] --- Gacha x{evt.resultItems.Count} Result ---</color>");

        for (int i = 0; i < evt.resultItems.Count; i++)
        {
            var item = evt.resultItems[i];
            string colorHex = GetGradeColorHex(item.Grade);
            _logBuilder.AppendLine($"  └ [{i + 1}] <color={colorHex}>[{item.Grade}] {item.DisplayName}</color>");
        }

        _logBuilder.AppendLine($"<color=#888888>  (Pity Stack: {evt.currentPityStack} / 100)</color>");
        _logBuilder.AppendLine();

        UpdateLogDisplay();
    }

    // 천장 텍스트 업데이트
    private void UpdatePityText()
    {
        if (pityCountText != null && GachaController.Instance != null)
        {
            pityCountText.text = $"Pity Stack: <color=#FFD700>{GachaController.Instance.CurrentPityStack}</color> / 100";
        }
    }

    // 개별 텍스트 로그 추가
    private void AppendLog(string message)
    {
        _logBuilder.AppendLine(message);
        UpdateLogDisplay();
    }

    // 로그 텍스트 업데이트 및 최신 로그(최하단) 자동 스크롤 추적
    private void UpdateLogDisplay()
    {
        if (logContentText == null)
        {
            Debug.LogWarning("[GachaUI] logContentText (TMP_Text)가 Inspector에 연결되어 있지 않습니다!");
            return;
        }

        // 1. 텍스트 대입
        logContentText.text = _logBuilder.ToString();

        // 2. 텍스트 길이에 맞게 Content 높이 즉시 강제 갱신
        Canvas.ForceUpdateCanvases();

        // 3. 스크롤을 최하단(0f = 최신 로그 위치)으로 이동
        if (logScrollRect != null)
        {
            logScrollRect.verticalNormalizedPosition = 0f;
        }
    }








    // 로그 지우기
    private void ClearLog()
    {
        _logBuilder.Clear();
        UpdateLogDisplay();
    }

    // 등급별 텍스트 색상 HEX 코드 반환
    private string GetGradeColorHex(TestRarityGrade grade)
    {
        switch (grade)
        {
            case TestRarityGrade.GradeSSR: return "#FFD700"; // Gold
            case TestRarityGrade.GradeSR:  return "#A335EE"; // Purple
            case TestRarityGrade.GradeR:   return "#0070DD"; // Blue
            case TestRarityGrade.GradeN:   return "#FFFFFF"; // White
            default:                       return "#FFFFFF"; // White
        }
    }


    #endregion
}
