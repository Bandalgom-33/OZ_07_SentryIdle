using EndlessGuard.Unit.Data;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    [CustomEditor(typeof(HealSO))]
    [CanEditMultipleObjects]
    public sealed class HealSOEditor : PassiveDataSOEditor
    {
        private SerializedProperty healAmount;
        private SerializedProperty healIntervalSeconds;

        protected override void OnEnable()
        {
            base.OnEnable();

            healAmount = serializedObject.FindProperty("healAmount");
            healIntervalSeconds = serializedObject.FindProperty("healIntervalSeconds");
        }

        protected override void DrawSpecificFields()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("아군 회복 기본값", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(healAmount, new GUIContent("HP 회복량", "회복이 발동했을 때 대상 아군 몬스터에게 적용하는 기본 추천 회복량입니다. 실제 수치는 몬스터 데이터의 패시브 개별 수치에서 조정할 수 있습니다."));
            EditorGUILayout.PropertyField(healIntervalSeconds, new GUIContent("회복 주기 (초)", "아군 회복 효과가 반복해서 발동하는 기본 추천 주기입니다. 실제 수치는 몬스터 데이터의 패시브 개별 수치에서 조정할 수 있습니다."));

            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox("회복 대상은 살아있는 아군 몬스터 중 현재 HP 비율이 가장 낮은 몬스터 1명입니다.", MessageType.Info);
        }
    }
}