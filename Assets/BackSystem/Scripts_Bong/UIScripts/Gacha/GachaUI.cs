using System;
using System.Collections.Generic;
using System.Text;
using EndlessGuard.Unit.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 가챠 시스템 UI 제어 및 정보 표시 컴포넌트
public class GachaUI : MonoBehaviour
{
    #region 직렬화 필드

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

    [Header("상태 및 재화 텍스트")]
    [Tooltip("현재 보유 다이아 수량 표시 텍스트")]
    [SerializeField] private TMP_Text currentDiamondText;

    [Tooltip("현재 누적 천장 스택 수치 UI 표시 텍스트")]
    [SerializeField] private TMP_Text pityCountText;

    [Header("로그 뷰어")]
    [Tooltip("ScrollView 내 실시간 가챠 로그 텍스트")]
    [SerializeField] private TMP_Text logContentText;

    [Tooltip("스크롤뷰 컴포넌트 참조")]
    [SerializeField] private ScrollRect logScrollRect;

    [Tooltip("로그 기록 전체 비우기 버튼")]
    [SerializeField] private Button clearLogButton;

    [Header("카탈로그 참조")]
    [Tooltip("로그 복원 시 유닛 이름 및 성급 조회를 위한 카탈로그 SO")]
    [SerializeField] private UnitCatalog unitCatalog;

    #endregion

    #region 비공개 필드 및 상수

    private readonly StringBuilder _logBuilder = new StringBuilder(4096);
    private static readonly string[] NumFormats = { "", "K", "M", "B", "T", "Qa", "Qi" };

    #endregion

    #region 라이프 사이클

    // 버튼 이벤트 리스너 바인딩 및 카탈로그 초기화
    private void Awake()
    {
        if (closePanelButton != null) closePanelButton.onClick.AddListener(() => SetPanelActive(false));
        if (drawSingleButton != null) drawSingleButton.onClick.AddListener(OnClickDrawSingle);
        if (drawTenButton != null) drawTenButton.onClick.AddListener(OnClickDrawTen);
        if (clearLogButton != null) clearLogButton.onClick.AddListener(ClearLog);

        if (unitCatalog == null && CollectionDataProvider.Instance != null)
        {
            unitCatalog = CollectionDataProvider.Instance.UnitCatalog;
        }
    }

    // 전역 이벤트 구독 및 초기 UI 갱신
    private void OnEnable()
    {
        EventBus.Subscribe<GachaDrawCompletedEvent>(OnGachaCompleted);
        EventBus.Subscribe<CurrencyChangedEvent>(OnCurrencyChanged);
        EventBus.Subscribe<DataLoadEvent>(OnDataLoaded);
        
        CurrencyManager.OnDiamondChange += UpdateDiamondUI;

        if (CurrencyManager.Instance != null)
        {
            UpdateDiamondUI(CurrencyManager.Instance.Diamond);
        }

        UpdatePityText();
        UpdateDrawButtonsInteractable();
        RestoreSavedDrawLogs();
    }

    // 전역 이벤트 구독 해제
    private void OnDisable()
    {
        EventBus.Unsubscribe<GachaDrawCompletedEvent>(OnGachaCompleted);
        EventBus.Unsubscribe<CurrencyChangedEvent>(OnCurrencyChanged);
        EventBus.Unsubscribe<DataLoadEvent>(OnDataLoaded);

        CurrencyManager.OnDiamondChange -= UpdateDiamondUI;
    }

    #endregion

    #region UI 패널 제어 및 버튼 이벤트 핸들러

    // 가챠 패널 활성화 상태 전환 및 데이터 저장 처리
    public void SetPanelActive(bool active)
    {
        if (gachaPanel != null)
        {
            gachaPanel.SetActive(active);

            if (active)
            {
                if (CurrencyManager.Instance != null)
                {
                    UpdateDiamondUI(CurrencyManager.Instance.Diamond);
                }

                UpdatePityText();
                UpdateDrawButtonsInteractable();
                RestoreSavedDrawLogs();
            }
            else
            {
                if (GachaController.Instance != null)
                {
                    GachaController.Instance.SaveIfDirty();
                }
            }
        }
    }

