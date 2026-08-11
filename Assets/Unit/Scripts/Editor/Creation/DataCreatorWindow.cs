using EndlessGuard.Unit.Data;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    public sealed class DataCreatorWindow : EditorWindow
    {
        private string unitDisplayName;
        private string enemyDisplayName;
        private UnitCatalog unitCatalog;
        private EnemyCatalog enemyCatalog;
        private string resultMessage;
        private MessageType resultType = MessageType.None;

        [MenuItem("Tools/Endless Guard/데이터 제작 도구")]
        public static void OpenWindow()
        {
            DataCreatorWindow window = GetWindow<DataCreatorWindow>("데이터 제작 도구");
            window.minSize = new Vector2(440f, 360f);
        }

        private void OnEnable()
        {
            LoadCatalogs();
        }

        private void OnFocus()
        {
            LoadCatalogs();
        }

        private void LoadCatalogs()
        {
            unitCatalog = DataCreatorUtility.LoadUnitCatalog();
            enemyCatalog = DataCreatorUtility.LoadEnemyCatalog();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("캐릭터·몬스터 데이터 제작 도구", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);

            EditorGUILayout.HelpBox("표시 이름을 입력하고 생성하면 지정된 데이터 폴더에 ScriptableObject가 생성됩니다. ID 발급과 Catalog 등록도 함께 처리됩니다.", MessageType.Info);

            DrawCatalogStatus();

            EditorGUILayout.Space(10f);
            DrawUnitCreator();

            EditorGUILayout.Space(12f);
            DrawEnemyCreator();

            if (!string.IsNullOrWhiteSpace(resultMessage))
            {
                EditorGUILayout.Space(10f);
                EditorGUILayout.HelpBox(resultMessage, resultType);
            }
        }

        private void DrawCatalogStatus()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Catalog 연결 상태", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(new GUIContent("캐릭터 Catalog"), unitCatalog, typeof(UnitCatalog), false);
                EditorGUILayout.ObjectField(new GUIContent("몬스터 Catalog"), enemyCatalog, typeof(EnemyCatalog), false);

                if (unitCatalog != null)
                {
                    EditorGUILayout.TextField(new GUIContent("다음 캐릭터 ID"), $"UNIT_{unitCatalog.LastIssuedNumber + 1:D4}");
                }

                if (enemyCatalog != null)
                {
                    EditorGUILayout.TextField(new GUIContent("다음 몬스터 ID"), $"ENEMY_{enemyCatalog.LastIssuedNumber + 1:D4}");
                }
            }

            if (unitCatalog == null || enemyCatalog == null)
            {
                EditorGUILayout.HelpBox("필수 Catalog 에셋을 찾지 못했습니다. Assets/Unit/Data/Catalogs 경로를 확인하세요.", MessageType.Error);

                if (GUILayout.Button("Catalog 다시 불러오기"))
                {
                    LoadCatalogs();
                }
            }
        }

        private void DrawUnitCreator()
        {
            EditorGUILayout.LabelField("캐릭터 데이터 생성", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("생성 경로", "Assets/Unit/Data/Units");

            unitDisplayName = EditorGUILayout.TextField(new GUIContent("표시 이름", "게임 화면과 제작 도구에 표시할 캐릭터 이름입니다."), unitDisplayName);

            bool cannotCreate = unitCatalog == null || string.IsNullOrWhiteSpace(unitDisplayName);

            using (new EditorGUI.DisabledScope(cannotCreate))
            {
                if (GUILayout.Button("캐릭터 데이터 생성"))
                {
                    bool success = DataCreatorUtility.TryCreateUnitData(unitDisplayName, out UnitDataSO createdData, out string message);
                    resultMessage = message;
                    resultType = success ? MessageType.Info : MessageType.Error;

                    if (success)
                    {
                        unitDisplayName = string.Empty;
                        LoadCatalogs();
                    }
                }
            }
        }

        private void DrawEnemyCreator()
        {
            EditorGUILayout.LabelField("몬스터 데이터 생성", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("생성 경로", "Assets/Unit/Data/Enemies");

            enemyDisplayName = EditorGUILayout.TextField(new GUIContent("표시 이름", "게임 화면과 제작 도구에 표시할 몬스터 이름입니다."), enemyDisplayName);

            bool cannotCreate = enemyCatalog == null || string.IsNullOrWhiteSpace(enemyDisplayName);

            using (new EditorGUI.DisabledScope(cannotCreate))
            {
                if (GUILayout.Button("몬스터 데이터 생성"))
                {
                    bool success = DataCreatorUtility.TryCreateEnemyData(enemyDisplayName, out EnemyDataSO createdData, out string message);
                    resultMessage = message;
                    resultType = success ? MessageType.Info : MessageType.Error;

                    if (success)
                    {
                        enemyDisplayName = string.Empty;
                        LoadCatalogs();
                    }
                }
            }
        }
    }
}