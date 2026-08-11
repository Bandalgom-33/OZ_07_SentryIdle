using EndlessGuard.Unit.Data;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    [CustomEditor(typeof(ReflectSO))]
    [CanEditMultipleObjects]
    public sealed class ReflectSOEditor : PassiveDataSOEditor
    {
        private SerializedProperty damageReflectPercent;

        protected override void OnEnable()
        {
            base.OnEnable();

            damageReflectPercent = serializedObject.FindProperty("damageReflectPercent");
        }

        protected override void DrawSpecificFields()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("피해 반사 기본값", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(damageReflectPercent, new GUIContent("피해 반사율 (%)", "캐릭터에게 실제로 받은 피해량 중 공격자에게 되돌려 주는 기본 추천 비율입니다. 실제 수치는 몬스터 데이터의 패시브 개별 수치에서 조정할 수 있습니다."));

            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox("반사 피해는 몬스터에게 실제로 적용된 피해량을 기준으로 계산하며, 반사 피해 자체는 다시 반사되지 않습니다.", MessageType.Info);
        }
    }
}