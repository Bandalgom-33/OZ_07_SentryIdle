using UnityEngine;

namespace EndlessGuard.Unit.Raid.Data
{
    [CreateAssetMenu(fileName = "RaidAudioProfile", menuName = "EndlessGuard/Raid/Audio Profile")]
    public sealed class RaidAudioProfileSO : ScriptableObject
    {
        [Header("레이드 시작")]
        [SerializeField] private AudioClip startRumble;
        [Range(0f, 1f)] [SerializeField] private float startVolume = 0.22f;

        [Header("레이드 BGM")]
        [SerializeField] private AudioClip phase1Music;
        [SerializeField] private AudioClip phase2Music;
        [SerializeField] private AudioClip phase3Music;
        [Range(0f, 1f)] [SerializeField] private float phase1MusicVolume = 0.42f;
        [Range(0f, 1f)] [SerializeField] private float phase2MusicVolume = 0.44f;
        [Range(0f, 1f)] [SerializeField] private float phase3MusicVolume = 0.46f;
        [Min(0.05f)] [SerializeField] private float musicFadeDuration = 0.72f;

        [Header("Phase 상승 음악")]
        [SerializeField] private AudioClip phase2Surge;
        [SerializeField] private AudioClip phase3Surge;
        [Range(0f, 1f)] [SerializeField] private float phase2SurgeVolume = 0.52f;
        [Range(0f, 1f)] [SerializeField] private float phase3SurgeVolume = 0.64f;

        [Header("Phase 붕괴")]
        [SerializeField] private AudioClip collapseImpact;
        [Range(0f, 1f)] [SerializeField] private float collapseImpactVolume = 0.55f;
        [SerializeField] private AudioClip collapseRumble;
        [Range(0f, 1f)] [SerializeField] private float collapseRumbleVolume = 0.32f;
        [SerializeField] private AudioClip[] collapseWhooshClips;
        [Range(0f, 1f)] [SerializeField] private float collapseWhooshVolume = 0.24f;
        [SerializeField] private AudioClip[] collapseBreakClips;
        [Range(0f, 1f)] [SerializeField] private float collapseBreakVolume = 0.22f;
        [SerializeField] private AudioClip collapseFinalDrop;
        [Range(0f, 1f)] [SerializeField] private float collapseFinalDropVolume = 0.40f;
        [Range(0.1f, 1f)] [SerializeField] private float collapseBackgroundDuck = 0.42f;

        public AudioClip StartRumble => startRumble;
        public float StartVolume => startVolume;
        public AudioClip Phase1Music => phase1Music;
        public AudioClip Phase2Music => phase2Music;
        public AudioClip Phase3Music => phase3Music;
        public float Phase1MusicVolume => phase1MusicVolume;
        public float Phase2MusicVolume => phase2MusicVolume;
        public float Phase3MusicVolume => phase3MusicVolume;
        public float MusicFadeDuration => musicFadeDuration;
        public AudioClip Phase2Surge => phase2Surge;
        public AudioClip Phase3Surge => phase3Surge;
        public float Phase2SurgeVolume => phase2SurgeVolume;
        public float Phase3SurgeVolume => phase3SurgeVolume;
        public AudioClip CollapseImpact => collapseImpact;
        public float CollapseImpactVolume => collapseImpactVolume;
        public AudioClip CollapseRumble => collapseRumble;
        public float CollapseRumbleVolume => collapseRumbleVolume;
        public AudioClip[] CollapseWhooshClips => collapseWhooshClips;
        public float CollapseWhooshVolume => collapseWhooshVolume;
        public AudioClip[] CollapseBreakClips => collapseBreakClips;
        public float CollapseBreakVolume => collapseBreakVolume;
        public AudioClip CollapseFinalDrop => collapseFinalDrop;
        public float CollapseFinalDropVolume => collapseFinalDropVolume;
        public float CollapseBackgroundDuck => collapseBackgroundDuck;

        public AudioClip GetMusic(RaidPhase phase)
        {
            switch (phase)
            {
                case RaidPhase.Phase2:
                    return phase2Music;
                case RaidPhase.Phase3:
                    return phase3Music;
                default:
                    return phase1Music;
            }
        }

        public float GetMusicVolume(RaidPhase phase)
        {
            switch (phase)
            {
                case RaidPhase.Phase2:
                    return phase2MusicVolume;
                case RaidPhase.Phase3:
                    return phase3MusicVolume;
                default:
                    return phase1MusicVolume;
            }
        }

        public AudioClip GetPhaseSurge(RaidPhase phase)
        {
            switch (phase)
            {
                case RaidPhase.Phase3:
                    return phase3Surge;
                case RaidPhase.Phase2:
                    return phase2Surge;
                default:
                    return null;
            }
        }

        public float GetPhaseSurgeVolume(RaidPhase phase)
        {
            switch (phase)
            {
                case RaidPhase.Phase3:
                    return phase3SurgeVolume;
                case RaidPhase.Phase2:
                    return phase2SurgeVolume;
                default:
                    return 0f;
            }
        }

        private void OnValidate()
        {
            startVolume = Mathf.Clamp01(startVolume);
            phase1MusicVolume = Mathf.Clamp01(phase1MusicVolume);
            phase2MusicVolume = Mathf.Clamp01(phase2MusicVolume);
            phase3MusicVolume = Mathf.Clamp01(phase3MusicVolume);
            musicFadeDuration = Mathf.Max(0.05f, musicFadeDuration);
            phase2SurgeVolume = Mathf.Clamp01(phase2SurgeVolume);
            phase3SurgeVolume = Mathf.Clamp01(phase3SurgeVolume);
            collapseImpactVolume = Mathf.Clamp01(collapseImpactVolume);
            collapseRumbleVolume = Mathf.Clamp01(collapseRumbleVolume);
            collapseWhooshVolume = Mathf.Clamp01(collapseWhooshVolume);
            collapseBreakVolume = Mathf.Clamp01(collapseBreakVolume);
            collapseFinalDropVolume = Mathf.Clamp01(collapseFinalDropVolume);
            collapseBackgroundDuck = Mathf.Clamp(collapseBackgroundDuck, 0.1f, 1f);
        }
    }
}
