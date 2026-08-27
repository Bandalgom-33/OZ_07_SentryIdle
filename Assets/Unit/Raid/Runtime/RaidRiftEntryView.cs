using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Raid.Data;
using UnityEngine;
using UnityEngine.Rendering;

namespace EndlessGuard.Unit.Raid.Runtime
{
    [DisallowMultipleComponent]
    public sealed class RaidRiftEntryView : MonoBehaviour
    {
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int RevealProgressId = Shader.PropertyToID("_RevealProgress");

        private readonly Dictionary<Vector2Int, RiftVisual> visuals = new Dictionary<Vector2Int, RiftVisual>(8);
        private readonly List<Vector2Int> desiredEntries = new List<Vector2Int>(8);
        private readonly List<Vector2Int> staleEntries = new List<Vector2Int>(8);
        private RaidBattleController battle;
        private RaidBoardRuntime board;
        private RaidBoardView boardView;
        private RaidTileVisualSetSO visualSet;

        private void Awake()
        {
            battle = GetComponent<RaidBattleController>();
            board = GetComponent<RaidBoardRuntime>();
            boardView = board != null ? board.BoardView : null;
            visualSet = boardView != null ? boardView.VisualSet : null;
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (battle == null || board == null)
            {
                Debug.LogError("RaidRiftEntryView는 RaidBattleController와 RaidBoardRuntime이 필요합니다.", this);
                enabled = false;
                return;
            }

            battle.OnRaidPreparing += HandleRaidPreparing;
            battle.OnRaidStarted += HandleRaidStarted;
            battle.OnRaidEnded += HandleRaidEnded;
            battle.OnPhaseTransitionStarted += HandlePhaseTransitionStarted;
            battle.OnPhaseTransitionCompleted += HandlePhaseTransitionCompleted;
        }

        private void Start()
        {
            RefreshImmediate();
        }

        private void OnDisable()
        {
            if (battle != null)
            {
                battle.OnRaidPreparing -= HandleRaidPreparing;
                battle.OnRaidStarted -= HandleRaidStarted;
                battle.OnRaidEnded -= HandleRaidEnded;
                battle.OnPhaseTransitionStarted -= HandlePhaseTransitionStarted;
                battle.OnPhaseTransitionCompleted -= HandlePhaseTransitionCompleted;
            }

            Clear();
        }

        private void Update()
        {
            if (visuals.Count == 0)
            {
                return;
            }

            float deltaTime = Mathf.Max(0f, Time.unscaledDeltaTime);

            foreach (KeyValuePair<Vector2Int, RiftVisual> pair in visuals)
            {
                pair.Value.Step(deltaTime);
            }
        }

        private void HandleRaidPreparing()
        {
            if (board != null && board.Phase == RaidPhase.Phase1)
            {
                Clear();
            }
        }

        private void HandleRaidStarted()
        {
            RefreshImmediate();
        }

        private void HandleRaidEnded(RaidBattleResult result)
        {
            Clear();
        }

        private void HandlePhaseTransitionStarted(RaidPhaseTransitionInfo info)
        {
            if (!CanShowRifts())
            {
                Clear();
                return;
            }

            EnsureForPhase(info.ToPhase, Mathf.Max(0.5f, info.Duration));
        }

        private void HandlePhaseTransitionCompleted(RaidPhaseTransitionInfo info)
        {
            if (!CanShowRifts())
            {
                Clear();
                return;
            }

            EnsureForPhase(info.ToPhase, 0f);
        }

        private void RefreshImmediate()
        {
            ResolveReferences();

            if (!CanShowRifts() || board.Board == null)
            {
                Clear();
                return;
            }

            EnsureForPhase(board.Phase, 0f);
        }

        private void ResolveReferences()
        {
            if (battle == null)
            {
                battle = GetComponent<RaidBattleController>();
            }

            if (board == null)
            {
                board = GetComponent<RaidBoardRuntime>();
            }

            if (boardView == null && board != null)
            {
                boardView = board.BoardView;
            }

            if (visualSet == null && boardView != null)
            {
                visualSet = boardView.VisualSet;
            }
        }

