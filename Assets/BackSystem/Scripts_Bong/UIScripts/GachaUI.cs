using System;
using System.Text;
using EndlessGuard.Unit.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GachaUI : MonoBehaviour
{
    #region SerializeFields (인스펙터 바인딩)

    [Header("패널 제어")]
    [Tooltip("가챠 전체 팝업 패널 오브젝트")]
    [SerializeField] private GameObject gachaPanel;

    [Tooltip("가챠 팝업 [X] 닫기 버튼")]
    [SerializeField] private Button closePanelButton;

    [Header("가챠 제어 버튼")]
    [Tooltip("1회 가챠 실행 버튼 (300 다이아 소모)")]
    [SerializeField] private Button drawSingleButton;

    [Tooltip("10회 가챠 실행 버튼 (3,000 다이아 소모)")]
    [SerializeField] private Button drawTenButton;

    [Tooltip("테스트용 치트 다이아 충전 버튼 (30,000 다이아)")]
    [SerializeField] private Button addCheatDiamondButton;

    [Header("상태 텍스트")]
    [Tooltip("현재 누적 천장 스택 수치 UI 표시 텍스트")]
    [SerializeField] private TMP_Text pityCountText;

    [Header("로그 뷰어")]
    [Tooltip("ScrollView 내 실시간 가챠 로그 텍스트")]
    [SerializeField] private TMP_Text logContentText;

    [Tooltip("스크롤뷰 컴포넌트 참조")]
    [SerializeField] private ScrollRect logScrollRect;

    [Tooltip("로그 기록 전체 비우기 버튼")]
    [SerializeField] private Button clearLogButton;

    #endregion

    #region 비공개 필드

    private readonly StringBuilder _logBuilder = new StringBuilder();

    #endregion

    #region 라이프 사이클

    // 버튼 이벤트 초기화 연산
    private void Awake()
    {
        if (closePanelButton != null) closePanelButton.onClick.AddListener(() => SetPanelActive(false));
        
        if (drawSingleButton != null) drawSingleButton.onClick.AddListener(OnClickDrawSingle);
        if (drawTenButton != null) drawTenButton.onClick.AddListener(OnClickDrawTen);
        if (addCheatDiamondButton != null) addCheatDiamondButton.onClick.AddListener(OnClickAddDiamond);
        if (clearLogButton != null) clearLogButton.onClick.AddListener(ClearLog);
    }

    // 이벤트 구독 및 천장 텍스트 갱신
    private void OnEnable()
    {
        EventBus.Subscribe<GachaDrawCompletedEvent>(OnGachaCompleted);
        UpdatePityText();
    }

    // 이벤트 구독 해제 연산
    private void OnDisable()
    {
        EventBus.Unsubscribe<GachaDrawCompletedEvent>(OnGachaCompleted);
    }

    #endregion

    #region UI 패널 토글 및 버튼 이벤트 핸들러

    // 가챠 패널 활성화 상태 전환
    public void SetPanelActive(bool active)
    {
        if (gachaPanel != null)
        {
            gachaPanel.SetActive(active);
            if (active) UpdatePityText();
        }
    }

    // 단발 가챠 실행 요청
    private void OnClickDrawSingle()
    {
        if (GachaController.Instance == null) return;
        GachaController.Instance.ExecuteGacha(1);
    }

    // 10연속 가챠 실행 요청
    private void OnClickDrawTen()
    {
        if (GachaController.Instance == null) return;
        GachaController.Instance.ExecuteGacha(10);
    }

    // 테스트용 다이아 추가 요청
    private void OnClickAddDiamond()
    {
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.GetDiamond(30000);
            AppendLog("<color=#00FFFF>[CHEAT] +30,000 Diamonds Added!</color>");
        }
    }

    #endregion

    #region 이벤트 수신 및 로그 뷰어 연산

    // 가챠 완료 이벤트 수신 및 로그 생성
    private void OnGachaCompleted(GachaDrawCompletedEvent evt)
    {
        UpdatePityText();

        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        _logBuilder.AppendLine($"<color=#00FFFF>[{timestamp}] --- Gacha x{evt.resultItems.Count} Result ---</color>");

        for (int i = 0; i < evt.resultItems.Count; i++)
        {
            var item = evt.resultItems[i];
            string colorHex = GetGradeColorHex(item.Grade);
            string newTag = !item.IsOwned ? " <color=#FFD700>[NEW!]</color>" : "";
            
            _logBuilder.AppendLine($"  └ [{i + 1}] <color={colorHex}>[{(int)item.Grade}성 {item.Grade}] {item.DisplayName}</color>{newTag}");
        }

        _logBuilder.AppendLine($"<color=#888888>  (Pity Stack: {evt.currentPityStack} / {GachaController.Instance.PityThreshold})</color>");
        _logBuilder.AppendLine();

        UpdateLogDisplay();
    }

    // 천장 스택 UI 텍스트 갱신
    private void UpdatePityText()
    {
        if (pityCountText != null && GachaController.Instance != null)
        {
            pityCountText.text = $"Pity Stack: <color=#00FFFF>{GachaController.Instance.CurrentPityStack}</color> / {GachaController.Instance.PityThreshold}";
        }
    }

    // 로그 메시지 추가 및 뷰어 갱신
    private void AppendLog(string message)
    {
        _logBuilder.AppendLine(message);
        UpdateLogDisplay();
    }

    // 로그 텍스트 및 스크롤위치 갱신
    private void UpdateLogDisplay()
    {
        if (logContentText == null) return;

        logContentText.text = _logBuilder.ToString();
        Canvas.ForceUpdateCanvases();

        if (logScrollRect != null)
        {
            logScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    // 로그 기록 초기화
    private void ClearLog()
    {
        _logBuilder.Clear();
        UpdateLogDisplay();
    }

    // 유닛 등급별 헥사 색상 코드 반환
    private string GetGradeColorHex(UnitGrade grade)
    {
        switch (grade)
        {
            case UnitGrade.SixStar:   return "#FFD700";
            case UnitGrade.FiveStar:  return "#FF4500";
            case UnitGrade.FourStar:  return "#A335EE";
            case UnitGrade.ThreeStar: return "#0070DD";
            case UnitGrade.TwoStar:   return "#1EFF00";
            case UnitGrade.OneStar:   return "#FFFFFF";
            default:                  return "#FFFFFF";
        }
    }

    #endregion
}
