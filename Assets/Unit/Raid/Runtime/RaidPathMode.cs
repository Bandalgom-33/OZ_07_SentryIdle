using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    public enum RaidPathMode
    {
        [InspectorName("순차 분산")]
        RoundRobin = 0,

        [InspectorName("무작위")]
        Random = 1
    }
}