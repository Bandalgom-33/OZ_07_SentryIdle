using System.Collections.Generic;
using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [CreateAssetMenu(fileName = "UnitData", menuName = "Endless Guard/Unit/Unit Data")]
    public sealed class UnitDataSO : ScriptableObject
    {
        [Header("데이터 식별 정보")]
        [Tooltip("제작 도구에서 UNIT_0001 형식으로 자동 발급하는 고유 ID입니다. 생성 후 직접 변경하지 않습니다.")]
        [SerializeField] private string unitId;

        [Tooltip("게임 화면과 제작 도구에 표시되는 캐릭터 이름입니다.")]
        [SerializeField] private string displayName;

        [Tooltip("캐릭터의 역할과 특징을 설명하는 내용입니다.")]
        [TextArea(2, 5)]
        [SerializeField] private string description;

        [Tooltip("캐릭터의 1성부터 6성까지의 성급을 설정합니다.")]
        [SerializeField] private UnitGrade grade = UnitGrade.None;

        [Tooltip("새로운 진행 데이터를 만들 때 이 캐릭터가 시작하는 레벨입니다. 실제 현재 레벨과 경험치는 별도의 진행도 데이터에서 관리합니다.")]
        [Min(1)]
        [SerializeField] private int initialLevel = 1;

        [Header("성장 데이터")]
        [Tooltip("이 캐릭터의 상위 분류에 맞는 레벨업/승급 성장 규칙을 조회할 공통 성장 테이블입니다. 같은 테이블을 모든 캐릭터가 공유합니다.")]
        [SerializeField] private UnitClassGrowthTableSO growthTable;

        [Header("캐릭터 분류")]
        [Tooltip("캐릭터의 상위 분류를 설정합니다.")]
        [SerializeField] private UnitClass unitClass = UnitClass.None;

        [Tooltip("캐릭터의 세부 분류를 설정합니다. 세부 분류는 패시브를 자동으로 결정하지 않습니다.")]
        [SerializeField] private UnitSubclass subclass = UnitSubclass.None;

        [Header("소환·배치 설정")]
        [Tooltip("캐릭터를 배치할 수 있는 위치를 설정합니다.")]
        [SerializeField] private UnitPlacement placement = UnitPlacement.None;

        [Tooltip("캐릭터를 필드에 소환할 때 필요한 기준 코스트입니다. 공통 성장과 패시브가 적용되기 전 값입니다.")]
        [Min(0)]
        [SerializeField] private int summonCost;

        [Tooltip("캐릭터가 사망하거나 퇴장한 뒤 다시 소환할 수 있을 때까지의 기준 시간입니다. 단위는 초입니다.")]
        [Min(0f)]
        [SerializeField] private float redeployTime;

        [Tooltip("이 캐릭터가 동시에 이동을 막을 수 있는 지상 몬스터의 최대 수입니다.")]
        [Min(0)]
        [SerializeField] private int blockCount;

        [Header("공통 기본 전투 능력치")]
        [Tooltip("레벨 성장, 공통 성장, 패시브와 버프가 적용되기 전 기준 전투 능력치입니다.")]
        [SerializeField] private CombatStats baseStats = new CombatStats();

        [Header("기본 공격 설정")]
        [Tooltip("캐릭터의 기본 공격 방식, 대상, 사거리와 동시 공격 대상 수를 설정합니다.")]
        [SerializeField] private AttackSettings attackSettings = new AttackSettings();

        [Header("캐릭터 전용 능력치")]
        [Tooltip("전투 중 매초 회복하는 기준 HP입니다.")]
        [Min(0f)]
        [SerializeField] private float hpRegenPerSecond;

        [Tooltip("기본 치명타 확률입니다. 퍼센트 단위로 입력하며 25는 25%를 의미합니다.")]
        [Min(0f)]
        [SerializeField] private float criticalChancePercent;

        [Tooltip("치명타 발생 시 추가되는 피해 비율입니다. 퍼센트 단위로 입력하며 50은 기본 피해에 50%가 추가됨을 의미합니다.")]
        [Min(0f)]
        [SerializeField] private float criticalDamageBonusPercent;

        [Header("스킬게이지 설정")]
        [Tooltip("캐릭터가 보유할 수 있는 최대 스킬게이지입니다.")]
        [Min(0f)]
        [SerializeField] private float maxSkillGauge;

        [Tooltip("전투 중 매초 자연 회복하는 스킬게이지입니다.")]
        [Min(0f)]
        [SerializeField] private float skillGaugeRegenPerSecond;

        [Tooltip("기본 공격을 한 번 완료할 때 획득하는 스킬게이지입니다.")]
        [Min(0f)]
        [SerializeField] private float skillGaugePerAttack;

        [Header("패시브 능력")]
        [Tooltip("이 캐릭터가 사용하는 재사용 가능한 패시브 데이터 목록입니다.")]
        [SerializeField] private List<PassiveDataSO> passives = new List<PassiveDataSO>();

        [Header("패시브 개별 수치")]
        [Tooltip("선택한 패시브의 숫자 수치를 이 캐릭터 전용으로 저장합니다. 패시브 기능은 공유하지만 이 목록의 수치는 캐릭터마다 다르게 설정할 수 있습니다.")]
        [SerializeField] private List<PassiveTuning> passiveTunings = new List<PassiveTuning>();

        [Header("캐릭터 프리팹")]
        [Tooltip("이 데이터를 기준으로 생성되거나 연결된 캐릭터 프리팹입니다.")]
        [SerializeField] private GameObject unitPrefab;

        public string UnitId => unitId;
        public string DisplayName => displayName;
        public string Description => description;
        public UnitGrade Grade => grade;
        public int InitialLevel => initialLevel;
        public UnitClassGrowthTableSO GrowthTable => growthTable;
        public UnitClass Class => unitClass;
        public UnitSubclass Subclass => subclass;
        public UnitPlacement Placement => placement;
        public int SummonCost => summonCost;
        public float RedeployTime => redeployTime;
        public int BlockCount => blockCount;
        public CombatStats BaseStats => baseStats;
        public AttackSettings AttackSettings => attackSettings;
        public float HpRegenPerSecond => hpRegenPerSecond;
        public float CriticalChancePercent => criticalChancePercent;
        public float CriticalDamageBonusPercent => criticalDamageBonusPercent;
        public float MaxSkillGauge => maxSkillGauge;
        public float SkillGaugeRegenPerSecond => skillGaugeRegenPerSecond;
        public float SkillGaugePerAttack => skillGaugePerAttack;
        public IReadOnlyList<PassiveDataSO> Passives => passives;
        public IReadOnlyList<PassiveTuning> PassiveTunings => passiveTunings;
        public GameObject UnitPrefab => unitPrefab;

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