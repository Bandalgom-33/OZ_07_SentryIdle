using EndlessGuard.Unit.Data;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    [CustomEditor(typeof(SizeDamagePassiveSO))]
    [CanEditMultipleObjects]
    public sealed class SizeDamagePassiveSOEditor : PassiveDataSOEditor
    {
        private SerializedProperty targetSize;
        private SerializedProperty bonusDamagePercent;

        protected override void OnEnable()
        {
            base.OnEnable();

            targetSize = serializedObject.FindProperty("targetSize");
            bonusDamagePercent = serializedObject.FindProperty("bonusDamagePercent");
        }

        protected override void DrawSpecificFields()
        {
            EditorGUILayout.Space(8f);

            EditorGUILayout.PropertyField(
                targetSize,
                new GUIContent("대상 몬스터 크기", "추가 피해를 적용할 몬스터 크기입니다."));

            EditorGUILayout.PropertyField(
                bonusDamagePercent,
                new GUIContent("추가 피해 비율 (%)", "100을 입력하면 기본 피해에 100%가 추가되어 최종 200% 피해가 됩니다."));

            if (!targetSize.hasMultipleDifferentValues && (EnemySize)targetSize.intValue == EnemySize.None)
            {
                EditorGUILayout.HelpBox("추가 피해를 적용할 몬스터 크기를 선택하세요.", MessageType.Warning);
            }
        }
    }
}