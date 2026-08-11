using EndlessGuard.Unit.Data;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    [CustomEditor(typeof(AttackSlowSO))]
    [CanEditMultipleObjects]
    public sealed class AttackSlowSOEditor : PassiveDataSOEditor
    {
        private SerializedProperty attackSpeedReductionPercent;
        private SerializedProperty durationSeconds;

        protected override void OnEnable()
        {
            base.OnEnable();

            attackSpeedReductionPercent = serializedObject.FindProperty("attackSpeedReductionPercent");
            durationSeconds = serializedObject.FindProperty("durationSeconds");
        }

        protected override void DrawSpecificFields()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("공격속도 감소 기본값", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(attackSpeedReductionPercent, new GUIContent("공격속도 감소율 (%)", "기본 공격 적중 시 대상 캐릭터에게 적용할 기본 추천 공격속도 감소율입니다. 실제 수치는 몬스터 데이터의 패시브 개별 수치에서 조정할 수 있습니다."));
            EditorGUILayout.PropertyField(durationSeconds, new GUIContent("지속시간 (초)", "공격속도 감소 효과가 유지되는 기본 추천 시간입니다. 실제 수치는 몬스터 데이터의 패시브 개별 수치에서 조정할 수 있습니다."));
        }
    }
}