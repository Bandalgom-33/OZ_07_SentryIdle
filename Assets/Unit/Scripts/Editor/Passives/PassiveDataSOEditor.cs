using EndlessGuard.Unit.Data;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    [CustomEditor(typeof(PassiveDataSO), true)]
    [CanEditMultipleObjects]
    public class PassiveDataSOEditor : UnityEditor.Editor
    {
        private SerializedProperty script;
        private SerializedProperty displayName;
        private SerializedProperty description;
        private SerializedProperty usableBy;
        private SerializedProperty compatibility;
        private SerializedProperty allowedUnitClasses;
        private SerializedProperty allowedEnemySizes;

        protected virtual void OnEnable()
        {
            script = serializedObject.FindProperty("m_Script");
            displayName = serializedObject.FindProperty("displayName");
            description = serializedObject.FindProperty("description");
            usableBy = serializedObject.FindProperty("usableBy");
            compatibility = serializedObject.FindProperty("compatibility");

            allowedUnitClasses = compatibility.FindPropertyRelative("allowedUnitClasses");
            allowedEnemySizes = compatibility.FindPropertyRelative("allowedEnemySizes");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            CombatDataEditorGUI.DrawReadOnlyProperty(
                script,
                "스크립트",
                "이 패시브 데이터 에셋을 정의하는 C# 스크립트입니다.");

            EditorGUILayout.PropertyField(
                displayName,
                new GUIContent(
                    "표시 이름",
                    "캐릭터·몬스터 데이터와 제작 도구에 표시되는 패시브 이름입니다."));

            EditorGUILayout.PropertyField(
                description,
                new GUIContent(
                    "설명",
                    "패시브의 발동 조건과 효과를 설명합니다."));

            EditorGUILayout.PropertyField(
                usableBy,
                new GUIContent(
                    "사용 가능 대상",
                    "캐릭터, 몬스터 또는 양쪽 모두가 사용할 수 있는지 설정합니다."));

            DrawCompatibility();

            DrawSpecificFields();

            serializedObject.ApplyModifiedProperties();
        }

        protected virtual void DrawSpecificFields()
        {
        }

        private void DrawCompatibility()
        {
            EditorGUILayout.Space(8f);

            compatibility.isExpanded = EditorGUILayout.Foldout(
                compatibility.isExpanded,
                "패시브 선택 풀",
                true);

            if (!compatibility.isExpanded)
            {
                return;
            }

            EditorGUI.indentLevel++;

            if (usableBy.hasMultipleDifferentValues)
            {
                EditorGUILayout.HelpBox(
                    "선택한 패시브 에셋들의 사용 가능 대상이 서로 달라 선택 풀을 함께 편집할 수 없습니다.",
                    MessageType.Info);

                EditorGUI.indentLevel--;
                return;
            }

            PassiveUserType selectedUserType = (PassiveUserType)usableBy.intValue;

            if (selectedUserType == PassiveUserType.None)
            {
                EditorGUILayout.HelpBox(
                    "사용 가능 대상을 먼저 선택하세요.",
                    MessageType.Warning);

                EditorGUI.indentLevel--;
                return;
            }

            if (selectedUserType == PassiveUserType.Unit || selectedUserType == PassiveUserType.Both)
            {
                PassiveCompatibilityEditorGUI.DrawUnitRestrictions(allowedUnitClasses);
            }

            if (selectedUserType == PassiveUserType.Enemy || selectedUserType == PassiveUserType.Both)
            {
                EditorGUILayout.Space(8f);
                PassiveCompatibilityEditorGUI.DrawEnemyRestrictions(allowedEnemySizes);
            }

            EditorGUI.indentLevel--;
        }
    }
}