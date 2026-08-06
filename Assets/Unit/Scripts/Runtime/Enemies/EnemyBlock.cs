using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyRuntimeState))]
    [RequireComponent(typeof(CombatHealth))]
    public sealed class EnemyBlock : MonoBehaviour
    {
        private EnemyRuntimeState state;
        private CombatHealth health;
        private UnitBlock blocker;

        public UnitBlock Blocker => blocker;
        public bool IsBlocked => blocker != null;
        public bool CanBeBlocked => state != null && state.IsInitialized && health != null && !health.IsDead && state.DataLink != null && state.DataLink.HasData && state.DataLink.EnemyData.MovementType == EnemyMovementType.Ground;

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