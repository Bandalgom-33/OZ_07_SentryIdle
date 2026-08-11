using System;
using System.Collections.Generic;
using System.Reflection;
using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Runtime;
using UnityEngine;

namespace EndlessGuard.Unit.Prototype.Phase2
{
    /// <summary>
    /// Prototype 전용 Runtime 데이터 복제/오버라이드 도구입니다.
    /// 원본 ScriptableObject를 절대 수정하지 않고 PlayMode 동안만 사용할 복제본을 만듭니다.
    /// </summary>
    internal static class Phase2PrototypeDataFactory
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        public static UnitDataSO CloneUnitData(
            UnitDataSO source,
            PassiveDataSO passive = null,
            GameObject summonPrefabOverride = null,
            UnitPlacement? placementOverride = null,
            UnitClassGrowthTableSO growthTableOverride = null,
            bool forceCriticalChance100 = false)
        {
            if (source == null)
            {
                return null;
            }

            UnitDataSO clone = UnityEngine.Object.Instantiate(source);
            clone.name = source.name + "_Phase2Runtime";
            clone.hideFlags = HideFlags.DontSave;

            SetField(clone, "passives", BuildPassiveList(passive));
            SetField(clone, "passiveTunings", BuildTunings(passive, summonPrefabOverride, 0f));

            if (placementOverride.HasValue)
            {
                SetField(clone, "placement", placementOverride.Value);
            }

            if (growthTableOverride != null)
            {
                SetField(clone, "growthTable", growthTableOverride);
            }

            if (forceCriticalChance100)
            {
                SetField(clone, "criticalChancePercent", 100f);
            }

            return clone;
        }

        public static EnemyDataSO CloneEnemyData(
            EnemyDataSO source,
            PassiveDataSO passive = null,
            GameObject summonPrefabOverride = null,
            float summonIntervalOverrideSeconds = 0f,
            EnemySize? sizeOverride = null,
            EnemyMovementType? movementOverride = null)
        {
            if (source == null)
            {
                return null;
            }

            EnemyDataSO clone = UnityEngine.Object.Instantiate(source);
            clone.name = source.name + "_Phase2Runtime";
            clone.hideFlags = HideFlags.DontSave;

            SetField(clone, "passives", BuildPassiveList(passive));
            SetField(clone, "passiveTunings", BuildTunings(passive, summonPrefabOverride, summonIntervalOverrideSeconds));

            if (sizeOverride.HasValue)
            {
                SetField(clone, "size", sizeOverride.Value);
            }

            if (movementOverride.HasValue)
            {
                SetField(clone, "movementType", movementOverride.Value);
            }

            return clone;
        }

        public static UnitClassGrowthTableSO CreateGrowthTable(
            UnitClassGrowthTableSO source,
            UnitClass unitClass,
            GrowthStatMask levelStats,
            float levelPercentPerStep,
            GrowthStatMask promotionStats,
            float promotionPercentPerStep,
            int baseMaxLevel,
            int firstPromotionMaxLevel)
        {
            UnitClassGrowthTableSO table = ScriptableObject.CreateInstance<UnitClassGrowthTableSO>();
            table.name = "Phase2GrowthTableRuntime";
            table.hideFlags = HideFlags.DontSave;

            SetField(table, "levelCurve", source != null ? source.LevelCurve : null);

            UnitClassGrowthProfile profile = new UnitClassGrowthProfile();
            SetField(profile, "unitClass", unitClass);
            SetField(profile, "levelGrowth", CreateGrowthRule(levelStats, levelPercentPerStep));
            SetField(profile, "promotionGrowth", CreateGrowthRule(promotionStats, promotionPercentPerStep));
            SetField(profile, "baseMaxLevel", Mathf.Max(1, baseMaxLevel));

            List<PromotionLevelCap> caps = new List<PromotionLevelCap>();

            if (firstPromotionMaxLevel > baseMaxLevel)
            {
                PromotionLevelCap cap = new PromotionLevelCap();
                SetField(cap, "promotionStage", 1);
                SetField(cap, "maxLevel", Mathf.Max(baseMaxLevel + 1, firstPromotionMaxLevel));
                caps.Add(cap);
            }

            SetField(profile, "promotionLevelCaps", caps);
            SetField(table, "profiles", new List<UnitClassGrowthProfile> { profile });

            return table;
        }

        public static EnemySize ResolveCompatibleEnemySize(PassiveDataSO passive, EnemySize fallback)
        {
            if (passive == null || passive.Compatibility == null || passive.Compatibility.AllowedEnemySizes == null)
            {
                return fallback;
            }

            IReadOnlyList<EnemySize> sizes = passive.Compatibility.AllowedEnemySizes;

            for (int i = 0; i < sizes.Count; i++)
            {
                if (sizes[i] != EnemySize.None)
                {
                    return sizes[i];
                }
            }

            return fallback;
        }

        public static bool AssignUnitData(UnitDataLink link, UnitDataSO data)
        {
            return link != null && SetField(link, "unitData", data);
        }

        public static bool AssignEnemyData(EnemyDataLink link, EnemyDataSO data)
        {
            return link != null && SetField(link, "enemyData", data);
        }

        private static UnitGrowthRule CreateGrowthRule(GrowthStatMask stats, float percentPerStep)
        {
            UnitGrowthRule rule = new UnitGrowthRule();
            SetField(rule, "affectedStats", stats);
            SetField(rule, "percentPerStep", Mathf.Max(0f, percentPerStep));
            SetField(rule, "stackMode", GrowthStackMode.LinearFromBase);
            return rule;
        }

        private static List<PassiveDataSO> BuildPassiveList(PassiveDataSO passive)
        {
            List<PassiveDataSO> result = new List<PassiveDataSO>(1);

            if (passive != null)
            {
                result.Add(passive);
            }

            return result;
        }

        private static List<PassiveTuning> BuildTunings(PassiveDataSO passive, GameObject summonPrefabOverride, float summonIntervalOverrideSeconds)
        {
            List<PassiveTuning> tunings = new List<PassiveTuning>(1);

            if (passive == null || (summonPrefabOverride == null && summonIntervalOverrideSeconds <= 0f))
            {
                return tunings;
            }

            PassiveTuning tuning = new PassiveTuning();
            SetField(tuning, "passive", passive);

            if (summonPrefabOverride != null)
            {
                PassiveRef passiveRef = new PassiveRef();
                SetField(passiveRef, "key", PassiveRefKey.SummonPrefab);
                SetField(passiveRef, "reference", summonPrefabOverride);
                SetField(tuning, "refs", new List<PassiveRef> { passiveRef });
            }

            if (summonIntervalOverrideSeconds > 0f)
            {
                PassiveValue passiveValue = new PassiveValue();
                SetField(passiveValue, "key", PassiveValueKey.SummonIntervalSeconds);
                SetField(passiveValue, "value", Mathf.Max(0.1f, summonIntervalOverrideSeconds));
                SetField(tuning, "values", new List<PassiveValue> { passiveValue });
            }

            tunings.Add(tuning);
            return tunings;
        }

        private static bool SetField(object target, string fieldName, object value)
        {
            if (target == null || string.IsNullOrEmpty(fieldName))
            {
                return false;
            }

            FieldInfo field = target.GetType().GetField(fieldName, PrivateInstance);

            if (field == null)
            {
                Debug.LogError($"Phase2 Prototype: {target.GetType().Name}.{fieldName} 필드를 찾지 못했습니다.");
                return false;
            }

            field.SetValue(target, value);
            return true;
        }
    }
}
