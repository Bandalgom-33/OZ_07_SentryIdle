using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using UnityEngine;

// 던전별 실시간 가동 상태 및 전투력 인스펙터 모니터링 구조체
[Serializable]
public struct DungeonInspectorState
{
    [Tooltip("던전 식별 ID")]
    public string dungeonId;
    [Tooltip("던전 명칭")]
    public string dungeonName;
    [Tooltip("현재 배치된 유닛 ID 3개")]
    public int[] assignedUnitIds;
    [Tooltip("현재 계산된 총 전투력")]
    public int totalCombatPower;
    [Tooltip("요구 최소 전투력")]
    public int requiredPower;
    [Tooltip("초과 보너스 배율 (%)")]
    public float bonusPercent;
    [Tooltip("현재 자동 생산 가동 여부")]
    public bool isRunning;
    [Tooltip("현재 주기 진행도 (0.0 ~ 1.0)")]
    public float progressRatio;
}

// 던전 시스템 총괄 마스터 매니저
public class DungeonManager : SingletonBase<DungeonManager>
{
    #region 직렬화 변수 (인스펙터 바인딩 및 모니터링)

    [Header("--- 카탈로그 참조 ---")]
    [Tooltip("던전 3종 ScriptableObject 리스트")]
    [SerializeField] private List<DungeonDataSO> dungeonList = new List<DungeonDataSO>();

    [Tooltip("유닛 메타데이터 조회를 위한 UnitCatalog")]
    [SerializeField] private UnitCatalog unitCatalog;

    [Header("--- [Debug] 실시간 던전 가동 현황 모니터링 ---")]
    [SerializeField] private List<DungeonInspectorState> dungeonInspectorViews = new List<DungeonInspectorState>();

    #endregion

    #region 내부 런타임 필드

    private readonly Dictionary<string, DungeonDataSO> _dungeonDataMap = new Dictionary<string, DungeonDataSO>();
    private readonly Dictionary<string, int[]> _assignedUnitsMap = new Dictionary<string, int[]>();
    private readonly Dictionary<string, float> _cycleTimerMap = new Dictionary<string, float>();
    private List<UnitSaveData> _cachedOwnedUnits = new List<UnitSaveData>();

    #endregion

    #region 라이프사이클

    // 던전 및 카탈로그 데이터 초기화
    protected override void Awake()
    {
        base.Awake();

        if (unitCatalog == null)
        {
            unitCatalog = Resources.Load<UnitCatalog>("Catalogs/UnitCatalog");
        }

        if (dungeonList == null || dungeonList.Count == 0)
        {
            DungeonDataSO[] loadedSO = Resources.LoadAll<DungeonDataSO>("Dungeons");
            if (loadedSO != null && loadedSO.Length > 0)
            {
                dungeonList = new List<DungeonDataSO>(loadedSO);
            }
        }

        InitializeDungeonMaps();
    }

    // 전역 이벤트 버스 구독 등록
    private void OnEnable()
    {
        EventBus.Subscribe<DataLoadEvent>(OnLoad);
        EventBus.Subscribe<DataSaveEvent>(OnSave);
        EventBus.Subscribe<DataResetEvent>(OnReset);
    }

    // 전역 이벤트 버스 구독 해제
    private void OnDisable()
    {
        EventBus.Unsubscribe<DataLoadEvent>(OnLoad);
        EventBus.Unsubscribe<DataSaveEvent>(OnSave);
        EventBus.Unsubscribe<DataResetEvent>(OnReset);
    }

    // 실시간 던전 자동 생산 타이머 및 게이지 갱신
    private void Update()
    {
        if (dungeonList == null || dungeonList.Count == 0) return;

        bool stateChanged = false;

        for (int i = 0; i < dungeonList.Count; i++)
        {
            DungeonDataSO dataSO = dungeonList[i];
            if (dataSO == null) continue;

            string dId = dataSO.DungeonId;
            int totalPower = GetDungeonTotalPower(dId);
            bool isRunning = totalPower >= dataSO.RequiredMinCombatPower;

            if (isRunning)
            {
                float currentTimer = _cycleTimerMap.TryGetValue(dId, out float t) ? t : 0.0f;
                currentTimer += Time.deltaTime;
                float cycleDuration = dataSO.BaseCycleSeconds;

                if (currentTimer >= cycleDuration)
                {
                    int completedCycles = (int)(currentTimer / cycleDuration);
                    currentTimer %= cycleDuration;

                    GrantDungeonReward(dataSO, totalPower, completedCycles);
                }

                _cycleTimerMap[dId] = currentTimer;
                float ratio = Mathf.Clamp01(currentTimer / cycleDuration);
                float remainingSec = Mathf.Max(0.0f, cycleDuration - currentTimer);

                EventBus.Publish(new DungeonProgressUpdatedEvent(dId, ratio, remainingSec, true));
                stateChanged = true;
            }
            else
            {
                EventBus.Publish(new DungeonProgressUpdatedEvent(dId, 0.0f, dataSO.BaseCycleSeconds, false));
            }
        }

        if (stateChanged)
        {
            RefreshInspectorViews();
        }
    }

