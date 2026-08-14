using System;
using EndlessGuard.Unit.Data;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    [CustomEditor(typeof(UnitLevelCurveSO))]
    public sealed class UnitLevelCurveSOEditor : UnityEditor.Editor
    {
        private SerializedProperty script;
        private SerializedProperty baseRequiredExp;
        private SerializedProperty linearIncreasePerLevel;
        private SerializedProperty powerCoefficient;
        private SerializedProperty powerExponent;
        private SerializedProperty levelOverrides;

        private bool showOverrides = true;
        private bool showPreview = true;
        private int previewStartLevel = 1;
        private int previewCount = 10;

        private void OnEnable()
        {
            script = serializedObject.FindProperty("m_Script");
            baseRequiredExp = serializedObject.FindProperty("baseRequiredExp");
            linearIncreasePerLevel = serializedObject.FindProperty("linearIncreasePerLevel");
            powerCoefficient = serializedObject.FindProperty("powerCoefficient");
            powerExponent = serializedObject.FindProperty("powerExponent");
            levelOverrides = serializedObject.FindProperty("levelOverrides");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            CombatDataEditorGUI.DrawReadOnlyProperty(script, "스크립트", "캐릭터 레벨별 필요 경험치 곡선을 정의하는 C# 스크립트입니다.");

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("필요 경험치 계산식", EditorStyles.boldLabel);

            baseRequiredExp.longValue = Math.Max(1L, EditorGUILayout.LongField(new GUIContent("기본 필요 경험치", "Lv.1에서 Lv.2로 올라갈 때 사용하는 기준 필요 경험치입니다."), baseRequiredExp.longValue));
            linearIncreasePerLevel.longValue = Math.Max(0L, EditorGUILayout.LongField(new GUIContent("레벨당 선형 증가량", "현재 레벨이 1 증가할 때마다 추가되는 선형 경험치입니다."), linearIncreasePerLevel.longValue));
            powerCoefficient.floatValue = Mathf.Max(0f, EditorGUILayout.FloatField(new GUIContent("거듭제곱 증가 계수", "높은 레벨에서 완만하게 추가되는 곡선 경험치의 계수입니다."), powerCoefficient.floatValue));
            powerExponent.floatValue = EditorGUILayout.Slider(new GUIContent("거듭제곱 지수", "값이 높을수록 후반 필요 경험치가 빠르게 증가합니다."), powerExponent.floatValue, 1f, 3f);

            EditorGUILayout.HelpBox("필요 경험치 = 기본 필요 경험치 + 선형 증가량 × (현재 레벨 - 1) + 거듭제곱 증가 계수 × (현재 레벨 - 1)^거듭제곱 지수", MessageType.Info);

            DrawOverrides();

            serializedObject.ApplyModifiedProperties();

            DrawPreview((UnitLevelCurveSO)target);
        }

        private void DrawOverrides()
        {
            EditorGUILayout.Space(8f);
            showOverrides = EditorGUILayout.Foldout(showOverrides, "특정 레벨 예외", true);

            if (!showOverrides)
            {
                return;
            }

            EditorGUI.indentLevel++;
            EditorGUILayout.HelpBox("자동 수식을 사용하지 않을 특정 레벨만 등록합니다. 현재 레벨 29는 Lv.29에서 Lv.30으로 올라갈 때를 의미합니다.", MessageType.Info);

            for (int i = 0; i < levelOverrides.arraySize; i++)
            {
                SerializedProperty element = levelOverrides.GetArrayElementAtIndex(i);
                SerializedProperty currentLevel = element.FindPropertyRelative("currentLevel");
                SerializedProperty requiredExp = element.FindPropertyRelative("requiredExp");

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"예외 {i + 1}", EditorStyles.boldLabel);
                currentLevel.intValue = Mathf.Max(1, EditorGUILayout.IntField(new GUIContent("현재 레벨", "이 레벨에서 다음 레벨로 올라갈 때 예외 경험치를 사용합니다."), currentLevel.intValue));
                requiredExp.longValue = Math.Max(1L, EditorGUILayout.LongField(new GUIContent("필요 경험치", "해당 레벨에서 다음 레벨로 올라가기 위해 필요한 경험치입니다."), requiredExp.longValue));

                if (HasDuplicateLevel(i, currentLevel.intValue))
                {
                    EditorGUILayout.HelpBox($"현재 레벨 {currentLevel.intValue}이 다른 예외 항목과 중복됩니다. 먼저 등록된 예외값이 사용됩니다.", MessageType.Warning);
                }

                if (GUILayout.Button("이 예외 제거"))
                {
                    levelOverrides.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndVertical();
                    break;
                }

                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("예외 레벨 추가"))
            {
                int newIndex = levelOverrides.arraySize;
                levelOverrides.InsertArrayElementAtIndex(newIndex);
                SerializedProperty newElement = levelOverrides.GetArrayElementAtIndex(newIndex);
                newElement.FindPropertyRelative("currentLevel").intValue = 1;
                newElement.FindPropertyRelative("requiredExp").longValue = 1L;
            }

            EditorGUI.indentLevel--;
        }

        private void DrawPreview(UnitLevelCurveSO curve)
        {
            EditorGUILayout.Space(8f);
            showPreview = EditorGUILayout.Foldout(showPreview, "필요 경험치 미리보기", true);

            if (!showPreview)
            {
                return;
            }

            EditorGUI.indentLevel++;

            previewStartLevel = Mathf.Max(1, EditorGUILayout.IntField(new GUIContent("미리보기 시작 레벨", "필요 경험치 미리보기를 시작할 현재 레벨입니다."), previewStartLevel));
            previewCount = EditorGUILayout.IntSlider(new GUIContent("표시 레벨 수", "한 번에 표시할 레벨 행의 수입니다."), previewCount, 1, 30);

            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("현재 레벨", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("다음 레벨", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("필요 경험치", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < previewCount; i++)
            {
                int currentLevel = previewStartLevel + i;
                int nextLevel = currentLevel + 1;
                long requiredExp = curve.GetRequiredExp(currentLevel);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Lv.{currentLevel}");
                EditorGUILayout.LabelField($"Lv.{nextLevel}");
                EditorGUILayout.LabelField($"{requiredExp:N0}");
                EditorGUILayout.EndHorizontal();
            }

            int previewTargetLevel = previewStartLevel + previewCount;
            long totalRequiredExp = curve.GetTotalRequiredExp(previewStartLevel, previewTargetLevel);

            EditorGUILayout.Space(4f);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField(new GUIContent("미리보기 구간 누적 경험치", $"Lv.{previewStartLevel}에서 Lv.{previewTargetLevel}까지 필요한 누적 경험치입니다."), $"{totalRequiredExp:N0}");
            }

            EditorGUI.indentLevel--;
        }

        private bool HasDuplicateLevel(int currentIndex, int targetLevel)
        {
            for (int i = 0; i < levelOverrides.arraySize; i++)
            {
                if (i == currentIndex)
                {
                    continue;
                }

                SerializedProperty element = levelOverrides.GetArrayElementAtIndex(i);
                SerializedProperty currentLevel = element.FindPropertyRelative("currentLevel");

                if (currentLevel.intValue == targetLevel)
                {
                    return true;
                }
            }

            return false;
        }
    }
}