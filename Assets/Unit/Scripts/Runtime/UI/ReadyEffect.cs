using System.Collections.Generic;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    public static class ReadyEffect
    {
        private const int InitialBurstCount = 6;
        private const int MaxBurstCount = 20;
        private const int RingSegments = 28;
        private const float BurstDuration = 0.55f;
        private const float BurstStartScale = 0.45f;
        private const float BurstEndScale = 1.35f;
        private const float LoopScale = 0.7f;
        private const float BurstWidth = 0.065f;
        private const float LoopWidth = 0.05f;

        private static readonly Dictionary<UnitRuntimeState, RingView> readyViews = new Dictionary<UnitRuntimeState, RingView>();
        private static readonly Stack<RingView> loopPool = new Stack<RingView>();
        private static readonly Stack<RingView> burstPool = new Stack<RingView>();
        private static readonly List<RingView> activeBursts = new List<RingView>();

        private static ReadyEffectRunner runner;
        private static Material sharedMaterial;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic()
        {
            readyViews.Clear();
            loopPool.Clear();
            burstPool.Clear();
            activeBursts.Clear();
            runner = null;
            sharedMaterial = null;
        }

        public static void Show(UnitRuntimeState target)
        {
            if (!CanShow(target) || readyViews.ContainsKey(target))
            {
                return;
            }

            EnsureRuntime();

            if (runner == null)
            {
                return;
            }

            Transform effectPoint = GetEffectPoint(target);
            RingView loopView = GetLoopView();

            if (loopView == null)
            {
                return;
            }

            loopView.ShowLoop(effectPoint);
            readyViews.Add(target, loopView);
            PlayBurst(effectPoint);
        }

        public static void Hide(UnitRuntimeState target)
        {
            if (target == null || !readyViews.TryGetValue(target, out RingView view))
            {
                return;
            }

            readyViews.Remove(target);

            if (view != null)
            {
                view.Hide(runner != null ? runner.transform : null);
                loopPool.Push(view);
            }
        }

        internal static void Step(float deltaTime)
        {
            for (int i = activeBursts.Count - 1; i >= 0; i--)
            {
                RingView view = activeBursts[i];

                if (view != null && view.StepBurst(deltaTime))
                {
                    continue;
                }

                activeBursts.RemoveAt(i);

                if (view != null)
                {
                    view.Hide(runner != null ? runner.transform : null);
                    burstPool.Push(view);
                }
            }
        }

        internal static void Shutdown()
        {
            foreach (KeyValuePair<UnitRuntimeState, RingView> pair in readyViews)
            {
                if (pair.Value != null)
                {
                    pair.Value.Destroy();
                }
            }

            readyViews.Clear();

            for (int i = 0; i < activeBursts.Count; i++)
            {
                if (activeBursts[i] != null)
                {
                    activeBursts[i].Destroy();
                }
            }

            activeBursts.Clear();

            while (loopPool.Count > 0)
            {
                RingView view = loopPool.Pop();

                if (view != null)
                {
                    view.Destroy();
                }
            }

            while (burstPool.Count > 0)
            {
                RingView view = burstPool.Pop();

                if (view != null)
                {
                    view.Destroy();
                }
            }

            if (sharedMaterial != null)
            {
                Object.Destroy(sharedMaterial);
                sharedMaterial = null;
            }

            runner = null;
        }

        private static void EnsureRuntime()
        {
            if (runner == null)
            {
                GameObject root = new GameObject("ReadyEffect");
                runner = root.AddComponent<ReadyEffectRunner>();
            }

            if (sharedMaterial == null)
            {
                sharedMaterial = CreateMaterial();
            }

            if (burstPool.Count == 0 && activeBursts.Count == 0)
            {
                for (int i = 0; i < InitialBurstCount; i++)
                {
                    RingView view = CreateRing("ReadyBurst", BurstWidth);
                    view.Hide(runner.transform);
                    burstPool.Push(view);
                }
            }
        }

        private static void PlayBurst(Transform effectPoint)
        {
            RingView view = GetBurstView();

            if (view == null)
            {
                return;
            }

            view.PlayBurst(effectPoint);
            activeBursts.Add(view);
        }

        private static RingView GetLoopView()
        {
            if (loopPool.Count > 0)
            {
                return loopPool.Pop();
            }

            return CreateRing("ReadyLoop", LoopWidth);
        }

        private static RingView GetBurstView()
        {
            if (burstPool.Count > 0)
            {
                return burstPool.Pop();
            }

            if (activeBursts.Count < MaxBurstCount)
            {
                return CreateRing("ReadyBurst", BurstWidth);
            }

            if (activeBursts.Count == 0)
            {
                return null;
            }

            RingView oldest = activeBursts[0];
            activeBursts.RemoveAt(0);
            oldest.Hide(runner != null ? runner.transform : null);
            return oldest;
        }

        private static RingView CreateRing(string objectName, float width)
        {
            if (sharedMaterial == null)
            {
                sharedMaterial = CreateMaterial();
            }

            GameObject ringObject = new GameObject(objectName);
            LineRenderer line = ringObject.AddComponent<LineRenderer>();

            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = RingSegments;
            line.widthMultiplier = width;
            line.numCapVertices = 0;
            line.numCornerVertices = 2;
            line.alignment = LineAlignment.View;
            line.sharedMaterial = sharedMaterial;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.sortingOrder = 60;

            for (int i = 0; i < RingSegments; i++)
            {
                float angle = i / (float)RingSegments * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)));
            }

            return new RingView(ringObject, line);
        }

        private static Material CreateMaterial()
        {
            Shader shader = Shader.Find("Sprites/Default");

            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            }

            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader == null)
            {
                Debug.LogError("ReadyEffect에서 사용할 Shader를 찾지 못했습니다.");
                return null;
            }

            Material material = new Material(shader);
            material.name = "ReadyEffect_Runtime";
            material.hideFlags = HideFlags.HideAndDontSave;
            return material;
        }

        private static Transform GetEffectPoint(UnitRuntimeState target)
        {
            return target.Anchors != null && target.Anchors.EffectPoint != null ? target.Anchors.EffectPoint : target.transform;
        }

        private static bool CanShow(UnitRuntimeState target)
        {
            return target != null && target.gameObject.activeInHierarchy && target.IsInitialized && target.Health != null && !target.Health.IsDead;
        }

        private sealed class RingView
        {
            private static readonly Color ReadyColor = new Color(1f, 0.82f, 0.15f, 1f);

            private readonly GameObject gameObject;
            private readonly Transform transform;
            private readonly LineRenderer line;

            private float elapsed;

            public RingView(GameObject viewObject, LineRenderer lineRenderer)
            {
                gameObject = viewObject;
                transform = viewObject.transform;
                line = lineRenderer;
            }

            public void ShowLoop(Transform parent)
            {
                transform.SetParent(parent, false);
                transform.localPosition = new Vector3(0f, 0.12f, 0f);
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one * LoopScale;

                line.startColor = ReadyColor;
                line.endColor = ReadyColor;

                elapsed = 0f;
                gameObject.SetActive(true);
            }

            public void PlayBurst(Transform parent)
            {
                transform.SetParent(parent, false);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one * BurstStartScale;

                line.startColor = ReadyColor;
                line.endColor = ReadyColor;

                elapsed = 0f;
                gameObject.SetActive(true);
            }

            public bool StepBurst(float deltaTime)
            {
                if (!gameObject.activeSelf)
                {
                    return false;
                }

                elapsed += Mathf.Max(0f, deltaTime);
                float progress = BurstDuration > 0f ? Mathf.Clamp01(elapsed / BurstDuration) : 1f;
                float smooth = Mathf.SmoothStep(0f, 1f, progress);

                transform.localPosition = new Vector3(0f, Mathf.Lerp(-0.1f, 0.35f, smooth), 0f);
                transform.localScale = Vector3.one * Mathf.Lerp(BurstStartScale, BurstEndScale, smooth);

                Color color = ReadyColor;
                color.a = 1f - smooth;
                line.startColor = color;
                line.endColor = color;

                return progress < 1f;
            }

            public void Hide(Transform poolRoot)
            {
                gameObject.SetActive(false);
                transform.SetParent(poolRoot, false);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;
                elapsed = 0f;
            }

            public void Destroy()
            {
                if (gameObject != null)
                {
                    Object.Destroy(gameObject);
                }
            }
        }
    }

    [DisallowMultipleComponent]
    internal sealed class ReadyEffectRunner : MonoBehaviour
    {
        private void Update()
        {
            ReadyEffect.Step(Time.deltaTime);
        }

        private void OnDestroy()
        {
            ReadyEffect.Shutdown();
        }
    }
}