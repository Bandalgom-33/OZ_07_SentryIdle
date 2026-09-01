using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class AudioSettingsUI : MonoBehaviour
{
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

	[SerializeField] private TMP_Text bgmValueText;
	[SerializeField] private TMP_Text sfxValueText;

    //설정창의 슬라이더에 현재 저장된 볼륨값에 맞춰 위치를 맞춤
    private void OnEnable()
    {
        if (SoundManager.Instance == null) return;

        bgmSlider.SetValueWithoutNotify(SoundManager.Instance.BGMVolume );
        sfxSlider.SetValueWithoutNotify( SoundManager.Instance.SFXVolume);
        
        UpdateBGMText(SoundManager.Instance.BGMVolume);
        UpdateSFXText(SoundManager.Instance.SFXVolume);
    }
    
    public void UpdateBGMText(float value)
    {
        if (bgmValueText == null) return;

        bgmValueText.text = $"{Mathf.RoundToInt(value * 100f)}%";
    }

    public void UpdateSFXText(float value)
    {
        if (sfxValueText == null) return;

        sfxValueText.text = $"{Mathf.RoundToInt(value * 100f)}%";
    }
}