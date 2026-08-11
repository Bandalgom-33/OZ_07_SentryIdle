using EndlessGuard.Unit.Data;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    [CustomEditor(typeof(CostGainPassiveSO))]
    [CanEditMultipleObjects]
    public sealed class CostGainPassiveSOEditor : PassiveDataSOEditor
    {
        private SerializedProperty trigger;
        private SerializedProperty summonCostGain;

        protected override void OnEnable()
        {
            base.OnEnable();

            trigger = serializedObject.FindProperty("trigger");
            summonCostGain = serializedObject.FindProperty("summonCostGain");
        }

        protected override void DrawSpecificFields()
        {
            EditorGUILayout.Space(8f);

            EditorGUILayout.LabelField("소환 코스트 획득 설정", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(
                trigger,
                new GUIContent(
                    "발동 조건",
                    "이 패시브가 소환 코스트를 획득하는 전투 이벤트입니다."));

            EditorGUILayout.PropertyField(
                summonCostGain,
                new GUIContent(
                    "기본 코스트 획득량",
                    "새 캐릭터에 처음 설정할 추천값입니다. 캐릭터별 실제 수치는 UnitDataSO의 패시브 개별 수치에서 독립적으로 조정합니다."));
        }
    }
}