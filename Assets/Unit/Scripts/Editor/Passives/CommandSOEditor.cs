using EndlessGuard.Unit.Data;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    [CustomEditor(typeof(CommandSO))]
    [CanEditMultipleObjects]
    public sealed class CommandSOEditor : PassiveDataSOEditor
    {
        private SerializedProperty attackBonusPercent;
        private SerializedProperty attackSpeedBonusPercent;

        protected override void OnEnable()
        {
            base.OnEnable();

            attackBonusPercent = serializedObject.FindProperty("attackBonusPercent");
            attackSpeedBonusPercent = serializedObject.FindProperty("attackSpeedBonusPercent");
        }

        protected override void DrawSpecificFields()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("지휘 오라 기본값", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(attackBonusPercent, new GUIContent("공격력 증가율 (%)", "지휘관이 살아있는 동안 아군 몬스터에게 적용하는 기본 추천 공격력 증가율입니다. 실제 수치는 몬스터 데이터의 패시브 개별 수치에서 조정할 수 있습니다."));
            EditorGUILayout.PropertyField(attackSpeedBonusPercent, new GUIContent("공격속도 증가율 (%)", "지휘관이 살아있는 동안 아군 몬스터에게 적용하는 기본 추천 공격속도 증가율입니다. 실제 수치는 몬스터 데이터의 패시브 개별 수치에서 조정할 수 있습니다."));

            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox("지휘 오라는 지휘관이 필드에 살아있는 동안 모든 살아있는 아군 몬스터에게 적용됩니다. 실제 런타임에서는 매 프레임 전체 몬스터를 검색하지 않고 등장·사망 등의 상태 변화 시 갱신하는 방식으로 구현합니다.", MessageType.Info);
        }
    }
}