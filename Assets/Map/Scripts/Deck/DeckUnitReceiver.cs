using System.Collections;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Runtime;
using UnityEngine;

public class DeckUnitReceiver : MonoBehaviour
{
    [Header("아군 배치 높이")]
    [SerializeField] private float groundUnitHeight = 1.0f;
    [SerializeField] private float highGroundUnitHeight = 1.25f;

    [Header("재소환 쿨타임 설정")]
    [SerializeField, Min(0f)] private float respawnCooldown = 5.0f;
    
    [Header("소환 딜레이 설정")]
    [Tooltip("맵 생성/진입 후 초기 소환 개시까지의 딜레이(초)")]
    [SerializeField, Min(0f)] private float initialSpawnDelay = 0.5f;

    [Tooltip("유닛 1기 소환 성공 후 다음 유닛 소환 시도까지의 텀(초)")]
    [SerializeField, Min(0f)] private float spawnInterval = 0.5f;
    
    [SerializeField] private MapGenerator mapGenerator;
    
    private readonly List<UnitDataSO> deckUnits = new List<UnitDataSO>();
    
    private const int MaxFieldUnitCount = 10;

    private readonly HashSet<string> spawnedUnitIds = new HashSet<string>();
    // 덱 추가/제거 및 유닛 검색용 딕셔너리
    private readonly Dictionary<string, UnitRuntimeState> spawnedUnits = new Dictionary<string, UnitRuntimeState>();
    // 유닛별 점유 타일 매핑 딕셔너리
    private readonly Dictionary<string, TileNode> spawnedUnitTiles = new Dictionary<string, TileNode>();
    // 사망 이벤트 처리를 위한 CombatHealth 매핑 딕셔너리
    private readonly Dictionary<CombatHealth, string> healthToUnitId = new Dictionary<CombatHealth, string>();
    private readonly Dictionary<CombatHealth, TileNode> healthToTileNode = new Dictionary<CombatHealth, TileNode>();
    // 사망 후 개별 쿨타임 진행 중인 유닛 ID 및 코루틴 추적
    private readonly HashSet<string> coolingDownUnitIds = new HashSet<string>();
    private readonly Dictionary<string, Coroutine> respawnCoroutines = new Dictionary<string, Coroutine>();
    
    private Coroutine initialSpawnRoutine;
    private Coroutine spawnSequenceRoutine;
    private bool isSpawningSequence;

