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
        private const string UnitBarsPrefabPath = "Assets/Unit/Prefabs/UI/UnitBars.prefab";
        private const string DefaultAttackImpactPrefabPath = "Assets/Unit/Prefabs/VFX/Combat/Impact/CombatImpact_Normal.prefab";
        private const string DefaultAttackSoundPrefabPath = "Assets/Unit/Prefabs/Audio/Combat/AttackSound_Default.prefab";
        private const string HitRuleAssetPath = "Assets/Unit/Data/Combat/HitRule.asset";
        private const string DamageRuleAssetPath = "Assets/Unit/Data/Combat/DamageRule.asset";
        private const string PrototypeNamespace = "EndlessGuard.Unit.Prototype";

        public static bool TryCreateUnitPrefab(UnitDataSO unitData, out GameObject prefabAsset, out string message)
        {
            prefabAsset = null;
            message = string.Empty;

            if (unitData == null || string.IsNullOrWhiteSpace(unitData.UnitId) || string.IsNullOrWhiteSpace(unitData.DisplayName))
            {
                return false;
            }

            if (unitData.UnitPrefab != null)
            {
                prefabAsset = unitData.UnitPrefab;
                return false;
            }

            EnsureFolder(UnitPrefabFolder);

            string prefabName = BuildPrefabName(unitData.UnitId, unitData.DisplayName);
            string prefabPath = $"{UnitPrefabFolder}/{prefabName}.prefab";

            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                return false;
            }

            GameObject root = null;

            try
            {
                HitRuleSO hitRule = LoadAsset<HitRuleSO>(HitRuleAssetPath);
                DamageRuleSO damageRule = LoadAsset<DamageRuleSO>(DamageRuleAssetPath);
                root = CreateBaseRoot(prefabName, 1f, out CombatEntityAnchors anchors, out Transform uiAnchor);

                UnitDataLink dataLink = EnsureComponent<UnitDataLink>(root);
                AssignReference(dataLink, "unitData", unitData);

                EnsureComponent<CombatHealth>(root);
                EnsureComponent<CombatGridPosition>(root);
                EnsureComponent<UnitRuntimeState>(root);
                EnsureComponent<UnitFacingView>(root);
                EnsureComponent<UnitBlock>(root);

                UnitAttack attack = EnsureComponent<UnitAttack>(root);
                AssignReference(attack, "hitRule", hitRule);
                AssignReference(attack, "damageRule", damageRule);

                EnsureComponent<DamageNumberEmitter>(root);
                EnsureComponent<HitFlash>(root);
                HitShake hitShake = EnsureComponent<HitShake>(root);
                ConfigureHitShake(hitShake, false);

                CreateUnitAttackFeedback(anchors.AttackPoint);
                CreateUnitBars(uiAnchor);
                ValidateUnitRoot(root, anchors, dataLink, uiAnchor, hitRule, damageRule);

                prefabAsset = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);

                if (prefabAsset == null)
                {
                    return false;
                }

                AssignReference(unitData, "unitPrefab", prefabAsset);
                EditorUtility.SetDirty(unitData);
                FinishCreation(prefabAsset);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
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
            message = string.Empty;

            if (enemyData == null || string.IsNullOrWhiteSpace(enemyData.EnemyId) || string.IsNullOrWhiteSpace(enemyData.DisplayName))
            {
                return false;
            }

            if (enemyData.EnemyPrefab != null)
            {
                prefabAsset = enemyData.EnemyPrefab;
                return false;
            }

            EnsureFolder(EnemyPrefabFolder);

            string prefabName = BuildPrefabName(enemyData.EnemyId, enemyData.DisplayName);
            string prefabPath = $"{EnemyPrefabFolder}/{prefabName}.prefab";

            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                return false;
            }

            GameObject root = null;

            try
            {
                HitRuleSO hitRule = LoadAsset<HitRuleSO>(HitRuleAssetPath);
                DamageRuleSO damageRule = LoadAsset<DamageRuleSO>(DamageRuleAssetPath);
                float visualScale = GetEnemyVisualScale(enemyData.Size);
                root = CreateBaseRoot(prefabName, visualScale, out CombatEntityAnchors anchors, out _);

                EnemyDataLink dataLink = EnsureComponent<EnemyDataLink>(root);
                AssignReference(dataLink, "enemyData", enemyData);

                EnsureComponent<CombatHealth>(root);
                EnsureComponent<CombatGridPosition>(root);
                EnsureComponent<EnemyRuntimeState>(root);
                EnsureComponent<EnemyBlock>(root);
                EnsureComponent<EnemyMove>(root);

                EnemyAttack attack = EnsureComponent<EnemyAttack>(root);
                AssignReference(attack, "hitRule", hitRule);
                AssignReference(attack, "damageRule", damageRule);

                EnsureComponent<DamageNumberEmitter>(root);
                EnsureComponent<HitFlash>(root);
                HitShake hitShake = EnsureComponent<HitShake>(root);
                ConfigureHitShake(hitShake, true);

                ValidateEnemyRoot(root, anchors, dataLink, hitRule, damageRule);

                prefabAsset = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);

                if (prefabAsset == null)
                {
                    return false;
                }

                AssignReference(enemyData, "enemyPrefab", prefabAsset);
                EditorUtility.SetDirty(enemyData);
                FinishCreation(prefabAsset);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
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

        private static GameObject CreateBaseRoot(string rootName, float visualScale, out CombatEntityAnchors anchors, out Transform uiAnchor)
        {
            GameObject root = new GameObject(rootName);
            anchors = root.AddComponent<CombatEntityAnchors>();

            Transform visualRoot = CreateChild(root.transform, "VisualRoot", Vector3.zero);
            Transform attackPoint = CreateChild(root.transform, "AttackPoint", new Vector3(0f, visualScale * 1.2f, visualScale * 0.6f));
            Transform effectPoint = CreateChild(root.transform, "EffectPoint", new Vector3(0f, visualScale, 0f));
            uiAnchor = CreateChild(root.transform, "UIAnchor", new Vector3(0f, visualScale * 2.3f, 0f));

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
            capsule.transform.localScale = Vector3.one * visualScale;

            Collider collider = capsule.GetComponent<Collider>();

            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
        }

        private static void CreateUnitAttackFeedback(Transform attackPoint)
        {
            GameObject impactPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultAttackImpactPrefabPath);
            GameObject soundPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultAttackSoundPrefabPath);

            if (attackPoint == null || impactPrefab == null || soundPrefab == null)
            {
                throw new InvalidOperationException();
            }

            GameObject impact = PrefabUtility.InstantiatePrefab(impactPrefab, attackPoint) as GameObject;
            GameObject sound = PrefabUtility.InstantiatePrefab(soundPrefab, attackPoint) as GameObject;

            if (impact == null || sound == null)
            {
                throw new InvalidOperationException();
            }

            impact.name = "AttackImpact";
            impact.SetActive(false);
            sound.name = "AttackSound";
            sound.SetActive(false);
        }

        private static void CreateUnitBars(Transform uiAnchor)
        {
            GameObject barsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UnitBarsPrefabPath);

            if (uiAnchor == null || barsPrefab == null || PrefabUtility.InstantiatePrefab(barsPrefab, uiAnchor) == null)
            {
                throw new InvalidOperationException();
            }
        }

        private static T LoadAsset<T>(string assetPath) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);

            if (asset == null)
            {
                throw new InvalidOperationException();
            }

            return asset;
        }

        private static T EnsureComponent<T>(GameObject root) where T : Component
        {
            T component = root.GetComponent<T>();
            return component != null ? component : root.AddComponent<T>();
        }

        private static void AssignReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);

            if (property == null)
            {
                throw new InvalidOperationException();
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
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

        private static void ConfigureHitShake(HitShake hitShake, bool useBackwardRecoil)
        {
            SerializedObject serializedHitShake = new SerializedObject(hitShake);
            SerializedProperty reactionDurationProperty = serializedHitShake.FindProperty("reactionDuration");
            SerializedProperty recoilEnabledProperty = serializedHitShake.FindProperty("useBackwardRecoil");
            SerializedProperty recoilDistanceProperty = serializedHitShake.FindProperty("recoilDistance");

            if (reactionDurationProperty == null || recoilEnabledProperty == null || recoilDistanceProperty == null)
            {
                throw new InvalidOperationException();
            }

            reactionDurationProperty.floatValue = useBackwardRecoil ? 0.18f : 0.12f;
            recoilEnabledProperty.boolValue = useBackwardRecoil;
            recoilDistanceProperty.floatValue = useBackwardRecoil ? 0.16f : 0f;
            serializedHitShake.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ValidateUnitRoot(GameObject root, CombatEntityAnchors anchors, UnitDataLink dataLink, Transform uiAnchor, HitRuleSO hitRule, DamageRuleSO damageRule)
        {
            ValidateNoPrototypeComponents(root);

            if (!anchors.IsComplete || !dataLink.HasData)
            {
                throw new InvalidOperationException();
            }

            RequireComponents(root, typeof(CombatHealth), typeof(CombatGridPosition), typeof(UnitRuntimeState), typeof(UnitFacingView), typeof(UnitBlock), typeof(UnitAttack), typeof(DamageNumberEmitter), typeof(HitFlash), typeof(HitShake));

            UnitAttack attack = root.GetComponent<UnitAttack>();

            if (attack.HitRule != hitRule || attack.DamageRule != damageRule || anchors.AttackPoint.GetComponentInChildren<AttackImpactVfxTemplate>(true) == null || anchors.AttackPoint.GetComponentInChildren<AttackHitSoundTemplate>(true) == null)
            {
                throw new InvalidOperationException();
            }

            ValidateUnitBars(uiAnchor);
        }

        private static void ValidateEnemyRoot(GameObject root, CombatEntityAnchors anchors, EnemyDataLink dataLink, HitRuleSO hitRule, DamageRuleSO damageRule)
        {
            ValidateNoPrototypeComponents(root);

            if (!anchors.IsComplete || !dataLink.HasData)
            {
                throw new InvalidOperationException();
            }

            RequireComponents(root, typeof(CombatHealth), typeof(CombatGridPosition), typeof(EnemyRuntimeState), typeof(EnemyBlock), typeof(EnemyMove), typeof(EnemyAttack), typeof(DamageNumberEmitter), typeof(HitFlash), typeof(HitShake));

            EnemyAttack attack = root.GetComponent<EnemyAttack>();

            if (attack.HitRule != hitRule || attack.DamageRule != damageRule)
            {
                throw new InvalidOperationException();
            }
        }

        private static void ValidateUnitBars(Transform uiAnchor)
        {
            UnitBars bars = uiAnchor != null ? uiAnchor.GetComponentInChildren<UnitBars>(true) : null;

            if (bars == null)
            {
                throw new InvalidOperationException();
            }

            SerializedObject serializedBars = new SerializedObject(bars);

            if (serializedBars.FindProperty("hpFill")?.objectReferenceValue == null || serializedBars.FindProperty("skillFill")?.objectReferenceValue == null)
            {
                throw new InvalidOperationException();
            }
        }

        private static void RequireComponents(GameObject root, params Type[] componentTypes)
        {
            for (int i = 0; i < componentTypes.Length; i++)
            {
                if (root.GetComponent(componentTypes[i]) == null)
                {
                    throw new InvalidOperationException();
                }
            }
        }

        private static void ValidateNoPrototypeComponents(GameObject root)
        {
            MonoBehaviour[] components = root.GetComponentsInChildren<MonoBehaviour>(true);

            for (int i = 0; i < components.Length; i++)
            {
                MonoBehaviour component = components[i];

                if (component == null)
                {
                    continue;
                }

                string componentNamespace = component.GetType().Namespace ?? string.Empty;

                if (componentNamespace == PrototypeNamespace || componentNamespace.StartsWith($"{PrototypeNamespace}.", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException();
                }
            }
        }

        private static void FinishCreation(GameObject prefabAsset)
        {
            AssetDatabase.SaveAssets();
            Selection.activeObject = prefabAsset;
            EditorGUIUtility.PingObject(prefabAsset);
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
            string result = value.Trim();

            foreach (char invalidCharacter in Path.GetInvalidFileNameChars())
            {
                result = result.Replace(invalidCharacter, '_');
            }

            return result.Replace('/', '_').Replace('\\', '_');
        }

        private static void EnsureFolder(string folderPath)
        {
            string[] parts = folderPath.Split('/');
            string currentPath = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string nextPath = $"{currentPath}/{parts[i]}";

                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, parts[i]);
                }

                currentPath = nextPath;
            }
        }
    }
}