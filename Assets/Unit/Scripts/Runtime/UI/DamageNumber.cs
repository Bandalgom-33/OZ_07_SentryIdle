using EndlessGuard.Unit.Data;
using TMPro;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    public enum DamageNumberTargetType
    {
        Enemy = 0,
        Unit = 1
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class DamageNumber : MonoBehaviour
    {
        [Header("피해 숫자 이동")]
        [Tooltip("피해 숫자가 화면에 표시되는 전체 시간입니다.")]
        [Min(0.05f)]
        [SerializeField] private float lifetime = 0.75f;

        [Tooltip("새 피해가 없어도 숫자가 자신의 수명 동안 자연스럽게 상승하는 거리입니다.")]
        [Min(0f)]
        [SerializeField] private float riseDistance = 60f;

        [Tooltip("같은 대상에게 새 피해가 발생할 때 기존 숫자가 추가로 밀려 올라가는 거리입니다.")]
        [Min(0f)]
        [SerializeField] private float hitPushDistance = 16f;

        [Tooltip("피해 숫자가 시작 위치에서 올라갈 수 있는 최대 높이입니다.")]
        [Min(1f)]
        [SerializeField] private float maxRiseHeight = 140f;

        [Tooltip("새 피해로 밀려난 숫자가 목표 높이에 부드럽게 접근하는 시간입니다.")]
        [Min(0.01f)]
        [SerializeField] private float pushSmoothTime = 0.08f;

        [Tooltip("새 피해로 숫자가 위로 밀릴 때 사용할 최대 이동 속도입니다.")]
        [Min(1f)]
        [SerializeField] private float pushMaxSpeed = 320f;

        [Header("일반 피해 숫자 팝")]
        [Tooltip("일반 피해 숫자가 처음 생성되는 순간의 크기입니다.")]
        [Min(0.1f)]
        [SerializeField] private float startScale = 0.5f;

        [Tooltip("일반 피해 숫자가 처음 팍 나타날 때 순간적으로 커지는 크기입니다.")]
        [Min(0.1f)]
        [SerializeField] private float popScale = 1.15f;

        [Tooltip("일반 피해 숫자의 팝 연출이 끝난 뒤 유지하는 기본 크기입니다.")]
        [Min(0.1f)]
        [SerializeField] private float settleScale = 1f;

        [Tooltip("일반 피해 숫자가 시작 크기에서 팝 크기까지 커지는 시간입니다.")]
        [Min(0.01f)]
        [SerializeField] private float popDuration = 0.06f;

        [Tooltip("일반 피해 숫자가 팝 크기에서 기본 크기로 돌아오는 시간입니다.")]
        [Min(0.01f)]
        [SerializeField] private float settleDuration = 0.08f;

        [Header("치명타 피해 숫자 팝")]
        [Tooltip("치명타 숫자가 처음 생성되는 순간의 크기입니다.")]
        [Min(0.1f)]
        [SerializeField] private float criticalStartScale = 0.5f;

        [Tooltip("치명타 숫자가 순간적으로 팍 커질 때 도달하는 크기입니다.")]
        [Min(0.1f)]
        [SerializeField] private float criticalPopScale = 1.45f;

        [Tooltip("치명타 숫자가 강한 팝 이후 안정될 때 유지하는 크기입니다.")]
        [Min(0.1f)]
        [SerializeField] private float criticalSettleScale = 1.08f;

        [Tooltip("치명타 숫자가 시작 크기에서 강한 팝 크기까지 커지는 시간입니다.")]
        [Min(0.01f)]
        [SerializeField] private float criticalPopDuration = 0.045f;

        [Tooltip("치명타 숫자가 가장 큰 크기를 잠깐 유지하는 시간입니다.")]
        [Min(0f)]
        [SerializeField] private float criticalPeakHold = 0.035f;

        [Tooltip("치명타 숫자가 큰 크기에서 안정 크기로 돌아오는 시간입니다.")]
        [Min(0.01f)]
        [SerializeField] private float criticalSettleDuration = 0.10f;

        [Header("몬스터 피해 숫자 색상")]
        [Tooltip("몬스터가 일반 피해를 받았을 때 표시할 색상입니다.")]
        [SerializeField] private Color normalColor = new Color32(245, 245, 245, 255);

        [Tooltip("몬스터가 치명타 피해를 받았을 때 표시할 색상입니다.")]
        [SerializeField] private Color criticalColor = new Color32(255, 210, 70, 255);

        [Header("캐릭터 피해 숫자 색상")]
        [Tooltip("캐릭터가 일반 피해를 받았을 때 표시할 붉은색입니다.")]
        [SerializeField] private Color unitNormalColor = new Color32(255, 100, 100, 255);

        [Tooltip("캐릭터가 치명타 피해를 받았을 때 표시할 더 짙은 붉은색입니다.")]
        [SerializeField] private Color unitCriticalColor = new Color32(200, 50, 50, 255);

        [Header("피해 숫자 등장 색상")]
        [Tooltip("숫자가 등장하는 순간 잠깐 표시할 플래시 색상입니다.")]
        [SerializeField] private Color spawnFlashColor = Color.white;

        [Tooltip("숫자가 등장할 때 플래시 색상에서 원래 피해 색상으로 돌아오는 시간입니다.")]
        [Min(0f)]
        [SerializeField] private float spawnFlashDuration = 0.07f;

        [Header("피해 숫자 페이드")]
        [Tooltip("전체 표시 시간 중 이 비율까지는 선명하게 유지하고 이후부터 사라지기 시작합니다.")]
        [Range(0f, 0.95f)]
        [SerializeField] private float fadeStartRatio = 0.35f;

        private RectTransform rectTransform;
        private TextMeshProUGUI label;
        private Vector2 startPosition;
        private Vector3 baseScale;
        private DamageInfo damageInfo;
        private Color damageColor;
        private float activeStartScale;
        private float activePopScale;
        private float activeSettleScale;
        private float activePopDuration;
        private float activePeakHold;
        private float activeSettleDuration;
        private float elapsedTime;
        private float currentPushOffset;
        private float targetPushOffset;
        private float pushVelocity;
        private int targetId;
        private int displayVersion;
        private bool isPlaying;

        public bool IsPlaying => isPlaying;
        public int TargetId => targetId;
        public int DisplayVersion => displayVersion;
        public Vector2 AnchoredPosition => rectTransform != null ? rectTransform.anchoredPosition : startPosition;
        public Vector2 VisualSize
        {
            get
            {
                if (label == null)
                {
                    return rectTransform != null ? rectTransform.rect.size : Vector2.zero;
                }

                Vector2 preferredSize = label.GetPreferredValues();
                return new Vector2(preferredSize.x, preferredSize.y);
            }
        }
        public Vector3 VisualScale => rectTransform != null ? rectTransform.localScale : Vector3.one;
        public float VisualAlpha => label != null ? label.color.a : 0f;
        public DamageInfo DamageInfo => damageInfo;
        public float FinalDamage => damageInfo.FinalDamage;
        public DamageType DamageType => damageInfo.DamageType;
        public bool IsCritical => damageInfo.IsCritical;

        private void Awake()
        {
            Prepare();
        }

        public void Prepare()
        {
            if (rectTransform == null)
            {
                rectTransform = transform as RectTransform;
            }

            if (label == null)
            {
                label = GetComponent<TextMeshProUGUI>();
            }

            if (rectTransform != null)
            {
                baseScale = rectTransform.localScale;
            }
        }

        public void Show(float damage, Vector2 anchoredPosition, int ownerId)
        {
            Show(new DamageInfo(damage, DamageType.None, false), anchoredPosition, ownerId, DamageNumberTargetType.Enemy);
        }

        public void Show(DamageInfo newDamageInfo, Vector2 anchoredPosition, int ownerId)
        {
            Show(newDamageInfo, anchoredPosition, ownerId, DamageNumberTargetType.Enemy);
        }

        public void Show(DamageInfo newDamageInfo, Vector2 anchoredPosition, int ownerId, DamageNumberTargetType targetType)
        {
            Prepare();

            if (rectTransform == null || label == null || newDamageInfo.FinalDamage <= 0f)
            {
                return;
            }

            displayVersion++;
            damageInfo = newDamageInfo;
            ApplyScaleSettings();
            damageColor = GetDamageColor(targetType);
            startPosition = anchoredPosition;
            elapsedTime = 0f;
            currentPushOffset = 0f;
            targetPushOffset = 0f;
            pushVelocity = 0f;
            targetId = ownerId;
            isPlaying = true;

            rectTransform.anchoredPosition = startPosition;
            rectTransform.localScale = baseScale * activeStartScale;
            label.color = spawnFlashDuration > 0f ? spawnFlashColor : damageColor;
            label.SetText("{0:0}", damageInfo.FinalDamage);
            gameObject.SetActive(true);
        }

        public void PushUp()
        {
            if (!isPlaying)
            {
                return;
            }

            float maxPushOffset = Mathf.Max(0f, maxRiseHeight - riseDistance);
            targetPushOffset = Mathf.Min(maxPushOffset, targetPushOffset + hitPushDistance);
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
            UpdateColor(progress);

            return elapsedTime < lifetime;
        }

        public void Hide()
        {
            isPlaying = false;
            damageInfo = default;
            damageColor = normalColor;
            elapsedTime = 0f;
            currentPushOffset = 0f;
            targetPushOffset = 0f;
            pushVelocity = 0f;
            targetId = 0;

            if (rectTransform != null)
            {
                rectTransform.localScale = baseScale;
            }

            if (label != null)
            {
                label.color = normalColor;
            }

            gameObject.SetActive(false);
        }

        private Color GetDamageColor(DamageNumberTargetType targetType)
        {
            if (targetType == DamageNumberTargetType.Unit)
            {
                return damageInfo.IsCritical ? unitCriticalColor : unitNormalColor;
            }

            return damageInfo.IsCritical ? criticalColor : normalColor;
        }

        private void ApplyScaleSettings()
        {
            if (damageInfo.IsCritical)
            {
                activeStartScale = criticalStartScale;
                activePopScale = criticalPopScale;
                activeSettleScale = criticalSettleScale;
                activePopDuration = criticalPopDuration;
                activePeakHold = criticalPeakHold;
                activeSettleDuration = criticalSettleDuration;
                return;
            }

            activeStartScale = startScale;
            activePopScale = popScale;
            activeSettleScale = settleScale;
            activePopDuration = popDuration;
            activePeakHold = 0f;
            activeSettleDuration = settleDuration;
        }

        private void UpdateScale()
        {
            if (rectTransform == null)
            {
                return;
            }

            float scaleMultiplier;
            float holdEndTime = activePopDuration + activePeakHold;
            float settleEndTime = holdEndTime + activeSettleDuration;

            if (elapsedTime <= activePopDuration)
            {
                float popProgress = activePopDuration > 0f ? Mathf.Clamp01(elapsedTime / activePopDuration) : 1f;
                scaleMultiplier = Mathf.Lerp(activeStartScale, activePopScale, Mathf.SmoothStep(0f, 1f, popProgress));
            }
            else if (elapsedTime <= holdEndTime)
            {
                scaleMultiplier = activePopScale;
            }
            else if (elapsedTime <= settleEndTime)
            {
                float settleElapsed = elapsedTime - holdEndTime;
                float settleProgress = activeSettleDuration > 0f ? Mathf.Clamp01(settleElapsed / activeSettleDuration) : 1f;
                scaleMultiplier = Mathf.Lerp(activePopScale, activeSettleScale, Mathf.SmoothStep(0f, 1f, settleProgress));
            }
            else
            {
                scaleMultiplier = activeSettleScale;
            }

            rectTransform.localScale = baseScale * scaleMultiplier;
        }

        private void UpdateColor(float progress)
        {
            if (label == null)
            {
                return;
            }

            Color currentColor = damageColor;

            if (spawnFlashDuration > 0f && elapsedTime < spawnFlashDuration)
            {
                float flashProgress = Mathf.Clamp01(elapsedTime / spawnFlashDuration);
                currentColor = Color.Lerp(spawnFlashColor, damageColor, Mathf.SmoothStep(0f, 1f, flashProgress));
            }

            float alpha = damageColor.a;

            if (progress > fadeStartRatio)
            {
                float fadeLength = Mathf.Max(0.0001f, 1f - fadeStartRatio);
                float fadeProgress = Mathf.Clamp01((progress - fadeStartRatio) / fadeLength);
                alpha = Mathf.Lerp(damageColor.a, 0f, Mathf.SmoothStep(0f, 1f, fadeProgress));
            }

            currentColor.a = alpha;
            label.color = currentColor;
        }
    }
}