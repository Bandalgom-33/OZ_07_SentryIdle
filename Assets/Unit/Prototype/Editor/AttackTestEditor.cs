using EndlessGuard.Unit.Prototype;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    [CustomEditor(typeof(AttackTest))]
    public sealed class AttackTestEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            AttackTest test = (AttackTest)target;

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("자동 공격 검증 상태", EditorStyles.boldLabel);
            DrawState(test);

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("검증 버튼은 Play 상태에서 사용할 수 있습니다.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(8f);

            if (GUILayout.Button("자동 공격 검증 준비"))
            {
                Execute(test.SetupTest);
            }

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("자동 공격 시작"))
            {
                Execute(test.StartTest);
            }

            if (GUILayout.Button("자동 공격 정지"))
            {
                Execute(test.StopTest);
            }

            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("공격 검증 초기화"))
            {
                Execute(test.ResetResult);
            }
        }

        private static void DrawState(AttackTest test)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(new GUIContent("공격 대상"), test.Target == null ? null : test.Target.gameObject, typeof(GameObject), true);
                EditorGUILayout.Toggle(new GUIContent("검증 준비 완료"), test.IsReady);
                EditorGUILayout.Toggle(new GUIContent("자동 공격 실행 중"), test.IsRunning);
                EditorGUILayout.FloatField(new GUIContent("경과 시간"), test.ElapsedSeconds);
                EditorGUILayout.FloatField(new GUIContent("시작 HP"), test.StartHp);
                EditorGUILayout.FloatField(new GUIContent("현재 HP"), test.CurrentHp);
                EditorGUILayout.FloatField(new GUIContent("적용 피해"), test.AppliedDamage);
                EditorGUILayout.IntField(new GUIContent("첫 번째 공격 횟수"), test.FirstAttackCount);
                EditorGUILayout.IntField(new GUIContent("두 번째 공격 횟수"), test.SecondAttackCount);
                EditorGUILayout.IntField(new GUIContent("세 번째 공격 횟수"), test.ThirdAttackCount);
                EditorGUILayout.Toggle(new GUIContent("최종 검증 성공"), test.FinalPassed);
                EditorGUILayout.TextArea(string.IsNullOrWhiteSpace(test.Message) ? "자동 공격 검증 결과가 없습니다." : test.Message);
            }
        }

        private void Execute(System.Action action)
        {
            action.Invoke();
            Repaint();
        }
    }
}