using System;
using EndlessGuard.Unit.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 던전 카드 UI 컴포넌트
public class UIDungeonCard : MonoBehaviour
{
    #region 직렬화 변수 (인스펙터 바인딩)

    [Header("--- 좌측: 던전 기본 정보 ---")]
    [Tooltip("던전 대표 썸네일 이미지")]
    [SerializeField] private Image dungeonIconImage;

    [Tooltip("던전 명칭 텍스트 (예: 고블린 지하 광산)")]
    [SerializeField] private TMP_Text dungeonNameText;

    [Header("--- 상단: 가동 상태 표시 ---")]
    [Tooltip("생산 정상 가동 중(전투력 충족)일 때 활성화할 긍정 상태 오브젝트/아이콘")]
    [SerializeField] private GameObject statusPositiveObject;

    [Tooltip("생산 정지(전투력 부족)일 때 활성화할 부정 상태 오브젝트/아이콘")]
    [SerializeField] private GameObject statusNegativeObject;

    [Tooltip("던전 가동 상태 설명 텍스트 (예: 생산 가동 중 / 전투력 부족)")]
    [SerializeField] private TMP_Text statusText;

    [Header("--- 중앙: 전투력 및 진행도 ---")]
    [Tooltip("현재 배치 총 전투력 / 요구 최소 전투력 텍스트 (예: 140 / 50)")]
    [SerializeField] private TMP_Text combatPowerText;

    [Tooltip("전투력 초과 보너스 배율 텍스트 (예: (+180%))")]
    [SerializeField] private TMP_Text bonusText;

    [Tooltip("1회 생산 주기 진행도 슬라이더")]
    [SerializeField] private Slider progressSlider;

    [Tooltip("남은 생산 시간 표시 텍스트 (예: 15.2s)")]
    [SerializeField] private TMP_Text timeRemainingText;

    [Header("--- 하단: 배치 유닛 캐릭터 이미지 카드 슬롯 ---")]
    [Tooltip("던전에 배치된 유닛의 초상화 및 빈 슬롯을 표시할 캐릭터 이미지 카드 배열")]
    [SerializeField] private UIDungeonCharacterImageCard[] unitCharacterCards = new UIDungeonCharacterImageCard[3];

    [Header("--- 유닛 편성 모달 호출 버튼 (2종) ---")]
    [Tooltip("카드 프리팹 루트 전체를 터치했을 때 팀 편성을 여는 버튼")]
    [SerializeField] private Button rootCardButton;

    [Tooltip("하단 편성 변경 전용 버튼 (프리팹 하단 [편성 변경 버튼])")]
    [SerializeField] private Button bottomFormationButton;

    [Header("--- 우측 하단: 1회 생산 보상 패널 ---")]
    [Tooltip("최종 골드 보상량 텍스트")]
    [SerializeField] private TMP_Text rewardGoldText;

    [Tooltip("최종 다이아 보상량 텍스트")]
    [SerializeField] private TMP_Text rewardDiamondText;

    [Tooltip("최종 스테이지 마석 보상량 텍스트")]
    [SerializeField] private TMP_Text rewardStoneText;

    #endregion

    #region 내부 상태 필드

    private string _dungeonId = string.Empty;
    private Action<string> _onOpenFormationCallback;

    #endregion

    #region 유니티 생명주기 및 이벤트 바인딩

    // 컴포넌트 초기화 및 버튼 리스너 등록
    private void Awake()
    {
        if (rootCardButton == null)
        {
            rootCardButton = GetComponent<Button>();
        }

        if (rootCardButton != null)
        {
            rootCardButton.onClick.AddListener(OnClickOpenFormation);
        }

        if (bottomFormationButton != null)
        {
            bottomFormationButton.onClick.AddListener(OnClickOpenFormation);
        }

        if (unitCharacterCards != null)
        {
            for (int i = 0; i < unitCharacterCards.Length; i++)
            {
                if (unitCharacterCards[i] != null)
                {
                    unitCharacterCards[i].OnCardClicked.AddListener(OnClickOpenFormation);
                }
            }
        }
    }

    // 버튼 이벤트 리스너 해제
    private void OnDestroy()
    {
        if (rootCardButton != null)
        {
            rootCardButton.onClick.RemoveListener(OnClickOpenFormation);
        }

        if (bottomFormationButton != null)
        {
            bottomFormationButton.onClick.RemoveListener(OnClickOpenFormation);
        }

        if (unitCharacterCards != null)
        {
            for (int i = 0; i < unitCharacterCards.Length; i++)
            {
                if (unitCharacterCards[i] != null)
                {
                    unitCharacterCards[i].OnCardClicked.RemoveListener(OnClickOpenFormation);
                }
            }
        }
    }

