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

        [Tooltip("몬스터의 전투 역할 정보입니다.")]
        [SerializeField] private EnemyRole role = EnemyRole.None;

        [Header("몬스터 전투 규칙")]
        [Tooltip("저지된 캐릭터만 공격할지, 저지되지 않아도 공격 범위 안의 캐릭터를 공격할지 설정합니다.")]
        [SerializeField] private EnemyAttackRule attackRule = EnemyAttackRule.BlockedOnly;

        [Header("범위 공격 반복")]
        [Tooltip("지상 범위 공격 몬스터가 한 번 멈춘 뒤 공격을 유지하는 시간입니다. 0이면 기존처럼 대상이 범위에 있는 동안 계속 멈춰 공격합니다.")]
        [Min(0f)]
        [SerializeField] private float inRangeFireDuration;

        [Tooltip("집중 공격이 끝난 뒤 다시 공격하기 전까지 강제로 전진하는 시간입니다. 0이면 범위 공격 반복 기능을 사용하지 않습니다.")]
        [Min(0f)]
        [SerializeField] private float inRangeAdvanceDuration;

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

        [Header("패시브 개별 수치")]
        [Tooltip("선택한 패시브의 숫자 수치를 이 몬스터 전용으로 저장합니다. 패시브 기능은 공유하지만 이 목록의 수치는 몬스터마다 다르게 설정할 수 있습니다.")]
        [SerializeField] private List<PassiveTuning> passiveTunings = new List<PassiveTuning>();

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
        public float InRangeFireDuration => inRangeFireDuration;
        public float InRangeAdvanceDuration => inRangeAdvanceDuration;
        public bool UsesInRangeAttackCycle => attackRule == EnemyAttackRule.InRange && movementType == EnemyMovementType.Ground && inRangeFireDuration > 0f && inRangeAdvanceDuration > 0f;
        public CombatStats BaseStats => baseStats;
        public AttackSettings AttackSettings => attackSettings;
        public int RewardExp => rewardExp;
        public int RewardGold => rewardGold;
        public IReadOnlyList<PassiveDataSO> Passives => passives;
        public IReadOnlyList<PassiveTuning> PassiveTunings => passiveTunings;
        public GameObject EnemyPrefab => enemyPrefab;

        public PassiveTuning GetPassiveTuning(PassiveDataSO passive)
        {
            if (passive == null || passiveTunings == null)
            {
                return null;
            }

            for (int i = 0; i < passiveTunings.Count; i++)
            {
                PassiveTuning tuning = passiveTunings[i];

                if (tuning != null && tuning.Passive == passive)
                {
                    return tuning;
                }
            }

            return null;
        }
    }
}