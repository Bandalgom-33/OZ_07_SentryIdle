using UnityEngine;
using UnityEngine.UI;

namespace EndlessGuard.Unit.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class CriticalBurst : MonoBehaviour
    {
        [Header("치명타 폭발 프레임")]
        [Tooltip("치명타 숫자 뒤에서 표시되는 폭발형 외곽 그래픽입니다.")]
        [SerializeField] private BurstGraphic burstGraphic;

        [Tooltip("폭발형 외곽선의 기본 색상입니다.")]
        [SerializeField] private Color burstColor = new Color32(255, 175, 45, 255);

        [Tooltip("치명타 숫자 영역에 추가할 가로·세로 여백입니다. 숫자 에셋 크기가 바뀌어도 이 여백을 기준으로 자동 맞춥니다.")]
        [SerializeField] private Vector2 framePadding = new Vector2(40f, 24f);

        [Tooltip("치명타 숫자의 투명도에 곱할 폭발 프레임의 최대 투명도 비율입니다.")]
        [Range(0f, 1f)]
        [SerializeField] private float burstAlpha = 0.85f;

        [Header("치명타 작은 스파크")]
        [Tooltip("폭발 주변에서 짧게 퍼지는 작은 스파크 이미지들입니다.")]
        [SerializeField] private Image[] sparks;

        [Tooltip("스파크의 기본 색상입니다.")]
        [SerializeField] private Color sparkColor = new Color32(255, 220, 110, 255);

        [Tooltip("스파크가 표시되는 전체 시간입니다.")]
        [Min(0.05f)]
        [SerializeField] private float sparkDuration = 0.16f;

        [Tooltip("스파크가 중심에서 바깥으로 이동하는 거리입니다.")]
        [Min(0f)]
        [SerializeField] private float sparkDistance = 34f;

        [Tooltip("스파크들이 퍼지는 방향의 시작 각도입니다.")]
        [SerializeField] private float sparkAngleOffset = 22.5f;

        [Tooltip("스파크가 처음 표시될 때의 크기입니다.")]
        [Min(0.1f)]
        [SerializeField] private float sparkStartScale = 1f;

        [Tooltip("스파크가 사라질 때의 크기입니다.")]
        [Min(0f)]
        [SerializeField] private float sparkEndScale = 0.25f;

        private RectTransform rectTransform;
        private DamageNumber followNumber;
        private int followVersion;
        private float elapsedTime;
        private bool isPlaying;

        public bool IsPlaying => isPlaying;

        private void Awake()
        {
            Prepare();
        }

        private void LateUpdate()
        {
            FollowDamageNumber();
        }

        public void Prepare()
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }

            if (burstGraphic == null)
            {
                burstGraphic = GetComponentInChildren<BurstGraphic>(true);
            }

            if (sparks == null || sparks.Length == 0)
            {
                sparks = GetComponentsInChildren<Image>(true);
            }
        }

        public void Play(DamageNumber target)
        {
            Prepare();

            if (target == null)
            {
                Hide();
                return;
            }

            followNumber = target;
            followVersion = target.DisplayVersion;
            elapsedTime = 0f;
            isPlaying = true;
            gameObject.SetActive(true);

            FollowDamageNumber();
            ResetVisual();
        }

        public bool Step(float deltaTime)
        {
            if (!isPlaying || followNumber == null)
            {
                return false;
            }

            if (!followNumber.IsPlaying || followNumber.DisplayVersion != followVersion)
            {
                return false;
            }

            elapsedTime += Mathf.Max(0f, deltaTime);
            UpdateBurstAlpha();
            UpdateSparks();
            return true;
        }

        public void Hide()
        {
            isPlaying = false;
            followNumber = null;
            followVersion = 0;
            elapsedTime = 0f;

            if (burstGraphic != null)
            {
                Color color = burstColor;
                color.a = 0f;
                burstGraphic.color = color;
            }

            for (int i = 0; i < sparks.Length; i++)
            {
                Image spark = sparks[i];

                if (spark == null)
                {
                    continue;
                }

                Color color = sparkColor;
                color.a = 0f;
                spark.color = color;
            }

            gameObject.SetActive(false);
        }

        private void FollowDamageNumber()
        {
            if (!isPlaying || followNumber == null || rectTransform == null)
            {
                return;
            }

            if (!followNumber.IsPlaying || followNumber.DisplayVersion != followVersion)
            {
                return;
            }

            rectTransform.anchoredPosition = followNumber.AnchoredPosition;
            rectTransform.sizeDelta = followNumber.VisualSize + framePadding;
            rectTransform.localScale = followNumber.VisualScale;
        }

        private void ResetVisual()
        {
            if (burstGraphic != null)
            {
                Color color = burstColor;
                color.a = burstColor.a * burstAlpha;
                burstGraphic.color = color;
            }

            int sparkCount = sparks.Length;

            for (int i = 0; i < sparkCount; i++)
            {
                Image spark = sparks[i];

                if (spark == null)
                {
                    continue;
                }

                float angle = sparkAngleOffset + 360f * i / Mathf.Max(1, sparkCount);
                RectTransform sparkRect = spark.rectTransform;

                sparkRect.anchoredPosition = Vector2.zero;
                sparkRect.localScale = Vector3.one * sparkStartScale;
                sparkRect.localRotation = Quaternion.Euler(0f, 0f, angle - 90f);
                spark.color = sparkColor;
            }
        }

        private void UpdateBurstAlpha()
        {
            if (burstGraphic == null || followNumber == null)
            {
                return;
            }

            Color color = burstColor;
            color.a = burstColor.a * burstAlpha * followNumber.VisualAlpha;
            burstGraphic.color = color;
        }

        private void UpdateSparks()
        {
            int sparkCount = sparks.Length;
            float progress = sparkDuration > 0f ? Mathf.Clamp01(elapsedTime / sparkDuration) : 1f;
            float moveProgress = Mathf.SmoothStep(0f, 1f, progress);

            for (int i = 0; i < sparkCount; i++)
            {
                Image spark = sparks[i];

                if (spark == null)
                {
                    continue;
                }

                float angle = sparkAngleOffset + 360f * i / Mathf.Max(1, sparkCount);
                float radians = angle * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));

                spark.rectTransform.anchoredPosition = direction * sparkDistance * moveProgress;
                float scale = Mathf.Lerp(sparkStartScale, sparkEndScale, moveProgress);
                spark.rectTransform.localScale = Vector3.one * scale;

                Color color = sparkColor;
                color.a = sparkColor.a * (1f - Mathf.SmoothStep(0f, 1f, progress));
                spark.color = color;
            }
        }
    }
}