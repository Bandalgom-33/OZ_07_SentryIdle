using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;
    
    [Header("Common UI Sound")]
    [SerializeField] private AudioClip uiClickSound;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }
    
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
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
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        if (sfxSource == null) return;

        //여러 효과음도 PlayOneShot으로 재생 가능
        sfxSource.PlayOneShot(clip);
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
    
   
}