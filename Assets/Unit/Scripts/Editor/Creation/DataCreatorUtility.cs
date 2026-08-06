using System;
using System.IO;
using EndlessGuard.Unit.Data;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    internal static class DataCreatorUtility
    {
        private const string UnitCatalogPath = "Assets/Unit/Data/Catalogs/UnitCatalog.asset";
        private const string EnemyCatalogPath = "Assets/Unit/Data/Catalogs/EnemyCatalog.asset";
        private const string UnitDataFolder = "Assets/Unit/Data/Units";
        private const string EnemyDataFolder = "Assets/Unit/Data/Enemies";

        public static UnitCatalog LoadUnitCatalog()
        {
            return AssetDatabase.LoadAssetAtPath<UnitCatalog>(UnitCatalogPath);
        }

        public static EnemyCatalog LoadEnemyCatalog()
        {
            return AssetDatabase.LoadAssetAtPath<EnemyCatalog>(EnemyCatalogPath);
        }

        public static bool TryCreateUnitData(string displayName, out UnitDataSO createdData, out string message)
        {
            UnitCatalog catalog = LoadUnitCatalog();
            return TryCreateData<UnitDataSO, UnitCatalog>(displayName, UnitDataFolder, "UnitData", catalog, CatalogEditorUtility.SyncUnitCatalog, data => data.UnitId, "캐릭터", out createdData, out message);
        }

        public static bool TryCreateEnemyData(string displayName, out EnemyDataSO createdData, out string message)
        {
            EnemyCatalog catalog = LoadEnemyCatalog();
            return TryCreateData<EnemyDataSO, EnemyCatalog>(displayName, EnemyDataFolder, "EnemyData", catalog, CatalogEditorUtility.SyncEnemyCatalog, data => data.EnemyId, "몬스터", out createdData, out message);
        }

        private static bool TryCreateData<TData, TCatalog>(string displayName, string folderPath, string fallbackFileName, TCatalog catalog, Func<TCatalog, CatalogSyncResult> syncCatalog, Func<TData, string> idGetter, string dataLabel, out TData createdData, out string message)
            where TData : ScriptableObject
            where TCatalog : ScriptableObject
        {
            createdData = null;

            if (string.IsNullOrWhiteSpace(displayName))
            {
                message = $"{dataLabel} 표시 이름을 입력하세요.";
                return false;
            }

            if (catalog == null)
            {
                message = $"{dataLabel} Catalog 에셋을 찾지 못했습니다.";
                return false;
            }

            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                message = $"{dataLabel} 데이터 폴더를 찾지 못했습니다: {folderPath}";
                return false;
            }

            string assetName = SanitizeAssetName(displayName);

            if (string.IsNullOrWhiteSpace(assetName))
            {
                assetName = fallbackFileName;
            }

            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/{assetName}.asset");
            TData data = ScriptableObject.CreateInstance<TData>();

            AssetDatabase.CreateAsset(data, assetPath);

            SerializedObject dataObject = new SerializedObject(data);
            SerializedProperty displayNameProperty = dataObject.FindProperty("displayName");

            if (displayNameProperty == null)
            {
                AssetDatabase.DeleteAsset(assetPath);
                message = $"{dataLabel} 데이터에서 displayName 필드를 찾지 못해 생성을 취소했습니다.";
                return false;
            }

            dataObject.Update();
            displayNameProperty.stringValue = displayName.Trim();
            dataObject.ApplyModifiedProperties();

            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();

            CatalogSyncResult syncResult = syncCatalog(catalog);

            if (!syncResult.Success)
            {
                AssetDatabase.DeleteAsset(assetPath);
                AssetDatabase.SaveAssets();
                message = $"{dataLabel} 데이터 생성을 취소했습니다. {syncResult.Message}";
                return false;
            }

            createdData = data;
            Selection.activeObject = data;
            EditorGUIUtility.PingObject(data);

            message = $"{dataLabel} 데이터 생성이 완료되었습니다.\nID: {idGetter(data)}\n경로: {assetPath}";
            return true;
        }

        private static string SanitizeAssetName(string displayName)
        {
            string sanitizedName = displayName.Trim();
            char[] invalidCharacters = Path.GetInvalidFileNameChars();

            for (int i = 0; i < invalidCharacters.Length; i++)
            {
                sanitizedName = sanitizedName.Replace(invalidCharacters[i], '_');
            }

            sanitizedName = sanitizedName.Replace('/', '_');
            sanitizedName = sanitizedName.Replace('\\', '_');
            sanitizedName = sanitizedName.Trim().TrimEnd('.');

            return sanitizedName;
        }
    }
}