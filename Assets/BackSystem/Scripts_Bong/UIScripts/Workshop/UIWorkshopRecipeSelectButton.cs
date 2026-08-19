using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 공방 레시피 목록에서 특정 레시피를 선택하기 위한 버튼 프리팹 컴포넌트
[RequireComponent(typeof(Button))]
public class UIWorkshopRecipeSelectButton : MonoBehaviour
{
    #region 인스펙터 바인딩 필드

    [Header("레시피 설정")]
    [Tooltip("레시피 인덱스 번호")]
    [SerializeField] private int recipeIndex;

    [Tooltip("레시피 대표 아이콘/이미지를 렌더링하는 내부 Image 컴포넌트")]
    [SerializeField] private Image recipeImage;

    [Tooltip("현재 선택되었을 때 강조 표시할 테두리/오브젝트 (선택 사항)")]
    [SerializeField] private GameObject selectedHighlightObject;

    [Header("잠금(미해금) UI 요소")]
    [Tooltip("미해금 시 활성화되는 잠금 오버레이 오브젝트 (선택 사항)")]
    [SerializeField] private GameObject lockedOverlayObject;

    [Tooltip("미해금 시 표시할 필요 레벨 텍스트 (예: Lv.2, 선택 사항)")]
    [SerializeField] private TMP_Text lockedLevelText;

    #endregion

    #region 이벤트 및 내부 변수

    // 버튼 클릭 시 선택된 레시피 인덱스를 전달하는 이벤트
    public event Action<int> OnRecipeSelected;

    private Button _button;
    private CraftingRecipeSO _recipeSO;
    private bool _isLocked = false;

    public int RecipeIndex => recipeIndex;
    public Image RecipeImage => recipeImage;
    public CraftingRecipeSO RecipeSO => _recipeSO;
    public bool IsLocked => _isLocked;

    #endregion

    #region 라이프사이클

    // 버튼 클릭 리스너 등록
    private void Awake()
    {
        _button = GetComponent<Button>();
        if (_button != null)
        {
            _button.onClick.AddListener(HandleClick);
        }
    }

    #endregion

    #region 데이터 바인딩 및 렌더링

    // ScriptableObject 기반 데이터 바인딩
    public void BindRecipeSO(CraftingRecipeSO recipeSO, int index, Action<int> onSelectCallback = null)
    {
        _recipeSO = recipeSO;
        recipeIndex = index;

        if (onSelectCallback != null)
        {
            OnRecipeSelected = onSelectCallback;
        }

        if (recipeImage != null && recipeSO != null)
        {
            if (recipeSO.recipeIcon != null)
            {
                recipeImage.sprite = recipeSO.recipeIcon;
                recipeImage.enabled = true;
            }
            else
            {
                recipeImage.enabled = true;
            }
        }
    }

    // 버튼 클릭 이벤트 핸들러
    private void HandleClick()
    {
        OnRecipeSelected?.Invoke(recipeIndex);
    }

    // 선택 상태 강조 하이라이트 설정
    public void SetSelected(bool isSelected)
    {
        if (selectedHighlightObject != null)
        {
            selectedHighlightObject.SetActive(isSelected);
        }
    }

    // 잠금 상태 및 필요 레벨 텍스트 갱신
    public void SetLocked(bool isLocked, int requiredLevel)
    {
        _isLocked = isLocked;

        if (lockedOverlayObject != null)
        {
            lockedOverlayObject.SetActive(isLocked);
        }

        if (lockedLevelText != null)
        {
            lockedLevelText.text = isLocked ? $"Lv.{requiredLevel}" : string.Empty;
            lockedLevelText.gameObject.SetActive(isLocked);
        }

        if (recipeImage != null)
        {
            recipeImage.color = isLocked ? new Color(0.4f, 0.4f, 0.4f, 0.7f) : Color.white;
        }
    }

    // 레시피 인덱스 번호 설정
    public void SetRecipeIndex(int index)
    {
        recipeIndex = index;
    }

    #endregion
}
