using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [CreateAssetMenu(fileName = "SizeDamagePassive", menuName = "Endless Guard/Passive/크기 대상 추가 피해")]
    public sealed class SizeDamagePassiveSO : PassiveDataSO
    {
        [Header("크기 대상 추가 피해 설정")]
        [Tooltip("추가 피해를 적용할 몬스터 크기입니다.")]
        [SerializeField] private EnemySize targetSize = EnemySize.None;

        [Tooltip("대상 크기의 몬스터에게 추가할 피해 비율입니다. 100을 입력하면 기본 피해에 100%를 추가하여 최종 200% 피해가 됩니다.")]
        [Min(0f)]
        [SerializeField] private float bonusDamagePercent;

        public EnemySize TargetSize => targetSize;
        public float BonusDamagePercent => bonusDamagePercent;
    }
}