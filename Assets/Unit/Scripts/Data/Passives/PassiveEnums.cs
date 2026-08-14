using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    public enum PassiveUserType
    {
        [InspectorName("미설정")]
        None = 0,

        [InspectorName("캐릭터 전용")]
        Unit = 1,

        [InspectorName("몬스터 전용")]
        Enemy = 2,

        [InspectorName("캐릭터·몬스터 공용")]
        Both = 3
    }
}