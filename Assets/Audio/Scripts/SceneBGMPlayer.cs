using UnityEngine;

public class SceneBGMPlayer : MonoBehaviour
{
    [SerializeField] private AudioClip bgmClip;

    private void Start()
    {
        if (SoundManager.Instance == null) return;
        if (bgmClip == null) return;

        SoundManager.Instance.PlayBGM(bgmClip);
    }
}