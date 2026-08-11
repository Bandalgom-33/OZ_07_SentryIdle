using EndlessGuard.Unit.Data;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    [CustomEditor(typeof(HeavyArmorSO))]
    [CanEditMultipleObjects]
    public sealed class HeavyArmorSOEditor : PassiveDataSOEditor
    {
        private SerializedProperty physicalDefenseBonusPercent;
        private SerializedProperty magicalDefenseBonusPercent;
        private SerializedProperty moveSpeedReductionPercent;

        protected override void OnEnable()
        {
            base.OnEnable();

            physicalDefenseBonusPercent = serializedObject.FindProperty("physicalDefenseBonusPercent");
            magicalDefenseBonusPercent = serializedObject.FindProperty("magicalDefenseBonusPercent");
            moveSpeedReductionPercent = serializedObject.FindProperty("moveSpeedReductionPercent");
        }

        protected override void DrawSpecificFields()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("중갑 방어력 기본값", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(physicalDefenseBonusPercent, new GUIContent("물리 방어력 증가율 (%)", "중갑 패시브로 증가하는 기본 추천 물리 방어력 비율입니다. 실제 수치는 몬스터 데이터의 패시브 개별 수치에서 조정할 수 있습니다."));
            EditorGUILayout.PropertyField(magicalDefenseBonusPercent, new GUIContent("마법 방어력 증가율 (%)", "중갑 패시브로 증가하는 기본 추천 마법 방어력 비율입니다. 실제 수치는 몬스터 데이터의 패시브 개별 수치에서 조정할 수 있습니다."));

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("중갑 이동속도 기본값", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(moveSpeedReductionPercent, new GUIContent("이동속도 감소율 (%)", "중갑 패시브로 감소하는 기본 추천 이동속도 비율입니다. 실제 수치는 몬스터 데이터의 패시브 개별 수치에서 조정할 수 있습니다."));
        }
    }
}