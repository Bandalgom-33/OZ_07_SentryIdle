using System;
using System.Collections;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Runtime;
using UnityEngine;
using Random = UnityEngine.Random;

namespace EndlessGuard.TestBattle
{
    // 플레이어 덱 및 보유 DP 연동 기반 아군 유닛 자동 소환 및 격자 배치 매니저
    public class TestUnitSummonManager : MonoBehaviour
    {
        #region 인스펙터 직렬화 필드

        [Header("--- 맵 생성기 참조 ---")]
        [Tooltip("배치 가능한 격자 타일 정보를 제공받을 TestMapGenerator")]
        [SerializeField] private TestMapGenerator mapGenerator;

        [Header("--- 유닛 카탈로그 (예비용) ---")]
        [Tooltip("DeckManager에 유닛이 미편성되어 있을 때 사용할 기본 유닛 카탈로그")]
        [SerializeField] private UnitCatalog unitCatalog;

        [Header("--- 필드 유닛 제한 및 초기 배치 설정 ---")]
        [Tooltip("필드에 동시에 생존할 수 있는 최대 아군 유닛 수 (기본 10기)")]
        [SerializeField] private int maxFieldUnitCount = 10;

        [Tooltip("맵 생성 직후 자동으로 배치할 초기 아군 유닛 수")]
        [SerializeField] private int initialSpawnCount = 4;

        [Tooltip("초기 배치 시 DP 코스트 소모를 무시할지 여부")]
        [SerializeField] private bool ignoreDpCostForInitialSpawn = true;

        [Header("--- 자동 지속 소환 설정 ---")]
        [Tooltip("DP 잔액과 빈 타일을 주기적으로 검사하여 자동으로 추가 소환할지 여부")]
        [SerializeField] private bool autoSpawnEnabled = true;

        [Tooltip("자동 소환 검사 주기 (초)")]
        [SerializeField] private float autoSpawnInterval = 1.0f;

        [Header("--- 타일별 Y축 높이 오프셋 ---")]
        [Tooltip("Ground/Path 타일 배치 시 적용할 높이 오프셋")]
        [SerializeField] private float groundUnitHeight = 0f;

        [Tooltip("HighGround 타일 배치 시 적용할 높이 오프셋")]
        [SerializeField] private float highGroundUnitHeight = 0.25f;

        #endregion

        #region 내부 런타임 캐시 필드

        private readonly Dictionary<CombatHealth, TileNode> _occupiedTilesByUnit = new Dictionary<CombatHealth, TileNode>();
        private readonly HashSet<string> _activeUnitIds = new HashSet<string>();
        private Coroutine _autoSpawnCoroutine;

        #endregion

        #region 프로퍼티

        public int CurrentFieldUnitCount => _occupiedTilesByUnit.Count;

        #endregion

        #region 라이프사이클

        // 참조 캐싱 및 예비 카탈로그 로드
        private void Awake()
        {
            if (mapGenerator == null)
            {
                mapGenerator = FindFirstObjectByType<TestMapGenerator>();
            }

            if (unitCatalog == null)
            {
                unitCatalog = Resources.Load<UnitCatalog>("Catalogs/UnitCatalog");
            }
        }

        // 전역 이벤트 리스너 등록
        private void OnEnable()
        {
            EventBus.Subscribe<NormalDeckChangedEvent>(OnNormalDeckChanged);
        }

        // 이벤트 구독 해제 및 아군 유닛 정리
        private void OnDisable()
        {
            EventBus.Unsubscribe<NormalDeckChangedEvent>(OnNormalDeckChanged);
            StopAutoSpawn();
            ClearAllUnits();
        }

        #endregion

        #region 소환 초기화 및 루프 제어

        // 초기 아군 배치 및 자동 소환 루프 시작
        public void InitializeSummoning()
        {
            ClearAllUnits();
            SpawnInitialUnits();

            if (autoSpawnEnabled)
            {
                StartAutoSpawn();
            }
        }

        // 초기 지정 수량 아군 유닛 일괄 배치
        private void SpawnInitialUnits()
        {
            for (int i = 0; i < initialSpawnCount; i++)
            {
                if (CurrentFieldUnitCount >= maxFieldUnitCount)
                {
                    break;
                }

                TrySpawnNextDeckUnit(ignoreDpCostForInitialSpawn);
            }
        }

        // 자동 지속 소환 코루틴 가동
        public void StartAutoSpawn()
        {
            if (_autoSpawnCoroutine != null)
            {
                StopCoroutine(_autoSpawnCoroutine);
            }
            _autoSpawnCoroutine = StartCoroutine(AutoSpawnRoutine());
        }

        // 자동 지속 소환 코루틴 중단
        public void StopAutoSpawn()
        {
            if (_autoSpawnCoroutine != null)
            {
                StopCoroutine(_autoSpawnCoroutine);
                _autoSpawnCoroutine = null;
            }
        }

