using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    [DisallowMultipleComponent]
    public sealed class AttackHitSoundTemplate : MonoBehaviour
    {
        [SerializeField] private AudioClip[] clips;
        [SerializeField, Range(0f, 1f)] private float volume = 0.35f;
        [SerializeField] private float pitchMin = 0.96f;
        [SerializeField] private float pitchMax = 1.04f;
        [SerializeField, Range(0f, 1f)] private float spatialBlend = 0.15f;
        [SerializeField, Min(0.01f)] private float minDistance = 4f;
        [SerializeField, Min(0.01f)] private float maxDistance = 28f;

        private int lastClipIndex = -1;

        public float Volume => Mathf.Clamp01(volume);
        public float SpatialBlend => Mathf.Clamp01(spatialBlend);
        public float MinDistance => Mathf.Max(0.01f, minDistance);
        public float MaxDistance => Mathf.Max(MinDistance, maxDistance);

        public AudioClip GetNextClip()
        {
            if (clips == null || clips.Length == 0)
            {
                return null;
            }

            if (clips.Length == 1)
            {
                lastClipIndex = clips[0] != null ? 0 : -1;
                return clips[0];
            }

            int startIndex = Random.Range(0, clips.Length);
            for (int offset = 0; offset < clips.Length; offset++)
            {
                int index = (startIndex + offset) % clips.Length;
                if (index == lastClipIndex || clips[index] == null)
                {
                    continue;
                }

                lastClipIndex = index;
                return clips[index];
            }

            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] == null)
                {
                    continue;
                }

                lastClipIndex = i;
                return clips[i];
            }

            lastClipIndex = -1;
            return null;
        }

        public float GetPitch()
        {
            float min = Mathf.Clamp(Mathf.Min(pitchMin, pitchMax), -3f, 3f);
            float max = Mathf.Clamp(Mathf.Max(pitchMin, pitchMax), -3f, 3f);
            return Mathf.Approximately(min, max) ? min : Random.Range(min, max);
        }

        private void OnValidate()
        {
            volume = Mathf.Clamp01(volume);
            spatialBlend = Mathf.Clamp01(spatialBlend);
            pitchMin = Mathf.Clamp(pitchMin, -3f, 3f);
            pitchMax = Mathf.Clamp(pitchMax, -3f, 3f);
            minDistance = Mathf.Max(0.01f, minDistance);
            maxDistance = Mathf.Max(minDistance, maxDistance);
        }
    }
}
