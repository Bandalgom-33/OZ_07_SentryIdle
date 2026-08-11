using EndlessGuard.Unit.Prototype.Phase2;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor.Phase2
{
    [CustomEditor(typeof(SummonPrototypeController))]
    public sealed class SummonPrototypeControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            SummonPrototypeController controller = (SummonPrototypeController)target;

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("소환 Runtime 결과", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("캐릭터 소환자", controller.UnitOwner, typeof(Component), true);
                EditorGUILayout.ObjectField("몬스터 소환자", controller.EnemyOwner, typeof(Component), true);
                EditorGUILayout.IntField("최근 캐릭터 소환 수", controller.LastUnitSpawnedCount);
                EditorGUILayout.IntField("최근 몬스터 소환 수", controller.LastEnemySpawnedCount);
                EditorGUILayout.IntField("활성 캐릭터 소환물", controller.ActiveUnitSummonCount);
                EditorGUILayout.IntField("활성 몬스터 소환물", controller.ActiveEnemySummonCount);
                EditorGUILayout.TextArea(controller.LastMessage ?? string.Empty);
            }

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("실제 소환물 Prefab을 만든 뒤 Play Mode에서 검증합니다.", MessageType.Info);
                return;
            }

            if (GUILayout.Button("소환자 2종 생성")) Execute(controller.SpawnOwners);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("캐릭터 소환물 생성")) Execute(controller.SpawnUnitSummon);
            if (GUILayout.Button("몬스터 소환물 생성")) Execute(controller.SpawnEnemySummon);
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("활성 소환물 수 갱신")) Execute(controller.RefreshCounts);
            if (GUILayout.Button("모든 소환물 풀 반환")) Execute(controller.ReleaseAllSummons);
            if (GUILayout.Button("소환 Prototype 초기화")) Execute(controller.ResetPrototype);
        }

        private void Execute(System.Action action)
        {
            action?.Invoke();
            Repaint();
        }
    }
}
