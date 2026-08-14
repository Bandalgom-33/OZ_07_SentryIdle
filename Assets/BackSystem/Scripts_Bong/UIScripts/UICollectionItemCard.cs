using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UICollectionItemCard : MonoBehaviour
{
    #region 직렬화 변수

    [Header("--- UI 컴포넌트 바인딩 ---")]
    [Tooltip("유닛 초상화 이미지")]
    [SerializeField] private Image portraitImage;

    [Tooltip("유닛 이름 텍스트 (예: UNIT F 또는 김하진)")]
    [SerializeField] private TMP_Text unitNameText;

    [Tooltip("보유 상태 텍스트 (AVAILABLE / LOCKED)")]
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

    #region 바인딩 메서드

    // 컬렉션 아이템 뷰모델 데이터 바인딩
    public void Bind(CollectionItemViewModel viewModel)
    {
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
            statusText.text = viewModel.IsOwned ? "AVAILABLE" : "LOCKED";
            statusText.color = viewModel.IsOwned ? Color.cyan : Color.gray;
        }

        if (deckBadgeText != null)
        {
            deckBadgeText.gameObject.SetActive(viewModel.IsInDeck);

            if (viewModel.IsInDeck)
            {
                deckBadgeText.text = $"DECK {viewModel.DeckSlotIndex + 1}";
            }
        }
    }

    #endregion
}
