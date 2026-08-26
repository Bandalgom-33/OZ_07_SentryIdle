using System;
using System.Collections;
using System.Collections.Generic;
using EndlessGuard.Unit.Raid.Data;
using UnityEngine;
using UnityEngine.Rendering;

namespace EndlessGuard.Unit.Raid.Runtime
{
    [DisallowMultipleComponent]
    public sealed class RaidRouteGuideView : MonoBehaviour
    {
        private static readonly int VisibilityId = Shader.PropertyToID("_Visibility");
        private static readonly int LayerColorId = Shader.PropertyToID("_LayerColor");
        private static readonly int LayerIntensityId = Shader.PropertyToID("_LayerIntensity");
        private static readonly int LayerAlphaId = Shader.PropertyToID("_LayerAlpha");
        private static readonly int PulseBoostId = Shader.PropertyToID("_PulseBoost");

        private static readonly Color GlowColor = new Color(0.24f, 0.18f, 1.0f, 1f);
        private static readonly Color CoreColor = new Color(0.52f, 0.78f, 1.35f, 1f);
        private const int RequiredPassCount = 3;
        private const float CompletionHoldSeconds = 0.12f;
        private const float MaxGuideDeltaTime = 0.05f;

        [Header("참조")]
        [SerializeField] private Material routeMaterial;

        [Header("이동 안내")]
        [Min(0.35f)] [SerializeField] private float passDuration = 2.4f;
        [Min(0f)] [SerializeField] private float repeatInterval = 0.18f;
        [Min(0.5f)] [SerializeField] private float trailLengthTiles = 4.5f;

        [Header("페이즈 경로 안내")]
        [Tooltip("Phase 전환 후 새로 열린 Entry Route를 몇 번 흘려 보여줄지 결정합니다.")]
        [Range(1, 3)] [SerializeField] private int phasePassCount = 2;
        [Tooltip("Phase 전환 후 새 Route 한 번의 흐름 시간입니다.")]
        [Min(0.35f)] [SerializeField] private float phasePassDuration = 1.25f;
        [Tooltip("Phase 새 Route 반복 사이의 짧은 간격입니다.")]
        [Min(0f)] [SerializeField] private float phaseRepeatInterval = 0.12f;

        [Header("표현")]
        [Min(0f)] [SerializeField] private float heightOffset = 0.22f;
        [Min(0.05f)] [SerializeField] private float glowWidthTiles = 0.52f;
        [Min(0.02f)] [SerializeField] private float coreWidthTiles = 0.16f;

        private readonly List<RouteStrip> strips = new List<RouteStrip>(16);
        private Transform routeRoot;
        private bool playing;

        public bool IsPlaying => playing;

        public bool CanPlay(RaidBoardRuntime boardRuntime)
        {
            return routeMaterial != null &&
                   boardRuntime != null &&
                   boardRuntime.Board != null &&
                   boardRuntime.TravelPaths != null &&
                   boardRuntime.TravelPaths.Count > 0;
        }

        public IEnumerator Play(RaidBoardRuntime boardRuntime, Action onFinalPassStarted = null)
        {
            yield return PlayInternal(boardRuntime, null, RequiredPassCount, passDuration, repeatInterval, onFinalPassStarted);
        }

        public bool CanPlayNewEntries(RaidBoardRuntime boardRuntime, RaidMapSO previousMap)
        {
            return CanPlay(boardRuntime) && previousMap != null && CountIncludedPaths(boardRuntime, previousMap) > 0;
        }

        public IEnumerator PlayNewEntries(RaidBoardRuntime boardRuntime, RaidMapSO previousMap)
        {
            int passes = Mathf.Clamp(phasePassCount, 1, 3);
            float duration = phasePassDuration > 0f ? phasePassDuration : 1.25f;
            float interval = Mathf.Max(0f, phaseRepeatInterval);
            yield return PlayInternal(boardRuntime, previousMap, passes, duration, interval, null);
        }