        private bool CanShowRifts()
        {
            return board != null && board.Family != null && board.Family.ShowRiftEntries && boardView != null && visualSet != null && visualSet.CollapseCrackMaterial != null && (!board.Family.ShowRiftEntryBeams || visualSet.CollapseBeamMaterial != null);
        }

        private void EnsureForPhase(RaidPhase phase, float openingDuration)
        {
            if (board == null || board.Board == null || !board.TryGetMapData(RaidPhase.Phase1, out RaidMapSO phase1Map) || !board.TryGetMapData(phase, out RaidMapSO targetMap))
            {
                return;
            }

            CollectRiftEntries(phase1Map, targetMap, desiredEntries);
            staleEntries.Clear();

            foreach (KeyValuePair<Vector2Int, RiftVisual> pair in visuals)
            {
                if (!desiredEntries.Contains(pair.Key))
                {
                    staleEntries.Add(pair.Key);
                }
            }

            for (int i = 0; i < staleEntries.Count; i++)
            {
                Vector2Int coordinate = staleEntries[i];
                visuals[coordinate].Dispose();
                visuals.Remove(coordinate);
            }

            for (int i = 0; i < desiredEntries.Count; i++)
            {
                Vector2Int coordinate = desiredEntries[i];

                if (visuals.TryGetValue(coordinate, out RiftVisual existing))
                {
                    if (openingDuration <= 0f)
                    {
                        existing.FinishOpening();
                    }

                    continue;
                }

                Vector3 worldPosition = board.Board.TileToWorld(coordinate);
                RiftVisual visual = new RiftVisual(boardView.transform, visualSet.CollapseCrackMaterial, visualSet.CollapseBeamMaterial, board.Family.ShowRiftEntryBeams, worldPosition, board.Board.TileSize, coordinate);
                visuals.Add(coordinate, visual);

                if (openingDuration > 0f)
                {
                    visual.Open(openingDuration);
                }
                else
                {
                    visual.FinishOpening();
                }
            }
        }

        private static void CollectRiftEntries(RaidMapSO phase1Map, RaidMapSO targetMap, List<Vector2Int> output)
        {
            output.Clear();

            if (phase1Map == null || targetMap == null || targetMap.Phase == RaidPhase.Phase1)
            {
                return;
            }

            for (int i = 0; i < targetMap.NodeCount; i++)
            {
                RaidMapNodeData node = targetMap.GetNode(i);

                if (node.Type != RaidMapNodeType.Entry || HasEntry(phase1Map, node.Coordinate) || output.Contains(node.Coordinate))
                {
                    continue;
                }

                output.Add(node.Coordinate);
            }
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

        private void Clear()
        {
            foreach (KeyValuePair<Vector2Int, RiftVisual> pair in visuals)
            {
                pair.Value.Dispose();
            }

            visuals.Clear();
            desiredEntries.Clear();
            staleEntries.Clear();
        }

        private sealed class RiftVisual
        {
            private readonly GameObject root;
            private readonly Mesh groundMesh;
            private readonly Mesh beamMesh;
            private readonly MeshRenderer groundRenderer;
            private readonly MeshRenderer beamRenderer;
            private readonly MaterialPropertyBlock groundProperties = new MaterialPropertyBlock();
            private readonly MaterialPropertyBlock beamProperties = new MaterialPropertyBlock();
            private readonly float pulseOffset;
            private float time;
            private float openingDuration;
            private float openingElapsed;
            private bool opening;

            public RiftVisual(Transform parent, Material groundMaterial, Material beamMaterial, bool showBeam, Vector3 worldPosition, float tileSize, Vector2Int coordinate)
            {
                root = new GameObject($"RiftEntry_{coordinate.x:00}_{coordinate.y:00}");
                root.layer = parent.gameObject.layer;
                root.transform.SetParent(parent, true);
                root.transform.position = worldPosition + Vector3.up * 0.08f;
                root.transform.rotation = Quaternion.identity;
                root.transform.localScale = Vector3.one;
                pulseOffset = (coordinate.x * 0.73f + coordinate.y * 1.17f) % 6.28318f;

                groundMesh = BuildGroundMesh(tileSize);
                groundRenderer = CreateRenderer(root.transform, "RiftGround", groundMesh, groundMaterial, 58);

                if (showBeam && beamMaterial != null)
                {
                    beamMesh = BuildBeamMesh(tileSize);
                    beamRenderer = CreateRenderer(root.transform, "RiftAurora", beamMesh, beamMaterial, 59);
                }

                groundProperties.SetFloat(RevealProgressId, 1f);
                Apply(0f, 0f, 0.62f);
            }

            public void Open(float duration)
            {
                opening = true;
                openingDuration = Mathf.Max(0.5f, duration);
                openingElapsed = 0f;
                Apply(0f, 0f, 0.62f);
            }

            public void FinishOpening()
            {
                opening = false;
                openingElapsed = openingDuration;
                ApplyPersistent();
            }

            public void Step(float deltaTime)
            {
                time += deltaTime;

                if (opening)
                {
                    openingElapsed += deltaTime;
                    float normalized = openingDuration > 0f ? Mathf.Clamp01(openingElapsed / openingDuration) : 1f;
                    float reveal = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.08f, 0.74f, normalized));
                    float surge = Mathf.Sin(Mathf.Clamp01(normalized / 0.76f) * Mathf.PI) * 0.42f;
                    float flicker = 0.94f + Mathf.Sin(time * 12.8f + pulseOffset) * 0.06f;
                    float energy = reveal * (1f + surge) * flicker;
                    float scale = Mathf.Lerp(0.58f, 1.06f, reveal);
                    Apply(0.9f * energy, 1.12f * energy, scale);

                    if (normalized >= 1f)
                    {
                        opening = false;
                    }

                    return;
                }

                ApplyPersistent();
            }

