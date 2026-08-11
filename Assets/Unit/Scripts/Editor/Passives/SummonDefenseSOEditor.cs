using EndlessGuard.Unit.Data;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    [CustomEditor(typeof(SummonDefenseSO))]
    [CanEditMultipleObjects]
    public sealed class SummonDefenseSOEditor : PassiveDataSOEditor
    {
        private SerializedProperty physicalDefensePerSummonPercent;
        private SerializedProperty magicalDefensePerSummonPercent;

        protected override void OnEnable()
        {
            base.OnEnable();

            physicalDefensePerSummonPercent = serializedObject.FindProperty("physicalDefensePerSummonPercent");
            magicalDefensePerSummonPercent = serializedObject.FindProperty("magicalDefensePerSummonPercent");
        }

        protected override void DrawSpecificFields()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("소환물 방어력 증가 기본값", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(physicalDefensePerSummonPercent, new GUIContent("소환물 1개당 물리 방어력 증가율 (%)", "아군 소환물 1개당 증가하는 물리 방어력의 기본 추천값입니다. 실제 수치는 캐릭터 데이터의 패시브 개별 수치에서 조정할 수 있습니다."));

            EditorGUILayout.PropertyField(magicalDefensePerSummonPercent, new GUIContent("소환물 1개당 마법 방어력 증가율 (%)", "아군 소환물 1개당 증가하는 마법 방어력의 기본 추천값입니다. 실제 수치는 캐릭터 데이터의 패시브 개별 수치에서 조정할 수 있습니다."));
        }
    }
}