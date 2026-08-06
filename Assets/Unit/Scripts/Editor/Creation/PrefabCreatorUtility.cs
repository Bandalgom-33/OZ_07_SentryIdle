using System;
using System.IO;
using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Runtime;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    internal static class PrefabCreatorUtility
    {
        private const string UnitPrefabFolder = "Assets/Unit/Prefabs/Units";
        private const string EnemyPrefabFolder = "Assets/Unit/Prefabs/Enemies";

        public static bool TryCreateUnitPrefab(UnitDataSO unitData, out GameObject prefabAsset, out string message)
        {
            prefabAsset = null;

            if (unitData == null)
            {
                message = "캐릭터 데이터가 선택되지 않았습니다.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(unitData.UnitId))
            {
                message = "캐릭터 데이터 ID가 비어 있어 프리팹을 생성할 수 없습니다.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(unitData.DisplayName))
            {
                message = "캐릭터 표시 이름이 비어 있어 프리팹을 생성할 수 없습니다.";
                return false;
            }

            if (unitData.UnitPrefab != null)
            {
                prefabAsset = unitData.UnitPrefab;
                message = $"이미 연결된 캐릭터 프리팹이 있습니다.\n{AssetDatabase.GetAssetPath(prefabAsset)}";
                return false;
            }

            EnsureFolder(UnitPrefabFolder);

            string prefabName = BuildPrefabName(unitData.UnitId, unitData.DisplayName);
            string prefabPath = $"{UnitPrefabFolder}/{prefabName}.prefab";
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (existingPrefab != null)
            {
                prefabAsset = existingPrefab;
                message = $"같은 경로에 프리팹이 이미 존재합니다. 기존 프리팹 보호를 위해 자동으로 덮어쓰지 않습니다.\n{prefabPath}";
                return false;
            }

            GameObject root = null;

            try
            {
                root = CreateBaseRoot(prefabName, 1f, out CombatEntityAnchors anchors);

                UnitDataLink dataLink = root.AddComponent<UnitDataLink>();
                AssignUnitData(dataLink, unitData);

                UnitRuntimeState runtimeState = root.AddComponent<UnitRuntimeState>();
                UnitBlock unitBlock = root.AddComponent<UnitBlock>();

                ValidateUnitRoot(root, anchors, dataLink, runtimeState, unitBlock);

                prefabAsset = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);

                if (prefabAsset == null)
                {
                    message = "캐릭터 프리팹 저장에 실패했습니다.";
                    return false;
                }

                AssignUnitPrefab(unitData, prefabAsset);
                AssetDatabase.SaveAssets();
                Selection.activeObject = prefabAsset;
                EditorGUIUtility.PingObject(prefabAsset);

                message = $"캐릭터 프리팹을 생성하고 데이터에 연결했습니다.\n{prefabPath}\nCapsuleVisual에는 검증용 캐릭터 머티리얼을 수동으로 지정하세요.";
                return true;
            }
            catch (Exception exception)
            {
                message = $"캐릭터 프리팹 생성 중 오류가 발생했습니다.\n{exception.Message}";
                return false;
            }
            finally
            {
                if (root != null)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
        }

        public static bool TryCreateEnemyPrefab(EnemyDataSO enemyData, out GameObject prefabAsset, out string message)
        {
            prefabAsset = null;

            if (enemyData == null)
            {
                message = "몬스터 데이터가 선택되지 않았습니다.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(enemyData.EnemyId))
            {
                message = "몬스터 데이터 ID가 비어 있어 프리팹을 생성할 수 없습니다.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(enemyData.DisplayName))
            {
                message = "몬스터 표시 이름이 비어 있어 프리팹을 생성할 수 없습니다.";
                return false;
            }

            if (enemyData.EnemyPrefab != null)
            {
                prefabAsset = enemyData.EnemyPrefab;
                message = $"이미 연결된 몬스터 프리팹이 있습니다.\n{AssetDatabase.GetAssetPath(prefabAsset)}";
                return false;
            }

            EnsureFolder(EnemyPrefabFolder);

            string prefabName = BuildPrefabName(enemyData.EnemyId, enemyData.DisplayName);
            string prefabPath = $"{EnemyPrefabFolder}/{prefabName}.prefab";
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (existingPrefab != null)
            {
                prefabAsset = existingPrefab;
                message = $"같은 경로에 프리팹이 이미 존재합니다. 기존 프리팹 보호를 위해 자동으로 덮어쓰지 않습니다.\n{prefabPath}";
                return false;
            }

            GameObject root = null;

            try
            {
                float visualScale = GetEnemyVisualScale(enemyData.Size);
                root = CreateBaseRoot(prefabName, visualScale, out CombatEntityAnchors anchors);

                EnemyDataLink dataLink = root.AddComponent<EnemyDataLink>();
                AssignEnemyData(dataLink, enemyData);

                EnemyRuntimeState runtimeState = root.AddComponent<EnemyRuntimeState>();
                EnemyBlock enemyBlock = root.AddComponent<EnemyBlock>();

                ValidateEnemyRoot(root, anchors, dataLink, runtimeState, enemyBlock);

                prefabAsset = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);

                if (prefabAsset == null)
                {
                    message = "몬스터 프리팹 저장에 실패했습니다.";
                    return false;
                }

                AssignEnemyPrefab(enemyData, prefabAsset);
                AssetDatabase.SaveAssets();
                Selection.activeObject = prefabAsset;
                EditorGUIUtility.PingObject(prefabAsset);

                message = $"몬스터 프리팹을 생성하고 데이터에 연결했습니다.\n{prefabPath}\nCapsuleVisual에는 검증용 몬스터 머티리얼을 수동으로 지정하세요.";
                return true;
            }
            catch (Exception exception)
            {
                message = $"몬스터 프리팹 생성 중 오류가 발생했습니다.\n{exception.Message}";
                return false;
            }
            finally
            {
                if (root != null)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
        }

        private static GameObject CreateBaseRoot(string rootName, float visualScale, out CombatEntityAnchors anchors)
        {
            GameObject root = new GameObject(rootName);
            anchors = root.AddComponent<CombatEntityAnchors>();

            Transform visualRoot = CreateChild(root.transform, "VisualRoot", Vector3.zero);
            Transform attackPoint = CreateChild(root.transform, "AttackPoint", new Vector3(0f, visualScale * 1.2f, visualScale * 0.6f));
            Transform effectPoint = CreateChild(root.transform, "EffectPoint", new Vector3(0f, visualScale, 0f));
            Transform uiAnchor = CreateChild(root.transform, "UIAnchor", new Vector3(0f, visualScale * 2.3f, 0f));

            CreateCapsuleVisual(visualRoot, visualScale);
            AssignAnchors(anchors, visualRoot, attackPoint, effectPoint, uiAnchor);

            return root;
        }

        private static Transform CreateChild(Transform parent, string childName, Vector3 localPosition)
        {
            GameObject child = new GameObject(childName);
            Transform childTransform = child.transform;

            childTransform.SetParent(parent, false);
            childTransform.localPosition = localPosition;
            childTransform.localRotation = Quaternion.identity;
            childTransform.localScale = Vector3.one;

            return childTransform;
        }

        private static void CreateCapsuleVisual(Transform visualRoot, float visualScale)
        {
            GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "CapsuleVisual";
            capsule.transform.SetParent(visualRoot, false);
            capsule.transform.localPosition = new Vector3(0f, visualScale, 0f);
            capsule.transform.localRotation = Quaternion.identity;
            capsule.transform.localScale = Vector3.one * visualScale;

            Collider collider = capsule.GetComponent<Collider>();

            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
        }

        private static void AssignAnchors(CombatEntityAnchors anchors, Transform visualRoot, Transform attackPoint, Transform effectPoint, Transform uiAnchor)
        {
            SerializedObject serializedAnchors = new SerializedObject(anchors);
            serializedAnchors.FindProperty("visualRoot").objectReferenceValue = visualRoot;
            serializedAnchors.FindProperty("attackPoint").objectReferenceValue = attackPoint;
            serializedAnchors.FindProperty("effectPoint").objectReferenceValue = effectPoint;
            serializedAnchors.FindProperty("uiAnchor").objectReferenceValue = uiAnchor;
            serializedAnchors.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignUnitData(UnitDataLink dataLink, UnitDataSO unitData)
        {
            SerializedObject serializedLink = new SerializedObject(dataLink);
            serializedLink.FindProperty("unitData").objectReferenceValue = unitData;
            serializedLink.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignEnemyData(EnemyDataLink dataLink, EnemyDataSO enemyData)
        {
            SerializedObject serializedLink = new SerializedObject(dataLink);
            serializedLink.FindProperty("enemyData").objectReferenceValue = enemyData;
            serializedLink.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignUnitPrefab(UnitDataSO unitData, GameObject prefabAsset)
        {
            SerializedObject serializedData = new SerializedObject(unitData);
            serializedData.FindProperty("unitPrefab").objectReferenceValue = prefabAsset;
            serializedData.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(unitData);
        }

        private static void AssignEnemyPrefab(EnemyDataSO enemyData, GameObject prefabAsset)
        {
            SerializedObject serializedData = new SerializedObject(enemyData);
            serializedData.FindProperty("enemyPrefab").objectReferenceValue = prefabAsset;
            serializedData.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(enemyData);
        }

        private static void ValidateUnitRoot(GameObject root, CombatEntityAnchors anchors, UnitDataLink dataLink, UnitRuntimeState runtimeState, UnitBlock unitBlock)
        {
            if (!anchors.IsComplete)
            {
                throw new InvalidOperationException("캐릭터 프리팹 기준점 연결이 완성되지 않았습니다.");
            }

            if (!dataLink.HasData)
            {
                throw new InvalidOperationException("캐릭터 프리팹에 UnitDataSO가 연결되지 않았습니다.");
            }

            if (root.GetComponent<CombatHealth>() == null)
            {
                throw new InvalidOperationException("캐릭터 프리팹에 CombatHealth가 생성되지 않았습니다.");
            }

            if (root.GetComponent<CombatGridPosition>() == null)
            {
                throw new InvalidOperationException("캐릭터 프리팹에 CombatGridPosition이 생성되지 않았습니다.");
            }

            if (runtimeState == null)
            {
                throw new InvalidOperationException("캐릭터 프리팹에 UnitRuntimeState가 생성되지 않았습니다.");
            }

            if (unitBlock == null)
            {
                throw new InvalidOperationException("캐릭터 프리팹에 UnitBlock이 생성되지 않았습니다.");
            }
        }

        private static void ValidateEnemyRoot(GameObject root, CombatEntityAnchors anchors, EnemyDataLink dataLink, EnemyRuntimeState runtimeState, EnemyBlock enemyBlock)
        {
            if (!anchors.IsComplete)
            {
                throw new InvalidOperationException("몬스터 프리팹 기준점 연결이 완성되지 않았습니다.");
            }

            if (!dataLink.HasData)
            {
                throw new InvalidOperationException("몬스터 프리팹에 EnemyDataSO가 연결되지 않았습니다.");
            }

            if (root.GetComponent<CombatHealth>() == null)
            {
                throw new InvalidOperationException("몬스터 프리팹에 CombatHealth가 생성되지 않았습니다.");
            }

            if (root.GetComponent<CombatGridPosition>() == null)
            {
                throw new InvalidOperationException("몬스터 프리팹에 CombatGridPosition이 생성되지 않았습니다.");
            }

            if (runtimeState == null)
            {
                throw new InvalidOperationException("몬스터 프리팹에 EnemyRuntimeState가 생성되지 않았습니다.");
            }

            if (enemyBlock == null)
            {
                throw new InvalidOperationException("몬스터 프리팹에 EnemyBlock이 생성되지 않았습니다.");
            }
        }

        private static float GetEnemyVisualScale(EnemySize enemySize)
        {
            switch (enemySize)
            {
                case EnemySize.Small:
                    return 0.8f;

                case EnemySize.Large:
                    return 1.5f;

                default:
                    return 1f;
            }
        }

        private static string BuildPrefabName(string dataId, string displayName)
        {
            return $"{SanitizeFileName(dataId)}_{SanitizeFileName(displayName)}";
        }

        private static string SanitizeFileName(string value)
        {
            string sanitizedValue = value.Trim();

            foreach (char invalidCharacter in Path.GetInvalidFileNameChars())
            {
                sanitizedValue = sanitizedValue.Replace(invalidCharacter, '_');
            }

            sanitizedValue = sanitizedValue.Replace('/', '_');
            sanitizedValue = sanitizedValue.Replace('\\', '_');

            return string.IsNullOrWhiteSpace(sanitizedValue) ? "Unnamed" : sanitizedValue;
        }

        private static void EnsureFolder(string folderPath)
        {
            string[] pathParts = folderPath.Split('/');
            string currentPath = pathParts[0];

            for (int i = 1; i < pathParts.Length; i++)
            {
                string nextPath = $"{currentPath}/{pathParts[i]}";

                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, pathParts[i]);
                }

                currentPath = nextPath;
            }
        }
    }
}