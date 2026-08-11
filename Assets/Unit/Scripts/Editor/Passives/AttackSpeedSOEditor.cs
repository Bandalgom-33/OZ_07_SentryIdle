using EndlessGuard.Unit.Data;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    [CustomEditor(typeof(AttackSpeedSO))]
    [CanEditMultipleObjects]
    public sealed class AttackSpeedSOEditor : PassiveDataSOEditor
    {
        private SerializedProperty targetSize;
        private SerializedProperty attackSpeedBonusPercent;
        private SerializedProperty durationSeconds;

        protected override void OnEnable()
        {
            base.OnEnable();

            targetSize = serializedObject.FindProperty("targetSize");
            attackSpeedBonusPercent = serializedObject.FindProperty("attackSpeedBonusPercent");
            durationSeconds = serializedObject.FindProperty("durationSeconds");
        }

        protected override void DrawSpecificFields()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("공격속도 증가 조건", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(targetSize, new GUIContent("대상 몬스터 크기", "공격속도 증가 효과를 발동시키는 몬스터 크기입니다."));

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("공격속도 증가 기본값", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(attackSpeedBonusPercent, new GUIContent("공격속도 증가율 (%)", "조건에 맞는 몬스터에게 기본 공격이 적중했을 때 적용하는 기본 추천 공격속도 증가율입니다. 실제 수치는 캐릭터 데이터의 패시브 개별 수치에서 조정할 수 있습니다."));
            EditorGUILayout.PropertyField(durationSeconds, new GUIContent("지속시간 (초)", "공격속도 증가 효과가 유지되는 기본 추천 시간입니다. 실제 수치는 캐릭터 데이터의 패시브 개별 수치에서 조정할 수 있습니다."));
        }
    }
}