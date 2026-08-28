using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 공방 UI 내 '제작 목록'에 동적으로 추가되는 개별 진행도 슬롯 프리팹 컴포넌트
public class UIWorkshopQueueItemSlot : MonoBehaviour
{
    #region 인스펙터 바인딩 필드

    [Header("1. 레시피 대표 이미지")]
    [Tooltip("등록된 레시피 아이템의 이미지를 표시하는 Image 컴포넌트")]
    [SerializeField] private Image recipeIconImage;

    [Header("2. 제작 진행도 슬라이더")]
    [Tooltip("해당 레시피의 실시간 제작 진행도를 나타내는 슬라이더")]
    [SerializeField] private Slider progressBar;

    [Header("3. 제작 진행도 텍스트")]
    [Tooltip("남은 시간 및 HOLD 상태를 표시하는 텍스트 (예: 2.1s 남음 또는 [재료 부족 대기])")]
    [SerializeField] private TMP_Text progressText;

    [Header("4. 편의 요소 (선택 사항)")]
    [Tooltip("등록된 아이템의 이름을 표시하는 텍스트 (선택 사항)")]
    [SerializeField] private TMP_Text itemNameText;

    [Tooltip("제작 목록에서 즉시 해제할 수 있는 [X] 버튼 (선택 사항)")]
    [SerializeField] private Button removeButton;

    #endregion

    #region 내부 변수 및 프로퍼티

    // 현재 슬롯에 바인딩된 레시피 인덱스
    private int _boundRecipeIndex = -1;
    private CraftingRecipeSO _recipeSO;
    private Action<int> _onRemoveCallback;

    public int BoundRecipeIndex => _boundRecipeIndex;
    public CraftingRecipeSO BoundRecipeSO => _recipeSO;

    #endregion

    #region 라이프사이클

    // 해제 버튼 클릭 리스너 등록
    private void Awake()
    {
        if (removeButton != null)
        {
            removeButton.onClick.AddListener(OnClickRemove);
        }
    }

    #endregion

    #region 데이터 바인딩 및 실시간 렌더링

    // ScriptableObject 기반 슬롯 데이터 및 아이콘 바인딩
    public void BindRecipe(CraftingRecipeSO recipeSO, int recipeIndex, Action<int> onRemoveCallback = null)
    {
        _boundRecipeIndex = recipeIndex;
        _recipeSO = recipeSO;
        _onRemoveCallback = onRemoveCallback;

        if (recipeSO == null) return;

        if (itemNameText != null)
        {
            itemNameText.text = recipeSO.DisplayName;
        }

        if (recipeIconImage != null)
        {
            Sprite icon = recipeSO.RecipeIcon;
            if (icon != null)
            {
                recipeIconImage.sprite = icon;
                recipeIconImage.enabled = true;
            }
            else
            {
                recipeIconImage.enabled = false;
            }
        }

        if (progressBar != null)
        {
            progressBar.value = 0f;
        }

        if (progressText != null)
        {
            progressText.text = "대기 중";
        }
    }

    // 실시간 제작 진행률 및 텍스트 갱신
    public void UpdateProgress(float normalizedProgress, float remainingTime, CraftingController.RecipeState state)
    {
        if (progressBar != null)
        {
            progressBar.value = normalizedProgress;
        }

        if (progressText != null)
        {
            if (state == CraftingController.RecipeState.Hold)
            {
                progressText.text = "<color=#FF4444>[재료 부족 대기]</color>";
            }
            else if (state == CraftingController.RecipeState.Crafting)
            {
                progressText.text = $"{remainingTime:F1}s 남음 ({normalizedProgress * 100:F0}%)";
            }
            else
            {
                progressText.text = "<color=#888888>대기 중</color>";
            }
        }
    }

    #endregion

    #region 버튼 핸들러

    // 슬롯 해제 버튼 클릭 이벤트 처리
    private void OnClickRemove()
    {
        if (_boundRecipeIndex >= 0)
        {
            if (_onRemoveCallback != null)
            {
                _onRemoveCallback.Invoke(_boundRecipeIndex);
            }
            else
            {
                CraftingController.Instance?.RemoveRecipeFromQueue(_boundRecipeIndex);
            }
        }
    }

    #endregion
}
