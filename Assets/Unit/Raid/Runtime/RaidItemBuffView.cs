using EndlessGuard.Unit.Raid.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    [DisallowMultipleComponent]
    public sealed class RaidItemBuffView : MonoBehaviour
    {
        private static readonly int StartTimeId = Shader.PropertyToID("_BuffStartTime");
        private static readonly int EndTimeId = Shader.PropertyToID("_BuffEndTime");
        private static readonly int StackNormalizedId = Shader.PropertyToID("_StackNormalized");
        private static readonly int StackCountId = Shader.PropertyToID("_StackCount");

        private readonly VisualSlot attack = new VisualSlot();
        private readonly VisualSlot attackSpeed = new VisualSlot();
        private readonly VisualSlot heal = new VisualSlot();
        private MaterialPropertyBlock propertyBlock;

        private void OnDisable()
        {
            Clear();
        }

        public void Show(RaidItemType type, GameObject prefab, float remainingSeconds, int stack, int maxStack)
        {
            if (prefab == null)
            {
                Hide(type);
                return;
            }

            VisualSlot slot = GetSlot(type);
            if (slot == null)
            {
                return;
            }

            EnsureVisual(slot, prefab);
            if (slot.Instance == null)
            {
                return;
            }

            bool wasActive = slot.Instance.activeSelf;
            slot.StartTime = Time.time;
            if (!wasActive)
            {
                slot.Instance.SetActive(true);
                PlayParticles(slot);
            }

            float endTime = float.IsPositiveInfinity(remainingSeconds) ? 0f : Time.time + Mathf.Max(0.05f, remainingSeconds);
            ApplyState(slot, slot.StartTime, endTime, stack, maxStack);
        }

        public void Hide(RaidItemType type)
        {
            VisualSlot slot = GetSlot(type);
            if (slot == null || slot.Instance == null)
            {
                return;
            }

            StopParticles(slot);
            slot.Instance.SetActive(false);
            slot.StartTime = 0f;
        }

        public void Clear()
        {
            Hide(RaidItemType.Attack);
            Hide(RaidItemType.AttackSpeed);
            Hide(RaidItemType.Heal);
        }

        private VisualSlot GetSlot(RaidItemType type)
        {
            switch (type)
            {
                case RaidItemType.Attack:
                    return attack;
                case RaidItemType.AttackSpeed:
                    return attackSpeed;
                case RaidItemType.Heal:
                    return heal;
                default:
                    return null;
            }
        }

        private void EnsureVisual(VisualSlot slot, GameObject prefab)
        {
            if (slot.Instance != null && slot.Prefab == prefab)
            {
                return;
            }

            if (slot.Instance != null)
            {
                Destroy(slot.Instance);
            }

            slot.Prefab = prefab;
            slot.Instance = Instantiate(prefab, transform, false);
            slot.Instance.name = prefab.name;
            slot.Renderers = slot.Instance.GetComponentsInChildren<Renderer>(true);
            slot.Particles = slot.Instance.GetComponentsInChildren<ParticleSystem>(true);
            slot.Instance.SetActive(false);
            slot.StartTime = 0f;
        }

        private void ApplyState(VisualSlot slot, float startTime, float endTime, int stack, int maxStack)
        {
            if (slot.Renderers == null || slot.Renderers.Length == 0)
            {
                return;
            }

            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }

            for (int i = 0; i < slot.Renderers.Length; i++)
            {
                Renderer target = slot.Renderers[i];
                if (target == null)
                {
                    continue;
                }

                target.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat(StartTimeId, startTime);
                propertyBlock.SetFloat(EndTimeId, endTime);
                propertyBlock.SetFloat(StackNormalizedId, maxStack > 1 ? Mathf.Clamp01((stack - 1f) / (maxStack - 1f)) : 1f);
                propertyBlock.SetFloat(StackCountId, Mathf.Max(1, stack));
                target.SetPropertyBlock(propertyBlock);
                propertyBlock.Clear();
            }
        }

        private static void PlayParticles(VisualSlot slot)
        {
            if (slot.Particles == null)
            {
                return;
            }

            for (int i = 0; i < slot.Particles.Length; i++)
            {
                ParticleSystem particle = slot.Particles[i];
                if (particle == null)
                {
                    continue;
                }

                particle.Clear(true);
                particle.Play(true);
            }
        }

        private static void StopParticles(VisualSlot slot)
        {
            if (slot.Particles == null)
            {
                return;
            }

            for (int i = 0; i < slot.Particles.Length; i++)
            {
                ParticleSystem particle = slot.Particles[i];
                if (particle != null)
                {
                    particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
        }

        private sealed class VisualSlot
        {
            public GameObject Prefab;
            public GameObject Instance;
            public Renderer[] Renderers;
            public ParticleSystem[] Particles;
            public float StartTime;
        }
    }
}
