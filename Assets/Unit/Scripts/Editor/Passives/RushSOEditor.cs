using EndlessGuard.Unit.Data;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    [CustomEditor(typeof(RushSO))]
    [CanEditMultipleObjects]
    public sealed class RushSOEditor : PassiveDataSOEditor
    {
        private SerializedProperty bonusMoveSpeedPercent;

        protected override void OnEnable()
        {
            base.OnEnable();

            bonusMoveSpeedPercent = serializedObject.FindProperty("bonusMoveSpeedPercent");
        }

        protected override void DrawSpecificFields()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("돌격 이동속도 기본값", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(bonusMoveSpeedPercent, new GUIContent("이동속도 증가율 (%)", "몬스터가 처음으로 저지되기 전까지 적용할 기본 추천 이동속도 증가율입니다. 실제 수치는 몬스터 데이터의 패시브 개별 수치에서 조정할 수 있습니다."));

            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox("이 이동속도 보너스는 몬스터가 처음으로 저지되는 순간 해제되며, 이후 저지가 풀려도 다시 활성화되지 않습니다.", MessageType.Info);
        }
    }
}