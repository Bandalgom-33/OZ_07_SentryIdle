using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class GuideStep
{
    [Header("--- 가이드 단계 설정 ---")]
    [Tooltip("가이드 단계 식별용 명칭")]
    public string stepName;

    [Tooltip("해당 단계에서 하이라이트할 대상 버튼")]
    public Button targetButton;

    [Tooltip("가이드 제목 텍스트")]
    public string titleText;

    [TextArea(3, 6)]
    [Tooltip("가이드 설명 문구")]
    public string descriptionText;
}

public class GuideManager : MonoBehaviour
{
    #region 싱글톤 인스턴스

    public static GuideManager Instance { get; private set; }

    #endregion

    #region 직렬화 변수

    [Header("--- 가이드 UI 요소 ---")]
    [Tooltip("가이드 전체 루트 패널 오브젝트")]
    [SerializeField] private GameObject guidePanel;

    [Tooltip("가이드 제목 표시 TMP 텍스트")]
    [SerializeField] private TMP_Text titleText;

    [Tooltip("가이드 설명 표시 TMP 텍스트")]
    [SerializeField] private TMP_Text descriptionText;

    [Tooltip("가이드 진행 단계 표시 TMP 텍스트")]
    [SerializeField] private TMP_Text stepIndicatorText;

    [Tooltip("대상 버튼 위치를 따라다니는 하이라이트 프레임 RectTransform")]
    [SerializeField] private RectTransform highlightFrame;

    [Header("--- 가이드 단계 목록 ---")]
    [Tooltip("순서대로 진행될 가이드 단계 목록")]
    [SerializeField] private List<GuideStep> guideSteps = new List<GuideStep>();

    [Header("--- 연출 설정 ---")]
    [Tooltip("하이라이트 프레임 패딩 여백")]
    [SerializeField] private Vector2 highlightPadding = new Vector2(20f, 20f);

    [Tooltip("하이라이트 프레임 펄스 최소 스케일")]
    [SerializeField] private float pulseMinScale = 0.95f;

    [Tooltip("하이라이트 프레임 펄스 최대 스케일")]
    [SerializeField] private float pulseMaxScale = 1.05f;

    [Tooltip("하이라이트 프레임 펄스 주기(초)")]
    [SerializeField] private float pulseDuration = 0.8f;

    [Tooltip("단계 전환 후 입력 방지 쿨다운(초)")]
    [SerializeField] private float inputCooldown = 0.2f;

    #endregion

    #region 내부 필드

    private int _currentStepIndex = -1;
    private bool _isGuideActive = false;
    private bool _isGuideCompleted = false;
    private float _stepStartTime = 0f;
    private Coroutine _pulseRoutine;

    #endregion

    #region 라이프 사이클

    // 싱글톤 인스턴스 할당
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (guideSteps == null || guideSteps.Count == 0)
        {
            InitializeDefaultSteps();
        }

