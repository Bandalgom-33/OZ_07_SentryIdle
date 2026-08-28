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
    
    private bool isCheckingWaitingUnits;

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
            SpawnCurrentDeck();
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

        // 실제 필드 유닛 제거 및 이벤트 구독 해제
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

        // 필드에 존재 중이라는 기록 제거 및 진행 중인 쿨타임 코루틴 정리
        spawnedUnitIds.Remove(unitId);
        if (respawnCoroutines.TryGetValue(unitId, out Coroutine routine) && routine != null)
        {
            StopCoroutine(routine);
        }
        respawnCoroutines.Remove(unitId);
        coolingDownUnitIds.Remove(unitId);

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
        // 1. 유효성 및 맵/프리팹 참조 검증
        if (unitData == null) return;
        if (unitData.UnitPrefab == null) return;
        if (mapGenerator == null) return;

        Debug.Log(
            $"[DeckUnitReceiver] 실제 소환: " +
            $"{unitData.DisplayName} / {unitData.UnitId}"
        );
        
        // 2. 필드 최대 생존 유닛 수(10기) 및 유닛 중복 소환 검사
        if (spawnedUnitIds.Count >= MaxFieldUnitCount) return;
        if (spawnedUnitIds.Contains(unitData.UnitId)) return;

        // 3. 유닛 배치 속성(Placement)에 따른 타일 타입 선정
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

        // 4. 배치 가능한 빈 타일 검색
        TileNode targetTile = mapGenerator.FindRandomDeployableTile(targetTileType);
        if (targetTile == null) return;
        
        if (CurrencyManager.Instance == null)
        {
            Debug.LogWarning("[DeckUnitReceiver] CurrencyManager.Instance가 없습니다.");
            return;
        }

        int summonCost = unitData.SummonCost;

        // 5. DP 잔여량 사전 검증 (HasDpCost)
        if (!CurrencyManager.Instance.HasDpCost(summonCost))
        {
            Debug.Log(
                $"[DeckUnitReceiver] DP 부족 - " +
                $"{unitData.DisplayName} 필요 DP: {summonCost}"
            );
            return;
        }
        
        // 6. 타일 위치 기반 월드 좌표 산출 및 고지대/지상 높이 오프셋 적용
        Vector3 worldPosition = mapGenerator.MapRenderer.GridToWorld(targetTile.GridPosition);
        if (targetTile.TileType == TileType.HighGround)
        {
            worldPosition.y += highGroundUnitHeight;
        }
        else
        {
            worldPosition.y += groundUnitHeight;
        }

        // 7. 유닛 프리팹 인스턴스화
        GameObject instance = Instantiate(
            unitData.UnitPrefab,
            worldPosition,
            unitData.UnitPrefab.transform.rotation
        );

        UnitRuntimeState runtimeState = instance.GetComponent<UnitRuntimeState>();

        // 컴포넌트 부재 시 안전하게 인스턴스만 파괴하고 DP 차감 없이 즉시 중단
        if (runtimeState == null)
        {
            Destroy(instance);
            return;
        }

        // 8. 런타임 상태 및 그리드 좌표 초기화
        runtimeState.InitializeRuntime();

        if (runtimeState.GridPosition != null)
        {
            runtimeState.GridPosition.Initialize(
                targetTile.GridPosition,
                GridFacingDirection.East,
                CombatTargetLayer.Ground
            );
        }

        // 9. 유닛 소환 및 초기화 완료 후 DP 후차감 (TrySpendDpCost)
        if (!CurrencyManager.Instance.TrySpendDpCost(summonCost))
        {
            // 만약 그 사이 DP가 차감되어 부족해진 예외 상황 발생 시 롤백 파괴
            Destroy(instance);
            return;
        }

        // 10. 타일 점유 활성화 및 컬렉션 등록
        targetTile.SetOccupied(true);
        spawnedUnitIds.Add(unitData.UnitId);
        spawnedUnits[unitData.UnitId] = runtimeState;
        spawnedUnitTiles[unitData.UnitId] = targetTile;

        // 11. 사망 이벤트 구독 등록
        if (runtimeState.Health != null)
        {
            healthToUnitId[runtimeState.Health] = unitData.UnitId;
            healthToTileNode[runtimeState.Health] = targetTile;

            runtimeState.Health.OnDied -= HandleUnitDied;
            runtimeState.Health.OnDied += HandleUnitDied;
        }

        // 12. 정상 소환 완료 및 소모된 DP 로그 출력
        Debug.Log($"[DeckUnitReceiver] 유닛 소환 완료: {unitData.DisplayName} | 소모 DP: {summonCost} (남은 DP: {CurrencyManager.Instance.DpCost})");
    }

    // 아군 유닛 사망 시 디스폰 및 개별 재소환 쿨타임 시작 처리
    private void HandleUnitDied(CombatHealth health)
    {
        if (health == null) return;

        health.OnDied -= HandleUnitDied;

        // 1. 점유 타일 해제
        if (healthToTileNode.TryGetValue(health, out TileNode occupiedTile))
        {
            if (occupiedTile != null)
            {
                occupiedTile.SetOccupied(false);
            }
            healthToTileNode.Remove(health);
        }

        // 2. 소환 컬렉션 및 매핑 정보 정리
        string deadUnitId = null;
        if (healthToUnitId.TryGetValue(health, out string unitId))
        {
            deadUnitId = unitId;
            spawnedUnitIds.Remove(unitId);
            spawnedUnits.Remove(unitId);
            spawnedUnitTiles.Remove(unitId);
            healthToUnitId.Remove(health);
        }

        // 3. 이펙트 회수 및 게임 오브젝트 파괴
        UnitRuntimeState runtimeState = health.GetComponent<UnitRuntimeState>();
        if (runtimeState != null)
        {
            ReadyEffect.Hide(runtimeState);
        }

        if (health.gameObject != null)
        {
            Destroy(health.gameObject);
        }

        // 4. 사망 유닛 개별 재소환 쿨타임 타이머 코루틴 가동
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

        // 쿨타임 종료 후 덱 우선순위에 맞게 소환 시도
        TrySpawnWaitingDeckUnits();
    }

    // 필드의 모든 아군 유닛 일괄 정리 및 점유 해제 (패배 또는 맵 초기화용)
    public void ClearAllUnits()
    {
        // 1. 진행 중인 모든 재소환 쿨타임 코루틴 중단
        foreach (var routine in respawnCoroutines.Values)
        {
            if (routine != null)
            {
                StopCoroutine(routine);
            }
        }
        respawnCoroutines.Clear();
        coolingDownUnitIds.Clear();

        // 2. 생존 유닛 이벤트 해제 및 오브젝트 파괴
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

        // 3. 점유 타일 해제
        foreach (var pair in spawnedUnitTiles)
        {
            if (pair.Value != null)
            {
                pair.Value.SetOccupied(false);
            }
        }

        // 4. 컬렉션 일괄 초기화
        spawnedUnitIds.Clear();
        spawnedUnits.Clear();
        spawnedUnitTiles.Clear();
        healthToUnitId.Clear();
        healthToTileNode.Clear();
    }
    
    // 맵 생성 및 재생성 완료 시 새 맵 타일 구조에 맞추어 아군 유닛 재배치
    private void HandleMapGenerated()
    {
        ClearAllUnits();
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

            // 쿨타임 진행 중인 유닛은 대기
            if (coolingDownUnitIds.Contains(unitData.UnitId))
                continue;

            SpawnDeckUnit(unitData);
        }

        isCheckingWaitingUnits = false;
    }
}