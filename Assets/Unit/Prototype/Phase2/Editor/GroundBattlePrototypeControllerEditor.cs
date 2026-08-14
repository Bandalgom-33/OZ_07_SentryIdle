using EndlessGuard.Unit.Prototype.Phase2;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor.Phase2
{
    [CustomEditor(typeof(GroundBattlePrototypeController))]
    public sealed class GroundBattlePrototypeControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GroundBattlePrototypeController controller = (GroundBattlePrototypeController)target;

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Ground 통합 Prototype 상태", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Toggle("전투 진행 중", controller.BattleRunning);
                EditorGUILayout.IntField("현재 배치 캐릭터", controller.ActiveUnitCount);
                EditorGUILayout.IntField("대기 캐릭터", controller.ReserveUnitCount);
                EditorGUILayout.IntField("출구 HP", controller.CurrentExitHp);

                EditorGUILayout.Space(5f);
                EditorGUILayout.LabelField("배치 코스트", EditorStyles.miniBoldLabel);
                EditorGUILayout.IntField("현재 코스트", controller.CurrentCost);
                EditorGUILayout.IntField("최대 코스트", controller.MaxCost);
                EditorGUILayout.IntField("누적 소비 코스트", controller.TotalCostSpent);
                EditorGUILayout.IntField("초당 자동 획득", controller.TotalCostRegenerated);
                EditorGUILayout.IntField("패시브 획득 코스트", controller.TotalPassiveCostGained);
                EditorGUILayout.IntField("패시브 획득 요청 횟수", controller.PassiveCostRequestCount);
                EditorGUILayout.TextArea(controller.LastCostMessage ?? string.Empty);

                EditorGUILayout.Space(5f);
                EditorGUILayout.LabelField("골드", EditorStyles.miniBoldLabel);
                EditorGUILayout.IntField("현재 골드", controller.CurrentGold);
                EditorGUILayout.IntField("누적 획득 골드", controller.TotalGoldEarned);
                EditorGUILayout.IntField("누적 소비 골드", controller.TotalGoldSpent);

                EditorGUILayout.Space(5f);
                EditorGUILayout.LabelField("레벨 / 경험치 / 승급", EditorStyles.miniBoldLabel);
                EditorGUILayout.IntField("진행 변경 이벤트", controller.ProgressEventCount);
                EditorGUILayout.TextArea(controller.LastProgressMessage ?? string.Empty);

                EditorGUILayout.Space(5f);
                EditorGUILayout.LabelField("전투 결과", EditorStyles.miniBoldLabel);
                EditorGUILayout.IntField("몬스터 생성", controller.EnemySpawnCount);
                EditorGUILayout.IntField("몬스터 사망", controller.EnemyDeathCount);
                EditorGUILayout.IntField("출구 도달", controller.EnemyReachedGoalCount);
                EditorGUILayout.IntField("캐릭터 교체", controller.ReplacementCount);
                EditorGUILayout.TextArea(controller.LastMessage ?? string.Empty);
            }

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Ground Prototype 검증 버튼은 Play Mode에서 사용합니다.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(6f);

            if (GUILayout.Button("Ground 전투 시작", GUILayout.Height(30f)))
            {
                controller.StartBattle();
            }

            if (GUILayout.Button("캐릭터 1명 교체"))
            {
                controller.ReplaceOneUnit();
            }

            if (GUILayout.Button("전투 초기화"))
            {
                controller.ResetBattle();
            }
        }
    }
}