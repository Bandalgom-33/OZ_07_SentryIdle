using System.Collections.Generic;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    public static class AidEffect
    {
        private const int InitialPulseCount = 8;
        private const int MaxPulseCount = 32;
        private const int RingSegments = 28;
        private const float PulseDuration = 0.55f;
        private const float PulseStartScale = 0.55f;
        private const float PulseEndScale = 1.25f;
        private const float ShieldScale = 0.85f;
        private const float PulseWidth = 0.055f;
        private const float ShieldWidth = 0.07f;

        private static readonly List<RingView> activePulses = new List<RingView>(InitialPulseCount);
        private static readonly Stack<RingView> pulsePool = new Stack<RingView>(InitialPulseCount);
        private static readonly Dictionary<CombatHealth, RingView> shieldViews = new Dictionary<CombatHealth, RingView>();
        private static readonly Stack<RingView> shieldPool = new Stack<RingView>(8);

        private static AidEffectRunner runner;
        private static Material sharedMaterial;

        public static void ShowShield(UnitRuntimeState target)
        {
            if (!CanShow(target) || target.Health.CurrentShield <= 0f)
            {
                return;
            }

            EnsureRuntime();

            CombatHealth health = target.Health;

            if (!shieldViews.ContainsKey(health))
            {
                RingView shieldView = shieldPool.Count > 0 ? shieldPool.Pop() : CreateRing("ShieldRing", ShieldWidth);
                Transform parent = GetEffectPoint(target);

                shieldView.ShowPersistent(parent, GetShieldColor(), ShieldScale);
                shieldViews.Add(health, shieldView);

                health.OnHealthChanged += HandleShieldChanged;
                health.OnDied += HandleShieldDied;
            }

            ShowPulseInternal(target, GetShieldColor());
        }

        public static void ShowHeal(UnitRuntimeState target)
        {
            if (!CanShow(target))
            {
                return;
            }

            EnsureRuntime();
            ShowPulseInternal(target, GetHealColor());
        }

        public static void ShowSkill(UnitRuntimeState target)
        {
            if (!CanShow(target))
            {
                return;
            }

            EnsureRuntime();
            ShowPulseInternal(target, GetSkillColor());
        }

        internal static void Step(float deltaTime)
        {
            for (int i = activePulses.Count - 1; i >= 0; i--)
            {
                RingView view = activePulses[i];

                if (view != null && view.StepPulse(deltaTime))
                {
                    continue;
                }

                activePulses.RemoveAt(i);

                if (view == null)
                {
                    continue;
                }

                view.Hide();
                pulsePool.Push(view);
            }
        }

        internal static void Shutdown()
        {
            foreach (KeyValuePair<CombatHealth, RingView> pair in shieldViews)
            {
                CombatHealth health = pair.Key;

                if (health != null)
                {
                    health.OnHealthChanged -= HandleShieldChanged;
                    health.OnDied -= HandleShieldDied;
                }

                if (pair.Value != null)
                {
                    pair.Value.Destroy();
                }
            }

            shieldViews.Clear();

            for (int i = 0; i < activePulses.Count; i++)
            {
                if (activePulses[i] != null)
                {
                    activePulses[i].Destroy();
                }
            }

            activePulses.Clear();

            while (pulsePool.Count > 0)
            {
                RingView view = pulsePool.Pop();

                if (view != null)
                {
                    view.Destroy();
                }
            }

            while (shieldPool.Count > 0)
            {
                RingView view = shieldPool.Pop();

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
                GameObject root = new GameObject("AidEffect");
                runner = root.AddComponent<AidEffectRunner>();
            }

            if (sharedMaterial == null)
            {
                sharedMaterial = CreateMaterial();
            }

            if (pulsePool.Count == 0 && activePulses.Count == 0)
            {
                for (int i = 0; i < InitialPulseCount; i++)
                {
                    RingView view = CreateRing("AidPulse", PulseWidth);
                    view.Hide();
                    pulsePool.Push(view);
                }
            }
        }

        private static void ShowPulseInternal(UnitRuntimeState target, Color color)
        {
            RingView view = GetPulseView();

            if (view == null)
            {
                return;
            }

            view.PlayPulse(GetEffectPoint(target), color);
            activePulses.Add(view);
        }

        private static RingView GetPulseView()
        {
            if (pulsePool.Count > 0)
            {
                return pulsePool.Pop();
            }

            if (activePulses.Count < MaxPulseCount)
            {
                return CreateRing("AidPulse", PulseWidth);
            }

            if (activePulses.Count == 0)
            {
                return null;
            }

            RingView oldest = activePulses[0];
            activePulses.RemoveAt(0);
            oldest.Hide();
            return oldest;
        }

        private static void HandleShieldChanged(CombatHealth health)
        {
            if (health == null || health.CurrentShield > 0f)
            {
                return;
            }

            ReleaseShield(health);
        }

        private static void HandleShieldDied(CombatHealth health)
        {
            ReleaseShield(health);
        }

        private static void ReleaseShield(CombatHealth health)
        {
            if (health == null || !shieldViews.TryGetValue(health, out RingView view))
            {
                return;
            }

            health.OnHealthChanged -= HandleShieldChanged;
            health.OnDied -= HandleShieldDied;

            shieldViews.Remove(health);

            if (view != null)
            {
                view.Hide();
                shieldPool.Push(view);
            }
        }

        private static RingView CreateRing(string objectName, float width)
        {
            EnsureMaterial();

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
            line.sortingOrder = 50;

            for (int i = 0; i < RingSegments; i++)
            {
                float angle = i / (float)RingSegments * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)));
            }

            return new RingView(ringObject, line);
        }

        private static void EnsureMaterial()
        {
            if (sharedMaterial == null)
            {
                sharedMaterial = CreateMaterial();
            }
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
                Debug.LogError("AidEffect에서 사용할 Shader를 찾지 못했습니다.");
                return null;
            }

            Material material = new Material(shader);
            material.name = "AidEffect_Runtime";
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

        private static Color GetShieldColor()
        {
            return new Color(0.25f, 0.75f, 1f, 1f);
        }

        private static Color GetHealColor()
        {
            return new Color(0.25f, 1f, 0.4f, 1f);
        }

        private static Color GetSkillColor()
        {
            return new Color(1f, 0.82f, 0.2f, 1f);
        }

        private sealed class RingView
        {
            private readonly GameObject gameObject;
            private readonly Transform transform;
            private readonly LineRenderer line;

            private Color color;
            private float elapsed;

            public RingView(GameObject viewObject, LineRenderer lineRenderer)
            {
                gameObject = viewObject;
                transform = viewObject.transform;
                line = lineRenderer;
            }

            public void ShowPersistent(Transform parent, Color ringColor, float scale)
            {
                transform.SetParent(parent, false);
                transform.localPosition = new Vector3(0f, 0.15f, 0f);
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one * scale;

                color = ringColor;
                SetAlpha(0.85f);

                elapsed = 0f;
                gameObject.SetActive(true);
            }

            public void PlayPulse(Transform parent, Color ringColor)
            {
                transform.SetParent(parent, false);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one * PulseStartScale;

                color = ringColor;
                SetAlpha(1f);

                elapsed = 0f;
                gameObject.SetActive(true);
            }

            public bool StepPulse(float deltaTime)
            {
                if (!gameObject.activeSelf)
                {
                    return false;
                }

                elapsed += Mathf.Max(0f, deltaTime);
                float progress = PulseDuration > 0f ? Mathf.Clamp01(elapsed / PulseDuration) : 1f;
                float smooth = Mathf.SmoothStep(0f, 1f, progress);

                transform.localPosition = new Vector3(0f, Mathf.Lerp(-0.1f, 0.35f, smooth), 0f);
                transform.localScale = Vector3.one * Mathf.Lerp(PulseStartScale, PulseEndScale, smooth);
                SetAlpha(1f - smooth);

                return progress < 1f;
            }

            public void Hide()
            {
                gameObject.SetActive(false);
                transform.SetParent(runner != null ? runner.transform : null, false);
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

            private void SetAlpha(float alpha)
            {
                Color currentColor = color;
                currentColor.a = Mathf.Clamp01(alpha);
                line.startColor = currentColor;
                line.endColor = currentColor;
            }
        }
    }

    [DisallowMultipleComponent]
    internal sealed class AidEffectRunner : MonoBehaviour
    {
        private void Update()
        {
            AidEffect.Step(Time.deltaTime);
        }

        private void OnDestroy()
        {
            AidEffect.Shutdown();
        }
    }
}