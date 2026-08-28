using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

// 캐릭터 보관함 팝업 UI 및 유닛 컬렉션 조회/일반 덱 장착 해제를 총괄하는 윈도우 컨트롤러
public class UICollectionWindow : MonoBehaviour
{
    #region 직렬화 변수 (인스펙터 바인딩)

    [Header("--- 헤더 UI 요소 ---")]
    [Tooltip("상단 보유 유닛 카운트/정보 텍스트")]
    [SerializeField] private TMP_Text headerInfoText;

    [Tooltip("패널 닫기 버튼")]
    [SerializeField] private Button closeButton;

    [Header("--- 5열 카드 그리드 슬롯 리스트 ---")]
    [Tooltip("화면에 노출할 5개 카드 슬롯 컴포넌트 배열 (5열 레이아웃)")]
    [SerializeField] private UICollectionItemCard[] cardSlots = new UICollectionItemCard[5];

    [Header("--- 하단 페이지네이션 컨트롤러 ---")]
    [Tooltip("이전 페이지 버튼 (<)")]
    [SerializeField] private Button prevPageButton;

    [Tooltip("다음 페이지 버튼 (>)")]
    [SerializeField] private Button nextPageButton;

    [Tooltip("페이지 번호 표기 텍스트 (예: 1 / 2)")]
    [SerializeField] private TMP_Text pageText;

    [Header("장비 관리 버튼")]
    [Tooltip("선택된 캐릭터 장비창 오픈 버튼")]
    [SerializeField] private Button equipActionButton;

    [Tooltip("장비 관리 버튼 텍스트")]
    [SerializeField] private TMP_Text equipActionButtonText;

    [Tooltip("장비 인벤토리 패널 오브젝트")]
    [SerializeField] private GameObject equipmentInventoryPanel;

    #endregion

    #region 내부 필드

    // 보관함 전체 유닛 뷰모델 목록
    private List<CollectionItemViewModel> _allViewModels = new List<CollectionItemViewModel>();

    // 현재 선택된 유닛 뷰모델 캐시
    private CollectionItemViewModel _selectedViewModel;

    // 현재 선택된 유닛 식별 키 (페이지 전환 시에도 선택 유지를 위함)
    private string _selectedUnitKey = string.Empty;

    // 페이지네이션 현재 페이지 인덱스 (0-based)
    private int _currentPage = 0;

    // 1페이지당 노출 카드 수량 (5열 단일 행 고정)
    private const int ItemsPerPage = 5;

    #endregion

    #region 라이프 사이클 및 초기화

    // UI 버튼 이벤트 바인딩 및 카드 슬롯 초기화
    private void Awake()
    {
        // 팝업 닫기 및 페이지 이동 버튼 리스너 바인딩
        if (closeButton != null) closeButton.onClick.AddListener(CloseWindow);
        if (prevPageButton != null) prevPageButton.onClick.AddListener(OnPrevPageClicked);
        if (nextPageButton != null) nextPageButton.onClick.AddListener(OnNextPageClicked);

        // 장비 관리 버튼 리스너 등록
        if (equipActionButton != null)
        {
            equipActionButton.onClick.AddListener(OnEquipActionButtonClicked);

            if (equipActionButtonText == null)
            {
                equipActionButtonText = equipActionButton.GetComponentInChildren<TMP_Text>();
            }
        }

        // 5개 카드 슬롯에 클릭 콜백 바인딩
        for (int i = 0; i < cardSlots.Length; i++)
        {
            if (cardSlots[i] != null)
            {
                cardSlots[i].Initialize(OnCardSlotClicked);
            }
        }
    }

    // 이벤트 버스 구독 및 UI 갱신
    private void OnEnable()
    {
        EventBus.Subscribe<GachaDrawCompletedEvent>(OnGachaDrawCompleted);
        EventBus.Subscribe<NormalDeckChangedEvent>(OnDeckChanged);
        EventBus.Subscribe<RaidDeckChangedEvent>(OnRaidDeckChanged);

        // 창이 활성화될 때 최신 유닛 정보 및 덱 편성 상태로 UI를 갱신
        RefreshCollectionUI();
    }

    // 이벤트 구독 해제 연산 (메모리 누수 방지)
    private void OnDisable()
    {
        EventBus.Unsubscribe<GachaDrawCompletedEvent>(OnGachaDrawCompleted);
        EventBus.Unsubscribe<NormalDeckChangedEvent>(OnDeckChanged);
        EventBus.Unsubscribe<RaidDeckChangedEvent>(OnRaidDeckChanged);
    }

    // 가챠 완료 시 UI 갱신
    private void OnGachaDrawCompleted(GachaDrawCompletedEvent evt)
    {
        RefreshCollectionUI();
    }

    // 일반 덱 변경 시 UI 갱신 (덱 뱃지 및 액션 버튼 상태 동기화)
    private void OnDeckChanged(NormalDeckChangedEvent evt)
    {
        RefreshCollectionUI();
    }

    // 레이드 덱 변경 시 UI 갱신
    private void OnRaidDeckChanged(RaidDeckChangedEvent evt)
    {
        RefreshCollectionUI();
    }

    #endregion

    #region 카드 선택 및 덱 조작 로직

    // 카드 슬롯 클릭 이벤트 수신 핸들러
    private void OnCardSlotClicked(CollectionItemViewModel clickedViewModel)
    {
        if (clickedViewModel == null) return;

        // 선택된 유닛 정보 갱신
        _selectedViewModel = clickedViewModel;
        _selectedUnitKey = clickedViewModel.UnitId;

        // 현재 화면에 노출된 슬롯들의 선택 하이라이트 동기화
        UpdateCardSlotsSelection();

        // 선택된 유닛의 상태에 맞추어 장비 관리 버튼 상태 갱신
        UpdateEquipActionButtonState();
    }

