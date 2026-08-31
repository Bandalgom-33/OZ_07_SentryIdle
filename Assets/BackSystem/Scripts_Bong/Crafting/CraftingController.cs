using System;
using System.Collections.Generic;
using UnityEngine;

public class CraftingController : SingletonBase<CraftingController>
{
    #region 레시피 상태 열거형

    public enum RecipeState
    {
        Idle,
        Crafting,
        Hold
    }

    #endregion

    #region 인스펙터 바인딩 필드

    [Header("--- 레시피 데이터베이스 ---")]
    [Tooltip("공방에서 제작 가능한 CraftingRecipeSO 에셋 목록")]
    [SerializeField] private List<CraftingRecipeSO> recipeDatabase = new List<CraftingRecipeSO>();

    #endregion

    #region 내부 변수 및 프로퍼티

    private int _factoryLevel = 1;
    private bool _isGlobalAutoEnabled = false;
    private bool _isDirty = false;
    private bool _isUpgrading = false;
    private FactoryUpgradeResult _lastUpgradeResult = FactoryUpgradeResult.Success;
    private readonly List<int> _activeRecipeQueue = new List<int>();
    private readonly Dictionary<string, float> _recipeProgressMap = new Dictionary<string, float>();
    private RecipeState[] _recipeStates = Array.Empty<RecipeState>();

    public int FactoryLevel => _factoryLevel;
    public int MaxActiveSlots => FactoryUpgradeProcessor.GetMaxActiveSlots(_factoryLevel);
    public int OutputAmount => FactoryUpgradeProcessor.GetCraftingOutputAmount(_factoryLevel);
    public float SpeedMultiplier => FactoryUpgradeProcessor.GetCraftingSpeedMultiplier(_factoryLevel);
    public bool IsGlobalAutoEnabled => _isGlobalAutoEnabled;
    public bool IsDirty => _isDirty;
    public bool IsUpgrading => _isUpgrading;
    public FactoryUpgradeResult LastUpgradeResult => _lastUpgradeResult;
    public List<CraftingRecipeSO> RecipeDatabase => recipeDatabase;
    public IReadOnlyList<CraftingRecipeSO> Recipes => recipeDatabase;
    public IReadOnlyList<int> ActiveRecipeQueue => _activeRecipeQueue;
    public int CurrentQueueCount => _activeRecipeQueue.Count;
    public bool CanUpgrade => _factoryLevel < FactoryUpgradeProcessor.MaxFactoryLevel;

    public bool HasEnoughUpgradeMaterials
    {
        get
        {
            if (!CanUpgrade) return false;
            if (!FactoryUpgradeProcessor.GetUpgradeCost(_factoryLevel, out long gold, out long wave, out long stage)) return false;
            CurrencyManager cm = CurrencyManager.Instance;
            if (cm == null) return false;
            return cm.HasGold(gold) && cm.HasWaveStone(wave) && cm.HasStageStone(stage);
        }
    }

    // 특정 레시피 해금 여부 확인
    public bool IsRecipeUnlocked(int recipeIndex)
    {
        if (recipeIndex < 0 || recipeIndex >= recipeDatabase.Count || recipeDatabase[recipeIndex] == null)
        {
            return false;
        }
        return recipeDatabase[recipeIndex].unlockFactoryLevel <= _factoryLevel;
    }

    // 특정 레시피 해금 요구 공장 레벨 반환
    public int GetRequiredLevel(int recipeIndex)
    {
        if (recipeIndex >= 0 && recipeIndex < recipeDatabase.Count && recipeDatabase[recipeIndex] != null)
        {
            return recipeDatabase[recipeIndex].unlockFactoryLevel;
        }
        return 1;
    }

    // 상태 배열 크기 동적 동기화
    private void EnsureArrayCapacity(int requiredCount)
    {
        int count = Mathf.Max(requiredCount, recipeDatabase != null ? recipeDatabase.Count : 0);
        if (_recipeStates == null || _recipeStates.Length < count)
        {
            RecipeState[] newStates = new RecipeState[count];
            if (_recipeStates != null && _recipeStates.Length > 0)
            {
                Array.Copy(_recipeStates, newStates, _recipeStates.Length);
            }
            _recipeStates = newStates;
        }
    }

    #endregion

    #region 라이프사이클 및 초기화

