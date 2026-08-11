using EndlessGuard.Unit.Prototype;
using EndlessGuard.Unit.Runtime;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    [CustomEditor(typeof(TargetTest))]
    public sealed class TargetTestEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            TargetTest test = (TargetTest)target;

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("몬스터 대상 탐색 검증", EditorStyles.boldLabel);
            DrawEnemyTargetState(test);

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("캐릭터 대상 탐색 검증", EditorStyles.boldLabel);
            DrawUnitTargetState(test);

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("검증 버튼은 Play 상태에서 사용할 수 있습니다.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(8f);

            if (GUILayout.Button("몬스터 대상 탐색 검증"))
            {
                Execute(test.VerifyTargets);
            }

            if (GUILayout.Button("몬스터 검증 초기화"))
            {
                Execute(test.ResetResult);
            }

            EditorGUILayout.Space(8f);

            if (GUILayout.Button("캐릭터 대상 검증 준비"))
            {
                Execute(test.SetupUnitTarget);
            }

            if (GUILayout.Button("캐릭터 대상 탐색 검증"))
            {
                Execute(test.VerifyUnitTarget);
            }

            if (GUILayout.Button("캐릭터 검증 초기화"))
            {
                Execute(test.ResetUnitResult);
            }
        }

        private static void DrawEnemyTargetState(TargetTest test)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                DrawUnitTarget("예상 대상", test.ExpectedTarget);
                EditorGUILayout.Space(4f);
                DrawEnemyResult("첫 번째 몬스터", test.FirstFound, test.FirstTarget, test.FirstPassed);
                DrawEnemyResult("두 번째 몬스터", test.SecondFound, test.SecondTarget, test.SecondPassed);
                DrawEnemyResult("세 번째 몬스터", test.ThirdFound, test.ThirdTarget, test.ThirdPassed);
                EditorGUILayout.Space(4f);
                EditorGUILayout.Toggle(new GUIContent("최종 검증 성공"), test.FinalPassed);
                EditorGUILayout.TextArea(string.IsNullOrWhiteSpace(test.ResultMessage) ? "몬스터 대상 탐색 검증 결과가 없습니다." : test.ResultMessage);
            }
        }

        private static void DrawUnitTargetState(TargetTest test)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                DrawUnitTarget("공격 캐릭터", test.UnitAttacker);
                DrawEnemyTarget("예상 몬스터", test.ExpectedEnemyTarget);
                DrawEnemyTarget("발견 몬스터", test.FoundEnemyTarget);
                EditorGUILayout.Toggle(new GUIContent("검증 준비 완료"), test.UnitTargetReady);
                EditorGUILayout.Toggle(new GUIContent("대상 발견"), test.UnitTargetFound);

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("남은 경로 거리", EditorStyles.miniBoldLabel);
                EditorGUILayout.FloatField(new GUIContent("첫 번째 몬스터"), test.FirstRemainingDistance);
                EditorGUILayout.FloatField(new GUIContent("두 번째 몬스터"), test.SecondRemainingDistance);
                EditorGUILayout.FloatField(new GUIContent("세 번째 몬스터"), test.ThirdRemainingDistance);

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("방향", EditorStyles.miniBoldLabel);
                EditorGUILayout.EnumPopup(new GUIContent("시작 방향"), test.InitialFacing);
                EditorGUILayout.EnumPopup(new GUIContent("선택 후 방향"), test.FinalFacing);

                EditorGUILayout.Space(4f);
                EditorGUILayout.Toggle(new GUIContent("경로 우선순위 성공"), test.PriorityPassed);
                EditorGUILayout.Toggle(new GUIContent("방향 변경 성공"), test.FacingPassed);
                EditorGUILayout.Toggle(new GUIContent("최종 검증 성공"), test.UnitFinalPassed);
                EditorGUILayout.TextArea(string.IsNullOrWhiteSpace(test.UnitResultMessage) ? "캐릭터 대상 탐색 검증 결과가 없습니다." : test.UnitResultMessage);
            }
        }

        private static void DrawEnemyResult(string label, bool found, UnitRuntimeState target, bool passed)
        {
            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
            EditorGUILayout.Toggle(new GUIContent("대상 발견"), found);
            DrawUnitTarget("발견 대상", target);
            EditorGUILayout.Toggle(new GUIContent("검증 성공"), passed);
        }

        private static void DrawUnitTarget(string label, UnitRuntimeState target)
        {
            EditorGUILayout.ObjectField(new GUIContent(label), target == null ? null : target.gameObject, typeof(GameObject), true);
        }

        private static void DrawEnemyTarget(string label, EnemyRuntimeState target)
        {
            EditorGUILayout.ObjectField(new GUIContent(label), target == null ? null : target.gameObject, typeof(GameObject), true);
        }

        private void Execute(System.Action action)
        {
            action.Invoke();
            Repaint();
        }
    }
}