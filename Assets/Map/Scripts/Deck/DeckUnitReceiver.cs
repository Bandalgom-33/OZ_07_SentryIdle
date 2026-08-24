using UnityEngine;
using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Runtime;
using System.Collections.Generic;

public class DeckUnitReceiver : MonoBehaviour
{
    [SerializeField] private MapGenerator mapGenerator;

    private readonly List<UnitDataSO> deckUnits = new List<UnitDataSO>();

    private void OnEnable()
    {
        EventBus.Subscribe<NormalDeckChangedEvent>(OnNormalDeckChanged);
    }

    private void Start()
    {
        if (mapGenerator == null)
        {
            mapGenerator = FindFirstObjectByType<MapGenerator>();
        }

        if (DeckManager.Instance == null) return;
        

        List<DeckSlotUnitEntry> currentUnits = DeckManager.Instance.GetActiveDeckSlotEntries(DeckType.Normal);

        UpdateDeckUnits(currentUnits);

        for (int i = 0; i < deckUnits.Count; i++)
        {
            SpawnDeckUnit(deckUnits[i]);
        }
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<NormalDeckChangedEvent>(OnNormalDeckChanged);
    }

    private void OnNormalDeckChanged(NormalDeckChangedEvent evt)
    {
        UpdateDeckUnits(evt.activeUnits);
    }

    private void UpdateDeckUnits(IReadOnlyList<DeckSlotUnitEntry> units)
    {
        deckUnits.Clear();

        for (int i = 0; i < units.Count; i++)
        {
            UnitDataSO unitData = units[i].unitData;

            if (unitData == null) continue;
            
            deckUnits.Add(unitData);
        }
    }

    private void SpawnDeckUnit(UnitDataSO unitData)
    {
        if (unitData == null) return;
        if (unitData.UnitPrefab == null) return;
        if (mapGenerator == null) return;

        TileType targetTileType;

        switch (unitData.Placement)
        {
            case UnitPlacement.Ground:
                targetTileType = TileType.Path;
                break;

            case UnitPlacement.HighGround:
                targetTileType = TileType.HighGround;
                break;

            case UnitPlacement.GroundAndHighGround:
                targetTileType = Random.value > 0.5f
                    ? TileType.Path
                    : TileType.HighGround;
                break;

            default:
                return;
        }

        TileNode targetTile = mapGenerator.FindRandomDeployableTile(targetTileType);

        if (targetTile == null) return;
        
        Vector3 worldPosition = mapGenerator.MapRenderer.GridToWorld(targetTile.GridPosition);

        if (targetTile.TileType == TileType.HighGround)
        {
            worldPosition.y += 0.8f;
        }
        else
        {
            worldPosition.y += 0.5f;
        }

        GameObject instance =
            Instantiate(
                unitData.UnitPrefab,
                worldPosition,
                unitData.UnitPrefab.transform.rotation
            );

        UnitRuntimeState runtimeState =
            instance.GetComponent<UnitRuntimeState>();

        if (runtimeState == null)
        {
            Destroy(instance);
            return;
        }

        runtimeState.InitializeRuntime();

        if (runtimeState.GridPosition != null)
        {
            runtimeState.GridPosition.Initialize(
                targetTile.GridPosition,
                GridFacingDirection.East,
                CombatTargetLayer.Ground
            );
        }

        targetTile.SetOccupied(true);
    }
}