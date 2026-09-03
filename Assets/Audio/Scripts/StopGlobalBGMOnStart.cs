using UnityEngine;

public class StopGlobalBGMOnStart : MonoBehaviour
{
    private void Start()
    {
        if (SoundManager.Instance == null) return;

        SoundManager.Instance.StopBGM();
    }
}