    #endregion

    #region 보상 지급 로직

    // 던전 1회 완료 보상 실시간 지급 처리
    private void GrantDungeonReward(DungeonDataSO dataSO, int totalPower, int cycleCount)
    {
        if (dataSO == null || cycleCount <= 0) return;

        long finalGoldPerCycle = dataSO.CalculateFinalGold(totalPower);
        long finalDiaPerCycle = dataSO.CalculateFinalDiamond(totalPower);
        long finalStonePerCycle = dataSO.CalculateFinalStageStone(totalPower);

        long totalGold = finalGoldPerCycle * cycleCount;
        long totalDia = finalDiaPerCycle * cycleCount;
        long totalStone = finalStonePerCycle * cycleCount;
        float bonusRatio = dataSO.CalculateBonusRatio(totalPower);

        if (CurrencyManager.Instance != null)
        {
            if (totalGold > 0) CurrencyManager.Instance.AddCurrency(CurrencyType.Gold, totalGold, applyModifiers: false);
            if (totalDia > 0) CurrencyManager.Instance.AddCurrency(CurrencyType.Diamond, totalDia, applyModifiers: false);
            if (totalStone > 0) CurrencyManager.Instance.AddCurrency(CurrencyType.StageStone, totalStone, applyModifiers: false);
        }

        EventBus.Publish(new DungeonCycleCompletedEvent(
            dataSO.DungeonId,
            totalGold,
            totalDia,
            totalStone,
            bonusRatio
        ));
    }

    #endregion

    #region 런타임 실시간 전투력 계산 API

    // 유닛 런타임 레벨 조회
    public int GetUnitRuntimeLevel(int unitId)
    {
        if (unitId <= 0) return 1;

        string unitKey = $"UNIT_{unitId:D4}";

        if (CollectionDataProvider.Instance != null)
        {
            var viewModels = CollectionDataProvider.Instance.GetCollectionViewModels();
            if (viewModels != null)
            {
                for (int i = 0; i < viewModels.Count; i++)
                {
                    if (viewModels[i] != null && viewModels[i].UnitId == unitKey)
                    {
                        return Mathf.Max(1, viewModels[i].Level);
                    }
                }
            }
        }

        UnitSaveData unitSave = FindOwnedUnitSave(unitId);
        if (unitSave != null)
        {
            return Mathf.Max(1, unitSave.level);
        }

        if (unitCatalog != null && unitCatalog.TryGetById(unitKey, out UnitDataSO so) && so != null)
        {
            return Mathf.Max(1, so.InitialLevel);
        }

        return 1;
    }

    // 유닛 런타임 돌파 단계 조회
    public int GetUnitRuntimeBreakthrough(int unitId)
    {
        if (unitId <= 0) return 0;

        string unitKey = $"UNIT_{unitId:D4}";

        if (CollectionDataProvider.Instance != null)
        {
            var viewModels = CollectionDataProvider.Instance.GetCollectionViewModels();
            if (viewModels != null)
            {
                for (int i = 0; i < viewModels.Count; i++)
                {
                    if (viewModels[i] != null && viewModels[i].UnitId == unitKey)
                    {
                        return Mathf.Max(0, viewModels[i].BreakThroughStep);
                    }
                }
            }
        }

        UnitSaveData unitSave = FindOwnedUnitSave(unitId);
        if (unitSave != null)
        {
            return Mathf.Max(0, unitSave.breakThroughStep);
        }

        return 0;
    }

    // 유닛 런타임 실시간 전투력 계산
    public int GetUnitCombatPower(int unitId)
    {
        if (unitId <= 0) return 0;

        int level = GetUnitRuntimeLevel(unitId);
        int breakthroughMultiplier = GetUnitRuntimeBreakthrough(unitId) + 1;

        return Mathf.Max(1, level * breakthroughMultiplier);
    }

