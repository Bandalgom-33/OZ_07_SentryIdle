using EndlessGuard.Unit.Data;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    [CustomEditor(typeof(FrontlineCommandSO))]
    [CanEditMultipleObjects]
    public sealed class FrontlineCommandSOEditor : PassiveDataSOEditor
    {
        private SerializedProperty attackSpeedBonusPercent;

        protected override void OnEnable()
        {
            base.OnEnable();
            attackSpeedBonusPercent = serializedObject.FindProperty("attackSpeedBonusPercent");
        }

        protected override void DrawSpecificFields()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("전선 지휘 기본값", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(attackSpeedBonusPercent, new GUIContent("공격속도 증가율 (%)", "아군 소환물이 필드에 1개 이상 존재할 때 자신의 공격속도가 증가하는 기본 추천 비율입니다. 실제 수치는 캐릭터 데이터의 패시브 개별 수치에서 조정할 수 있습니다."));
            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox("아군 소환물이 1개 이상이면 효과가 활성화되고, 소환물 수가 늘어나도 중첩되지 않습니다. 마지막 아군 소환물이 사라지면 효과가 즉시 해제됩니다.", MessageType.Info);
        }
    }
}