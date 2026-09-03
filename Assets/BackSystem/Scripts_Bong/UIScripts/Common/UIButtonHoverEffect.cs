using UnityEngine;
using UnityEngine.EventSystems;

// 버튼 마우스 호버 시 시각적 강조 효과 컴포넌트
public class UIButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    #region 직렬화 필드

    [Header("스케일 확대 설정")]
    [Tooltip("마우스 오버 시 크기 확대 효과 활성화 여부")]
    [SerializeField] private bool useScaleEffect = true;

    [Tooltip("마우스 호버 시 적용할 상대적 스케일 비율 (예: 1.08배)")]
    [SerializeField] private Vector3 hoverScale = new Vector3(1.08f, 1.08f, 1.0f);

    [Tooltip("스케일 보간 전환 속도")]
    [SerializeField] private float transitionSpeed = 15f;

    [Header("강조 이펙트 / 외곽선 오브젝트 (선택 사항)")]
    [Tooltip("마우스 호버 시 활성화할 하이라이트/아웃라인 게임오브젝트")]
    [SerializeField] private GameObject highlightEffectObject;

    #endregion

    #region 비공개 필드

    private Vector3 _originalScale;
    private Vector3 _targetScale;

    #endregion

    #region 라이프사이클

    // 초기 스케일 캐싱 및 강조 오브젝트 비활성화 초기화
    private void Awake()
    {
        _originalScale = transform.localScale;
        _targetScale = _originalScale;

        if (highlightEffectObject != null)
        {
            highlightEffectObject.SetActive(false);
        }
    }

    // 마우스 호버 상태에 따른 스케일 보간 갱신
    private void Update()
    {
        if (useScaleEffect)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.unscaledDeltaTime * transitionSpeed);
        }
    }

    // 비활성화 시 스케일 및 강조 오브젝트 상태 원복
    private void OnDisable()
    {
        transform.localScale = _originalScale;
        _targetScale = _originalScale;

        if (highlightEffectObject != null)
        {
            highlightEffectObject.SetActive(false);
        }
    }

    #endregion

    #region uGUI 이벤트 인터페이스 구현

    // 마우스 진입 시 스케일 확대 및 강조 오브젝트 활성화 처리
    public void OnPointerEnter(PointerEventData eventData)
    {
        _targetScale = Vector3.Scale(_originalScale, hoverScale);

        if (highlightEffectObject != null)
        {
            highlightEffectObject.SetActive(true);
        }
    }

    // 마우스 이탈 시 스케일 원복 및 강조 오브젝트 비활성화 처리
    public void OnPointerExit(PointerEventData eventData)
    {
        _targetScale = _originalScale;

        if (highlightEffectObject != null)
        {
            highlightEffectObject.SetActive(false);
        }
    }

    #endregion
}
