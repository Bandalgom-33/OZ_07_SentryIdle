using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Runtime;
using UnityEngine;

// 3D 씬 환경에서 SPUM 2D 캐릭터 스프라이트를 카메라를 향하도록 정렬하고,
// 그리드 상의 이동/바라보는 방향(East/West)에 맞춰 스프라이트의 좌우 플립(Scale X)을 제어하는 컴포넌트입니다.
[DisallowMultipleComponent]
public sealed class SpumBillboard : MonoBehaviour
{
    [Header("빌보드 대상")]
    [Tooltip("카메라 방향으로 회전할 Transform. 비워두면 CombatEntityAnchors.VisualRoot → 이름 탐색 순으로 자동 설정.")]
    [SerializeField] private Transform visualRoot;

    [Tooltip("East/West 방향 전환 시 localScale.x 부호를 적용할 Transform. 비워두면 VisualRoot 직계 첫 자식을 자동 사용.")]
    [SerializeField] private Transform facingTarget;

    [Header("카메라")]
    [Tooltip("사용할 카메라. 비워두면 Camera.main을 자동 사용.")]
    [SerializeField] private Camera overrideCamera;

    [Header("빌보드 설정")]
    [Tooltip("true: Y축만 회전하여 캐릭터가 수직을 유지. false: 카메라 방향에 완전 정렬.")]
    [SerializeField] private bool yAxisOnly = true;

    [Tooltip("true: 기본 스프라이트 좌우 반전(기본 방향이 반대인 프리팹용).")]
    [SerializeField] private bool invertFacing;

    // 엔티티의 그리드 위치 및 방향 변경 이벤트를 감지하기 위한 참조
    private CombatGridPosition gridPosition;

    // 빌보드 연산의 기준이 되는 렌더링 카메라 캐시
    private Camera activeCamera;

    // 기본 방향 부호
    private float facingSign = 1f;

    // OnFacingChanged 이벤트의 중복 등록 방지를 위한 구독 플래그
    private bool subscribed;

    // 필수 컴포넌트(visualRoot 등)의 유효성이 검증되었는지 나타내는 플래그
    private bool isReady;

    // 컴포넌트 생성 시 필요한 자식 Transform 및 부착된 컴포넌트 참조를 탐색하고 유효성을 검증합니다.
    private void Awake()
    {
        ResolveReferences();
    }

    // 오브젝트 활성화 시 참조 재검증, 이벤트 등록 및 현재 그리드 방향을 즉시 스케일에 반영합니다.
    // 풀링(Object Pooling) 환경에서 재사용될 때 이전 상태를 올바르게 갱신하기 위해 필수적입니다.
    private void OnEnable()
    {
        ResolveReferences();
        Subscribe();

        // 초기화 완료 상태라면 즉시 현재 방향 부호 계산
        if (gridPosition != null && gridPosition.IsInitialized)
        {
            ApplyFacingSign(gridPosition.FacingDirection);
        }
    }

    // 비활성화 시 이벤트 리스너를 해제하여 메모리 누수 및 미사용 객체로의 콜백 호출을 방지합니다.
    private void OnDisable()
    {
        Unsubscribe();
    }

    // 카메라 이동 및 애니메이션 처리가 완료된 후 최종 변환(Transform)을 적용하기 위해 LateUpdate를 사용합니다.
    // Update에서 처리할 경우 카메라 위치 갱신과의 프레임 불일치로 인한 떨림(Jittering)이 발생할 수 있습니다.
    private void LateUpdate()
    {
        // 필수 참조가 없으면 불필요한 연산 중단
        if (!isReady)
        {
            return;
        }

        // 활성 카메라가 없거나 파괴된 경우 폴백 카메라(Camera.main)를 획득
        if (activeCamera == null)
        {
            activeCamera = overrideCamera != null ? overrideCamera : Camera.main;

            if (activeCamera == null)
            {
                return; // 씬에 활성화된 카메라가 없을 때는 연산 건너뜀
            }
        }

        if (visualRoot == null)
        {
            isReady = false;
            return;
        }

        // 1단계: 카메라를 향하도록 회전 적용
        ApplyBillboard();

        // 2단계: 바라보는 방향(East/West)에 따른 Scale X 플립 적용
        ApplyFacingFlip();
    }

