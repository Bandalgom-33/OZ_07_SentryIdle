using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [CreateAssetMenu(fileName = "CritSummon", menuName = "Endless Guard/Passive/치명타 소환")]
    public sealed class CritSummonSO : PassiveDataSO
    {
        [Header("치명타 소환 기본값")]
        [Tooltip("캐릭터별 소환물 프리팹이 따로 설정되지 않았을 때 사용할 기본 소환물 프리팹입니다.")]
        [SerializeField] private GameObject summonPrefab;

        [Tooltip("치명타로 소환한 뒤 다시 소환할 수 있을 때까지의 대기시간입니다.")]
        [Min(0f)]
        [SerializeField] private float summonCooldownSeconds = 3f;

        [Tooltip("이 패시브로 동시에 유지할 수 있는 소환물의 최대 개수입니다.")]
        [Min(1)]
        [SerializeField] private int maxActiveSummons = 3;

        public GameObject SummonPrefab => summonPrefab;
        public float SummonCooldownSeconds => Mathf.Max(0f, summonCooldownSeconds);
        public int MaxActiveSummons => Mathf.Max(1, maxActiveSummons);

        public override bool TryGetDefaultReference(PassiveRefKey key, out Object reference)
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