    // 컨트롤러 싱글톤 초기화 및 상태 배열 구성
    protected override void Awake()
    {
        base.Awake();
        InitializeRecipeDatabase();
        EnsureArrayCapacity(recipeDatabase.Count);
    }

    // 인스펙터 레시피 데이터베이스 유효성 검증
    private void InitializeRecipeDatabase()
    {
        if (recipeDatabase == null || recipeDatabase.Count == 0)
        {
            Debug.LogError("[CraftingController] 인스펙터에 등록된 CraftingRecipeSO 목록이 비어 있습니다! 레시피 에셋을 할당해주세요.");
        }
        EnsureArrayCapacity(recipeDatabase != null ? recipeDatabase.Count : 0);
    }

    // 전역 이벤트 버스 구독 등록
    private void OnEnable()
    {
        EventBus.Subscribe<DataSaveEvent>(OnSave);
        EventBus.Subscribe<DataLoadEvent>(OnLoad);
        EventBus.Subscribe<DataResetEvent>(OnReset);
    }

    // 전역 이벤트 버스 구독 해제
    private void OnDisable()
    {
        EventBus.Unsubscribe<DataSaveEvent>(OnSave);
        EventBus.Unsubscribe<DataLoadEvent>(OnLoad);
        EventBus.Unsubscribe<DataResetEvent>(OnReset);
    }

    // 애플리케이션 종료 시 자동 저장
    private void OnApplicationQuit()
    {
        SaveIfDirty();
    }

