using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class GachaButtonSound : MonoBehaviour
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();

        // 기존 기본 클릭음 제거
        UIButtonSound uiButtonSound = GetComponent<UIButtonSound>();
        if (uiButtonSound != null)
        {
            uiButtonSound.enabled = false;
        }

        button.onClick.AddListener(PlayGachaSound);
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(PlayGachaSound);
        }
    }

    private void PlayGachaSound()
    {
        if (SoundManager.Instance == null) return;

        SoundManager.Instance.PlayGachaSound();
    }
}