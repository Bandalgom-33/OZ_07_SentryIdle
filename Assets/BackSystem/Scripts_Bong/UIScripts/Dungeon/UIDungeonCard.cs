using System;
using EndlessGuard.Unit.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 단일 던전 슬롯 카드 UI 컴포넌트
public class UIDungeonCard : MonoBehaviour
{
    #region 직렬화 변수 (인스펙터 바인딩)

    [Header("--- 좌측 상단: 던전 정보 ---")]
    [Tooltip("던전 명칭 텍스트 (예: 고블린 지하 광산)")]
    [SerializeField] private TMP_Text dungeonNameText;

    [Header("--- 우측 상단: 전투력 정보 ---")]
    [Tooltip("현재 배치 유닛 총 전투력 / 던전 요구 최소 전투력 텍스트 (예: 140 / 50)")]
    [SerializeField] private TMP_Text combatPowerText;

    [Tooltip("초과 보너스 배율 텍스트 (예: (+180%))")]
    [SerializeField] private TMP_Text bonusText;

    [Header("--- 중앙: 던전 진행 슬라이더 ---")]
    [Tooltip("던전 1회 생산 주기 진행도 슬라이더")]
    [SerializeField] private Slider progressSlider;

    [Tooltip("남은 시간 표시 텍스트 (예: 15.2s)")]
    [SerializeField] private TMP_Text timeRemainingText;

    [Header("--- 좌측 하단: 배치 유닛 슬롯 3개 ---")]
    [Tooltip("배치된 3명 유닛의 초상화 이미지 배열")]
    [SerializeField] private Image[] unitPortraitImages = new Image[3];

    [Tooltip("미배치 빈 슬롯일 때 표시될 [+] 아이콘 게임오브젝트 배열")]
    [SerializeField] private GameObject[] emptyPlusIcons = new GameObject[3];

    [Tooltip("유닛 슬롯 영역 전체 터치 시 팀 편성 모달을 여는 버튼")]
    [SerializeField] private Button openFormationButton;

    [Header("--- 우측 하단: 1회 생산 보상 패널 ---")]
    [Tooltip("최종 골드 보상량 텍스트")]
    [SerializeField] private TMP_Text rewardGoldText;

    [Tooltip("최종 다이아 보상량 텍스트")]
    [SerializeField] private TMP_Text rewardDiamondText;

    [Tooltip("최종 스테이지 마석 보상량 텍스트")]
    [SerializeField] private TMP_Text rewardStoneText;

    [Header("--- 유닛 카탈로그 및 초상화 참조 ---")]
    [SerializeField] private UnitCatalog unitCatalog;
    [SerializeField] private UnitPortraitCatalogSO portraitCatalog;

    #endregion

    #region 내부 상태 필드

    private string _dungeonId = string.Empty;
    private Action<string> _onOpenFormationCallback;

    #endregion

    #region 라이프사이클 및 이벤트 바인딩

    // 카탈로그 로드 및 편성 버튼 리스너 등록
    private void Awake()
    {
        if (unitCatalog == null)
        {
            unitCatalog = CollectionDataProvider.Instance != null 
                ? CollectionDataProvider.Instance.UnitCatalog 
                : Resources.Load<UnitCatalog>("Catalogs/UnitCatalog");
        }

        if (portraitCatalog == null)
        {
            portraitCatalog = CollectionDataProvider.Instance != null 
                ? CollectionDataProvider.Instance.PortraitCatalog 
                : Resources.Load<UnitPortraitCatalogSO>("UnitPortraitCatalog");
        }

        if (openFormationButton != null)
        {
            openFormationButton.onClick.AddListener(OnClickOpenFormation);
        }
    }

    // 던전 실시간 이벤트 구독 등록
    private void OnEnable()
    {
        EventBus.Subscribe<DungeonProgressUpdatedEvent>(OnProgressUpdated);
        EventBus.Subscribe<DungeonFormationChangedEvent>(OnFormationChanged);
        EventBus.Subscribe<DungeonCycleCompletedEvent>(OnCycleCompleted);
    }

    // 던전 실시간 이벤트 구독 해제
    private void OnDisable()
    {
        EventBus.Unsubscribe<DungeonProgressUpdatedEvent>(OnProgressUpdated);
        EventBus.Unsubscribe<DungeonFormationChangedEvent>(OnFormationChanged);
        EventBus.Unsubscribe<DungeonCycleCompletedEvent>(OnCycleCompleted);
    }

    // 팀 편성 팝업 호출 콜백 실행
    private void OnClickOpenFormation()
    {
        if (!string.IsNullOrEmpty(_dungeonId))
        {
            _onOpenFormationCallback?.Invoke(_dungeonId);
        }
    }

    #endregion

    #region 데이터 바인딩 및 뷰 갱신

