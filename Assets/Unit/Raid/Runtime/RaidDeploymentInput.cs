using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Raid.Data;
using EndlessGuard.Unit.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

namespace EndlessGuard.Unit.Raid.Runtime
{
    [DisallowMultipleComponent]
    public sealed class RaidDeploymentInput : MonoBehaviour
    {
        private RaidBattleController battle;
        private RaidBoardRuntime boardRuntime;
        private RaidRosterRuntime roster;
        private RaidDeploymentRuntime deployment;
        private RaidDeploymentPlanner planner;
        private RaidHudView hud;
        private Camera worldCamera;
        private RaidRosterSlotState selectedSlot;
        private Vector2Int pendingTile;
        private bool isFacingDrag;
        private GridFacingDirection previewFacing = GridFacingDirection.North;
        private RaidDeployTileDisplay deployTileDisplay;

        private void Awake()
        {
            ResolveDependencies();
            deployTileDisplay = new RaidDeployTileDisplay();
        }

        private void OnEnable()
        {
            ResolveDependencies();

            if (battle != null)
            {
                battle.OnModeChanged += HandleModeChanged;
                battle.OnRaidEnded += HandleRaidEnded;
                battle.OnPhaseTransitionStarted += HandlePhaseTransitionStarted;
            }
        }

        private void OnDisable()
        {
            if (battle != null)
            {
                battle.OnModeChanged -= HandleModeChanged;
                battle.OnRaidEnded -= HandleRaidEnded;
                battle.OnPhaseTransitionStarted -= HandlePhaseTransitionStarted;
            }

            ClearPlacementSelection(true);
            deployTileDisplay?.Dispose();
        }

        private void Update()
        {
            if (battle == null || boardRuntime == null || roster == null || deployment == null || battle.State != RaidBattleState.Running || battle.IsTransitioning)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                ClearPlacementSelection(true);
                return;
            }

            Vector2 pointer = Input.mousePosition;

            if (Input.GetMouseButtonDown(0))
            {
                if (TryHandleRosterSlotClick(pointer))
                {
                    return;
                }

                if (IsPointerOverUi())
                {
                    return;
                }

                if (battle.Mode == RaidBattleMode.Manual && selectedSlot != null && selectedSlot.CanDeploy)
                {
                    if (TryGetBoardTile(pointer, out Vector2Int tile) && deployment.IsTileDeployable(selectedSlot.UnitData, tile))
                    {
                        pendingTile = tile;
                        previewFacing = planner != null ? planner.GetBestFacing(selectedSlot.UnitData, tile) : GridFacingDirection.North;
                        isFacingDrag = true;
                        AttackRangeDisplay.ShowPlacement(selectedSlot.UnitData, pendingTile, previewFacing);
                        return;
                    }
                }

                HandleWorldSelection(pointer);
            }

            if (isFacingDrag && Input.GetMouseButton(0))
            {
                UpdateFacingPreview(pointer);
            }

            if (isFacingDrag && Input.GetMouseButtonUp(0))
            {
                CommitManualPlacement();
            }
        }

        private bool TryHandleRosterSlotClick(Vector2 pointer)
        {
            if (hud == null || roster == null || battle == null)
            {
                return false;
            }

            for (int i = 0; i < RaidRosterRuntime.SlotsPerTeam; i++)
            {
                RectTransform rect = hud.GetRosterSlotRect(i);
                if (rect == null || !RectTransformUtility.RectangleContainsScreenPoint(rect, pointer, null))
                {
                    continue;
                }

                RaidRosterSlotState slot = roster.GetSlot(battle.SelectedTeamIndex, i);
                if (slot == null || !slot.HasUnit)
                {
                    ClearPlacementSelection(true);
                    return true;
                }

                if (slot.Status == RaidRosterSlotStatus.Deployed && slot.DeployedUnit != null)
                {
                    ClearPlacementSelection(false);
                    AttackRangeDisplay.ShowSelected(slot.DeployedUnit);
                    return true;
                }

                if (battle.Mode != RaidBattleMode.Manual || !slot.CanDeploy)
                {
                    return true;
                }

                selectedSlot = slot;
                hud.SetDeploymentSelection(slot.TeamIndex, slot.SlotIndex);
                deployTileDisplay.Show(slot.UnitData, boardRuntime, deployment, battle.Config);
                AttackRangeDisplay.Hide();
                return true;
            }

            return false;
        }