        // 주기적 DP 확인 및 자동 추가 소환 코루틴
        private IEnumerator AutoSpawnRoutine()
        {
            while (autoSpawnEnabled)
            {
                yield return new WaitForSeconds(autoSpawnInterval);

                if (mapGenerator != null && mapGenerator.IsMapGenerated && CurrentFieldUnitCount < maxFieldUnitCount)
                {
                    TrySpawnNextDeckUnit(ignoreDpCost: false);
                }
            }
        }

        #endregion

        #region 아군 유닛 소환 핵심 로직

        // 덱 또는 카탈로그 기반 아군 유닛 선별 및 소환 시도
        public bool TrySpawnNextDeckUnit(bool ignoreDpCost = false)
        {
            if (CurrentFieldUnitCount >= maxFieldUnitCount || mapGenerator == null || !mapGenerator.IsMapGenerated)
            {
                return false;
            }

            UnitDataSO targetUnitData = PickDeployableUnitData();
            if (targetUnitData == null || targetUnitData.UnitPrefab == null)
            {
                return false;
            }

            TileType targetTileType = DetermineTargetTileType(targetUnitData.Placement);

            TileNode candidateTile = mapGenerator.FindRandomDeployableTile(targetTileType);
            if (candidateTile == null)
            {
                if (targetUnitData.Placement == UnitPlacement.GroundAndHighGround)
                {
                    TileType altType = targetTileType == TileType.Path ? TileType.HighGround : TileType.Path;
                    candidateTile = mapGenerator.FindRandomDeployableTile(altType);
                }
            }

            if (candidateTile == null)
            {
                return false;
            }

            int summonCost = Mathf.Max(0, targetUnitData.SummonCost);
            if (!ignoreDpCost)
            {
                if (CurrencyManager.Instance != null)
                {
                    if (!CurrencyManager.Instance.HasDpCost(summonCost) || !CurrencyManager.Instance.TrySpendDpCost(summonCost))
                    {
                        return false;
                    }
                }
            }

            return SpawnUnitOnTile(targetUnitData, candidateTile);
        }

        // 지정 타일 기반 유닛 인스턴스화 및 컴포넌트 초기화
        private bool SpawnUnitOnTile(UnitDataSO unitData, TileNode tileNode)
        {
            if (unitData == null || unitData.UnitPrefab == null || mapGenerator == null || mapGenerator.MapRenderer == null)
            {
                return false;
            }

            Vector3 worldPos = mapGenerator.MapRenderer.GridToWorld(tileNode.GridPosition);
            worldPos.y += (tileNode.TileType == TileType.HighGround) ? highGroundUnitHeight : groundUnitHeight;

            GameObject instance = Instantiate(unitData.UnitPrefab, worldPos, unitData.UnitPrefab.transform.rotation);
            instance.name = $"Ally_{unitData.DisplayName}_{tileNode.GridPosition.x}_{tileNode.GridPosition.y}";

            UnitDataLink dataLink = instance.GetComponent<UnitDataLink>();

            UnitRuntimeState runtimeState = instance.GetComponent<UnitRuntimeState>();
            if (runtimeState == null)
            {
                Debug.LogError($"[TestUnitSummonManager] {instance.name}에 UnitRuntimeState 컴포넌트가 없습니다.", instance);
                Destroy(instance);
                return false;
            }

            runtimeState.InitializeRuntime();

            GridFacingDirection facing = CalculateOptimalFacingDirection(tileNode.GridPosition);

            if (runtimeState.GridPosition != null)
            {
                runtimeState.GridPosition.Initialize(tileNode.GridPosition, facing, CombatTargetLayer.Ground);
            }

            tileNode.SetOccupied(true);

            _activeUnitIds.Add(unitData.UnitId);
            EventBus.Publish(new UnitFieldSpawnStateChangedEvent(unitData.UnitId, true));

            RegisterUnitDeath(runtimeState, tileNode);

            Debug.Log($"[TestUnitSummonManager] 아군 소환 완료: {unitData.DisplayName} -> 타일 {tileNode.GridPosition} (방향: {facing})");
            return true;
        }

        #endregion

        #region 방향 산출 및 데이터 선별 헬퍼

