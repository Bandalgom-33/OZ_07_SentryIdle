using EndlessGuard.Unit.Prototype.Phase2;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor.Phase2
{
    [CustomEditor(typeof(GroundMapPrototypeController))]
    public sealed class GroundMapPrototypeControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            GroundMapPrototypeController controller = (GroundMapPrototypeController)target;

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Ground Map 검증 결과", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.IntField("전체 타일", controller.TileCount);
                EditorGUILayout.IntField("Ground", controller.GroundTileCount);
                EditorGUILayout.IntField("HighGround", controller.HighGroundTileCount);
                EditorGUILayout.Toggle("최근 배치 규칙", controller.LastPlacementPassed);
                EditorGUILayout.Toggle("지상 경로 규칙", controller.LastGroundRoutePassed);
                EditorGUILayout.TextArea(controller.LastMessage ?? string.Empty);
            }

            if (GUILayout.Button("Ground Map / 지상 경로 검사"))
            {
                controller.RefreshAndValidateMap();
                Repaint();
            }

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("배치/이동/전투 버튼은 Play Mode에서 사용합니다.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("배치 규칙", EditorStyles.boldLabel);
            if (GUILayout.Button("지상 캐릭터 -> Ground (성공 확인)")) Execute(controller.TestGroundUnitOnGround);
            if (GUILayout.Button("지상 캐릭터 -> HighGround (거부 확인)")) Execute(controller.TestGroundUnitOnHighGroundShouldFail);
            if (GUILayout.Button("언덕 캐릭터 -> HighGround (성공 확인)")) Execute(controller.TestHighGroundUnitOnHighGround);
            if (GUILayout.Button("언덕 캐릭터 -> Ground (거부 확인)")) Execute(controller.TestHighGroundUnitOnGroundShouldFail);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("몬스터 이동 / 통합 전투", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("지상 몬스터 생성")) Execute(controller.SpawnGroundEnemy);
            if (GUILayout.Button("공중 몬스터 생성")) Execute(controller.SpawnAirEnemy);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("전투 시작")) Execute(controller.StartCombat);
            if (GUILayout.Button("전투 정지")) Execute(controller.StopCombat);
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("생성 개체 초기화")) Execute(controller.ResetActors);
        }

        private void Execute(System.Action action)
        {
            action?.Invoke();
            Repaint();
        }
    }
}
