using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    internal readonly struct CatalogSyncResult
    {
        public CatalogSyncResult(bool success, int dataCount, int issuedCount, int lastIssuedNumber, string message)
        {
            Success = success;
            DataCount = dataCount;
            IssuedCount = issuedCount;
            LastIssuedNumber = lastIssuedNumber;
            Message = message;
        }

        public bool Success { get; }
        public int DataCount { get; }
        public int IssuedCount { get; }
        public int LastIssuedNumber { get; }
        public string Message { get; }
    }

    internal static class CatalogEditorUtility
    {
        private const string UnitDataFolder = "Assets/Unit/Data/Units";
        private const string EnemyDataFolder = "Assets/Unit/Data/Enemies";
        private const string UnitPrefix = "UNIT_";
        private const string EnemyPrefix = "ENEMY_";

        public static CatalogSyncResult SyncUnitCatalog(UnitCatalog catalog)
        {
            return SyncCatalog(catalog, LoadAssets<UnitDataSO>(UnitDataFolder), "lastIssuedNumber", "units", "unitId", UnitPrefix, data => data.UnitId, "캐릭터");
        }

        public static CatalogSyncResult SyncEnemyCatalog(EnemyCatalog catalog)
        {
            return SyncCatalog(catalog, LoadAssets<EnemyDataSO>(EnemyDataFolder), "lastIssuedNumber", "enemies", "enemyId", EnemyPrefix, data => data.EnemyId, "몬스터");
        }

        private static CatalogSyncResult SyncCatalog<TCatalog, TData>(TCatalog catalog, List<TData> dataAssets, string lastIssuedFieldName, string listFieldName, string idFieldName,
            string idPrefix, Func<TData, string> idGetter, string dataLabel)
            where TCatalog : ScriptableObject
            where TData : ScriptableObject
        {
            if (catalog == null)
            {
                return new CatalogSyncResult(false, 0, 0, 0, $"{dataLabel} Catalog 참조가 없습니다.");
            }

            SerializedObject catalogObject = new SerializedObject(catalog);
            SerializedProperty lastIssuedProperty = catalogObject.FindProperty(lastIssuedFieldName);
            SerializedProperty listProperty = catalogObject.FindProperty(listFieldName);

            if (lastIssuedProperty == null || listProperty == null)
            {
                return new CatalogSyncResult(false, 0, 0, 0, $"{dataLabel} Catalog의 직렬화 필드를 찾지 못했습니다.");
            }

            int lastIssuedNumber = lastIssuedProperty.intValue;
            Dictionary<string, TData> usedIds = new Dictionary<string, TData>(StringComparer.Ordinal);
            List<TData> missingIdAssets = new List<TData>();

            for (int i = 0; i < dataAssets.Count; i++)
            {
                TData data = dataAssets[i];
                string currentId = idGetter(data);

                if (string.IsNullOrWhiteSpace(currentId))
                {
                    missingIdAssets.Add(data);
                    continue;
                }

                if (!TryParseId(currentId, idPrefix, out int parsedNumber))
                {
                    string assetPath = AssetDatabase.GetAssetPath(data);
                    Debug.LogError($"{assetPath}의 ID 형식이 올바르지 않습니다: {currentId}", data);
                    return new CatalogSyncResult(false, dataAssets.Count, 0, lastIssuedNumber, "잘못된 ID 형식이 있어 동기화를 중단했습니다. Console을 확인하세요.");
                }

                if (usedIds.TryGetValue(currentId, out TData duplicateData))
                {
                    string firstPath = AssetDatabase.GetAssetPath(duplicateData);
                    string secondPath = AssetDatabase.GetAssetPath(data);
                    Debug.LogError($"중복 ID가 발견되었습니다: {currentId}\n{firstPath}\n{secondPath}", data);
                    return new CatalogSyncResult(false, dataAssets.Count, 0, lastIssuedNumber, "중복 ID가 있어 동기화를 중단했습니다. Console을 확인하세요.");
                }

                usedIds.Add(currentId, data);

                if (parsedNumber > lastIssuedNumber)
                {
                    lastIssuedNumber = parsedNumber;
                }
            }

            for (int i = 0; i < missingIdAssets.Count; i++)
            {
                SerializedObject dataObject = new SerializedObject(missingIdAssets[i]);

                if (dataObject.FindProperty(idFieldName) == null)
                {
                    string assetPath = AssetDatabase.GetAssetPath(missingIdAssets[i]);
                    Debug.LogError($"{assetPath}에서 ID 필드 {idFieldName}을 찾지 못했습니다.", missingIdAssets[i]);
                    return new CatalogSyncResult(false, dataAssets.Count, 0, lastIssuedNumber, "ID 필드를 찾지 못해 동기화를 중단했습니다. Console을 확인하세요.");
                }
            }

            int issuedCount = 0;

            for (int i = 0; i < missingIdAssets.Count; i++)
            {
                TData data = missingIdAssets[i];
                string newId;

                do
                {
                    lastIssuedNumber++;
                    newId = $"{idPrefix}{lastIssuedNumber:D4}";
                }
                while (usedIds.ContainsKey(newId));

                SerializedObject dataObject = new SerializedObject(data);
                SerializedProperty idProperty = dataObject.FindProperty(idFieldName);

                dataObject.Update();
                idProperty.stringValue = newId;
                dataObject.ApplyModifiedProperties();

                EditorUtility.SetDirty(data);
                usedIds.Add(newId, data);
                issuedCount++;
            }

            catalogObject.Update();
            lastIssuedProperty.intValue = lastIssuedNumber;
            listProperty.arraySize = dataAssets.Count;

            for (int i = 0; i < dataAssets.Count; i++)
            {
                listProperty.GetArrayElementAtIndex(i).objectReferenceValue = dataAssets[i];
            }

            catalogObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();

            string message = $"{dataLabel} 데이터 {dataAssets.Count}개를 등록했고, 새 ID {issuedCount}개를 발급했습니다. 마지막 발급 번호는 {lastIssuedNumber}입니다.";
            return new CatalogSyncResult(true, dataAssets.Count, issuedCount, lastIssuedNumber, message);
        }

        private static List<T> LoadAssets<T>(string folderPath) where T : ScriptableObject
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folderPath });
            List<T> assets = new List<T>(guids.Length);

            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);

                if (asset != null)
                {
                    assets.Add(asset);
                }
            }

            assets.Sort((left, right) => string.CompareOrdinal(AssetDatabase.GetAssetPath(left), AssetDatabase.GetAssetPath(right)));
            return assets;
        }

        private static bool TryParseId(string id, string prefix, out int number)
        {
            number = 0;

            if (!id.StartsWith(prefix, StringComparison.Ordinal))
            {
                return false;
            }

            string numberText = id.Substring(prefix.Length);

            if (numberText.Length < 4)
            {
                return false;
            }

            for (int i = 0; i < numberText.Length; i++)
            {
                if (numberText[i] < '0' || numberText[i] > '9')
                {
                    return false;
                }
            }

            return int.TryParse(numberText, out number) && number > 0;
        }
    }
}