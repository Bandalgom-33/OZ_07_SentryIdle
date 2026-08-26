using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [CreateAssetMenu(fileName = "Summon", menuName = "Endless Guard/Passive/주기적 소환")]
    public sealed class SummonSO : PassiveDataSO
    {
        [Header("주기적 소환 기본값")]
        [Tooltip("소환 효과가 반복해서 발동하는 주기입니다. 단위는 초입니다. 몬스터별 패시브 개별 수치에서 따로 조정할 수 있습니다.")]
        [Min(0.1f)]
        [SerializeField] private float summonIntervalSeconds = 10f;

        [Tooltip("한 번의 소환 효과가 발동할 때 생성하는 기본 소환물 수입니다. 몬스터별 패시브 개별 수치에서 따로 조정할 수 있습니다.")]
        [Min(1)]
        [SerializeField] private int summonCount = 1;

        [Tooltip("한 소환사 몬스터가 동시에 유지할 수 있는 최대 소환물 수입니다. 상한에 도달하면 다음 소환 주기를 건너뜁니다.")]
        [Min(1)]
        [SerializeField] private int maxActiveSummons = 4;

        [Tooltip("몬스터별 소환물 프리팹이 따로 설정되지 않았을 때 사용할 기본 소환물 프리팹입니다. 실제 소환물 제작 후 연결하며 현재는 비어 있어도 정상입니다.")]
        [SerializeField] private GameObject summonPrefab;

        public float SummonIntervalSeconds => summonIntervalSeconds;
        public int SummonCount => summonCount;
        public int MaxActiveSummons => maxActiveSummons;
        public GameObject SummonPrefab => summonPrefab;

        public override bool TryGetDefaultValue(PassiveValueKey key, out float value)
        {
            switch (key)
            {
                case PassiveValueKey.SummonIntervalSeconds:
                    value = summonIntervalSeconds;
                    return true;

                case PassiveValueKey.SummonCount:
                    value = summonCount;
                    return true;

                case PassiveValueKey.MaxActiveSummons:
                    value = maxActiveSummons;
                    return true;

                default:
                    value = 0f;
                    return false;
            }
        }

        public override bool TryGetDefaultReference(PassiveRefKey key, out UnityEngine.Object reference)
        {
            if (key == PassiveRefKey.SummonPrefab)
            {
                reference = summonPrefab;
                return true;
            }

            reference = null;
            return false;
        }
    }
}