    // 던전 관련 전역 이벤트 구독 등록
    private void OnEnable()
    {
        EventBus.Subscribe<DungeonProgressUpdatedEvent>(OnProgressUpdated);
        EventBus.Subscribe<DungeonFormationChangedEvent>(OnFormationChanged);
        EventBus.Subscribe<DungeonCycleCompletedEvent>(OnCycleCompleted);
    }

    // 던전 관련 전역 이벤트 구독 해제
    private void OnDisable()
    {
        EventBus.Unsubscribe<DungeonProgressUpdatedEvent>(OnProgressUpdated);
        EventBus.Unsubscribe<DungeonFormationChangedEvent>(OnFormationChanged);
        EventBus.Unsubscribe<DungeonCycleCompletedEvent>(OnCycleCompleted);
    }

    // 팀 편성 모달 오픈 콜백 호출 처리
    private void OnClickOpenFormation()
    {
        if (!string.IsNullOrEmpty(_dungeonId))
        {
            _onOpenFormationCallback?.Invoke(_dungeonId);
        }
    }

    #endregion

    #region 데이터 바인딩 및 뷰 갱신

    // 던전 식별자 및 콜백 기반 카드 데이터 바인딩
    public void Bind(string dungeonId, Action<string> onOpenFormation)
    {
        _dungeonId = dungeonId;
        _onOpenFormationCallback = onOpenFormation;
        RefreshCardView();
    }

    // 던전 카드의 전체 UI 정보 갱신
    public void RefreshCardView()
    {
        if (string.IsNullOrEmpty(_dungeonId) || DungeonManager.Instance == null) return;

        DungeonDataSO dataSO = DungeonManager.Instance.GetDungeonData(_dungeonId);
        if (dataSO == null) return;

        if (dungeonIconImage != null && dataSO.DungeonIcon != null)
        {
            dungeonIconImage.sprite = dataSO.DungeonIcon;
            dungeonIconImage.enabled = true;
        }

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

        if (statusPositiveObject != null)
        {
            statusPositiveObject.SetActive(isRunning);
        }

        if (statusNegativeObject != null)
        {
            statusNegativeObject.SetActive(!isRunning);
        }

        if (statusText != null)
        {
            if (isRunning)
            {
                statusText.text = "가동중";
                statusText.color = Color.cyan;
            }
            else
            {
                statusText.text = "전투력 부족";
                statusText.color = Color.gray;
            }
        }

        RefreshUnitSlots();

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

    // 배치된 유닛 초상화 슬롯 목록 갱신
    private void RefreshUnitSlots()
    {
        if (unitCharacterCards == null || unitCharacterCards.Length == 0) return;

        int[] assignedUnits = DungeonManager.Instance != null 
            ? DungeonManager.Instance.GetAssignedUnitIds(_dungeonId) 
            : null;

        UnitPortraitCatalogSO portraitCatalog = CollectionDataProvider.Instance != null 
            ? CollectionDataProvider.Instance.PortraitCatalog 
            : null;

        for (int i = 0; i < unitCharacterCards.Length; i++)
        {
            UIDungeonCharacterImageCard card = unitCharacterCards[i];
            if (card == null) continue;

            int unitId = (assignedUnits != null && i < assignedUnits.Length) ? assignedUnits[i] : -1;
            bool isOccupied = unitId > 0;

            if (isOccupied)
            {
                string unitKey = $"UNIT_{unitId:D4}";
                Sprite portrait = portraitCatalog != null ? portraitCatalog.GetPortraitByUnitId(unitKey) : null;
                card.SetCharacter(portrait);
            }
            else
            {
                card.SetEmpty();
            }
        }
    }

    #endregion

    #region 실시간 이벤트 수신 핸들러

    // 생산 진행도 갱신 이벤트 수신 처리
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

    // 던전 편성 변경 이벤트 수신 처리
    private void OnFormationChanged(DungeonFormationChangedEvent evt)
    {
        if (evt.dungeonId == _dungeonId)
        {
            RefreshCardView();
        }
    }

    // 생산 주기 완료 이벤트 수신 처리
    private void OnCycleCompleted(DungeonCycleCompletedEvent evt)
    {
        if (evt.dungeonId == _dungeonId)
        {
            RefreshCardView();
        }
    }

    #endregion
}
