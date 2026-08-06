using System;
using System.Collections.Generic;
using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [Serializable]
    public sealed class PassiveCompatibility
    {
        [Header("캐릭터 분류 제한")]
        [Tooltip("비어 있으면 모든 캐릭터 상위 분류에서 사용할 수 있습니다.")]
        [SerializeField] private List<UnitClass> allowedUnitClasses = new List<UnitClass>();

        [Tooltip("비어 있으면 모든 캐릭터 세부 분류에서 사용할 수 있습니다.")]
        [SerializeField] private List<UnitSubclass> allowedUnitSubclasses = new List<UnitSubclass>();

        [Header("몬스터 분류 제한")]
        [Tooltip("비어 있으면 일반, 엘리트와 보스 모두 사용할 수 있습니다.")]
        [SerializeField] private List<EnemyCategory> allowedEnemyCategories = new List<EnemyCategory>();

        [Tooltip("비어 있으면 지상과 공중 몬스터 모두 사용할 수 있습니다.")]
        [SerializeField] private List<EnemyMovementType> allowedEnemyMovementTypes = new List<EnemyMovementType>();

        [Tooltip("비어 있으면 모든 몬스터 크기에서 사용할 수 있습니다.")]
        [SerializeField] private List<EnemySize> allowedEnemySizes = new List<EnemySize>();

        [Tooltip("비어 있으면 모든 몬스터 전투 역할에서 사용할 수 있습니다.")]
        [SerializeField] private List<EnemyRole> allowedEnemyRoles = new List<EnemyRole>();

        public IReadOnlyList<UnitClass> AllowedUnitClasses => allowedUnitClasses;
        public IReadOnlyList<UnitSubclass> AllowedUnitSubclasses => allowedUnitSubclasses;
        public IReadOnlyList<EnemyCategory> AllowedEnemyCategories => allowedEnemyCategories;
        public IReadOnlyList<EnemyMovementType> AllowedEnemyMovementTypes => allowedEnemyMovementTypes;
        public IReadOnlyList<EnemySize> AllowedEnemySizes => allowedEnemySizes;
        public IReadOnlyList<EnemyRole> AllowedEnemyRoles => allowedEnemyRoles;

        public bool IsUnitAllowed(UnitClass unitClass, UnitSubclass subclass)
        {
            if (unitClass == UnitClass.None || subclass == UnitSubclass.None)
            {
                return false;
            }

            if (!UnitClassRules.IsSubclassAllowed(unitClass, subclass))
            {
                return false;
            }

            return IsAllowed(allowedUnitClasses, unitClass) && IsAllowed(allowedUnitSubclasses, subclass);
        }

        public bool IsEnemyAllowed(EnemyCategory category, EnemyMovementType movementType, EnemySize size, EnemyRole role)
        {
            if (category == EnemyCategory.None || movementType == EnemyMovementType.None || size == EnemySize.None || role == EnemyRole.None)
            {
                return false;
            }

            return IsAllowed(allowedEnemyCategories, category)
                && IsAllowed(allowedEnemyMovementTypes, movementType)
                && IsAllowed(allowedEnemySizes, size)
                && IsAllowed(allowedEnemyRoles, role);
        }

        private static bool IsAllowed<T>(List<T> restrictions, T value)
        {
            if (restrictions == null || restrictions.Count == 0)
            {
                return true;
            }

            EqualityComparer<T> comparer = EqualityComparer<T>.Default;

            for (int i = 0; i < restrictions.Count; i++)
            {
                if (comparer.Equals(restrictions[i], value))
                {
                    return true;
                }
            }

            return false;
        }
    }
}