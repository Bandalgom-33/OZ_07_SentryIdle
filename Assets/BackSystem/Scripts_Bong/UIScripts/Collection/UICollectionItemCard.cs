using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 개별 유닛 컬렉션 카드 UI 요소 표시 및 클릭 인터랙션을 담당하는 슬롯 컴포넌트
public class UICollectionItemCard : MonoBehaviour
{
    #region 직렬화 변수

    [Header("--- UI 컴포넌트 바인딩 ---")]
    [Tooltip("카드 전체 클릭 감지를 위한 버튼 컴포넌트")]
    [SerializeField] private Button cardButton;

    [Tooltip("유닛 선택 시 활성화되는 테두리/하이라이트 게임오브젝트")]
    [SerializeField] private GameObject selectedIndicator;

    [Tooltip("유닛 초상화 이미지")]
    [SerializeField] private Image portraitImage;

    [Tooltip("유닛 이름 텍스트 (예: UNIT F 또는 김하진)")]
    [SerializeField] private TMP_Text unitNameText;

    [Tooltip("보유 상태 및 레벨 텍스트 (보유 시 Lv.X, 미보유 시 LOCKED)")]
    [SerializeField] private TMP_Text statusText;

    [Tooltip("덱 배치 뱃지 텍스트 (예: DECK 1)")]
    [SerializeField] private TMP_Text deckBadgeText;

    [Tooltip("유닛 성급/돌파 표기 텍스트")]
    [SerializeField] private TMP_Text gradeText;

    [Header("--- 색상 및 시각 효과 ---")]
    [Tooltip("보유 중일 때 초상화 색상")]
    [SerializeField] private Color ownedColor = Color.white;

    [Tooltip("미획득일 때 초상화 실루엣 색상")]
    [SerializeField] private Color lockedColor = new Color(0.3f, 0.3f, 0.3f, 0.7f);

    #endregion

    #region 내부 필드 및 이벤트

    private CollectionItemViewModel _cachedViewModel;
    private Action<CollectionItemViewModel> _onClickCallback;
    private bool _isSelected;

    #endregion

    #region 프로퍼티

    public CollectionItemViewModel CurrentViewModel => _cachedViewModel;
    public bool IsSelected => _isSelected;

    #endregion

    #region 라이프사이클 및 초기화

    // 버튼 클릭 이벤트 리스너 등록
    private void Awake()
    {
        if (cardButton == null)
        {
            cardButton = GetComponent<Button>();
        }

        if (cardButton != null)
        {
            cardButton.onClick.AddListener(OnCardClicked);
        }
    }

    // 카드 슬롯 초기화 및 클릭 콜백 연결
    public void Initialize(Action<CollectionItemViewModel> onClickCallback)
    {
        _onClickCallback = onClickCallback;
    }

    // 카드 클릭 시 등록된 상위 콜백 호출
    private void OnCardClicked()
    {
        if (_cachedViewModel != null)
        {
            _onClickCallback?.Invoke(_cachedViewModel);
        }
    }

    #endregion

    #region 바인딩 및 선택 상태 제어

    // 컬렉션 아이템 뷰모델 데이터 바인딩
    public void Bind(CollectionItemViewModel viewModel)
    {
        _cachedViewModel = viewModel;

        if (viewModel == null) return;

        if (unitNameText != null)
        {
            unitNameText.text = !string.IsNullOrEmpty(viewModel.DisplayName) ? viewModel.DisplayName : viewModel.UnitId;
        }

        if (gradeText != null)
        {
            gradeText.text = $"{viewModel.Grade}";
        }

        if (portraitImage != null)
        {
            if (viewModel.PortraitIcon != null)
            {
                portraitImage.sprite = viewModel.PortraitIcon;
                portraitImage.enabled = true;
            }
            else
            {
                portraitImage.enabled = false;
            }

            portraitImage.color = viewModel.IsOwned ? ownedColor : lockedColor;
        }

        if (statusText != null)
        {
            statusText.text = viewModel.IsOwned ? $"Lv.{viewModel.Level}" : "LOCKED";
            statusText.color = viewModel.IsOwned ? Color.cyan : Color.gray;
        }

        if (deckBadgeText != null)
        {
            deckBadgeText.gameObject.SetActive(viewModel.IsInDeck);

            if (viewModel.IsInDeck)
            {
                string prefix = viewModel.CurrentDeckType switch
                {
                    DeckType.Normal => "DECK",
                    DeckType.Raid1 => "RAID 1",
                    DeckType.Raid2 => "RAID 2",
                    _ => "DECK"
                };
                deckBadgeText.text = $"{prefix} {viewModel.DeckSlotIndex + 1}";
            }
        }
    }

    // 유닛 카드 선택 상태 하이라이트 갱신
    public void SetSelected(bool isSelected)
    {
        _isSelected = isSelected;

        if (selectedIndicator != null)
        {
            selectedIndicator.SetActive(isSelected);
        }
    }

    // 유닛 레벨 및 상태 텍스트 단독 갱신
    public void UpdateLevel(int newLevel)
    {
        if (_cachedViewModel != null)
        {
            _cachedViewModel.Level = newLevel;
            if (statusText != null && _cachedViewModel.IsOwned)
            {
                statusText.text = $"Lv.{newLevel}";
            }
        }
    }

    #endregion
}
