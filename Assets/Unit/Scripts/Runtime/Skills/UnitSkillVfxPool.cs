using System.Collections.Generic;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    /// <summary>
    /// UnitDataSO의 스킬 VFX Prefab을 공용 풀로 재사용합니다.
    /// 캐릭터 데이터에서 Prefab 참조만 교체하면 런타임 코드는 변경할 필요가 없습니다.
    /// </summary>
    public sealed class UnitSkillVfxPool : MonoBehaviour
    {
        private const int InitialCapacityPerPrefab = 4;
        private const int MaximumCapacityPerPrefab = 64;
        private const float MaximumSafetyLifetime = 6f;

        private sealed class EffectEntry
        {
            public GameObject Instance;
            public ParticleSystem[] Particles;
            public Vector3 BaseScale;
            public bool IsActive;
            public float StartedAt;
            public float ExpireAt;
        }

        private sealed class EffectPool
        {
            public GameObject Prototype;
            public readonly List<EffectEntry> Entries = new List<EffectEntry>(InitialCapacityPerPrefab);
        }

        private static UnitSkillVfxPool instance;
        private readonly Dictionary<GameObject, EffectPool> pools = new Dictionary<GameObject, EffectPool>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
        }

        public static bool Play(GameObject prefab, Vector3 worldPosition, float scaleMultiplier)
        {
            if (prefab == null)
            {
                return false;
            }

            UnitSkillVfxPool pool = GetOrCreateInstance();
            return pool != null && pool.PlayInternal(prefab, worldPosition, scaleMultiplier);
        }

        private static UnitSkillVfxPool GetOrCreateInstance()
        {
            if (instance != null)
            {
                return instance;
            }

            GameObject root = new GameObject("UnitSkillVfxPool");
            DontDestroyOnLoad(root);
            instance = root.AddComponent<UnitSkillVfxPool>();
            return instance;
        }

        private void Update()
        {
            float now = Time.unscaledTime;

            foreach (KeyValuePair<GameObject, EffectPool> pair in pools)
            {
                List<EffectEntry> entries = pair.Value.Entries;
                for (int i = 0; i < entries.Count; i++)
                {
                    EffectEntry entry = entries[i];
                    if (entry == null || !entry.IsActive)
                    {
                        continue;
                    }

                    if (now >= entry.ExpireAt || !IsAlive(entry))
                    {
                        Deactivate(entry);
                    }
                }
            }
        }

        private bool PlayInternal(GameObject prefab, Vector3 worldPosition, float scaleMultiplier)
        {
            if (!pools.TryGetValue(prefab, out EffectPool effectPool))
            {
                effectPool = CreatePool(prefab);
                if (effectPool == null)
                {
                    return false;
                }

                pools.Add(prefab, effectPool);
            }

            EffectEntry entry = GetAvailableEntry(effectPool);
            if (entry == null || entry.Instance == null)
            {
                return false;
            }

            StopAndClear(entry);

            Transform effectTransform = entry.Instance.transform;
            effectTransform.SetPositionAndRotation(worldPosition, Quaternion.identity);
            effectTransform.localScale = entry.BaseScale * Mathf.Max(0.01f, scaleMultiplier);
            entry.Instance.SetActive(true);

            float calculatedLifetime = 0.25f;
            for (int i = 0; i < entry.Particles.Length; i++)
            {
                ParticleSystem particle = entry.Particles[i];
                if (particle == null)
                {
                    continue;
                }

                ParticleSystem.MainModule main = particle.main;
                main.loop = false;
                main.playOnAwake = false;
                main.stopAction = ParticleSystemStopAction.None;
                calculatedLifetime = Mathf.Max(calculatedLifetime, GetMaximumLifetime(particle));
                particle.Play(false);
            }

            float now = Time.unscaledTime;
            entry.StartedAt = now;
            entry.ExpireAt = now + Mathf.Min(MaximumSafetyLifetime, Mathf.Max(0.25f, calculatedLifetime + 0.1f));
            entry.IsActive = true;
            return true;
        }

        private EffectPool CreatePool(GameObject sourcePrefab)
        {
            if (sourcePrefab == null)
            {
                return null;
            }

            EffectPool effectPool = new EffectPool();
            effectPool.Prototype = Instantiate(sourcePrefab, transform);
            effectPool.Prototype.name = $"Prototype_{sourcePrefab.name}";
            Prepare(effectPool.Prototype);
            effectPool.Prototype.SetActive(false);

            for (int i = 0; i < InitialCapacityPerPrefab; i++)
            {
                effectPool.Entries.Add(CreateEntry(effectPool, i));
            }

            return effectPool;
        }

        private EffectEntry GetAvailableEntry(EffectPool effectPool)
        {
            for (int i = 0; i < effectPool.Entries.Count; i++)
            {
                EffectEntry entry = effectPool.Entries[i];
                if (entry != null && !entry.IsActive)
                {
                    return entry;
                }
            }

            if (effectPool.Entries.Count < MaximumCapacityPerPrefab)
            {
                EffectEntry created = CreateEntry(effectPool, effectPool.Entries.Count);
                effectPool.Entries.Add(created);
                return created;
            }

            EffectEntry oldest = null;
            for (int i = 0; i < effectPool.Entries.Count; i++)
            {
                EffectEntry entry = effectPool.Entries[i];
                if (entry != null && (oldest == null || entry.StartedAt < oldest.StartedAt))
                {
                    oldest = entry;
                }
            }

            if (oldest != null)
            {
                Deactivate(oldest);
            }

            return oldest;
        }

        private EffectEntry CreateEntry(EffectPool effectPool, int index)
        {
            GameObject effect = Instantiate(effectPool.Prototype, transform);
            effect.name = $"SkillVfx_{index + 1:00}";
            ParticleSystem[] particles = Prepare(effect);
            Vector3 baseScale = effect.transform.localScale;
            effect.SetActive(false);

            return new EffectEntry
            {
                Instance = effect,
                Particles = particles,
                BaseScale = baseScale,
                IsActive = false,
                StartedAt = 0f,
                ExpireAt = 0f
            };
        }

        private static ParticleSystem[] Prepare(GameObject effect)
        {
            ParticleSystem[] particles = effect.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particles.Length; i++)
            {
                ParticleSystem particle = particles[i];
                if (particle == null)
                {
                    continue;
                }

                ParticleSystem.MainModule main = particle.main;
                main.loop = false;
                main.playOnAwake = false;
                main.stopAction = ParticleSystemStopAction.None;
                particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            return particles;
        }

        private static float GetMaximumLifetime(ParticleSystem particle)
        {
            ParticleSystem.MainModule main = particle.main;
            return GetCurveMaximum(main.startDelay) + main.duration + GetCurveMaximum(main.startLifetime);
        }

        private static float GetCurveMaximum(ParticleSystem.MinMaxCurve curve)
        {
            switch (curve.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    return curve.constant;
                case ParticleSystemCurveMode.TwoConstants:
                    return curve.constantMax;
                case ParticleSystemCurveMode.Curve:
                case ParticleSystemCurveMode.TwoCurves:
                    return curve.curveMultiplier;
                default:
                    return 0f;
            }
        }

        private static bool IsAlive(EffectEntry entry)
        {
            if (entry.Particles == null || entry.Particles.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < entry.Particles.Length; i++)
            {
                ParticleSystem particle = entry.Particles[i];
                if (particle != null && particle.IsAlive(false))
                {
                    return true;
                }
            }

            return false;
        }

        private static void StopAndClear(EffectEntry entry)
        {
            if (entry == null || entry.Particles == null)
            {
                return;
            }

            for (int i = 0; i < entry.Particles.Length; i++)
            {
                ParticleSystem particle = entry.Particles[i];
                if (particle != null)
                {
                    particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
        }

        private static void Deactivate(EffectEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            StopAndClear(entry);
            entry.IsActive = false;
            entry.StartedAt = 0f;
            entry.ExpireAt = 0f;

            if (entry.Instance != null)
            {
                entry.Instance.SetActive(false);
            }
        }
    }
}
