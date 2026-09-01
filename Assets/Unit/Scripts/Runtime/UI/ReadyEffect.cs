using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace EndlessGuard.Unit.Runtime
{
    /// <summary>
    /// SP 스킬 사용 가능 상태를 캐릭터 실루엣 주변의 고정 셰이더 오라로 표시합니다.
    /// ParticleSystem을 사용하지 않으므로 조각이 위로 날아가거나 캐릭터를 벗어나지 않습니다.
    /// </summary>
    public static class ReadyEffect
    {
        private const string ConfigResourcesPath = "Combat/UnitReadyEffectConfig";
        private const string FallbackShaderName = "EndlessGuard/SP Ready Aura";

        private static readonly int AlphaScaleId = Shader.PropertyToID("_AlphaScale");
        private static readonly int PhaseOffsetId = Shader.PropertyToID("_PhaseOffset");

        private static readonly Dictionary<UnitRuntimeState, AuraView> activeViews = new Dictionary<UnitRuntimeState, AuraView>();
        private static readonly Stack<AuraView> pool = new Stack<AuraView>();

        private static ReadyEffectRunner runner;
        private static UnitReadyEffectConfigSO config;
        private static Mesh sharedQuadMesh;
        private static Material fallbackMaterial;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic()
        {
            activeViews.Clear();
            pool.Clear();
            runner = null;
            config = null;
            sharedQuadMesh = null;
            fallbackMaterial = null;
        }

        public static void Show(UnitRuntimeState target)
        {
            if (!CanShow(target) || activeViews.ContainsKey(target))
            {
                return;
            }

            EnsureRuntime();
            if (runner == null)
            {
                return;
            }

            AuraView view = GetView();
            if (view == null)
            {
                return;
            }

            view.Show(target.transform, config);
            activeViews.Add(target, view);
        }

        public static void Hide(UnitRuntimeState target)
        {
            if (target == null || !activeViews.TryGetValue(target, out AuraView view))
            {
                return;
            }

            activeViews.Remove(target);
            if (view == null || view.IsDestroyed)
            {
                return;
            }

            view.Hide(runner != null ? runner.transform : null);
            pool.Push(view);
        }

        internal static void Shutdown()
        {
            foreach (KeyValuePair<UnitRuntimeState, AuraView> pair in activeViews)
            {
                pair.Value?.Destroy();
            }
            activeViews.Clear();

            while (pool.Count > 0)
            {
                pool.Pop()?.Destroy();
            }

            if (sharedQuadMesh != null)
            {
                Object.Destroy(sharedQuadMesh);
                sharedQuadMesh = null;
            }

            if (fallbackMaterial != null)
            {
                Object.Destroy(fallbackMaterial);
                fallbackMaterial = null;
            }

            config = null;
            runner = null;
        }

        private static void EnsureRuntime()
        {
            if (runner == null)
            {
                GameObject root = new GameObject("SPReadyEffectPool");
                runner = root.AddComponent<ReadyEffectRunner>();
            }

            if (config == null)
            {
                config = Resources.Load<UnitReadyEffectConfigSO>(ConfigResourcesPath);
            }

            if (sharedQuadMesh == null)
            {
                sharedQuadMesh = CreateQuadMesh();
            }
        }

        private static AuraView GetView()
        {
            while (pool.Count > 0)
            {
                AuraView pooled = pool.Pop();
                if (pooled != null && !pooled.IsDestroyed)
                {
                    return pooled;
                }
            }

            return CreateView();
        }

        private static AuraView CreateView()
        {
            EnsureRuntime();
            if (sharedQuadMesh == null)
            {
                return null;
            }

            Material material = GetAuraMaterial();
            if (material == null)
            {
                Debug.LogWarning("SP MAX Aura Material/Shader를 찾지 못해 준비 오라를 표시하지 않습니다.");
                return null;
            }

            GameObject root = new GameObject("SPReadyAura_Shader");
            if (runner != null)
            {
                root.transform.SetParent(runner.transform, false);
            }

            int sorting = config != null ? config.SortingOrder : -20;
            float coreScale = config != null ? config.CoreLayerScale : 1f;
            float outerScale = config != null ? config.OuterLayerScale : 1.08f;
            float outerAlpha = config != null ? config.OuterLayerAlpha : 0.42f;

            AuraLayer outer = CreateLayer(root.transform, "AuraOuter", material, outerScale, outerAlpha, 2.17f, sorting - 1);
            AuraLayer core = CreateLayer(root.transform, "AuraCore", material, coreScale, 1f, 0f, sorting);

            root.SetActive(false);
            return new AuraView(root, outer, core);
        }

        private static AuraLayer CreateLayer(
            Transform parent,
            string name,
            Material material,
            float layerScale,
            float alphaScale,
            float phaseOffset,
            int sortingOrder)
        {
            GameObject layerObject = new GameObject(name);
            layerObject.transform.SetParent(parent, false);

            MeshFilter filter = layerObject.AddComponent<MeshFilter>();
            filter.sharedMesh = sharedQuadMesh;

            MeshRenderer renderer = layerObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            renderer.sortingOrder = sortingOrder;

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetFloat(AlphaScaleId, alphaScale);
            block.SetFloat(PhaseOffsetId, phaseOffset);
            renderer.SetPropertyBlock(block);

            SPReadyAuraBillboard billboard = layerObject.AddComponent<SPReadyAuraBillboard>();
            return new AuraLayer(layerObject.transform, billboard, layerScale);
        }

        private static Material GetAuraMaterial()
        {
            if (config != null && config.AuraMaterial != null)
            {
                return config.AuraMaterial;
            }

            if (fallbackMaterial != null)
            {
                return fallbackMaterial;
            }

            Shader shader = Shader.Find(FallbackShaderName);
            if (shader == null)
            {
                return null;
            }

            fallbackMaterial = new Material(shader)
            {
                name = "SPReadyAura_Fallback_Runtime",
                hideFlags = HideFlags.HideAndDontSave
            };
            return fallbackMaterial;
        }

        private static Mesh CreateQuadMesh()
        {
            Mesh mesh = new Mesh
            {
                name = "SPReadyAura_Quad",
                hideFlags = HideFlags.HideAndDontSave
            };

            mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3( 0.5f, -0.5f, 0f),
                new Vector3( 0.5f,  0.5f, 0f),
                new Vector3(-0.5f,  0.5f, 0f),
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f),
            };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateBounds();
            return mesh;
        }

        private static bool CanShow(UnitRuntimeState target)
        {
            return target != null
                && target.gameObject.activeInHierarchy
                && target.IsInitialized
                && target.Health != null
                && !target.Health.IsDead;
        }

        private readonly struct AuraLayer
        {
            public readonly Transform Transform;
            public readonly SPReadyAuraBillboard Billboard;
            public readonly float Scale;

            public AuraLayer(Transform transform, SPReadyAuraBillboard billboard, float scale)
            {
                Transform = transform;
                Billboard = billboard;
                Scale = scale;
            }
        }

        private sealed class AuraView
        {
            private readonly GameObject gameObject;
            private readonly Transform transform;
            private readonly AuraLayer outerLayer;
            private readonly AuraLayer coreLayer;

            public bool IsDestroyed => gameObject == null;

            public AuraView(GameObject viewObject, AuraLayer outer, AuraLayer core)
            {
                gameObject = viewObject;
                transform = viewObject != null ? viewObject.transform : null;
                outerLayer = outer;
                coreLayer = core;
            }

            public void Show(Transform parent, UnitReadyEffectConfigSO effectConfig)
            {
                if (gameObject == null || transform == null || parent == null)
                {
                    return;
                }

                transform.SetParent(parent, false);
                transform.localPosition = effectConfig != null ? effectConfig.LocalPosition : new Vector3(0f, 0.92f, 0f);
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;

                Vector2 size = effectConfig != null ? effectConfig.AuraSize : new Vector2(1.30f, 2.15f);
                SetLayerSize(outerLayer, size);
                SetLayerSize(coreLayer, size);

                gameObject.SetActive(true);
                outerLayer.Billboard?.RefreshCamera();
                coreLayer.Billboard?.RefreshCamera();
            }

            public void Hide(Transform poolRoot)
            {
                if (gameObject == null)
                {
                    return;
                }

                gameObject.SetActive(false);
                if (poolRoot != null)
                {
                    transform.SetParent(poolRoot, false);
                }

                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;
            }

            public void Destroy()
            {
                if (gameObject != null)
                {
                    Object.Destroy(gameObject);
                }
            }

            private static void SetLayerSize(AuraLayer layer, Vector2 baseSize)
            {
                if (layer.Transform == null)
                {
                    return;
                }

                float width = Mathf.Max(0.05f, baseSize.x * layer.Scale);
                float height = Mathf.Max(0.05f, baseSize.y * layer.Scale);
                layer.Transform.localPosition = Vector3.zero;
                layer.Transform.localScale = new Vector3(width, height, 1f);
            }
        }
    }

    /// <summary>
    /// 오라 Quad가 카메라를 향하도록 고정합니다. 위치는 유닛에 붙어 있고 회전만 카메라를 따라갑니다.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class SPReadyAuraBillboard : MonoBehaviour
    {
        private Camera cachedCamera;

        public void RefreshCamera()
        {
            cachedCamera = Camera.main;
            FaceCamera();
        }

        private void OnEnable()
        {
            RefreshCamera();
        }

        private void LateUpdate()
        {
            if (cachedCamera == null || !cachedCamera.isActiveAndEnabled)
            {
                cachedCamera = Camera.main;
            }

            FaceCamera();
        }

        private void FaceCamera()
        {
            if (cachedCamera == null)
            {
                return;
            }

            // Quad는 Cull Off 셰이더를 사용하므로 카메라 회전을 그대로 따라가면 안정적으로 화면을 향합니다.
            transform.rotation = cachedCamera.transform.rotation;
        }
    }

    [DisallowMultipleComponent]
    internal sealed class ReadyEffectRunner : MonoBehaviour
    {
        private void OnDestroy()
        {
            ReadyEffect.Shutdown();
        }
    }
}