    // 문자열 키 기반 유닛 전투력 계산
    public int GetUnitCombatPower(string unitKey)
    {
        int unitId = ParseUnitId(unitKey);
        return GetUnitCombatPower(unitId);
    }

    // 던전 총 전투력 합산 반환
    public int GetDungeonTotalPower(string dungeonId)
    {
        if (!_assignedUnitsMap.TryGetValue(dungeonId, out int[] slots) || slots == null)
        {
            return 0;
        }

        int sum = 0;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] > 0)
            {
                sum += GetUnitCombatPower(slots[i]);
            }
        }
        return sum;
    }

    // 유닛 파견 던전 ID 및 슬롯 인덱스 조회
    public string GetAssignedDungeonId(int unitId, out int slotIndex)
    {
        slotIndex = -1;
        if (unitId <= 0) return null;

        foreach (var pair in _assignedUnitsMap)
        {
            int[] slots = pair.Value;
            if (slots == null) continue;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == unitId)
                {
                    slotIndex = i;
                    return pair.Key;
                }
            }
        }
        return null;
    }

    #endregion

    #region 유닛 파견 편성 조작 API

    // 던전 슬롯 유닛 배치 및 중복 해제 처리
    public bool AssignUnitToSlot(string targetDungeonId, int targetSlotIndex, int unitId)
    {
        if (string.IsNullOrEmpty(targetDungeonId) || targetSlotIndex < 0 || targetSlotIndex >= 3 || unitId <= 0)
        {
            return false;
        }

        if (!_assignedUnitsMap.TryGetValue(targetDungeonId, out int[] targetSlots))
        {
            return false;
        }

        string prevDungeonId = GetAssignedDungeonId(unitId, out int prevSlotIndex);
        if (!string.IsNullOrEmpty(prevDungeonId))
        {
            _assignedUnitsMap[prevDungeonId][prevSlotIndex] = -1;
            PublishFormationChanged(prevDungeonId);
        }

        targetSlots[targetSlotIndex] = unitId;
        PublishFormationChanged(targetDungeonId);
        return true;
    }

    // 빈 슬롯 유닛 자동 장착
    public bool TryAddUnitToDungeon(string dungeonId, int unitId, out int assignedSlotIndex)
    {
        assignedSlotIndex = -1;
        if (string.IsNullOrEmpty(dungeonId) || unitId <= 0) return false;

        if (!_assignedUnitsMap.TryGetValue(dungeonId, out int[] slots)) return false;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] <= 0)
            {
                AssignUnitToSlot(dungeonId, i, unitId);
                assignedSlotIndex = i;
                return true;
            }
        }

        return false;
    }

    // 던전 슬롯 유닛 해제
    public bool RemoveUnitFromSlot(string dungeonId, int slotIndex)
    {
        if (string.IsNullOrEmpty(dungeonId) || slotIndex < 0 || slotIndex >= 3) return false;

        if (_assignedUnitsMap.TryGetValue(dungeonId, out int[] slots))
        {
            if (slots[slotIndex] != -1)
            {
                slots[slotIndex] = -1;
                PublishFormationChanged(dungeonId);
                return true;
            }
        }
        return false;
    }

    // 던전 전체 슬롯 초기화
    public void ClearDungeonSlots(string dungeonId)
    {
        if (string.IsNullOrEmpty(dungeonId)) return;

        if (_assignedUnitsMap.TryGetValue(dungeonId, out int[] slots))
        {
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = -1;
            }
            PublishFormationChanged(dungeonId);
        }
    }

    // 최강 전투력 유닛 3인 자동 일괄 배치
    public void AutoAssignHighestPowerUnits(string dungeonId)
    {
        if (string.IsNullOrEmpty(dungeonId) || _cachedOwnedUnits == null || _cachedOwnedUnits.Count == 0)
        {
            return;
        }

        ClearDungeonSlots(dungeonId);

        List<(int unitId, int combatPower)> candidates = new List<(int, int)>();

        for (int i = 0; i < _cachedOwnedUnits.Count; i++)
        {
            int uId = _cachedOwnedUnits[i].unitId;
            if (uId <= 0) continue;

            string assignedDungeon = GetAssignedDungeonId(uId, out _);
            if (string.IsNullOrEmpty(assignedDungeon))
            {
                int power = GetUnitCombatPower(uId);
                candidates.Add((uId, power));
            }
        }

        candidates.Sort((a, b) => b.combatPower.CompareTo(a.combatPower));

        int assignCount = Mathf.Min(3, candidates.Count);
        for (int i = 0; i < assignCount; i++)
        {
            AssignUnitToSlot(dungeonId, i, candidates[i].unitId);
        }
    }

    #endregion

    #region 데이터 조회 API

    // 전체 던전 SO 목록 반환
    public IReadOnlyList<DungeonDataSO> GetAllDungeonData() => dungeonList;

    // 던전 ID 기반 SO 조회
    public DungeonDataSO GetDungeonData(string dungeonId)
    {
        if (string.IsNullOrEmpty(dungeonId)) return null;
        _dungeonDataMap.TryGetValue(dungeonId, out DungeonDataSO so);
        return so;
    }

    // 던전 배치 유닛 ID 배열 반환
    public int[] GetAssignedUnitIds(string dungeonId)
    {
        if (_assignedUnitsMap.TryGetValue(dungeonId, out int[] slots) && slots != null)
        {
            return (int[])slots.Clone();
        }
        return new int[3] { -1, -1, -1 };
    }

    // 던전 현재 진행 타이머 반환
    public float GetCurrentCycleTimer(string dungeonId)
    {
        return _cycleTimerMap.TryGetValue(dungeonId, out float t) ? t : 0.0f;
    }

    #endregion

    #region 세이브 / 로드 및 오프라인 방치 정산

    // 세이브 데이터 로드 및 오프라인 보상 정산
    private void OnLoad(DataLoadEvent evt)
    {
        if (evt.saveData == null) return;

        if (evt.saveData.unitDeck != null && evt.saveData.unitDeck.ownedUnits != null)
        {
            _cachedOwnedUnits = evt.saveData.unitDeck.ownedUnits;
        }

        if (evt.saveData.dungeon != null && evt.saveData.dungeon.dungeonSlots != null)
        {
            for (int i = 0; i < evt.saveData.dungeon.dungeonSlots.Count; i++)
            {
                DungeonSlotSaveData slotSave = evt.saveData.dungeon.dungeonSlots[i];
                if (slotSave == null || string.IsNullOrEmpty(slotSave.dungeonId)) continue;

                if (slotSave.assignedUnitIds != null && slotSave.assignedUnitIds.Length == 3)
                {
                    _assignedUnitsMap[slotSave.dungeonId] = (int[])slotSave.assignedUnitIds.Clone();
                }

                _cycleTimerMap[slotSave.dungeonId] = slotSave.currentCycleTimer;
            }
        }

        ProcessOfflineDungeonRewards(evt.saveData.lastSaveTimestamp);

        for (int i = 0; i < dungeonList.Count; i++)
        {
            if (dungeonList[i] != null)
            {
                PublishFormationChanged(dungeonList[i].DungeonId);
            }
        }
        RefreshInspectorViews();
    }

    // 오프라인 누적 생산 보상 일괄 지급
    private void ProcessOfflineDungeonRewards(string lastSaveTimestamp)
    {
        if (string.IsNullOrEmpty(lastSaveTimestamp)) return;

        if (!DateTime.TryParse(lastSaveTimestamp, out DateTime lastSaveTime))
        {
            return;
        }

        TimeSpan elapsed = DateTime.UtcNow - lastSaveTime;
        float offlineSeconds = (float)elapsed.TotalSeconds;
        if (offlineSeconds <= 0.0f) return;

        for (int i = 0; i < dungeonList.Count; i++)
        {
            DungeonDataSO dataSO = dungeonList[i];
            if (dataSO == null) continue;

            string dId = dataSO.DungeonId;
            int totalPower = GetDungeonTotalPower(dId);
            bool isRunning = totalPower >= dataSO.RequiredMinCombatPower;

            if (isRunning)
            {
                float prevTimer = _cycleTimerMap.TryGetValue(dId, out float t) ? t : 0.0f;
                float totalAccumulatedTime = prevTimer + offlineSeconds;
                float cycleDuration = dataSO.BaseCycleSeconds;

                int offlineCycles = (int)(totalAccumulatedTime / cycleDuration);
                float remainingTimer = totalAccumulatedTime % cycleDuration;

                if (offlineCycles > 0)
                {
                    GrantDungeonReward(dataSO, totalPower, offlineCycles);
                }

                _cycleTimerMap[dId] = remainingTimer;
            }
        }
    }

    // 던전 진행 상태 데이터 저장
    private void OnSave(DataSaveEvent evt)
    {
        if (evt.saveData == null) return;

        if (evt.saveData.dungeon == null)
        {
            evt.saveData.dungeon = new DungeonSaveData();
        }

        evt.saveData.dungeon.dungeonSlots.Clear();

        foreach (var pair in _assignedUnitsMap)
        {
            string dId = pair.Key;
            int[] slots = pair.Value;
            float timer = _cycleTimerMap.TryGetValue(dId, out float t) ? t : 0.0f;

            evt.saveData.dungeon.dungeonSlots.Add(new DungeonSlotSaveData
            {
                dungeonId = dId,
                assignedUnitIds = (int[])slots.Clone(),
                currentCycleTimer = timer
            });
        }
    }

    // 던전 데이터 초기화
    private void OnReset(DataResetEvent evt)
    {
        InitializeDungeonMaps();
        for (int i = 0; i < dungeonList.Count; i++)
        {
            if (dungeonList[i] != null)
            {
                PublishFormationChanged(dungeonList[i].DungeonId);
            }
        }
        RefreshInspectorViews();
    }

    #endregion

    #region 내부 헬퍼 메서드

    // 내부 런타임 맵 초기화
    private void InitializeDungeonMaps()
    {
        _dungeonDataMap.Clear();

        for (int i = 0; i < dungeonList.Count; i++)
        {
            DungeonDataSO so = dungeonList[i];
            if (so == null) continue;

            string dId = so.DungeonId;
            _dungeonDataMap[dId] = so;

            if (!_assignedUnitsMap.ContainsKey(dId))
            {
                _assignedUnitsMap[dId] = new int[3] { -1, -1, -1 };
            }

            if (!_cycleTimerMap.ContainsKey(dId))
            {
                _cycleTimerMap[dId] = 0.0f;
            }
        }
    }

    // 던전 편성 변경 이벤트 발행
    private void PublishFormationChanged(string dungeonId)
    {
        if (!_dungeonDataMap.TryGetValue(dungeonId, out DungeonDataSO so) || so == null) return;

        int totalPower = GetDungeonTotalPower(dungeonId);
        int reqPower = so.RequiredMinCombatPower;
        float bonus = so.CalculateBonusRatio(totalPower);
        bool isRunning = totalPower >= reqPower;
        int[] slots = GetAssignedUnitIds(dungeonId);

        EventBus.Publish(new DungeonFormationChangedEvent(
            dungeonId,
            slots,
            totalPower,
            reqPower,
            bonus,
            isRunning
        ));

        RefreshInspectorViews();
    }

    // 인스펙터 디버그 모니터링 뷰 갱신
    private void RefreshInspectorViews()
    {
        dungeonInspectorViews.Clear();

        for (int i = 0; i < dungeonList.Count; i++)
        {
            DungeonDataSO so = dungeonList[i];
            if (so == null) continue;

            string dId = so.DungeonId;
            int totalPower = GetDungeonTotalPower(dId);
            int reqPower = so.RequiredMinCombatPower;
            float bonus = so.CalculateBonusRatio(totalPower);
            bool isRunning = totalPower >= reqPower;
            float timer = _cycleTimerMap.TryGetValue(dId, out float t) ? t : 0.0f;
            float ratio = Mathf.Clamp01(timer / so.BaseCycleSeconds);

            dungeonInspectorViews.Add(new DungeonInspectorState
            {
                dungeonId = dId,
                dungeonName = so.DungeonName,
                assignedUnitIds = GetAssignedUnitIds(dId),
                totalCombatPower = totalPower,
                requiredPower = reqPower,
                bonusPercent = bonus * 100.0f,
                isRunning = isRunning,
                progressRatio = ratio
            });
        }
    }

    // 보유 유닛 세이브 데이터 조회
    private UnitSaveData FindOwnedUnitSave(int unitId)
    {
        if (_cachedOwnedUnits == null || _cachedOwnedUnits.Count == 0) return null;

        for (int i = 0; i < _cachedOwnedUnits.Count; i++)
        {
            if (_cachedOwnedUnits[i].unitId == unitId)
            {
                return _cachedOwnedUnits[i];
            }
        }
        return null;
    }

    // 유닛 문자열 키 정수 ID 변환
    private int ParseUnitId(string unitKey)
    {
        if (string.IsNullOrEmpty(unitKey)) return -1;
        if (int.TryParse(unitKey.Replace("UNIT_", ""), out int id))
        {
            return id;
        }
        return -1;
    }

    #endregion
}
