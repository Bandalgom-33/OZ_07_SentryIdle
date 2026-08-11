using EndlessGuard.Unit.Data;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    [CustomEditor(typeof(LifeStealSO))]
    [CanEditMultipleObjects]
    public sealed class LifeStealSOEditor : PassiveDataSOEditor
    {
        private SerializedProperty lifeStealPercent;

        protected override void OnEnable()
        {
            base.OnEnable();

            lifeStealPercent = serializedObject.FindProperty("lifeStealPercent");
        }

        protected override void DrawSpecificFields()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("흡혈 기본값", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(lifeStealPercent, new GUIContent("흡혈 비율 (%)", "기본 공격으로 실제 적용한 피해량 중 자신의 HP로 회복하는 기본 추천 비율입니다. 실제 수치는 몬스터 데이터의 패시브 개별 수치에서 조정할 수 있습니다."));

            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox("흡혈량은 계산 피해가 아니라 대상에게 실제로 적용된 피해량을 기준으로 계산합니다. 따라서 오버킬 피해는 흡혈량에 포함되지 않습니다.", MessageType.Info);
        }
    }
}