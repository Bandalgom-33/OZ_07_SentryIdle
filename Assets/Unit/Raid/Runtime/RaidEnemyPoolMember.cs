using System.Collections;
using EndlessGuard.Unit.Runtime;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    [DisallowMultipleComponent]
    public sealed class RaidEnemyPoolMember : EnemyDespawnHandler
    {
        private Coroutine releaseRoutine;

        public override void Despawn(float delay)
        {
            if (releaseRoutine != null || !gameObject.activeInHierarchy)
            {
                return;
            }

            if (delay <= 0f)
            {
                RaidEnemyPool.Release(gameObject);
                return;
            }

            releaseRoutine = StartCoroutine(ReleaseAfterDelay(delay));
        }

        private void OnEnable()
        {
            releaseRoutine = null;
        }

        private void OnDisable()
        {
            releaseRoutine = null;
        }

        private IEnumerator ReleaseAfterDelay(float delay)
        {
            float elapsed = 0f;

            while (elapsed < delay)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            releaseRoutine = null;
            RaidEnemyPool.Release(gameObject);
        }
    }
}