        if (guidePanel != null)
        {
            guidePanel.SetActive(false);
        }
    }

    // 전역 이벤트 구독 등록
    private void OnEnable()
    {
        EventBus.Subscribe<DataLoadEvent>(OnDataLoaded);
        EventBus.Subscribe<DataSaveEvent>(OnDataSaved);
        EventBus.Subscribe<DataResetEvent>(OnDataReset);
    }

    // 전역 이벤트 구독 해제
    private void OnDisable()
    {
        EventBus.Unsubscribe<DataLoadEvent>(OnDataLoaded);
        EventBus.Unsubscribe<DataSaveEvent>(OnDataSaved);
        EventBus.Unsubscribe<DataResetEvent>(OnDataReset);
    }

    // 모든 사용자 입력 감지 및 다음 단계 전환 처리
    private void Update()
    {
        if (!_isGuideActive) return;
        if (Time.unscaledTime - _stepStartTime < inputCooldown) return;

        if (CheckAnyInput())
        {
            ProceedToNextStep();
        }
    }

    #endregion

    #region 세이브 데이터 이벤트 연동

    // 세이브 데이터 로드 시 가이드 완료 여부 반영
    private void OnDataLoaded(DataLoadEvent evt)
    {
        if (evt.saveData != null)
        {
            _isGuideCompleted = evt.saveData.isGuideCompleted;
        }
    }

    // 세이브 데이터 저장 시 가이드 완료 상태 기록
    private void OnDataSaved(DataSaveEvent evt)
    {
        if (evt.saveData != null)
        {
            evt.saveData.isGuideCompleted = _isGuideCompleted;
        }
    }

    // 데이터 초기화 시 가이드 완료 상태 리셋
    private void OnDataReset(DataResetEvent evt)
    {
        _isGuideCompleted = false;
    }

    #endregion

    #region 가이드 제어

    // 조건 충족 시 가이드 시퀀스 시작
    public void StartGuideIfNeeded()
    {
        bool hasSaveFile = SaveManager.Instance != null && SaveManager.Instance.HasExistingSaveFile;

        if (hasSaveFile || _isGuideCompleted)
        {
            return;
        }

        StartGuide();
    }

    // 가이드 시퀀스 강제 시작
    public void StartGuide()
    {
        if (guideSteps == null || guideSteps.Count == 0)
        {
            return;
        }

        _isGuideActive = true;
        _currentStepIndex = -1;

        if (guidePanel != null)
        {
            guidePanel.SetActive(true);
        }

        ProceedToNextStep();
    }

    // 다음 가이드 단계로 이동
    public void ProceedToNextStep()
    {
        _currentStepIndex++;

        if (_currentStepIndex >= guideSteps.Count)
        {
            CompleteGuide();
            return;
        }

        ShowStep(_currentStepIndex);
    }

    // 지정된 인덱스의 가이드 단계 표시
    private void ShowStep(int index)
    {
        if (index < 0 || index >= guideSteps.Count) return;

        GuideStep step = guideSteps[index];
        _stepStartTime = Time.unscaledTime;

        if (titleText != null)
        {
            titleText.text = !string.IsNullOrEmpty(step.titleText) ? step.titleText : step.stepName;
        }

        if (descriptionText != null)
        {
            descriptionText.text = step.descriptionText;
        }

        if (stepIndicatorText != null)
        {
            stepIndicatorText.text = $"{index + 1} / {guideSteps.Count}";
        }

        UpdateHighlightTarget(step.targetButton);
    }

    // 대상 버튼 위치로 하이라이트 프레임 이동 및 크기 동기화
    private void UpdateHighlightTarget(Button targetButton)
    {
        if (highlightFrame == null) return;

        if (targetButton == null)
        {
            highlightFrame.gameObject.SetActive(false);
            return;
        }

        highlightFrame.gameObject.SetActive(true);

        RectTransform targetRect = targetButton.GetComponent<RectTransform>();
        if (targetRect != null)
        {
            highlightFrame.pivot = new Vector2(0.5f, 0.5f);

            Vector3[] corners = new Vector3[4];
            targetRect.GetWorldCorners(corners);
            Vector3 centerPos = (corners[0] + corners[2]) * 0.5f;

            highlightFrame.position = centerPos;
            highlightFrame.sizeDelta = targetRect.rect.size + highlightPadding;
        }

        if (_pulseRoutine != null)
        {
            StopCoroutine(_pulseRoutine);
        }
        _pulseRoutine = StartCoroutine(PulseHighlightFrame());
    }

    // 하이라이트 프레임 펄스 애니메이션 루프 연출
    private IEnumerator PulseHighlightFrame()
    {
        if (highlightFrame == null) yield break;

        while (_isGuideActive)
        {
            float elapsed = 0f;
            while (elapsed < pulseDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float halfDuration = pulseDuration * 0.5f;
                float progress = elapsed < halfDuration
                    ? elapsed / halfDuration
                    : 1f - ((elapsed - halfDuration) / halfDuration);

                float currentScale = Mathf.Lerp(pulseMinScale, pulseMaxScale, progress);
                highlightFrame.localScale = new Vector3(currentScale, currentScale, 1f);
                yield return null;
            }
        }
    }

    // 가이드 완료 처리 및 세이브 저장
    private void CompleteGuide()
    {
        _isGuideActive = false;
        _isGuideCompleted = true;

        if (_pulseRoutine != null)
        {
            StopCoroutine(_pulseRoutine);
            _pulseRoutine = null;
        }

        if (guidePanel != null)
        {
            guidePanel.SetActive(false);
        }

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGameData(force: true);
        }
    }

    #endregion

    #region 입력 확인 및 초기화 보조

    // 사용자 입력 발생 여부 확인
    private bool CheckAnyInput()
    {
        if (Input.anyKeyDown) return true;
        if (Input.GetMouseButtonDown(0)) return true;
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) return true;
        return false;
    }

    // 8종 기본 가이드 항목 및 설명 문구 초기 세팅
    private void InitializeDefaultSteps()
    {
        guideSteps = new List<GuideStep>
        {
            new GuideStep
            {
                stepName = "유닛 보관함",
                titleText = "유닛 보관함",
                descriptionText = "보유 중인 유닛들의 목록과 상세 능력치, 성장 상태를 확인할 수 있습니다."
            },
            new GuideStep
            {
                stepName = "덱 편성 창",
                titleText = "덱 편성 창",
                descriptionText = "전투와 던전에 출전시킬 유닛들을 배치하고 나만의 팀을 구성할 수 있습니다."
            },
            new GuideStep
            {
                stepName = "유닛 뽑기 창",
                titleText = "유닛 뽑기 창",
                descriptionText = "다이아를 소모하여 전장에 합류할 새로운 영웅 유닛들을 소환할 수 있습니다."
            },
            new GuideStep
            {
                stepName = "업그레이드 창",
                titleText = "업그레이드 창",
                descriptionText = "골드와 재화를 사용하여 아군 전체의 기본 전투 스탯을 영구적으로 강화합니다."
            },
            new GuideStep
            {
                stepName = "공방",
                titleText = "공방",
                descriptionText = "전투와 방치를 통해 획득한 재료들로 유용한 소모품과 장비를 제작합니다."
            },
            new GuideStep
            {
                stepName = "인벤토리",
                titleText = "인벤토리",
                descriptionText = "획득한 장비와 소모품을 확인하고, 유닛에게 장비를 장착시킬 수 있습니다."
            },
            new GuideStep
            {
                stepName = "던전",
                titleText = "던전",
                descriptionText = "유닛을 파견하여 다양한 성장 재료와 마석을 지속적으로 획득할 수 있습니다."
            },
            new GuideStep
            {
                stepName = "레이드",
                titleText = "레이드",
                descriptionText = "강력한 보스 몬스터에게 도전하여 최고급 제작 재료인 레이드 마석을 획득합니다."
            }
        };
    }

    #endregion
}
