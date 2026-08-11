using EndlessGuard.Unit.Data;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    [CustomEditor(typeof(CleanseSO))]
    [CanEditMultipleObjects]
    public sealed class CleanseSOEditor : PassiveDataSOEditor
    {
        private SerializedProperty cleanseIntervalSeconds;

        protected override void OnEnable()
        {
            base.OnEnable();

            cleanseIntervalSeconds = serializedObject.FindProperty("cleanseIntervalSeconds");
        }

        protected override void DrawSpecificFields()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("상태이상 정화 기본값", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(cleanseIntervalSeconds, new GUIContent("정화 주기 (초)", "아군 몬스터의 상태이상을 제거하는 효과가 반복해서 발동하는 기본 추천 주기입니다. 실제 수치는 몬스터 데이터의 패시브 개별 수치에서 조정할 수 있습니다."));

            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox("정화 가능한 상태이상 종류, 대상 우선순위와 한 번에 정화할 대상 수는 실제 상태이상 시스템을 구현할 때 함께 결정합니다.", MessageType.Info);
        }
    }
}