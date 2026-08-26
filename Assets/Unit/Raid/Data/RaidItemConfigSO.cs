using System;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Data
{
    public enum RaidItemType
    {
        Attack = 0,
        AttackSpeed = 1,
        Heal = 2
    }

    [Serializable]
    public sealed class RaidItemDefinition
    {
        [Header("표시")]
        [Tooltip("전장 타일 위에 아이템이 존재하는 동안 표시할 프리팹입니다.")]
        [SerializeField] private GameObject visualPrefab;
        [Tooltip("캐릭터가 아이템을 획득하는 순간 짧게 재생할 프리팹입니다.")]
        [SerializeField] private GameObject consumeVisualPrefab;
        [Tooltip("획득 순간 VFX의 재생 시간을 제어합니다.")]
        [Min(0.05f)] [SerializeField] private float consumeVisualLifetime = 0.65f;
        [Tooltip("지속형 버프가 활성화된 동안 캐릭터에 표시할 프리팹입니다.")]
        [SerializeField] private GameObject buffVisualPrefab;

        [Header("효과")]
        [Min(0f)] [SerializeField] private float effectPercent;
        [Tooltip("버프의 기본 지속시간입니다. 실제 P1/P2 지속시간은 아래 Phase 배율을 곱해 계산합니다.")]
        [Min(0f)] [SerializeField] private float durationSeconds = 30f;

        public GameObject VisualPrefab => visualPrefab;
        public GameObject ConsumeVisualPrefab => consumeVisualPrefab;
        public float ConsumeVisualLifetime => Mathf.Max(0.05f, consumeVisualLifetime);
        public GameObject BuffVisualPrefab => buffVisualPrefab;
        public float EffectPercent => Mathf.Max(0f, effectPercent);
        public float DurationSeconds => Mathf.Max(0f, durationSeconds);
    }

    [Serializable]
    public sealed class RaidItemPhaseTuning
    {
        [Tooltip("해당 Phase에서 몬스터 한 마리가 사망할 때 아이템 드롭을 시도할 확률입니다.")]
        [Range(0f, 1f)] [SerializeField] private float dropChance = 0.2f;
        [Tooltip("획득되지 않은 아이템이 전장에 기본으로 남아 있는 시간입니다. Auto가 Cost를 모아 예약한 아이템은 필요한 만큼 연장될 수 있습니다.")]
        [Min(1f)] [SerializeField] private float activeLifetimeSeconds = 30f;
        [Tooltip("각 아이템 Definition의 기본 지속시간에 곱하는 Phase 배율입니다. 예: 기본 30초 × 1.5 = 45초.")]
        [Min(0.1f)] [SerializeField] private float buffDurationMultiplier = 1f;

        public float DropChance => Mathf.Clamp01(dropChance);
        public float ActiveLifetimeSeconds => Mathf.Max(1f, activeLifetimeSeconds);
        public float BuffDurationMultiplier => Mathf.Max(0.1f, buffDurationMultiplier);

        public RaidItemPhaseTuning()
        {
        }

        public RaidItemPhaseTuning(float dropChance, float activeLifetimeSeconds, float buffDurationMultiplier)
        {
            this.dropChance = dropChance;
            this.activeLifetimeSeconds = activeLifetimeSeconds;
            this.buffDurationMultiplier = buffDurationMultiplier;
        }

        public void Validate()
        {
            dropChance = Mathf.Clamp01(dropChance);
            activeLifetimeSeconds = Mathf.Max(1f, activeLifetimeSeconds);
            buffDurationMultiplier = Mathf.Max(0.1f, buffDurationMultiplier);
        }
    }

    [CreateAssetMenu(fileName = "RaidItemConfig", menuName = "EndlessGuard/Raid/Item Config")]
    public sealed class RaidItemConfigSO : ScriptableObject
    {
        [Header("Phase 1 아이템")]
        [Tooltip("P1은 몬스터 밀도가 낮으므로 드롭률과 버프 유지시간을 상대적으로 높여 같은 종류 버프를 이어갈 기회를 확보합니다.")]
        [SerializeField] private RaidItemPhaseTuning phase1 = new RaidItemPhaseTuning(0.28f, 35f, 1.5f);

        [Header("Phase 2 아이템")]
        [Tooltip("P2는 몬스터 밀도가 높으므로 드롭률을 낮추고 유지시간을 줄여 전체 아이템 발생량이 P1과 비슷한 수준이 되게 합니다.")]
        [SerializeField] private RaidItemPhaseTuning phase2 = new RaidItemPhaseTuning(0.16f, 25f, 1.1666666f);

        [Header("공통 드롭")]
        [Tooltip("전장에 동시에 존재할 수 있는 미획득 아이템 수입니다.")]
        [Range(1, 8)] [SerializeField] private int maxActiveItems = 4;
        [Tooltip("Auto가 아이템을 먹기 위해 Cost를 모을 때 예상 Cost 도달시간 뒤에 추가로 보장할 여유시간입니다.")]
        [Min(0f)] [SerializeField] private float reservationGraceSeconds = 5f;
        [Tooltip("타일 표면 위에서 Item VFX 루트에 더할 높이입니다.")]
        [SerializeField] private float visualHeightOffset = 0.025f;

        [Header("전역 지속 버프")]
        [Tooltip("공격력/공격속도/회복 버프의 최대 중첩 수입니다. 같은 버프를 다시 획득하면 중첩이 1 증가하고 지속시간은 해당 Phase 최대치로 초기화됩니다.")]
        [Range(1, 10)] [SerializeField] private int maxBuffStacks = 10;

        [Header("공격력 - 획득 시점 필드")]
        [Tooltip("아이템을 획득한 순간 필드에 배치되어 살아 있는 일반 캐릭터에게 현재 중첩 수가 적용됩니다. 버프 도중 새로 배치된 캐릭터는 다음 동일 버프 획득 전까지 적용되지 않습니다.")]
        [SerializeField] private RaidItemDefinition attack = new RaidItemDefinition();

        [Header("공격속도 - 획득 시점 필드")]
        [Tooltip("Effect Percent는 스택당 기본 공격 횟수 증가율(%)입니다. 캐릭터의 기존 공격 주기(Attacks Per Second)는 변경하지 않고, 한 번의 기본 공격 기회에서 발생하는 타격 횟수를 늘립니다. 아이템 획득 순간 필드 캐릭터만 적용되며 이후 새 배치 캐릭터는 다음 동일 버프 획득 때 편입됩니다.")]
        [SerializeField] private RaidItemDefinition attackSpeed = new RaidItemDefinition();

        [Header("회복 - 획득 시점 필드 지속 회복")]
        [Tooltip("HEAL의 Effect Percent는 스택당 초당 최대 HP 회복률(%), 기본 Duration Seconds는 Phase 배율 적용 전 기준시간입니다.")]
        [SerializeField] private RaidItemDefinition heal = new RaidItemDefinition();

        public int MaxActiveItems => Mathf.Clamp(maxActiveItems, 1, 8);
        public float ReservationGraceSeconds => Mathf.Max(0f, reservationGraceSeconds);
        public float VisualHeightOffset => visualHeightOffset;
        public int MaxBuffStacks => Mathf.Clamp(maxBuffStacks, 1, 10);

        public float GetDropChance(RaidPhase phase)
        {
            RaidItemPhaseTuning tuning = GetPhaseTuning(phase);
            return tuning != null ? tuning.DropChance : 0f;
        }

        public float GetActiveLifetimeSeconds(RaidPhase phase)
        {
            RaidItemPhaseTuning tuning = GetPhaseTuning(phase);
            return tuning != null ? tuning.ActiveLifetimeSeconds : 0f;
        }

        public RaidItemDefinition GetDefinition(RaidItemType type)
        {
            switch (type)
            {
                case RaidItemType.Attack:
                    return attack;
                case RaidItemType.AttackSpeed:
                    return attackSpeed;
                case RaidItemType.Heal:
                    return heal;
                default:
                    return null;
            }
        }

        public float GetBuffEffectPercent(RaidItemType type)
        {
            RaidItemDefinition definition = GetDefinition(type);
            return definition != null ? definition.EffectPercent : 0f;
        }

        public float GetBuffDurationSeconds(RaidItemType type, RaidPhase phase)
        {
            RaidItemDefinition definition = GetDefinition(type);
            if (definition == null)
            {
                return 0f;
            }

            RaidItemPhaseTuning tuning = GetPhaseTuning(phase);
            float multiplier = tuning != null ? tuning.BuffDurationMultiplier : 1f;
            return definition.DurationSeconds * multiplier;
        }

        private RaidItemPhaseTuning GetPhaseTuning(RaidPhase phase)
        {
            switch (phase)
            {
                case RaidPhase.Phase1:
                    return phase1;
                case RaidPhase.Phase2:
                    return phase2;
                default:
                    return null;
            }
        }

        private void OnValidate()
        {
            if (phase1 == null)
            {
                phase1 = new RaidItemPhaseTuning(0.28f, 35f, 1.5f);
            }

            if (phase2 == null)
            {
                phase2 = new RaidItemPhaseTuning(0.16f, 25f, 1.1666666f);
            }

            phase1.Validate();
            phase2.Validate();
            maxActiveItems = Mathf.Clamp(maxActiveItems, 1, 8);
            reservationGraceSeconds = Mathf.Max(0f, reservationGraceSeconds);
            maxBuffStacks = Mathf.Clamp(maxBuffStacks, 1, 10);
        }
    }
}
