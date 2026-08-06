using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [CreateAssetMenu(
        fileName = "MoveSpeedBoostPassive",
        menuName = "Endless Guard/Passive/이동속도 증가")]
    public sealed class MoveSpeedBoostPassiveSO : PassiveDataSO
    {
        [Header("이동속도 증가 설정")]
        [Tooltip(
            "기준 이동속도에 추가할 비율입니다. " +
            "50을 입력하면 기준 이동속도의 50%가 추가되어 최종 150%가 됩니다.")]
        [Min(0f)]
        [SerializeField] private float bonusMoveSpeedPercent;

        public float BonusMoveSpeedPercent => bonusMoveSpeedPercent;
    }
}