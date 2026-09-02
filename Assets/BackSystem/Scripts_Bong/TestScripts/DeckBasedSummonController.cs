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
    // 아군 유닛 소환 우선순위 규칙 열거형
    public enum SummonPriorityMode
    {
        [InspectorName("덱 슬롯 순서 (1번 슬롯부터)")]
        DeckOrder = 0,

        [InspectorName("DP 코스트 낮은 순 (최소 코스트 우선)")]
        LowestCostFirst = 1
    }

    // 덱 편성 기반 아군 유닛 배치, DP 소모 검증 및 자동 소환 제어 컴포넌트
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

        private readonly List<UnitDataSO> _deckUnits = new List<UnitDataSO>();
        private readonly Dictionary<CombatHealth, TileNode> _occupiedTilesByUnit = new Dictionary<CombatHealth, TileNode>();
        private readonly Dictionary<CombatHealth, UnitDataSO> _spawnedUnitDataMap = new Dictionary<CombatHealth, UnitDataSO>();
        private readonly HashSet<string> _activeUnitIds = new HashSet<string>();
        private Coroutine _autoSpawnRoutine;

        #endregion

        #region 프로퍼티 및 이벤트

        public SummonPriorityMode CurrentPriorityMode => priorityMode;
        public int CurrentFieldUnitCount => _occupiedTilesByUnit.Count;

        public event Action<SummonPriorityMode> OnPriorityModeChanged;

        #endregion

        #region 라이프사이클

        // 컴포넌트 캐싱 및 UI 초기화
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

        // 덱 변경 및 맵 생성 이벤트 리스너 등록
        private void OnEnable()
        {
            EventBus.Subscribe<NormalDeckChangedEvent>(OnNormalDeckChanged);

            if (mapGenerator != null)
            {
                mapGenerator.OnMapGenerated += HandleMapRegenerated;
            }
        }

        // 덱 데이터 동기화 및 맵 생성 확인
        private void Start()
        {
            RefreshDeckDataFromManager();

            if (mapGenerator != null && mapGenerator.IsMapGenerated)
            {
                HandleMapRegenerated();
            }
        }

        // 이벤트 구독 해제 및 필드 유닛 정리
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

        // 맵 재생성 완료 이벤트 콜백
        private void HandleMapRegenerated()
        {
            ClearAllFieldUnits();

            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.ResetDpCostOnRoundStart();
            }

            if (autoSpawnEnabled)
            {
                StartAutoSpawnLoop();
            }
        }

        // 덱 매니저로부터 일반 덱 유닛 목록 갱신
        private void RefreshDeckDataFromManager()
        {
            if (DeckManager.Instance == null) return;

            List<DeckSlotUnitEntry> activeSlots = DeckManager.Instance.GetActiveDeckSlotEntries(DeckType.Normal);
            UpdateDeckUnitList(activeSlots);
        }

        // 덱 슬롯 엔트리 목록 동기화
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

        // 일반 덱 변경 이벤트 콜백
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

        // 소환 우선순위 모드 토글 전환
        public void TogglePriorityMode()
        {
            priorityMode = (priorityMode == SummonPriorityMode.DeckOrder)
                ? SummonPriorityMode.LowestCostFirst
                : SummonPriorityMode.DeckOrder;

            UpdateStatusUI();
            OnPriorityModeChanged?.Invoke(priorityMode);
            Debug.Log($"[DeckBasedSummonController] 소환 우선순위 규칙 변경: {priorityMode}");
        }

        // 소환 우선순위 모드 직접 설정
        public void SetPriorityMode(SummonPriorityMode mode)
        {
            priorityMode = mode;
            UpdateStatusUI();
            OnPriorityModeChanged?.Invoke(priorityMode);
        }

        // 소환 모드 텍스트 UI 갱신
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

        // 자동 소환 코루틴 가동
        public void StartAutoSpawnLoop()
        {
            if (_autoSpawnRoutine != null)
            {
                StopCoroutine(_autoSpawnRoutine);
            }
            _autoSpawnRoutine = StartCoroutine(AutoSpawnLoopRoutine());
        }

        // 자동 소환 코루틴 정지
        public void StopAutoSpawnLoop()
        {
            if (_autoSpawnRoutine != null)
            {
                StopCoroutine(_autoSpawnRoutine);
                _autoSpawnRoutine = null;
            }
        }

        // 주기적 DP 확인 및 유닛 순차 소환 코루틴
        private IEnumerator AutoSpawnLoopRoutine()
        {
            while (autoSpawnEnabled)
            {
                yield return new WaitForSeconds(spawnCheckInterval);

                if (mapGenerator == null || mapGenerator.Grid == null) continue;
                if (CurrentFieldUnitCount >= maxFieldUnits) continue;

                TrySpawnNextEligibleUnit();
            }
        }

        // 소환 우선순위 규칙에 따른 유닛 선별 및 소환 시도
        private bool TrySpawnNextEligibleUnit()
        {
            if (_deckUnits.Count == 0 || mapGenerator == null || !mapGenerator.IsMapGenerated) return false;

            List<UnitDataSO> candidateUnits = _deckUnits
                .Where(u => u != null && u.UnitPrefab != null && !_activeUnitIds.Contains(u.UnitId))
                .ToList();

            if (candidateUnits.Count == 0) return false;

            if (priorityMode == SummonPriorityMode.LowestCostFirst)
            {
                candidateUnits.Sort((a, b) => a.SummonCost.CompareTo(b.SummonCost));
            }

            foreach (UnitDataSO unitToSpawn in candidateUnits)
            {
                int requiredCost = Mathf.Max(0, unitToSpawn.SummonCost);

                if (CurrencyManager.Instance != null)
                {
                    if (!CurrencyManager.Instance.HasDpCost(requiredCost))
                    {
                        continue;
                    }

                    if (!CurrencyManager.Instance.TrySpendDpCost(requiredCost))
                    {
                        continue;
                    }
                }

                bool spawnSuccess = SpawnUnitOnTile(unitToSpawn);
                if (spawnSuccess)
                {
                    return true;
                }
            }

            return false;
        }

        // 타일 검색 및 아군 유닛 인스턴스화
        private bool SpawnUnitOnTile(UnitDataSO unitData)
        {
            if (unitData == null || unitData.UnitPrefab == null || mapGenerator == null || mapGenerator.MapRenderer == null)
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
                    targetTileType = (Random.value > 0.5f) ? TileType.Path : TileType.HighGround;
                    break;

                default:
                    targetTileType = TileType.Path;
                    break;
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

            Vector3 spawnWorldPos = mapGenerator.MapRenderer.GridToWorld(targetTile.GridPosition);
            spawnWorldPos.y += (targetTile.TileType == TileType.HighGround) ? highGroundUnitHeight : groundUnitHeight;

            GameObject unitInstance = Instantiate(unitData.UnitPrefab, spawnWorldPos, unitData.UnitPrefab.transform.rotation);
            unitInstance.name = $"Ally_{unitData.DisplayName}_{targetTile.GridPosition.x}_{targetTile.GridPosition.y}";

            UnitRuntimeState runtimeState = unitInstance.GetComponent<UnitRuntimeState>();
            if (runtimeState == null)
            {
                Debug.LogError($"[DeckBasedSummonController] {unitData.DisplayName} 프리팹에 UnitRuntimeState 컴포넌트가 없습니다.", unitInstance);
                Destroy(unitInstance);
                return false;
            }

            runtimeState.InitializeRuntime();

            GridFacingDirection facing = CalculateOptimalFacingDirection(targetTile.GridPosition);

            if (runtimeState.GridPosition != null)
            {
                runtimeState.GridPosition.Initialize(targetTile.GridPosition, facing, CombatTargetLayer.Ground);
            }

            targetTile.SetOccupied(true);

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

        // 경로 좌표 분석을 통한 최적 시선 방향 산출
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

        // 유닛 사망 이벤트 콜백
        private void HandleUnitDied(CombatHealth health)
        {
            if (health == null) return;

            if (_occupiedTilesByUnit.TryGetValue(health, out TileNode occupiedTile))
            {
                if (occupiedTile != null)
                {
                    occupiedTile.SetOccupied(false);
                }
                _occupiedTilesByUnit.Remove(health);
            }

            if (_spawnedUnitDataMap.TryGetValue(health, out UnitDataSO unitData) && unitData != null)
            {
                _activeUnitIds.Remove(unitData.UnitId);
                EventBus.Publish(new UnitFieldSpawnStateChangedEvent(unitData.UnitId, false));
                _spawnedUnitDataMap.Remove(health);
            }

            health.OnDied -= HandleUnitDied;

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

        // 필드 상의 모든 아군 유닛 일괄 정리
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