        // 미소환 유닛 중 배치 대상 선별
        private UnitDataSO PickDeployableUnitData()
        {
            if (DeckManager.Instance != null)
            {
                List<UnitDataSO> deckUnits = DeckManager.Instance.GetRegisteredUnitData(DeckType.Normal);
                if (deckUnits != null && deckUnits.Count > 0)
                {
                    List<UnitDataSO> availableUnits = new List<UnitDataSO>();
                    for (int i = 0; i < deckUnits.Count; i++)
                    {
                        if (deckUnits[i] != null && !_activeUnitIds.Contains(deckUnits[i].UnitId))
                        {
                            availableUnits.Add(deckUnits[i]);
                        }
                    }

                    if (availableUnits.Count > 0)
                    {
                        return availableUnits[Random.Range(0, availableUnits.Count)];
                    }

                    return null;
                }
            }

            if (unitCatalog != null && unitCatalog.Units.Count > 0)
            {
                List<UnitDataSO> availableUnits = new List<UnitDataSO>();
                for (int i = 0; i < unitCatalog.Units.Count; i++)
                {
                    if (unitCatalog.Units[i] != null && !_activeUnitIds.Contains(unitCatalog.Units[i].UnitId))
                    {
                        availableUnits.Add(unitCatalog.Units[i]);
                    }
                }

                if (availableUnits.Count > 0)
                {
                    return availableUnits[Random.Range(0, availableUnits.Count)];
                }
            }

            return null;
        }

        // 유닛 배치 적성에 따른 타일 타입 결정
        private TileType DetermineTargetTileType(UnitPlacement placement)
        {
            switch (placement)
            {
                case UnitPlacement.HighGround:
                    return TileType.HighGround;
                case UnitPlacement.Ground:
                    return TileType.Path;
                case UnitPlacement.GroundAndHighGround:
                    return Random.value > 0.5f ? TileType.Path : TileType.HighGround;
                default:
                    return TileType.Path;
            }
        }

        // 소환 타일 기준 최적 시선 방향 산출
        private GridFacingDirection CalculateOptimalFacingDirection(Vector2Int tileCoord)
        {
            if (mapGenerator == null) return GridFacingDirection.West;

            IReadOnlyList<Vector2Int> path = mapGenerator.PathPositionA;
            if (path != null && path.Count > 0)
            {
                int index = -1;
                for (int i = 0; i < path.Count; i++)
                {
                    if (path[i] == tileCoord)
                    {
                        index = i;
                        break;
                    }
                }

                if (index > 0)
                {
                    Vector2Int deltaToPrev = path[index - 1] - tileCoord;
                    return TestMapGenerator.CalculateFacingDirection(deltaToPrev);
                }
            }

            return GridFacingDirection.West;
        }

        #endregion

        #region 사망 처리 및 정리 로직

        // 유닛 사망 이벤트 리스너 바인딩
        private void RegisterUnitDeath(UnitRuntimeState unitState, TileNode tileNode)
        {
            if (unitState == null || unitState.Health == null) return;

            CombatHealth health = unitState.Health;
            _occupiedTilesByUnit[health] = tileNode;

            health.OnDied -= HandleUnitDied;
            health.OnDied += HandleUnitDied;
        }

        // 유닛 사망 이벤트 콜백
        private void HandleUnitDied(CombatHealth health)
        {
            if (health == null) return;

            if (_occupiedTilesByUnit.TryGetValue(health, out TileNode tile))
            {
                if (tile != null)
                {
                    tile.SetOccupied(false);
                }
                _occupiedTilesByUnit.Remove(health);
            }

            health.OnDied -= HandleUnitDied;

            UnitRuntimeState unitState = health.GetComponent<UnitRuntimeState>();
            if (unitState != null)
            {
                ReadyEffect.Hide(unitState);

                string unitId = unitState.UnitId;
                if (!string.IsNullOrEmpty(unitId))
                {
                    _activeUnitIds.Remove(unitId);
                    EventBus.Publish(new UnitFieldSpawnStateChangedEvent(unitId, false));
                }
            }

            if (health.gameObject != null)
            {
                Destroy(health.gameObject);
            }
        }

        // 필드 상의 모든 아군 유닛 일괄 정리
        public void ClearAllUnits()
        {
            foreach (KeyValuePair<CombatHealth, TileNode> pair in _occupiedTilesByUnit)
            {
                if (pair.Key != null)
                {
                    pair.Key.OnDied -= HandleUnitDied;

                    if (pair.Value != null)
                    {
                        pair.Value.SetOccupied(false);
                    }

                    UnitRuntimeState unitState = pair.Key.GetComponent<UnitRuntimeState>();
                    if (unitState != null)
                    {
                        ReadyEffect.Hide(unitState);
                    }

                    if (pair.Key.gameObject != null)
                    {
                        Destroy(pair.Key.gameObject);
                    }
                }
            }

            _occupiedTilesByUnit.Clear();

            foreach (string unitId in _activeUnitIds)
            {
                EventBus.Publish(new UnitFieldSpawnStateChangedEvent(unitId, false));
            }
            _activeUnitIds.Clear();
        }

        // 일반 덱 변경 이벤트 콜백
        private void OnNormalDeckChanged(NormalDeckChangedEvent evt)
        {
            Debug.Log($"[TestUnitSummonManager] 일반 덱 변경 감지 (장착 유닛 수: {evt.activeUnits.Count})");
        }

        #endregion
    }
}
