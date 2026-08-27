using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    public static class AttackRangeDisplay
    {
        private const string RootName = "AttackRangeDisplay";
        private const string TileName = "AttackRangeTile";
        private const string MaterialResourcePath = "Materials/MAT_AttackRange";

        private static readonly List<GameObject> tiles = new List<GameObject>();

        private static UnitRuntimeState selectedUnit;
        private static Transform root;
        private static Material material;
        private static bool visible;

        public static bool IsVisible => visible;
        public static UnitRuntimeState SelectedUnit => selectedUnit;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            selectedUnit = null;
            root = null;
            material = null;
            tiles.Clear();
            visible = false;
        }

        public static bool ShowPlacement(UnitDataSO unitData, Vector2Int tileCoordinate, GridFacingDirection facingDirection)
        {
            UnsubscribeSelectedUnit();
            selectedUnit = null;
            return Show(unitData, tileCoordinate, facingDirection);
        }

        public static bool ShowSelected(UnitRuntimeState unit)
        {
            if (!CanDisplay(unit))
            {
                Hide();
                return false;
            }

            if (selectedUnit != unit)
            {
                UnsubscribeSelectedUnit();
                selectedUnit = unit;
                SubscribeSelectedUnit();
            }

            return RefreshSelected();
        }

        public static bool RefreshSelected()
        {
            if (!CanDisplay(selectedUnit))
            {
                Hide();
                return false;
            }

            return Show(selectedUnit.DataLink.UnitData, selectedUnit.GridPosition.TileCoordinate, selectedUnit.GridPosition.FacingDirection);
        }

        public static void Hide()
        {
            UnsubscribeSelectedUnit();
            selectedUnit = null;
            visible = false;

            for (int i = 0; i < tiles.Count; i++)
            {
                GameObject tile = tiles[i];

                if (tile != null)
                {
                    tile.SetActive(false);
                }
            }
        }

        private static bool Show(UnitDataSO unitData, Vector2Int originTile, GridFacingDirection facingDirection)
        {
            if (unitData == null || unitData.AttackSettings == null || unitData.AttackSettings.AttackMode == AttackMode.None || unitData.AttackSettings.TargetCount <= 0 || unitData.AttackSettings.BasicAttackRange == null || !AttackRangeTileService.HasProvider)
            {
                HideVisualsOnly();
                return false;
            }

            AttackSettings attackSettings = unitData.AttackSettings;
            IReadOnlyList<Vector2Int> attackTiles = attackSettings.BasicAttackRange.AttackTiles;
            int visibleCount = 0;

            for (int i = 0; i < attackTiles.Count; i++)
            {
                Vector2Int patternTile = attackTiles[i];

                if (!BasicAttackRangeEvaluator.IsWithinAttackDistance(attackSettings, patternTile))
                {
                    continue;
                }

                Vector2Int relativeTile = BasicAttackRangeEvaluator.ConvertPatternTileToWorldTile(
                    patternTile,
                    attackSettings.RangeRotationMode,
                    facingDirection);
                Vector2Int worldTile = originTile + relativeTile;

                if (!AttackRangeTileService.TryGetTile(worldTile, out AttackRangeTile tileData))
                {
                    continue;
                }

                GameObject tile = GetTile(visibleCount);
                tile.transform.position = tileData.WorldPosition;
                tile.transform.localScale = tileData.Scale;
                tile.SetActive(true);
                visibleCount++;
            }

            for (int i = visibleCount; i < tiles.Count; i++)
            {
                GameObject tile = tiles[i];

                if (tile != null)
                {
                    tile.SetActive(false);
                }
            }

            visible = visibleCount > 0;
            return visible;
        }

        private static void HideVisualsOnly()
        {
            visible = false;

            for (int i = 0; i < tiles.Count; i++)
            {
                GameObject tile = tiles[i];

                if (tile != null)
                {
                    tile.SetActive(false);
                }
            }
        }

        private static GameObject GetTile(int index)
        {
            EnsureRoot();

            while (tiles.Count <= index)
            {
                tiles.Add(CreateTile());
            }

            if (tiles[index] == null)
            {
                tiles[index] = CreateTile();
            }

            return tiles[index];
        }

        private static void EnsureRoot()
        {
            if (root != null)
            {
                return;
            }

            tiles.Clear();
            GameObject rootObject = new GameObject(RootName);
            root = rootObject.transform;
        }

        private static GameObject CreateTile()
        {
            GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tile.name = TileName;
            tile.transform.SetParent(root, false);

            Collider collider = tile.GetComponent<Collider>();

            if (collider != null)
            {
                collider.enabled = false;
            }

            Renderer renderer = tile.GetComponent<Renderer>();

            if (renderer != null)
            {
                renderer.sharedMaterial = GetMaterial();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            tile.SetActive(false);
            return tile;
        }

        private static Material GetMaterial()
        {
            if (material != null)
            {
                return material;
            }

            material = Resources.Load<Material>(MaterialResourcePath);

            if (material == null)
            {
                Debug.LogError($"공격범위 Material을 찾지 못했습니다. Assets/Unit/Resources/{MaterialResourcePath}.mat 경로를 확인하세요.");
            }

            return material;
        }

        private static bool CanDisplay(UnitRuntimeState unit)
        {
            return unit != null && unit.gameObject.activeInHierarchy && unit.IsInitialized && !unit.IsSummon && unit.Health != null && !unit.Health.IsDead && unit.DataLink != null && unit.DataLink.HasData && unit.GridPosition != null && unit.GridPosition.IsInitialized;
        }

        private static void SubscribeSelectedUnit()
        {
            if (selectedUnit == null || selectedUnit.GridPosition == null)
            {
                return;
            }

            selectedUnit.GridPosition.OnTileChanged += HandleGridChanged;
            selectedUnit.GridPosition.OnFacingChanged += HandleGridChanged;
        }

        private static void UnsubscribeSelectedUnit()
        {
            if (selectedUnit == null || selectedUnit.GridPosition == null)
            {
                return;
            }

            selectedUnit.GridPosition.OnTileChanged -= HandleGridChanged;
            selectedUnit.GridPosition.OnFacingChanged -= HandleGridChanged;
        }

        private static void HandleGridChanged(CombatGridPosition gridPosition)
        {
            RefreshSelected();
        }

    }
}