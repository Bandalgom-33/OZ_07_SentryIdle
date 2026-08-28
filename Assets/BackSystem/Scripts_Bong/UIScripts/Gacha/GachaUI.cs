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

    #region 직렬화 필드 추가

    [Header("카탈로그 참조")]
    [Tooltip("로그 복원 시 유닛 이름 및 성급 조회를 위한 카탈로그 SO")]
    [SerializeField] private UnitCatalog unitCatalog;

    #endregion

    #region 비공개 필드

    private readonly StringBuilder _logBuilder = new StringBuilder();

    #endregion

    #region 라이프 사이클

    // 버튼 이벤트 초기화 및 카탈로그 로드
    private void Awake()
    {
        if (closePanelButton != null) closePanelButton.onClick.AddListener(() => SetPanelActive(false));
        
        if (drawSingleButton != null) drawSingleButton.onClick.AddListener(OnClickDrawSingle);
        if (drawTenButton != null) drawTenButton.onClick.AddListener(OnClickDrawTen);
        if (addCheatDiamondButton != null) addCheatDiamondButton.onClick.AddListener(OnClickAddDiamond);
        if (clearLogButton != null) clearLogButton.onClick.AddListener(ClearLog);

        if (unitCatalog == null)
        {
            unitCatalog = CollectionDataProvider.Instance != null 
                ? CollectionDataProvider.Instance.UnitCatalog 
                : Resources.Load<UnitCatalog>("Catalogs/UnitCatalog");
        }
    }

    // 이벤트 버스 구독 및 UI 초기화
    private void OnEnable()
    {
        EventBus.Subscribe<GachaDrawCompletedEvent>(OnGachaCompleted);
        EventBus.Subscribe<CurrencyChangedEvent>(OnCurrencyChanged);
        EventBus.Subscribe<DataLoadEvent>(OnDataLoaded);
        UpdatePityText();
        UpdateDrawButtonsInteractable();
        RestoreSavedDrawLogs();
    }

    // 이벤트 버스 구독 해제
    private void OnDisable()
    {
        EventBus.Unsubscribe<GachaDrawCompletedEvent>(OnGachaCompleted);
        EventBus.Unsubscribe<CurrencyChangedEvent>(OnCurrencyChanged);
        EventBus.Unsubscribe<DataLoadEvent>(OnDataLoaded);
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
                RestoreSavedDrawLogs();
            }
        }
    }

    // 세이브 데이터 로드 이벤트 콜백
    private void OnDataLoaded(DataLoadEvent evt)
    {
        RestoreSavedDrawLogs();
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

    #region 이벤트 수신 및 로그 뷰어 연산 (시간 및 유닛 정보만 출력)

    // 세이브된 가챠 로그 전체를 읽어와 스크롤뷰에 복원 출력
    public void RestoreSavedDrawLogs()
    {
        _logBuilder.Clear();

        if (GachaController.Instance != null && GachaController.Instance.DrawLogs != null)
        {
            var logs = GachaController.Instance.DrawLogs;
            for (int i = 0; i < logs.Count; i++)
            {
                var entry = logs[i];
                string unitKey = UnitIdHelper.ToUnitKey(entry.unitId);
                string unitName = $"Unit {entry.unitId}";
                UnitGrade grade = UnitGrade.OneStar;

                if (unitCatalog != null && unitCatalog.TryGetById(unitKey, out UnitDataSO dataSO) && dataSO != null)
                {
                    unitName = dataSO.DisplayName;
                    grade = dataSO.Grade;
                }

                string colorHex = GetGradeColorHex(grade);
                string time = string.IsNullOrEmpty(entry.timestamp) ? "--:--:--" : entry.timestamp;
                _logBuilder.AppendLine($"[{time}] <color={colorHex}>[{(int)grade}성] {unitName}</color>");
            }
        }

        UpdateLogDisplay();
    }

    // 가챠 완료 이벤트 수신 및 결과 로그 추가 (돌파 단계 제외, 시간 및 유닛 정보만 기록)
    private void OnGachaCompleted(GachaDrawCompletedEvent evt)
    {
        UpdatePityText();
        UpdateDrawButtonsInteractable();

        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        for (int i = 0; i < evt.resultItems.Count; i++)
        {
            var item = evt.resultItems[i];
            string colorHex = GetGradeColorHex(item.Grade);
            _logBuilder.AppendLine($"[{timestamp}] <color={colorHex}>[{(int)item.Grade}성] {item.DisplayName}</color>");
        }

        UpdateLogDisplay();
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
