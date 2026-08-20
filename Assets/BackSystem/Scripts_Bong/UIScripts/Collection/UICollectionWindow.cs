using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("--- 하단 제어 버튼 ---")]
    [Tooltip("필터링 버튼")]
    [SerializeField] private Button filterButton;

    [Tooltip("세부 정보 버튼")]
    [SerializeField] private Button detailButton;

    #endregion

    #region 내부 필드

    private List<CollectionItemViewModel> _allViewModels = new List<CollectionItemViewModel>();
    private int _currentPage = 0;
    private const int ItemsPerPage = 5;

    #endregion

    #region 라이프 사이클 및 초기화

    // UI 버튼 이벤트 바인딩
    private void Awake()
    {
        if (closeButton != null) closeButton.onClick.AddListener(CloseWindow);
        if (prevPageButton != null) prevPageButton.onClick.AddListener(OnPrevPageClicked);
        if (nextPageButton != null) nextPageButton.onClick.AddListener(OnNextPageClicked);
    }

    // 이벤트 버스 구독 및 UI 갱신
    private void OnEnable()
    {
        EventBus.Subscribe<GachaDrawCompletedEvent>(OnGachaDrawCompleted);
        EventBus.Subscribe<NormalDeckChangedEvent>(OnDeckChanged);
        EventBus.Subscribe<RaidDeckChangedEvent>(OnRaidDeckChanged);
        RefreshCollectionUI();
    }

    // 이벤트 구독 해제 연산
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

    // 일반 덱 변경 시 UI 갱신
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

    #region UI 갱신 연산

    // 보관함 UI 및 슬롯 데이터 갱신
    public void RefreshCollectionUI()
    {
        if (CollectionDataProvider.Instance == null) return;

        _allViewModels = CollectionDataProvider.Instance.GetCollectionViewModels();

        int ownedCount = 0;
        for (int i = 0; i < _allViewModels.Count; i++)
        {
            if (_allViewModels[i].IsOwned) ownedCount++;
        }

        if (headerInfoText != null)
        {
            headerInfoText.text = $"OWNED OPERATORS {ownedCount} / {_allViewModels.Count}";
        }

        RenderCurrentPage();
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
                slot.gameObject.SetActive(true);
                slot.Bind(_allViewModels[targetIndex]);
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
