using UnityEngine;
using UnityEngine.UI;

public class AudioSettingsUI : MonoBehaviour
{
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    //설정창의 슬라이더에 현재 저장된 볼륨값에 맞춰 위치를 맞춤
    private void OnEnable()
    {
        if (SoundManager.Instance == null) return;

        bgmSlider.SetValueWithoutNotify(
            SoundManager.Instance.BGMVolume
        );

        sfxSlider.SetValueWithoutNotify(
            SoundManager.Instance.SFXVolume
        );
    }
}