using UnityEngine;

[CreateAssetMenu(
    fileName = "GameProgressAudioDataSo",
    menuName = "GameAudio/Progress Audio Data")]
public class GameProgressAudioDataSo : ScriptableObject
{
    [Header("Wave Start")]
    [SerializeField] private AudioClip waveStartSound;
    [Range(0f, 1f)]
    [SerializeField] private float waveStartVolume = 1f;

    [Header("Wave Clear")]
    [SerializeField] private AudioClip waveClearSound;
    [Range(0f, 1f)]
    [SerializeField] private float waveClearVolume = 1f;

    [Header("Stage Clear")]
    [SerializeField] private AudioClip stageClearSound;
    [Range(0f, 1f)]
    [SerializeField] private float stageClearVolume = 1f;

    [Header("Stage Fail")]
    [SerializeField] private AudioClip stageFailSound;
    [Range(0f, 1f)]
    [SerializeField] private float stageFailVolume = 1f;

    [Header("Reward")]
    [SerializeField] private AudioClip rewardSound;
    [Range(0f, 1f)]
    [SerializeField] private float rewardVolume = 1f;


    public AudioClip WaveStartSound => waveStartSound;
    public float WaveStartVolume => waveStartVolume;

    public AudioClip WaveClearSound => waveClearSound;
    public float WaveClearVolume => waveClearVolume;

    public AudioClip StageClearSound => stageClearSound;
    public float StageClearVolume => stageClearVolume;

    public AudioClip StageFailSound => stageFailSound;
    public float StageFailVolume => stageFailVolume;

    public AudioClip RewardSound => rewardSound;
    public float RewardVolume => rewardVolume;
}