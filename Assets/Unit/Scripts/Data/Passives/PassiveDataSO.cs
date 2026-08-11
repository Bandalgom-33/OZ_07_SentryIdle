using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    public abstract class PassiveDataSO : ScriptableObject
    {
        [Header("패시브 기본 정보")]
        [Tooltip("캐릭터·몬스터 데이터와 제작 도구에 표시되는 패시브 이름입니다.")]
        [SerializeField] private string displayName;

        [Tooltip("패시브의 발동 조건과 효과를 설명하는 내용입니다.")]
        [TextArea(2, 5)]
        [SerializeField] private string description;

        [Tooltip("이 패시브를 캐릭터, 몬스터 또는 양쪽 모두가 사용할 수 있는지 설정합니다.")]
        [SerializeField] private PassiveUserType usableBy = PassiveUserType.Both;

        [Header("패시브 선택 풀")]
        [Tooltip("캐릭터는 상위 직군, 몬스터는 크기를 기준으로 이 패시브를 선택할 수 있는 범위를 설정합니다.")]
        [SerializeField] private PassiveCompatibility compatibility = new PassiveCompatibility();

        public string DisplayName => displayName;
        public string Description => description;
        public PassiveUserType UsableBy => usableBy;
        public PassiveCompatibility Compatibility => compatibility;

        public bool CanBeUsedBy(PassiveUserType userType)
        {
            if (userType == PassiveUserType.None || usableBy == PassiveUserType.None)
            {
                return false;
            }

            return usableBy == PassiveUserType.Both || usableBy == userType;
        }

        public bool CanBeUsedByUnit(UnitClass unitClass)
        {
            if (!CanBeUsedBy(PassiveUserType.Unit) || compatibility == null)
            {
                return false;
            }

            return compatibility.IsUnitAllowed(unitClass);
        }

        public bool CanBeUsedByUnit(UnitClass unitClass, UnitSubclass subclass)
        {
            return CanBeUsedByUnit(unitClass);
        }

        public bool CanBeUsedByEnemy(EnemySize size)
        {
            if (!CanBeUsedBy(PassiveUserType.Enemy) || compatibility == null)
            {
                return false;
            }

            return compatibility.IsEnemyAllowed(size);
        }

        public bool CanBeUsedByEnemy(EnemyCategory category, EnemyMovementType movementType, EnemySize size, EnemyRole role)
        {
            return CanBeUsedByEnemy(size);
        }

        public virtual bool TryGetDefaultValue(PassiveValueKey key, out float value)
        {
            value = 0f;
            return false;
        }

        public virtual bool TryGetDefaultReference(PassiveRefKey key, out UnityEngine.Object reference)
        {
            reference = null;
            return false;
        }
    }
}