    private void OnEnable()
    {
        EventBus.Subscribe<NormalDeckChangedEvent>(OnNormalDeckChanged);
        CurrencyManager.OnDpCostChange += OnDpCostChanged;

        if (mapGenerator == null)
        {
            mapGenerator = FindFirstObjectByType<MapGenerator>();
        }

        // 맵 재생성 시마다 아군 유닛을 새 지형에 맞게 재배치하기 위해 OnMapGenerated 지속 구독
        if (mapGenerator != null)
        {
            mapGenerator.OnMapGenerated -= HandleMapGenerated;
            mapGenerator.OnMapGenerated += HandleMapGenerated;
        }
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
            if (initialSpawnRoutine != null) StopCoroutine(initialSpawnRoutine);
            initialSpawnRoutine = StartCoroutine(DelayedInitialSpawnRoutine());
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

    private void OnDestroy()
    {
        ClearAllUnits();
    }
    
    private void OnDpCostChanged(int currentDp)
    {
        // 1초마다 발생하는 빈번한 DP 변경 감지 로그는 콘솔 가독성을 위해 제외하고 재소환만 시도
        TrySpawnWaitingDeckUnits();
    }

    // 덱 변경 이벤트 콜백
    private void OnNormalDeckChanged(NormalDeckChangedEvent evt)
    {
        HashSet<string> newDeckUnitIds = new HashSet<string>();

        for (int i = 0; i < evt.activeUnits.Count; i++)
        {
            UnitDataSO unitData = evt.activeUnits[i].unitData;
            if (unitData == null || string.IsNullOrEmpty(unitData.UnitId)) continue;
            newDeckUnitIds.Add(unitData.UnitId);
        }

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
        TrySpawnWaitingDeckUnits();
    }
    
    // 소환된 유닛 필드에서 제거
    private void RemoveSpawnedUnit(string unitId)
    {
        if (string.IsNullOrEmpty(unitId)) return;

        if (spawnedUnitTiles.TryGetValue(unitId, out TileNode occupiedTile))
        {
            if (occupiedTile != null)
            {
                occupiedTile.SetOccupied(false);
            }

            spawnedUnitTiles.Remove(unitId);
        }

        if (spawnedUnits.TryGetValue(unitId, out UnitRuntimeState unitState))
        {
            if (unitState != null)
            {
                if (unitState.Health != null)
                {
                    unitState.Health.OnDied -= HandleUnitDied;
                    healthToUnitId.Remove(unitState.Health);
                    healthToTileNode.Remove(unitState.Health);
                }

                ReadyEffect.Hide(unitState);
                Destroy(unitState.gameObject);
            }

            spawnedUnits.Remove(unitId);
        }

        spawnedUnitIds.Remove(unitId);
        if (respawnCoroutines.TryGetValue(unitId, out Coroutine routine) && routine != null)
        {
            StopCoroutine(routine);
        }
        respawnCoroutines.Remove(unitId);
        coolingDownUnitIds.Remove(unitId);

        PublishFieldUnitCount();
        Debug.Log($"[DeckUnitReceiver] 덱 변경으로 유닛 제거: {unitId}");
    }

    // 덱 유닛 목록 갱신 (중복 유닛 적재 방지)
    private void UpdateDeckUnits(IReadOnlyList<DeckSlotUnitEntry> units)
    {
        deckUnits.Clear();
        HashSet<string> addedIds = new HashSet<string>();

        for (int i = 0; i < units.Count; i++)
        {
            UnitDataSO unitData = units[i].unitData;

            if (unitData == null || string.IsNullOrEmpty(unitData.UnitId)) continue;
            
            if (addedIds.Add(unitData.UnitId))
            {
                deckUnits.Add(unitData);
            }
        }
    }

    // 단일 유닛 소환 처리
    private bool SpawnDeckUnit(UnitDataSO unitData)
    {
        if (unitData == null || unitData.UnitPrefab == null || mapGenerator == null || mapGenerator.MapRenderer == null)
        {
            return false;
        }

        if (spawnedUnitIds.Count >= MaxFieldUnitCount)
        {
            return false;
        }

        if (spawnedUnitIds.Contains(unitData.UnitId))
        {
            return false;
        }

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
                targetTileType = Random.value > 0.5f ? TileType.Path : TileType.HighGround;
                break;

            default:
                return false;
        }

        TileNode targetTile = mapGenerator.FindRandomDeployableTile(targetTileType);
        if (targetTile == null && unitData.Placement == UnitPlacement.GroundAndHighGround)
        {
            TileType fallbackType = (targetTileType == TileType.Path) ? TileType.HighGround : TileType.Path;
            targetTile = mapGenerator.FindRandomDeployableTile(fallbackType);
        }

        if (targetTile == null)
        {
            return false;
        }
        
        if (CurrencyManager.Instance == null)
        {
            return false;
        }

        int summonCost = unitData.SummonCost;

        if (!CurrencyManager.Instance.HasDpCost(summonCost))
        {
            return false;
        }
        
        Vector3 worldPosition = mapGenerator.MapRenderer.GridToWorld(targetTile.GridPosition);
        if (targetTile.TileType == TileType.HighGround)
        {
            worldPosition.y += highGroundUnitHeight;
        }
        else
        {
            worldPosition.y += groundUnitHeight;
        }

        GameObject instance = Instantiate(
            unitData.UnitPrefab,
            worldPosition,
            unitData.UnitPrefab.transform.rotation
        );

        UnitRuntimeState runtimeState = instance.GetComponent<UnitRuntimeState>();

        if (runtimeState == null)
        {
            Destroy(instance);
            return false;
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

        if (!CurrencyManager.Instance.TrySpendDpCost(summonCost))
        {
            Destroy(instance);
            return false;
        }

        targetTile.SetOccupied(true);
        spawnedUnitIds.Add(unitData.UnitId);
        spawnedUnits[unitData.UnitId] = runtimeState;
        spawnedUnitTiles[unitData.UnitId] = targetTile;

        if (runtimeState.Health != null)
        {
            healthToUnitId[runtimeState.Health] = unitData.UnitId;
            healthToTileNode[runtimeState.Health] = targetTile;

            runtimeState.Health.OnDied -= HandleUnitDied;
            runtimeState.Health.OnDied += HandleUnitDied;
        }

        PublishFieldUnitCount();
        Debug.Log($"[DeckUnitReceiver] 유닛 소환 완료: {unitData.DisplayName} | 소모 DP: {summonCost} (남은 DP: {CurrencyManager.Instance.DpCost})");
        return true;
    }

    // 유닛 사망 이벤트 콜백
    private void HandleUnitDied(CombatHealth health)
    {
        if (health == null) return;

        health.OnDied -= HandleUnitDied;

        if (healthToTileNode.TryGetValue(health, out TileNode occupiedTile))
        {
            if (occupiedTile != null)
            {
                occupiedTile.SetOccupied(false);
            }
            healthToTileNode.Remove(health);
        }

        string deadUnitId = null;
        if (healthToUnitId.TryGetValue(health, out string unitId))
        {
            deadUnitId = unitId;
            spawnedUnitIds.Remove(unitId);
            spawnedUnits.Remove(unitId);
            spawnedUnitTiles.Remove(unitId);
            healthToUnitId.Remove(health);
        }

        UnitRuntimeState runtimeState = health.GetComponent<UnitRuntimeState>();
        if (runtimeState != null)
        {
            ReadyEffect.Hide(runtimeState);
        }

        if (health.gameObject != null)
        {
            Destroy(health.gameObject);
        }

        PublishFieldUnitCount();

        if (!string.IsNullOrEmpty(deadUnitId))
        {
            if (respawnCoroutines.TryGetValue(deadUnitId, out Coroutine existingRoutine) && existingRoutine != null)
            {
                StopCoroutine(existingRoutine);
            }
            respawnCoroutines[deadUnitId] = StartCoroutine(RespawnCooldownRoutine(deadUnitId));
        }
    }

    // 사망 유닛 개별 재소환 쿨타임 대기 코루틴
    private IEnumerator RespawnCooldownRoutine(string unitId)
    {
        coolingDownUnitIds.Add(unitId);
        yield return new WaitForSeconds(respawnCooldown);

        coolingDownUnitIds.Remove(unitId);
        respawnCoroutines.Remove(unitId);

        TrySpawnWaitingDeckUnits();
    }

    // 필드 유닛 일괄 정리
    public void ClearAllUnits()
    {
        if (initialSpawnRoutine != null)
        {
            StopCoroutine(initialSpawnRoutine);
            initialSpawnRoutine = null;
        }

        if (spawnSequenceRoutine != null)
        {
            StopCoroutine(spawnSequenceRoutine);
            spawnSequenceRoutine = null;
        }
        isSpawningSequence = false;

        foreach (var routine in respawnCoroutines.Values)
        {
            if (routine != null)
            {
                StopCoroutine(routine);
            }
        }
        respawnCoroutines.Clear();
        coolingDownUnitIds.Clear();

        foreach (var pair in spawnedUnits)
        {
            if (pair.Value != null)
            {
                if (pair.Value.Health != null)
                {
                    pair.Value.Health.OnDied -= HandleUnitDied;
                }

                ReadyEffect.Hide(pair.Value);

                if (pair.Value.gameObject != null)
                {
                    Destroy(pair.Value.gameObject);
                }
            }
        }

        foreach (var pair in spawnedUnitTiles)
        {
            if (pair.Value != null)
            {
                pair.Value.SetOccupied(false);
            }
        }

        spawnedUnitIds.Clear();
        spawnedUnits.Clear();
        spawnedUnitTiles.Clear();
        healthToUnitId.Clear();
        healthToTileNode.Clear();

        PublishFieldUnitCount();
    }
    
    // 맵 생성 완료 이벤트 콜백
    private void HandleMapGenerated()
    {
        ClearAllUnits();

        if (initialSpawnRoutine != null)
        {
            StopCoroutine(initialSpawnRoutine);
        }
        initialSpawnRoutine = StartCoroutine(DelayedInitialSpawnRoutine());
    }

    // 맵 생성 후 지연 대기 및 소환 시작 코루틴
    private IEnumerator DelayedInitialSpawnRoutine()
    {
        yield return new WaitForSeconds(initialSpawnDelay);
        SpawnCurrentDeck();
        initialSpawnRoutine = null;
    }
    
    // 현재 덱 유닛 목록 갱신 및 소환 시작
    private void SpawnCurrentDeck()
    {
        if (DeckManager.Instance == null) return;

        List<DeckSlotUnitEntry> currentUnits =
            DeckManager.Instance.GetActiveDeckSlotEntries(DeckType.Normal);

        UpdateDeckUnits(currentUnits);
        TrySpawnWaitingDeckUnits();
    }
    
    // 대기 유닛 순차 소환 코루틴 가동
    private void TrySpawnWaitingDeckUnits()
    {
        if (isSpawningSequence) return;

        if (spawnSequenceRoutine != null)
        {
            StopCoroutine(spawnSequenceRoutine);
        }
        spawnSequenceRoutine = StartCoroutine(SpawnSequenceRoutine());
    }

    // 0.5초 간격 유닛 순차 소환 코루틴
    private IEnumerator SpawnSequenceRoutine()
    {
        isSpawningSequence = true;

        for (int i = 0; i < deckUnits.Count; i++)
        {
            if (spawnedUnitIds.Count >= MaxFieldUnitCount)
            {
                break;
            }

            UnitDataSO unitData = deckUnits[i];

            if (unitData == null || string.IsNullOrEmpty(unitData.UnitId))
            {
                continue;
            }

            if (spawnedUnitIds.Contains(unitData.UnitId))
            {
                continue;
            }

            if (coolingDownUnitIds.Contains(unitData.UnitId))
            {
                continue;
            }

            bool spawned = SpawnDeckUnit(unitData);
            if (spawned)
            {
                yield return new WaitForSeconds(spawnInterval);
            }
        }

        isSpawningSequence = false;
        spawnSequenceRoutine = null;
    }

    // 필드 소환 유닛 수 변경 이벤트 발행
    private void PublishFieldUnitCount()
    {
        EventBus.Publish(new FieldUnitCountChangedEvent(spawnedUnitIds.Count, MaxFieldUnitCount));
    }
}