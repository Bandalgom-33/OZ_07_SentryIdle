using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Prototype.Phase2;
using EndlessGuard.Unit.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    public static class GroundPrototypeSetupWindow
    {
        private const string RootName = "GroundPrototype";
        private const string MapName = "Map";
        private const string RouteName = "EnemyRoute";
        private const string UnitCatalogPath = "Assets/Unit/Data/Catalogs/UnitCatalog.asset";
        private const string EnemyCatalogPath = "Assets/Unit/Data/Catalogs/EnemyCatalog.asset";
        private const string MaterialFolder = "Assets/Unit/Materials/Prototype";
        private const string GroundMaterialPath = MaterialFolder + "/GroundPrototype_Ground.mat";
        private const string RouteMaterialPath = MaterialFolder + "/GroundPrototype_Route.mat";
        private const string HighGroundMaterialPath = MaterialFolder + "/GroundPrototype_HighGround.mat";
        private const string EntranceMaterialPath = MaterialFolder + "/GroundPrototype_Entrance.mat";
        private const string ExitMaterialPath = MaterialFolder + "/GroundPrototype_Exit.mat";

        private static readonly Vector2Int[] RouteCoordinates =
        {
            new Vector2Int(-8, 0),
            new Vector2Int(-7, 0),
            new Vector2Int(-6, 0),
            new Vector2Int(-5, 0),
            new Vector2Int(-4, 0),
            new Vector2Int(-3, 0),
            new Vector2Int(-2, 0),
            new Vector2Int(-2, 1),
            new Vector2Int(-1, 1),
            new Vector2Int(0, 1),
            new Vector2Int(1, 1),
            new Vector2Int(2, 1),
            new Vector2Int(3, 1),
            new Vector2Int(4, 1),
            new Vector2Int(4, 0),
            new Vector2Int(5, 0),
            new Vector2Int(6, 0),
            new Vector2Int(7, 0),
            new Vector2Int(8, 0)
        };

        [MenuItem("Tools/Endless Guard/Unit/Ground Prototype/실제 게임형 검증맵 생성")]
        public static void CreateGroundPrototype()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Play Mode를 종료한 뒤 Ground Prototype을 생성하세요.");
                return;
            }

            GameObject existingRoot = GameObject.Find(RootName);

            if (existingRoot != null)
            {
                Undo.DestroyObjectImmediate(existingRoot);
            }

            GameObject root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Create Ground Prototype");

            CombatLoop combatLoop = Undo.AddComponent<CombatLoop>(root);
            GroundBattlePrototypeController battleController = Undo.AddComponent<GroundBattlePrototypeController>(root);

            GameObject mapObject = new GameObject(MapName);
            Undo.RegisterCreatedObjectUndo(mapObject, "Create Ground Prototype Map");
            mapObject.transform.SetParent(root.transform, false);

            Dictionary<Vector2Int, Phase2GroundTile> tiles = CreateMap(mapObject.transform);

            GameObject routeObject = new GameObject(RouteName);
            Undo.RegisterCreatedObjectUndo(routeObject, "Create Ground Prototype Route");
            routeObject.transform.SetParent(root.transform, false);

            Phase2EnemyRoute route = Undo.AddComponent<Phase2EnemyRoute>(routeObject);

            ConfigureRoute(route, tiles);
            ConfigureBattleController(battleController);
            ConfigureCamera();

            EditorUtility.SetDirty(combatLoop);
            EditorUtility.SetDirty(battleController);
            EditorUtility.SetDirty(route);

            EditorSceneManager.MarkSceneDirty(root.scene);
            AssetDatabase.SaveAssets();

            Selection.activeGameObject = root;

            Debug.Log("Ground 실제 게임형 검증맵 생성 완료: 17x9 전장 / Ground + HighGround / 입구 + 출구 / 정식 UnitCatalog + EnemyCatalog 연결.", root);
        }

        private static Dictionary<Vector2Int, Phase2GroundTile> CreateMap(Transform parent)
        {
            EnsureFolder(MaterialFolder);

            Material groundMaterial = GetOrCreateMaterial(GroundMaterialPath, new Color(0.22f, 0.27f, 0.32f, 1f));
            Material routeMaterial = GetOrCreateMaterial(RouteMaterialPath, new Color(0.12f, 0.18f, 0.24f, 1f));
            Material highGroundMaterial = GetOrCreateMaterial(HighGroundMaterialPath, new Color(0.34f, 0.41f, 0.48f, 1f));
            Material entranceMaterial = GetOrCreateMaterial(EntranceMaterialPath, new Color(0.45f, 0.12f, 0.12f, 1f));
            Material exitMaterial = GetOrCreateMaterial(ExitMaterialPath, new Color(0.10f, 0.32f, 0.55f, 1f));

            HashSet<Vector2Int> routeSet = new HashSet<Vector2Int>(RouteCoordinates);
            Dictionary<Vector2Int, Phase2GroundTile> result = new Dictionary<Vector2Int, Phase2GroundTile>();

            Vector2Int entrance = RouteCoordinates[0];
            Vector2Int exit = RouteCoordinates[RouteCoordinates.Length - 1];

            for (int z = -4; z <= 4; z++)
            {
                for (int x = -8; x <= 8; x++)
                {
                    Vector2Int coordinate = new Vector2Int(x, z);
                    bool isRoute = routeSet.Contains(coordinate);
                    bool isEntrance = coordinate == entrance;
                    bool isExit = coordinate == exit;
                    bool isHighGround = IsHighGroundCoordinate(coordinate);

                    GameObject tileObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    tileObject.name = GetTileName(coordinate, isHighGround, isRoute, isEntrance, isExit);
                    Undo.RegisterCreatedObjectUndo(tileObject, "Create Ground Prototype Tile");
                    tileObject.transform.SetParent(parent, false);
                    tileObject.transform.localPosition = new Vector3(x, isHighGround ? 0.17f : 0f, z);
                    tileObject.transform.localScale = isHighGround ? new Vector3(0.94f, 0.34f, 0.94f) : new Vector3(0.94f, 0.08f, 0.94f);

                    Collider collider = tileObject.GetComponent<Collider>();

                    if (collider != null)
                    {
                        Object.DestroyImmediate(collider);
                    }

                    Renderer renderer = tileObject.GetComponent<Renderer>();

                    if (renderer != null)
                    {
                        renderer.sharedMaterial = isEntrance ? entranceMaterial : isExit ? exitMaterial : isHighGround ? highGroundMaterial : isRoute ? routeMaterial : groundMaterial;
                    }

                    Phase2GroundTile tile = Undo.AddComponent<Phase2GroundTile>(tileObject);
                    SerializedObject tileObjectSO = new SerializedObject(tile);
                    tileObjectSO.FindProperty("coordinate").vector2IntValue = coordinate;
                    tileObjectSO.FindProperty("surface").enumValueIndex = isHighGround ? (int)Phase2TileSurface.HighGround : (int)Phase2TileSurface.Ground;
                    tileObjectSO.ApplyModifiedPropertiesWithoutUndo();

                    result.Add(coordinate, tile);
                    EditorUtility.SetDirty(tile);
                }
            }

            return result;
        }

        private static bool IsHighGroundCoordinate(Vector2Int coordinate)
        {
            if (coordinate.y == 3 && ((coordinate.x >= -6 && coordinate.x <= -3) || (coordinate.x >= 3 && coordinate.x <= 6)))
            {
                return true;
            }

            if (coordinate.y == -2 && ((coordinate.x >= -6 && coordinate.x <= -4) || (coordinate.x >= 4 && coordinate.x <= 6)))
            {
                return true;
            }

            return coordinate.y == -3 && (coordinate.x == -5 || coordinate.x == -4 || coordinate.x == 4 || coordinate.x == 5);
        }

        private static string GetTileName(Vector2Int coordinate, bool isHighGround, bool isRoute, bool isEntrance, bool isExit)
        {
            if (isEntrance)
            {
                return $"Entrance_{coordinate.x}_{coordinate.y}";
            }

            if (isExit)
            {
                return $"Exit_{coordinate.x}_{coordinate.y}";
            }

            if (isHighGround)
            {
                return $"HighGround_{coordinate.x}_{coordinate.y}";
            }

            if (isRoute)
            {
                return $"Route_{coordinate.x}_{coordinate.y}";
            }

            return $"Ground_{coordinate.x}_{coordinate.y}";
        }

        private static void ConfigureRoute(Phase2EnemyRoute route, Dictionary<Vector2Int, Phase2GroundTile> tiles)
        {
            SerializedObject routeSO = new SerializedObject(route);
            SerializedProperty routeTiles = routeSO.FindProperty("routeTiles");
            routeTiles.arraySize = RouteCoordinates.Length;

            for (int i = 0; i < RouteCoordinates.Length; i++)
            {
                routeTiles.GetArrayElementAtIndex(i).objectReferenceValue = tiles[RouteCoordinates[i]];
            }

            routeSO.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureBattleController(GroundBattlePrototypeController controller)
        {
            UnitCatalog unitCatalog = AssetDatabase.LoadAssetAtPath<UnitCatalog>(UnitCatalogPath);
            EnemyCatalog enemyCatalog = AssetDatabase.LoadAssetAtPath<EnemyCatalog>(EnemyCatalogPath);

            SerializedObject controllerSO = new SerializedObject(controller);
            controllerSO.FindProperty("unitCatalog").objectReferenceValue = unitCatalog;
            controllerSO.FindProperty("enemyCatalog").objectReferenceValue = enemyCatalog;
            controllerSO.FindProperty("initialUnitCount").intValue = 8;
            controllerSO.FindProperty("initialHighGroundUnitCount").intValue = 3;
            controllerSO.FindProperty("enemySpawnInterval").floatValue = 2f;
            controllerSO.FindProperty("replacementDelay").floatValue = 0.5f;
            controllerSO.FindProperty("maxExitHp").intValue = 10;
            controllerSO.FindProperty("airHeight").floatValue = 2f;
            controllerSO.ApplyModifiedPropertiesWithoutUndo();

            if (unitCatalog == null || enemyCatalog == null)
            {
                Debug.LogError($"Ground Prototype Catalog 연결 실패: UnitCatalog={unitCatalog != null}, EnemyCatalog={enemyCatalog != null}", controller);
            }
        }

        private static void ConfigureCamera()
        {
            Camera mainCamera = Camera.main;

            if (mainCamera == null)
            {
                GameObject cameraObject = GameObject.Find("Main Camera");

                if (cameraObject != null)
                {
                    mainCamera = cameraObject.GetComponent<Camera>();
                }
            }

            if (mainCamera == null)
            {
                Debug.LogError("Main Camera를 찾지 못했습니다.");
                return;
            }

            Undo.RecordObject(mainCamera.transform, "Configure Ground Prototype Camera");
            Undo.RecordObject(mainCamera, "Configure Ground Prototype Camera");

            mainCamera.transform.position = new Vector3(0.45f, 11f, -11f);
            mainCamera.transform.rotation = Quaternion.Euler(48f, 0f, 0f);
            mainCamera.transform.localScale = Vector3.one;
            mainCamera.orthographic = false;
            mainCamera.fieldOfView = 70f;
            mainCamera.nearClipPlane = 0.3f;
            mainCamera.farClipPlane = 1000f;

            EditorUtility.SetDirty(mainCamera.transform);
            EditorUtility.SetDirty(mainCamera);
        }

        private static Material GetOrCreateMaterial(string path, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");

                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureFolder(string folderPath)
        {
            string[] segments = folderPath.Split('/');
            string currentPath = segments[0];

            for (int i = 1; i < segments.Length; i++)
            {
                string nextPath = currentPath + "/" + segments[i];

                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, segments[i]);
                }

                currentPath = nextPath;
            }
        }
    }
}