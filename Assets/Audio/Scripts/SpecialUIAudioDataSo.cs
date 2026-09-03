using UnityEngine;

[CreateAssetMenu(
    fileName = "SpecialUIAudioDataSo",
    menuName = "GameAudio/Special UI Audio Data")]
public class SpecialUIAudioDataSo : ScriptableObject
{
    [Header("Gacha")]
    [SerializeField] private AudioClip gachaSound;

    [Range(0f, 1f)]
    [SerializeField] private float gachaVolume = 1f;

    public AudioClip GachaSound => gachaSound;
    public float GachaVolume => gachaVolume;
}