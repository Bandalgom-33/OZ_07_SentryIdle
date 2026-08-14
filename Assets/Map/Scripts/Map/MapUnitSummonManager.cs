using System.Collections;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Runtime;
using UnityEngine;

namespace EndlessGuard.Map
{
    // 맵 내 아군 유닛의 지속적인 소환(Spawn), 사망 시 타일 해제 및 디스폰(Despawn),
    // 필드 유닛 최대 제한(10기), 그리고 CurrencyManager와의 DP 코스트 연동 전담 관리 컨트롤러 클래스
    public class MapUnitSummonManager : MonoBehaviour
    {
        #region 에디터 설정 필드

        [Header("맵 생성기 연동")]
        [Tooltip("맵 격자 정보 및 타일 배치를 제공받을 MapGenerator 컴포넌트")]
        [SerializeField] private MapGenerator mapGenerator;

        [Header("소환 유닛 프리팹")]
        [Tooltip("근거리(Ground) 아군 유닛 Prefab (내부 UnitDataLink/UnitDataSO 연결 필수)")]
        [SerializeField] private GameObject meleeUnitPrefab;

        [Tooltip("원거리(HighGround) 아군 유닛 Prefab (내부 UnitDataLink/UnitDataSO 연결 필수)")]
        [SerializeField] private GameObject rangedUnitPrefab;

        [Header("유닛 오프셋 설정")]
        [Tooltip("Ground 타일 배치 시 적용할 추가 Y축 높이 오프셋")]
        [SerializeField] private float meleeUnitHeight = 0f;

        [Tooltip("HighGround 타일 배치 시 언덕 높이에 맞춰 적용할 추가 Y축 높이 오프셋")]
        [SerializeField] private float rangedUnitHeight = 0.25f;

        [Header("필드 유닛 수 제한 및 초기 소환 설정")]
        [Tooltip("필드에 동시에 살아있을 수 있는 최대 아군 유닛 수 제한")]
        [SerializeField] private int maxFieldUnitCount = 10;

        [Tooltip("맵 생성 완료 직후 자동으로 배치할 초기 아군 유닛 수")]
        [SerializeField] private int initialSpawnCount = 4;

        [Tooltip("초기 배치 시 DP 코스트 소모 무시 및 즉시 배치 여부")]
        [SerializeField] private bool ignoreDpCostForInitialSpawn = true;

        [Header("DP 코스트 및 소환 설정")]
        [Tooltip("유닛 데이터(UnitDataSO)의 SummonCost를 읽을 수 없을 때 사용할 기본 예비 DP 코스트")]
        [SerializeField] private int defaultUnitSpawnCost = 10;

        [Tooltip("자동 지속 소환 루프의 활성화 여부")]
        [SerializeField] private bool autoSpawnEnabled = true;

        [Tooltip("DP 잔액 및 배치 타일을 검사하는 주기(초)")]
        [SerializeField] private float autoSpawnCheckInterval = 1.0f;

        #endregion

        #region 비공개 필드 및 추적 데이터

        // 유닛 CombatHealth 컴포넌트와 점유 타일(TileNode) 매핑 딕셔너리
        private readonly Dictionary<CombatHealth, TileNode> _occupiedTilesByUnit = new Dictionary<CombatHealth, TileNode>();

        // 코루틴 중복 실행 방지용 참조 변수
        private Coroutine _autoSpawnCoroutine;

        // 현재 필드 생존 아군 유닛 수 프로퍼티
        public int CurrentFieldUnitCount => _occupiedTilesByUnit.Count;

        #endregion

        #region 라이프사이클

        private void Awake()
        {
            // Inspector 미연결 시 씬 내 MapGenerator 컴포넌트 자동 탐색 및 검증 처리
            if (mapGenerator == null)
            {
                mapGenerator = FindFirstObjectByType<MapGenerator>();
                if (mapGenerator == null)
                {
                    Debug.LogError("[MapUnitSummonManager] 씬에서 MapGenerator를 찾을 수 없습니다. 참조를 할당하세요.", this);
                }
            }
        }

        private void Start()
        {
            if (mapGenerator == null) return;

            // 맵 생성 완료 여부 검사 후 초기 배치 및 지속 소환 루프 가동 처리
            if (mapGenerator.IsMapGenerated)
            {
                OnMapGenerationCompleted();
            }
            else
            {
                // 맵 생성 진행 중일 때 생성 완료 이벤트 구독 처리
                mapGenerator.OnMapGenerated += OnMapGenerationCompleted;
            }
        }

