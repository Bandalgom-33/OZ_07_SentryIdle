using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    public sealed class AttackHitSoundPool : MonoBehaviour
    {
        private const int VoiceCount = 16;

        private sealed class Voice
        {
            public AudioSource Source;
            public float StartedAt;
        }

        private static AttackHitSoundPool instance;
        private readonly Voice[] voices = new Voice[VoiceCount];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
        }

        public static bool ShowHit(AttackHitSoundTemplate template, CombatEntityAnchors targetAnchors, Transform fallback)
        {
            if (template == null)
            {
                return false;
            }

            AudioClip clip = template.GetNextClip();
            if (clip == null)
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

            AttackHitSoundPool pool = GetOrCreateInstance();
            return pool != null && pool.Play(template, clip, position);
        }

        private static AttackHitSoundPool GetOrCreateInstance()
        {
            if (instance != null)
            {
                return instance;
            }

            GameObject root = new GameObject("AttackHitSoundPool");
            DontDestroyOnLoad(root);
            instance = root.AddComponent<AttackHitSoundPool>();
            instance.CreateVoices();
            return instance;
        }

        private void CreateVoices()
        {
            for (int i = 0; i < voices.Length; i++)
            {
                GameObject voiceObject = new GameObject($"Voice_{i + 1:00}");
                voiceObject.transform.SetParent(transform, false);
                AudioSource source = voiceObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.dopplerLevel = 0f;
                source.reverbZoneMix = 0f;
                voices[i] = new Voice { Source = source, StartedAt = 0f };
            }
        }

        private bool Play(AttackHitSoundTemplate template, AudioClip clip, Vector3 position)
        {
            Voice voice = GetVoice();
            if (voice == null || voice.Source == null)
            {
                return false;
            }

            AudioSource source = voice.Source;
            source.Stop();
            source.transform.position = position;
            source.clip = clip;
            source.volume = template.Volume;
            source.pitch = template.GetPitch();
            source.spatialBlend = template.SpatialBlend;
            source.minDistance = template.MinDistance;
            source.maxDistance = template.MaxDistance;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.Play();
            voice.StartedAt = Time.unscaledTime;
            return true;
        }

        private Voice GetVoice()
        {
            Voice oldest = null;
            for (int i = 0; i < voices.Length; i++)
            {
                Voice voice = voices[i];
                if (voice == null || voice.Source == null)
                {
                    continue;
                }

                if (!voice.Source.isPlaying)
                {
                    return voice;
                }

                if (oldest == null || voice.StartedAt < oldest.StartedAt)
                {
                    oldest = voice;
                }
            }

            return oldest;
        }
    }
}
