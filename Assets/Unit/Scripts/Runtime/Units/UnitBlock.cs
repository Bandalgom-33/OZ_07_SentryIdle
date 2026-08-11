using System.Collections.Generic;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UnitRuntimeState))]
    [RequireComponent(typeof(CombatHealth))]
    public sealed class UnitBlock : MonoBehaviour
    {
        private readonly List<EnemyBlock> enemies = new List<EnemyBlock>();
        private UnitRuntimeState state;
        private CombatHealth health;

        public UnitRuntimeState State => state;
        public int MaxCount => state != null && state.DataLink != null && state.DataLink.HasData ? Mathf.Max(0, state.DataLink.UnitData.BlockCount) : 0;
        public int Count => enemies.Count;
        public bool IsFull => MaxCount <= 0 || Count >= MaxCount;
        public IReadOnlyList<EnemyBlock> Enemies => enemies;

        private void Awake()
        {
            state = GetComponent<UnitRuntimeState>();
            health = GetComponent<CombatHealth>();
            health.OnDied += HandleDied;
        }

        private void OnDisable()
        {
            ReleaseAll();
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.OnDied -= HandleDied;
            }
        }

        public bool CanBlock(EnemyBlock enemy)
        {
            if (enemy == null || state == null || health == null)
            {
                return false;
            }

            if (!state.IsInitialized || health.IsDead || MaxCount <= 0)
            {
                return false;
            }

            if (!enemy.CanBeBlocked || enemy.IsBlocked)
            {
                return false;
            }

            return Count < MaxCount;
        }

        public void ReleaseAll()
        {
            while (enemies.Count > 0)
            {
                BlockLink.Release(enemies[enemies.Count - 1]);
            }
        }

        internal void Attach(EnemyBlock enemy)
        {
            if (enemy != null && !enemies.Contains(enemy))
            {
                enemies.Add(enemy);
            }
        }

        internal void Detach(EnemyBlock enemy)
        {
            if (enemy != null)
            {
                enemies.Remove(enemy);
            }
        }

        private void HandleDied(CombatHealth sender)
        {
            ReleaseAll();
        }
    }
}