    // 세이브 데이터 로드 이벤트 수신 처리
    private void OnDataLoaded(DataLoadEvent evt)
    {
        if (CurrencyManager.Instance != null)
        {
            UpdateDiamondUI(CurrencyManager.Instance.Diamond);
        }
        RestoreSavedDrawLogs();
    }

    // 단차(1회) 가챠 실행 요청
    private void OnClickDrawSingle()
    {
        if (GachaController.Instance == null) return;
        GachaController.Instance.ExecuteGacha(1);
    }

    // 연차(10회) 가챠 실행 요청
    private void OnClickDrawTen()
    {
        if (GachaController.Instance == null) return;
        GachaController.Instance.ExecuteGacha(10);
    }

    // 재화 변경 이벤트 수신 시 버튼 인터랙션 갱신 처리
    private void OnCurrencyChanged(CurrencyChangedEvent evt)
    {
        if (evt.currencyType == CurrencyType.Diamond)
        {
            UpdateDrawButtonsInteractable();
        }
    }

    // 보유 다이아 및 가챠 진행 상태에 따른 버튼 활성화 상태 갱신
    private void UpdateDrawButtonsInteractable()
    {
        if (CurrencyManager.Instance == null || GachaController.Instance == null) return;

        bool isDrawing = GachaController.Instance.IsDrawing;
        long currentDiamond = CurrencyManager.Instance.Diamond;

        if (drawSingleButton != null)
        {
            drawSingleButton.interactable = !isDrawing && currentDiamond >= GachaController.Instance.SingleDrawCost;
        }

        if (drawTenButton != null)
        {
            drawTenButton.interactable = !isDrawing && currentDiamond >= GachaController.Instance.TenDrawCost;
        }
    }

    // 보유 다이아 수량 텍스트 UI 갱신
    private void UpdateDiamondUI(long diamond)
    {
        if (currentDiamondText != null)
        {
            currentDiamondText.text = FormatCurrencyNumber(diamond);
        }
    }

    // 대용량 숫자 단위 축약 포맷팅 반환
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

    #region 이벤트 수신 및 로그 뷰어 연산

    // 세이브된 가챠 로그 기록 스크롤뷰 복원
    public void RestoreSavedDrawLogs()
    {
        _logBuilder.Clear();

        if (GachaController.Instance != null)
        {
            List<GachaLogEntry> logs = GachaController.Instance.GetOrderedDrawLogs();
            for (int i = 0; i < logs.Count; i++)
            {
                GachaLogEntry entry = logs[i];
                if (entry == null) continue;

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

    // 가챠 뽑기 완료 이벤트 수신 및 결과 로그 추가
    private void OnGachaCompleted(GachaDrawCompletedEvent evt)
    {
        UpdatePityText();
        UpdateDrawButtonsInteractable();

        if (evt.resultItems == null) return;

        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        for (int i = 0; i < evt.resultItems.Count; i++)
        {
            GachaRewardItem item = evt.resultItems[i];
            if (item == null) continue;

            string colorHex = GetGradeColorHex(item.Grade);
            _logBuilder.AppendLine($"[{timestamp}] <color={colorHex}>[{(int)item.Grade}성] {item.DisplayName}</color>");
        }

        UpdateLogDisplay();
    }

    // 현재 누적 천장 스택 텍스트 갱신
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

    // 스크롤뷰 텍스트 갱신 및 스크롤 최하단 이동
    private void UpdateLogDisplay()
    {
        if (logContentText != null)
        {
            logContentText.text = _logBuilder.ToString();
        }

        if (logScrollRect != null)
        {
            logScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    // 가챠 로그 기록 전체 비우기
    public void ClearLog()
    {
        _logBuilder.Clear();
        if (logContentText != null)
        {
            logContentText.text = string.Empty;
        }

        if (GachaController.Instance != null)
        {
            GachaController.Instance.ClearDrawLogs();
        }
    }

    // 유닛 성 등급별 UI 강조 색상 반환
    private string GetGradeColorHex(UnitGrade grade)
    {
        return grade switch
        {
            UnitGrade.SixStar => "#FFD700",
            UnitGrade.FiveStar => "#FF4500",
            UnitGrade.FourStar => "#A335EE",
            UnitGrade.ThreeStar => "#0070DD",
            UnitGrade.TwoStar => "#1EFF00",
            _ => "#FFFFFF"
        };
    }

    #endregion
}