    // 모바일 백그라운드 전환 시 자동 저장
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveIfDirty();
        }
    }

    // 매 프레임 제작 틱 루프 갱신
    private void Update()
    {
        UpdateCraftingProgress(Time.deltaTime);
    }

    #endregion

    #region 자동 제작 틱 루프

    // 제작 진행도 갱신 및 완료 처리
    private void UpdateCraftingProgress(float deltaTime)
    {
        CurrencyManager cm = CurrencyManager.Instance;
        ConsumableItemManager cim = ConsumableItemManager.Instance;
        if (cm == null || cim == null) return;

        EnsureArrayCapacity(recipeDatabase.Count);

        float speed = SpeedMultiplier;
        int currentOutput = OutputAmount;

        for (int i = 0; i < _recipeStates.Length; i++)
        {
            if (!_activeRecipeQueue.Contains(i) || !_isGlobalAutoEnabled)
            {
                _recipeStates[i] = RecipeState.Idle;
            }
        }

        if (!_isGlobalAutoEnabled || _activeRecipeQueue.Count == 0)
        {
            return;
        }

        for (int q = 0; q < _activeRecipeQueue.Count; q++)
        {
            int recipeIndex = _activeRecipeQueue[q];
            if (recipeIndex < 0 || recipeIndex >= recipeDatabase.Count || recipeDatabase[recipeIndex] == null) continue;

            if (!IsRecipeUnlocked(recipeIndex)) continue;

            CraftingRecipeSO recipe = recipeDatabase[recipeIndex];
            string rId = recipe.recipeId;

            int maxExpectedAmount = recipe.outputAmount * currentOutput;
            int availableCapacity = recipe.resultItem == null || InventoryGridManager.Instance == null
                ? maxExpectedAmount
                : InventoryGridManager.Instance.GetAvailableCapacityForItem(recipe.resultItem);

            if (availableCapacity <= 0 || !HasCraftingMaterials(recipe))
            {
                _recipeStates[recipeIndex] = RecipeState.Hold;
                continue;
            }

            _recipeStates[recipeIndex] = RecipeState.Crafting;

            float currentProgress = _recipeProgressMap.TryGetValue(rId, out float p) ? p : 0f;
            currentProgress += deltaTime * speed;

            if (currentProgress >= recipe.baseCraftingTime)
            {
                if (TrySpendRecipeMaterials(recipe))
                {
                    int finalAmount = Mathf.Min(maxExpectedAmount, availableCapacity);

                    if (recipe.resultItem != null && finalAmount > 0)
                    {
                        InventoryGridManager.Instance?.AddItem(recipe.resultItem, finalAmount);
                        Debug.Log($"[CraftingController] 제작 완료: {recipe.DisplayName} x{finalAmount} 인벤토리 입고");
                    }

                    _recipeProgressMap[rId] = 0f;
                    _isDirty = true;
                }
                else
                {
                    _recipeProgressMap[rId] = currentProgress;
                }
            }
            else
            {
                _recipeProgressMap[rId] = currentProgress;
            }
        }
    }

    // 레시피 제작 소모 재화 보유 여부 검증
    private bool HasCraftingMaterials(CraftingRecipeSO recipe)
    {
        if (recipe == null || CurrencyManager.Instance == null) return false;

        CurrencyManager cm = CurrencyManager.Instance;
        if (recipe.goldCost > 0 && !cm.HasGold(recipe.goldCost)) return false;
        if (recipe.diamondCost > 0 && !cm.HasDiamond(recipe.diamondCost)) return false;

        return recipe.requiredStoneType switch
        {
            StoneType.WaveStone => cm.HasWaveStone(recipe.stoneCost),
            StoneType.DungeonStone => cm.HasDungeonStone(recipe.stoneCost),
            StoneType.RaidStone => cm.HasRaidStone(recipe.stoneCost),
            _ => false
        };
    }

    // 레시피 제작 소모 재화 차감
    private bool TrySpendRecipeMaterials(CraftingRecipeSO recipe)
    {
        if (!HasCraftingMaterials(recipe)) return false;

        CurrencyManager cm = CurrencyManager.Instance;

        if (recipe.goldCost > 0 && !cm.TrySpendGold(recipe.goldCost)) return false;
        if (recipe.diamondCost > 0 && !cm.TrySpendDiamond(recipe.diamondCost)) return false;

        if (recipe.requiredStoneType == StoneType.WaveStone)
        {
            return cm.TrySpendWaveStone(recipe.stoneCost);
        }
        else if (recipe.requiredStoneType == StoneType.DungeonStone)
        {
            return cm.TrySpendDungeonStone(recipe.stoneCost);
        }
        else if (recipe.requiredStoneType == StoneType.RaidStone)
        {
            return cm.TrySpendRaidStone(recipe.stoneCost);
        }

        return false;
    }

    #endregion

    #region 제작 큐 조작 API

    // 레시피 큐 등록 여부 확인
    public bool IsRecipeInQueue(int recipeIndex)
    {
        return _activeRecipeQueue.Contains(recipeIndex);
    }

    // 레시피 큐 등록
    public bool AddRecipeToQueue(int recipeIndex)
    {
        if (recipeIndex < 0 || recipeIndex >= recipeDatabase.Count) return false;

        if (!IsRecipeUnlocked(recipeIndex))
        {
            int reqLevel = GetRequiredLevel(recipeIndex);
            Debug.LogWarning($"[CraftingController] 해당 레시피는 공장 Lv.{reqLevel} 이상에서 해금됩니다.");
            return false;
        }

        if (_activeRecipeQueue.Contains(recipeIndex))
        {
            return false;
        }

        if (_activeRecipeQueue.Count >= MaxActiveSlots)
        {
            Debug.LogWarning($"[CraftingController] 제작 슬롯 한도({MaxActiveSlots}개)가 가득 찼습니다.");
            return false;
        }

        EnsureArrayCapacity(recipeDatabase.Count);
        _activeRecipeQueue.Add(recipeIndex);
        _isDirty = true;
        return true;
    }

    // 레시피 큐 해제
    public bool RemoveRecipeFromQueue(int recipeIndex)
    {
        if (!_activeRecipeQueue.Contains(recipeIndex))
        {
            return false;
        }

        _activeRecipeQueue.Remove(recipeIndex);

        if (recipeIndex >= 0 && recipeIndex < recipeDatabase.Count && recipeDatabase[recipeIndex] != null)
        {
            string rId = recipeDatabase[recipeIndex].recipeId;
            _recipeProgressMap[rId] = 0f;
        }

        if (_recipeStates != null && recipeIndex >= 0 && recipeIndex < _recipeStates.Length)
        {
            _recipeStates[recipeIndex] = RecipeState.Idle;
        }

        _isDirty = true;
        return true;
    }

    // 전역 자동 전환 스위치 설정
    public void SetGlobalAuto(bool enabled)
    {
        _isGlobalAutoEnabled = enabled;
        _isDirty = true;
    }

    // 레시피 토글 상태 조회
    public bool IsRecipeToggleOn(int recipeIndex) => IsRecipeInQueue(recipeIndex);

    // 레시피 토글 상태 제어
    public bool SetRecipeToggle(int recipeIndex, bool isEnabled) => isEnabled ? AddRecipeToQueue(recipeIndex) : RemoveRecipeFromQueue(recipeIndex);

    // 레시피 1회 수동 즉시 제작
    public bool CraftOnce(int recipeIndex)
    {
        if (recipeIndex < 0 || recipeIndex >= recipeDatabase.Count) return false;

        if (!IsRecipeUnlocked(recipeIndex))
        {
            return false;
        }

        CraftingRecipeSO recipe = recipeDatabase[recipeIndex];
        if (recipe == null) return false;

        int maxExpectedAmount = recipe.outputAmount * OutputAmount;
        int availableCapacity = recipe.resultItem == null || InventoryGridManager.Instance == null
            ? maxExpectedAmount
            : InventoryGridManager.Instance.GetAvailableCapacityForItem(recipe.resultItem);

        if (availableCapacity <= 0)
        {
            return false;
        }

        if (!TrySpendRecipeMaterials(recipe)) return false;

        int finalAmount = Mathf.Min(maxExpectedAmount, availableCapacity);
        if (recipe.resultItem != null && finalAmount > 0)
        {
            InventoryGridManager.Instance?.AddItem(recipe.resultItem, finalAmount);
        }

        _isDirty = true;
        return true;
    }

    #endregion

    #region 조회 및 진행도 API

    // 특정 레시피의 정규화 진행률 반환
    public float GetRecipeNormalizedProgress(int recipeIndex)
    {
        if (recipeIndex < 0 || recipeIndex >= recipeDatabase.Count || recipeDatabase[recipeIndex] == null) return 0f;

        string rId = recipeDatabase[recipeIndex].recipeId;
        float baseTime = recipeDatabase[recipeIndex].baseCraftingTime;
        if (baseTime <= 0f) return 0f;

        float progress = _recipeProgressMap.TryGetValue(rId, out float p) ? p : 0f;
        return Mathf.Clamp01(progress / baseTime);
    }

    // 특정 레시피의 남은 소요 시간 반환
    public float GetRecipeRemainingTime(int recipeIndex)
    {
        if (recipeIndex < 0 || recipeIndex >= recipeDatabase.Count || recipeDatabase[recipeIndex] == null) return 0f;

        string rId = recipeDatabase[recipeIndex].recipeId;
        float baseTime = recipeDatabase[recipeIndex].baseCraftingTime;
        float speed = SpeedMultiplier;
        float progress = _recipeProgressMap.TryGetValue(rId, out float p) ? p : 0f;
        float remainingProgress = Mathf.Max(0f, baseTime - progress);
        return remainingProgress / speed;
    }

    // 특정 레시피의 실시간 제작 상태 반환
    public RecipeState GetRecipeState(int recipeIndex)
    {
        if (_recipeStates == null || recipeIndex < 0 || recipeIndex >= _recipeStates.Length) return RecipeState.Idle;
        return _recipeStates[recipeIndex];
    }

    #endregion

    #region 공장 업그레이드 및 디스크 저장

    // 공장 레벨업 실행
    public bool UpgradeFactory()
    {
        if (_isUpgrading)
        {
            _lastUpgradeResult = FactoryUpgradeResult.AlreadyUpgrading;
            return false;
        }

        _isUpgrading = true;
        try
        {
            _lastUpgradeResult = FactoryUpgradeProcessor.TryUpgradeFactory(ref _factoryLevel);
            if (_lastUpgradeResult == FactoryUpgradeResult.Success)
            {
                _isDirty = true;
                SaveIfDirty();
                return true;
            }
            return false;
        }
        finally
        {
            _isUpgrading = false;
        }
    }

    // 변경사항 존재 시 디스크 저장 수행
    public void SaveIfDirty()
    {
        if (_isDirty)
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.SaveGameData();
            }
            _isDirty = false;
        }
    }

    // 테스트용 재료 충전
    public void AddTestMaterials()
    {
        CurrencyManager cm = CurrencyManager.Instance;
        if (cm != null)
        {
            cm.GetGold(10000, applyModifiers: false);
            cm.GetWaveStone(50);
            cm.GetDungeonStone(50);
            cm.GetRaidStone(50);
        }
    }

    #endregion

    #region 세이브 및 로드 연동

    // 세이브 데이터에 공방 상태 기록
    private void OnSave(DataSaveEvent evt)
    {
        if (evt.saveData == null) return;
        if (evt.saveData.crafting == null)
        {
            evt.saveData.crafting = new CraftingSaveData();
        }

        EnsureArrayCapacity(recipeDatabase.Count);

        evt.saveData.crafting.factoryLevel = _factoryLevel;
        evt.saveData.crafting.isGlobalAutoEnabled = _isGlobalAutoEnabled;
        evt.saveData.crafting.queuedRecipeIndices = new List<int>(_activeRecipeQueue);

        evt.saveData.crafting.queuedRecipeIds = new List<string>();
        for (int i = 0; i < _activeRecipeQueue.Count; i++)
        {
            int idx = _activeRecipeQueue[i];
            if (idx >= 0 && idx < recipeDatabase.Count && recipeDatabase[idx] != null)
            {
                evt.saveData.crafting.queuedRecipeIds.Add(recipeDatabase[idx].recipeId);
            }
        }

        evt.saveData.crafting.progressEntries = new List<RecipeProgressSaveEntry>();
        foreach (var pair in _recipeProgressMap)
        {
            evt.saveData.crafting.progressEntries.Add(new RecipeProgressSaveEntry
            {
                recipeId = pair.Key,
                progress = pair.Value
            });
        }
    }

    // 세이브 데이터로부터 공방 상태 복원
    private void OnLoad(DataLoadEvent evt)
    {
        if (evt.saveData == null || evt.saveData.crafting == null) return;

        EnsureArrayCapacity(recipeDatabase.Count);

        _factoryLevel = Mathf.Clamp(evt.saveData.crafting.factoryLevel, 1, FactoryUpgradeProcessor.MaxFactoryLevel);
        _isGlobalAutoEnabled = evt.saveData.crafting.isGlobalAutoEnabled;

        _activeRecipeQueue.Clear();
        if (evt.saveData.crafting.queuedRecipeIds != null && evt.saveData.crafting.queuedRecipeIds.Count > 0)
        {
            foreach (string rId in evt.saveData.crafting.queuedRecipeIds)
            {
                int idx = FindRecipeIndexById(rId);
                if (idx >= 0 && !_activeRecipeQueue.Contains(idx))
                {
                    _activeRecipeQueue.Add(idx);
                }
            }
        }
        else if (evt.saveData.crafting.queuedRecipeIndices != null)
        {
            foreach (int idx in evt.saveData.crafting.queuedRecipeIndices)
            {
                if (idx >= 0 && idx < recipeDatabase.Count && !_activeRecipeQueue.Contains(idx))
                {
                    _activeRecipeQueue.Add(idx);
                }
            }
        }

        _recipeProgressMap.Clear();
        if (evt.saveData.crafting.progressEntries != null && evt.saveData.crafting.progressEntries.Count > 0)
        {
            foreach (var entry in evt.saveData.crafting.progressEntries)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.recipeId))
                {
                    _recipeProgressMap[entry.recipeId] = entry.progress;
                }
            }
        }
        else if (evt.saveData.crafting.recipeProgresses != null)
        {
            for (int i = 0; i < evt.saveData.crafting.recipeProgresses.Count; i++)
            {
                if (i < recipeDatabase.Count && recipeDatabase[i] != null)
                {
                    _recipeProgressMap[recipeDatabase[i].recipeId] = evt.saveData.crafting.recipeProgresses[i];
                }
            }
        }

        _isDirty = false;
    }

    // 공방 데이터 초기화
    private void OnReset(DataResetEvent evt)
    {
        _factoryLevel = 1;
        _isGlobalAutoEnabled = false;
        _activeRecipeQueue.Clear();
        _recipeProgressMap.Clear();
        EnsureArrayCapacity(recipeDatabase.Count);
        if (_recipeStates != null) Array.Clear(_recipeStates, 0, _recipeStates.Length);
        _isDirty = false;
    }

    // 레시피 ID로 인덱스 역탐색
    private int FindRecipeIndexById(string recipeId)
    {
        if (string.IsNullOrEmpty(recipeId) || recipeDatabase == null) return -1;
        for (int i = 0; i < recipeDatabase.Count; i++)
        {
            if (recipeDatabase[i] != null && string.Equals(recipeDatabase[i].recipeId, recipeId, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }
        return -1;
    }

    #endregion
}
