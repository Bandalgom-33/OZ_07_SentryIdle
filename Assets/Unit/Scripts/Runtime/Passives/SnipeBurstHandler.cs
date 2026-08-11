using System;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    internal sealed class SnipeBurstHandler : IEnemySnipeBurstPassiveHandler
    {
        public Type DataType => typeof(SnipeBurstSO);

        public int GetBurstAttackCount(EnemyRuntimeState owner, PassiveDataSO passive, PassiveTuning tuning)
        {
            SnipeBurstSO data = passive as SnipeBurstSO;

            if (data == null)
            {
                return 0;
            }

            float count = tuning != null ? tuning.GetValue(PassiveValueKey.BurstAttackCount) : data.BurstAttackCount;
            return Mathf.Max(1, Mathf.RoundToInt(count));
        }

        public float GetForcedMoveSeconds(EnemyRuntimeState owner, PassiveDataSO passive, PassiveTuning tuning)
        {
            SnipeBurstSO data = passive as SnipeBurstSO;

            if (data == null)
            {
                return 0f;
            }

            float seconds = tuning != null ? tuning.GetValue(PassiveValueKey.ForcedMoveSeconds) : data.ForcedMoveSeconds;
            return Mathf.Max(0f, seconds);
        }
    }
}
