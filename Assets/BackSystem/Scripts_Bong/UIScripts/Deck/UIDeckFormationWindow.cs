using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIDeckFormationWindow : MonoBehaviour
{
    #region 직렬화 변수 (인스펙터 바인딩)

    [Header("--- 헤더 UI 요소 ---")]
    [Tooltip("창 닫기 버튼 (우측 상단 X 버튼)")]
    [SerializeField] private Button closeButton;

    [Header("--- 상단 덱 슬롯 UI (기존 DeckUI 컴포넌트) ---")]
    [Tooltip("상단 10개 덱 슬롯 유닛 초상화 표시를 담당하는 DeckUI 컴포넌트")]
    [SerializeField] private DeckUI topDeckUI;

    [Header("--- 하단 보유 유닛 스크롤 영역 ---")]
    [Tooltip("보유 유닛 목록 스크롤뷰 컴포넌트")]
    [SerializeField] private ScrollRect unitScrollRect;

    [Tooltip("보유 유닛 카드들이 생성될 부모 Content 트랜스폼")]
    [SerializeField] private Transform unitItemContent;

    [Tooltip("하단 스크롤 영역에 인스턴스화할 UICollectionItemCard 프리팹")]
    [SerializeField] private UICollectionItemCard unitCardPrefab;

    [Header("--- 중앙 하단 덱 종류 전환 컨트롤 ---")]
    [Tooltip("이전 덱 전환 버튼 (<)")]
    [SerializeField] private Button prevDeckButton;

    [Tooltip("다음 덱 전환 버튼 (>)")]
    [SerializeField] private Button nextDeckButton;

    [Tooltip("현재 선택된 덱 명칭 표시 TMP 텍스트 (일반 덱 / 레이드 1 / 레이드 2)")]
    [SerializeField] private TMP_Text deckTitleText;

    [Header("--- 우측 하단 액션 버튼 ---")]
    [Tooltip("선택된 유닛 등록/해제 액션 버튼")]
    [SerializeField] private Button actionButton;

    [Tooltip("등록/해제 버튼 내부 라벨 텍스트")]
    [SerializeField] private TMP_Text actionButtonText;

    [Tooltip("현재 덱의 모든 슬롯을 비우는 전체 해제 버튼")]
    [SerializeField] private Button clearAllButton;

    #endregion

    #region 내부 필드

    private DeckType _currentDeckType = DeckType.Normal;
    private readonly List<UICollectionItemCard> _spawnedCardPool = new List<UICollectionItemCard>();
    private List<CollectionItemViewModel> _cachedViewModels = new List<CollectionItemViewModel>();
    private CollectionItemViewModel _selectedViewModel;
    private string _selectedUnitKey = string.Empty;

    #endregion

    #region 라이프 사이클

    // 버튼 클릭 이벤트 및 초기 참조 바인딩
    private void Awake()
    {
        InitializeButtonListeners();
    }

    // 전역 덱 변경 및 데이터 이벤트 구독
    private void OnEnable()
    {
        EventBus.Subscribe<NormalDeckChangedEvent>(OnNormalDeckChanged);
        EventBus.Subscribe<RaidDeckChangedEvent>(OnRaidDeckChanged);
        EventBus.Subscribe<GachaDrawCompletedEvent>(OnGachaDrawCompleted);
        EventBus.Subscribe<DataLoadEvent>(OnDataLoaded);

        RefreshWindowUI();
    }

    // 전역 이벤트 구독 해제
    private void OnDisable()
    {
        EventBus.Unsubscribe<NormalDeckChangedEvent>(OnNormalDeckChanged);
        EventBus.Unsubscribe<RaidDeckChangedEvent>(OnRaidDeckChanged);
        EventBus.Unsubscribe<GachaDrawCompletedEvent>(OnGachaDrawCompleted);
        EventBus.Unsubscribe<DataLoadEvent>(OnDataLoaded);
    }

    #endregion

    #region 초기화 보조 메서드

    // UI 버튼 리스너 일괄 바인딩
    private void InitializeButtonListeners()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseWindow);
        }

        if (prevDeckButton != null)
        {
            prevDeckButton.onClick.AddListener(OnPrevDeckClicked);
        }

        if (nextDeckButton != null)
        {
            nextDeckButton.onClick.AddListener(OnNextDeckClicked);
        }

        if (actionButton != null)
        {
            actionButton.onClick.AddListener(OnActionButtonClicked);
        }

        if (clearAllButton != null)
        {
            clearAllButton.onClick.AddListener(OnClearAllClicked);
        }
    }

    #endregion

    #region 이벤트 수신 핸들러

    // 일반 덱 변경 이벤트 수신 처리
    private void OnNormalDeckChanged(NormalDeckChangedEvent evt)
    {
        if (_currentDeckType == DeckType.Normal)
        {
            RefreshTopDeckSlots();
            RefreshUnitScrollList();
            UpdateActionButtonState();
        }
    }

    // 레이드 덱 변경 이벤트 수신 처리
    private void OnRaidDeckChanged(RaidDeckChangedEvent evt)
    {
        if (_currentDeckType == evt.raidTeamType)
        {
            RefreshTopDeckSlots();
            RefreshUnitScrollList();
            UpdateActionButtonState();
        }
    }

    // 가챠 완료 시 보유 유닛 목록 갱신
    private void OnGachaDrawCompleted(GachaDrawCompletedEvent evt)
    {
        RefreshUnitScrollList();
        UpdateActionButtonState();
    }

    // 데이터 로드 완료 시 전체 UI 갱신
    private void OnDataLoaded(DataLoadEvent evt)
    {
        RefreshWindowUI();
    }

    #endregion

    #region 덱 종류 전환 제어

    // 이전 덱 전환 처리 (Normal -> Raid2 -> Raid1 -> Normal 순환)
    private void OnPrevDeckClicked()
    {
        _currentDeckType = _currentDeckType switch
        {
            DeckType.Normal => DeckType.Raid2,
            DeckType.Raid1 => DeckType.Normal,
            DeckType.Raid2 => DeckType.Raid1,
            _ => DeckType.Normal
        };

        OnDeckTypeChanged();
    }

    // 다음 덱 전환 처리 (Normal -> Raid1 -> Raid2 -> Normal 순환)
    private void OnNextDeckClicked()
    {
        _currentDeckType = _currentDeckType switch
        {
            DeckType.Normal => DeckType.Raid1,
            DeckType.Raid1 => DeckType.Raid2,
            DeckType.Raid2 => DeckType.Normal,
            _ => DeckType.Normal
        };

        OnDeckTypeChanged();
    }

    // 덱 종류 변경 시 UI 일괄 갱신
    private void OnDeckTypeChanged()
    {
        UpdateDeckTitleText();
        RefreshTopDeckSlots();
        RefreshUnitScrollList();
        UpdateActionButtonState();
    }

    // 중앙 하단 덱 제목 텍스트 갱신
    private void UpdateDeckTitleText()
    {
        if (deckTitleText != null)
        {
            deckTitleText.text = _currentDeckType switch
            {
                DeckType.Normal => "일반 덱",
                DeckType.Raid1 => "레이드 1",
                DeckType.Raid2 => "레이드 2",
                _ => "일반 덱"
            };
        }
    }

    #endregion

    #region 상단 덱 슬롯 UI 갱신

    // 상단 덱 슬롯 데이터 동기화
    private void RefreshTopDeckSlots()
    {
        if (topDeckUI == null || DeckManager.Instance == null) return;

        int[] currentSlots = DeckManager.Instance.GetDeckSlotsCopy(_currentDeckType);
        topDeckUI.UpdateDeckUI(currentSlots);
    }

    #endregion

    #region 하단 보유 유닛 스크롤 영역 갱신

    // 하단 보유 유닛 목록 스크롤뷰 갱신
    private void RefreshUnitScrollList()
    {
        if (CollectionDataProvider.Instance == null || unitItemContent == null || unitCardPrefab == null) return;

        _cachedViewModels = CollectionDataProvider.Instance.GetCollectionViewModels(_currentDeckType);

        // 현재 선택된 유닛 뷰모델 최신 데이터 동기화
        if (!string.IsNullOrEmpty(_selectedUnitKey))
        {
            _selectedViewModel = _cachedViewModels.Find(vm => vm.UnitId == _selectedUnitKey);
        }

        int totalCount = _cachedViewModels.Count;

        // 필요한 만큼 프리팹 인스턴스 확장 풀링
        while (_spawnedCardPool.Count < totalCount)
        {
            UICollectionItemCard newCard = Instantiate(unitCardPrefab, unitItemContent);
            newCard.Initialize(OnUnitCardClicked);
            _spawnedCardPool.Add(newCard);
        }

        // 전체 풀 순회하며 데이터 바인딩 및 활성화
        for (int i = 0; i < _spawnedCardPool.Count; i++)
        {
            UICollectionItemCard card = _spawnedCardPool[i];
            if (card == null) continue;

            if (i < totalCount)
            {
                CollectionItemViewModel vm = _cachedViewModels[i];
                card.gameObject.SetActive(true);
                card.Bind(vm);

                bool isSelected = !string.IsNullOrEmpty(_selectedUnitKey) && vm.UnitId == _selectedUnitKey;
                card.SetSelected(isSelected);
            }
            else
            {
                card.gameObject.SetActive(false);
            }
        }
    }

    // 보유 유닛 카드 클릭 콜백 처리
    private void OnUnitCardClicked(CollectionItemViewModel clickedViewModel)
    {
        if (clickedViewModel == null) return;

        _selectedViewModel = clickedViewModel;
        _selectedUnitKey = clickedViewModel.UnitId;

        UpdateCardSelectionVisuals();
        UpdateActionButtonState();
    }

    // 카드 슬롯들의 선택 하이라이트 시각화 동기화
    private void UpdateCardSelectionVisuals()
    {
        for (int i = 0; i < _spawnedCardPool.Count; i++)
        {
            UICollectionItemCard card = _spawnedCardPool[i];
            if (card == null || !card.gameObject.activeSelf || card.CurrentViewModel == null) continue;

            bool isSelected = !string.IsNullOrEmpty(_selectedUnitKey) && card.CurrentViewModel.UnitId == _selectedUnitKey;
            card.SetSelected(isSelected);
        }
    }

    #endregion

    #region 액션 버튼 제어 (등록 / 해제 / 전체 해제)

    // 등록/해제 버튼 클릭 처리
    private void OnActionButtonClicked()
    {
        if (_selectedViewModel == null || !_selectedViewModel.IsOwned) return;
        if (DeckManager.Instance == null) return;

        int unitId = UnitIdHelper.ParseUnitId(_selectedViewModel.UnitId);
        if (unitId <= 0) return;

        // 이미 현재 덱에 편성되어 있는 경우 -> 덱에서 해제
        if (_selectedViewModel.IsInDeck)
        {
            DeckManager.Instance.RemoveUnit(_currentDeckType, unitId);
        }
        // 미편성 상태인 경우 -> 현재 덱의 빈 슬롯에 자동 추가
        else
        {
            bool added = DeckManager.Instance.TryAddUnitToDeck(_currentDeckType, unitId, out int assignedIndex);
            if (!added)
            {
                Debug.LogWarning($"[UIDeckFormationWindow] {_currentDeckType} 슬롯이 가득 차서 유닛(ID: {unitId})을 추가할 수 없습니다.");
            }
        }
    }

    // 현재 덱 전체 슬롯 일괄 해제 처리
    private void OnClearAllClicked()
    {
        if (DeckManager.Instance == null) return;

        DeckManager.Instance.ClearDeck(_currentDeckType);
    }

    // 선택 유닛 상태에 따른 액션 버튼 인터랙션 및 텍스트 갱신
    private void UpdateActionButtonState()
    {
        if (actionButton == null) return;

        // 선택된 유닛이 없거나 미보유 유닛인 경우 비활성화
        if (_selectedViewModel == null || !_selectedViewModel.IsOwned)
        {
            actionButton.interactable = false;
            if (actionButtonText != null)
            {
                actionButtonText.text = (_selectedViewModel == null) ? "선택 필요" : "미보유";
            }
            return;
        }

        // 보유 유닛이 선택된 경우 활성화
        actionButton.interactable = true;

        if (actionButtonText != null)
        {
            actionButtonText.text = _selectedViewModel.IsInDeck ? "해제" : "등록";
        }
    }

    #endregion

    #region 전체 UI 갱신 및 창 제어

    // 전체 창 UI 동기화
    public void RefreshWindowUI()
    {
        UpdateDeckTitleText();
        RefreshTopDeckSlots();
        RefreshUnitScrollList();
        UpdateActionButtonState();
    }

    // 덱 편성 창 닫기 및 세이브 데이터 디스크 저장
    public void CloseWindow()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGameData(force: true);
        }

        gameObject.SetActive(false);
    }

    #endregion
}
