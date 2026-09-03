using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class AudioSettingsUI : MonoBehaviour
{
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

	[SerializeField] private TMP_Text bgmValueText;
	[SerializeField] private TMP_Text sfxValueText;

    // 패널 활성화 시 저장된 볼륨값으로 슬라이더 초기화 및 리스너 등록
    private void OnEnable()
    {
        if (SoundManager.Instance == null) return;

        // 슬라이더 이벤트 중복 트리거 방지를 위한 SetValueWithoutNotify 호출
        if (bgmSlider != null)
        {
            bgmSlider.SetValueWithoutNotify(SoundManager.Instance.BGMVolume);
            bgmSlider.onValueChanged.RemoveListener(OnBGMVolumeChanged);
            bgmSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(SoundManager.Instance.SFXVolume);
            sfxSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);
            sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }

        UpdateBGMText(SoundManager.Instance.BGMVolume);
        UpdateSFXText(SoundManager.Instance.SFXVolume);
    }

    // 패널 비활성화 시 슬라이더 리스너 안전 해제
    private void OnDisable()
    {
        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.RemoveListener(OnBGMVolumeChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);
        }
    }

    // 배경음악 슬라이더 변경 시 실시간 사운드 반영 및 텍스트 갱신
    private void OnBGMVolumeChanged(float value)
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetBGMVolume(value);
        }
        UpdateBGMText(value);
    }

    // 효과음 슬라이더 변경 시 실시간 사운드 반영 및 텍스트 갱신
    private void OnSFXVolumeChanged(float value)
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetSFXVolume(value);
        }
        UpdateSFXText(value);
    }

    // 배경음악 볼륨 퍼센트 텍스트 UI 갱신
    public void UpdateBGMText(float value)
    {
        if (bgmValueText == null) return;

        bgmValueText.text = $"{Mathf.RoundToInt(value * 100f)}%";
    }

    // 효과음 볼륨 퍼센트 텍스트 UI 갱신
    public void UpdateSFXText(float value)
    {
        if (sfxValueText == null) return;

        sfxValueText.text = $"{Mathf.RoundToInt(value * 100f)}%";
    }
}