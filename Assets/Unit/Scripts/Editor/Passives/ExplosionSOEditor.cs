using EndlessGuard.Unit.Data;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    [CustomEditor(typeof(ExplosionSO))]
    [CanEditMultipleObjects]
    public sealed class ExplosionSOEditor : PassiveDataSOEditor
    {
        private SerializedProperty damageType;
        private SerializedProperty explosionDamage;
        private SerializedProperty explosionRadiusTiles;

        protected override void OnEnable()
        {
            base.OnEnable();

            damageType = serializedObject.FindProperty("damageType");
            explosionDamage = serializedObject.FindProperty("explosionDamage");
            explosionRadiusTiles = serializedObject.FindProperty("explosionRadiusTiles");
        }

        protected override void DrawSpecificFields()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("사망 폭발 설정", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(damageType, new GUIContent("피해 유형", "사망 폭발이 캐릭터에게 적용하는 피해 유형입니다."));

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("사망 폭발 기본값", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(explosionDamage, new GUIContent("폭발 피해량", "사망 시 폭발 범위 안의 캐릭터에게 적용하는 기본 추천 피해량입니다. 실제 수치는 몬스터 데이터의 패시브 개별 수치에서 조정할 수 있습니다."));
            EditorGUILayout.PropertyField(explosionRadiusTiles, new GUIContent("폭발 반경 (타일)", "사망 위치를 기준으로 폭발 피해를 적용할 격자 반경입니다. 실제 수치는 몬스터 데이터의 패시브 개별 수치에서 조정할 수 있습니다."));

            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox("폭발 범위는 월드 물리 반경이 아니라 전투 격자 기준으로 판정합니다.", MessageType.Info);
        }
    }
}