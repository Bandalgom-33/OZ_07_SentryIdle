using System;
using System.Collections;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Runtime;
using UnityEngine;
using Random = UnityEngine.Random;

namespace EndlessGuard.TestBattle
{
    /// <summary>
    /// 플레이어의 덱(DeckManager) 및 보유 DP(CurrencyManager)와 연동하여,
    /// 격자 타일에 아군 유닛을 자동으로 소환 및 배치하고 사망 시 정리하는 소환 매니저 클래스
    /// </summary>
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

        // 현재 소환되어 필드에 배치된 아군 유닛 및 타일 추적 매핑 (사망 시 점유 해제용)
        private readonly Dictionary<CombatHealth, TileNode> _occupiedTilesByUnit = new Dictionary<CombatHealth, TileNode>();

        // 현재 필드에 생존 중인 유닛 식별자 목록 (중복 소환 방지 및 HUD 명암 연동용)
        private readonly HashSet<string> _activeUnitIds = new HashSet<string>();

        // 자동 소환 코루틴 참조
        private Coroutine _autoSpawnCoroutine;

        #endregion

        #region 프로퍼티

        // 현재 필드에 배치된 아군 유닛 수
        public int CurrentFieldUnitCount => _occupiedTilesByUnit.Count;

        #endregion

        #region 라이프사이클

        private void Awake()
        {
            // 인스펙터 미연결 시 씬 내 TestMapGenerator 컴포넌트 자동 탐색
            if (mapGenerator == null)
            {
                mapGenerator = FindFirstObjectByType<TestMapGenerator>();
            }

            if (unitCatalog == null)
            {
                unitCatalog = Resources.Load<UnitCatalog>("Catalogs/UnitCatalog");
            }
        }

