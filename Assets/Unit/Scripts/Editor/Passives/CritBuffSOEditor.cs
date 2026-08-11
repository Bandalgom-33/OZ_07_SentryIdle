using EndlessGuard.Unit.Data;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    [CustomEditor(typeof(CritBuffSO))]
    [CanEditMultipleObjects]
    public sealed class CritBuffSOEditor : PassiveDataSOEditor
    {
        private SerializedProperty finalDamageBonusPercent;
        private SerializedProperty durationSeconds;

        protected override void OnEnable()
        {
            base.OnEnable();

            finalDamageBonusPercent = serializedObject.FindProperty("finalDamageBonusPercent");
            durationSeconds = serializedObject.FindProperty("durationSeconds");
        }

        protected override void DrawSpecificFields()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("치명타 후 피해 증가 기본값", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(
                finalDamageBonusPercent,
                new GUIContent(
                    "최종 피해 증가율 (%)",
                    "치명타 적중 후 적용되는 최종 피해 증가율의 기본 추천값입니다. 실제 수치는 캐릭터 데이터의 패시브 개별 수치에서 독립적으로 조정할 수 있습니다."));

            EditorGUILayout.PropertyField(
                durationSeconds,
                new GUIContent(
                    "지속시간 (초)",
                    "치명타 적중 후 최종 피해 증가 효과가 유지되는 시간의 기본 추천값입니다. 실제 수치는 캐릭터 데이터의 패시브 개별 수치에서 독립적으로 조정할 수 있습니다."));
        }
    }
}