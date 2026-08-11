using EndlessGuard.Unit.Data;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    [CustomEditor(typeof(SizeAttackSO))]
    [CanEditMultipleObjects]
    public sealed class SizeAttackSOEditor : PassiveDataSOEditor
    {
        private SerializedProperty targetSize;
        private SerializedProperty attackBonusPercent;

        protected override void OnEnable()
        {
            base.OnEnable();

            targetSize = serializedObject.FindProperty("targetSize");
            attackBonusPercent = serializedObject.FindProperty("attackBonusPercent");
        }

        protected override void DrawSpecificFields()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("크기 대상 공격력 증가 설정", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(targetSize, new GUIContent("대상 몬스터 크기", "공격력 증가 효과를 적용할 몬스터 크기입니다."));
            EditorGUILayout.PropertyField(attackBonusPercent, new GUIContent("공격력 증가율 (%)", "해당 크기의 몬스터를 공격할 때 적용하는 기본 추천 공격력 증가율입니다. 실제 수치는 캐릭터 데이터의 패시브 개별 수치에서 조정할 수 있습니다."));
        }
    }
}