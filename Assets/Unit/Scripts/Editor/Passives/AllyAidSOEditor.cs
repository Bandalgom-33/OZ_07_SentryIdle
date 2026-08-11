using EndlessGuard.Unit.Data;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    [CustomEditor(typeof(AllyAidSO))]
    [CanEditMultipleObjects]
    public sealed class AllyAidSOEditor : PassiveDataSOEditor
    {
        private SerializedProperty shieldAmount;
        private SerializedProperty healAmount;
        private SerializedProperty skillGaugeGain;

        protected override void OnEnable()
        {
            base.OnEnable();

            shieldAmount = serializedObject.FindProperty("shieldAmount");
            healAmount = serializedObject.FindProperty("healAmount");
            skillGaugeGain = serializedObject.FindProperty("skillGaugeGain");
        }

        protected override void DrawSpecificFields()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("아군 지원 기본값", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(shieldAmount, new GUIContent("보호막량", "보호막 효과가 선택됐을 때 아군에게 부여하는 기본 추천 보호막량입니다. 실제 수치는 캐릭터 데이터의 패시브 개별 수치에서 조정할 수 있습니다."));

            EditorGUILayout.PropertyField(healAmount, new GUIContent("HP 회복량", "HP 회복 효과가 선택됐을 때 아군에게 회복하는 기본 추천 HP입니다. 실제 수치는 캐릭터 데이터의 패시브 개별 수치에서 조정할 수 있습니다."));

            EditorGUILayout.PropertyField(skillGaugeGain, new GUIContent("스킬게이지 회복량", "스킬게이지 회복 효과가 선택됐을 때 아군에게 부여하는 기본 추천 스킬게이지입니다. 실제 수치는 캐릭터 데이터의 패시브 개별 수치에서 조정할 수 있습니다."));
        }
    }
}