    // 빌보드 대상 Transform과 컴포넌트 참조를 단계별(VisualRoot -> CombatEntityAnchors -> Find)로 안전하게 바인딩합니다.
    private void ResolveReferences()
    {
        // 1단계: VisualRoot 참조 탐색 (앵커 컴포넌트 우선 -> 자식 검색 순)
        if (visualRoot == null)
        {
            CombatEntityAnchors anchors = GetComponent<CombatEntityAnchors>();

            if (anchors != null && anchors.VisualRoot != null)
            {
                visualRoot = anchors.VisualRoot;
            }
            else
            {
                visualRoot = transform.Find("VisualRoot");
            }
        }

        // VisualRoot를 끝내 찾지 못한 경우 오류 출력 후 동작 비활성화
        if (visualRoot == null)
        {
            Debug.LogError($"[SpumBillboard] {name}: VisualRoot를 찾을 수 없습니다. Inspector에서 직접 할당하거나 CombatEntityAnchors를 추가하세요.", this);
            isReady = false;
            return;
        }

        // 2단계: 플립 대상 Transform 지정 (기본값: VisualRoot의 첫 번째 자식 SPUM 유닛)
        if (facingTarget == null && visualRoot.childCount > 0)
        {
            facingTarget = visualRoot.GetChild(0);
        }

        // 3단계: 그리드 방향 연동 컴포넌트 참조
        if (gridPosition == null)
        {
            gridPosition = GetComponent<CombatGridPosition>();
        }

        if (overrideCamera != null)
        {
            activeCamera = overrideCamera;
        }

        isReady = true;
    }

    // 그리드 방향 변경 이벤트를 구독하여 실시간 방향 전환을 반영합니다.
    // 중복 구독 방지를 위해 subscribed 플래그를 체크합니다.
    private void Subscribe()
    {
        if (subscribed || gridPosition == null)
        {
            return;
        }

        gridPosition.OnFacingChanged += HandleFacingChanged;
        subscribed = true;
    }

    // 구독했던 OnFacingChanged 이벤트를 안전하게 해제합니다.
    private void Unsubscribe()
    {
        if (!subscribed)
        {
            return;
        }

        if (gridPosition != null)
        {
            gridPosition.OnFacingChanged -= HandleFacingChanged;
        }

        subscribed = false;
    }

    // 엔티티의 그리드 방향 변경 이벤트 수신 시 방향 부호를 갱신합니다.
    private void HandleFacingChanged(CombatGridPosition changedPosition)
    {
        if (changedPosition == gridPosition)
        {
            ApplyFacingSign(changedPosition.FacingDirection);
        }
    }

    // 스프라이트 방향 부호 계산
    private void ApplyFacingSign(GridFacingDirection facing)
    {
        float sign = facing == GridFacingDirection.East ? 1f : -1f;
        facingSign = invertFacing ? -sign : sign;
    }

    // 빌보드 회전 적용
    private void ApplyBillboard()
    {
        if (yAxisOnly)
        {
            visualRoot.rotation = Quaternion.Euler(0f, activeCamera.transform.eulerAngles.y, 0f);
        }
        else
        {
            visualRoot.rotation = activeCamera.transform.rotation;
        }
    }

    // 스프라이트 좌우 반전 적용
    private void ApplyFacingFlip()
    {
        if (facingTarget == null)
        {
            return;
        }

        Vector3 scale = facingTarget.localScale;
        float absX = Mathf.Abs(scale.x);

        if (absX < 0.0001f)
        {
            return;
        }

        float sign = facingSign;

        if (gridPosition == null && invertFacing)
        {
            sign = -1f;
        }

        scale.x = absX * sign;
        facingTarget.localScale = scale;
    }
}
