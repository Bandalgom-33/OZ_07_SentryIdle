using System;
using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [Serializable]
    public sealed class PassiveRef
    {
        [Tooltip("이 참조가 패시브에서 어떤 용도로 사용되는지 나타냅니다.")]
        [SerializeField] private PassiveRefKey key = PassiveRefKey.None;

        [Tooltip("이 캐릭터 또는 몬스터가 해당 패시브에서 실제로 사용할 에셋 참조입니다.")]
        [SerializeField] private UnityEngine.Object reference;

        public PassiveRefKey Key => key;
        public UnityEngine.Object Reference => reference;

        public T GetReference<T>() where T : UnityEngine.Object
        {
            return reference as T;
        }
    }
}