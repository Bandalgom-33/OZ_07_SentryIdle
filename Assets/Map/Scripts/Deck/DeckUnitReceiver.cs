using UnityEngine;
using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Runtime;
using System.Collections.Generic;

public class DeckUnitReceiver : MonoBehaviour
{
    [SerializeField] private MapGenerator mapGenerator;
    

    private readonly List<UnitDataSO> deckUnits = new List<UnitDataSO>();
    
    private const int MaxFieldUnitCount = 10;

    private readonly HashSet<string> spawnedUnitIds = new HashSet<string>();
    //덱 추가, 제거 할때 유닛을 찾기위한 딕셔너리
    private readonly Dictionary<string, UnitRuntimeState> spawnedUnits = new Dictionary<string, UnitRuntimeState>();
    // 유닛을 제거할 때 해당 타일 점유 확인
    private readonly Dictionary<string, TileNode> spawnedUnitTiles = new Dictionary<string, TileNode>();
    
    private bool isCheckingWaitingUnits;

    private void OnEnable()
    {
        EventBus.Subscribe<NormalDeckChangedEvent>(OnNormalDeckChanged);

        CurrencyManager.OnDpCostChange += OnDpCostChanged;
    }

    private void Start()
    {
        if (mapGenerator == null)
        {
            mapGenerator = FindFirstObjectByType<MapGenerator>();
        }

        if (mapGenerator == null) return;

        if (mapGenerator.IsMapGenerated)
        {
            SpawnCurrentDeck();
        }
        else
        {
            mapGenerator.OnMapGenerated += HandleMapGenerated;
        }
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<NormalDeckChangedEvent>(OnNormalDeckChanged);

        CurrencyManager.OnDpCostChange -= OnDpCostChanged;

        if (mapGenerator != null)
        {
            mapGenerator.OnMapGenerated -= HandleMapGenerated;
        }
    }
    
    private void OnDpCostChanged(int currentDp)
    {
        Debug.Log($"[DeckUnitReceiver] DP 변경 감지: {currentDp}");
        //재소환 시도
        TrySpawnWaitingDeckUnits();
    }

    private void OnNormalDeckChanged(NormalDeckChangedEvent evt)
    {
        //새덱의 UnitID 목록 만들기
        HashSet<string> newDeckUnitIds = new HashSet<string>();

        //찾기
        for (int i = 0; i < evt.activeUnits.Count; i++)
        {
            UnitDataSO unitData = evt.activeUnits[i].unitData;

            if (unitData == null) continue;

            newDeckUnitIds.Add(unitData.UnitId);
        }

        // 유닛 찾기
        List<string> removeUnitIds = new List<string>();
        
        foreach (string spawnedUnitId in spawnedUnitIds)
        {
            if (!newDeckUnitIds.Contains(spawnedUnitId))
            {
                removeUnitIds.Add(spawnedUnitId);
            }
        }

        for (int i = 0; i < removeUnitIds.Count; i++)
        {
            RemoveSpawnedUnit(removeUnitIds[i]);
        }

        UpdateDeckUnits(evt.activeUnits);

        for (int i = 0; i < deckUnits.Count; i++)
        {
            UnitDataSO unitData = deckUnits[i];

            if (unitData == null) continue;

            if (!spawnedUnitIds.Contains(unitData.UnitId))
            {
                SpawnDeckUnit(unitData);
            }
        }
    }
    
    private void RemoveSpawnedUnit(string unitId)
    {
        if (string.IsNullOrEmpty(unitId)) return;

        // 점유 중이던 타일 해제
        if (spawnedUnitTiles.TryGetValue(unitId, out TileNode occupiedTile))
        {
            if (occupiedTile != null)
            {
                occupiedTile.SetOccupied(false);
            }

            spawnedUnitTiles.Remove(unitId);
        }

        // 실제 필드 유닛 제거
        if (spawnedUnits.TryGetValue(unitId, out UnitRuntimeState unitState))
        {
            if (unitState != null)
            {
                Destroy(unitState.gameObject);
            }

            spawnedUnits.Remove(unitId);
        }

        // 필드에 존재 중이라는 기록 제거
        spawnedUnitIds.Remove(unitId);

        Debug.Log($"[DeckUnitReceiver] 덱 변경으로 유닛 제거: {unitId}");
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
        Debug.Log(
            $"[DeckUnitReceiver] 실제 소환: " +
            $"{unitData.DisplayName} / {unitData.UnitId}"
        );
        
        //이미 10마리면 소환하지 않도록, 같은 유닛이 이미 있으면 동일하게 소환 x
        if (spawnedUnitIds.Count >= MaxFieldUnitCount) return;
        if (spawnedUnitIds.Contains(unitData.UnitId)) return;
        

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
        
        if (CurrencyManager.Instance == null)
        {
            Debug.LogWarning("[DeckUnitReceiver] CurrencyManager.Instance가 없습니다.");
            return;
        }

        int summonCost = unitData.SummonCost;

        if (!CurrencyManager.Instance.TrySpendDpCost(summonCost))
        {
            Debug.Log(
                $"[DeckUnitReceiver] DP 부족 - " +
                $"{unitData.DisplayName} 필요 DP: {summonCost}"
            );

            return;
        }
        
        
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
        
        spawnedUnitIds.Add(unitData.UnitId);
        //해당 유닛 찾기
        spawnedUnits[unitData.UnitId] = runtimeState;
        //해당 타일 찾기
        spawnedUnitTiles[unitData.UnitId] = targetTile;
    }
    
    private void HandleMapGenerated()
    {
        if (mapGenerator != null)
        {
            mapGenerator.OnMapGenerated -= HandleMapGenerated;
        }

        SpawnCurrentDeck();
    }
    
    //덱 읽고 소환
    private void SpawnCurrentDeck()
    {
        if (DeckManager.Instance == null) return;

        List<DeckSlotUnitEntry> currentUnits =
            DeckManager.Instance.GetActiveDeckSlotEntries(DeckType.Normal);

        Debug.Log(
            $"[DeckUnitReceiver] 현재 덱 유닛 수: {currentUnits.Count}"
        );

        UpdateDeckUnits(currentUnits);

        for (int i = 0; i < deckUnits.Count; i++)
        {
            SpawnDeckUnit(deckUnits[i]);
        }
    }
    
    private void TrySpawnWaitingDeckUnits()
    {
        if (isCheckingWaitingUnits)
            return;

        isCheckingWaitingUnits = true;

        for (int i = 0; i < deckUnits.Count; i++)
        {
            UnitDataSO unitData = deckUnits[i];

            if (unitData == null)
                continue;

            // 이미 필드에 있으면 건너뜀
            if (spawnedUnitIds.Contains(unitData.UnitId))
                continue;

            SpawnDeckUnit(unitData);
        }

        isCheckingWaitingUnits = false;
    }
}