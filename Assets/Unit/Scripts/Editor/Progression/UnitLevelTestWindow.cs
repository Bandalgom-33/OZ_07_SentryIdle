using System;
using EndlessGuard.Unit.Data;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    public sealed class UnitLevelTestWindow : EditorWindow
    {
        private UnitDataSO unitData;
        private UnitLevelCurveSO levelCurve;
        private UnitProgressData testProgress;
        private UnitLevelResult lastResult;
        private int maxLevel = 30;
        private long expToAdd = 1000L;
        private bool hasResult;

        [MenuItem("Tools/Endless Guard/레벨업 계산 검증")]
        public static void Open()
        {
            UnitLevelTestWindow window = GetWindow<UnitLevelTestWindow>("레벨업 계산 검증");
            window.minSize = new Vector2(430f, 430f);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("캐릭터 레벨업 계산 검증", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("실제 저장 데이터를 변경하지 않고 임시 진행도를 생성하여 경험치와 연속 레벨업 계산을 검증합니다.", MessageType.Info);

            EditorGUILayout.Space(6f);
            unitData = (UnitDataSO)EditorGUILayout.ObjectField(new GUIContent("캐릭터 데이터", "초기 레벨과 캐릭터 ID를 가져올 UnitDataSO입니다."), unitData, typeof(UnitDataSO), false);
            levelCurve = (UnitLevelCurveSO)EditorGUILayout.ObjectField(new GUIContent("경험치 곡선", "레벨별 필요 경험치를 계산할 UnitLevelCurveSO입니다."), levelCurve, typeof(UnitLevelCurveSO), false);
            maxLevel = Mathf.Max(1, EditorGUILayout.IntField(new GUIContent("현재 최대 레벨", "승급 상태에 따라 허용되는 현재 최대 레벨입니다."), maxLevel));

            using (new EditorGUI.DisabledScope(unitData == null))
            {
                if (GUILayout.Button("테스트 진행도 초기화"))
                {
                    testProgress = UnitProgressData.Create(unitData);
                    hasResult = false;
                }
            }

            EditorGUILayout.Space(8f);
            DrawCurrentProgress();

            EditorGUILayout.Space(8f);
            expToAdd = Math.Max(0L, EditorGUILayout.LongField(new GUIContent("지급할 경험치", "현재 테스트 진행도에 추가할 경험치입니다."), expToAdd));

            using (new EditorGUI.DisabledScope(testProgress == null || levelCurve == null))
            {
                if (GUILayout.Button("경험치 지급"))
                {
                    lastResult = UnitLevelCalculator.AddExperience(testProgress, levelCurve, maxLevel, expToAdd);
                    hasResult = true;
                }
            }

            DrawLastResult();
        }

        private void DrawCurrentProgress()
        {
            EditorGUILayout.LabelField("현재 테스트 진행도", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField(new GUIContent("캐릭터 ID"), testProgress == null ? "진행도 없음" : testProgress.UnitId);
                EditorGUILayout.IntField(new GUIContent("현재 레벨"), testProgress == null ? 0 : testProgress.CurrentLevel);
                EditorGUILayout.TextField(new GUIContent("현재 경험치"), testProgress == null ? "0" : $"{testProgress.CurrentExp:N0}");
                EditorGUILayout.TextField(new GUIContent("다음 레벨 필요 경험치"), GetNextRequiredExpText());
            }
        }

        private string GetNextRequiredExpText()
        {
            if (testProgress == null || levelCurve == null)
            {
                return "확인 불가";
            }

            if (testProgress.CurrentLevel >= maxLevel)
            {
                return "최대 레벨";
            }

            return $"{levelCurve.GetRequiredExp(testProgress.CurrentLevel):N0}";
        }

        private void DrawLastResult()
        {
            if (!hasResult)
            {
                return;
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("최근 계산 결과", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.IntField(new GUIContent("이전 레벨"), lastResult.PreviousLevel);
                EditorGUILayout.IntField(new GUIContent("현재 레벨"), lastResult.CurrentLevel);
                EditorGUILayout.IntField(new GUIContent("상승한 레벨 수"), lastResult.LevelsGained);
                EditorGUILayout.TextField(new GUIContent("지급 경험치"), $"{lastResult.GainedExp:N0}");
                EditorGUILayout.TextField(new GUIContent("소비 경험치"), $"{lastResult.ConsumedExp:N0}");
                EditorGUILayout.TextField(new GUIContent("남은 경험치"), $"{lastResult.CurrentExp:N0}");
                EditorGUILayout.TextField(new GUIContent("폐기 경험치"), $"{lastResult.DiscardedExp:N0}");
                EditorGUILayout.Toggle(new GUIContent("최대 레벨 도달"), lastResult.ReachedMaxLevel);
            }
        }
    }
}