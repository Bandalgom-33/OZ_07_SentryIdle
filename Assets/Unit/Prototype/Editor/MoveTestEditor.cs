using EndlessGuard.Unit.Prototype;
using EndlessGuard.Unit.Runtime;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    [CustomEditor(typeof(MoveTest))]
    public sealed class MoveTestEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            MoveTest test = (MoveTest)target;

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("이동 검증 상태", EditorStyles.boldLabel);
            DrawState(test);

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("검증 버튼은 Play 상태에서 사용할 수 있습니다.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(8f);

            if (GUILayout.Button("이동 검증 준비"))
            {
                Execute(test.Setup);
            }

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("이동 시작"))
            {
                Execute(test.StartMove);
            }

            if (GUILayout.Button("이동 정지"))
            {
                Execute(test.StopMove);
            }

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawState(MoveTest test)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EnemyMove move = test.EnemyMove;
                EditorGUILayout.ObjectField(new GUIContent("검증 몬스터"), move == null ? null : move.gameObject, typeof(GameObject), true);
                EditorGUILayout.Toggle(new GUIContent("이동 실행 중"), test.IsRunning);
                EditorGUILayout.Toggle(new GUIContent("저지됨"), move != null && move.IsBlocked);
                EditorGUILayout.Toggle(new GUIContent("출구 도달"), test.ReachedGoal);
                EditorGUILayout.IntField(new GUIContent("다음 경로 인덱스"), move == null ? 0 : move.NodeIndex);
                EditorGUILayout.IntField(new GUIContent("전체 경로 수"), move == null ? 0 : move.NodeCount);
                EditorGUILayout.Vector3Field(new GUIContent("현재 월드 위치"), move == null ? Vector3.zero : move.transform.position);

                CombatGridPosition grid = move == null ? null : move.GetComponent<CombatGridPosition>();
                EditorGUILayout.Vector2IntField(new GUIContent("현재 타일 좌표"), grid == null ? Vector2Int.zero : grid.TileCoordinate);
                EditorGUILayout.EnumPopup(new GUIContent("현재 방향"), grid == null ? EndlessGuard.Unit.Data.GridFacingDirection.North : grid.FacingDirection);
                EditorGUILayout.TextArea(string.IsNullOrWhiteSpace(test.LastMessage) ? "이동 검증 결과가 없습니다." : test.LastMessage);
            }
        }

        private void Execute(System.Action action)
        {
            action.Invoke();
            Repaint();
        }
    }
}