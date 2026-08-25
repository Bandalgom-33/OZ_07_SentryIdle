using System;
using System.Text;
using EndlessGuard.Unit.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 가챠 뽑기 조작, 천장 스택 확인, 치트 다이아 충전 및 실시간 가챠/돌파 로그 출력을 담당하는 UI 뷰어
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

    // 버튼 이벤트 초기화
    private void Awake()
    {
        if (closePanelButton != null) closePanelButton.onClick.AddListener(() => SetPanelActive(false));
        
        if (drawSingleButton != null) drawSingleButton.onClick.AddListener(OnClickDrawSingle);
        if (drawTenButton != null) drawTenButton.onClick.AddListener(OnClickDrawTen);
        if (addCheatDiamondButton != null) addCheatDiamondButton.onClick.AddListener(OnClickAddDiamond);
        if (clearLogButton != null) clearLogButton.onClick.AddListener(ClearLog);
    }

    // 이벤트 버스 구독 및 UI 초기화
    private void OnEnable()
    {
        EventBus.Subscribe<GachaDrawCompletedEvent>(OnGachaCompleted);
        EventBus.Subscribe<CurrencyChangedEvent>(OnCurrencyChanged);
        UpdatePityText();
        UpdateDrawButtonsInteractable();
    }

    // 이벤트 버스 구독 해제
    private void OnDisable()
    {
        EventBus.Unsubscribe<GachaDrawCompletedEvent>(OnGachaCompleted);
        EventBus.Unsubscribe<CurrencyChangedEvent>(OnCurrencyChanged);
    }

    #endregion

    #region UI 패널 토글 및 버튼 이벤트 핸들러

    // 가챠 패널 활성화/비활성화 전환
    public void SetPanelActive(bool active)
    {
        if (gachaPanel != null)
        {
            gachaPanel.SetActive(active);
            if (active)
            {
                UpdatePityText();
                UpdateDrawButtonsInteractable();
            }
        }
    }

    // 1회 단차 가챠 실행 요청
    private void OnClickDrawSingle()
    {
        if (GachaController.Instance == null) return;
        GachaController.Instance.ExecuteGacha(1);
    }

    // 10회 연속 가챠 실행 요청
    private void OnClickDrawTen()
    {
        if (GachaController.Instance == null) return;
        GachaController.Instance.ExecuteGacha(10);
    }

    // 테스트용 다이아 충전 요청
    private void OnClickAddDiamond()
    {
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.GetDiamond(30000);
            AppendLog("<color=#00FFFF>[CHEAT] +30,000 Diamonds Added!</color>");
        }
    }

    // 재화 변동 시 뽑기 버튼 활성화 상태 갱신
    private void OnCurrencyChanged(CurrencyChangedEvent evt)
    {
        UpdateDrawButtonsInteractable();
    }

    // 보유 다이아 잔액에 따라 뽑기 버튼 활성화/비활성화 처리
    private void UpdateDrawButtonsInteractable()
    {
        if (CurrencyManager.Instance == null || GachaController.Instance == null) return;

        long currentDiamond = CurrencyManager.Instance.Diamond;
        if (drawSingleButton != null)
        {
            drawSingleButton.interactable = currentDiamond >= GachaController.Instance.SingleDrawCost;
        }

        if (drawTenButton != null)
        {
            drawTenButton.interactable = currentDiamond >= GachaController.Instance.TenDrawCost;
        }
    }

    #endregion

    #region 이벤트 수신 및 상세 돌파 로그 뷰어 연산

    // 가챠 완료 이벤트 수신 및 상세 결과 로그 기록
    private void OnGachaCompleted(GachaDrawCompletedEvent evt)
    {
        UpdatePityText();
        UpdateDrawButtonsInteractable();

        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        _logBuilder.AppendLine($"<color=#00FFFF>[{timestamp}] --- Gacha x{evt.resultItems.Count} Result ---</color>");

        for (int i = 0; i < evt.resultItems.Count; i++)
        {
            var item = evt.resultItems[i];
            string colorHex = GetGradeColorHex(item.Grade);
            string statusTag = FormatResultStatusTag(item);
            
            _logBuilder.AppendLine($"  └ [{i + 1}] <color={colorHex}>[{(int)item.Grade}성 {item.Grade}] {item.DisplayName}</color> {statusTag}");
        }

        _logBuilder.AppendLine($"<color=#888888>  (Pity Stack: {evt.currentPityStack} / {GachaController.Instance.PityThreshold})</color>");
        _logBuilder.AppendLine();

        UpdateLogDisplay();
    }

    // 가챠 결과 아이템의 신규/돌파/풀돌 상태를 색상 태그로 포맷팅하는 헬퍼 메서드 (폰트 깨짐 방지를 위해 특수문자 배제)
    private string FormatResultStatusTag(IGachaRewardItem item)
    {
        return item.ResultType switch
        {
            // 신규 캐릭터 최초 해금 상태
            GachaResultType.NewUnlock => 
                "<color=#FFD700>[NEW! 신규 해금 (0단계)]</color>",

            // 중복 획득으로 인한 한계돌파 단계 상승 상태
            GachaResultType.Breakthrough => 
                $"<color=#00FF00>[돌파 성공! ({item.PreviousBreakthroughStep}단계 -> {item.CurrentBreakthroughStep}단계)]</color>",

            // 이미 6단계 풀돌에 도달한 캐릭터 획득 상태
            GachaResultType.MaxBreakthroughReached => 
                $"<color=#FFA500>[MAX 돌파! ({item.CurrentBreakthroughStep}단계 최대 돌파 완료)]</color>",

            _ => string.Empty
        };
    }

    // 천장 스택 UI 텍스트 갱신
    private void UpdatePityText()
    {
        if (pityCountText != null && GachaController.Instance != null)
        {
            pityCountText.text = $"<color=#00FFFF>{GachaController.Instance.CurrentPityStack}</color> / {GachaController.Instance.PityThreshold}";
        }
    }

    // 단일 로그 텍스트 추가
    public void AppendLog(string message)
    {
        _logBuilder.AppendLine(message);
        UpdateLogDisplay();
    }

    // ScrollView 텍스트 갱신 및 최하단 스크롤
    private void UpdateLogDisplay()
    {
        if (logContentText != null)
        {
            logContentText.text = _logBuilder.ToString();
        }

        if (logScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            logScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    // 로그 초기화
    public void ClearLog()
    {
        _logBuilder.Clear();
        if (logContentText != null)
        {
            logContentText.text = string.Empty;
        }
    }

    // 성 등급별 UI 강조 색상 반환
    private string GetGradeColorHex(UnitGrade grade)
    {
        return grade switch
        {
            UnitGrade.SixStar => "#FFD700",   // 6성 황금/전설
            UnitGrade.FiveStar => "#FF4500",  // 5성 주황/영웅
            UnitGrade.FourStar => "#A335EE",  // 4성 보라/희귀
            UnitGrade.ThreeStar => "#0070DD", // 3성 파랑/고급
            UnitGrade.TwoStar => "#1EFF00",   // 2성 초록/일반
            _ => "#FFFFFF"                    // 1성 흰색/기초
        };
    }

    #endregion
}
