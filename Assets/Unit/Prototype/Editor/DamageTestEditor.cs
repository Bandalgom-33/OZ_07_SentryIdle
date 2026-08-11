using EndlessGuard.Unit.Prototype;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    [CustomEditor(typeof(DamageTest))]
    public sealed class DamageTestEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            DamageTest test = (DamageTest)target;

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("피해 숫자 Pop·Push 검증", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(new GUIContent("검증 캐릭터"), test.Target == null ? null : test.Target.gameObject, typeof(GameObject), true);

                EditorGUILayout.Space(4f);
                EditorGUILayout.Toggle(new GUIContent("검증 준비 완료"), test.IsReady);
                EditorGUILayout.Toggle(new GUIContent("검증 실행 중"), test.IsRunning);
                EditorGUILayout.Toggle(new GUIContent("연속 피해 입력 완료"), test.BurstComplete);

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("연속 숫자 확인", EditorStyles.miniBoldLabel);
                EditorGUILayout.IntField(new GUIContent("대상 최대 표시 수"), test.MaxNumbersPerTarget);
                EditorGUILayout.IntField(new GUIContent("적용 피해 횟수"), test.AppliedHitCount);
                EditorGUILayout.IntField(new GUIContent("최대 동시 숫자"), test.PeakActiveCount);
                EditorGUILayout.IntField(new GUIContent("연타 종료 활성 숫자"), test.ActiveCountAfterBurst);
                EditorGUILayout.Toggle(new GUIContent("표시 개수 제한 성공"), test.NumberLimitPassed);

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("풀 반환", EditorStyles.miniBoldLabel);
                EditorGUILayout.Toggle(new GUIContent("풀 반환 대기 시작"), test.PoolReturnStarted);
                EditorGUILayout.IntField(new GUIContent("현재 활성 숫자"), test.CurrentActiveCount);
                EditorGUILayout.IntField(new GUIContent("현재 대기 숫자"), test.AvailableCount);
                EditorGUILayout.FloatField(new GUIContent("반환 경과 시간"), test.ReturnElapsedSeconds);
                EditorGUILayout.Toggle(new GUIContent("전부 풀 반환"), test.PoolReturnPassed);

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("캐릭터 피해", EditorStyles.miniBoldLabel);
                EditorGUILayout.FloatField(new GUIContent("시작 HP"), test.StartHp);
                EditorGUILayout.FloatField(new GUIContent("현재 HP"), test.CurrentHp);
                EditorGUILayout.FloatField(new GUIContent("총 적용 피해"), test.TotalAppliedDamage);
                EditorGUILayout.Toggle(new GUIContent("피해 적용 성공"), test.DamagePassed);

                EditorGUILayout.Space(4f);
                EditorGUILayout.Toggle(new GUIContent("최종 검증 성공"), test.FinalPassed);
                EditorGUILayout.FloatField(new GUIContent("전체 경과 시간"), test.ElapsedSeconds);
                EditorGUILayout.TextArea(string.IsNullOrWhiteSpace(test.Message) ? "피해 숫자 Pop·Push 검증 결과가 없습니다." : test.Message);
            }

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("검증 버튼은 Play 상태에서 사용할 수 있습니다.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(8f);

            if (GUILayout.Button("피해 숫자 검증 준비"))
            {
                Execute(test.SetupTest);
            }

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("6연타 시작"))
            {
                Execute(test.StartTest);
            }

            if (GUILayout.Button("검증 정지"))
            {
                Execute(test.StopTest);
            }

            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("피해 숫자 검증 초기화"))
            {
                Execute(test.ResetResult);
            }
        }

        private void Execute(System.Action action)
        {
            action.Invoke();
            Repaint();
        }
    }
}