    // 던전 카드 초기 데이터 바인딩
    public void Bind(string dungeonId, Action<string> onOpenFormation)
    {
        _dungeonId = dungeonId;
        _onOpenFormationCallback = onOpenFormation;

        RefreshCardView();
    }

    // 던전 카드 전체 UI 정보 갱신
    public void RefreshCardView()
    {
        if (string.IsNullOrEmpty(_dungeonId) || DungeonManager.Instance == null) return;

        DungeonDataSO dataSO = DungeonManager.Instance.GetDungeonData(_dungeonId);
        if (dataSO == null) return;

        if (dungeonNameText != null)
        {
            dungeonNameText.text = dataSO.DungeonName;
        }

        int totalPower = DungeonManager.Instance.GetDungeonTotalPower(_dungeonId);
        int reqPower = dataSO.RequiredMinCombatPower;
        float bonusRatio = dataSO.CalculateBonusRatio(totalPower);
        bool isRunning = totalPower >= reqPower;

        if (combatPowerText != null)
        {
            combatPowerText.text = $"{totalPower} / {reqPower}";
            combatPowerText.color = isRunning ? Color.white : new Color(1f, 0.4f, 0.4f);
        }

        if (bonusText != null)
        {
            if (bonusRatio > 0.001f)
            {
                bonusText.text = $"(+{(bonusRatio * 100f):F0}% 보너스)";
                bonusText.gameObject.SetActive(true);
            }
            else
            {
                bonusText.gameObject.SetActive(false);
            }
        }

        int[] assignedUnits = DungeonManager.Instance.GetAssignedUnitIds(_dungeonId);
        for (int i = 0; i < unitPortraitImages.Length; i++)
        {
            int unitId = (assignedUnits != null && i < assignedUnits.Length) ? assignedUnits[i] : -1;
            bool isOccupied = unitId > 0;

            if (emptyPlusIcons != null && i < emptyPlusIcons.Length && emptyPlusIcons[i] != null)
            {
                emptyPlusIcons[i].SetActive(!isOccupied);
            }

            if (unitPortraitImages[i] != null)
            {
                if (isOccupied)
                {
                    string unitKey = $"UNIT_{unitId:D4}";
                    Sprite icon = (portraitCatalog != null) ? portraitCatalog.GetPortraitByUnitId(unitKey) : null;

                    if (icon != null)
                    {
                        unitPortraitImages[i].sprite = icon;
                        unitPortraitImages[i].enabled = true;
                    }
                    else
                    {
                        unitPortraitImages[i].enabled = false;
                    }
                }
                else
                {
                    unitPortraitImages[i].enabled = false;
                }
            }
        }

        long finalGold = dataSO.CalculateFinalGold(totalPower);
        long finalDia = dataSO.CalculateFinalDiamond(totalPower);
        long finalStone = dataSO.CalculateFinalStageStone(totalPower);

        if (rewardGoldText != null) rewardGoldText.text = $"+{finalGold:N0}";
        if (rewardDiamondText != null) rewardDiamondText.text = $"+{finalDia:N0}";
        if (rewardStoneText != null) rewardStoneText.text = $"+{finalStone:N0}";

        float timer = DungeonManager.Instance.GetCurrentCycleTimer(_dungeonId);
        float progress = Mathf.Clamp01(timer / dataSO.BaseCycleSeconds);
        if (progressSlider != null)
        {
            progressSlider.value = isRunning ? progress : 0.0f;
        }

        if (timeRemainingText != null)
        {
            if (isRunning)
            {
                float remain = Mathf.Max(0.0f, dataSO.BaseCycleSeconds - timer);
                timeRemainingText.text = $"{remain:F1}s";
            }
            else
            {
                timeRemainingText.text = "전투력 부족";
            }
        }
    }

    #endregion

    #region 이벤트 수신 핸들러

    // 실시간 생산 진행 슬라이더 갱신 처리
    private void OnProgressUpdated(DungeonProgressUpdatedEvent evt)
    {
        if (evt.dungeonId != _dungeonId) return;

        if (progressSlider != null)
        {
            progressSlider.value = evt.progressRatio;
        }

        if (timeRemainingText != null)
        {
            timeRemainingText.text = evt.isRunning ? $"{evt.remainingSeconds:F1}s" : "전투력 부족";
        }
    }

    // 편성 변경 이벤트 수신 갱신 처리
    private void OnFormationChanged(DungeonFormationChangedEvent evt)
    {
        if (evt.dungeonId == _dungeonId)
        {
            RefreshCardView();
        }
    }

    // 1회 생산 완료 및 보상 획득 갱신 처리
    private void OnCycleCompleted(DungeonCycleCompletedEvent evt)
    {
        if (evt.dungeonId == _dungeonId)
        {
            RefreshCardView();
        }
    }

    #endregion
}