        private IEnumerator PlayInternal(RaidBoardRuntime boardRuntime, RaidMapSO previousMap, int passCount, float configuredDuration, float configuredInterval, Action onFinalPassStarted)
        {
            StopImmediate();

            if (!CanPlay(boardRuntime))
            {
                yield break;
            }

            EnsureRoot();
            int activeStripCount = BuildRouteStrips(boardRuntime, previousMap);

            if (activeStripCount <= 0)
            {
                yield break;
            }

            playing = true;
            SetVisibility(1f);
            HideAllWindows();

            int loops = Mathf.Max(1, passCount);
            float duration = Mathf.Max(0.35f, configuredDuration);
            float interval = Mathf.Max(0f, configuredInterval);

            for (int loop = 0; loop < loops; loop++)
            {
                if (loop == loops - 1)
                {
                    onFinalPassStarted?.Invoke();
                }

                float elapsed = 0f;

                while (elapsed < duration)
                {
                    elapsed += GetGuideDeltaTime();
                    float progress = Mathf.Clamp01(elapsed / duration);
                    UpdateMovingWindows(progress);
                    yield return null;
                }

                UpdateMovingWindows(1f);
                HideAllWindows();

                if (loop < loops - 1 && interval > 0f)
                {
                    float wait = 0f;

                    while (wait < interval)
                    {
                        wait += GetGuideDeltaTime();
                        yield return null;
                    }
                }
            }

            HideAllWindows();

            if (CompletionHoldSeconds > 0f)
            {
                float completionWait = 0f;

                while (completionWait < CompletionHoldSeconds)
                {
                    completionWait += GetGuideDeltaTime();
                    yield return null;
                }
            }

            SetStripsActive(false);
            playing = false;
        }

        private static float GetGuideDeltaTime()
        {
            return Mathf.Min(Mathf.Max(0f, Time.unscaledDeltaTime), MaxGuideDeltaTime);
        }

        public void StopImmediate()
        {
            playing = false;
            SetVisibility(0f);
            HideAllWindows();
            SetStripsActive(false);
        }

        private void OnDisable()
        {
            StopImmediate();
        }

        private void OnValidate()
        {
            passDuration = Mathf.Max(0.35f, passDuration);
            repeatInterval = Mathf.Max(0f, repeatInterval);
            trailLengthTiles = Mathf.Max(0.5f, trailLengthTiles);
            phasePassCount = Mathf.Clamp(phasePassCount, 1, 3);
            phasePassDuration = Mathf.Max(0.35f, phasePassDuration);
            phaseRepeatInterval = Mathf.Max(0f, phaseRepeatInterval);
            heightOffset = Mathf.Max(0f, heightOffset);
            glowWidthTiles = Mathf.Max(0.05f, glowWidthTiles);
            coreWidthTiles = Mathf.Clamp(coreWidthTiles, 0.02f, glowWidthTiles);
        }

        private void EnsureRoot()
        {
            if (routeRoot != null)
            {
                return;
            }

            GameObject root = new GameObject("RouteGuide");
            routeRoot = root.transform;
            routeRoot.SetParent(transform, false);
        }

        private int BuildRouteStrips(RaidBoardRuntime boardRuntime, RaidMapSO previousMap)
        {
            IReadOnlyList<RaidTravelPath> travelPaths = boardRuntime.TravelPaths;
            float tileSize = boardRuntime.Board.TileSize;
            int requiredStripCount = CountIncludedPaths(boardRuntime, previousMap);
            EnsureStripCapacity(requiredStripCount);

            int stripIndex = 0;

            for (int pathIndex = 0; pathIndex < travelPaths.Count; pathIndex++)
            {
                RaidTravelPath travelPath = travelPaths[pathIndex];

                if (!ShouldIncludePath(boardRuntime, travelPath, previousMap))
                {
                    continue;
                }

                RouteStrip strip = strips[stripIndex++];
                strip.SetActive(true);
                strip.Configure(travelPath, heightOffset, tileSize * glowWidthTiles, tileSize * coreWidthTiles, tileSize * trailLengthTiles);
            }

            for (int i = stripIndex; i < strips.Count; i++)
            {
                strips[i].SetActive(false);
            }

            return requiredStripCount;
        }

