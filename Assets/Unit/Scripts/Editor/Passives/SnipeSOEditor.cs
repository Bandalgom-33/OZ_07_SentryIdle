using EndlessGuard.Unit.Data;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    [CustomEditor(typeof(SnipeSO))]
    [CanEditMultipleObjects]
    public sealed class SnipeSOEditor : PassiveDataSOEditor
    {
        private SerializedProperty targetSize;
        private SerializedProperty bonusDamagePercent;
        private SerializedProperty damagePerDistancePercent;
        private SerializedProperty maxDistanceDamagePercent;

        protected override void OnEnable()
        {
            base.OnEnable();

            targetSize = serializedObject.FindProperty("targetSize");
            bonusDamagePercent = serializedObject.FindProperty("bonusDamagePercent");
            damagePerDistancePercent = serializedObject.FindProperty("damagePerDistancePercent");
            maxDistanceDamagePercent = serializedObject.FindProperty("maxDistanceDamagePercent");
        }

        protected override void DrawSpecificFields()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("저격 대상 조건", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(targetSize, new GUIContent("대상 몬스터 크기", "저격수 패시브의 추가 피해를 적용할 몬스터 크기입니다."));

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("저격 추가 피해 기본값", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(bonusDamagePercent, new GUIContent("대상 기본 추가 피해율 (%)", "조건에 맞는 몬스터에게 기본적으로 적용하는 추가 피해율입니다. 실제 수치는 캐릭터 데이터의 패시브 개별 수치에서 조정할 수 있습니다."));
            EditorGUILayout.PropertyField(damagePerDistancePercent, new GUIContent("거리 1당 추가 피해율 (%)", "공격자와 대상 사이의 거리 1당 추가되는 피해율입니다. 실제 수치는 캐릭터 데이터의 패시브 개별 수치에서 조정할 수 있습니다."));
            EditorGUILayout.PropertyField(maxDistanceDamagePercent, new GUIContent("거리 추가 피해 최대치 (%)", "거리로 증가할 수 있는 추가 피해율의 최대치입니다. 실제 수치는 캐릭터 데이터의 패시브 개별 수치에서 조정할 수 있습니다."));
        }
    }
}