        private void OnEnable()
        {
            // EventBus의 덱 편성 변경 이벤트 구독 (덱 변경 시 필요 시 즉시 반응)
            EventBus.Subscribe<NormalDeckChangedEvent>(OnNormalDeckChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<NormalDeckChangedEvent>(OnNormalDeckChanged);
            StopAutoSpawn();
            ClearAllUnits();
        }

        #endregion

        #region 소환 초기화 및 루프 제어

        /// <summary>
        /// 맵 생성 완료 후 초기 유닛 배치 및 자동 소환 코루틴을 시작합니다.
        /// </summary>
        public void InitializeSummoning()
        {
            // 1. 기존 잔존 유닛 정리
            ClearAllUnits();

            // 2. 초기 아군 로스터 소환
            SpawnInitialUnits();

            // 3. 자동 소환 루프 가동
            if (autoSpawnEnabled)
            {
                StartAutoSpawn();
            }
        }

        // 초기 유닛 스폰 루프
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

        /// <summary>
        /// 자동 지속 소환 코루틴 가동
        /// </summary>
        public void StartAutoSpawn()
        {
            if (_autoSpawnCoroutine != null)
            {
                StopCoroutine(_autoSpawnCoroutine);
            }
            _autoSpawnCoroutine = StartCoroutine(AutoSpawnRoutine());
        }

        /// <summary>
        /// 자동 소환 코루틴 중단
        /// </summary>
        public void StopAutoSpawn()
        {
            if (_autoSpawnCoroutine != null)
            {
                StopCoroutine(_autoSpawnCoroutine);
                _autoSpawnCoroutine = null;
            }
        }

        // 주기적으로 DP 잔액과 빈 타일을 검사하여 아군을 소환하는 루틴
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

        /// <summary>
        /// DeckManager의 편성 덱(또는 UnitCatalog)에서 유닛을 선정하고,
        /// DP 코스트 및 빈 타일을 검증하여 필드에 안전하게 소환합니다.
        /// </summary>
        public bool TrySpawnNextDeckUnit(bool ignoreDpCost = false)
        {
            // 1. 필드 최대 유닛 수(10기) 한도 검사
            if (CurrentFieldUnitCount >= maxFieldUnitCount || mapGenerator == null || !mapGenerator.IsMapGenerated)
            {
                return false;
            }

            // 2. 소환할 UnitDataSO 후보 선별 (DeckManager 일반 덱 우선 -> 미편성 시 UnitCatalog 폴백)
            UnitDataSO targetUnitData = PickDeployableUnitData();
            if (targetUnitData == null || targetUnitData.UnitPrefab == null)
            {
                return false;
            }

            // 3. 유닛의 배치 적성(Placement)에 따른 타일 타입 선정
            TileType targetTileType = DetermineTargetTileType(targetUnitData.Placement);

            // 4. 배치 가능한 빈 타일 검색
            TileNode candidateTile = mapGenerator.FindRandomDeployableTile(targetTileType);
            if (candidateTile == null)
            {
                // 주 타일이 꽉 찼고 복합 배치가 가능한 경우 보조 타일 타입 재검색
                if (targetUnitData.Placement == UnitPlacement.GroundAndHighGround)
                {
                    TileType altType = targetTileType == TileType.Path ? TileType.HighGround : TileType.Path;
                    candidateTile = mapGenerator.FindRandomDeployableTile(altType);
                }
            }

            if (candidateTile == null)
            {
                return false; // 빈 배치 타일 없음
            }

            // 5. CurrencyManager 보유 DP 검증 및 차감
            int summonCost = Mathf.Max(0, targetUnitData.SummonCost);
            if (!ignoreDpCost)
            {
                if (CurrencyManager.Instance != null)
                {
                    if (!CurrencyManager.Instance.HasDpCost(summonCost) || !CurrencyManager.Instance.TrySpendDpCost(summonCost))
                    {
                        return false; // DP 부족
                    }
                }
            }

            // 6. 유닛 인스턴스화 및 런타임 초기화 실행
            return SpawnUnitOnTile(targetUnitData, candidateTile);
        }

        /// <summary>
        /// 지정된 타일에 유닛 프리팹을 생성하고 전투 컴포넌트 및 사망 이벤트를 바인딩합니다.
        /// </summary>
        private bool SpawnUnitOnTile(UnitDataSO unitData, TileNode tileNode)
        {
            if (unitData == null || unitData.UnitPrefab == null || mapGenerator == null || mapGenerator.MapRenderer == null)
            {
                return false;
            }

            // 1. 월드 좌표 및 타일 높이 오프셋 계산
            Vector3 worldPos = mapGenerator.MapRenderer.GridToWorld(tileNode.GridPosition);
            worldPos.y += (tileNode.TileType == TileType.HighGround) ? highGroundUnitHeight : groundUnitHeight;

            // 2. 프리팹 인스턴스 생성
            GameObject instance = Instantiate(unitData.UnitPrefab, worldPos, unitData.UnitPrefab.transform.rotation);
            instance.name = $"Ally_{unitData.DisplayName}_{tileNode.GridPosition.x}_{tileNode.GridPosition.y}";

            // 3. UnitDataLink 확인 (프리팹에 기본 연결되어 있음)
            UnitDataLink dataLink = instance.GetComponent<UnitDataLink>();

            // 4. UnitRuntimeState 초기화
            UnitRuntimeState runtimeState = instance.GetComponent<UnitRuntimeState>();
            if (runtimeState == null)
            {
                Debug.LogError($"[TestUnitSummonManager] {instance.name}에 UnitRuntimeState 컴포넌트가 없습니다.", instance);
                Destroy(instance);
                return false;
            }

            runtimeState.InitializeRuntime();

            // 5. 바라보는 방향(GridFacingDirection) 자동 계산 (적이 다가오는 경로를 마주보도록 설정)
            GridFacingDirection facing = CalculateOptimalFacingDirection(tileNode.GridPosition);

            // 6. CombatGridPosition 초기화 (이 시점에 CombatRegistry에 좌표 등록됨)
            if (runtimeState.GridPosition != null)
            {
                runtimeState.GridPosition.Initialize(tileNode.GridPosition, facing, CombatTargetLayer.Ground);
            }

            // 7. 타일 점유(Occupied) 상태 확정
            tileNode.SetOccupied(true);

            // 8. 중복 소환 방지 목록 등록 및 HUD 덱 슬롯 명암 변경 이벤트 발행
            _activeUnitIds.Add(unitData.UnitId);
            EventBus.Publish(new UnitFieldSpawnStateChangedEvent(unitData.UnitId, true));

            // 9. 사망 시 타일 해제 및 디스폰을 위한 이벤트 바인딩
            RegisterUnitDeath(runtimeState, tileNode);

            Debug.Log($"[TestUnitSummonManager] 아군 소환 완료: {unitData.DisplayName} -> 타일 {tileNode.GridPosition} (방향: {facing})");
            return true;
        }

        #endregion

        #region 방향 산출 및 데이터 선별 헬퍼

        // DeckManager에서 아직 필드에 소환되지 않은 유닛을 우선 선별 (중복 소환 방지)
        private UnitDataSO PickDeployableUnitData()
        {
            if (DeckManager.Instance != null)
            {
                List<UnitDataSO> deckUnits = DeckManager.Instance.GetRegisteredUnitData(DeckType.Normal);
                if (deckUnits != null && deckUnits.Count > 0)
                {
                    // 필드에 이미 생존 중인 유닛(_activeUnitIds)은 제외하고 후보 필터링
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

                    // 덱의 모든 유닛이 이미 필드에 소환된 경우 추가 소환 불가
                    return null;
                }
            }

            // 예비 카탈로그 폴백 (덱이 비어있는 테스트 환경)
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

        // 유닛 배치 적성에 따른 최적 타일 타입 결정
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

        // 소환 타일 주변의 적 이동 경로를 분석하여 적을 정면으로 마주보도록 방향 계산
        private GridFacingDirection CalculateOptimalFacingDirection(Vector2Int tileCoord)
        {
            if (mapGenerator == null) return GridFacingDirection.West;

            // 스폰 지점(X=0)에서 골(X=width-1)로 이동하므로 기본적으로 왼쪽(West, 적이 오는 방향)을 바라봄
            // 타일이 경로상에 있는 경우 경로의 역방향(이전 노드 방향)을 계산
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

                // 경로 타일인 경우: 적이 이전 노드에서 오므로 이전 노드(index-1)를 바라봄
                if (index > 0)
                {
                    Vector2Int deltaToPrev = path[index - 1] - tileCoord;
                    return TestMapGenerator.CalculateFacingDirection(deltaToPrev);
                }
            }

            // 기본 방향은 서쪽(West: 입구 방향)
            return GridFacingDirection.West;
        }

        #endregion

        #region 사망 처리 및 정리 로직

        // 유닛 사망 이벤트 바인딩
        private void RegisterUnitDeath(UnitRuntimeState unitState, TileNode tileNode)
        {
            if (unitState == null || unitState.Health == null) return;

            CombatHealth health = unitState.Health;
            _occupiedTilesByUnit[health] = tileNode;

            health.OnDied -= HandleUnitDied;
            health.OnDied += HandleUnitDied;
        }

        // 아군 유닛 사망 콜백: 점유 타일 즉시 해제, 이펙트 회수 및 게임오브젝트 파괴
        private void HandleUnitDied(CombatHealth health)
        {
            if (health == null) return;

            // 1. 점유 타일 해제
            if (_occupiedTilesByUnit.TryGetValue(health, out TileNode tile))
            {
                if (tile != null)
                {
                    tile.SetOccupied(false);
                }
                _occupiedTilesByUnit.Remove(health);
            }

            // 2. 핸들러 해제
            health.OnDied -= HandleUnitDied;

            // 3. ReadyEffect 이펙트 풀링 회수 및 소환 해제 이벤트 발행
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

            // 4. 오브젝트 제거
            if (health.gameObject != null)
            {
                Destroy(health.gameObject);
            }
        }

        /// <summary>
        /// 필드의 모든 아군 유닛을 파괴하고 타일 점유 상태를 일괄 초기화합니다.
        /// </summary>
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

            // 필드 생존 유닛 목록 일괄 초기화 및 HUD 비활성화 해제 알림
            foreach (string unitId in _activeUnitIds)
            {
                EventBus.Publish(new UnitFieldSpawnStateChangedEvent(unitId, false));
            }
            _activeUnitIds.Clear();
        }

        private void OnNormalDeckChanged(NormalDeckChangedEvent evt)
        {
            Debug.Log($"[TestUnitSummonManager] 일반 덱 변경 감지 (장착 유닛 수: {evt.activeUnits.Count})");
        }

        #endregion
    }
}
