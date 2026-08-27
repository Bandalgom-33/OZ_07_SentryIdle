using System.Collections.Generic;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    /// <summary>
    /// 공격자 Prefab의 AttackPoint > AttackImpact 템플릿을 기준으로 적 EffectPoint에 1회성 적중 VFX를 재생합니다.
    /// impactId별로 풀을 공유하므로 같은 VFX를 사용하는 여러 캐릭터가 불필요하게 별도 풀을 만들지 않습니다.
    /// </summary>
    public sealed class AttackImpactVfxPool : MonoBehaviour
    {
        private const int InitialCapacityPerImpact = 6;
        private const int MaximumCapacityPerImpact = 32;
        private const float SafetyLifetime = 1.6f;

        private sealed class EffectEntry
        {
            public GameObject Instance;
            public ParticleSystem[] Particles;
            public bool IsActive;
            public float StartedAt;
            public float ExpireAt;
        }

        private sealed class ImpactPool
        {
            public GameObject Prototype;
            public readonly List<EffectEntry> Entries = new List<EffectEntry>(InitialCapacityPerImpact);
        }

        private static AttackImpactVfxPool instance;
        private readonly Dictionary<string, ImpactPool> pools = new Dictionary<string, ImpactPool>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
        }

        public static bool ShowHit(AttackImpactVfxTemplate template, CombatEntityAnchors targetAnchors, Transform fallback)
        {
            if (template == null)
            {
                return false;
            }

            Vector3 position;
            if (targetAnchors != null && targetAnchors.EffectPoint != null)
            {
                position = targetAnchors.EffectPoint.position;
            }
            else if (fallback != null)
            {
                position = fallback.position;
            }
            else
            {
                return false;
            }

            AttackImpactVfxPool pool = GetOrCreateInstance();
            return pool != null && pool.Play(template, position);
        }

        private static AttackImpactVfxPool GetOrCreateInstance()
        {
            if (instance != null)
            {
                return instance;
            }

            GameObject root = new GameObject("AttackImpactVfxPool");
            DontDestroyOnLoad(root);
            instance = root.AddComponent<AttackImpactVfxPool>();
            return instance;
        }

        private void Update()
        {
            float now = Time.unscaledTime;
            foreach (KeyValuePair<string, ImpactPool> pair in pools)
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

        private bool Play(AttackImpactVfxTemplate template, Vector3 worldPosition)
        {
            string key = template.ImpactId;
            if (!pools.TryGetValue(key, out ImpactPool impactPool))
            {
                impactPool = CreateImpactPool(template);
                if (impactPool == null)
                {
                    return false;
                }
                pools.Add(key, impactPool);
            }

            EffectEntry entry = GetAvailableEntry(impactPool);
            if (entry == null || entry.Instance == null)
            {
                return false;
            }

            StopAndClear(entry);
            Transform effectTransform = entry.Instance.transform;
            effectTransform.SetPositionAndRotation(worldPosition, Quaternion.identity);
            entry.Instance.SetActive(true);

            float calculatedLifetime = 0.1f;
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
            entry.ExpireAt = now + Mathf.Min(SafetyLifetime, Mathf.Max(0.2f, calculatedLifetime + 0.08f));
            entry.IsActive = true;
            return true;
        }

        private ImpactPool CreateImpactPool(AttackImpactVfxTemplate sourceTemplate)
        {
            if (sourceTemplate == null)
            {
                return null;
            }

            ImpactPool impactPool = new ImpactPool();
            impactPool.Prototype = Instantiate(sourceTemplate.gameObject, transform);
            impactPool.Prototype.name = $"Prototype_{sourceTemplate.ImpactId}";
            impactPool.Prototype.transform.localPosition = Vector3.zero;
            impactPool.Prototype.transform.localRotation = Quaternion.identity;
            impactPool.Prototype.transform.localScale = sourceTemplate.transform.localScale;
            PrepareParticles(impactPool.Prototype);
            impactPool.Prototype.SetActive(false);

            for (int i = 0; i < InitialCapacityPerImpact; i++)
            {
                impactPool.Entries.Add(CreateEntry(impactPool, i));
            }

            return impactPool;
        }

        private EffectEntry GetAvailableEntry(ImpactPool impactPool)
        {
            for (int i = 0; i < impactPool.Entries.Count; i++)
            {
                EffectEntry entry = impactPool.Entries[i];
                if (entry != null && !entry.IsActive)
                {
                    return entry;
                }
            }

            if (impactPool.Entries.Count < MaximumCapacityPerImpact)
            {
                EffectEntry created = CreateEntry(impactPool, impactPool.Entries.Count);
                impactPool.Entries.Add(created);
                return created;
            }

            EffectEntry oldest = null;
            for (int i = 0; i < impactPool.Entries.Count; i++)
            {
                EffectEntry entry = impactPool.Entries[i];
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

        private EffectEntry CreateEntry(ImpactPool impactPool, int index)
        {
            GameObject effect = Instantiate(impactPool.Prototype, transform);
            effect.name = $"Impact_{index + 1:00}";
            ParticleSystem[] particles = PrepareParticles(effect);
            effect.SetActive(false);
            return new EffectEntry
            {
                Instance = effect,
                Particles = particles,
                IsActive = false,
                StartedAt = 0f,
                ExpireAt = 0f
            };
        }

        private static ParticleSystem[] PrepareParticles(GameObject effect)
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