            public void Dispose()
            {
                if (root != null)
                {
                    UnityEngine.Object.Destroy(root);
                }

                if (groundMesh != null)
                {
                    UnityEngine.Object.Destroy(groundMesh);
                }

                if (beamMesh != null)
                {
                    UnityEngine.Object.Destroy(beamMesh);
                }
            }

            private void ApplyPersistent()
            {
                float slowPulse = 0.88f + Mathf.Sin(time * 2.7f + pulseOffset) * 0.08f + Mathf.Sin(time * 5.3f + pulseOffset * 0.7f) * 0.04f;
                Apply(0.58f * slowPulse, 0.66f * slowPulse, 1f);
            }

            private void Apply(float groundIntensity, float beamIntensity, float scale)
            {
                if (root != null)
                {
                    root.transform.localScale = Vector3.one * Mathf.Max(0.1f, scale);
                }

                if (groundRenderer != null)
                {
                    groundProperties.SetFloat(IntensityId, Mathf.Clamp(groundIntensity, 0f, 2f));
                    groundRenderer.SetPropertyBlock(groundProperties);
                }

                if (beamRenderer != null)
                {
                    beamProperties.SetFloat(IntensityId, Mathf.Clamp(beamIntensity, 0f, 2f));
                    beamRenderer.SetPropertyBlock(beamProperties);
                }
            }

            private static MeshRenderer CreateRenderer(Transform parent, string name, Mesh mesh, Material material, int sortingOrder)
            {
                GameObject instance = new GameObject(name);
                instance.layer = parent.gameObject.layer;
                instance.transform.SetParent(parent, false);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;
                MeshFilter filter = instance.AddComponent<MeshFilter>();
                filter.sharedMesh = mesh;
                MeshRenderer renderer = instance.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
                renderer.sortingOrder = sortingOrder;
                return renderer;
            }

