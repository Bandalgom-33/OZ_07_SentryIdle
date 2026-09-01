using System;
using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [Serializable]
    public sealed class UnitSkillSettings
    {
        [Header("SP 스킬 기본")]
        [Tooltip("이 캐릭터가 SP 스킬을 사용할지 설정합니다.")]
        [SerializeField] private bool enabled = true;

        [Tooltip("필요 SP가 모이고 공격 가능한 적이 있으면 자동으로 스킬을 사용합니다.")]
        [SerializeField] private bool autoCastWhenReady = true;

        [Tooltip("스킬 한 번에 소비하는 SP입니다. 0 이하이면 최대 스킬게이지를 전부 소비하는 것으로 처리합니다.")]
        [Min(0f)]
        [SerializeField] private float skillGaugeCost = 100f;

        [Header("대상·범위")]
        [Tooltip("단일 대상, 대표 대상을 중심으로 한 범위 공격, 맵 전체 공격 중 하나를 선택합니다. 단일/범위는 캐릭터의 기본 공격 타일 범위 안에서만 대상을 찾고, 맵 전체만 사거리 제한을 무시합니다.")]
        [SerializeField] private UnitSkillTargetScope targetScope = UnitSkillTargetScope.Single;

        [Tooltip("스킬이 공격할 수 있는 적의 층을 설정합니다.")]
        [SerializeField] private AttackTarget attackTarget = AttackTarget.GroundAndAir;

        [Tooltip("단일/범위 스킬의 대표 대상을 어떤 기준으로 고를지 설정합니다.")]
        [SerializeField] private UnitSkillTargetPriority targetPriority = UnitSkillTargetPriority.ClosestToGoal;

        [Tooltip("범위 공격일 때, 기본 공격 타일 범위 안에서 선택된 대표 대상의 타일을 중심으로 같이 적중할 주변 타일을 직접 선택합니다. 단일/맵전체에서는 사용하지 않습니다.")]
        [SerializeField] private SkillAreaTileData areaTileRange = new SkillAreaTileData();

        [Tooltip("범위 공격이 동시에 공격할 최대 적 수입니다. 0이면 범위 안의 모든 적을 공격합니다.")]
        [Min(0)]
        [SerializeField] private int areaTargetLimit;

        [Header("피해 계산")]
        [Tooltip("스킬 피해를 물리 피해 또는 마법 피해로 계산합니다.")]
        [SerializeField] private DamageType damageType = DamageType.Physical;

        [Tooltip("스킬 피해 계산의 기준이 되는 공격 능력치를 선택합니다.")]
        [SerializeField] private UnitSkillAttackPowerSource attackPowerSource = UnitSkillAttackPowerSource.PhysicalAttack;

        [Tooltip("기준 공격력에 곱할 스킬 계수입니다. 250은 공격력의 250%를 의미합니다.")]
        [Min(0f)]
        [SerializeField] private float attackPowerPercent = 250f;

        [Tooltip("공격력 계수 계산 뒤 추가하는 고정 피해량입니다.")]
        [Min(0f)]
        [SerializeField] private float flatDamage;

        [Tooltip("켜면 물리/마법 방어력을 공통 DamageRule로 계산합니다. 끄면 방어력을 무시합니다.")]
        [SerializeField] private bool applyDefense = true;

        [Tooltip("켜면 현재 캐릭터 패시브의 공격력/최종 피해 보정을 스킬에도 적용합니다.")]
        [SerializeField] private bool applyPassiveDamageModifiers = true;

        [Tooltip("켜면 각 스킬 타격마다 캐릭터의 현재 치명타 확률로 치명타를 판정합니다.")]
        [SerializeField] private bool canCritical;

        [Header("타격 방식")]
        [Tooltip("피해를 한 번에 적용할지, 여러 번 나누어 적용할지 설정합니다.")]
        [SerializeField] private UnitSkillHitMode hitMode = UnitSkillHitMode.SingleHit;

        [Tooltip("다단히트일 때 타격 횟수입니다.")]
        [Min(1)]
        [SerializeField] private int hitCount = 3;

        [Tooltip("다단히트 사이의 시간 간격입니다.")]
        [Min(0f)]
        [SerializeField] private float hitIntervalSeconds = 0.15f;

        [Tooltip("다단히트에서 설정한 총 계수를 타수로 나눌지, 매 타격마다 전체 계수를 적용할지 선택합니다.")]
        [SerializeField] private UnitSkillMultiHitDamageMode multiHitDamageMode = UnitSkillMultiHitDamageMode.SplitTotalPower;

        [Tooltip("스킬 발동 중 기본 공격을 잠시 막는 최소 시간입니다. 다단히트 지속시간보다 짧으면 실제 다단히트가 끝날 때까지 기본 공격을 막습니다.")]
        [Min(0f)]
        [SerializeField] private float castLockSeconds = 0.45f;

        [Header("스킬 VFX")]
        [Tooltip("스킬 타격에 사용할 VFX Prefab입니다. 캐릭터별로 이 참조만 바꾸면 다른 VFX를 사용할 수 있습니다.")]
        [SerializeField] private GameObject vfxPrefab;

        [Tooltip("VFX를 맞는 적마다, 대표 대상 한 곳, 또는 시전자 위치 중 어디에 출력할지 설정합니다.")]
        [SerializeField] private UnitSkillVfxSpawnMode vfxSpawnMode = UnitSkillVfxSpawnMode.EachTarget;

        [Tooltip("VFX 위치에 더할 월드 오프셋입니다.")]
        [SerializeField] private Vector3 vfxOffset;

        [Tooltip("VFX 크기 배율입니다.")]
        [Min(0.01f)]
        [SerializeField] private float vfxScale = 0.55f;

        [Tooltip("다단히트일 때 매 타격마다 VFX를 재생할지 설정합니다. 끄면 첫 타격에만 재생합니다.")]
        [SerializeField] private bool playVfxEveryHit = true;

        public bool Enabled => enabled;
        public bool AutoCastWhenReady => autoCastWhenReady;
        public float SkillGaugeCost => Mathf.Max(0f, skillGaugeCost);
        public UnitSkillTargetScope TargetScope => targetScope;
        public AttackTarget AttackTarget => attackTarget;
        public UnitSkillTargetPriority TargetPriority => targetPriority;
        public SkillAreaTileData AreaTileRange => areaTileRange;
        public int AreaTargetLimit => Mathf.Max(0, areaTargetLimit);
        public DamageType DamageType => damageType;
        public UnitSkillAttackPowerSource AttackPowerSource => attackPowerSource;
        public float AttackPowerPercent => Mathf.Max(0f, attackPowerPercent);
        public float FlatDamage => Mathf.Max(0f, flatDamage);
        public bool ApplyDefense => applyDefense;
        public bool ApplyPassiveDamageModifiers => applyPassiveDamageModifiers;
        public bool CanCritical => canCritical;
        public UnitSkillHitMode HitMode => hitMode;
        public int HitCount => hitMode == UnitSkillHitMode.MultiHit ? Mathf.Max(1, hitCount) : 1;
        public float HitIntervalSeconds => Mathf.Max(0f, hitIntervalSeconds);
        public UnitSkillMultiHitDamageMode MultiHitDamageMode => multiHitDamageMode;
        public float CastLockSeconds => Mathf.Max(0f, castLockSeconds);
        public GameObject VfxPrefab => vfxPrefab;
        public UnitSkillVfxSpawnMode VfxSpawnMode => vfxSpawnMode;
        public Vector3 VfxOffset => vfxOffset;
        public float VfxScale => Mathf.Max(0.01f, vfxScale);
        public bool PlayVfxEveryHit => playVfxEveryHit;

        public float ResolveGaugeCost(float maxSkillGauge)
        {
            if (skillGaugeCost > 0f)
            {
                return skillGaugeCost;
            }

            return Mathf.Max(0f, maxSkillGauge);
        }

        public float ResolvePerHitPowerPercent()
        {
            int resolvedHitCount = HitCount;
            if (resolvedHitCount <= 1 || multiHitDamageMode == UnitSkillMultiHitDamageMode.FullPowerEachHit)
            {
                return AttackPowerPercent;
            }

            return AttackPowerPercent / resolvedHitCount;
        }

        public float ResolvePerHitFlatDamage()
        {
            int resolvedHitCount = HitCount;
            if (resolvedHitCount <= 1 || multiHitDamageMode == UnitSkillMultiHitDamageMode.FullPowerEachHit)
            {
                return FlatDamage;
            }

            return FlatDamage / resolvedHitCount;
        }
    }
}
