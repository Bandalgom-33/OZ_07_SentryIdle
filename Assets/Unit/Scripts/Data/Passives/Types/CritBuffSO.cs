using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [CreateAssetMenu(fileName = "CritBuff", menuName = "Endless Guard/Passive/치명타 후 피해 증가")]
    public sealed class CritBuffSO : PassiveDataSO
    {
        [Header("치명타 후 피해 증가 기본값")]
        [Tooltip("치명타 적중 후 지속시간 동안 증가하는 최종 피해 비율입니다. 캐릭터별 패시브 개별 수치에서 별도로 조정할 수 있습니다.")]
        [Min(0f)]
        [SerializeField] private float finalDamageBonusPercent = 30f;

        [Tooltip("치명타 적중 후 최종 피해 증가 효과가 유지되는 시간입니다. 단위는 초입니다. 캐릭터별 패시브 개별 수치에서 별도로 조정할 수 있습니다.")]
        [Min(0f)]
        [SerializeField] private float durationSeconds = 3f;

        public float FinalDamageBonusPercent => finalDamageBonusPercent;
        public float DurationSeconds => durationSeconds;

        public override bool TryGetDefaultValue(PassiveValueKey key, out float value)
        {
            switch (key)
            {
                case PassiveValueKey.FinalDamageBonusPercent:
                    value = finalDamageBonusPercent;
                    return true;

                case PassiveValueKey.DurationSeconds:
                    value = durationSeconds;
                    return true;

                default:
                    value = 0f;
                    return false;
            }
        }
    }
}