        private void OnDestroy()
        {
            if (mapGenerator != null)
            {
                mapGenerator.OnMapGenerated -= OnMapGenerationCompleted;
            }

            // 객체 파괴 시 등록된 이벤트 구독 일괄 정리 및 메모리 누수 방지 처리
            ClearAllUnitSubscriptions();
        }

        #endregion

        #region 초기화 및 배치 제어

        // MapGenerator의 맵 격자 생성 완료 콜백 및 초기 배치 수행 기능
        private void OnMapGenerationCompleted()
        {
            if (mapGenerator != null)
            {
                mapGenerator.OnMapGenerated -= OnMapGenerationCompleted;
            }

            // 1. 초기 아군 유닛 스폰 및 사망 이벤트 바인딩 일원화 수행
            SpawnInitialUnits();

            // 2. 자동 지속 소환 코루틴 루프 실행 처리
            if (autoSpawnEnabled)
            {
                StartAutoSpawnLoop();
            }
        }

        // 게임 시작 시 초기 배치 아군 유닛 생성 기능
        private void SpawnInitialUnits()
        {
            for (int i = 0; i < initialSpawnCount; i++)
            {
                // 필드 10기 유닛 수 제한 검사
                if (CurrentFieldUnitCount >= maxFieldUnitCount)
                {
                    break;
                }

                TrySpawnUnit(ignoreDpCostForInitialSpawn);
            }
        }

        #endregion

        #region 자동 소환 루프 및 DP 제어 로직

        // 외부/내부 요청에 따른 자동 소환 루프 코루틴 시작 처리
        public void StartAutoSpawnLoop()
        {
            if (_autoSpawnCoroutine != null)
            {
                StopCoroutine(_autoSpawnCoroutine);
            }
            _autoSpawnCoroutine = StartCoroutine(AutoSpawnLoop());
        }

        // 외부/내부 요청에 따른 자동 소환 루프 코루틴 정지 처리
        public void StopAutoSpawnLoop()
        {
            if (_autoSpawnCoroutine != null)
            {
                StopCoroutine(_autoSpawnCoroutine);
                _autoSpawnCoroutine = null;
            }
        }

        // 설정 주기별 DP 코스트 및 맵 배치 타일 검사를 통한 아군 유닛 지속 소환 코루틴
        private IEnumerator AutoSpawnLoop()
        {
            while (autoSpawnEnabled)
            {
                yield return new WaitForSeconds(autoSpawnCheckInterval);

                // 맵 생성 완료 및 타일 격자 유효성 확인 후 소환 시도
                if (mapGenerator != null && mapGenerator.Grid != null)
                {
                    TrySpawnUnit(false);
                }
            }
        }

        // 유닛 프리팹의 UnitDataLink/UnitDataSO 기준 개별 소환 코스트(SummonCost) 동적 추출 기능
        public int GetUnitSummonCost(GameObject unitPrefab)
        {
            if (unitPrefab == null) return defaultUnitSpawnCost;

            // 1. UnitDataLink 컴포넌트 직접 조회를 통한 UnitDataSO 참조
            UnitDataLink dataLink = unitPrefab.GetComponent<UnitDataLink>();
            if (dataLink != null && dataLink.HasData && dataLink.UnitData != null)
            {
                return dataLink.UnitData.SummonCost;
            }

            // 2. UnitRuntimeState를 통한 DataLink 보조 참조
            UnitRuntimeState runtimeState = unitPrefab.GetComponent<UnitRuntimeState>();
            if (runtimeState != null && runtimeState.DataLink != null && runtimeState.DataLink.HasData && runtimeState.DataLink.UnitData != null)
            {
                return runtimeState.DataLink.UnitData.SummonCost;
            }

            // 데이터 미존재 시 예비 기본 코스트 반환
            return defaultUnitSpawnCost;
        }

