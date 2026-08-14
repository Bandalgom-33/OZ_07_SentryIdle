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

        public string FamilyId => familyId;
        public string DisplayName => displayName;
        public string Concept => concept;
        public bool IsComplete => IsPhaseMap(phase1, RaidPhase.Phase1) && IsPhaseMap(phase2, RaidPhase.Phase2) && IsPhaseMap(phase3, RaidPhase.Phase3);

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

        private static bool IsPhaseMap(RaidMapSO map, RaidPhase phase)
        {
            return map != null && map.HasData && map.Phase == phase;
        }
    }
}
