using System;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Data
{
    [CreateAssetMenu(fileName = "RaidMapFamily", menuName = "Endless Guard/Raid/Map Family")]
    public sealed class RaidMapFamilySO : ScriptableObject
    {
        [Header("식별")]
        [SerializeField] private string familyId;
        [SerializeField] private string displayName;

        [TextArea(2, 5)]
        [SerializeField] private string concept;

        [Header("단계 맵")]
        [SerializeField] private RaidMapSO phase1;
        [SerializeField] private RaidMapSO phase2;
        [SerializeField] private RaidMapSO phase3;

        [Header("페이즈 연출")]
        [Tooltip("Phase 전환으로 새로 생기는 중간 Entry를 Rift로 표시할지 결정합니다.")]
        [SerializeField] private bool showRiftEntries;

        [Tooltip("Rift Entry 위에 세로 발광 빛기둥을 표시할지 결정합니다.")]
        [SerializeField] private bool showRiftEntryBeams = true;

        [Tooltip("Phase 2 전환 시작 충격 배율입니다.")]
        [Min(0.1f)] [SerializeField] private float phase2ImpactScale = 1f;
        [Tooltip("Phase 2 전환 지속 진동 배율입니다.")]
        [Min(0.1f)] [SerializeField] private float phase2RumbleScale = 1f;
        [Tooltip("Phase 2 균열/오로라/낙하 연출 배율입니다.")]
        [Min(0.1f)] [SerializeField] private float phase2CollapseFxScale = 1f;

        [Tooltip("Phase 3 전환 시작 충격 배율입니다.")]
        [Min(0.1f)] [SerializeField] private float phase3ImpactScale = 1f;
        [Tooltip("Phase 3 전환 지속 진동 배율입니다.")]
        [Min(0.1f)] [SerializeField] private float phase3RumbleScale = 1f;
        [Tooltip("Phase 3 균열/오로라/낙하 연출 배율입니다.")]
        [Min(0.1f)] [SerializeField] private float phase3CollapseFxScale = 1f;

        public string FamilyId => familyId;
        public string DisplayName => displayName;
        public string Concept => concept;
        public bool ShowRiftEntries => showRiftEntries;
        public bool ShowRiftEntryBeams => showRiftEntryBeams;
        public bool IsComplete => IsPhaseMap(phase1, RaidPhase.Phase1) && IsPhaseMap(phase2, RaidPhase.Phase2) && IsPhaseMap(phase3, RaidPhase.Phase3);

        public float GetTransitionImpactScale(RaidPhase phase)
        {
            return phase == RaidPhase.Phase3 ? PositiveOrOne(phase3ImpactScale) : PositiveOrOne(phase2ImpactScale);
        }

        public float GetTransitionRumbleScale(RaidPhase phase)
        {
            return phase == RaidPhase.Phase3 ? PositiveOrOne(phase3RumbleScale) : PositiveOrOne(phase2RumbleScale);
        }

        public float GetTransitionCollapseFxScale(RaidPhase phase)
        {
            return phase == RaidPhase.Phase3 ? PositiveOrOne(phase3CollapseFxScale) : PositiveOrOne(phase2CollapseFxScale);
        }

        public bool TryGetMap(RaidPhase phase, out RaidMapSO map)
        {
            switch (phase)
            {
                case RaidPhase.Phase1:
                    map = phase1;
                    break;
                case RaidPhase.Phase2:
                    map = phase2;
                    break;
                case RaidPhase.Phase3:
                    map = phase3;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(phase), phase, "지원하지 않는 Raid Phase입니다.");
            }

            return IsPhaseMap(map, phase);
        }

        private static float PositiveOrOne(float value)
        {
            return value > 0f ? value : 1f;
        }

        private static bool IsPhaseMap(RaidMapSO map, RaidPhase phase)
        {
            return map != null && map.HasData && map.Phase == phase;
        }
    }
}