        // 필드 생존 수(최대 10기) 및 DP 코스트 보유량 검증 후 무작위 타일 아군 유닛 소환 시도 기능
        public bool TrySpawnUnit(bool ignoreDpCost = false)
        {
            // 1. 필드 아군 유닛 수 10기 제한 검사
            if (CurrentFieldUnitCount >= maxFieldUnitCount)
            {
                return false;
            }

            // 2. CurrencyManager 인스턴스 존재 유무 검증
            if (!ignoreDpCost && CurrencyManager.Instance == null)
            {
                Debug.LogWarning("[MapUnitSummonManager] CurrencyManager 인스턴스가 존재하지 않아 소환을 진행할 수 없습니다.");
                return false;
            }

            // 3. 소환 대상 타일 타입 무작위 선별 (근거리 Path 타일 vs 원거리 HighGround 타일)
            bool isMeleeTarget = Random.value > 0.5f;
            TileType primaryTileType = isMeleeTarget ? TileType.Path : TileType.HighGround;
            TileType secondaryTileType = isMeleeTarget ? TileType.HighGround : TileType.Path;

            TileNode targetTile = mapGenerator.FindRandomDeployableTile(primaryTileType);
            
            // 주 소환 타일 타입 부재 시 보조 타일 타입 탐색
            if (targetTile == null)
            {
                targetTile = mapGenerator.FindRandomDeployableTile(secondaryTileType);
            }

            // 배치 가능 빈 타일 미존재 시 소환 중단 처리
            if (targetTile == null)
            {
                return false;
            }

            // 4. 결정된 타일 대응 프리팹 선택 및 UnitDataSO 기준 소환 코스트 추출
            GameObject targetPrefab = (targetTile.TileType == TileType.Path) ? meleeUnitPrefab : rangedUnitPrefab;
            int requiredDpCost = GetUnitSummonCost(targetPrefab);

            // 5. CurrencyManager 보유 DP 검증 및 안전 차감 처리
            if (!ignoreDpCost)
            {
                if (!CurrencyManager.Instance.HasDpCost(requiredDpCost))
                {
                    return false; // DP 부족 처리
                }

                if (!CurrencyManager.Instance.TrySpendDpCost(requiredDpCost))
                {
                    return false;
                }
            }

            // 6. 타일 타입별 유닛 생성 함수 호출
            if (targetTile.TileType == TileType.Path)
            {
                return SpawnMeleeUnitOnTile(targetTile);
            }
            else
            {
                return SpawnRangedUnitOnTile(targetTile);
            }
        }

        #endregion

        #region 유닛 소환 및 이벤트 바인딩 로직

        // 지정 타일 위치 기반 근거리 아군 유닛 Instantiate 생성, 타일 점유 설정 및 사망 이벤트 연동 처리
        private bool SpawnMeleeUnitOnTile(TileNode tileNode)
        {
            if (meleeUnitPrefab == null || mapGenerator == null || mapGenerator.MapRenderer == null)
            {
                Debug.LogError("[MapUnitSummonManager] meleeUnitPrefab 또는 MapRenderer가 설정되지 않았습니다.", this);
                return false;
            }

            // 격자 좌표의 월드 좌표 변환 및 높이 오프셋 적용
            Vector3 worldPos = mapGenerator.MapRenderer.GridToWorld(tileNode.GridPosition);
            worldPos.y += meleeUnitHeight;

            // 유닛 프리팹 Instantiate 생성
            GameObject instance = Instantiate(meleeUnitPrefab, worldPos, meleeUnitPrefab.transform.rotation);
            UnitRuntimeState state = instance.GetComponent<UnitRuntimeState>();

            if (state == null || !state.IsInitialized || state.GridPosition == null)
            {
                Debug.LogError($"[MapUnitSummonManager] {meleeUnitPrefab.name}에 정상적인 UnitRuntimeState가 없습니다.", instance);
                Destroy(instance);
                return false;
            }

            // 유닛 격자 위치 초기화 및 타일 Occupied 상태 설정
            state.GridPosition.Initialize(tileNode.GridPosition, GridFacingDirection.East, CombatTargetLayer.Ground);
            tileNode.SetOccupied(true);

            // 사망 시 디스폰 및 타일 해제를 위한 CombatHealth.OnDied 이벤트 바인딩
            RegisterUnitDeathEvent(state, tileNode);

            return true;
        }

