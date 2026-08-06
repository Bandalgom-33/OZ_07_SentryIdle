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
        private SerializedProperty allowedUnitSubclasses;
        private SerializedProperty allowedEnemyCategories;
        private SerializedProperty allowedEnemyMovementTypes;
        private SerializedProperty allowedEnemySizes;
        private SerializedProperty allowedEnemyRoles;

        protected virtual void OnEnable()
        {
            script = serializedObject.FindProperty("m_Script");
            displayName = serializedObject.FindProperty("displayName");
            description = serializedObject.FindProperty("description");
            usableBy = serializedObject.FindProperty("usableBy");
            compatibility = serializedObject.FindProperty("compatibility");

            allowedUnitClasses = compatibility.FindPropertyRelative("allowedUnitClasses");
            allowedUnitSubclasses = compatibility.FindPropertyRelative("allowedUnitSubclasses");
            allowedEnemyCategories = compatibility.FindPropertyRelative("allowedEnemyCategories");
            allowedEnemyMovementTypes = compatibility.FindPropertyRelative("allowedEnemyMovementTypes");
            allowedEnemySizes = compatibility.FindPropertyRelative("allowedEnemySizes");
            allowedEnemyRoles = compatibility.FindPropertyRelative("allowedEnemyRoles");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            CombatDataEditorGUI.DrawReadOnlyProperty(script, "스크립트", "이 패시브 데이터 에셋을 정의하는 C# 스크립트입니다.");

            EditorGUILayout.PropertyField(displayName, new GUIContent("표시 이름", "캐릭터·몬스터 데이터와 제작 도구에 표시되는 패시브 이름입니다."));
            EditorGUILayout.PropertyField(description, new GUIContent("설명", "패시브의 발동 조건과 효과를 설명합니다."));
            EditorGUILayout.PropertyField(usableBy, new GUIContent("사용 가능 대상", "캐릭터, 몬스터 또는 양쪽 모두가 사용할 수 있는지 설정합니다."));

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
            compatibility.isExpanded = EditorGUILayout.Foldout(compatibility.isExpanded, "패시브 호환 조건", true);

            if (!compatibility.isExpanded)
            {
                return;
            }

            EditorGUI.indentLevel++;

            if (usableBy.hasMultipleDifferentValues)
            {
                EditorGUILayout.HelpBox("선택한 패시브 에셋들의 사용 가능 대상이 서로 달라 호환 조건을 함께 편집할 수 없습니다.", MessageType.Info);
                EditorGUI.indentLevel--;
                return;
            }

            PassiveUserType selectedUserType = (PassiveUserType)usableBy.intValue;

            if (selectedUserType == PassiveUserType.None)
            {
                EditorGUILayout.HelpBox("사용 가능 대상을 먼저 선택하세요.", MessageType.Warning);
                EditorGUI.indentLevel--;
                return;
            }

            EditorGUILayout.HelpBox("제한 목록이 비어 있으면 해당 분류를 제한하지 않습니다.", MessageType.Info);

            if (selectedUserType == PassiveUserType.Unit || selectedUserType == PassiveUserType.Both)
            {
                PassiveCompatibilityEditorGUI.DrawUnitRestrictions(allowedUnitClasses, allowedUnitSubclasses);
            }

            if (selectedUserType == PassiveUserType.Enemy || selectedUserType == PassiveUserType.Both)
            {
                DrawEnemyCompatibility();
            }

            EditorGUI.indentLevel--;
        }

        private void DrawEnemyCompatibility()
        {
            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("몬스터 분류 제한", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(
                allowedEnemyCategories,
                new GUIContent("허용 몬스터 분류", "비어 있으면 일반, 엘리트와 보스 모두 사용할 수 있습니다."),
                true);

            EditorGUILayout.PropertyField(
                allowedEnemyMovementTypes,
                new GUIContent("허용 이동 유형", "비어 있으면 지상과 공중 몬스터 모두 사용할 수 있습니다."),
                true);

            EditorGUILayout.PropertyField(
                allowedEnemySizes,
                new GUIContent("허용 몬스터 크기", "비어 있으면 모든 몬스터 크기에서 사용할 수 있습니다."),
                true);

            EditorGUILayout.PropertyField(
                allowedEnemyRoles,
                new GUIContent("허용 전투 역할", "비어 있으면 모든 몬스터 전투 역할에서 사용할 수 있습니다."),
                true);
        }
    }
}