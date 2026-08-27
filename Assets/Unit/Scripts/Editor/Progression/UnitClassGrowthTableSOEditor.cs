using EndlessGuard.Unit.Data;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    [CustomEditor(typeof(UnitClassGrowthTableSO))]
    public sealed class UnitClassGrowthTableSOEditor : UnityEditor.Editor
    {
        private const float PromotionGrowthPercent = 10f;
        private const int BaseMaxLevel = 30;
        private const int MaxPromotionStage = 6;
        private const int LevelIncreasePerPromotion = 15;

        private SerializedProperty levelCurve;
        private SerializedProperty profiles;

        private void OnEnable()
        {
            levelCurve = serializedObject.FindProperty("levelCurve");
            profiles = serializedObject.FindProperty("profiles");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(levelCurve, new GUIContent("공통 경험치 곡선", "모든 상위 분류가 공유하는 레벨업 필요 경험치 곡선입니다."));

            EditorGUILayout.Space(8f);

            DrawPresetButton();

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("상위 분류별 성장", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("레벨업은 상위 분류별 성장 규칙을 사용합니다. 승급은 모든 분류가 11종 능력치를 동일한 비율로 성장시키며 최대 레벨을 확장합니다.", MessageType.Info);

            for (int i = 0; i < profiles.arraySize; i++)
            {
                SerializedProperty profile = profiles.GetArrayElementAtIndex(i);
                SerializedProperty unitClass = profile.FindPropertyRelative("unitClass");
                SerializedProperty levelGrowth = profile.FindPropertyRelative("levelGrowth");
                SerializedProperty promotionGrowth = profile.FindPropertyRelative("promotionGrowth");
                SerializedProperty baseMaxLevel = profile.FindPropertyRelative("baseMaxLevel");
                SerializedProperty promotionLevelCaps = profile.FindPropertyRelative("promotionLevelCaps");

                UnitClass classValue = (UnitClass)unitClass.intValue;

                EditorGUILayout.Space(5f);

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField(GetClassDisplayName(unitClass, classValue), EditorStyles.boldLabel);

                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.PropertyField(unitClass, new GUIContent("상위 분류"));
                    }

                    DrawGrowthRule(levelGrowth, "레벨업 성장");
                    DrawGrowthRule(promotionGrowth, "승급 성장");

                    EditorGUILayout.PropertyField(baseMaxLevel, new GUIContent("기본 최대 레벨"));
                    EditorGUILayout.PropertyField(promotionLevelCaps, new GUIContent("승급 단계별 최대 레벨"), true);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPresetButton()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("정식 성장 밸런스", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("현재 프로젝트에서 사용하는 정식 기본 성장값을 6개 상위 분류에 한 번에 적용합니다.", EditorStyles.wordWrappedMiniLabel);

                EditorGUILayout.Space(4f);

                if (!GUILayout.Button("정식 성장값 일괄 적용", GUILayout.Height(28f)))
                {
                    return;
                }

                bool confirmed = EditorUtility.DisplayDialog("정식 성장값 적용", "현재 UnitClassGrowthTable의 성장 설정을 정식 기본값으로 덮어씁니다.\n\n계속하시겠습니까?", "적용", "취소");

                if (!confirmed)
                {
                    return;
                }

                ApplyRecommendedDefaults();
            }
        }

        private void ApplyRecommendedDefaults()
        {
            Undo.RecordObject(target, "Unit Class Growth Defaults");

            serializedObject.Update();

            for (int i = 0; i < profiles.arraySize; i++)
            {
                SerializedProperty profile = profiles.GetArrayElementAtIndex(i);
                SerializedProperty unitClassProperty = profile.FindPropertyRelative("unitClass");
                SerializedProperty levelGrowth = profile.FindPropertyRelative("levelGrowth");
                SerializedProperty promotionGrowth = profile.FindPropertyRelative("promotionGrowth");
                SerializedProperty baseMaxLevel = profile.FindPropertyRelative("baseMaxLevel");
                SerializedProperty promotionLevelCaps = profile.FindPropertyRelative("promotionLevelCaps");

                UnitClass unitClass = (UnitClass)unitClassProperty.intValue;

                GetLevelGrowthPreset(unitClass, out GrowthStatMask levelStats, out float levelPercent);

                SetGrowthRule(levelGrowth, levelStats, levelPercent);
                SetGrowthRule(promotionGrowth, GrowthStatMask.All, PromotionGrowthPercent);

                baseMaxLevel.intValue = BaseMaxLevel;

                SetPromotionLevelCaps(promotionLevelCaps);
            }

            serializedObject.ApplyModifiedProperties();

            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();

        }

        private static void SetGrowthRule(SerializedProperty rule, GrowthStatMask affectedStats, float percentPerStep)
        {
            if (rule == null)
            {
                return;
            }

            SerializedProperty affectedStatsProperty = rule.FindPropertyRelative("affectedStats");
            SerializedProperty percentPerStepProperty = rule.FindPropertyRelative("percentPerStep");
            SerializedProperty stackModeProperty = rule.FindPropertyRelative("stackMode");

            affectedStatsProperty.intValue = (int)affectedStats;
            percentPerStepProperty.floatValue = percentPerStep;
            stackModeProperty.enumValueIndex = (int)GrowthStackMode.LinearFromBase;
        }

        private static void SetPromotionLevelCaps(SerializedProperty promotionLevelCaps)
        {
            if (promotionLevelCaps == null)
            {
                return;
            }

            promotionLevelCaps.arraySize = MaxPromotionStage;

            for (int stage = 1; stage <= MaxPromotionStage; stage++)
            {
                SerializedProperty element = promotionLevelCaps.GetArrayElementAtIndex(stage - 1);
                SerializedProperty promotionStage = element.FindPropertyRelative("promotionStage");
                SerializedProperty maxLevel = element.FindPropertyRelative("maxLevel");

                promotionStage.intValue = stage;
                maxLevel.intValue = BaseMaxLevel + LevelIncreasePerPromotion * stage;
            }
        }

        private static void GetLevelGrowthPreset(UnitClass unitClass, out GrowthStatMask affectedStats, out float percent)
        {
            switch (unitClass)
            {
                case UnitClass.Vanguard:
                    affectedStats = GrowthStatMask.MaxHp | GrowthStatMask.HpRegenPerSecond | GrowthStatMask.PhysicalAttack | GrowthStatMask.MagicalAttack | GrowthStatMask.AttacksPerSecond | GrowthStatMask.Accuracy | GrowthStatMask.Evasion;
                    percent = 1.5f;
                    return;

                case UnitClass.Guard:
                    affectedStats = GrowthStatMask.MaxHp | GrowthStatMask.PhysicalAttack | GrowthStatMask.MagicalAttack | GrowthStatMask.AttacksPerSecond | GrowthStatMask.PhysicalDefense | GrowthStatMask.MagicalDefense | GrowthStatMask.Accuracy | GrowthStatMask.CriticalChancePercent | GrowthStatMask.CriticalDamageBonusPercent;
                    percent = 1.5f;
                    return;

                case UnitClass.Defender:
                    affectedStats = GrowthStatMask.MaxHp | GrowthStatMask.HpRegenPerSecond | GrowthStatMask.PhysicalDefense | GrowthStatMask.MagicalDefense | GrowthStatMask.Evasion;
                    percent = 1.8f;
                    return;

                case UnitClass.Supporter:
                    affectedStats = GrowthStatMask.MaxHp | GrowthStatMask.HpRegenPerSecond | GrowthStatMask.PhysicalAttack | GrowthStatMask.MagicalAttack | GrowthStatMask.AttacksPerSecond | GrowthStatMask.Accuracy | GrowthStatMask.CriticalChancePercent | GrowthStatMask.CriticalDamageBonusPercent;
                    percent = 1.4f;
                    return;

                case UnitClass.Sniper:
                    affectedStats = GrowthStatMask.PhysicalAttack | GrowthStatMask.MagicalAttack | GrowthStatMask.AttacksPerSecond | GrowthStatMask.Accuracy | GrowthStatMask.Evasion | GrowthStatMask.CriticalChancePercent | GrowthStatMask.CriticalDamageBonusPercent;
                    percent = 1.7f;
                    return;

                case UnitClass.Specialist:
                    affectedStats = GrowthStatMask.All;
                    percent = 1.2f;
                    return;

                default:
                    affectedStats = GrowthStatMask.None;
                    percent = 0f;
                    return;
            }
        }

        private static string GetClassDisplayName(SerializedProperty unitClassProperty, UnitClass classValue)
        {
            if (unitClassProperty != null && unitClassProperty.enumValueIndex >= 0 && unitClassProperty.enumValueIndex < unitClassProperty.enumDisplayNames.Length)
            {
                return unitClassProperty.enumDisplayNames[unitClassProperty.enumValueIndex];
            }

            return ObjectNames.NicifyVariableName(classValue.ToString());
        }

        private static void DrawGrowthRule(SerializedProperty rule, string label)
        {
            if (rule == null)
            {
                return;
            }

            SerializedProperty affectedStats = rule.FindPropertyRelative("affectedStats");
            SerializedProperty percentPerStep = rule.FindPropertyRelative("percentPerStep");
            SerializedProperty stackMode = rule.FindPropertyRelative("stackMode");

            EditorGUILayout.Space(3f);
            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(affectedStats, new GUIContent("성장 능력치"));
            EditorGUILayout.PropertyField(percentPerStep, new GUIContent("공통 성장률 (%)"));
            EditorGUILayout.PropertyField(stackMode, new GUIContent("누적 방식"));
        }
    }
}
