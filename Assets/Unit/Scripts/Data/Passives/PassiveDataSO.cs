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

        [Header("패시브 호환 조건")]
        [Tooltip("캐릭터와 몬스터의 분류에 따라 이 패시브를 사용할 수 있는지 검사하는 조건입니다. 비어 있는 제한 목록은 해당 조건을 제한하지 않습니다.")]
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

        public bool CanBeUsedByUnit(UnitClass unitClass, UnitSubclass subclass)
        {
            if (!CanBeUsedBy(PassiveUserType.Unit) || compatibility == null)
            {
                return false;
            }

            return compatibility.IsUnitAllowed(unitClass, subclass);
        }

        public bool CanBeUsedByEnemy(EnemyCategory category, EnemyMovementType movementType, EnemySize size, EnemyRole role)
        {
            if (!CanBeUsedBy(PassiveUserType.Enemy) || compatibility == null)
            {
                return false;
            }

            return compatibility.IsEnemyAllowed(category, movementType, size, role);
        }
    }
}