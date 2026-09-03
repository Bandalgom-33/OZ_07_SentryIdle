using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;
    
    [Header("Common UI Sound")]
    [SerializeField] private AudioClip uiClickSound;
    
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;
    
    [Header("Game Progress Audio")]
    [SerializeField] private GameProgressAudioDataSo gameProgressAudioData;
    
    [Header("Special UI Audio")]
    [SerializeField] private SpecialUIAudioDataSo specialUIAudioData;

    //볼륨을 저장할 키 생성
    private const string BGM_VOLUME_KEY = "BGMVolume";
    private const string SFX_VOLUME_KEY = "SFXVolume";
    
    public float BGMVolume { get; private set; } = 1f;
    public float SFXVolume { get; private set; } = 1f;
    

    public event Action<float, float> OnVolumeChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
        //저장한 볼륨 호출
        LoadVolumeSettings();
    }
    
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        EventBus.Subscribe<DataSaveEvent>(OnSave);
        EventBus.Subscribe<DataLoadEvent>(OnLoad);
        EventBus.Subscribe<DataResetEvent>(OnReset);
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        EventBus.Unsubscribe<DataSaveEvent>(OnSave);
        EventBus.Unsubscribe<DataLoadEvent>(OnLoad);
        EventBus.Unsubscribe<DataResetEvent>(OnReset);
    }


    //배경음악 재생시키기
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;
        if (bgmSource == null) return;

        //이미 같은 BGM 재생중이면 시작 ㄴㄴ
        if (bgmSource.clip == clip && bgmSource.isPlaying)
        {
            return;
        }

        bgmSource.clip = clip;
        bgmSource.Play();
    }

    //BGM 정지
    public void StopBGM()
    {
        if (bgmSource == null) return;

        bgmSource.Stop();
    }

    //효과음 재생 시키기
    // 기본 효과음 재생
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        if (sfxSource == null) return;

        sfxSource.PlayOneShot(clip);
    }

    // 볼륨 지정 효과음 재생
    public void PlaySFX(AudioClip clip, float volumeScale)
    {
        if (clip == null) return;
        if (sfxSource == null) return;

        sfxSource.PlayOneShot(
            clip,
            Mathf.Clamp01(volumeScale)
        );
    }
    
    //클릭시 사운드 출력시키기
    public void PlayUIClick()
    {
        if (uiClickSound == null) return;

        PlaySFX(uiClickSound);
    }
    
    //씬 이동시 자동으로 버튼에 사운드 할당시키기
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AttachUIButtonSounds();
    }
    
    
    private void AttachUIButtonSounds()
    {
        Button[] buttons = FindObjectsByType<Button>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        //모든 버튼 검색
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            
            //버튼 없으면 자동 추가
            if (button.GetComponent<UIButtonSound>() != null)
                //있으면 그대로 진행
                continue;
            
            button.gameObject.AddComponent<UIButtonSound>();
        }
    }
    
    //마스터, BGM, SFX 볼륨 UI슬라이더로 조절하기
   /*
    public void SetMasterVolume(float value)
    {
        float db = Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f;
        audioMixer.SetFloat("MasterVolume", db);
    }
    */

   
    // 배경음악 볼륨 설정 및 실시간 반영
    public void SetBGMVolume(float value)
    {
        // 볼륨값 0.0~1.0 정규화 범위 보정
        BGMVolume = Mathf.Clamp01(value);

        // 오디오 믹서 데시벨 변환 적용
        ApplyBGMVolume(BGMVolume);

        // 빠른 로드를 위한 PlayerPrefs 캐시 기록
        PlayerPrefs.SetFloat(BGM_VOLUME_KEY, BGMVolume);
        PlayerPrefs.Save();

        // 볼륨 변경 알림 발행
        OnVolumeChanged?.Invoke(BGMVolume, SFXVolume);
    }

    // 효과음 볼륨 설정 및 실시간 반영
    public void SetSFXVolume(float value)
    {
        // 볼륨값 0.0~1.0 정규화 범위 보정
        SFXVolume = Mathf.Clamp01(value);

        // 오디오 믹서 데시벨 변환 적용
        ApplySFXVolume(SFXVolume);

        // 빠른 로드를 위한 PlayerPrefs 캐시 기록
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, SFXVolume);
        PlayerPrefs.Save();

        // 볼륨 변경 알림 발행
        OnVolumeChanged?.Invoke(BGMVolume, SFXVolume);
    }

    // 오디오 설정 세이브 데이터 저장 처리
    private void OnSave(DataSaveEvent evt)
    {
        if (evt.saveData == null) return;
        if (evt.saveData.sound == null)
        {
            evt.saveData.sound = new SoundSaveData();
        }

        // 현재 적용된 볼륨 수치 세이브 데이터 기록
        evt.saveData.sound.bgmVolume = BGMVolume;
        evt.saveData.sound.sfxVolume = SFXVolume;
    }

    // 오디오 설정 세이브 데이터 로드 처리
    private void OnLoad(DataLoadEvent evt)
    {
        if (evt.saveData == null || evt.saveData.sound == null) return;

        // 세이브 데이터에 저장된 볼륨값 복원 적용
        SetBGMVolume(evt.saveData.sound.bgmVolume);
        SetSFXVolume(evt.saveData.sound.sfxVolume);
    }

    // 오디오 설정 기본값 초기화 처리
    private void OnReset(DataResetEvent evt)
    {
        // 기본 100% 볼륨으로 복원
        SetBGMVolume(1f);
        SetSFXVolume(1f);
    }

    // 로컬 캐시 볼륨 설정 불러오기
    private void LoadVolumeSettings()
    {
        // 세이브 파일 로드 전 초기 믹서 적용용 로컬 캐시 조회
        BGMVolume = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 1f);
        SFXVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);

        ApplyBGMVolume(BGMVolume);
        ApplySFXVolume(SFXVolume);
    }
    
    //Mixer 적용 담당 메서드
    private void ApplyBGMVolume(float value)
    {
        if (audioMixer == null) return;

        float db =
            Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f;

        audioMixer.SetFloat("BGMVolume", db);
    }

    private void ApplySFXVolume(float value)
    {
        if (audioMixer == null) return;

        float db =
            Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f;

        audioMixer.SetFloat("SFXVolume", db);
    }
    
    //웨이브 시작 사운드
    public void PlayWaveStartSound()
    {
        if (gameProgressAudioData == null) return;

        PlaySFX( gameProgressAudioData.WaveStartSound, gameProgressAudioData.WaveStartVolume);
    }

    //웨이브 클리어 사운드
    public void PlayWaveClearSound()
    {
        if (gameProgressAudioData == null) return;

        PlaySFX( gameProgressAudioData.WaveClearSound, gameProgressAudioData.WaveClearVolume );
    }

    //스테이지 클리어사운드
    public void PlayStageClearSound()
    {
        if (gameProgressAudioData == null) return;

        PlaySFX(gameProgressAudioData.StageClearSound, gameProgressAudioData.StageClearVolume );
    }

    //스테이지 실패 사운드
    public void PlayStageFailSound()
    {
        if (gameProgressAudioData == null) return;

        PlaySFX( gameProgressAudioData.StageFailSound, gameProgressAudioData.StageFailVolume );
    }

    //보상 사운드
    public void PlayRewardSound()
    {
        if (gameProgressAudioData == null) return;

        PlaySFX(gameProgressAudioData.RewardSound, gameProgressAudioData.RewardVolume);
    }
    
    //가쳐 사운드 재생
    public void PlayGachaSound()
    {
        if (specialUIAudioData == null) return;

        PlaySFX(
            specialUIAudioData.GachaSound,
            specialUIAudioData.GachaVolume
        );
    }
   
}