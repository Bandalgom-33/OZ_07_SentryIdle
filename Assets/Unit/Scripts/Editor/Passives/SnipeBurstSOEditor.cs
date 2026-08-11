using EndlessGuard.Unit.Data;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    [CustomEditor(typeof(SnipeBurstSO))]
    [CanEditMultipleObjects]
    public sealed class SnipeBurstSOEditor : PassiveDataSOEditor
    {
        private SerializedProperty burstAttackCount;
        private SerializedProperty forcedMoveSeconds;

        protected override void OnEnable()
        {
            base.OnEnable();

            burstAttackCount = serializedObject.FindProperty("burstAttackCount");
            forcedMoveSeconds = serializedObject.FindProperty("forcedMoveSeconds");
        }

        protected override void DrawSpecificFields()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("저격 연속 공격 기본값", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(burstAttackCount, new GUIContent("연속 공격 횟수", "공격 사거리 안에서 가장 먼 캐릭터를 선택한 뒤 연속으로 공격하는 기본 추천 횟수입니다. 실제 수치는 몬스터 데이터의 패시브 개별 수치에서 조정할 수 있습니다."));
            EditorGUILayout.PropertyField(forcedMoveSeconds, new GUIContent("공격 후 강제 이동 시간 (초)", "연속 공격을 끝낸 뒤 다시 공격 대상을 탐색하기 전에 강제로 이동하는 기본 추천 시간입니다. 실제 수치는 몬스터 데이터의 패시브 개별 수치에서 조정할 수 있습니다."));

            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox("저격병은 기존 공격 사거리 안에 있는 캐릭터 중 가장 먼 대상을 선택합니다. 연속 공격이 끝나면 설정된 시간 동안 강제로 이동한 뒤 다시 대상을 탐색합니다.", MessageType.Info);
        }
    }
}