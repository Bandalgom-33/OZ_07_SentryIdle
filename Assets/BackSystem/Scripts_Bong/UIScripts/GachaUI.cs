using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 가챠 시스템 전용 UGUI 패널 바인딩 및 실시간 획득 로그 뷰어 UI 컨트롤러
public class GachaUI : MonoBehaviour
{
    #region SerializeFields (인스펙터 바인딩)

    [Header("패널 제어")]
    [SerializeField] private GameObject gachaPanel;       // 가챠 전체 팝업 패널 오브젝트
    [SerializeField] private Button openPanelButton;      // 메인 로비 가챠 패널 열기 버튼
    [SerializeField] private Button closePanelButton;     // 가챠 팝업 [X] 닫기 버튼

    [Header("가챠 제어 버튼")]
    [SerializeField] private Button drawSingleButton;     // 1회 가챠 실행 버튼 (300 다이아 소모)
    [SerializeField] private Button drawTenButton;        // 10회 가챠 실행 버튼 (3,000 다이아 소모)
    [SerializeField] private Button addCheatDiamondButton;// 테스트용 치트 다이아 충전 버튼 (30,000 다이아)

    [Header("상태 텍스트")]
    [SerializeField] private TMP_Text pityCountText;      // 현재 누적 천장 스택 수치 UI 표시 텍스트

    [Header("로그 뷰어")]
    [SerializeField] private TMP_Text logContentText;     // ScrollView 내 실시간 가챠 로그 쌓임 텍스트
    [SerializeField] private ScrollRect logScrollRect;    // 스크롤뷰 컴포넌트 참조
    [SerializeField] private Button clearLogButton;       // 로그 기록 전체 비우기 버튼

    #endregion

    #region 비공개 필드

    // 로그 문자열 결합 시 메모리 할당(GC) 최소화를 위한 StringBuilder 객체
    private readonly StringBuilder _logBuilder = new StringBuilder();

    #endregion

    #region 라이프 사이클

    // 이벤트 리스너 및 UI 버튼 클릭 바인딩 초기화
    private void Awake()
    {
        if (openPanelButton != null) openPanelButton.onClick.AddListener(() => SetPanelActive(true));
        if (closePanelButton != null) closePanelButton.onClick.AddListener(() => SetPanelActive(false));
        
        if (drawSingleButton != null) drawSingleButton.onClick.AddListener(OnClickDrawSingle);
        if (drawTenButton != null) drawTenButton.onClick.AddListener(OnClickDrawTen);
        if (addCheatDiamondButton != null) addCheatDiamondButton.onClick.AddListener(OnClickAddDiamond);
        if (clearLogButton != null) clearLogButton.onClick.AddListener(ClearLog);
    }

    // 중앙 EventBus 가챠 완료 이벤트 구독 및 천장 수치 UI 초기화
    private void OnEnable()
    {
        EventBus.Subscribe<GachaDrawCompletedEvent>(OnGachaCompleted);
        UpdatePityText();
    }

    // 중앙 EventBus 이벤트 구독 해제 (메모리 누수 및 오작동 방지)
    private void OnDisable()
    {
        EventBus.Unsubscribe<GachaDrawCompletedEvent>(OnGachaCompleted);
    }

    #endregion

    #region 패널 제어 및 버튼 이벤트 처리

    // 가챠 팝업 패널 활성화/비활성화 상태 전환 처리
    public void SetPanelActive(bool active)
    {
        if (gachaPanel != null) gachaPanel.SetActive(active);
        if (active) UpdatePityText();
    }

    // 1회 가챠 뽑기 버튼 클릭 이벤트 처리 및 다이아 잔액 검증
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

    // 10회 연차 뽑기 버튼 클릭 이벤트 처리 및 다이아 잔액 검증
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

    // 테스트용 치트 다이아 지급 처리 및 확인 로그 생성
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

    // 가챠 완료 이벤트 수신 및 획득 캐릭터 텍스트 로그 포맷팅 연산
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

    // 현재 누적 천장 수치 UI 텍스트 갱신 연산
    private void UpdatePityText()
    {
        if (pityCountText != null && GachaController.Instance != null)
        {
            pityCountText.text = $"Pity Stack: <color=#FFD700>{GachaController.Instance.CurrentPityStack}</color> / 100";
        }
    }

    // 개별 시스템 알림 메시지 로그 버퍼 추가 처리
    private void AppendLog(string message)
    {
        _logBuilder.AppendLine(message);
        UpdateLogDisplay();
    }

    // UI 캔버스 강제 레이아웃 재계산 및 최신 로그 최하단 스크롤 연산
    private void UpdateLogDisplay()
    {
        if (logContentText == null)
        {
            Debug.LogWarning("[GachaUI] logContentText (TMP_Text)가 Inspector에 연결되어 있지 않습니다!");
            return;
        }

        // 1. 텍스트 버퍼 대입
        logContentText.text = _logBuilder.ToString();

        // 2. Content 텍스트 높이 즉시 강제 재계산 (0으로 줄어드는 프레임 딜레이 방지)
        Canvas.ForceUpdateCanvases();

        // 3. 스크롤 위치 최하단(0f = 최신 로그 위치) 이동 연산
        if (logScrollRect != null)
        {
            logScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    // 로그 버퍼 초기화 및 UI 텍스트 비우기 처리
    private void ClearLog()
    {
        _logBuilder.Clear();
        UpdateLogDisplay();
    }

    // 희귀도 등급별 UI 표시 색상 HEX 코드 변환 연산
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
