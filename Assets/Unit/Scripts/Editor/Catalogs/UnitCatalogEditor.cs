using EndlessGuard.Unit.Data;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    [CustomEditor(typeof(UnitCatalog))]
    public sealed class UnitCatalogEditor : UnityEditor.Editor
    {
        private SerializedProperty script;
        private SerializedProperty lastIssuedNumber;
        private SerializedProperty units;
        private string resultMessage;
        private MessageType resultType = MessageType.None;

        private void OnEnable()
        {
            script = serializedObject.FindProperty("m_Script");
            lastIssuedNumber = serializedObject.FindProperty("lastIssuedNumber");
            units = serializedObject.FindProperty("units");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            CombatDataEditorGUI.DrawReadOnlyProperty(script, "스크립트", "이 Catalog 에셋을 정의하는 C# 스크립트입니다.");

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("ID 발급 상태", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.IntField(new GUIContent("마지막 발급 번호", "삭제된 데이터의 번호를 다시 사용하지 않기 위해 마지막 발급 이력을 보관합니다."), lastIssuedNumber.intValue);
                EditorGUILayout.TextField(new GUIContent("다음 발급 예정 ID", "현재 발급 이력을 기준으로 다음에 생성될 캐릭터 ID입니다."), $"UNIT_{lastIssuedNumber.intValue + 1:D4}");
            }

            EditorGUILayout.Space(5f);
            EditorGUILayout.HelpBox("Assets/Unit/Data/Units 폴더만 검색합니다. 기존 ID는 변경하지 않고, ID가 비어 있는 데이터에만 새 ID를 발급합니다. Catalog 목록은 검색 결과로 다시 구성됩니다.", MessageType.Info);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(units, new GUIContent("등록된 캐릭터 데이터"), true);
            }

            EditorGUILayout.Space(8f);

            if (GUILayout.Button("캐릭터 데이터 검색·ID 발급·등록"))
            {
                CatalogSyncResult result = CatalogEditorUtility.SyncUnitCatalog((UnitCatalog)target);
                resultMessage = result.Message;
                resultType = result.Success ? MessageType.Info : MessageType.Error;
                serializedObject.Update();
            }

            if (!string.IsNullOrWhiteSpace(resultMessage))
            {
                EditorGUILayout.HelpBox(resultMessage, resultType);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}