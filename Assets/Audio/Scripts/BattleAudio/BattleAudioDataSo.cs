using UnityEngine;

[CreateAssetMenu(
    fileName = "BattleAudioDataSo",
    menuName = "UnitBattleSoundSo/Audio/Combat Audio Profile")]
public class BattleAudioDataSo : ScriptableObject
{
    [Header("Attack")]
    [SerializeField] private AudioClip attackSound;
    [Range(0f, 1f)]
    [SerializeField] private float attackVolume = 1f;

    [Header("Hit")]
    [SerializeField] private AudioClip hitSound;
    [Range(0f, 1f)]
    [SerializeField] private float hitVolume = 1f;

    public AudioClip AttackSound => attackSound;
    public float AttackVolume => attackVolume;

    public AudioClip HitSound => hitSound;
    public float HitVolume => hitVolume;
}