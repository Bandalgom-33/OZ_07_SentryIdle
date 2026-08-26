using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace EndlessGuard.Unit.Raid.Runtime
{
    internal sealed class RaidBossLightningVfxPool
    {
        internal sealed class Entry
        {
            public GameObject Instance;
            public ParticleSystem[] Particles;
            public GameObject[] ImpactObjects;
            public ParticleSystem[][] ImpactParticles;
            public Vector3[] ImpactBaseScales;
            public Material[] RevealMaterials;
            public LineRenderer BoltGlow;
            public LineRenderer BoltCore;
            public LineRenderer[] BoltBranches;
            public LineRenderer ShockwaveRing;
            public LineRenderer[] GroundArcs;
            public Vector3[] BoltPoints;
            public Vector3[][] BoltBranchPoints;
            public Vector3[] ShockwavePoints;
            public Vector3[][] GroundArcPoints;
            public int[] BoltBranchStartIndices;
            public int ActiveBranchCount;
            public float ImpactFeedbackStartTime;
            public float ShockwaveRadius;
            public float ShockwaveWidth;
            public bool ImpactFeedbackActive;
            public int VariantIndex;
            public float BoltWidth;
            public float BoltHideTime;
            public float ReleaseTime;
            public bool TimedRelease;
            public bool InUse;
        }

        private const int PrewarmCount = 12;
        private const int MaximumPoolSize = 16;
        private const int MainBoltPointCount = 21;
        private const int MaximumBranchCount = 3;
        private const int BranchPointCount = 6;
        private const int ShockwavePointCount = 32;
        private const int GroundArcCount = 3;
        private const int GroundArcPointCount = 5;
        private const float BranchRevealDuration = 0.24f;
        private const float BoltImpactVisibleTime = 0.14f;
        private const float BoltImpactWidthMultiplier = 1.25f;
        private const float ShockwaveDuration = 0.28f;
        private const float GroundArcDuration = 0.42f;
        private const float ShockwaveRadiusMultiplier = 1.65f;
        private const float ShockwaveGroundOffset = 0.08f;
        private static readonly int RevealProgressId = Shader.PropertyToID("_RevealProgress");
        private static readonly int RevealFeatherId = Shader.PropertyToID("_RevealFeather");

        private static readonly string[] VariantPaths =
        {
            "Vfx/BossLightning/Prefabs/VFX_Zap_04_Green",
            "Vfx/BossLightning/Prefabs/VFX_Zap_02_Blue",
            "Vfx/BossLightning/Prefabs/VFX_Zap_05_Purple",
            "Vfx/BossLightning/Prefabs/VFX_Zap_03_Yellow",
            "Vfx/BossLightning/Prefabs/VFX_Zap_06_White"
        };

        private static readonly string[] BoltMaterialPaths =
        {
            "Vfx/BossLightning/Materials/M_VFX_Zap_Lightning_04_LUT_Add_Vertical",
            "Vfx/BossLightning/Materials/M_VFX_Zap_Lightning_02_LUT_Add_Vertical",
            "Vfx/BossLightning/Materials/M_VFX_Zap_Lightning_05_LUT_Add_Vertical",
            "Vfx/BossLightning/Materials/M_VFX_Zap_Lightning_03_LUT_Add_Vertical",
            "Vfx/BossLightning/Materials/M_VFX_Zap_Lightning_06_LUT_Add_Vertical"
        };

        private const string BoltCoreMaterialPath = "Vfx/BossLightning/Materials/M_VFX_Zap_Lightning_06_LUT_Add_Vertical";

        private readonly Transform root;
        private readonly GameObject[] variants = new GameObject[VariantPaths.Length];
        private readonly Material[] boltMaterials = new Material[BoltMaterialPaths.Length];
        private readonly List<Entry> entries = new List<Entry>(PrewarmCount);
        private Material boltCoreMaterial;
        private bool boltMaterialsReady;
        private int timedEntryCount;
        private uint boltShapeSequence = 0x9E3779B9u;

        public int VariantCount => variants.Length;
        public bool HasTimedEntries => timedEntryCount > 0;
        public bool IsReady { get; private set; }

        public RaidBossLightningVfxPool(Transform root)
        {
            this.root = root;
            IsReady = LoadVariants();
            boltMaterialsReady = LoadBoltMaterials();

            if (!IsReady)
            {
                return;
            }

            for (int i = 0; i < PrewarmCount; i++)
            {
                CreateEntry(i % variants.Length);
            }
        }

        public Entry BeginTravel(Vector3 position, float scale, int variantIndex, float simulationSpeed, float skyHeight, float boltWidth)
        {
            Entry entry = GetAvailableEntry(variantIndex);
            if (entry == null || entry.Instance == null)
            {
                return null;
            }

            PrepareTravel(entry);
            entry.Instance.transform.SetPositionAndRotation(position, Quaternion.identity);
            entry.Instance.transform.localScale = Vector3.one * Mathf.Max(0.1f, scale);
            uint shapeSeed = NextShapeSeed(position, variantIndex);
            BuildSkyBolt(entry, position, Mathf.Max(1f, skyHeight), Mathf.Max(0.05f, boltWidth), shapeSeed);
            SetReveal(entry, 0f);
            SetBoltReveal(entry, 0f);
            entry.Instance.SetActive(true);
            PlayActiveParticles(entry.Particles, simulationSpeed);
            entry.InUse = true;
            entry.TimedRelease = false;
            entry.BoltHideTime = float.PositiveInfinity;
            entry.ReleaseTime = float.PositiveInfinity;
            return entry;
        }

        public void SetReveal(Entry entry, float progress)
        {
            if (entry == null || entry.RevealMaterials == null)
            {
                return;
            }

            float revealProgress = Mathf.Clamp01(progress);
            for (int i = 0; i < entry.RevealMaterials.Length; i++)
            {
                Material material = entry.RevealMaterials[i];
                if (material == null)
                {
                    continue;
                }

                material.SetFloat(RevealProgressId, revealProgress);
                material.SetFloat(RevealFeatherId, 0.035f);
            }
        }

        public void SetBoltReveal(Entry entry, float progress)
        {
            if (entry == null || entry.BoltPoints == null || entry.BoltPoints.Length < 2)
            {
                return;
            }

            float revealProgress = Mathf.Clamp01(progress);
            ApplyPartialLine(entry.BoltGlow, entry.BoltPoints, revealProgress);
            ApplyPartialLine(entry.BoltCore, entry.BoltPoints, revealProgress);

            if (entry.BoltBranches == null || entry.BoltBranchPoints == null || entry.BoltBranchStartIndices == null)
            {
                return;
            }

            int branchCount = Mathf.Min(entry.ActiveBranchCount, Mathf.Min(entry.BoltBranches.Length, Mathf.Min(entry.BoltBranchPoints.Length, entry.BoltBranchStartIndices.Length)));
            for (int i = 0; i < entry.BoltBranches.Length; i++)
            {
                LineRenderer branch = entry.BoltBranches[i];
                if (i >= branchCount)
                {
                    HideLine(branch);
                    continue;
                }

                Vector3[] points = entry.BoltBranchPoints[i];
                if (branch == null || points == null || points.Length < 2)
                {
                    continue;
                }

                float branchStart = (float)entry.BoltBranchStartIndices[i] / (MainBoltPointCount - 1);
                float branchProgress = Mathf.Clamp01((revealProgress - branchStart) / BranchRevealDuration);
                ApplyPartialLine(branch, points, branchProgress);
            }
        }

        public void PromoteToImpact(Entry entry, float travelScale, float impactScale, float simulationSpeed, float lifetime, float now)
        {
            if (entry == null || !entry.InUse)
            {
                return;
            }

            float ratio = Mathf.Max(0.1f, impactScale) / Mathf.Max(0.1f, travelScale);
            SetBoltReveal(entry, 1f);
            SetBoltWidths(entry, BoltImpactWidthMultiplier);
            entry.BoltHideTime = now + BoltImpactVisibleTime;

            for (int i = 0; i < entry.ImpactObjects.Length; i++)
            {
                GameObject impactObject = entry.ImpactObjects[i];
                if (impactObject == null)
                {
                    continue;
                }

                impactObject.transform.localScale = entry.ImpactBaseScales[i] * ratio;
                impactObject.SetActive(true);
                PlayActiveParticles(entry.ImpactParticles[i], simulationSpeed);
            }

            if (!entry.TimedRelease)
            {
                timedEntryCount++;
            }

            BeginImpactFeedback(entry, impactScale, now);
            entry.TimedRelease = true;
            entry.ReleaseTime = now + Mathf.Max(0.1f, lifetime);
        }

        public void ReleaseTravel(Entry entry)
        {
            Release(entry);
        }

        public void ReleaseExpired(float now)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                if (entry == null || !entry.InUse)
                {
                    continue;
                }

                UpdateImpactFeedback(entry, now);

                if (entry.BoltHideTime > 0f && now >= entry.BoltHideTime)
                {
                    HideBolt(entry);
                    entry.BoltHideTime = 0f;
                }

                if (!entry.TimedRelease || now < entry.ReleaseTime)
                {
                    continue;
                }

                Release(entry);
            }
        }

        public void StopAll()
        {
            for (int i = 0; i < entries.Count; i++)
            {
                Release(entries[i]);
            }

            timedEntryCount = 0;
        }

        public void Dispose()
        {
            StopAll();

            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                if (entry == null || entry.RevealMaterials == null)
                {
                    continue;
                }

                for (int materialIndex = 0; materialIndex < entry.RevealMaterials.Length; materialIndex++)
                {
                    Material material = entry.RevealMaterials[materialIndex];
                    if (material != null)
                    {
                        Object.Destroy(material);
                    }
                }
            }

            entries.Clear();
        }

        private bool LoadVariants()
        {
            bool loadedAll = true;

            for (int i = 0; i < VariantPaths.Length; i++)
            {
                variants[i] = Resources.Load<GameObject>(VariantPaths[i]);
                if (variants[i] == null)
                {
                    Debug.LogError($"Raid boss lightning VFX is missing from Resources: {VariantPaths[i]}");
                    loadedAll = false;
                }
            }

            return loadedAll;
        }

        private bool LoadBoltMaterials()
        {
            bool loadedAll = true;

            for (int i = 0; i < BoltMaterialPaths.Length; i++)
            {
                boltMaterials[i] = Resources.Load<Material>(BoltMaterialPaths[i]);
                if (boltMaterials[i] == null)
                {
                    Debug.LogWarning($"Raid boss sky lightning material is missing from Resources: {BoltMaterialPaths[i]}");
                    loadedAll = false;
                }
            }

            boltCoreMaterial = Resources.Load<Material>(BoltCoreMaterialPath);
            if (boltCoreMaterial == null)
            {
                Debug.LogWarning($"Raid boss sky lightning core material is missing from Resources: {BoltCoreMaterialPath}");
                loadedAll = false;
            }

            return loadedAll;
        }

        private Entry GetAvailableEntry(int variantIndex)
        {
            int normalizedVariant = NormalizeVariantIndex(variantIndex);

            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                if (entry != null && entry.VariantIndex == normalizedVariant && !entry.InUse)
                {
                    return entry;
                }
            }

            if (entries.Count < MaximumPoolSize)
            {
                return CreateEntry(normalizedVariant);
            }

            Entry oldest = null;
            for (int i = 0; i < entries.Count; i++)
            {
                Entry candidate = entries[i];
                if (candidate == null || candidate.VariantIndex != normalizedVariant)
                {
                    continue;
                }

                if (oldest == null || candidate.ReleaseTime < oldest.ReleaseTime)
                {
                    oldest = candidate;
                }
            }

            if (oldest != null)
            {
                Release(oldest);
            }

            return oldest;
        }

        private Entry CreateEntry(int variantIndex)
        {
            int normalizedVariant = NormalizeVariantIndex(variantIndex);
            GameObject prefab = variants[normalizedVariant];
            if (prefab == null)
            {
                return null;
            }

            GameObject instance = Object.Instantiate(prefab, root);
            instance.name = $"BossLightning_{normalizedVariant + 1:00}";
            instance.SetActive(false);

            ParticleSystem[] particles = instance.GetComponentsInChildren<ParticleSystem>(true);
            Transform[] transforms = instance.GetComponentsInChildren<Transform>(true);
            List<GameObject> impactObjects = new List<GameObject>(4);
            List<ParticleSystem[]> impactParticles = new List<ParticleSystem[]>(4);
            List<Vector3> impactBaseScales = new List<Vector3>(4);
            List<Material> revealMaterials = new List<Material>(4);

            for (int i = 0; i < transforms.Length; i++)
            {
                Transform child = transforms[i];
                if (child == null || child == instance.transform)
                {
                    continue;
                }

                if (IsImpactObject(child.name))
                {
                    impactObjects.Add(child.gameObject);
                    impactParticles.Add(child.GetComponentsInChildren<ParticleSystem>(true));
                    impactBaseScales.Add(child.localScale);
                }

                if (!IsRevealObject(child.name))
                {
                    continue;
                }

                ParticleSystemRenderer renderer = child.GetComponent<ParticleSystemRenderer>();
                if (renderer != null && renderer.sharedMaterial != null)
                {
                    Material revealMaterial = new Material(renderer.sharedMaterial)
                    {
                        name = $"{renderer.sharedMaterial.name}_Reveal"
                    };
                    renderer.sharedMaterial = revealMaterial;
                    revealMaterials.Add(revealMaterial);
                }
            }

            LineRenderer boltGlow = null;
            LineRenderer boltCore = null;
            LineRenderer[] boltBranches = new LineRenderer[MaximumBranchCount];
            LineRenderer shockwaveRing = null;
            LineRenderer[] groundArcs = new LineRenderer[GroundArcCount];

            if (boltMaterialsReady)
            {
                boltGlow = CreateBoltLine(instance.transform, "SkyBoltGlow", boltMaterials[normalizedVariant], 24);
                boltCore = CreateBoltLine(instance.transform, "SkyBoltCore", boltCoreMaterial, 25);
                for (int i = 0; i < boltBranches.Length; i++)
                {
                    boltBranches[i] = CreateBoltLine(instance.transform, $"SkyBoltBranch_{i + 1:00}", boltMaterials[normalizedVariant], 23);
                }

                shockwaveRing = CreateImpactLine(instance.transform, "ImpactShockwave", boltCoreMaterial, 26, true, false);
                for (int i = 0; i < groundArcs.Length; i++)
                {
                    groundArcs[i] = CreateImpactLine(instance.transform, $"ImpactGroundArc_{i + 1:00}", boltMaterials[normalizedVariant], 24, false, true);
                }
            }

            Entry entry = new Entry
            {
                Instance = instance,
                Particles = particles,
                ImpactObjects = impactObjects.ToArray(),
                ImpactParticles = impactParticles.ToArray(),
                ImpactBaseScales = impactBaseScales.ToArray(),
                RevealMaterials = revealMaterials.ToArray(),
                BoltGlow = boltGlow,
                BoltCore = boltCore,
                BoltBranches = boltBranches,
                ShockwaveRing = shockwaveRing,
                GroundArcs = groundArcs,
                BoltPoints = new Vector3[MainBoltPointCount],
                BoltBranchPoints = CreateBranchPointBuffers(),
                ShockwavePoints = new Vector3[ShockwavePointCount],
                GroundArcPoints = CreateGroundArcPointBuffers(),
                BoltBranchStartIndices = new int[MaximumBranchCount],
                VariantIndex = normalizedVariant
            };

            entries.Add(entry);
            return entry;
        }

        private void PrepareTravel(Entry entry)
        {
            StopParticles(entry.Particles);
            HideBolt(entry);
            HideImpactFeedback(entry);
            entry.ActiveBranchCount = 0;
            entry.BoltHideTime = 0f;
            SetReveal(entry, 1f);

            for (int i = 0; i < entry.ImpactObjects.Length; i++)
            {
                GameObject impactObject = entry.ImpactObjects[i];
                if (impactObject == null)
                {
                    continue;
                }

                impactObject.transform.localScale = entry.ImpactBaseScales[i];
                impactObject.SetActive(false);
            }
        }

        private void Release(Entry entry)
        {
            if (entry == null || !entry.InUse)
            {
                return;
            }

            if (entry.TimedRelease && timedEntryCount > 0)
            {
                timedEntryCount--;
            }

            StopParticles(entry.Particles);
            HideBolt(entry);
            HideImpactFeedback(entry);

            for (int i = 0; i < entry.ImpactObjects.Length; i++)
            {
                GameObject impactObject = entry.ImpactObjects[i];
                if (impactObject != null)
                {
                    impactObject.transform.localScale = entry.ImpactBaseScales[i];
                    impactObject.SetActive(true);
                }
            }

            SetReveal(entry, 1f);

            if (entry.Instance != null)
            {
                entry.Instance.SetActive(false);
            }

            entry.BoltHideTime = 0f;
            entry.ReleaseTime = 0f;
            entry.TimedRelease = false;
            entry.InUse = false;
        }

        private static Vector3[][] CreateBranchPointBuffers()
        {
            Vector3[][] buffers = new Vector3[MaximumBranchCount][];
            for (int i = 0; i < buffers.Length; i++)
            {
                buffers[i] = new Vector3[BranchPointCount];
            }

            return buffers;
        }

        private static Vector3[][] CreateGroundArcPointBuffers()
        {
            Vector3[][] buffers = new Vector3[GroundArcCount][];
            for (int i = 0; i < buffers.Length; i++)
            {
                buffers[i] = new Vector3[GroundArcPointCount];
            }

            return buffers;
        }

        private static LineRenderer CreateBoltLine(Transform parent, string objectName, Material material, int sortingOrder)
        {
            if (parent == null || material == null)
            {
                return null;
            }

            GameObject lineObject = new GameObject(objectName);
            Transform lineTransform = lineObject.transform;
            lineTransform.SetParent(parent, false);

            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.sharedMaterial = material;
            line.textureMode = LineTextureMode.Stretch;
            line.alignment = LineAlignment.View;
            line.widthMultiplier = 1f;
            line.widthCurve = new AnimationCurve(new Keyframe(0f, 0.4f), new Keyframe(0.12f, 1f), new Keyframe(0.82f, 0.82f), new Keyframe(1f, 0.18f));
            line.numCornerVertices = 2;
            line.numCapVertices = 0;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.lightProbeUsage = LightProbeUsage.Off;
            line.reflectionProbeUsage = ReflectionProbeUsage.Off;
            line.sortingOrder = sortingOrder;
            line.positionCount = 0;
            line.enabled = false;
            return line;
        }

        private static LineRenderer CreateImpactLine(Transform parent, string objectName, Material material, int sortingOrder, bool loop, bool tapered)
        {
            if (parent == null || material == null)
            {
                return null;
            }

            GameObject lineObject = new GameObject(objectName);
            lineObject.transform.SetParent(parent, false);

            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.sharedMaterial = material;
            line.textureMode = LineTextureMode.Stretch;
            line.alignment = LineAlignment.View;
            line.loop = loop;
            line.widthMultiplier = 0.1f;
            line.widthCurve = tapered ? new AnimationCurve(new Keyframe(0f, 0.1f), new Keyframe(0.18f, 1f), new Keyframe(0.78f, 0.72f), new Keyframe(1f, 0.05f)) : AnimationCurve.Constant(0f, 1f, 1f);
            line.numCornerVertices = loop ? 2 : 1;
            line.numCapVertices = 0;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.lightProbeUsage = LightProbeUsage.Off;
            line.reflectionProbeUsage = ReflectionProbeUsage.Off;
            line.sortingOrder = sortingOrder;
            line.positionCount = 0;
            line.enabled = false;
            return line;
        }

        private static void BuildSkyBolt(Entry entry, Vector3 strikePosition, float skyHeight, float boltWidth, uint seed)
        {
            if (entry == null || entry.BoltPoints == null || entry.BoltPoints.Length != MainBoltPointCount)
            {
                return;
            }

            uint randomState = seed != 0u ? seed : 0xA341316Cu;
            float actualHeight = skyHeight * NextRange(ref randomState, 0.92f, 1.08f);
            float actualWidth = boltWidth * NextRange(ref randomState, 0.9f, 1.12f);
            float lateralJitter = Mathf.Max(0.22f, actualWidth * NextRange(ref randomState, 1.8f, 2.55f));
            Vector2 topOffset2D = NextInsideUnitCircle(ref randomState) * actualWidth * NextRange(ref randomState, 0.55f, 1.45f);
            Vector2 sweepDirection = NextInsideUnitCircle(ref randomState);
            if (sweepDirection.sqrMagnitude < 0.001f)
            {
                sweepDirection = Vector2.right;
            }

            sweepDirection.Normalize();
            float sweepStrength = actualWidth * NextRange(ref randomState, 0.5f, 1.6f);
            float sweepCycles = NextRange(ref randomState, 1.15f, 2.35f);
            float sweepPhase = NextRange(ref randomState, -0.7f, 0.7f);
            Vector3 top = strikePosition + Vector3.up * actualHeight + new Vector3(topOffset2D.x, 0f, topOffset2D.y);
            Vector2 previousJitter = Vector2.zero;

            for (int i = 0; i < MainBoltPointCount; i++)
            {
                float t = (float)i / (MainBoltPointCount - 1);
                Vector3 point = Vector3.Lerp(top, strikePosition, t);

                if (i > 0 && i < MainBoltPointCount - 1)
                {
                    float envelope = Mathf.Pow(Mathf.Sin(t * Mathf.PI), 0.82f);
                    Vector2 impulse = NextInsideUnitCircle(ref randomState) * lateralJitter;
                    previousJitter = Vector2.Lerp(previousJitter, impulse, NextRange(ref randomState, 0.62f, 0.9f));
                    float sweep = Mathf.Sin((t * sweepCycles + sweepPhase) * Mathf.PI) * sweepStrength;
                    Vector2 offset = previousJitter + sweepDirection * sweep;
                    point += new Vector3(offset.x * envelope, 0f, offset.y * envelope);
                }

                entry.BoltPoints[i] = point;
            }

            entry.BoltWidth = actualWidth;
            int minBranches = actualWidth >= 0.5f ? 1 : 0;
            int maxBranches = actualWidth >= 0.5f ? MaximumBranchCount : 2;
            entry.ActiveBranchCount = NextRangeInt(ref randomState, minBranches, maxBranches + 1);
            BuildBoltBranches(entry, actualWidth, ref randomState);
            SetBoltWidths(entry, 1f);
        }

        private static void BuildBoltBranches(Entry entry, float boltWidth, ref uint randomState)
        {
            if (entry.BoltBranchPoints == null || entry.BoltBranchStartIndices == null)
            {
                return;
            }

            int activeCount = Mathf.Clamp(entry.ActiveBranchCount, 0, MaximumBranchCount);
            int previousStart = -10;

            for (int branchIndex = 0; branchIndex < MaximumBranchCount; branchIndex++)
            {
                if (branchIndex >= activeCount)
                {
                    entry.BoltBranchStartIndices[branchIndex] = 0;
                    continue;
                }

                int startIndex = NextRangeInt(ref randomState, 3, MainBoltPointCount - 3);
                if (Mathf.Abs(startIndex - previousStart) < 2)
                {
                    startIndex = Mathf.Clamp(startIndex + (branchIndex % 2 == 0 ? 2 : -2), 3, MainBoltPointCount - 4);
                }

                previousStart = startIndex;
                entry.BoltBranchStartIndices[branchIndex] = startIndex;
                Vector3[] points = entry.BoltBranchPoints[branchIndex];
                Vector3 start = entry.BoltPoints[startIndex];
                Vector2 direction2D = NextInsideUnitCircle(ref randomState);
                if (direction2D.sqrMagnitude < 0.001f)
                {
                    direction2D = branchIndex % 2 == 0 ? Vector2.left : Vector2.right;
                }

                direction2D.Normalize();
                float widthScale = Mathf.Clamp(boltWidth / 0.72f, 0.55f, 1.25f);
                float branchLength = NextRange(ref randomState, 3.2f, 6.1f) * widthScale;
                float verticalDrop = NextRange(ref randomState, 1.1f, 3.1f) * widthScale;
                Vector3 end = start + new Vector3(direction2D.x * branchLength, -verticalDrop, direction2D.y * branchLength);
                Vector2 branchBend = NextInsideUnitCircle(ref randomState) * boltWidth * NextRange(ref randomState, 0.35f, 0.95f);

                for (int pointIndex = 0; pointIndex < BranchPointCount; pointIndex++)
                {
                    float t = (float)pointIndex / (BranchPointCount - 1);
                    Vector3 point = Vector3.Lerp(start, end, t);
                    if (pointIndex > 0 && pointIndex < BranchPointCount - 1)
                    {
                        float envelope = Mathf.Sin(t * Mathf.PI);
                        Vector2 jitter = NextInsideUnitCircle(ref randomState) * boltWidth * NextRange(ref randomState, 0.45f, 0.9f);
                        Vector2 offset = jitter + branchBend * envelope;
                        point += new Vector3(offset.x, 0f, offset.y);
                    }

                    points[pointIndex] = point;
                }
            }
        }

        private void BeginImpactFeedback(Entry entry, float impactScale, float now)
        {
            if (entry == null || entry.Instance == null)
            {
                return;
            }

            entry.ImpactFeedbackStartTime = now;
            entry.ShockwaveRadius = Mathf.Max(0.8f, impactScale * ShockwaveRadiusMultiplier);
            entry.ShockwaveWidth = Mathf.Clamp(impactScale * 0.11f, 0.08f, 0.24f);
            entry.ImpactFeedbackActive = true;

            uint randomState = NextShapeSeed(entry.Instance.transform.position, entry.VariantIndex + 17);
            BuildGroundArcs(entry, entry.Instance.transform.position, impactScale, ref randomState);
            UpdateImpactFeedback(entry, now);
        }

        private static void UpdateImpactFeedback(Entry entry, float now)
        {
            if (entry == null || !entry.ImpactFeedbackActive || entry.Instance == null)
            {
                return;
            }

            float elapsed = Mathf.Max(0f, now - entry.ImpactFeedbackStartTime);
            float shockProgress = Mathf.Clamp01(elapsed / ShockwaveDuration);
            float shockEase = 1f - (1f - shockProgress) * (1f - shockProgress);

            if (entry.ShockwaveRing != null && entry.ShockwavePoints != null && shockProgress < 1f)
            {
                Vector3 center = entry.Instance.transform.position + Vector3.up * ShockwaveGroundOffset;
                float radius = Mathf.Lerp(0.18f, entry.ShockwaveRadius, shockEase);
                for (int i = 0; i < entry.ShockwavePoints.Length; i++)
                {
                    float angle = (float)i / entry.ShockwavePoints.Length * Mathf.PI * 2f;
                    entry.ShockwavePoints[i] = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                }

                entry.ShockwaveRing.enabled = true;
                entry.ShockwaveRing.positionCount = entry.ShockwavePoints.Length;
                entry.ShockwaveRing.SetPositions(entry.ShockwavePoints);
                entry.ShockwaveRing.widthMultiplier = Mathf.Max(0.01f, entry.ShockwaveWidth * (1f - shockProgress));
            }
            else
            {
                HideLine(entry.ShockwaveRing);
            }

            float arcProgress = Mathf.Clamp01(elapsed / GroundArcDuration);
            if (entry.GroundArcs != null)
            {
                for (int i = 0; i < entry.GroundArcs.Length; i++)
                {
                    LineRenderer arc = entry.GroundArcs[i];
                    Vector3[] points = entry.GroundArcPoints != null && i < entry.GroundArcPoints.Length ? entry.GroundArcPoints[i] : null;
                    if (arc == null || points == null || arcProgress >= 1f)
                    {
                        HideLine(arc);
                        continue;
                    }

                    float flicker = Mathf.Abs(Mathf.Sin((elapsed * 34f) + i * 1.73f));
                    bool visible = flicker > 0.24f || arcProgress < 0.16f;
                    if (!visible)
                    {
                        HideLine(arc);
                        continue;
                    }

                    arc.enabled = true;
                    arc.positionCount = points.Length;
                    arc.SetPositions(points);
                    arc.widthMultiplier = Mathf.Max(0.01f, entry.ShockwaveWidth * 0.65f * (1f - arcProgress) * (0.65f + flicker * 0.35f));
                }
            }

            if (shockProgress >= 1f && arcProgress >= 1f)
            {
                entry.ImpactFeedbackActive = false;
            }
        }

        private static void BuildGroundArcs(Entry entry, Vector3 center, float impactScale, ref uint randomState)
        {
            if (entry.GroundArcPoints == null)
            {
                return;
            }

            float scale = Mathf.Max(0.75f, impactScale);
            Vector3 groundCenter = center + Vector3.up * (ShockwaveGroundOffset + 0.015f);

            for (int arcIndex = 0; arcIndex < entry.GroundArcPoints.Length; arcIndex++)
            {
                Vector3[] points = entry.GroundArcPoints[arcIndex];
                if (points == null || points.Length < 2)
                {
                    continue;
                }

                Vector2 direction = NextInsideUnitCircle(ref randomState);
                if (direction.sqrMagnitude < 0.001f)
                {
                    direction = arcIndex % 2 == 0 ? Vector2.right : Vector2.left;
                }

                direction.Normalize();
                float length = NextRange(ref randomState, 1.15f, 2.35f) * scale;
                Vector2 lateral = new Vector2(-direction.y, direction.x);

                for (int pointIndex = 0; pointIndex < points.Length; pointIndex++)
                {
                    float t = (float)pointIndex / (points.Length - 1);
                    float distance = length * t;
                    float envelope = Mathf.Sin(t * Mathf.PI);
                    float side = NextRange(ref randomState, -0.34f, 0.34f) * scale * envelope;
                    Vector2 offset = direction * distance + lateral * side;
                    points[pointIndex] = groundCenter + new Vector3(offset.x, 0f, offset.y);
                }
            }
        }

        private static void HideImpactFeedback(Entry entry)
        {
            if (entry == null)
            {
                return;
            }

            HideLine(entry.ShockwaveRing);
            if (entry.GroundArcs != null)
            {
                for (int i = 0; i < entry.GroundArcs.Length; i++)
                {
                    HideLine(entry.GroundArcs[i]);
                }
            }

            entry.ImpactFeedbackStartTime = 0f;
            entry.ShockwaveRadius = 0f;
            entry.ShockwaveWidth = 0f;
            entry.ImpactFeedbackActive = false;
        }

        private uint NextShapeSeed(Vector3 position, int variantIndex)
        {
            boltShapeSequence = unchecked(boltShapeSequence + 0x9E3779B9u);
            uint seed = boltShapeSequence;
            seed ^= unchecked((uint)Mathf.RoundToInt(position.x * 100f) * 73856093u);
            seed ^= unchecked((uint)Mathf.RoundToInt(position.y * 100f) * 19349663u);
            seed ^= unchecked((uint)Mathf.RoundToInt(position.z * 100f) * 83492791u);
            seed ^= unchecked((uint)(variantIndex + 1) * 2654435761u);
            return Scramble(seed);
        }

        private static uint Scramble(uint value)
        {
            value ^= value << 13;
            value ^= value >> 17;
            value ^= value << 5;
            return value != 0u ? value : 0xA341316Cu;
        }

        private static float Next01(ref uint state)
        {
            state = Scramble(state);
            return (state & 0x00FFFFFFu) / 16777215f;
        }

        private static float NextRange(ref uint state, float min, float max)
        {
            return Mathf.Lerp(min, max, Next01(ref state));
        }

        private static int NextRangeInt(ref uint state, int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                return minInclusive;
            }

            int range = maxExclusive - minInclusive;
            return minInclusive + Mathf.Min(range - 1, Mathf.FloorToInt(Next01(ref state) * range));
        }

        private static Vector2 NextInsideUnitCircle(ref uint state)
        {
            float angle = Next01(ref state) * Mathf.PI * 2f;
            float radius = Mathf.Sqrt(Next01(ref state));
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }

        private static void SetBoltWidths(Entry entry, float multiplier)
        {
            if (entry == null)
            {
                return;
            }

            float width = Mathf.Max(0.05f, entry.BoltWidth) * Mathf.Max(0.1f, multiplier);
            if (entry.BoltGlow != null)
            {
                entry.BoltGlow.widthMultiplier = width;
            }

            if (entry.BoltCore != null)
            {
                entry.BoltCore.widthMultiplier = width * 0.3f;
            }

            if (entry.BoltBranches == null)
            {
                return;
            }

            for (int i = 0; i < entry.BoltBranches.Length; i++)
            {
                LineRenderer branch = entry.BoltBranches[i];
                if (branch != null)
                {
                    branch.widthMultiplier = width * 0.42f;
                }
            }
        }

        private static void ApplyPartialLine(LineRenderer line, Vector3[] points, float progress)
        {
            if (line == null || points == null || points.Length < 2 || progress <= 0f)
            {
                if (line != null)
                {
                    line.positionCount = 0;
                    line.enabled = false;
                }

                return;
            }

            float clamped = Mathf.Clamp01(progress);
            float scaledIndex = clamped * (points.Length - 1);
            int wholeIndex = Mathf.Min(Mathf.FloorToInt(scaledIndex), points.Length - 2);
            float fraction = scaledIndex - wholeIndex;
            int positionCount = Mathf.Clamp(wholeIndex + 2, 2, points.Length);

            line.enabled = true;
            line.positionCount = positionCount;

            for (int i = 0; i <= wholeIndex; i++)
            {
                line.SetPosition(i, points[i]);
            }

            Vector3 lastPoint = clamped >= 0.9999f ? points[points.Length - 1] : Vector3.Lerp(points[wholeIndex], points[wholeIndex + 1], fraction);
            line.SetPosition(positionCount - 1, lastPoint);
        }

        private static void HideBolt(Entry entry)
        {
            if (entry == null)
            {
                return;
            }

            HideLine(entry.BoltGlow);
            HideLine(entry.BoltCore);

            if (entry.BoltBranches != null)
            {
                for (int i = 0; i < entry.BoltBranches.Length; i++)
                {
                    HideLine(entry.BoltBranches[i]);
                }
            }

            SetBoltWidths(entry, 1f);
        }

        private static void HideLine(LineRenderer line)
        {
            if (line == null)
            {
                return;
            }

            line.positionCount = 0;
            line.enabled = false;
        }

        private static void PlayActiveParticles(ParticleSystem[] particles, float simulationSpeed)
        {
            if (particles == null)
            {
                return;
            }

            for (int i = 0; i < particles.Length; i++)
            {
                ParticleSystem particle = particles[i];
                if (particle == null || !particle.gameObject.activeInHierarchy)
                {
                    continue;
                }

                ParticleSystem.MainModule main = particle.main;
                main.simulationSpeed = Mathf.Max(0.01f, simulationSpeed);
                particle.Play(true);
            }
        }

        private static void StopParticles(ParticleSystem[] particles)
        {
            if (particles == null)
            {
                return;
            }

            for (int i = 0; i < particles.Length; i++)
            {
                ParticleSystem particle = particles[i];
                if (particle != null)
                {
                    particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
        }

        private int NormalizeVariantIndex(int variantIndex)
        {
            int count = variants.Length;
            if (count <= 1)
            {
                return 0;
            }

            return ((variantIndex % count) + count) % count;
        }

        private static bool IsImpactObject(string objectName)
        {
            return objectName == "Zap Add Floor" || objectName == "Scorch" || objectName == "Flare" || objectName == "Light Spawn";
        }

        private static bool IsRevealObject(string objectName)
        {
            return objectName == "Zap BG" || objectName == "Zap LUT" || objectName == "Zap" || objectName == "Zap Add";
        }
    }
}