        private static int CountIncludedPaths(RaidBoardRuntime boardRuntime, RaidMapSO previousMap)
        {
            if (boardRuntime == null || boardRuntime.TravelPaths == null)
            {
                return 0;
            }

            int count = 0;

            for (int i = 0; i < boardRuntime.TravelPaths.Count; i++)
            {
                if (ShouldIncludePath(boardRuntime, boardRuntime.TravelPaths[i], previousMap))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool ShouldIncludePath(RaidBoardRuntime boardRuntime, RaidTravelPath travelPath, RaidMapSO previousMap)
        {
            if (boardRuntime == null || travelPath == null || boardRuntime.CurrentMapData == null)
            {
                return false;
            }

            if (previousMap == null)
            {
                return true;
            }

            RaidMapSO currentMap = boardRuntime.CurrentMapData;

            if (travelPath.EntryNodeId < 0 || travelPath.EntryNodeId >= currentMap.NodeCount)
            {
                return false;
            }

            Vector2Int entryCoordinate = currentMap.GetNode(travelPath.EntryNodeId).Coordinate;
            return !HasEntry(previousMap, entryCoordinate);
        }

        private static bool HasEntry(RaidMapSO map, Vector2Int coordinate)
        {
            if (map == null)
            {
                return false;
            }

            for (int i = 0; i < map.NodeCount; i++)
            {
                RaidMapNodeData node = map.GetNode(i);

                if (node.Type == RaidMapNodeType.Entry && node.Coordinate == coordinate)
                {
                    return true;
                }
            }

            return false;
        }

        private void EnsureStripCapacity(int requiredCount)
        {
            while (strips.Count < requiredCount)
            {
                int index = strips.Count;
                strips.Add(CreateStrip(index));
            }
        }

        private RouteStrip CreateStrip(int index)
        {
            GameObject stripRootObject = new GameObject($"Path_{index + 1:00}");
            Transform stripRoot = stripRootObject.transform;
            stripRoot.SetParent(routeRoot, false);

            LineRenderer glow = CreateLineRenderer(stripRoot, "Glow", 0);
            LineRenderer core = CreateLineRenderer(stripRoot, "Core", 1);
            ApplyTailGradient(glow, 0.72f);
            ApplyTailGradient(core, 1f);

            MaterialPropertyBlock glowProperties = new MaterialPropertyBlock();
            glowProperties.SetColor(LayerColorId, GlowColor);
            glowProperties.SetFloat(LayerIntensityId, 0.82f);
            glowProperties.SetFloat(LayerAlphaId, 0.38f);
            glowProperties.SetFloat(PulseBoostId, 0.72f);

            MaterialPropertyBlock coreProperties = new MaterialPropertyBlock();
            coreProperties.SetColor(LayerColorId, CoreColor);
            coreProperties.SetFloat(LayerIntensityId, 1.35f);
            coreProperties.SetFloat(LayerAlphaId, 0.92f);
            coreProperties.SetFloat(PulseBoostId, 1.55f);

            return new RouteStrip(stripRootObject, glow, core, glowProperties, coreProperties);
        }

        private LineRenderer CreateLineRenderer(Transform parent, string objectName, int sortingOrder)
        {
            GameObject lineObject = new GameObject(objectName);
            Transform lineTransform = lineObject.transform;
            lineTransform.SetParent(parent, false);

            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.sharedMaterial = routeMaterial;
            line.useWorldSpace = true;
            line.loop = false;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Tile;
            line.numCornerVertices = 6;
            line.numCapVertices = 8;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.lightProbeUsage = LightProbeUsage.Off;
            line.reflectionProbeUsage = ReflectionProbeUsage.Off;
            line.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            line.sortingOrder = sortingOrder;
            return line;
        }

        private static void ApplyTailGradient(LineRenderer line, float headAlpha)
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(headAlpha * 0.14f, 0.22f),
                    new GradientAlphaKey(headAlpha * 0.52f, 0.58f),
                    new GradientAlphaKey(headAlpha, 1f)
                });
            line.colorGradient = gradient;
        }

        private void UpdateMovingWindows(float progress)
        {
            float clamped = Mathf.Clamp01(progress);

            for (int i = 0; i < strips.Count; i++)
            {
                strips[i].SetProgress(clamped);
            }
        }

        private void HideAllWindows()
        {
            for (int i = 0; i < strips.Count; i++)
            {
                strips[i].HideWindow();
            }
        }

        private void SetVisibility(float visibility)
        {
            float clamped = Mathf.Clamp01(visibility);

            for (int i = 0; i < strips.Count; i++)
            {
                strips[i].SetVisibility(clamped);
            }
        }

        private void SetStripsActive(bool active)
        {
            for (int i = 0; i < strips.Count; i++)
            {
                strips[i].SetActive(active);
            }
        }

        private sealed class RouteStrip
        {
            private const float MinimumVisibleDistance = 0.02f;

            private readonly GameObject root;
            private readonly LineRenderer glow;
            private readonly LineRenderer core;
            private readonly MaterialPropertyBlock glowProperties;
            private readonly MaterialPropertyBlock coreProperties;

            private Vector3[] pathPoints = new Vector3[0];
            private float[] cumulativeDistances = new float[0];
            private Vector3[] windowPoints = new Vector3[0];
            private float totalLength;
            private float trailLength;

            public RouteStrip(GameObject root, LineRenderer glow, LineRenderer core, MaterialPropertyBlock glowProperties, MaterialPropertyBlock coreProperties)
            {
                this.root = root;
                this.glow = glow;
                this.core = core;
                this.glowProperties = glowProperties;
                this.coreProperties = coreProperties;
            }

