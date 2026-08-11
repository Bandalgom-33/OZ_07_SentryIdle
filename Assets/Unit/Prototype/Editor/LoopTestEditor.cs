using EndlessGuard.Unit.Prototype;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    [CustomEditor(typeof(LoopTest))]
    public sealed class LoopTestEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            LoopTest test = (LoopTest)target;

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("양방향 전투 검증 상태", EditorStyles.boldLabel);
            DrawState(test);

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("검증 버튼은 Play 상태에서 사용할 수 있습니다.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(8f);

            if (GUILayout.Button("양방향 전투 검증 준비"))
            {
                Execute(test.SetupTest);
            }

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("양방향 전투 시작"))
            {
                Execute(test.StartTest);
            }

            if (GUILayout.Button("양방향 전투 정지"))
            {
                Execute(test.StopTest);
            }

            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("양방향 검증 초기화"))
            {
                Execute(test.ResetResult);
            }
        }

        private static void DrawState(LoopTest test)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(new GUIContent("캐릭터"), test.Unit == null ? null : test.Unit.gameObject, typeof(GameObject), true);
                EditorGUILayout.ObjectField(new GUIContent("몬스터"), test.Enemy == null ? null : test.Enemy.gameObject, typeof(GameObject), true);

                EditorGUILayout.Space(4f);
                EditorGUILayout.Toggle(new GUIContent("검증 준비 완료"), test.IsReady);
                EditorGUILayout.Toggle(new GUIContent("양방향 전투 실행 중"), test.IsRunning);
                EditorGUILayout.Toggle(new GUIContent("몬스터 저지됨"), test.IsBlocked);
                EditorGUILayout.Toggle(new GUIContent("캐릭터 자동 공격"), test.UnitAttacked);
                EditorGUILayout.Toggle(new GUIContent("몬스터 자동 공격"), test.EnemyAttacked);
                EditorGUILayout.Toggle(new GUIContent("스킬 게이지 획득"), test.SkillGaugeGained);
                EditorGUILayout.Toggle(new GUIContent("출구 도달"), test.GoalReached);
                EditorGUILayout.FloatField(new GUIContent("경과 시간"), test.ElapsedSeconds);

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("캐릭터 상태", EditorStyles.miniBoldLabel);
                EditorGUILayout.FloatField(new GUIContent("시작 HP"), test.UnitStartHp);
                EditorGUILayout.FloatField(new GUIContent("현재 HP"), test.UnitCurrentHp);
                EditorGUILayout.FloatField(new GUIContent("받은 피해"), test.EnemyAppliedDamage);
                EditorGUILayout.FloatField(new GUIContent("시작 스킬 게이지"), test.StartSkillGauge);
                EditorGUILayout.FloatField(new GUIContent("현재 스킬 게이지"), test.CurrentSkillGauge);
                EditorGUILayout.FloatField(new GUIContent("획득 스킬 게이지"), test.GainedSkillGauge);

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("몬스터 상태", EditorStyles.miniBoldLabel);
                EditorGUILayout.FloatField(new GUIContent("시작 HP"), test.EnemyStartHp);
                EditorGUILayout.FloatField(new GUIContent("현재 HP"), test.EnemyCurrentHp);
                EditorGUILayout.FloatField(new GUIContent("받은 피해"), test.UnitAppliedDamage);

                EditorGUILayout.Space(4f);
                EditorGUILayout.Toggle(new GUIContent("최종 검증 성공"), test.FinalPassed);
                EditorGUILayout.TextArea(string.IsNullOrWhiteSpace(test.Message) ? "양방향 전투 검증 결과가 없습니다." : test.Message);
            }
        }

        private void Execute(System.Action action)
        {
            action.Invoke();
            Repaint();
        }
    }
}