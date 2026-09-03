using UnityEngine;

public class UnitAudioPlayer : MonoBehaviour
{
    [SerializeField] private BattleAudioDataSo audioData;

    public void PlayAttackSound()
    {
        if (audioData == null) return;
        if (SoundManager.Instance == null) return;

        SoundManager.Instance.PlaySFX( audioData.AttackSound, audioData.AttackVolume);
    }

    public void PlayHitSound()
    {
        if (audioData == null) return;
        if (SoundManager.Instance == null) return;

        SoundManager.Instance.PlaySFX(audioData.HitSound, audioData.HitVolume );
    }
}