            public void Configure(RaidTravelPath travelPath, float heightOffset, float glowWidth, float coreWidth, float trailLength)
            {
                if (travelPath == null || travelPath.PointCount < 2)
                {
                    SetActive(false);
                    return;
                }

                int pointCount = travelPath.PointCount;
                EnsurePathCapacity(pointCount);
                totalLength = 0f;

                for (int i = 0; i < pointCount; i++)
                {
                    Vector3 point = travelPath.GetPoint(i);
                    point.y += heightOffset;
                    pathPoints[i] = point;

                    if (i == 0)
                    {
                        cumulativeDistances[i] = 0f;
                        continue;
                    }

                    totalLength += Vector3.Distance(pathPoints[i - 1], point);
                    cumulativeDistances[i] = totalLength;
                }

                this.trailLength = Mathf.Max(MinimumVisibleDistance, trailLength);
                glow.widthMultiplier = glowWidth;
                core.widthMultiplier = coreWidth;
                HideWindow();
            }

            public void SetProgress(float progress)
            {
                if (!root.activeSelf || totalLength <= MinimumVisibleDistance)
                {
                    HideWindow();
                    return;
                }

                float travelDistance = totalLength + trailLength;
                float headDistance = Mathf.Clamp01(progress) * travelDistance;
                float tailDistance = Mathf.Max(0f, headDistance - trailLength);
                float visibleHeadDistance = Mathf.Min(totalLength, headDistance);

                if (visibleHeadDistance - tailDistance <= MinimumVisibleDistance || tailDistance >= totalLength)
                {
                    HideWindow();
                    return;
                }

                int windowCount = BuildWindow(tailDistance, visibleHeadDistance);

                if (windowCount < 2)
                {
                    HideWindow();
                    return;
                }

                glow.positionCount = windowCount;
                core.positionCount = windowCount;

                for (int i = 0; i < windowCount; i++)
                {
                    Vector3 point = windowPoints[i];
                    glow.SetPosition(i, point);
                    core.SetPosition(i, point + Vector3.up * 0.012f);
                }
            }

            public void HideWindow()
            {
                glow.positionCount = 0;
                core.positionCount = 0;
            }

            public void SetVisibility(float visibility)
            {
                glowProperties.SetFloat(VisibilityId, visibility);
                coreProperties.SetFloat(VisibilityId, visibility);
                glow.SetPropertyBlock(glowProperties);
                core.SetPropertyBlock(coreProperties);
            }

            public void SetActive(bool active)
            {
                if (root.activeSelf != active)
                {
                    root.SetActive(active);
                }

                if (!active)
                {
                    HideWindow();
                }
            }

            private void EnsurePathCapacity(int pointCount)
            {
                if (pathPoints.Length != pointCount)
                {
                    pathPoints = new Vector3[pointCount];
                    cumulativeDistances = new float[pointCount];
                }

                int requiredWindowCapacity = pointCount + 2;

                if (windowPoints.Length < requiredWindowCapacity)
                {
                    windowPoints = new Vector3[requiredWindowCapacity];
                }
            }

            private int BuildWindow(float tailDistance, float headDistance)
            {
                int count = 0;
                windowPoints[count++] = EvaluatePosition(tailDistance);

                for (int i = 1; i < cumulativeDistances.Length - 1; i++)
                {
                    float distance = cumulativeDistances[i];

                    if (distance > tailDistance && distance < headDistance)
                    {
                        windowPoints[count++] = pathPoints[i];
                    }
                }

                Vector3 headPoint = EvaluatePosition(headDistance);

                if ((windowPoints[count - 1] - headPoint).sqrMagnitude > 0.000001f)
                {
                    windowPoints[count++] = headPoint;
                }

                return count;
            }

            private Vector3 EvaluatePosition(float distance)
            {
                if (distance <= 0f)
                {
                    return pathPoints[0];
                }

                if (distance >= totalLength)
                {
                    return pathPoints[pathPoints.Length - 1];
                }

                int upperIndex = FindUpperPointIndex(distance);
                int lowerIndex = Mathf.Max(0, upperIndex - 1);
                float lowerDistance = cumulativeDistances[lowerIndex];
                float upperDistance = cumulativeDistances[upperIndex];
                float segmentLength = Mathf.Max(0.0001f, upperDistance - lowerDistance);
                float t = Mathf.Clamp01((distance - lowerDistance) / segmentLength);
                return Vector3.LerpUnclamped(pathPoints[lowerIndex], pathPoints[upperIndex], t);
            }

            private int FindUpperPointIndex(float distance)
            {
                int low = 1;
                int high = cumulativeDistances.Length - 1;

                while (low < high)
                {
                    int mid = (low + high) >> 1;

                    if (cumulativeDistances[mid] < distance)
                    {
                        low = mid + 1;
                    }
                    else
                    {
                        high = mid;
                    }
                }

                return low;
            }
        }
    }
}
