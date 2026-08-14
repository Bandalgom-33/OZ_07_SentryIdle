using System;
using System.Collections.Generic;
using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [Serializable]
    public sealed class PassiveCompatibility
    {
        [Header("캐릭터 패시브 풀")]
        [Tooltip("이 패시브가 속하는 캐릭터 상위 직군 풀입니다. 스페셜리스트는 이 설정과 관계없이 모든 캐릭터 패시브를 선택할 수 있습니다. 비어 있으면 일반 직군에서는 선택할 수 없고 스페셜리스트만 사용할 수 있습니다.")]
        [SerializeField] private List<UnitClass> allowedUnitClasses = new List<UnitClass>();

        [Header("몬스터 패시브 풀")]
        [Tooltip("이 패시브가 속하는 몬스터 크기 풀입니다. 소형, 중형, 대형 중 하나 이상을 명시적으로 설정합니다. 비어 있으면 몬스터가 이 패시브를 선택할 수 없습니다.")]
        [SerializeField] private List<EnemySize> allowedEnemySizes = new List<EnemySize>();

        public IReadOnlyList<UnitClass> AllowedUnitClasses => allowedUnitClasses;

        public IReadOnlyList<EnemySize> AllowedEnemySizes => allowedEnemySizes;

        public bool IsUnitAllowed(UnitClass unitClass)
        {
            if (unitClass == UnitClass.None)
            {
                return false;
            }

            if (unitClass == UnitClass.Specialist)
            {
                return true;
            }

            return IsExplicitlyAllowed(allowedUnitClasses, unitClass);
        }

        public bool IsUnitAllowed(UnitClass unitClass, UnitSubclass subclass)
        {
            return IsUnitAllowed(unitClass);
        }

        public bool IsEnemyAllowed(EnemySize size)
        {
            if (size == EnemySize.None)
            {
                return false;
            }

            return IsExplicitlyAllowed(allowedEnemySizes, size);
        }

        public bool IsEnemyAllowed(EnemyCategory category, EnemyMovementType movementType, EnemySize size, EnemyRole role)
        {
            return IsEnemyAllowed(size);
        }

        private static bool IsExplicitlyAllowed<T>(List<T> allowedValues, T value)
        {
            if (allowedValues == null || allowedValues.Count == 0)
            {
                return false;
            }

            EqualityComparer<T> comparer = EqualityComparer<T>.Default;

            for (int i = 0; i < allowedValues.Count; i++)
            {
                if (comparer.Equals(allowedValues[i], value))
                {
                    return true;
                }
            }

            return false;
        }
    }
}