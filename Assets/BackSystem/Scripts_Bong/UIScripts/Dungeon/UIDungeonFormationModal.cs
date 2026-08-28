using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 던전 슬롯 UI 바인딩 데이터 구조
[Serializable]
public class DungeonModalSlotView
{
    [Tooltip("슬롯 루트 오브젝트")]
    public GameObject slotRoot;
    [Tooltip("장착된 유닛이 있을 때 활성화될 루트")]
    public GameObject occupiedRoot;
    [Tooltip("빈 슬롯일 때 활성화될 [+] 루트")]
    public GameObject emptyRoot;
    [Tooltip("유닛 초상화 이미지")]
    public Image portraitImage;
    [Tooltip("유닛 이름 텍스트")]
    public TMP_Text unitNameText;
    [Tooltip("유닛 스탯 텍스트 (Lv.30 / 2돌 / 전투력 90)")]
    public TMP_Text statInfoText;
    [Tooltip("슬롯 해제 [X] 버튼")]
    public Button removeButton;
}

// 던전 유닛 일괄 편성 및 최강 자동 배치 팝업 컨트롤러
public class UIDungeonFormationModal : MonoBehaviour
{
    #region 직렬화 변수 (인스펙터 바인딩)

    [Header("--- 헤더 및 상태 정보 ---")]
    [Tooltip("던전 이름 텍스트")]
    [SerializeField] private TMP_Text dungeonTitleText;

    [Tooltip("총 전투력 / 요구 전투력 텍스트 (예: 140 / 50)")]
    [SerializeField] private TMP_Text combatPowerText;

    [Tooltip("가동 상태 및 보너스 배율 텍스트 (예: 생산 가동 중 (+180% 보너스))")]
    [SerializeField] private TMP_Text bonusStatusText;

    [Header("--- 상단 3개 던전 슬롯 뷰 ---")]
    [SerializeField] private DungeonModalSlotView[] slotViews = new DungeonModalSlotView[3];

    [Header("--- 제어 버튼 ---")]
    [Tooltip("⚡ 최강 전투력 자동 배치 버튼")]
    [SerializeField] private Button autoAssignButton;

    [Tooltip("전체 슬롯 비우기 버튼")]
    [SerializeField] private Button clearAllButton;

    [Tooltip("팝업 닫기 버튼")]
    [SerializeField] private Button closeButton;

    [Header("--- 하단 보유 유닛 목록 스크롤 뷰 ---")]
    [Tooltip("유닛 카드들이 인스턴스화될 컨테이너 트랜스폼")]
    [SerializeField] private Transform unitCardContainer;

    [Tooltip("보유 유닛 카드 프리팹")]
    [SerializeField] private UIDungeonFormationUnitCard unitCardPrefab;

    [Header("--- 유닛 카탈로그 및 초상화 참조 ---")]
    [SerializeField] private UnitCatalog unitCatalog;
    [SerializeField] private UnitPortraitCatalogSO portraitCatalog;

    #endregion

    #region 내부 상태 필드

    private string _currentDungeonId = string.Empty;
    private readonly List<UIDungeonFormationUnitCard> _instantiatedCards = new List<UIDungeonFormationUnitCard>();

    #endregion

    #region 라이프사이클 및 이벤트 등록

    // 카탈로그 로드 및 버튼 리스너 등록
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

        if (autoAssignButton != null)
        {
            autoAssignButton.onClick.AddListener(OnClickAutoAssign);
        }

