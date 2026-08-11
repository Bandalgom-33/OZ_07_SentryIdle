using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CombatHealth))]
    [RequireComponent(typeof(CombatEntityAnchors))]
    public sealed class HitFlash : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [Header("피격 플래시")]
        [Tooltip("피격 순간 외형에 섞을 플래시 색상입니다.")]
        [SerializeField] private Color flashColor = new Color32(255, 214, 214, 255);

        [Tooltip("원래 색상에 플래시 색상을 얼마나 강하게 섞을지 설정합니다.")]
        [Range(0f, 1f)]
        [SerializeField] private float flashStrength = 0.45f;

        [Tooltip("피격 플래시가 원래 색으로 돌아오는 전체 시간입니다.")]
        [Min(0.01f)]
        [SerializeField] private float flashDuration = 0.09f;

        private CombatHealth health;
        private CombatEntityAnchors anchors;
        private RendererState[] rendererStates;
        private float elapsedTime;
        private bool subscribed;

        private void Awake()
        {
            health = GetComponent<CombatHealth>();
            anchors = GetComponent<CombatEntityAnchors>();
            CacheRenderers();
            Subscribe();
            enabled = false;
        }

        private void Update()
        {
            if (rendererStates == null || rendererStates.Length == 0)
            {
                enabled = false;
                return;
            }

            elapsedTime += Time.deltaTime;
            float progress = flashDuration > 0f ? Mathf.Clamp01(elapsedTime / flashDuration) : 1f;
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);

            for (int i = 0; i < rendererStates.Length; i++)
            {
                RendererState state = rendererStates[i];

                if (state == null || state.Renderer == null)
                {
                    continue;
                }

                Color flashTarget = Color.Lerp(state.OriginalColor, flashColor, flashStrength);
                flashTarget.a = state.OriginalColor.a;
                Color currentColor = Color.Lerp(flashTarget, state.OriginalColor, easedProgress);
                ApplyColor(state, currentColor);
            }

            if (progress < 1f)
            {
                return;
            }

            RestoreColors();
            enabled = false;
        }

        private void OnDisable()
        {
            if (Application.isPlaying)
            {
                RestoreColors();
            }
        }

        private void OnDestroy()
        {
            Unsubscribe();
            RestoreColors();
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
            if (sender != health || appliedDamage <= 0f || rendererStates == null || rendererStates.Length == 0)
            {
                return;
            }

            elapsedTime = 0f;

            for (int i = 0; i < rendererStates.Length; i++)
            {
                RendererState state = rendererStates[i];

                if (state == null || state.Renderer == null)
                {
                    continue;
                }

                Color flashTarget = Color.Lerp(state.OriginalColor, flashColor, flashStrength);
                flashTarget.a = state.OriginalColor.a;
                ApplyColor(state, flashTarget);
            }

            enabled = true;
        }

        private void CacheRenderers()
        {
            Transform visualRoot = anchors != null ? anchors.VisualRoot : null;

            if (visualRoot == null)
            {
                rendererStates = new RendererState[0];
                return;
            }

            Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
            rendererStates = new RendererState[renderers.Length];

            for (int i = 0; i < renderers.Length; i++)
            {
                rendererStates[i] = CreateRendererState(renderers[i]);
            }
        }

        private static RendererState CreateRendererState(Renderer targetRenderer)
        {
            RendererState state = new RendererState();
            state.Renderer = targetRenderer;

            if (targetRenderer is SpriteRenderer spriteRenderer)
            {
                state.SpriteRenderer = spriteRenderer;
                state.OriginalColor = spriteRenderer.color;
                return state;
            }

            Material sharedMaterial = targetRenderer != null ? targetRenderer.sharedMaterial : null;

            if (sharedMaterial != null && sharedMaterial.HasProperty(BaseColorId))
            {
                state.ColorPropertyId = BaseColorId;
                state.OriginalColor = sharedMaterial.GetColor(BaseColorId);
            }
            else if (sharedMaterial != null && sharedMaterial.HasProperty(ColorId))
            {
                state.ColorPropertyId = ColorId;
                state.OriginalColor = sharedMaterial.GetColor(ColorId);
            }
            else
            {
                state.ColorPropertyId = 0;
                state.OriginalColor = Color.white;
            }

            state.PropertyBlock = new MaterialPropertyBlock();
            return state;
        }

        private static void ApplyColor(RendererState state, Color color)
        {
            if (state.SpriteRenderer != null)
            {
                state.SpriteRenderer.color = color;
                return;
            }

            if (state.Renderer == null || state.ColorPropertyId == 0 || state.PropertyBlock == null)
            {
                return;
            }

            state.Renderer.GetPropertyBlock(state.PropertyBlock);
            state.PropertyBlock.SetColor(state.ColorPropertyId, color);
            state.Renderer.SetPropertyBlock(state.PropertyBlock);
        }

        private void RestoreColors()
        {
            if (rendererStates == null)
            {
                return;
            }

            for (int i = 0; i < rendererStates.Length; i++)
            {
                RendererState state = rendererStates[i];

                if (state == null || state.Renderer == null)
                {
                    continue;
                }

                ApplyColor(state, state.OriginalColor);
            }
        }

        private sealed class RendererState
        {
            public Renderer Renderer;
            public SpriteRenderer SpriteRenderer;
            public MaterialPropertyBlock PropertyBlock;
            public Color OriginalColor;
            public int ColorPropertyId;
        }
    }
}