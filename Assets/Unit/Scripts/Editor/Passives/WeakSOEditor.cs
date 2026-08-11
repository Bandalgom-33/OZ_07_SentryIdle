using EndlessGuard.Unit.Data;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    [CustomEditor(typeof(WeakSO))]
    [CanEditMultipleObjects]
    public sealed class WeakSOEditor : PassiveDataSOEditor
    {
        private SerializedProperty physicalDefenseReductionPercent;
        private SerializedProperty magicalDefenseReductionPercent;
        private SerializedProperty durationSeconds;

        protected override void OnEnable()
        {
            base.OnEnable();

            physicalDefenseReductionPercent = serializedObject.FindProperty("physicalDefenseReductionPercent");
            magicalDefenseReductionPercent = serializedObject.FindProperty("magicalDefenseReductionPercent");
            durationSeconds = serializedObject.FindProperty("durationSeconds");
        }

        protected override void DrawSpecificFields()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("방어력 감소 기본값", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(physicalDefenseReductionPercent, new GUIContent("물리 방어력 감소율 (%)", "기본 공격 적중 시 대상에게 적용할 물리 방어력 감소율의 기본 추천값입니다. 실제 수치는 캐릭터 데이터의 패시브 개별 수치에서 조정할 수 있습니다."));
            EditorGUILayout.PropertyField(magicalDefenseReductionPercent, new GUIContent("마법 방어력 감소율 (%)", "기본 공격 적중 시 대상에게 적용할 마법 방어력 감소율의 기본 추천값입니다. 실제 수치는 캐릭터 데이터의 패시브 개별 수치에서 조정할 수 있습니다."));
            EditorGUILayout.PropertyField(durationSeconds, new GUIContent("지속시간 (초)", "방어력 감소 효과가 유지되는 시간의 기본 추천값입니다. 실제 수치는 캐릭터 데이터의 패시브 개별 수치에서 조정할 수 있습니다."));
        }
    }
}