        if (clearAllButton != null)
        {
            clearAllButton.onClick.AddListener(OnClickClearAll);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseModal);
        }

        for (int i = 0; i < slotViews.Length; i++)
        {
            int slotIdx = i;
            if (slotViews[slotIdx] != null && slotViews[slotIdx].removeButton != null)
            {
                slotViews[slotIdx].removeButton.onClick.AddListener(() => OnClickRemoveSlot(slotIdx));
            }
        }
    }

    // 편성 변경 이벤트 구독 등록
    private void OnEnable()
    {
        EventBus.Subscribe<DungeonFormationChangedEvent>(OnDungeonFormationChanged);
    }

    // 편성 변경 이벤트 구독 해제
    private void OnDisable()
    {
        EventBus.Unsubscribe<DungeonFormationChangedEvent>(OnDungeonFormationChanged);
    }

    #endregion

    #region 모달 열기 / 닫기

    // 팀 편성 모달 오픈 및 UI 갱신
    public void OpenModal(string dungeonId)
    {
        _currentDungeonId = dungeonId;
        gameObject.SetActive(true);
        RefreshUI();
    }

    // 팀 편성 모달 닫기
    public void CloseModal()
    {
        gameObject.SetActive(false);
    }

    #endregion

    #region 이벤트 수신 및 UI 갱신

    // 편성 변경 이벤트 수신 갱신 처리
    private void OnDungeonFormationChanged(DungeonFormationChangedEvent evt)
    {
        if (gameObject.activeSelf)
        {
            RefreshUI();
        }
    }

    // 상단 3개 슬롯 및 하단 유닛 목록 전체 갱신
    public void RefreshUI()
    {
        if (string.IsNullOrEmpty(_currentDungeonId) || DungeonManager.Instance == null) return;

        DungeonDataSO dataSO = DungeonManager.Instance.GetDungeonData(_currentDungeonId);
        if (dataSO == null) return;

        if (dungeonTitleText != null)
        {
            dungeonTitleText.text = $"{dataSO.DungeonName} - 유닛 파견 편성";
        }

        int totalPower = DungeonManager.Instance.GetDungeonTotalPower(_currentDungeonId);
        int reqPower = dataSO.RequiredMinCombatPower;
        float bonusRatio = dataSO.CalculateBonusRatio(totalPower);
        bool isRunning = totalPower >= reqPower;

        if (combatPowerText != null)
        {
            combatPowerText.text = $"총 전투력: {totalPower} / 요구 {reqPower}";
            combatPowerText.color = isRunning ? Color.green : Color.red;
        }

        if (bonusStatusText != null)
        {
            if (isRunning)
            {
                bonusStatusText.text = $"생산 가동 중 (+{(bonusRatio * 100f):F0}% 보너스)";
                bonusStatusText.color = Color.cyan;
            }
            else
            {
                bonusStatusText.text = "전투력 부족 (생산 정지)";
                bonusStatusText.color = Color.gray;
            }
        }

        int[] assignedUnits = DungeonManager.Instance.GetAssignedUnitIds(_currentDungeonId);
        for (int i = 0; i < slotViews.Length; i++)
        {
            DungeonModalSlotView sv = slotViews[i];
            if (sv == null) continue;

            int unitId = (assignedUnits != null && i < assignedUnits.Length) ? assignedUnits[i] : -1;
            bool isOccupied = unitId > 0;

            if (sv.occupiedRoot != null) sv.occupiedRoot.SetActive(isOccupied);
            if (sv.emptyRoot != null) sv.emptyRoot.SetActive(!isOccupied);

            if (isOccupied)
            {
                string unitKey = $"UNIT_{unitId:D4}";
                string uName = unitKey;
                Sprite icon = (portraitCatalog != null) ? portraitCatalog.GetPortraitByUnitId(unitKey) : null;

                if (unitCatalog != null && unitCatalog.TryGetById(unitKey, out UnitDataSO so) && so != null)
                {
                    uName = so.DisplayName;
                }

                int power = DungeonManager.Instance.GetUnitCombatPower(unitId);

                if (sv.portraitImage != null && icon != null)
                {
                    sv.portraitImage.sprite = icon;
                    sv.portraitImage.enabled = true;
                }
                else if (sv.portraitImage != null)
                {
                    sv.portraitImage.enabled = false;
                }

                if (sv.unitNameText != null) sv.unitNameText.text = uName;
                if (sv.statInfoText != null) sv.statInfoText.text = $"전투력: {power}";
            }
        }

        RefreshUnitCardList();
    }

    // 보유 유닛 목록 전투력 순 정렬 및 카드 렌더링
    private void RefreshUnitCardList()
    {
        if (unitCardContainer == null || unitCardPrefab == null || unitCatalog == null) return;

        IReadOnlyList<UnitDataSO> allUnits = unitCatalog.Units;
        if (allUnits == null || allUnits.Count == 0) return;

        List<(UnitDataSO dataSO, int unitId, int power, string assignedDungeon)> unitInfoList = new List<(UnitDataSO, int, int, string)>();

        for (int i = 0; i < allUnits.Count; i++)
        {
            UnitDataSO so = allUnits[i];
            if (so == null) continue;

            int uId = ParseUnitId(so.UnitId);
            if (uId <= 0) continue;

            // 미보유 유닛은 던전 파견 편성 목록에서 제외 (보유 유닛만 노출)
            if (DungeonManager.Instance != null && !DungeonManager.Instance.IsUnitOwned(uId))
            {
                continue;
            }

            int power = DungeonManager.Instance != null ? DungeonManager.Instance.GetUnitCombatPower(uId) : 0;
            string assignedDungeon = DungeonManager.Instance != null ? DungeonManager.Instance.GetAssignedDungeonId(uId, out _) : null;

            unitInfoList.Add((so, uId, power, assignedDungeon));
        }

        unitInfoList.Sort((a, b) =>
        {
            bool aIsCurrent = a.assignedDungeon == _currentDungeonId;
            bool bIsCurrent = b.assignedDungeon == _currentDungeonId;
            if (aIsCurrent != bIsCurrent) return bIsCurrent.CompareTo(aIsCurrent);

            bool aUnassigned = string.IsNullOrEmpty(a.assignedDungeon);
            bool bUnassigned = string.IsNullOrEmpty(b.assignedDungeon);
            if (aUnassigned != bUnassigned) return bUnassigned.CompareTo(aUnassigned);

            return b.power.CompareTo(a.power);
        });

        for (int i = 0; i < unitInfoList.Count; i++)
        {
            var info = unitInfoList[i];
            UIDungeonFormationUnitCard card;

            if (i < _instantiatedCards.Count)
            {
                card = _instantiatedCards[i];
                card.gameObject.SetActive(true);
            }
            else
            {
                card = Instantiate(unitCardPrefab, unitCardContainer);
                _instantiatedCards.Add(card);
            }

            Sprite icon = (portraitCatalog != null) ? portraitCatalog.GetPortraitByUnitId(info.dataSO.UnitId) : null;
            bool isAssigned = !string.IsNullOrEmpty(info.assignedDungeon);

            card.Bind(
                info.unitId,
                info.dataSO.DisplayName,
                icon,
                info.power,
                isAssigned,
                OnClickUnitCard
            );
        }

        for (int i = unitInfoList.Count; i < _instantiatedCards.Count; i++)
        {
            _instantiatedCards[i].gameObject.SetActive(false);
        }
    }

    #endregion

    #region 유저 조작 핸들러

    // 보유 유닛 카드 클릭 시 장착 및 해제 처리
    private void OnClickUnitCard(int unitId)
    {
        if (string.IsNullOrEmpty(_currentDungeonId) || DungeonManager.Instance == null) return;

        string assignedDungeon = DungeonManager.Instance.GetAssignedDungeonId(unitId, out int slotIdx);

        if (assignedDungeon == _currentDungeonId)
        {
            DungeonManager.Instance.RemoveUnitFromSlot(_currentDungeonId, slotIdx);
            return;
        }

        if (DungeonManager.Instance.TryAddUnitToDungeon(_currentDungeonId, unitId, out _))
        {
            return;
        }

        DungeonManager.Instance.AssignUnitToSlot(_currentDungeonId, 2, unitId);
    }

    // 개별 슬롯 유닛 해제 처리
    private void OnClickRemoveSlot(int slotIndex)
    {
        if (!string.IsNullOrEmpty(_currentDungeonId) && DungeonManager.Instance != null)
        {
            DungeonManager.Instance.RemoveUnitFromSlot(_currentDungeonId, slotIndex);
        }
    }

    // 최강 전투력 자동 배치 실행
    private void OnClickAutoAssign()
    {
        if (!string.IsNullOrEmpty(_currentDungeonId) && DungeonManager.Instance != null)
        {
            DungeonManager.Instance.AutoAssignHighestPowerUnits(_currentDungeonId);
        }
    }

    // 전체 슬롯 비우기 실행
    private void OnClickClearAll()
    {
        if (!string.IsNullOrEmpty(_currentDungeonId) && DungeonManager.Instance != null)
        {
            DungeonManager.Instance.ClearDungeonSlots(_currentDungeonId);
        }
    }

    #endregion

    #region 내부 헬퍼

    // 유닛 문자열 키 정수 ID 변환
    private int ParseUnitId(string unitKey)
    {
        if (string.IsNullOrEmpty(unitKey)) return -1;
        if (int.TryParse(unitKey.Replace("UNIT_", ""), out int id))
        {
            return id;
        }
        return -1;
    }

    #endregion
}
