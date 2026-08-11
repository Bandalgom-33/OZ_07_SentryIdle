using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyRuntimeState))]
    [RequireComponent(typeof(CombatHealth))]
    public sealed class EnemyBlock : MonoBehaviour
    {
        [Header("저지 위치")]
        [Tooltip("저지될 때 캐릭터 타일 중심으로부터 몬스터 중심이 떨어질 거리입니다. 타일 간격을 1로 보며, 소형 0.8, 중형 1.0, 대형 1.2 이상을 기준으로 프리팹마다 조절합니다.")]
        [SerializeField, Min(0f)]
        private float blockStopDistance = 1f;

        private EnemyRuntimeState state;
        private CombatHealth health;
        private UnitBlock blocker;

        public EnemyRuntimeState State => state;
        public float BlockStopDistance => Mathf.Max(0f, blockStopDistance);
        public UnitBlock Blocker => blocker;
        public bool IsBlocked => blocker != null;
        public bool CanBeBlocked => state != null &&
                                    state.IsInitialized &&
                                    health != null &&
                                    !health.IsDead &&
                                    state.DataLink != null &&
                                    state.DataLink.HasData &&
                                    state.DataLink.EnemyData.MovementType == EnemyMovementType.Ground;

        private void Awake()
        {
            state = GetComponent<EnemyRuntimeState>();
            health = GetComponent<CombatHealth>();
            health.OnDied += HandleDied;
        }

        private void OnDisable()
        {
            BlockLink.Release(this);
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.OnDied -= HandleDied;
            }
        }

        private void OnValidate()
        {
            blockStopDistance = Mathf.Max(0f, blockStopDistance);
        }

        internal void Attach(UnitBlock unit)
        {
            blocker = unit;
        }

        internal void Detach()
        {
            blocker = null;
        }

        private void HandleDied(CombatHealth sender)
        {
            BlockLink.Release(this);
        }
    }
}