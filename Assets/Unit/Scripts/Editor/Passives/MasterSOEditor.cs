using EndlessGuard.Unit.Data;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    [CustomEditor(typeof(MasterSO))]
    [CanEditMultipleObjects]
    public sealed class MasterSOEditor : PassiveDataSOEditor
    {
        private SerializedProperty statType;
        private SerializedProperty statBonusPercent;

        protected override void OnEnable()
        {
            base.OnEnable();

            statType = serializedObject.FindProperty("statType");
            statBonusPercent = serializedObject.FindProperty("statBonusPercent");
        }

        protected override void DrawSpecificFields()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("마스터 능력치 설정", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(statType, new GUIContent("지정 전투 능력치", "마스터 패시브로 증가시킬 전투 능력치입니다."));
            EditorGUILayout.PropertyField(statBonusPercent, new GUIContent("지정 능력치 증가율 (%)", "선택한 전투 능력치에 적용할 기본 추천 증가율입니다. 실제 수치는 캐릭터 데이터의 패시브 개별 수치에서 조정할 수 있습니다."));

            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox("마스터 패시브는 지상·공중 몬스터를 모두 기본 공격 대상으로 허용합니다. 이 공격 대상 확장은 별도의 수치 설정 없이 마스터 패시브 기능 자체로 적용합니다.", MessageType.Info);
        }
    }
}