using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// 던전 캐릭터 이미지 슬롯 카드 UI 컴포넌트
public class UIDungeonCharacterImageCard : MonoBehaviour
{
    #region 직렬화 변수 (인스펙터 바인딩)

    [Header("--- UI 구성 요소 ---")]
    [Tooltip("카드 클릭 상호작용을 처리할 버튼 컴포넌트")]
    [SerializeField] private Button cardButton;

    [Tooltip("캐릭터 외형/초상화 스프라이트가 렌더링될 이미지 컴포넌트")]
    [SerializeField] private Image characterImage;

    [Tooltip("캐릭터가 배치되지 않았을 때(빈 상태) 화면에 표시할 트랜스폼/오브젝트 (예: [+] 아이콘 루트)")]
    [SerializeField] private Transform emptyTransform;

    [Tooltip("슬롯이 선택되었을 때 활성화할 시각적 테두리/하이라이트 게임오브젝트")]
    [SerializeField] private GameObject selectedBorderObject;

    [Header("--- 동작 옵션 ---")]
    [Tooltip("카드 클릭 시 내부적으로 선택 테두리 상태를 자동으로 토글할지 여부")]
    [SerializeField] private bool autoToggleSelectionOnClick = true;

    [Header("--- 유니티 이벤트 ---")]
    [Tooltip("카드 클릭 시 발생하는 기본 유니티 이벤트 (인스펙터 바인딩용)")]
    public UnityEvent OnCardClicked;

    [Tooltip("카드 클릭 시 자기 자신(컴포넌트)을 매개변수로 전달하는 유니티 이벤트")]
    public UnityEvent<UIDungeonCharacterImageCard> OnCardSelectedWithSelf;

    #endregion

    #region 내부 상태 및 프로퍼티

    private bool _hasCharacter = false;

    public bool HasCharacter => _hasCharacter;
    public bool IsSelected => selectedBorderObject != null && selectedBorderObject.activeSelf;
    public Button CardButton => cardButton;

    #endregion

    #region 유니티 생명주기 (Lifecycle)

    // 컴포넌트 초기화 및 버튼 리스너 등록
    private void Awake()
    {
        if (cardButton == null)
        {
            cardButton = GetComponent<Button>();
        }

        if (cardButton != null)
        {
            cardButton.onClick.AddListener(HandleCardClick);
        }
    }

    // 버튼 이벤트 리스너 해제
    private void OnDestroy()
    {
        if (cardButton != null)
        {
            cardButton.onClick.RemoveListener(HandleCardClick);
        }
    }

    #endregion

    #region 사용자 인터랙션 처리 (클릭 이벤트)

    // 카드 클릭 인터랙션 핸들링 처리
    private void HandleCardClick()
    {
        if (autoToggleSelectionOnClick && selectedBorderObject != null)
        {
            bool nextState = !selectedBorderObject.activeSelf;
            selectedBorderObject.SetActive(nextState);
        }

        OnCardClicked?.Invoke();
        OnCardSelectedWithSelf?.Invoke(this);
    }

    #endregion

    #region 외부 제어 공개 API (Public Interface)

    // 캐릭터 스프라이트 설정 및 슬롯 활성화 처리
    public void SetCharacter(Sprite sprite)
    {
        if (sprite == null)
        {
            SetEmpty();
            return;
        }

        _hasCharacter = true;

        if (characterImage != null)
        {
            characterImage.sprite = sprite;
            characterImage.enabled = true;
        }

        if (emptyTransform != null)
        {
            emptyTransform.gameObject.SetActive(false);
        }
    }

    // 캐릭터 이미지 해제 및 빈 슬롯 전환 처리
    public void SetEmpty()
    {
        _hasCharacter = false;

        if (characterImage != null)
        {
            characterImage.sprite = null;
            characterImage.enabled = false;
        }

        if (emptyTransform != null)
        {
            emptyTransform.gameObject.SetActive(true);
        }
    }

    // 선택 하이라이트 테두리 활성화 여부 설정
    public void SetSelected(bool isSelected)
    {
        if (selectedBorderObject != null)
        {
            selectedBorderObject.SetActive(isSelected);
        }
    }

    // 버튼 상호작용 활성화 여부 설정
    public void SetInteractable(bool interactable)
    {
        if (cardButton != null)
        {
            cardButton.interactable = interactable;
        }
    }

    #endregion
}