            private static Mesh BuildGroundMesh(float tileSize)
            {
                const int segments = 28;
                float innerRadius = tileSize * 0.18f;
                float middleRadius = tileSize * 0.34f;
                float outerRadius = tileSize * 0.54f;
                Vector3[] vertices = new Vector3[(segments + 1) * 3];
                Vector2[] uvs = new Vector2[vertices.Length];
                Color[] colors = new Color[vertices.Length];
                int[] triangles = new int[segments * 12];
                int triangleIndex = 0;

                for (int i = 0; i <= segments; i++)
                {
                    float t = i / (float)segments;
                    float angle = t * Mathf.PI * 2f;
                    Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                    int baseIndex = i * 3;
                    vertices[baseIndex] = direction * innerRadius;
                    vertices[baseIndex + 1] = direction * middleRadius;
                    vertices[baseIndex + 2] = direction * outerRadius;
                    uvs[baseIndex] = new Vector2(0f, t);
                    uvs[baseIndex + 1] = new Vector2(0.5f, t);
                    uvs[baseIndex + 2] = new Vector2(1f, t);
                    colors[baseIndex] = Color.white;
                    colors[baseIndex + 1] = Color.white;
                    colors[baseIndex + 2] = Color.white;

                    if (i >= segments)
                    {
                        continue;
                    }

                    int next = baseIndex + 3;
                    triangles[triangleIndex++] = baseIndex;
                    triangles[triangleIndex++] = next + 1;
                    triangles[triangleIndex++] = baseIndex + 1;
                    triangles[triangleIndex++] = baseIndex;
                    triangles[triangleIndex++] = next;
                    triangles[triangleIndex++] = next + 1;
                    triangles[triangleIndex++] = baseIndex + 1;
                    triangles[triangleIndex++] = next + 2;
                    triangles[triangleIndex++] = baseIndex + 2;
                    triangles[triangleIndex++] = baseIndex + 1;
                    triangles[triangleIndex++] = next + 1;
                    triangles[triangleIndex++] = next + 2;
                }

                Mesh mesh = new Mesh { name = "RaidRiftGroundMesh" };
                mesh.vertices = vertices;
                mesh.uv = uvs;
                mesh.colors = colors;
                mesh.triangles = triangles;
                mesh.RecalculateBounds();
                return mesh;
            }

            private static Mesh BuildBeamMesh(float tileSize)
            {
                const int planeCount = 3;
                Vector3[] vertices = new Vector3[planeCount * 4];
                Vector2[] uvs = new Vector2[vertices.Length];
                Color[] colors = new Color[vertices.Length];
                int[] triangles = new int[planeCount * 6];
                float halfWidth = tileSize * 0.31f;
                float height = tileSize * 3.4f;

                for (int plane = 0; plane < planeCount; plane++)
                {
                    float angle = plane * 60f * Mathf.Deg2Rad;
                    Vector3 right = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * halfWidth;
                    int vertexIndex = plane * 4;
                    int triangleIndex = plane * 6;
                    vertices[vertexIndex] = -right;
                    vertices[vertexIndex + 1] = right;
                    vertices[vertexIndex + 2] = right + Vector3.up * height;
                    vertices[vertexIndex + 3] = -right + Vector3.up * height;
                    uvs[vertexIndex] = new Vector2(0f, 0f);
                    uvs[vertexIndex + 1] = new Vector2(1f, 0f);
                    uvs[vertexIndex + 2] = new Vector2(1f, 1f);
                    uvs[vertexIndex + 3] = new Vector2(0f, 1f);
                    colors[vertexIndex] = Color.white;
                    colors[vertexIndex + 1] = Color.white;
                    colors[vertexIndex + 2] = Color.white;
                    colors[vertexIndex + 3] = Color.white;
                    triangles[triangleIndex] = vertexIndex;
                    triangles[triangleIndex + 1] = vertexIndex + 1;
                    triangles[triangleIndex + 2] = vertexIndex + 2;
                    triangles[triangleIndex + 3] = vertexIndex;
                    triangles[triangleIndex + 4] = vertexIndex + 2;
                    triangles[triangleIndex + 5] = vertexIndex + 3;
                }

                Mesh mesh = new Mesh { name = "RaidRiftBeamMesh" };
                mesh.vertices = vertices;
                mesh.uv = uvs;
                mesh.colors = colors;
                mesh.triangles = triangles;
                mesh.RecalculateBounds();
                return mesh;
            }
        }
    }
}