    // 장비 관리 버튼 클릭 시 유효성 검사 및 장비 UI 오픈 처리
    private void OnEquipActionButtonClicked()
    {
        if (_selectedViewModel == null)
        {
            Debug.LogWarning("[UICollectionWindow] 장착할 대상 유닛이 선택되지 않았습니다.");
            return;
        }

        if (!_selectedViewModel.IsOwned)
        {
            Debug.LogWarning($"[UICollectionWindow] 미보유 유닛({_selectedViewModel.DisplayName})은 장비를 장착할 수 없습니다.");
            return;
        }

        if (EquipmentManager.Instance != null)
        {
            EquipmentManager.Instance.SetCurrentUnit(_selectedViewModel.UnitId);
        }

        if (equipmentInventoryPanel != null)
        {
            equipmentInventoryPanel.SetActive(true);
        }
    }

    // 유닛 선택 상태에 따른 장비 버튼 활성화 및 텍스트 갱신 처리
    private void UpdateEquipActionButtonState()
    {
        if (equipActionButton == null) return;

        if (_selectedViewModel == null || !_selectedViewModel.IsOwned)
        {
            equipActionButton.interactable = false;
            if (equipActionButtonText != null)
            {
                equipActionButtonText.text = (_selectedViewModel == null) ? "선택 필요" : "미보유";
            }
            return;
        }

        equipActionButton.interactable = true;

        if (equipActionButtonText != null)
        {
            equipActionButtonText.text = "장비 관리";
        }
    }

    // 카드 슬롯들의 선택 인디케이터 시각화 갱신
    private void UpdateCardSlotsSelection()
    {
        for (int i = 0; i < cardSlots.Length; i++)
        {
            UICollectionItemCard slot = cardSlots[i];
            if (slot == null || slot.CurrentViewModel == null) continue;

            // 현재 선택된 유닛 식별자와 일치하는 카드만 하이라이트 활성화
            bool isSelected = !string.IsNullOrEmpty(_selectedUnitKey) && slot.CurrentViewModel.UnitId == _selectedUnitKey;
            slot.SetSelected(isSelected);
        }
    }

    #endregion

    #region UI 갱신 연산

    // 보관함 UI 및 슬롯 데이터 갱신
    public void RefreshCollectionUI()
    {
        if (CollectionDataProvider.Instance == null) return;

        // 최신 뷰모델 목록 가져오기 (일반 덱 장착 정보 포함)
        _allViewModels = CollectionDataProvider.Instance.GetCollectionViewModels(DeckType.Normal);

        int ownedCount = 0;
        for (int i = 0; i < _allViewModels.Count; i++)
        {
            if (_allViewModels[i].IsOwned) ownedCount++;
        }

        // 상단 보유 현황 텍스트 갱신
        if (headerInfoText != null)
        {
            headerInfoText.text = $"OWNED OPERATORS {ownedCount} / {_allViewModels.Count}";
        }

        // 이전에 선택된 유닛이 있다면 최신 데이터로 동기화
        if (!string.IsNullOrEmpty(_selectedUnitKey))
        {
            _selectedViewModel = _allViewModels.Find(vm => vm.UnitId == _selectedUnitKey);
        }

        // 현재 페이지 카드 슬롯 렌더링
        RenderCurrentPage();

        // 장비 관리 버튼 상태 동기화
        UpdateEquipActionButtonState();
    }

    // 현재 페이지 슬롯 렌더링
    private void RenderCurrentPage()
    {
        int totalItems = _allViewModels.Count;
        int maxPages = Mathf.Max(1, Mathf.CeilToInt((float)totalItems / ItemsPerPage));

        _currentPage = Mathf.Clamp(_currentPage, 0, maxPages - 1);

        if (pageText != null)
        {
            pageText.text = $"{_currentPage + 1} / {maxPages}";
        }

        if (prevPageButton != null) prevPageButton.interactable = _currentPage > 0;
        if (nextPageButton != null) nextPageButton.interactable = _currentPage < maxPages - 1;

        int startIndex = _currentPage * ItemsPerPage;

        for (int i = 0; i < cardSlots.Length; i++)
        {
            UICollectionItemCard slot = cardSlots[i];
            if (slot == null) continue;

            int targetIndex = startIndex + i;
            if (targetIndex < totalItems)
            {
                CollectionItemViewModel vm = _allViewModels[targetIndex];
                slot.gameObject.SetActive(true);
                slot.Bind(vm);

                // 선택 상태 동기화
                bool isSelected = !string.IsNullOrEmpty(_selectedUnitKey) && vm.UnitId == _selectedUnitKey;
                slot.SetSelected(isSelected);
            }
            else
            {
                slot.gameObject.SetActive(false);
            }
        }
    }

    #endregion

    #region 페이지 버튼 이벤트

    // 이전 페이지 이동 처리
    private void OnPrevPageClicked()
    {
        if (_currentPage > 0)
        {
            _currentPage--;
            RenderCurrentPage();
        }
    }

    // 다음 페이지 이동 처리
    private void OnNextPageClicked()
    {
        int maxPages = Mathf.CeilToInt((float)_allViewModels.Count / ItemsPerPage);
        if (_currentPage < maxPages - 1)
        {
            _currentPage++;
            RenderCurrentPage();
        }
    }

    // 창 닫기 연산
    public void CloseWindow()
    {
        gameObject.SetActive(false);
    }

    #endregion
}
