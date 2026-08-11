using EndlessGuard.Unit.Prototype;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    [CustomEditor(typeof(RangeTest))]
    public sealed class RangeTestEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            RangeTest test = (RangeTest)target;

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("InRange 원거리 검증 상태", EditorStyles.boldLabel);
            DrawState(test);

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("검증 버튼은 Play 상태에서 사용할 수 있습니다.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("검증 방식 선택", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("수동 제거 검증 준비"))
            {
                Execute(test.SetupManualTest);
            }

            if (GUILayout.Button("자연 사망 검증 준비"))
            {
                Execute(test.SetupDeathTest);
            }

            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("원거리 이동 시작"))
            {
                Execute(test.StartTest);
            }

            using (new EditorGUI.DisabledScope(!test.IsManualMode || !test.AttackOccurred || test.TargetLost))
            {
                if (GUILayout.Button("대상 제거 및 이동 재개"))
                {
                    Execute(test.RemoveTarget);
                }
            }

            if (GUILayout.Button("원거리 검증 정지"))
            {
                Execute(test.StopTest);
            }

            if (GUILayout.Button("원거리 검증 초기화"))
            {
                Execute(test.ResetResult);
            }
        }

        private static void DrawState(RangeTest test)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(new GUIContent("캐릭터"), test.Unit == null ? null : test.Unit.gameObject, typeof(GameObject), true);
                EditorGUILayout.ObjectField(new GUIContent("원거리 몬스터"), test.Enemy == null ? null : test.Enemy.gameObject, typeof(GameObject), true);
                EditorGUILayout.TextField(new GUIContent("검증 방식"), test.LossModeName);

                EditorGUILayout.Space(4f);
                EditorGUILayout.Toggle(new GUIContent("검증 준비 완료"), test.IsReady);
                EditorGUILayout.Toggle(new GUIContent("검증 실행 중"), test.IsRunning);
                EditorGUILayout.Toggle(new GUIContent("공격 정지 감지"), test.AttackPauseDetected);
                EditorGUILayout.Toggle(new GUIContent("원거리 공격 발생"), test.AttackOccurred);
                EditorGUILayout.Toggle(new GUIContent("저지 없이 공격"), test.NotBlockedAtPause);

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("대상 소실", EditorStyles.miniBoldLabel);
                EditorGUILayout.Toggle(new GUIContent("수동 제거"), test.TargetRemoved);
                EditorGUILayout.Toggle(new GUIContent("자연 사망"), test.TargetDied);
                EditorGUILayout.Toggle(new GUIContent("대상 소실 확인"), test.TargetLost);

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("이동 재개", EditorStyles.miniBoldLabel);
                EditorGUILayout.Toggle(new GUIContent("공격 정지 해제"), test.AttackPauseReleased);
                EditorGUILayout.Toggle(new GUIContent("이동 재개"), test.MovementResumed);
                EditorGUILayout.Toggle(new GUIContent("출구 도달"), test.GoalReached);
                EditorGUILayout.FloatField(new GUIContent("현재 단계 경과 시간"), test.PhaseElapsedSeconds);

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("공격 확인", EditorStyles.miniBoldLabel);
                EditorGUILayout.FloatField(new GUIContent("캐릭터 시작 HP"), test.UnitStartHp);
                EditorGUILayout.FloatField(new GUIContent("캐릭터 현재 HP"), test.UnitCurrentHp);
                EditorGUILayout.FloatField(new GUIContent("받은 피해"), test.AppliedDamage);
                EditorGUILayout.FloatField(new GUIContent("공격 정지 거리"), test.PauseWorldDistance);
                EditorGUILayout.Vector3Field(new GUIContent("공격 정지 위치"), test.PauseWorldPosition);
                EditorGUILayout.Vector3Field(new GUIContent("몬스터 현재 위치"), test.CurrentEnemyPosition);

                EditorGUILayout.Space(4f);
                EditorGUILayout.Toggle(new GUIContent("최종 검증 성공"), test.FinalPassed);
                EditorGUILayout.TextArea(string.IsNullOrWhiteSpace(test.Message) ? "InRange 원거리 검증 결과가 없습니다." : test.Message);
            }
        }

        private void Execute(System.Action action)
        {
            action.Invoke();
            Repaint();
        }
    }
}