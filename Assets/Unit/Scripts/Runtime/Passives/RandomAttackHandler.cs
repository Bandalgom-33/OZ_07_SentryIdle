using System;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    internal sealed class RandomAttackHandler : IEnemyRandomTargetPassiveHandler
    {
        public Type DataType => typeof(RandomAttackSO);

        public int GetRandomTargetCount(EnemyRuntimeState owner, PassiveDataSO passive, PassiveTuning tuning)
        {
            RandomAttackSO data = passive as RandomAttackSO;

            if (data == null)
            {
                return 0;
            }

            float count = tuning != null ? tuning.GetValue(PassiveValueKey.RandomTargetCount) : data.RandomTargetCount;
            return Mathf.Max(1, Mathf.RoundToInt(count));
        }
    }
}
