using EndlessGuard.Unit.Data;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    [CustomEditor(typeof(RandomAttackSO))]
    [CanEditMultipleObjects]
    public sealed class RandomAttackSOEditor : PassiveDataSOEditor
    {
        private SerializedProperty randomTargetCount;

        protected override void OnEnable()
        {
            base.OnEnable();

            randomTargetCount = serializedObject.FindProperty("randomTargetCount");
        }

        protected override void DrawSpecificFields()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("무작위 다중 공격 기본값", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(randomTargetCount, new GUIContent("무작위 공격 대상 수", "기본 공격 시 필드에 살아있는 유효한 캐릭터 중 중복 없이 무작위로 선택할 기본 추천 대상 수입니다. 실제 수치는 몬스터 데이터의 패시브 개별 수치에서 조정할 수 있습니다."));

            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox("대상 수보다 살아있는 캐릭터가 적으면 현재 존재하는 캐릭터까지만 공격하며, 한 번의 기본 공격에서 같은 캐릭터를 중복 선택하지 않습니다.", MessageType.Info);
        }
    }
}