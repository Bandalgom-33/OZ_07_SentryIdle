using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class MissFeedback : MonoBehaviour
    {
        [Header("MISS 이동")]
        [Tooltip("MISS 표시가 화면에 존재하는 전체 시간입니다.")]
        [Min(0.05f)]
        [SerializeField] private float lifetime = 0.65f;

        [Tooltip("MISS가 자신의 수명 동안 자연스럽게 상승하는 거리입니다.")]
        [Min(0f)]
        [SerializeField] private float riseDistance = 48f;

        [Tooltip("같은 대상에게 새로운 MISS가 발생할 때 기존 MISS가 추가로 밀려 올라가는 거리입니다.")]
        [Min(0f)]
        [SerializeField] private float pushDistance = 14f;

        [Tooltip("MISS가 시작 위치에서 올라갈 수 있는 최대 높이입니다.")]
        [Min(1f)]
        [SerializeField] private float maxRiseHeight = 90f;

        [Tooltip("연속 MISS로 위로 밀릴 때 부드럽게 이동하는 시간입니다.")]
        [Min(0.01f)]
        [SerializeField] private float pushSmoothTime = 0.07f;

        [Tooltip("연속 MISS로 밀릴 때 사용할 최대 이동 속도입니다.")]
        [Min(1f)]
        [SerializeField] private float pushMaxSpeed = 280f;

        [Header("MISS 팝")]
        [Tooltip("MISS가 처음 등장하는 크기입니다.")]
        [Min(0.1f)]
        [SerializeField] private float startScale = 0.55f;

        [Tooltip("MISS가 순간적으로 팍 커지는 최대 크기입니다.")]
        [Min(0.1f)]
        [SerializeField] private float popScale = 1.20f;

        [Tooltip("팝이 끝난 뒤 유지하는 크기입니다.")]
        [Min(0.1f)]
        [SerializeField] private float settleScale = 1f;

        [Tooltip("시작 크기에서 최대 크기까지 커지는 시간입니다.")]
        [Min(0.01f)]
        [SerializeField] private float popDuration = 0.05f;

        [Tooltip("최대 크기에서 기본 크기로 돌아오는 시간입니다.")]
        [Min(0.01f)]
        [SerializeField] private float settleDuration = 0.09f;

        [Header("MISS 표시")]
        [Tooltip("MISS 전체 표시를 기울일 각도입니다. 음수는 왼쪽으로 기울어집니다.")]
        [SerializeField] private float tiltAngle = -10f;

        [Tooltip("전체 표시 시간 중 이 비율 이후부터 투명해지기 시작합니다.")]
        [Range(0f, 0.95f)]
        [SerializeField] private float fadeStartRatio = 0.35f;

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Vector2 startPosition;
        private Vector3 baseScale;
        private Quaternion baseRotation;
        private float elapsedTime;
        private float currentPushOffset;
        private float targetPushOffset;
        private float pushVelocity;
        private int targetId;
        private float displayScale = 1f;
        private bool prepared;
        private bool isPlaying;

        public bool IsPlaying => isPlaying;
        public int TargetId => targetId;

        private void Awake()
        {
            Prepare();
        }

        public void Prepare()
        {
            if (prepared)
            {
                return;
            }

            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            baseScale = rectTransform.localScale;
            baseRotation = rectTransform.localRotation;
            prepared = true;
        }

        public void Show(Vector2 anchoredPosition, int ownerId)
        {
            Show(anchoredPosition, ownerId, 1f);
        }

        public void Show(Vector2 anchoredPosition, int ownerId, float scale)
        {
            Prepare();

            startPosition = anchoredPosition;
            elapsedTime = 0f;
            currentPushOffset = 0f;
            targetPushOffset = 0f;
            pushVelocity = 0f;
            targetId = ownerId;
            displayScale = Mathf.Clamp(scale, 0.1f, 3f);
            isPlaying = true;

            rectTransform.anchoredPosition = startPosition;
            rectTransform.localScale = baseScale * (startScale * displayScale);
            rectTransform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, tiltAngle);
            canvasGroup.alpha = 1f;
            gameObject.SetActive(true);
        }

        public void PushUp()
        {
            if (!isPlaying)
            {
                return;
            }

            float maxPushOffset = Mathf.Max(0f, maxRiseHeight - riseDistance);
            targetPushOffset = Mathf.Min(maxPushOffset, targetPushOffset + pushDistance);
        }

        public bool Step(float deltaTime)
        {
            if (!isPlaying)
            {
                return false;
            }

            float safeDeltaTime = Mathf.Max(0f, deltaTime);
            elapsedTime += safeDeltaTime;

            float progress = lifetime > 0f ? Mathf.Clamp01(elapsedTime / lifetime) : 1f;
            float naturalRise = riseDistance * Mathf.SmoothStep(0f, 1f, progress);

            currentPushOffset = Mathf.SmoothDamp(currentPushOffset, targetPushOffset, ref pushVelocity, pushSmoothTime, pushMaxSpeed, safeDeltaTime);

            float totalRise = Mathf.Min(maxRiseHeight, naturalRise + currentPushOffset);
            rectTransform.anchoredPosition = startPosition + Vector2.up * totalRise;

            UpdateScale();
            UpdateAlpha(progress);

            return elapsedTime < lifetime;
        }

        public void Hide()
        {
            isPlaying = false;
            elapsedTime = 0f;
            currentPushOffset = 0f;
            targetPushOffset = 0f;
            pushVelocity = 0f;
            targetId = 0;
            displayScale = 1f;

            if (rectTransform != null)
            {
                rectTransform.localScale = baseScale;
                rectTransform.localRotation = baseRotation;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

            gameObject.SetActive(false);
        }

        private void UpdateScale()
        {
            float scaleMultiplier;

            if (elapsedTime <= popDuration)
            {
                float progress = popDuration > 0f ? Mathf.Clamp01(elapsedTime / popDuration) : 1f;
                scaleMultiplier = Mathf.Lerp(startScale, popScale, Mathf.SmoothStep(0f, 1f, progress));
            }
            else if (elapsedTime <= popDuration + settleDuration)
            {
                float settleElapsed = elapsedTime - popDuration;
                float progress = settleDuration > 0f ? Mathf.Clamp01(settleElapsed / settleDuration) : 1f;
                scaleMultiplier = Mathf.Lerp(popScale, settleScale, Mathf.SmoothStep(0f, 1f, progress));
            }
            else
            {
                scaleMultiplier = settleScale;
            }

            rectTransform.localScale = baseScale * (scaleMultiplier * displayScale);
        }

        private void UpdateAlpha(float progress)
        {
            if (progress <= fadeStartRatio)
            {
                canvasGroup.alpha = 1f;
                return;
            }

            float fadeLength = Mathf.Max(0.0001f, 1f - fadeStartRatio);
            float fadeProgress = Mathf.Clamp01((progress - fadeStartRatio) / fadeLength);
            canvasGroup.alpha = 1f - Mathf.SmoothStep(0f, 1f, fadeProgress);
        }
    }
}