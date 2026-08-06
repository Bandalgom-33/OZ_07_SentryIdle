using EndlessGuard.Unit.Prototype;
using EndlessGuard.Unit.Runtime;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    [CustomEditor(typeof(BlockTest))]
    public sealed class BlockTestEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            BlockTest test = (BlockTest)target;

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("저지 검증 상태", EditorStyles.boldLabel);
            DrawState(test);

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("검증 버튼은 Play 상태에서 사용할 수 있습니다.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(8f);

            if (GUILayout.Button("저지 검증 준비"))
            {
                Execute(test.Setup);
            }

            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("저지 연결", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("첫 번째 저지"))
            {
                Execute(test.BindFirst);
            }

            if (GUILayout.Button("두 번째 저지"))
            {
                Execute(test.BindSecond);
            }

            if (GUILayout.Button("세 번째 저지"))
            {
                Execute(test.BindThird);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("저지 해제", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("첫 번째 해제"))
            {
                Execute(test.ReleaseFirst);
            }

            if (GUILayout.Button("두 번째 해제"))
            {
                Execute(test.ReleaseSecond);
            }

            if (GUILayout.Button("세 번째 해제"))
            {
                Execute(test.ReleaseThird);
            }

            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("전체 저지 해제"))
            {
                Execute(test.ReleaseAll);
            }

            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("사망 자동 해제", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("첫 번째 몬스터 사망"))
            {
                Execute(test.KillFirst);
            }

            if (GUILayout.Button("두 번째 몬스터 사망"))
            {
                Execute(test.KillSecond);
            }

            if (GUILayout.Button("캐릭터 사망"))
            {
                Execute(test.KillUnit);
            }

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawState(BlockTest test)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                UnitBlock unitBlock = test.UnitBlock;
                EditorGUILayout.IntField(new GUIContent("최대 저지 수"), unitBlock == null ? 0 : unitBlock.MaxCount);
                EditorGUILayout.IntField(new GUIContent("현재 저지 수"), unitBlock == null ? 0 : unitBlock.Count);
                EditorGUILayout.Toggle(new GUIContent("저지 한도 도달"), unitBlock != null && unitBlock.IsFull);
                DrawEnemy("첫 번째 몬스터", test.FirstBlock);
                DrawEnemy("두 번째 몬스터", test.SecondBlock);
                DrawEnemy("세 번째 몬스터", test.ThirdBlock);
                EditorGUILayout.Toggle(new GUIContent("최근 실행 결과"), test.LastResult);
                EditorGUILayout.TextArea(string.IsNullOrWhiteSpace(test.LastMessage) ? "저지 검증 결과가 없습니다." : test.LastMessage);
            }
        }

        private static void DrawEnemy(string label, EnemyBlock enemy)
        {
            EditorGUILayout.ObjectField(new GUIContent(label), enemy == null ? null : enemy.gameObject, typeof(GameObject), true);
            EditorGUILayout.Toggle(new GUIContent($"{label} 저지됨"), enemy != null && enemy.IsBlocked);
            EditorGUILayout.ObjectField(new GUIContent($"{label} 저지 캐릭터"), enemy == null || enemy.Blocker == null ? null : enemy.Blocker.gameObject, typeof(GameObject), true);
        }

        private void Execute(System.Action action)
        {
            action.Invoke();
            Repaint();
        }
    }
}