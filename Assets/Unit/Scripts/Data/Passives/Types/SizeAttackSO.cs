using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [CreateAssetMenu(fileName = "SizeAttack", menuName = "Endless Guard/Passive/크기 대상 공격력 증가")]
    public sealed class SizeAttackSO : PassiveDataSO
    {
        [Header("크기 대상 공격력 증가 설정")]
        [Tooltip("공격력 증가 효과를 적용할 몬스터 크기입니다.")]
        [SerializeField] private EnemySize targetSize = EnemySize.None;

        [Tooltip("해당 크기의 몬스터를 공격할 때 증가하는 공격력 비율입니다. 캐릭터별 패시브 개별 수치에서 따로 조정할 수 있습니다.")]
        [Min(0f)]
        [SerializeField] private float attackBonusPercent = 100f;

        public EnemySize TargetSize => targetSize;
        public float AttackBonusPercent => attackBonusPercent;

        public override bool TryGetDefaultValue(PassiveValueKey key, out float value)
        {
            if (key == PassiveValueKey.AttackBonusPercent)
            {
                value = attackBonusPercent;
                return true;
            }

            value = 0f;
            return false;
        }
    }
}