using System;
using System.Collections.Generic;
using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [CreateAssetMenu(fileName = "UnitClassGrowthTable", menuName = "Endless Guard/Progression/Unit Class Growth Table")]
    public sealed class UnitClassGrowthTableSO : ScriptableObject
    {
        [Header("공통 경험치 곡선")]
        [Tooltip("캐릭터 레벨업에 사용할 공통 필요 경험치 곡선입니다.")]
        [SerializeField] private UnitLevelCurveSO levelCurve;

        [Header("상위 분류별 성장 프로필")]
        [Tooltip("Vanguard, Guard, Defender 등 상위 분류별로 하나의 성장 프로필을 공유합니다.")]
        [SerializeField] private List<UnitClassGrowthProfile> profiles = new List<UnitClassGrowthProfile>();

        public UnitLevelCurveSO LevelCurve => levelCurve;
        public IReadOnlyList<UnitClassGrowthProfile> Profiles => profiles;

        public bool TryGetProfile(UnitClass unitClass, out UnitClassGrowthProfile profile)
        {
            if (profiles != null)
            {
                for (int i = 0; i < profiles.Count; i++)
                {
                    UnitClassGrowthProfile current = profiles[i];

                    if (current != null && current.UnitClass == unitClass)
                    {
                        profile = current;
                        return true;
                    }
                }
            }

            profile = null;
            return false;
        }

        public int GetMaxLevel(UnitClass unitClass, int promotionStage, int fallbackLevel = 1)
        {
            return TryGetProfile(unitClass, out UnitClassGrowthProfile profile)
                ? Mathf.Max(fallbackLevel, profile.GetMaxLevel(promotionStage))
                : Mathf.Max(1, fallbackLevel);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsureCurrentClasses();
        }

        private void EnsureCurrentClasses()
        {
            if (profiles == null)
            {
                profiles = new List<UnitClassGrowthProfile>();
            }

            Array values = Enum.GetValues(typeof(UnitClass));

            for (int valueIndex = 0; valueIndex < values.Length; valueIndex++)
            {
                UnitClass unitClass = (UnitClass)values.GetValue(valueIndex);

                if (unitClass == UnitClass.None || ContainsClass(unitClass))
                {
                    continue;
                }

                UnitClassGrowthProfile profile = new UnitClassGrowthProfile();
                profile.SetUnitClass(unitClass);
                profiles.Add(profile);
            }
        }

        private bool ContainsClass(UnitClass unitClass)
        {
            for (int i = 0; i < profiles.Count; i++)
            {
                UnitClassGrowthProfile current = profiles[i];

                if (current != null && current.UnitClass == unitClass)
                {
                    return true;
                }
            }

            return false;
        }
#endif
    }
}
