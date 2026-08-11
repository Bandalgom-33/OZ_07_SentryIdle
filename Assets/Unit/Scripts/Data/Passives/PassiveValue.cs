using System;
using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [Serializable]
    public sealed class PassiveValue
    {
        [Tooltip("이 수치가 패시브에서 어떤 의미로 사용되는지 나타냅니다.")]
        [SerializeField] private PassiveValueKey key = PassiveValueKey.None;

        [Tooltip("이 캐릭터 또는 몬스터가 실제로 사용할 패시브 수치입니다.")]
        [SerializeField] private float value;

        public PassiveValueKey Key => key;

        public float Value => value;
    }
}