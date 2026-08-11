using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    /// <summary>
    /// 전투 시스템에서 재사용할 수 있는 소환 요청입니다.
    /// 패시브뿐 아니라 이후 고유능력 등에서도 같은 소환 Runtime을 사용할 수 있습니다.
    /// </summary>
    public readonly struct SummonRequest
    {
        public UnitRuntimeState UnitOwner { get; }
        public EnemyRuntimeState EnemyOwner { get; }
        public GameObject Prefab { get; }
        public int Count { get; }
        public Object Source { get; }
        public Vector3 Origin { get; }

        public bool IsUnitRequest => UnitOwner != null;
        public bool IsEnemyRequest => EnemyOwner != null;

        public SummonRequest(UnitRuntimeState unitOwner, GameObject prefab, int count, Object source = null)
        {
            UnitOwner = unitOwner;
            EnemyOwner = null;
            Prefab = prefab;
            Count = Mathf.Max(1, count);
            Source = source;
            Origin = unitOwner != null ? unitOwner.transform.position : Vector3.zero;
        }

        public SummonRequest(EnemyRuntimeState enemyOwner, GameObject prefab, int count, Object source = null)
        {
            UnitOwner = null;
            EnemyOwner = enemyOwner;
            Prefab = prefab;
            Count = Mathf.Max(1, count);
            Source = source;
            Origin = enemyOwner != null ? enemyOwner.transform.position : Vector3.zero;
        }
    }
}
