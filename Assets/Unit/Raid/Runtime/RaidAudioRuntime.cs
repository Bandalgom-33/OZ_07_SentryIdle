using System.Collections;
using EndlessGuard.Unit.Raid.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RaidBattleController))]
    public sealed class RaidAudioRuntime : MonoBehaviour
    {
        private const string DefaultProfileResourcePath = "Audio/RaidAudioProfile";
        private const float VolumeEpsilon = 0.001f;
        private const float ReferenceCollapseDuration = 3.2f;

        [Header("레이드 오디오")]
        [Tooltip("비어 있으면 Unit/Raid/Resources/Audio/RaidAudioProfile을 사용합니다.")]
        [SerializeField] private RaidAudioProfileSO profile;

        private RaidBattleController battle;
        private Transform audioRoot;
        private AudioSource startSource;
        private AudioSource musicSourceA;
        private AudioSource musicSourceB;
        private AudioSource phaseSurgeSource;
        private AudioSource activeMusicSource;
        private AudioSource standbyMusicSource;
        private AudioSource collapseImpactSource;
        private AudioSource collapseRumbleSource;
        private AudioSource collapseDetailSource;
        private AudioSource collapseFinalSource;
        private Coroutine collapseRoutine;
        private float musicATarget;
        private float musicBTarget;
        private float backgroundDuck = 1f;
        private int lastCollapseWhooshIndex = -1;
        private int lastCollapseBreakIndex = -1;
        private bool audioActive;
        private bool fadeOutRequested;
        private bool collapseActive;

        public RaidAudioProfileSO Profile => profile;

        private void Awake()
        {
            battle = GetComponent<RaidBattleController>();
            ResolveProfile();
            EnsureAudioSources();
            StopImmediate();
        }

        private void Start()
        {
            if (profile == null || audioActive)
            {
                return;
            }

            StartAudio(RaidPhase.Phase1, false);
            if (activeMusicSource != null)
            {
                activeMusicSource.volume = profile.GetMusicVolume(RaidPhase.Phase1);
            }
        }

        private void OnEnable()
        {
            if (battle == null)
            {
                battle = GetComponent<RaidBattleController>();
            }

            if (battle == null)
            {
                return;
            }

            battle.OnRaidPreparing += HandleRaidPreparing;
            battle.OnRaidStarted += HandleRaidStarted;
            battle.OnRaidEnded += HandleRaidEnded;
            battle.OnPhaseTransitionStarted += HandlePhaseTransitionStarted;
            battle.OnPhaseTransitionCompleted += HandlePhaseTransitionCompleted;
            battle.OnStateChanged += HandleStateChanged;
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
                battle.OnStateChanged -= HandleStateChanged;
            }

            StopImmediate();
        }

        private void Update()
        {
            if (!audioActive || profile == null)
            {
                return;
            }

            float deltaTime = Mathf.Max(0f, Time.unscaledDeltaTime);
            float fadeDuration = Mathf.Max(0.05f, profile.MusicFadeDuration);
            float referenceVolume = Mathf.Max(0.01f, profile.Phase1MusicVolume, profile.Phase2MusicVolume, profile.Phase3MusicVolume, musicSourceA.volume, musicSourceB.volume);
            float maxDelta = deltaTime * referenceVolume / fadeDuration;
            musicSourceA.volume = Mathf.MoveTowards(musicSourceA.volume, musicATarget * backgroundDuck, maxDelta);
            musicSourceB.volume = Mathf.MoveTowards(musicSourceB.volume, musicBTarget * backgroundDuck, maxDelta);

            if (fadeOutRequested && MusicVolumesSilent())
            {
                StopImmediate();
            }
        }

        private void ResolveProfile()
        {
            if (profile == null)
            {
                profile = Resources.Load<RaidAudioProfileSO>(DefaultProfileResourcePath);
            }

            if (profile == null)
            {
                Debug.LogError($"RaidAudioProfile을 찾을 수 없습니다. Resources 경로: {DefaultProfileResourcePath}", this);
            }
        }

        private void EnsureAudioSources()
        {
            Transform existingRoot = transform.Find("RaidAudio");
            if (existingRoot == null)
            {
                GameObject root = new GameObject("RaidAudio");
                audioRoot = root.transform;
                audioRoot.SetParent(transform, false);
            }
            else
            {
                audioRoot = existingRoot;
            }

            startSource = EnsureSource("Start", false);
            musicSourceA = EnsureSource("MusicA", true);
            musicSourceB = EnsureSource("MusicB", true);
            phaseSurgeSource = EnsureSource("PhaseSurge", false);
            activeMusicSource = musicSourceA;
            standbyMusicSource = musicSourceB;
            collapseImpactSource = EnsureSource("CollapseImpact", false);
            collapseRumbleSource = EnsureSource("CollapseRumble", false);
            collapseDetailSource = EnsureSource("CollapseDetail", false);
            collapseFinalSource = EnsureSource("CollapseFinal", false);
        }

        private AudioSource EnsureSource(string objectName, bool loop)
        {
            Transform child = audioRoot.Find(objectName);
            GameObject sourceObject;
            if (child == null)
            {
                sourceObject = new GameObject(objectName);
                sourceObject.transform.SetParent(audioRoot, false);
            }
            else
            {
                sourceObject = child.gameObject;
            }

            AudioSource source = sourceObject.GetComponent<AudioSource>();
            if (source == null)
            {
                source = sourceObject.AddComponent<AudioSource>();
            }

            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            source.volume = 0f;
            source.pitch = 1f;
            return source;
        }

        private void HandleRaidPreparing()
        {
            if (!audioActive)
            {
                StartAudio(RaidPhase.Phase1, true);
                return;
            }

            if (profile != null && profile.StartRumble != null)
            {
                startSource.Stop();
                startSource.clip = profile.StartRumble;
                startSource.volume = profile.StartVolume;
                startSource.Play();
            }
        }

        private void HandleRaidStarted()
        {
            if (!audioActive)
            {
                StartAudio(battle != null ? battle.CurrentPhase : RaidPhase.Phase1, false);
            }
        }

        private void HandleRaidEnded(RaidBattleResult result)
        {
            BeginFadeOut();
        }

        private void HandlePhaseTransitionStarted(RaidPhaseTransitionInfo info)
        {
            if (profile == null || !audioActive)
            {
                return;
            }

            if (collapseRoutine != null)
            {
                StopCoroutine(collapseRoutine);
            }

            collapseRoutine = StartCoroutine(PlayCollapseAudio(info));
        }

        private void HandlePhaseTransitionCompleted(RaidPhaseTransitionInfo info)
        {
            collapseActive = false;
            backgroundDuck = 1f;
            ApplyPhase(info.ToPhase);
            PlayOneShot(phaseSurgeSource, profile != null ? profile.GetPhaseSurge(info.ToPhase) : null, profile != null ? profile.GetPhaseSurgeVolume(info.ToPhase) : 0f);
        }

        private void HandleStateChanged(RaidBattleState nextState)
        {
            if (nextState == RaidBattleState.Running && collapseActive)
            {
                collapseActive = false;
                backgroundDuck = 1f;
            }

            if (nextState == RaidBattleState.Idle && battle != null && !battle.IsPreparing && audioActive)
            {
                BeginFadeOut();
            }
        }

        private void StartAudio(RaidPhase phase, bool playStartRumble)
        {
            if (profile == null)
            {
                return;
            }

            fadeOutRequested = false;
            audioActive = true;
            collapseActive = false;
            backgroundDuck = 1f;
            StopCollapseAudio();
            StopSource(musicSourceA);
            StopSource(musicSourceB);
            activeMusicSource = musicSourceA;
            standbyMusicSource = musicSourceB;
            musicATarget = 0f;
            musicBTarget = 0f;
            PlayMusic(activeMusicSource, profile.GetMusic(phase), 0);

            if (playStartRumble && profile.StartRumble != null)
            {
                startSource.Stop();
                startSource.clip = profile.StartRumble;
                startSource.volume = profile.StartVolume;
                startSource.Play();
            }

            SetMusicTarget(activeMusicSource, profile.GetMusicVolume(phase));
        }

        private void ApplyPhase(RaidPhase phase)
        {
            AudioClip nextClip = profile.GetMusic(phase);
            float nextVolume = profile.GetMusicVolume(phase);
            if (activeMusicSource.clip == nextClip && activeMusicSource.isPlaying)
            {
                SetMusicTarget(activeMusicSource, nextVolume);
                SetMusicTarget(standbyMusicSource, 0f);
                return;
            }

            int syncSample = GetSynchronizedSample(activeMusicSource, nextClip);
            PlayMusic(standbyMusicSource, nextClip, syncSample);
            SetMusicTarget(activeMusicSource, 0f);
            SetMusicTarget(standbyMusicSource, nextVolume);
            AudioSource previous = activeMusicSource;
            activeMusicSource = standbyMusicSource;
            standbyMusicSource = previous;
        }

        private static int GetSynchronizedSample(AudioSource reference, AudioClip nextClip)
        {
            if (reference == null || reference.clip == null || nextClip == null || reference.clip.samples <= 0 || nextClip.samples <= 0)
            {
                return 0;
            }

            double normalized = (double)reference.timeSamples / reference.clip.samples;
            int sample = (int)(normalized * nextClip.samples);
            return Mathf.Clamp(sample, 0, Mathf.Max(0, nextClip.samples - 1));
        }

        private static void PlayMusic(AudioSource source, AudioClip clip, int timeSample)
        {
            source.Stop();
            source.clip = clip;
            source.loop = true;
            source.volume = 0f;
            source.pitch = 1f;
            if (clip == null)
            {
                return;
            }

            source.timeSamples = Mathf.Clamp(timeSample, 0, Mathf.Max(0, clip.samples - 1));
            source.Play();
        }

        private void SetMusicTarget(AudioSource source, float target)
        {
            if (source == musicSourceA)
            {
                musicATarget = Mathf.Clamp01(target);
            }
            else
            {
                musicBTarget = Mathf.Clamp01(target);
            }
        }

        private IEnumerator PlayCollapseAudio(RaidPhaseTransitionInfo info)
        {
            collapseActive = true;
            backgroundDuck = profile.CollapseBackgroundDuck;
            StopCollapseSources();

            float timingScale = Mathf.Max(0.2f, info.Duration / ReferenceCollapseDuration);
            PlayOneShot(collapseImpactSource, profile.CollapseImpact, profile.CollapseImpactVolume);

            yield return new WaitForSecondsRealtime(0.14f * timingScale);
            PlayOneShot(collapseRumbleSource, profile.CollapseRumble, profile.CollapseRumbleVolume);

            yield return new WaitForSecondsRealtime(0.48f * timingScale);
            PlayOneShot(collapseDetailSource, PickRandomClip(profile.CollapseWhooshClips, ref lastCollapseWhooshIndex), profile.CollapseWhooshVolume);

            yield return new WaitForSecondsRealtime(0.56f * timingScale);
            PlayOneShot(collapseDetailSource, PickRandomClip(profile.CollapseBreakClips, ref lastCollapseBreakIndex), profile.CollapseBreakVolume);

            yield return new WaitForSecondsRealtime(0.34f * timingScale);
            PlayOneShot(collapseDetailSource, PickRandomClip(profile.CollapseWhooshClips, ref lastCollapseWhooshIndex), profile.CollapseWhooshVolume * 0.9f);
            PlayOneShot(collapseDetailSource, PickRandomClip(profile.CollapseBreakClips, ref lastCollapseBreakIndex), profile.CollapseBreakVolume * 0.95f);

            yield return new WaitForSecondsRealtime(0.36f * timingScale);
            PlayOneShot(collapseDetailSource, PickRandomClip(profile.CollapseBreakClips, ref lastCollapseBreakIndex), profile.CollapseBreakVolume * 0.9f);

            yield return new WaitForSecondsRealtime(0.34f * timingScale);
            PlayOneShot(collapseFinalSource, profile.CollapseFinalDrop, profile.CollapseFinalDropVolume);
            collapseRoutine = null;
        }

        private static void PlayOneShot(AudioSource source, AudioClip clip, float volume)
        {
            if (source == null || clip == null || volume <= 0f)
            {
                return;
            }

            source.volume = 1f;
            source.pitch = 1f;
            source.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        private static AudioClip PickRandomClip(AudioClip[] clips, ref int lastIndex)
        {
            if (clips == null || clips.Length == 0)
            {
                return null;
            }

            int index = Random.Range(0, clips.Length);
            if (clips.Length > 1 && index == lastIndex)
            {
                index = (index + 1) % clips.Length;
            }

            int attempts = clips.Length;
            while (clips[index] == null && attempts-- > 0)
            {
                index = (index + 1) % clips.Length;
            }

            AudioClip clip = clips[index];
            if (clip != null)
            {
                lastIndex = index;
            }

            return clip;
        }

        private void BeginFadeOut()
        {
            if (!audioActive)
            {
                return;
            }

            fadeOutRequested = true;
            collapseActive = false;
            backgroundDuck = 1f;
            musicATarget = 0f;
            musicBTarget = 0f;
            startSource.Stop();
            StopCollapseAudio();
        }

        private bool MusicVolumesSilent()
        {
            return musicSourceA.volume <= VolumeEpsilon && musicSourceB.volume <= VolumeEpsilon;
        }

        private void StopImmediate()
        {
            audioActive = false;
            fadeOutRequested = false;
            collapseActive = false;
            backgroundDuck = 1f;
            musicATarget = 0f;
            musicBTarget = 0f;
            lastCollapseWhooshIndex = -1;
            lastCollapseBreakIndex = -1;
            StopCollapseAudio();
            StopSource(startSource);
            StopSource(musicSourceA);
            StopSource(musicSourceB);
            activeMusicSource = musicSourceA;
            standbyMusicSource = musicSourceB;
        }

        private void StopCollapseAudio()
        {
            if (collapseRoutine != null)
            {
                StopCoroutine(collapseRoutine);
                collapseRoutine = null;
            }

            StopCollapseSources();
        }

        private void StopCollapseSources()
        {
            StopSource(collapseImpactSource);
            StopSource(collapseRumbleSource);
            StopSource(collapseDetailSource);
            StopSource(collapseFinalSource);
        }

        private static void StopSource(AudioSource source)
        {
            if (source == null)
            {
                return;
            }

            source.Stop();
            source.clip = null;
            source.volume = 0f;
            source.pitch = 1f;
        }
    }
}
