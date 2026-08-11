using EndlessGuard.Unit.Data;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    [CustomEditor(typeof(DefenseAuraSO))]
    [CanEditMultipleObjects]
    public sealed class DefenseAuraSOEditor : PassiveDataSOEditor
    {
        private SerializedProperty physicalDefenseBonusPercent;
        private SerializedProperty magicalDefenseBonusPercent;

        protected override void OnEnable()
        {
            base.OnEnable();

            physicalDefenseBonusPercent = serializedObject.FindProperty("physicalDefenseBonusPercent");
            magicalDefenseBonusPercent = serializedObject.FindProperty("magicalDefenseBonusPercent");
        }

        protected override void DrawSpecificFields()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("방어 오라 기본값", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(physicalDefenseBonusPercent, new GUIContent("물리 방어력 증가율 (%)", "비호자가 살아있는 동안 아군 몬스터에게 적용하는 기본 추천 물리 방어력 증가율입니다. 실제 수치는 몬스터 데이터의 패시브 개별 수치에서 조정할 수 있습니다."));
            EditorGUILayout.PropertyField(magicalDefenseBonusPercent, new GUIContent("마법 방어력 증가율 (%)", "비호자가 살아있는 동안 아군 몬스터에게 적용하는 기본 추천 마법 방어력 증가율입니다. 실제 수치는 몬스터 데이터의 패시브 개별 수치에서 조정할 수 있습니다."));

            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox("방어 오라는 비호자가 필드에 살아있는 동안 모든 살아있는 아군 몬스터와 비호자 자신에게 적용됩니다. 실제 런타임에서는 등장·사망 등의 상태 변화 시 갱신하는 방식으로 구현합니다.", MessageType.Info);
        }
    }
}