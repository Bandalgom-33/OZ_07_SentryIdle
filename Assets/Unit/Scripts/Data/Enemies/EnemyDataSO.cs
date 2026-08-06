using System.Collections.Generic;
using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [CreateAssetMenu(fileName = "EnemyData", menuName = "Endless Guard/Enemy/Enemy Data")]
    public sealed class EnemyDataSO : ScriptableObject
    {
        [Header("데이터 식별 정보")]
        [Tooltip("제작 도구에서 ENEMY_0001 형식으로 자동 발급하는 고유 ID입니다. 생성 후 직접 변경하지 않습니다.")]
        [SerializeField] private string enemyId;

        [Tooltip("게임 화면과 제작 도구에 표시되는 몬스터 이름입니다.")]
        [SerializeField] private string displayName;

        [Tooltip("몬스터의 특징과 전투 역할을 설명하는 내용입니다.")]
        [TextArea(2, 5)]
        [SerializeField] private string description;

        [Header("몬스터 분류")]
        [Tooltip("몬스터가 일반, 엘리트 또는 보스 중 어느 분류인지 설정합니다.")]
        [SerializeField] private EnemyCategory category = EnemyCategory.None;

        [Tooltip("몬스터가 지상 또는 공중 중 어떤 방식으로 이동하는지 설정합니다.")]
        [SerializeField] private EnemyMovementType movementType = EnemyMovementType.None;

        [Tooltip("패시브와 전투 조건 판정에 사용하는 몬스터 크기 분류입니다.")]
        [SerializeField] private EnemySize size = EnemySize.None;

        [Tooltip("몬스터가 공격형 또는 서포터 중 어느 전투 역할인지 설정합니다.")]
        [SerializeField] private EnemyRole role = EnemyRole.None;

        [Header("몬스터 전투 규칙")]
        [Tooltip("저지된 캐릭터만 공격할지, 저지되지 않아도 공격 범위 안의 캐릭터를 공격할지 설정합니다.")]
        [SerializeField] private EnemyAttackRule attackRule = EnemyAttackRule.BlockedOnly;

        [Header("공통 기본 전투 능력치")]
        [Tooltip("패시브, 상태이상과 전투 중 효과가 적용되기 전 몬스터의 기준 전투 능력치입니다.")]
        [SerializeField] private CombatStats baseStats = new CombatStats();

        [Header("기본 공격 설정")]
        [Tooltip("몬스터의 기본 공격 방식, 피해 유형, 대상, 사거리와 동시 공격 대상 수를 설정합니다.")]
        [SerializeField] private AttackSettings attackSettings = new AttackSettings();

        [Header("처치 보상")]
        [Tooltip("이 몬스터가 사망했을 때 경험치 담당 시스템이 지급할 기준 경험치입니다.")]
        [Min(0)]
        [SerializeField] private int rewardExp;

        [Tooltip("이 몬스터가 사망했을 때 재화 담당 시스템이 지급할 기준 골드입니다.")]
        [Min(0)]
        [SerializeField] private int rewardGold;

        [Header("패시브 능력")]
        [Tooltip("이 몬스터가 사용하는 재사용 가능한 패시브 데이터 목록입니다.")]
        [SerializeField] private List<PassiveDataSO> passives = new List<PassiveDataSO>();

        [Header("몬스터 프리팹")]
        [Tooltip("이 데이터를 기준으로 생성되거나 연결된 몬스터 프리팹입니다.")]
        [SerializeField] private GameObject enemyPrefab;

        public string EnemyId => enemyId;
        public string DisplayName => displayName;
        public string Description => description;
        public EnemyCategory Category => category;
        public EnemyMovementType MovementType => movementType;
        public EnemySize Size => size;
        public EnemyRole Role => role;
        public EnemyAttackRule AttackRule => attackRule;
        public CombatStats BaseStats => baseStats;
        public AttackSettings AttackSettings => attackSettings;
        public int RewardExp => rewardExp;
        public int RewardGold => rewardGold;
        public IReadOnlyList<PassiveDataSO> Passives => passives;
        public GameObject EnemyPrefab => enemyPrefab;
    }
}