        // 지정 타일 위치 기반 원거리 아군 유닛 Instantiate 생성, 타일 점유 설정 및 사망 이벤트 연동 처리
        private bool SpawnRangedUnitOnTile(TileNode tileNode)
        {
            if (rangedUnitPrefab == null || mapGenerator == null || mapGenerator.MapRenderer == null)
            {
                Debug.LogError("[MapUnitSummonManager] rangedUnitPrefab 또는 MapRenderer가 설정되지 않았습니다.", this);
                return false;
            }

            // 격자 좌표의 월드 좌표 변환 및 높이 오프셋 적용
            Vector3 worldPos = mapGenerator.MapRenderer.GridToWorld(tileNode.GridPosition);
            worldPos.y += rangedUnitHeight;

            // 유닛 프리팹 Instantiate 생성
            GameObject instance = Instantiate(rangedUnitPrefab, worldPos, rangedUnitPrefab.transform.rotation);
            UnitRuntimeState state = instance.GetComponent<UnitRuntimeState>();

            if (state == null || !state.IsInitialized || state.GridPosition == null)
            {
                Debug.LogError($"[MapUnitSummonManager] {rangedUnitPrefab.name}에 정상적인 UnitRuntimeState가 없습니다.", instance);
                Destroy(instance);
                return false;
            }

            // 유닛 격자 위치 초기화 및 타일 Occupied 상태 설정
            state.GridPosition.Initialize(tileNode.GridPosition, GridFacingDirection.East, CombatTargetLayer.Ground);
            tileNode.SetOccupied(true);

            // 사망 시 디스폰 및 타일 해제를 위한 CombatHealth.OnDied 이벤트 바인딩
            RegisterUnitDeathEvent(state, tileNode);

            return true;
        }

        // 유닛 CombatHealth.OnDied 사망 이벤트 구독 및 타일 매핑 테이블 등록 기능
        private void RegisterUnitDeathEvent(UnitRuntimeState unitState, TileNode tileNode)
        {
            if (unitState == null || unitState.Health == null) return;

            CombatHealth health = unitState.Health;
            _occupiedTilesByUnit[health] = tileNode;

            // 이벤트 중복 구독 방지를 위한 선 해제 후 재구독 처리
            health.OnDied -= HandleUnitDied;
            health.OnDied += HandleUnitDied;
        }

        #endregion

        #region 디스폰 및 사망 처리 로직

        // 아군 유닛 사망 감지 시 점유 타일(TileNode) 즉시 해제, ReadyEffect 해제, 이벤트 해제 및 GameObject 디스폰(Destroy) 처리
        // Step 1: 사망 유닛 대응 매핑 TileNode 검색 및 SetOccupied(false) 호출
        // Step 2: OnDied 핸들러 구독 해제 및 딕셔너리 항목 삭제
        // Step 3: 유닛 부착 ReadyEffect 이펙트 사전 Hide 해제 및 풀 회수 처리
        // Step 4: 사망 유닛 GameObject 제거 및 메모리 해제
        private void HandleUnitDied(CombatHealth health)
        {
            if (health == null) return;

            // 1. 매핑 타일 정보 조회 및 점유 해제
            if (_occupiedTilesByUnit.TryGetValue(health, out TileNode occupiedTile))
            {
                if (occupiedTile != null)
                {
                    occupiedTile.SetOccupied(false);
                }
                _occupiedTilesByUnit.Remove(health);
            }

            // 2. 이벤트 핸들러 해제
            health.OnDied -= HandleUnitDied;

            // 3. 유닛 파괴 전 부착된 ReadyEffect 안전 해제 및 이펙트 풀 회수 처리
            UnitRuntimeState unitState = health.GetComponent<UnitRuntimeState>();
            if (unitState != null)
            {
                ReadyEffect.Hide(unitState);
            }

            // 4. 사망 유닛 GameObject 제거 (Destroy 처리)
            if (health.gameObject != null)
            {
                Destroy(health.gameObject);
            }
        }

        // 매니저 객체 파괴 시 전체 유닛의 사망 이벤트 구독 일괄 정리 및 메모리 누수 방지 처리
        private void ClearAllUnitSubscriptions()
        {
            foreach (KeyValuePair<CombatHealth, TileNode> pair in _occupiedTilesByUnit)
            {
                if (pair.Key != null)
                {
                    pair.Key.OnDied -= HandleUnitDied;
                }
            }
            _occupiedTilesByUnit.Clear();
        }

        #endregion
    }
}
