using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬이 시작되거나 변경될 때 하이어라키(DontDestroyOnLoad 씬 포함)에서 지정된 이름의 오브젝트를 자동으로 탐색하여 할당하고,
/// 버튼 클릭 이벤트를 통해 해당 오브젝트를 활성화(또는 토글)하는 UI 브릿지 컴포넌트입니다.
/// </summary>
[RequireComponent(typeof(Button))]
public class OptionWindowOpener : MonoBehaviour
{
    #region 직렬화 필드 (Inspector 설정)

    [Header("--- 탐색 대상 설정 ---")]
    [Tooltip("하이어라키에서 찾을 대상 오브젝트 이름 (예: 옵션창)")]
    [SerializeField] private string targetObjectName = "옵션창";

    [Header("--- 동작 옵션 ---")]
    [Tooltip("버튼 클릭 시 이미 열려있으면 닫을지(토글), 아니면 무조건 활성화만 할지 여부")]
    [SerializeField] private bool toggleOnButtonClick = true;

    [Tooltip("옵션창이 활성화될 때 게임을 일시정지(배속 0)할지 여부")]
    [SerializeField] private bool pauseGameOnOpen = false;

    #endregion

    #region 내부 변수

    // 런타임에 탐색하여 할당된 대상 오브젝트 캐싱 변수
    private GameObject _targetObject;

    // 본 오브젝트의 버튼 컴포넌트
    private Button _button;

    #endregion

    #region 라이프사이클

    private void Awake()
    {
        // 버튼 컴포넌트 참조 확보 및 리스너 등록
        _button = GetComponent<Button>();
        if (_button != null)
        {
            // 중복 리스너 등록 방지를 위해 기존 등록 제거 후 등록
            _button.onClick.RemoveListener(OnClickButton);
            _button.onClick.AddListener(OnClickButton);
        }

        // 초기 시작 씬에서 대상 오브젝트 탐색 시도
        ResolveTargetObject();
    }

    private void OnEnable()
    {
        // 씬이 전환되어 새로 시작될 때도 다시 하이어라키에서 대상을 탐색할 수 있도록 씬 로드 이벤트 등록
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        // 메모리 누수 방지 및 이벤트 중복 호출 차단
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    #endregion

    #region 씬 변경 처리 및 오브젝트 탐색

    /// <summary>
    /// 새로운 씬이 로드되었을 때 호출되어 대상을 재탐색합니다.
    /// </summary>
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 새 씬 진입 시 대상 재할당 진행
        ResolveTargetObject();
    }

    /// <summary>
    /// 하이어라키 전역(DontDestroyOnLoad 씬 포함)에서 비활성화된 오브젝트까지 포함하여 탐색 및 할당합니다.
    /// </summary>
    public void ResolveTargetObject()
    {
        // 1. 만약 이미 유효한 참조가 있다면 그대로 유지
        if (_targetObject != null) return;

        if (string.IsNullOrEmpty(targetObjectName))
        {
            Debug.LogWarning("[OptionWindowOpener] targetObjectName이 설정되지 않았습니다.");
            return;
        }

        // 2. SoundManager 싱글톤이 존재할 경우 SoundManager 하위 자식들을 우선 탐색 (비활성화 포함)
        if (SoundManager.Instance != null)
        {
            Transform[] soundChildren = SoundManager.Instance.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < soundChildren.Length; i++)
            {
                if (soundChildren[i] != null && soundChildren[i].name == targetObjectName)
                {
                    _targetObject = soundChildren[i].gameObject;
                    Debug.Log($"[OptionWindowOpener] SoundManager 하위에서 '{targetObjectName}'을(를) 찾아 할당했습니다.");
                    return;
                }
            }
        }

        // 3. 하이어라키 전역에서 비활성화된 오브젝트까지 포함하여 탐색 (비활성화 오브젝트 포함)
        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform t = allTransforms[i];

            // 프리팹 에셋(프로젝트 뷰)이나 씬 외부에 존재하는 에셋은 제외하고 실제 씬 하이어라키에 속한 것만 필터링
            if (t != null && t.gameObject.scene.isLoaded && t.name == targetObjectName)
            {
                _targetObject = t.gameObject;
                Debug.Log($"[OptionWindowOpener] 씬 하이어라키에서 '{targetObjectName}'을(를) 찾아 할당했습니다.");
                return;
            }
        }

        Debug.LogWarning($"[OptionWindowOpener] 하이어라키에서 '{targetObjectName}' 이름의 오브젝트를 찾지 못했습니다. 대상이 생성된 이후 다시 시도해야 할 수 있습니다.");
    }

    #endregion

    #region 버튼 클릭 및 활성화 제어

    /// <summary>
    /// 버튼을 클릭했을 때 대상 오브젝트의 활성화 상태를 제어합니다.
    /// </summary>
    public void OnClickButton()
    {
        // 참조가 누락되었거나 파괴된 경우 클릭 시점에 재탐색 시도
        if (_targetObject == null)
        {
            ResolveTargetObject();
        }

        if (_targetObject == null)
        {
            Debug.LogError($"[OptionWindowOpener] '{targetObjectName}' 오브젝트가 없어 열 수 없습니다.");
            return;
        }

        // 토글 옵션 활성화 시 현재 상태 반전, 비활성화 시 무조건 true
        bool nextState = toggleOnButtonClick ? !_targetObject.activeSelf : true;
        _targetObject.SetActive(nextState);

        // 옵션창이 열렸을 때 일시정지 옵션이 켜져있다면 게임 배속을 0으로 설정
        if (nextState && pauseGameOnOpen)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetGameSpeed(0);
            }
            else
            {
                Time.timeScale = 0f;
            }
        }
    }

    #endregion
}
