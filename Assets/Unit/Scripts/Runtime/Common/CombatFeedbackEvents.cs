using System;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    public static class CombatFeedbackEvents
    {
        public static event Action<CombatHealth, Vector3> OnAttackMissed;

        public static void PublishAttackMissed(CombatHealth target, Vector3 worldPosition)
        {
            if (target == null)
            {
                return;
            }

            OnAttackMissed?.Invoke(target, worldPosition);
        }
    }
}