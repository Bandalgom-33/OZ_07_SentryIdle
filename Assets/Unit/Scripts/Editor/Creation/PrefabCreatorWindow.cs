using EndlessGuard.Unit.Data;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    public sealed class PrefabCreatorWindow : EditorWindow
    {
        private UnitDataSO unitData;
        private EnemyDataSO enemyData;
        private string statusMessage;
        private MessageType statusMessageType = MessageType.None;

        [MenuItem("Tools/Endless Guard/프리팹 생성 도구")]
        public static void Open()
        {
            PrefabCreatorWindow window = GetWindow<PrefabCreatorWindow>("프리팹 생성 도구");
            window.minSize = new Vector2(480f, 340f);
            window.TryLoadSelectedAsset();
        }

        private void OnEnable()
        {
            TryLoadSelectedAsset();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Endless Guard 프리팹 생성 도구", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("캐릭터 또는 몬스터 데이터 SO를 기준으로 기본 프리팹 구조를 생성하고, 생성된 프리팹을 원본 데이터의 연결 프리팹 필드에 자동 등록합니다.", MessageType.Info);

            EditorGUILayout.Space(8f);
            DrawUnitSection();

            EditorGUILayout.Space(12f);
            DrawEnemySection();

            if (!string.IsNullOrWhiteSpace(statusMessage))
            {
                EditorGUILayout.Space(10f);
                EditorGUILayout.HelpBox(statusMessage, statusMessageType);
            }
        }

        private void DrawUnitSection()
        {
            EditorGUILayout.LabelField("캐릭터 기본 프리팹", EditorStyles.boldLabel);
            unitData = (UnitDataSO)EditorGUILayout.ObjectField(new GUIContent("캐릭터 데이터", "기본 프리팹을 생성할 UnitDataSO입니다."), unitData, typeof(UnitDataSO), false);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(new GUIContent("현재 연결 프리팹"), unitData == null ? null : unitData.UnitPrefab, typeof(GameObject), false);
            }

            using (new EditorGUI.DisabledScope(unitData == null || unitData.UnitPrefab != null))
            {
                if (GUILayout.Button("캐릭터 기본 프리팹 생성"))
                {
                    bool success = PrefabCreatorUtility.TryCreateUnitPrefab(unitData, out GameObject prefabAsset, out string message);
                    statusMessage = message;
                    statusMessageType = success ? MessageType.Info : MessageType.Warning;

                    if (prefabAsset != null)
                    {
                        Selection.activeObject = prefabAsset;
                        EditorGUIUtility.PingObject(prefabAsset);
                    }
                }
            }

            if (unitData != null && unitData.UnitPrefab != null && GUILayout.Button("연결된 캐릭터 프리팹 선택"))
            {
                Selection.activeObject = unitData.UnitPrefab;
                EditorGUIUtility.PingObject(unitData.UnitPrefab);
            }
        }

        private void DrawEnemySection()
        {
            EditorGUILayout.LabelField("몬스터 기본 프리팹", EditorStyles.boldLabel);
            enemyData = (EnemyDataSO)EditorGUILayout.ObjectField(new GUIContent("몬스터 데이터", "기본 프리팹을 생성할 EnemyDataSO입니다."), enemyData, typeof(EnemyDataSO), false);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(new GUIContent("현재 연결 프리팹"), enemyData == null ? null : enemyData.EnemyPrefab, typeof(GameObject), false);
            }

            using (new EditorGUI.DisabledScope(enemyData == null || enemyData.EnemyPrefab != null))
            {
                if (GUILayout.Button("몬스터 기본 프리팹 생성"))
                {
                    bool success = PrefabCreatorUtility.TryCreateEnemyPrefab(enemyData, out GameObject prefabAsset, out string message);
                    statusMessage = message;
                    statusMessageType = success ? MessageType.Info : MessageType.Warning;

                    if (prefabAsset != null)
                    {
                        Selection.activeObject = prefabAsset;
                        EditorGUIUtility.PingObject(prefabAsset);
                    }
                }
            }

            if (enemyData != null && enemyData.EnemyPrefab != null && GUILayout.Button("연결된 몬스터 프리팹 선택"))
            {
                Selection.activeObject = enemyData.EnemyPrefab;
                EditorGUIUtility.PingObject(enemyData.EnemyPrefab);
            }
        }

        private void TryLoadSelectedAsset()
        {
            if (Selection.activeObject is UnitDataSO selectedUnitData)
            {
                unitData = selectedUnitData;
                return;
            }

            if (Selection.activeObject is EnemyDataSO selectedEnemyData)
            {
                enemyData = selectedEnemyData;
            }
        }
    }
}