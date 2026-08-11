using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CombatHealth))]
    [RequireComponent(typeof(CombatEntityAnchors))]
    public sealed class HitShake : MonoBehaviour
    {
        [Header("피격 흔들림")]
        [Tooltip("피격 순간 외형이 좌우로 흔들리는 최대 거리입니다.")]
        [Min(0f)]
        [SerializeField] private float shakeDistance = 0.05f;

        [Tooltip("한 번의 피격 반응 동안 좌우 흔들림이 반복되는 횟수입니다.")]
        [Min(0.5f)]
        [SerializeField] private float shakeCycles = 2.5f;

        [Tooltip("피격 흔들림과 크기 반동이 끝날 때까지의 전체 시간입니다.")]
        [Min(0.01f)]
        [SerializeField] private float reactionDuration = 0.12f;

        [Header("피격 크기 반동")]
        [Tooltip("피격 직후 순간적으로 커지는 크기 배율입니다.")]
        [Min(1f)]
        [SerializeField] private float scaleUp = 1.07f;

        [Tooltip("확대 직후 살짝 눌리듯 작아지는 크기 배율입니다.")]
        [Range(0.5f, 1f)]
        [SerializeField] private float scaleDown = 0.96f;

        private CombatHealth health;
        private CombatEntityAnchors anchors;
        private Transform visualRoot;
        private Vector3 baseLocalPosition;
        private Vector3 baseLocalScale;
        private float elapsedTime;
        private bool subscribed;

        private void Awake()
        {
            health = GetComponent<CombatHealth>();
            anchors = GetComponent<CombatEntityAnchors>();
            CacheVisualRoot();
            Subscribe();
            enabled = false;
        }

        private void Update()
        {
            if (visualRoot == null)
            {
                enabled = false;
                return;
            }

            elapsedTime += Time.deltaTime;
            float progress = reactionDuration > 0f ? Mathf.Clamp01(elapsedTime / reactionDuration) : 1f;

            UpdatePosition(progress);
            UpdateScale(progress);

            if (progress < 1f)
            {
                return;
            }

            RestoreVisual();
            enabled = false;
        }

        private void OnDisable()
        {
            if (Application.isPlaying)
            {
                RestoreVisual();
            }
        }

        private void OnDestroy()
        {
            Unsubscribe();
            RestoreVisual();
        }

        private void Subscribe()
        {
            if (subscribed || health == null)
            {
                return;
            }

            health.OnDamaged += HandleDamaged;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            if (health != null)
            {
                health.OnDamaged -= HandleDamaged;
            }

            subscribed = false;
        }

        private void HandleDamaged(CombatHealth sender, float appliedDamage)
        {
            if (sender != health || appliedDamage <= 0f || visualRoot == null)
            {
                return;
            }

            elapsedTime = 0f;
            visualRoot.localPosition = baseLocalPosition + Vector3.right * shakeDistance;
            visualRoot.localScale = baseLocalScale * scaleUp;
            enabled = true;
        }

        private void CacheVisualRoot()
        {
            visualRoot = anchors != null ? anchors.VisualRoot : null;

            if (visualRoot == null)
            {
                return;
            }

            baseLocalPosition = visualRoot.localPosition;
            baseLocalScale = visualRoot.localScale;
        }

        private void UpdatePosition(float progress)
        {
            float envelope = 1f - Mathf.SmoothStep(0f, 1f, progress);
            float wave = Mathf.Sin(progress * Mathf.PI * 2f * shakeCycles);
            float offset = wave * shakeDistance * envelope;

            visualRoot.localPosition = baseLocalPosition + Vector3.right * offset;
        }

        private void UpdateScale(float progress)
        {
            float scaleMultiplier;

            if (progress < 0.25f)
            {
                float phase = Mathf.Clamp01(progress / 0.25f);
                scaleMultiplier = Mathf.Lerp(1f, scaleUp, Mathf.SmoothStep(0f, 1f, phase));
            }
            else if (progress < 0.55f)
            {
                float phase = Mathf.Clamp01((progress - 0.25f) / 0.30f);
                scaleMultiplier = Mathf.Lerp(scaleUp, scaleDown, Mathf.SmoothStep(0f, 1f, phase));
            }
            else
            {
                float phase = Mathf.Clamp01((progress - 0.55f) / 0.45f);
                scaleMultiplier = Mathf.Lerp(scaleDown, 1f, Mathf.SmoothStep(0f, 1f, phase));
            }

            visualRoot.localScale = baseLocalScale * scaleMultiplier;
        }

        private void RestoreVisual()
        {
            if (visualRoot == null)
            {
                return;
            }

            visualRoot.localPosition = baseLocalPosition;
            visualRoot.localScale = baseLocalScale;
        }
    }
}