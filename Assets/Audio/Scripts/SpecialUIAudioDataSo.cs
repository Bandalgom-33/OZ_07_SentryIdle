using UnityEngine;

[CreateAssetMenu(
    fileName = "SpecialUIAudioDataSo",
    menuName = "GameAudio/Special UI Audio Data")]
public class SpecialUIAudioDataSo : ScriptableObject
{
    [Header("Confirm / Cancel")]
    [SerializeField] private AudioClip confirmSound;
    [Range(0f, 1f)]
    [SerializeField] private float confirmVolume = 1f;

    [SerializeField] private AudioClip cancelSound;
    [Range(0f, 1f)]
    [SerializeField] private float cancelVolume = 1f;

    [Header("Gacha")]
    [SerializeField] private AudioClip gachaSound;
    [Range(0f, 1f)]
    [SerializeField] private float gachaVolume = 1f;

    [Header("Equipment")]
    [SerializeField] private AudioClip equipSound;
    [Range(0f, 1f)]
    [SerializeField] private float equipVolume = 1f;

    [Header("Upgrade")]
    [SerializeField] private AudioClip upgradeSound;
    [Range(0f, 1f)]
    [SerializeField] private float upgradeVolume = 1f;

    [Header("Popup")]
    [SerializeField] private AudioClip popupOpenSound;
    [Range(0f, 1f)]
    [SerializeField] private float popupOpenVolume = 1f;

    [SerializeField] private AudioClip popupCloseSound;
    [Range(0f, 1f)]
    [SerializeField] private float popupCloseVolume = 1f;

    public AudioClip ConfirmSound => confirmSound;
    public float ConfirmVolume => confirmVolume;

    public AudioClip CancelSound => cancelSound;
    public float CancelVolume => cancelVolume;

    public AudioClip GachaSound => gachaSound;
    public float GachaVolume => gachaVolume;

    public AudioClip EquipSound => equipSound;
    public float EquipVolume => equipVolume;

    public AudioClip UpgradeSound => upgradeSound;
    public float UpgradeVolume => upgradeVolume;

    public AudioClip PopupOpenSound => popupOpenSound;
    public float PopupOpenVolume => popupOpenVolume;

    public AudioClip PopupCloseSound => popupCloseSound;
    public float PopupCloseVolume => popupCloseVolume;
}