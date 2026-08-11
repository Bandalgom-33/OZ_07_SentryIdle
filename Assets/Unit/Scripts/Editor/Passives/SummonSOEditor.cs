using EndlessGuard.Unit.Data;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    [CustomEditor(typeof(SummonSO))]
    [CanEditMultipleObjects]
    public sealed class SummonSOEditor : PassiveDataSOEditor
    {
        private SerializedProperty summonIntervalSeconds;
        private SerializedProperty summonCount;
        private SerializedProperty summonPrefab;

        protected override void OnEnable()
        {
            base.OnEnable();

            summonIntervalSeconds = serializedObject.FindProperty("summonIntervalSeconds");
            summonCount = serializedObject.FindProperty("summonCount");
            summonPrefab = serializedObject.FindProperty("summonPrefab");
        }

        protected override void DrawSpecificFields()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("주기적 소환 기본값", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(summonIntervalSeconds, new GUIContent("소환 주기 (초)", "소환 효과가 반복해서 발동하는 기본 추천 주기입니다. 실제 수치는 몬스터 데이터의 패시브 개별 수치에서 조정할 수 있습니다."));
            EditorGUILayout.PropertyField(summonCount, new GUIContent("한 번에 소환하는 수", "한 번의 소환 효과가 발동할 때 생성하는 기본 추천 소환물 수입니다. 실제 수치는 몬스터 데이터의 패시브 개별 수치에서 조정할 수 있습니다."));
            EditorGUILayout.PropertyField(summonPrefab, new GUIContent("기본 소환물 프리팹", "몬스터별 소환물 프리팹이 따로 설정되지 않았을 때 사용할 기본 소환물 프리팹입니다. 현재는 비어 있어도 정상입니다."));

            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox("소환물은 맵의 유효한 랜덤 위치에 생성되고 가장 가까운 살아있는 캐릭터를 추적해 공격합니다. 출구로 이동하지 않으며 출구 도달 이벤트도 발생시키지 않습니다.", MessageType.Info);

            if (summonPrefab.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("현재 기본 소환물 프리팹이 연결되지 않았습니다. 실제 소환물 제작 후 연결하거나 몬스터 데이터의 패시브 개별 에셋 참조에서 별도로 지정할 수 있습니다.", MessageType.Info);
            }
        }
    }
}