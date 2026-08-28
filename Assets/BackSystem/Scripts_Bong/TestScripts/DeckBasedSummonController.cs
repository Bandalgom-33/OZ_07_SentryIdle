using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace EndlessGuard.TestBattle
{
    // 아군 유닛 소환 시 우선순위 규칙을 정의하는 열거형
    public enum SummonPriorityMode
    {
        [InspectorName("덱 슬롯 순서 (1번 슬롯부터)")]
        DeckOrder = 0,

        [InspectorName("DP 코스트 낮은 순 (최소 코스트 우선)")]
        LowestCostFirst = 1
    }

    // DeckManager의 덱 편성 데이터를 기반으로 CurrencyManager의 DP를 검증/차감하여 MapGenerator 타일에 아군을 순차 소환하고,
    // 소환 우선순위(덱 순서 vs 저코스트 우선) 전환, HUD 덱 카드 명암 연동 및 스테이지 전환/덱 변경 시 실시간 필드 리셋/재소환을 총괄하는 컨트롤러 클래스
    public class DeckBasedSummonController : MonoBehaviour
    {
        #region 인스펙터 직렬화 필드

        [Header("--- 맵 생성기 참조 ---")]
        [Tooltip("배치 가능 타일 검색 및 좌표 변환을 지원받을 MapGenerator 컴포넌트")]
        [SerializeField] private MapGenerator mapGenerator;

        [Header("--- 필드 유닛 및 소환 주기 설정 ---")]
        [Tooltip("필드에 동시에 생존 가능한 최대 아군 유닛 수 제한")]
        [SerializeField] private int maxFieldUnits = 10;

        [Tooltip("자동 지속 소환 활성화 여부")]
        [SerializeField] private bool autoSpawnEnabled = true;

        [Tooltip("DP 잔액 및 빈 타일을 검사하여 아군을 1기씩 소환하는 주기 (초 단위)")]
        [SerializeField] private float spawnCheckInterval = 1.0f;

        [Header("--- 유닛 배치 높이 오프셋 ---")]
        [Tooltip("지상(Path) 타일 배치 시 적용할 추가 Y축 높이")]
        [SerializeField] private float groundUnitHeight = 0.5f;

        [Tooltip("고지대(HighGround) 타일 배치 시 적용할 추가 Y축 높이")]
        [SerializeField] private float highGroundUnitHeight = 0.8f;

        [Header("--- 소환 우선순위 규칙 (인스펙터 노출) ---")]
        [Tooltip("소환 우선순위 규칙 (덱 순서 vs DP 낮은 순)")]
        [SerializeField] private SummonPriorityMode priorityMode = SummonPriorityMode.DeckOrder;

        [Header("--- UI 연동 (버튼 및 상태 텍스트) ---")]
        [Tooltip("소환 우선순위 규칙을 런타임에 전환하는 UI 버튼")]
        [SerializeField] private Button priorityToggleButton;

        [Tooltip("현재 활성화된 소환 우선순위 규칙을 표시할 TextMeshPro 텍스트 컴포넌트")]
        [SerializeField] private TextMeshProUGUI priorityStatusText;

        #endregion

        #region 내부 런타임 데이터

        // 현재 장착된 덱 유닛 데이터 목록
        private readonly List<UnitDataSO> _deckUnits = new List<UnitDataSO>();

        // 현재 필드에 소환되어 생존 중인 유닛의 CombatHealth와 점유 TileNode 매핑 딕셔너리
        private readonly Dictionary<CombatHealth, TileNode> _occupiedTilesByUnit = new Dictionary<CombatHealth, TileNode>();

        // 필드에 스폰된 유닛 인스턴스와 해당 UnitDataSO 매핑 테이블
        private readonly Dictionary<CombatHealth, UnitDataSO> _spawnedUnitDataMap = new Dictionary<CombatHealth, UnitDataSO>();

        // 필드에 살아있는 유닛들의 고유 ID 집합 (중복 소환 방지용)
        private readonly HashSet<string> _activeUnitIds = new HashSet<string>();

        // 자동 소환 코루틴 참조
        private Coroutine _autoSpawnRoutine;

        #endregion

        #region 프로퍼티 및 이벤트

        public SummonPriorityMode CurrentPriorityMode => priorityMode;
        public int CurrentFieldUnitCount => _occupiedTilesByUnit.Count;

        public event Action<SummonPriorityMode> OnPriorityModeChanged;

        #endregion

        #region 라이프사이클

        private void Awake()
        {
            if (mapGenerator == null)
            {
                mapGenerator = FindFirstObjectByType<MapGenerator>();
            }

            if (priorityToggleButton != null)
            {
                priorityToggleButton.onClick.AddListener(TogglePriorityMode);
            }

            UpdateStatusUI();
        }

        private void OnEnable()
        {
            // 덱 변경 이벤트 구독
            EventBus.Subscribe<NormalDeckChangedEvent>(OnNormalDeckChanged);

            // 기술적 근거: 스테이지 전환 시마다 맵이 재생성되므로 OnMapGenerated를 지속 구독하여 매번 아군을 안전하게 리셋/재배치
            if (mapGenerator != null)
            {
                mapGenerator.OnMapGenerated += HandleMapRegenerated;
            }
        }

        private void Start()
        {
            RefreshDeckDataFromManager();

            if (mapGenerator != null && mapGenerator.IsMapGenerated)
            {
                HandleMapRegenerated();
            }
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<NormalDeckChangedEvent>(OnNormalDeckChanged);

            if (mapGenerator != null)
            {
                mapGenerator.OnMapGenerated -= HandleMapRegenerated;
            }

            if (priorityToggleButton != null)
            {
                priorityToggleButton.onClick.RemoveListener(TogglePriorityMode);
            }

            StopAutoSpawnLoop();
            ClearAllFieldUnits();
        }

        #endregion

        #region 맵 생성 및 덱 동기화

        // 맵이 최초 생성되거나 스테이지 클리어로 재생성될 때 호출되는 콜백
        private void HandleMapRegenerated()
        {
            // 1. 이전 스테이지의 아군 유닛 일괄 파괴 및 타일 점유/추적 테이블 초기화
            ClearAllFieldUnits();

            // 2. 맵 시작 시 기본 DP(30 DP)로 안전하게 초기화
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.ResetDpCostOnRoundStart();
            }

            // 3. 초기 즉시 소환 없이 순수하게 DP 소모 검증을 거치는 자동 소환 루프 가동
            if (autoSpawnEnabled)
            {
                StartAutoSpawnLoop();
            }
        }

        // DeckManager 싱글톤에서 현재 활성화된 일반 덱 유닛 목록을 가져오는 함수
        private void RefreshDeckDataFromManager()
        {
            if (DeckManager.Instance == null) return;

            List<DeckSlotUnitEntry> activeSlots = DeckManager.Instance.GetActiveDeckSlotEntries(DeckType.Normal);
            UpdateDeckUnitList(activeSlots);
        }

        private void UpdateDeckUnitList(IReadOnlyList<DeckSlotUnitEntry> slotEntries)
        {
            _deckUnits.Clear();

            if (slotEntries == null) return;

            for (int i = 0; i < slotEntries.Count; i++)
            {
                if (slotEntries[i].unitData != null)
                {
                    _deckUnits.Add(slotEntries[i].unitData);
                }
            }
        }

        // 덱 편성 변경 이벤트 수신 시 필드 유닛 일괄 정리 후 신규 덱 기반 재소환 시작
        private void OnNormalDeckChanged(NormalDeckChangedEvent evt)
        {
            ClearAllFieldUnits();
            UpdateDeckUnitList(evt.activeUnits);

            if (autoSpawnEnabled)
            {
                StartAutoSpawnLoop();
            }
        }

        #endregion

        #region 소환 우선순위 모드 전환 (인스펙터 및 UI 연동)

        public void TogglePriorityMode()
        {
            priorityMode = (priorityMode == SummonPriorityMode.DeckOrder)
                ? SummonPriorityMode.LowestCostFirst
                : SummonPriorityMode.DeckOrder;

            UpdateStatusUI();
            OnPriorityModeChanged?.Invoke(priorityMode);
            Debug.Log($"[DeckBasedSummonController] 소환 우선순위 규칙 변경: {priorityMode}");
        }

        public void SetPriorityMode(SummonPriorityMode mode)
        {
            priorityMode = mode;
            UpdateStatusUI();
            OnPriorityModeChanged?.Invoke(priorityMode);
        }

        private void UpdateStatusUI()
        {
            if (priorityStatusText != null)
            {
                priorityStatusText.text = (priorityMode == SummonPriorityMode.DeckOrder)
                    ? "소환 모드: 덱 순서 (1번~)"
                    : "소환 모드: 저코스트 우선 (DP순)";
            }
        }

        #endregion

        #region 자동 소환 루프 및 DP 소모 로직

        public void StartAutoSpawnLoop()
        {
            if (_autoSpawnRoutine != null)
            {
                StopCoroutine(_autoSpawnRoutine);
            }
            _autoSpawnRoutine = StartCoroutine(AutoSpawnLoopRoutine());
        }

        public void StopAutoSpawnLoop()
        {
            if (_autoSpawnRoutine != null)
            {
                StopCoroutine(_autoSpawnRoutine);
                _autoSpawnRoutine = null;
            }
        }

        // 설정된 주기에 따라 보유 DP를 검사하고 유닛을 1기씩 순차 소환하는 루틴
        private IEnumerator AutoSpawnLoopRoutine()
        {
            while (autoSpawnEnabled)
            {
                yield return new WaitForSeconds(spawnCheckInterval);

                if (mapGenerator == null || mapGenerator.Grid == null) continue;
                if (CurrentFieldUnitCount >= maxFieldUnits) continue;

                // 기술적 근거: 사용자의 요청에 따라 기본 즉시 선배치 없이 순수하게 DP 코스트를 검증/차감하여 1기씩 소환
                TrySpawnNextEligibleUnit();
            }
        }

        // 미소환 유닛 중 설정된 우선순위 규칙에 따라 유닛을 선별하여 소환 시도
        private bool TrySpawnNextEligibleUnit()
        {
            if (_deckUnits.Count == 0 || mapGenerator == null || !mapGenerator.IsMapGenerated) return false;

            // 1. 덱 유닛 중 아직 필드에 소환되지 않은 유닛 후보군 추출
            List<UnitDataSO> candidateUnits = _deckUnits
                .Where(u => u != null && u.UnitPrefab != null && !_activeUnitIds.Contains(u.UnitId))
                .ToList();

            if (candidateUnits.Count == 0) return false;

            // 2. 설정된 소환 우선순위 규칙에 따른 정렬 적용
            if (priorityMode == SummonPriorityMode.LowestCostFirst)
            {
                candidateUnits.Sort((a, b) => a.SummonCost.CompareTo(b.SummonCost));
            }

            // 3. 정렬된 후보군 순서대로 DP 검사 후 소환 시도
            foreach (UnitDataSO unitToSpawn in candidateUnits)
            {
                int requiredCost = Mathf.Max(0, unitToSpawn.SummonCost);

                if (CurrencyManager.Instance != null)
                {
                    if (!CurrencyManager.Instance.HasDpCost(requiredCost))
                    {
                        continue; // DP 부족 시 저코스트 유닛 등 다음 후보 탐색
                    }

                    if (!CurrencyManager.Instance.TrySpendDpCost(requiredCost))
                    {
                        continue;
                    }
                }

                bool spawnSuccess = SpawnUnitOnTile(unitToSpawn);
                if (spawnSuccess)
                {
                    return true; // 1회 주기당 1기 소환 성공 후 루프 탈출
                }
            }

            return false;
        }

        // 유닛의 배치 속성(Placement)에 맞는 타일을 탐색하여 유닛 인스턴스화 및 격자 초기화 수행
        private bool SpawnUnitOnTile(UnitDataSO unitData)
        {
            if (unitData == null || unitData.UnitPrefab == null || mapGenerator == null || mapGenerator.MapRenderer == null)
            {
                return false;
            }

            // 1. 유닛 배치 속성에 따른 대상 타일 타입 결정
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
                    targetTileType = (Random.value > 0.5f) ? TileType.Path : TileType.HighGround;
                    break;

                default:
                    targetTileType = TileType.Path;
                    break;
            }

            // 2. MapGenerator의 빈 타일 검색 메서드 호출
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

            // 3. 그리드 좌표의 월드 좌표 변환 및 높이 오프셋 적용
            Vector3 spawnWorldPos = mapGenerator.MapRenderer.GridToWorld(targetTile.GridPosition);
            spawnWorldPos.y += (targetTile.TileType == TileType.HighGround) ? highGroundUnitHeight : groundUnitHeight;

            // 4. 프리팹 인스턴스화
            GameObject unitInstance = Instantiate(unitData.UnitPrefab, spawnWorldPos, unitData.UnitPrefab.transform.rotation);
            unitInstance.name = $"Ally_{unitData.DisplayName}_{targetTile.GridPosition.x}_{targetTile.GridPosition.y}";

            UnitRuntimeState runtimeState = unitInstance.GetComponent<UnitRuntimeState>();
            if (runtimeState == null)
            {
                Debug.LogError($"[DeckBasedSummonController] {unitData.DisplayName} 프리팹에 UnitRuntimeState 컴포넌트가 없습니다.", unitInstance);
                Destroy(unitInstance);
                return false;
            }

            // 5. 유닛 런타임 상태 초기화
            runtimeState.InitializeRuntime();

            // 6. 적이 다가오는 입구 방향을 정면으로 마주보도록 시선 방향 자동 산출
            GridFacingDirection facing = CalculateOptimalFacingDirection(targetTile.GridPosition);

            if (runtimeState.GridPosition != null)
            {
                runtimeState.GridPosition.Initialize(targetTile.GridPosition, facing, CombatTargetLayer.Ground);
            }

            // 7. 타일 점유 설정 및 매핑 테이블 등록
            targetTile.SetOccupied(true);

            // 8. HUD 덱 슬롯 명암 변경 이벤트 발행 (소환 완료 상태 알림)
            _activeUnitIds.Add(unitData.UnitId);
            EventBus.Publish(new UnitFieldSpawnStateChangedEvent(unitData.UnitId, true));

            CombatHealth health = runtimeState.Health;
            if (health != null)
            {
                _occupiedTilesByUnit[health] = targetTile;
                _spawnedUnitDataMap[health] = unitData;

                health.OnDied -= HandleUnitDied;
                health.OnDied += HandleUnitDied;
            }

            return true;
        }

        // 소환 타일 주변의 적 이동 경로를 분석하여 적을 정면으로 마주보도록 방향 계산
        private GridFacingDirection CalculateOptimalFacingDirection(Vector2Int tileCoord)
        {
            if (mapGenerator == null) return GridFacingDirection.West;

            IReadOnlyList<Vector2Int> path = mapGenerator.PathPosition;
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
                    if (deltaToPrev.x > 0) return GridFacingDirection.East;
                    if (deltaToPrev.x < 0) return GridFacingDirection.West;
                    if (deltaToPrev.y > 0) return GridFacingDirection.North;
                    if (deltaToPrev.y < 0) return GridFacingDirection.South;
                }
            }

            return GridFacingDirection.West;
        }

        #endregion

        #region 유닛 사망 및 필드 정리

        // 유닛 사망 감지 시 타일 점유 해제, 이펙트 회수 및 게임오브젝트 제거
        private void HandleUnitDied(CombatHealth health)
        {
            if (health == null) return;

            // 1. 점유 타일 해제
            if (_occupiedTilesByUnit.TryGetValue(health, out TileNode occupiedTile))
            {
                if (occupiedTile != null)
                {
                    occupiedTile.SetOccupied(false);
                }
                _occupiedTilesByUnit.Remove(health);
            }

            // 2. 사망 유닛 ID 추적 제거 및 HUD 덱 슬롯 명암 복구 이벤트 발행
            if (_spawnedUnitDataMap.TryGetValue(health, out UnitDataSO unitData) && unitData != null)
            {
                _activeUnitIds.Remove(unitData.UnitId);
                EventBus.Publish(new UnitFieldSpawnStateChangedEvent(unitData.UnitId, false));
                _spawnedUnitDataMap.Remove(health);
            }

            // 3. 사망 이벤트 핸들러 해제
            health.OnDied -= HandleUnitDied;

            // 4. ReadyEffect 이펙트 풀 회수 및 오브젝트 파괴
            UnitRuntimeState runtimeState = health.GetComponent<UnitRuntimeState>();
            if (runtimeState != null)
            {
                ReadyEffect.Hide(runtimeState);
            }

            if (health.gameObject != null)
            {
                Destroy(health.gameObject);
            }
        }

        // 현재 필드의 모든 아군 유닛을 일괄 파괴하고 타일 점유 상태를 초기화하는 메서드
        public void ClearAllFieldUnits()
        {
            foreach (var pair in _occupiedTilesByUnit)
            {
                if (pair.Key != null)
                {
                    pair.Key.OnDied -= HandleUnitDied;

                    if (pair.Value != null)
                    {
                        pair.Value.SetOccupied(false);
                    }

                    UnitRuntimeState state = pair.Key.GetComponent<UnitRuntimeState>();
                    if (state != null)
                    {
                        ReadyEffect.Hide(state);
                    }

                    if (pair.Key.gameObject != null)
                    {
                        Destroy(pair.Key.gameObject);
                    }
                }
            }

            _occupiedTilesByUnit.Clear();
            _spawnedUnitDataMap.Clear();

            foreach (string unitId in _activeUnitIds)
            {
                EventBus.Publish(new UnitFieldSpawnStateChangedEvent(unitId, false));
            }
            _activeUnitIds.Clear();
        }

        #endregion
    }
}
