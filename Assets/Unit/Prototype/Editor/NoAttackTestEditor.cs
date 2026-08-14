using EndlessGuard.Unit.Prototype;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    [CustomEditor(typeof(NoAttackTest))]
    public sealed class NoAttackTestEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            NoAttackTest test = (NoAttackTest)target;

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("비공격 몬스터 검증 상태", EditorStyles.boldLabel);
            DrawState(test);

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("검증 버튼은 Play 상태에서 사용할 수 있습니다.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(8f);

            if (GUILayout.Button("비공격 검증 준비"))
            {
                Execute(test.SetupTest);
            }

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("비공격 이동 시작"))
            {
                Execute(test.StartTest);
            }

            if (GUILayout.Button("비공격 검증 정지"))
            {
                Execute(test.StopTest);
            }

            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("비공격 검증 초기화"))
            {
                Execute(test.ResetResult);
            }
        }

        private static void DrawState(NoAttackTest test)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(new GUIContent("캐릭터"), test.Unit == null ? null : test.Unit.gameObject, typeof(GameObject), true);
                EditorGUILayout.ObjectField(new GUIContent("비공격 몬스터"), test.Enemy == null ? null : test.Enemy.gameObject, typeof(GameObject), true);

                EditorGUILayout.Space(4f);
                EditorGUILayout.Toggle(new GUIContent("검증 준비 완료"), test.IsReady);
                EditorGUILayout.Toggle(new GUIContent("검증 실행 중"), test.IsRunning);
                EditorGUILayout.Toggle(new GUIContent("지상 층 판정"), test.GroundLayerPassed);
                EditorGUILayout.Toggle(new GUIContent("공격 사거리 진입"), test.EnteredRange);
                EditorGUILayout.Toggle(new GUIContent("저지되지 않음"), test.NeverBlocked);
                EditorGUILayout.Toggle(new GUIContent("공격 정지 없음"), test.NeverAttackPaused);

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("비공격 확인", EditorStyles.miniBoldLabel);
                EditorGUILayout.Toggle(new GUIContent("공격하지 않음"), test.NeverAttacked);
                EditorGUILayout.IntField(new GUIContent("공격 횟수"), test.AttackCount);
                EditorGUILayout.Toggle(new GUIContent("캐릭터 HP 변화 없음"), test.HpUnchanged);
                EditorGUILayout.FloatField(new GUIContent("캐릭터 시작 HP"), test.UnitStartHp);
                EditorGUILayout.FloatField(new GUIContent("캐릭터 현재 HP"), test.UnitCurrentHp);
                EditorGUILayout.FloatField(new GUIContent("최소 접근 거리"), test.MinimumDistance);

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("이동 확인", EditorStyles.miniBoldLabel);
                EditorGUILayout.Toggle(new GUIContent("캐릭터 옆 통과"), test.PassedUnit);
                EditorGUILayout.Toggle(new GUIContent("출구 도달"), test.GoalReached);
                EditorGUILayout.Vector3Field(new GUIContent("몬스터 현재 위치"), test.CurrentEnemyPosition);
                EditorGUILayout.FloatField(new GUIContent("경과 시간"), test.ElapsedSeconds);

                EditorGUILayout.Space(4f);
                EditorGUILayout.Toggle(new GUIContent("최종 검증 성공"), test.FinalPassed);
                EditorGUILayout.TextArea(string.IsNullOrWhiteSpace(test.Message) ? "비공격 몬스터 검증 결과가 없습니다." : test.Message);
            }
        }

        private void Execute(System.Action action)
        {
            action.Invoke();
            Repaint();
        }
    }
}