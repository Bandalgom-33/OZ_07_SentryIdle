using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    [DisallowMultipleComponent]
    public sealed class RaidTimedVFX : MonoBehaviour
    {
        private static readonly int StartTimeId = Shader.PropertyToID("_VFXStartTime");
        private static readonly int DurationId = Shader.PropertyToID("_VFXDuration");

        private Renderer[] renderers;
        private ParticleSystem[] particles;
        private MaterialPropertyBlock propertyBlock;

        private void Awake()
        {
            CacheComponents();
        }

        public void Play(float durationSeconds)
        {
            CacheComponents();

            float duration = Mathf.Max(0.05f, durationSeconds);
            float startTime = Time.time;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer target = renderers[i];
                if (target == null)
                {
                    continue;
                }

                target.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat(StartTimeId, startTime);
                propertyBlock.SetFloat(DurationId, duration);
                target.SetPropertyBlock(propertyBlock);
                propertyBlock.Clear();
            }

            for (int i = 0; i < particles.Length; i++)
            {
                ParticleSystem particle = particles[i];
                if (particle == null)
                {
                    continue;
                }

                particle.Clear(true);
                particle.Play(true);
            }

            Destroy(gameObject, duration + 0.05f);
        }

        private void CacheComponents()
        {
            if (renderers == null)
            {
                renderers = GetComponentsInChildren<Renderer>(true);
            }

            if (particles == null)
            {
                particles = GetComponentsInChildren<ParticleSystem>(true);
            }

            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }
        }
    }
}
