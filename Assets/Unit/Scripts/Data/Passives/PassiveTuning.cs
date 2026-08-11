using System;
using System.Collections.Generic;
using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [Serializable]
    public sealed class PassiveTuning
    {
        [Tooltip("개별 수치와 참조를 적용할 패시브 정의입니다.")]
        [SerializeField] private PassiveDataSO passive;

        [Tooltip("이 캐릭터 또는 몬스터가 해당 패시브에서 실제로 사용할 개별 숫자 수치 목록입니다.")]
        [SerializeField] private List<PassiveValue> values = new List<PassiveValue>();

        [Tooltip("이 캐릭터 또는 몬스터가 해당 패시브에서 실제로 사용할 개별 에셋 참조 목록입니다.")]
        [SerializeField] private List<PassiveRef> refs = new List<PassiveRef>();

        public PassiveDataSO Passive => passive;
        public IReadOnlyList<PassiveValue> Values => values;
        public IReadOnlyList<PassiveRef> Refs => refs;

        public bool TryGetOverride(PassiveValueKey key, out float value)
        {
            value = 0f;

            if (key == PassiveValueKey.None || values == null)
            {
                return false;
            }

            for (int i = 0; i < values.Count; i++)
            {
                PassiveValue passiveValue = values[i];

                if (passiveValue == null || passiveValue.Key != key)
                {
                    continue;
                }

                value = passiveValue.Value;
                return true;
            }

            return false;
        }

        public float GetValue(PassiveValueKey key)
        {
            if (TryGetOverride(key, out float overrideValue))
            {
                return overrideValue;
            }

            if (passive != null && passive.TryGetDefaultValue(key, out float defaultValue))
            {
                return defaultValue;
            }

            return 0f;
        }

        public bool TryGetReferenceOverride<T>(PassiveRefKey key, out T reference) where T : UnityEngine.Object
        {
            reference = null;

            if (key == PassiveRefKey.None || refs == null)
            {
                return false;
            }

            for (int i = 0; i < refs.Count; i++)
            {
                PassiveRef passiveRef = refs[i];

                if (passiveRef == null || passiveRef.Key != key)
                {
                    continue;
                }

                reference = passiveRef.GetReference<T>();
                return reference != null;
            }

            return false;
        }

        public bool TryGetReference<T>(PassiveRefKey key, out T reference) where T : UnityEngine.Object
        {
            if (TryGetReferenceOverride(key, out reference))
            {
                return true;
            }

            if (passive != null && passive.TryGetDefaultReference(key, out UnityEngine.Object defaultReference))
            {
                reference = defaultReference as T;
                return reference != null;
            }

            reference = null;
            return false;
        }

        public T GetReference<T>(PassiveRefKey key) where T : UnityEngine.Object
        {
            return TryGetReference(key, out T reference) ? reference : null;
        }
    }
}