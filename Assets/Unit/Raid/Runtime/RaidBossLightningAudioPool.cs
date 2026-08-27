using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    internal sealed class RaidBossLightningAudioPool
    {
        private const int VoiceCount = 24;
        private const float FirstStrikeVolume = 0.82f;
        private const float MiddleStrikeVolume = 0.58f;
        private const float MapStrikeVolume = 0.36f;
        private const float FinalStrikeVolume = 0.9f;

        private const string FirstStrikePath = "Vfx/BossLightning/Audio/SFX_Vefects_Zap_Big_02";
        private const string FinalStrikePath = "Vfx/BossLightning/Audio/SFX_Vefects_Zap_Big_01";

        private static readonly string[] MiddleStrikePaths =
        {
            "Vfx/BossLightning/Audio/SFX_Vefects_Zap_Medium_02",
            "Vfx/BossLightning/Audio/SFX_Vefects_Zap_Medium_01",
            "Vfx/BossLightning/Audio/SFX_Vefects_Zap_Small_01"
        };

        private readonly AudioSource[] voices = new AudioSource[VoiceCount];
        private readonly AudioClip[] middleClips = new AudioClip[MiddleStrikePaths.Length];
        private AudioClip firstClip;
        private AudioClip finalClip;
        private int voiceCursor;
        private int middleClipCursor;

        public RaidBossLightningAudioPool(Transform root)
        {
            firstClip = LoadClip(FirstStrikePath);
            finalClip = LoadClip(FinalStrikePath);

            for (int i = 0; i < MiddleStrikePaths.Length; i++)
            {
                middleClips[i] = LoadClip(MiddleStrikePaths[i]);
            }

            for (int i = 0; i < voices.Length; i++)
            {
                GameObject voiceObject = new GameObject($"LightningAudio_{i + 1:00}");
                voiceObject.transform.SetParent(root, false);
                AudioSource source = voiceObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.dopplerLevel = 0f;
                source.spatialBlend = 0f;
                source.volume = 1f;
                voices[i] = source;
            }
        }

        public void PlayTarget(Vector3 position, int strikeIndex, int strikeCount)
        {
            if (strikeIndex == 0 && firstClip != null)
            {
                Play(position, firstClip, FirstStrikeVolume);
                return;
            }

            if (strikeIndex == strikeCount - 1 && finalClip != null)
            {
                Play(position, finalClip, FinalStrikeVolume);
                return;
            }

            Play(position, GetMiddleClip(), MiddleStrikeVolume);
        }

        public void PlayMap(Vector3 position, bool first, bool final)
        {
            AudioClip clip = first && firstClip != null ? firstClip : final && finalClip != null ? finalClip : GetMiddleClip();
            float volume = first ? MapStrikeVolume * 1.15f : final ? MapStrikeVolume * 1.25f : MapStrikeVolume;
            Play(position, clip, volume);
        }

        public void StopAll()
        {
            for (int i = 0; i < voices.Length; i++)
            {
                if (voices[i] != null)
                {
                    voices[i].Stop();
                }
            }
        }

        private void Play(Vector3 position, AudioClip clip, float volume)
        {
            if (clip == null || volume <= 0f)
            {
                return;
            }

            AudioSource voice = GetVoice();
            if (voice == null)
            {
                return;
            }

            voice.transform.position = position;
            voice.PlayOneShot(clip, volume);
        }

        private AudioSource GetVoice()
        {
            for (int i = 0; i < voices.Length; i++)
            {
                int index = (voiceCursor + i) % voices.Length;
                AudioSource voice = voices[index];
                if (voice != null && !voice.isPlaying)
                {
                    voiceCursor = (index + 1) % voices.Length;
                    return voice;
                }
            }

            AudioSource fallback = voices[voiceCursor % voices.Length];
            voiceCursor = (voiceCursor + 1) % voices.Length;
            if (fallback != null)
            {
                fallback.Stop();
            }

            return fallback;
        }

        private AudioClip GetMiddleClip()
        {
            for (int attempt = 0; attempt < middleClips.Length; attempt++)
            {
                int index = middleClipCursor++ % middleClips.Length;
                AudioClip clip = middleClips[index];
                if (clip != null)
                {
                    return clip;
                }
            }

            return null;
        }

        private static AudioClip LoadClip(string resourcePath)
        {
            AudioClip clip = Resources.Load<AudioClip>(resourcePath);
            if (clip == null)
            {
                Debug.LogError($"Raid boss lightning audio is missing from Resources: {resourcePath}");
                return null;
            }

            if (clip.loadState == AudioDataLoadState.Unloaded)
            {
                clip.LoadAudioData();
            }

            return clip;
        }
    }
}
