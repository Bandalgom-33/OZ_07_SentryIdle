using EndlessGuard.Unit.Data;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    [CustomEditor(typeof(CritSummonSO))]
    [CanEditMultipleObjects]
    public sealed class CritSummonSOEditor : PassiveDataSOEditor
    {
        private SerializedProperty summonPrefab;
        private SerializedProperty summonCooldownSeconds;
        private SerializedProperty maxActiveSummons;

        protected override void OnEnable()
        {
            base.OnEnable();

            summonPrefab = serializedObject.FindProperty("summonPrefab");
            summonCooldownSeconds = serializedObject.FindProperty("summonCooldownSeconds");
            maxActiveSummons = serializedObject.FindProperty("maxActiveSummons");
        }

        protected override void DrawSpecificFields()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("치명타 소환 설정", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(summonPrefab, new GUIContent("소환물 프리팹", "치명타 적중 시 생성할 소환물 프리팹입니다."));
            EditorGUILayout.PropertyField(summonCooldownSeconds, new GUIContent("소환 대기시간 (초)", "치명타로 소환한 뒤 다시 소환할 수 있을 때까지의 대기시간입니다."));
            EditorGUILayout.PropertyField(maxActiveSummons, new GUIContent("최대 동시 소환 수", "이 패시브로 동시에 유지할 수 있는 소환물의 최대 개수입니다."));

            if (summonPrefab.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("소환물 프리팹을 연결해야 치명타 적중 시 실제 소환이 가능합니다.", MessageType.Warning);
            }
        }
    }
}