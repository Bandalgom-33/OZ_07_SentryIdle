using EndlessGuard.Unit.Prototype;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    [CustomEditor(typeof(AirTest))]
    public sealed class AirTestEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            AirTest test = (AirTest)target;

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("공중 이동 공격 검증 상태", EditorStyles.boldLabel);
            DrawState(test);

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("검증 버튼은 Play 상태에서 사용할 수 있습니다.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(8f);

            if (GUILayout.Button("공중 검증 준비"))
            {
                Execute(test.SetupTest);
            }

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("공중 이동 시작"))
            {
                Execute(test.StartTest);
            }

            if (GUILayout.Button("공중 검증 정지"))
            {
                Execute(test.StopTest);
            }

            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("공중 검증 초기화"))
            {
                Execute(test.ResetResult);
            }
        }

        private static void DrawState(AirTest test)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(new GUIContent("캐릭터"), test.Unit == null ? null : test.Unit.gameObject, typeof(GameObject), true);
                EditorGUILayout.ObjectField(new GUIContent("공중 몬스터"), test.Enemy == null ? null : test.Enemy.gameObject, typeof(GameObject), true);

                EditorGUILayout.Space(4f);
                EditorGUILayout.Toggle(new GUIContent("검증 준비 완료"), test.IsReady);
                EditorGUILayout.Toggle(new GUIContent("검증 실행 중"), test.IsRunning);
                EditorGUILayout.Toggle(new GUIContent("공중 층 판정"), test.AirLayerPassed);
                EditorGUILayout.Toggle(new GUIContent("저지되지 않음"), test.NeverBlocked);
                EditorGUILayout.Toggle(new GUIContent("공격 중 정지 없음"), test.NeverAttackPaused);

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("이동 공격", EditorStyles.miniBoldLabel);
                EditorGUILayout.Toggle(new GUIContent("공격 발생"), test.AttackOccurred);
                EditorGUILayout.Toggle(new GUIContent("공격하면서 이동"), test.MovedWhileAttacking);
                EditorGUILayout.IntField(new GUIContent("공격 횟수"), test.AttackCount);
                EditorGUILayout.Vector3Field(new GUIContent("첫 공격 위치"), test.FirstAttackPosition);
                EditorGUILayout.Vector3Field(new GUIContent("현재 몬스터 위치"), test.CurrentEnemyPosition);

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("통과·사거리 이탈", EditorStyles.miniBoldLabel);
                EditorGUILayout.Toggle(new GUIContent("캐릭터 위치 통과"), test.PassedUnit);
                EditorGUILayout.Toggle(new GUIContent("공격 사거리 이탈"), test.RangeExited);
                EditorGUILayout.Toggle(new GUIContent("이탈 후 추가 공격 없음"), test.NoAttackAfterExit);
                EditorGUILayout.Toggle(new GUIContent("출구 도달"), test.GoalReached);

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("캐릭터 피해", EditorStyles.miniBoldLabel);
                EditorGUILayout.FloatField(new GUIContent("시작 HP"), test.UnitStartHp);
                EditorGUILayout.FloatField(new GUIContent("현재 HP"), test.UnitCurrentHp);
                EditorGUILayout.FloatField(new GUIContent("받은 피해"), test.AppliedDamage);
                EditorGUILayout.FloatField(new GUIContent("경과 시간"), test.ElapsedSeconds);

                EditorGUILayout.Space(4f);
                EditorGUILayout.Toggle(new GUIContent("최종 검증 성공"), test.FinalPassed);
                EditorGUILayout.TextArea(string.IsNullOrWhiteSpace(test.Message) ? "공중 이동 공격 검증 결과가 없습니다." : test.Message);
            }
        }

        private void Execute(System.Action action)
        {
            action.Invoke();
            Repaint();
        }
    }
}