        private void HandleWorldSelection(Vector2 pointer)
        {
            if (!TryGetBoardTile(pointer, out Vector2Int tile))
            {
                AttackRangeDisplay.Hide();
                return;
            }

            if (deployment.TryGetDeployedUnitAt(tile, out UnitRuntimeState unit))
            {
                ClearPlacementSelection(false);
                AttackRangeDisplay.ShowSelected(unit);
            }
            else
            {
                AttackRangeDisplay.Hide();
            }
        }

        private void UpdateFacingPreview(Vector2 pointer)
        {
            if (selectedSlot == null || !TryGetWorldPoint(pointer, out Vector3 worldPoint))
            {
                return;
            }

            RaidBoard board = boardRuntime.Board;
            Vector3 center = board.TileToWorld(pendingTile);
            Vector3 delta = worldPoint - center;
            delta.y = 0f;
            float deadZone = battle.Config != null ? battle.Config.ManualFacingDragDistance : 0.45f;

            if (delta.magnitude < deadZone)
            {
                return;
            }

            GridFacingDirection facing;
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.z))
            {
                facing = delta.x >= 0f ? GridFacingDirection.East : GridFacingDirection.West;
            }
            else
            {
                facing = delta.z >= 0f ? GridFacingDirection.North : GridFacingDirection.South;
            }

            if (facing == previewFacing)
            {
                return;
            }

            previewFacing = facing;
            AttackRangeDisplay.ShowPlacement(selectedSlot.UnitData, pendingTile, previewFacing);
        }

        private void CommitManualPlacement()
        {
            isFacingDrag = false;

            if (selectedSlot == null || battle.Mode != RaidBattleMode.Manual)
            {
                AttackRangeDisplay.Hide();
                return;
            }

            if (deployment.TryDeploy(selectedSlot, pendingTile, previewFacing, false, out UnitRuntimeState deployedUnit))
            {
                ClearPlacementSelection(false);
                AttackRangeDisplay.ShowSelected(deployedUnit);
            }
            else
            {
                deployTileDisplay.Show(selectedSlot.UnitData, boardRuntime, deployment, battle.Config);
                AttackRangeDisplay.ShowPlacement(selectedSlot.UnitData, pendingTile, previewFacing);
            }
        }

        private void ClearPlacementSelection(bool hideAttackRange)
        {
            selectedSlot = null;
            isFacingDrag = false;
            deployTileDisplay?.Hide();
            hud?.ClearDeploymentSelection();

            if (hideAttackRange)
            {
                AttackRangeDisplay.Hide();
            }
        }

        private bool TryGetBoardTile(Vector2 screenPosition, out Vector2Int tile)
        {
            tile = default;
            if (!TryGetWorldPoint(screenPosition, out Vector3 worldPoint) || boardRuntime == null || boardRuntime.Board == null)
            {
                return false;
            }

            return boardRuntime.Board.TryWorldToTile(worldPoint, out tile);
        }

        private bool TryGetWorldPoint(Vector2 screenPosition, out Vector3 worldPoint)
        {
            worldPoint = default;
            worldCamera = worldCamera != null ? worldCamera : Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();

            if (worldCamera == null || boardRuntime == null || boardRuntime.Board == null)
            {
                return false;
            }

            Vector3 boardPoint = boardRuntime.Board.TileToWorld(Vector2Int.zero);
            Plane plane = new Plane(Vector3.up, boardPoint);
            Ray ray = worldCamera.ScreenPointToRay(screenPosition);

            if (!plane.Raycast(ray, out float distance))
            {
                return false;
            }

            worldPoint = ray.GetPoint(distance);
            return true;
        }

        private bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private bool ResolveDependencies()
        {
            battle = battle != null ? battle : GetComponent<RaidBattleController>();
            boardRuntime = boardRuntime != null ? boardRuntime : GetComponent<RaidBoardRuntime>();
            roster = roster != null ? roster : GetComponent<RaidRosterRuntime>();
            deployment = deployment != null ? deployment : GetComponent<RaidDeploymentRuntime>();
            planner = planner != null ? planner : GetComponent<RaidDeploymentPlanner>();
            hud = hud != null ? hud : GetComponent<RaidHudView>();
            return battle != null && boardRuntime != null && roster != null && deployment != null;
        }

        private void HandleModeChanged(RaidBattleMode mode)
        {
            if (mode != RaidBattleMode.Manual)
            {
                ClearPlacementSelection(true);
            }
        }

        private void HandleRaidEnded(RaidBattleResult result)
        {
            ClearPlacementSelection(true);
        }

        private void HandlePhaseTransitionStarted(RaidPhaseTransitionInfo info)
        {
            ClearPlacementSelection(true);
        }
    }

    internal sealed class RaidDeployTileDisplay
    {
        private const string RootName = "RaidDeployTileDisplay";
        private readonly System.Collections.Generic.List<GameObject> tiles = new System.Collections.Generic.List<GameObject>();
        private Transform root;
        private Material material;

        public void Show(UnitDataSO unitData, RaidBoardRuntime boardRuntime, RaidDeploymentRuntime deployment, RaidBattleConfigSO config)
        {
            Hide();
            if (unitData == null || boardRuntime == null || boardRuntime.Board == null || deployment == null)
            {
                return;
            }

            RaidBoard board = boardRuntime.Board;
            int visible = 0;

            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    Vector2Int coordinate = new Vector2Int(x, y);
                    if (!deployment.IsTileDeployable(unitData, coordinate))
                    {
                        continue;
                    }

                    RaidTile tileData = board.GetTile(coordinate);
                    float height = config != null ? (tileData.IsHighGroundDeployable ? config.HighGroundDeployHeight : config.GroundDeployHeight) : (tileData.IsHighGroundDeployable ? 0.82f : 0.08f);
                    GameObject tile = GetTile(visible++);
                    tile.transform.position = board.TileToWorld(coordinate, height + 0.018f);
                    tile.transform.localScale = new Vector3(board.TileSize * 0.86f, 0.022f, board.TileSize * 0.86f);
                    tile.SetActive(true);
                }
            }
        }

        public void Hide()
        {
            for (int i = 0; i < tiles.Count; i++)
            {
                if (tiles[i] != null)
                {
                    tiles[i].SetActive(false);
                }
            }
        }

        public void Dispose()
        {
            if (root != null)
            {
                Object.Destroy(root.gameObject);
            }

            if (material != null)
            {
                Object.Destroy(material);
            }

            root = null;
            material = null;
            tiles.Clear();
        }

        private GameObject GetTile(int index)
        {
            EnsureRoot();
            while (tiles.Count <= index)
            {
                tiles.Add(CreateTile());
            }

            return tiles[index];
        }

        private void EnsureRoot()
        {
            if (root != null)
            {
                return;
            }

            GameObject rootObject = new GameObject(RootName);
            root = rootObject.transform;
        }

        private GameObject CreateTile()
        {
            GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tile.name = "DeployTile";
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
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            tile.SetActive(false);
            return tile;
        }

        private Material GetMaterial()
        {
            if (material != null)
            {
                return material;
            }

            Material source = Resources.Load<Material>("Materials/MAT_AttackRange");
            if (source != null)
            {
                material = new Material(source);
                material.name = "MAT_RaidDeployPreview_Runtime";
                material.SetColor("_BaseColor", new Color(0.08f, 0.88f, 0.62f, 0.38f));
                material.SetColor("_Color", new Color(0.08f, 0.88f, 0.62f, 0.38f));
                return material;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            material = new Material(shader);
            material.color = new Color(0.08f, 0.88f, 0.62f, 0.38f);
            return material;
        }
    }
}
