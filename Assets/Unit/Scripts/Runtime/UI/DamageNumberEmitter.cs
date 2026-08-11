using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CombatHealth))]
    [RequireComponent(typeof(CombatEntityAnchors))]
    public sealed class DamageNumberEmitter : MonoBehaviour
    {
        private CombatHealth health;
        private CombatEntityAnchors anchors;
        private DamageNumberTargetType targetType;
        private bool subscribed;

        private void Awake()
        {
            health = GetComponent<CombatHealth>();
            anchors = GetComponent<CombatEntityAnchors>();
            targetType = GetComponentInParent<UnitRuntimeState>() != null ? DamageNumberTargetType.Unit : DamageNumberTargetType.Enemy;
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (subscribed)
            {
                return;
            }

            if (health == null)
            {
                health = GetComponent<CombatHealth>();
            }

            if (anchors == null)
            {
                anchors = GetComponent<CombatEntityAnchors>();
            }

            if (health == null || anchors == null)
            {
                return;
            }

            health.OnDamageResolved += HandleDamageResolved;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            if (health != null)
            {
                health.OnDamageResolved -= HandleDamageResolved;
            }

            subscribed = false;
        }

        private void HandleDamageResolved(CombatHealth sender, DamageInfo damageInfo, float appliedDamage)
        {
            if (sender == null || damageInfo.FinalDamage <= 0f || appliedDamage <= 0f)
            {
                return;
            }

            Vector3 worldPosition = anchors != null && anchors.EffectPoint != null ? anchors.EffectPoint.position : transform.position;
            DamageNumberPool.Show(sender, damageInfo, worldPosition, targetType);
        }
    }
}