using System;
using System.Collections.Generic;
using UnityEngine;

// 6종 소모품 레시피 SO 목록 관리, 제작 큐 루프, 공장 레벨업 및 세이브 연동을 총괄하는 컨트롤러
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

    #region 인스펙터 바인딩 필드 (SO 데이터베이스)

    [Header("--- 레시피 데이터베이스 ---")]
    [Tooltip("공방에서 제작 가능한 CraftingRecipeSO 에셋 목록")]
    [SerializeField] private List<CraftingRecipeSO> recipeDatabase = new List<CraftingRecipeSO>();

    #endregion

    #region 내부 변수 및 프로퍼티

    private int _factoryLevel = 1;
    private bool _isGlobalAutoEnabled = false;
    private readonly List<int> _activeRecipeQueue = new List<int>();
    private float[] _recipeProgresses = Array.Empty<float>();
    private RecipeState[] _recipeStates = Array.Empty<RecipeState>();

    public int FactoryLevel => _factoryLevel;
    public int MaxActiveSlots => FactoryUpgradeProcessor.GetMaxActiveSlots(_factoryLevel);
    public int OutputAmount => FactoryUpgradeProcessor.GetCraftingOutputAmount(_factoryLevel);
    public float SpeedMultiplier => FactoryUpgradeProcessor.GetCraftingSpeedMultiplier(_factoryLevel);
    public bool IsGlobalAutoEnabled => _isGlobalAutoEnabled;
    public List<CraftingRecipeSO> RecipeDatabase => recipeDatabase;
    public IReadOnlyList<CraftingRecipeSO> Recipes => recipeDatabase;
    public IReadOnlyList<int> ActiveRecipeQueue => _activeRecipeQueue;
    public int CurrentQueueCount => _activeRecipeQueue.Count;

    // 특정 레시피 해금 여부 판정 연산
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

    // 레시피 데이터베이스 크기에 맞춘 진행도/상태 배열 동적 확장 및 보장
    private void EnsureArrayCapacity(int requiredCount)
    {
        int count = Mathf.Max(requiredCount, recipeDatabase != null ? recipeDatabase.Count : 0);
        if (_recipeProgresses == null || _recipeProgresses.Length < count)
        {
            float[] newProgresses = new float[count];
            if (_recipeProgresses != null && _recipeProgresses.Length > 0)
            {
                Array.Copy(_recipeProgresses, newProgresses, _recipeProgresses.Length);
            }
            _recipeProgresses = newProgresses;
        }

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

    // 컨트롤러 싱글톤 초기화 및 SO 데이터베이스 로드
    protected override void Awake()
    {
        base.Awake();
        InitializeRecipeDatabase();
        EnsureArrayCapacity(recipeDatabase.Count);
    }

    // 레시피 데이터베이스 자동 로드 및 런타임 생성 처리
    private void InitializeRecipeDatabase()
    {
        if (recipeDatabase == null || recipeDatabase.Count == 0)
        {
            CraftingRecipeSO[] loadedSOs = Resources.LoadAll<CraftingRecipeSO>("Crafting/Recipes");
            if (loadedSOs != null && loadedSOs.Length > 0)
            {
                recipeDatabase.AddRange(loadedSOs);
            }
            else
            {
                CreateDefaultRuntimeRecipes();
            }
        }
        EnsureArrayCapacity(recipeDatabase.Count);
    }

    // 기본 소모품 6종 및 레이드 마석 장비 레시피 4종 런타임 인스턴스 생성 처리
    private void CreateDefaultRuntimeRecipes()
    {
        recipeDatabase = new List<CraftingRecipeSO>()
        {
            // 1. 소모품 레시피 6종 (웨이브/던전 마석 소모)
            CreateRecipeInstance("RECIPE_HP_01", "Potion_Low", StoneType.WaveStone, 1, 4.0f, 1, 100, 0),
            CreateRecipeInstance("RECIPE_EXP_01", "ExpBook_Low", StoneType.DungeonStone, 1, 5.0f, 2, 200, 0),
            CreateRecipeInstance("RECIPE_HP_02", "Potion_Mid", StoneType.WaveStone, 3, 8.0f, 3, 300, 0),
            CreateRecipeInstance("RECIPE_EXP_02", "ExpBook_Mid", StoneType.DungeonStone, 2, 10.0f, 4, 600, 0),
            CreateRecipeInstance("RECIPE_HP_03", "Potion_High", StoneType.WaveStone, 10, 15.0f, 5, 1000, 0),
            CreateRecipeInstance("RECIPE_EXP_03", "ExpBook_High", StoneType.DungeonStone, 5, 20.0f, 5, 2000, 0),

            // 2. 레이드 마석 장비 제작 레시피 4종 (무기, 갑옷, 투구, 장신구)
            CreateEquipmentRecipeInstance("RECIPE_EQUIP_WEAPON_01", "Weapon_01", 5, 6.0f, 1, 500, 0),
            CreateEquipmentRecipeInstance("RECIPE_EQUIP_ARMOR_01", "Armor_01", 8, 8.0f, 2, 800, 0),
            CreateEquipmentRecipeInstance("RECIPE_EQUIP_HEAD_01", "Head_01", 6, 6.0f, 2, 600, 0),
            CreateEquipmentRecipeInstance("RECIPE_EQUIP_ACC_01", "Accessory_01", 10, 10.0f, 3, 1200, 0)
        };
    }

    // 단일 소모품 레시피 SO 인스턴스 생성 헬퍼
    private CraftingRecipeSO CreateRecipeInstance(
        string id, string itemId, StoneType stoneType, long stoneAmount, float time, int reqLevel, long gold = 0, long diamond = 0)
    {
        CraftingRecipeSO so = ScriptableObject.CreateInstance<CraftingRecipeSO>();
        so.recipeId = id;
        so.itemCategory = ItemCategory.Consumable;
        so.baseCraftingTime = time;
        so.goldCost = gold;
        so.diamondCost = diamond;
        so.requiredStoneType = stoneType;
        so.stoneCost = stoneAmount;
        so.outputAmount = 1;
        so.unlockFactoryLevel = reqLevel;

        ItemDataSO itemSO = InventoryGridManager.Instance != null ? InventoryGridManager.Instance.GetItemById(itemId) : Resources.Load<ItemDataSO>($"ItemDataSo/{itemId}");
        if (itemSO == null) itemSO = Resources.Load<ItemDataSO>(itemId);
        so.resultItem = itemSO;

        return so;
    }

    // 단일 장비 레시피 SO 인스턴스 생성 헬퍼 (레이드 마석 소모)
    private CraftingRecipeSO CreateEquipmentRecipeInstance(
        string id, string equipmentItemId, long raidStoneCost, float time, int reqLevel, long gold = 0, long diamond = 0)
    {
        CraftingRecipeSO so = ScriptableObject.CreateInstance<CraftingRecipeSO>();
        so.recipeId = id;
        so.itemCategory = ItemCategory.Equipment;
        so.baseCraftingTime = time;
        so.goldCost = gold;
        so.diamondCost = diamond;
        so.requiredStoneType = StoneType.RaidStone;
        so.stoneCost = raidStoneCost;
        so.outputAmount = 1;
        so.unlockFactoryLevel = reqLevel;

        ItemDataSO equipSO = InventoryGridManager.Instance != null ? InventoryGridManager.Instance.GetItemById(equipmentItemId) : Resources.Load<ItemDataSO>($"ItemDataSo/{equipmentItemId}");
        if (equipSO == null) equipSO = Resources.Load<ItemDataSO>(equipmentItemId);
        so.resultItem = equipSO;

        return so;
    }

    // 이벤트 구독 등록
    private void OnEnable()
    {
        EventBus.Subscribe<DataSaveEvent>(OnSave);
        EventBus.Subscribe<DataLoadEvent>(OnLoad);
        EventBus.Subscribe<DataResetEvent>(OnReset);
    }

    // 이벤트 구독 해제
    private void OnDisable()
    {
        EventBus.Unsubscribe<DataSaveEvent>(OnSave);
        EventBus.Unsubscribe<DataLoadEvent>(OnLoad);
        EventBus.Unsubscribe<DataResetEvent>(OnReset);
    }

    // 런타임 제작 틱 루프 갱신
    private void Update()
    {
        UpdateCraftingProgress(Time.deltaTime);
    }

    #endregion

    #region 자동 제작 틱 루프 (Hold & Resume 알고리즘)

    // 제작 진행도 갱신 및 완료 처리 연산
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

            // 1. 인벤토리 내 기존 스택 잔여 공간 및 빈 슬롯 수용 가능 용량 계산
            int maxExpectedAmount = recipe.outputAmount * currentOutput;
            int availableCapacity = recipe.resultItem == null || InventoryGridManager.Instance == null
                ? maxExpectedAmount
                : InventoryGridManager.Instance.GetAvailableCapacityForItem(recipe.resultItem);

            // 가방 수용 공간이 전혀 없거나(0개), 요구 소모 재료가 부족하면 Hold 전환
            if (availableCapacity <= 0 || !HasCraftingMaterials(recipe))
            {
                _recipeStates[recipeIndex] = RecipeState.Hold;
                continue;
            }

            _recipeStates[recipeIndex] = RecipeState.Crafting;
            _recipeProgresses[recipeIndex] += deltaTime * speed;

            if (_recipeProgresses[recipeIndex] >= recipe.baseCraftingTime)
            {
                if (TrySpendRecipeMaterials(recipe))
                {
                    // 남은 인벤토리 공간과 1회 생산량 중 수용 가능한 실제 수량 산정
                    int finalAmount = Mathf.Min(maxExpectedAmount, availableCapacity);

                    // 제작 완료 아이템 인벤토리 자동 입고
                    if (recipe.resultItem != null && finalAmount > 0)
                    {
                        InventoryGridManager.Instance?.AddItem(recipe.resultItem, finalAmount);
                        Debug.Log($"[CraftingController] 제작 완료: {recipe.DisplayName} x{finalAmount} 인벤토리 입고");
                    }

                    _recipeProgresses[recipeIndex] = 0f;
                    SaveManager.Instance?.SaveGameData();
                }
            }
        }
    }

    // 레시피 제작 소모 재화(골드/다이아/3종 마석) 보유 여부 검증
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

    // 레시피 제작 소모 재화(골드/다이아/3종 마석) 차감 처리
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

    #region 제작 목록(큐) 등록 및 해제 API

    // 레시피 제작 목록 등록 여부 확인
    public bool IsRecipeInQueue(int recipeIndex)
    {
        return _activeRecipeQueue.Contains(recipeIndex);
    }

    // 선택 레시피 제작 목록 등록 연산
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
        SaveManager.Instance.SaveGameData();
        return true;
    }

    // 선택 레시피 제작 목록 해제 연산
    public bool RemoveRecipeFromQueue(int recipeIndex)
    {
        if (!_activeRecipeQueue.Contains(recipeIndex))
        {
            return false;
        }

        _activeRecipeQueue.Remove(recipeIndex);
        if (_recipeProgresses != null && recipeIndex >= 0 && recipeIndex < _recipeProgresses.Length)
        {
            _recipeProgresses[recipeIndex] = 0f;
        }
        if (_recipeStates != null && recipeIndex >= 0 && recipeIndex < _recipeStates.Length)
        {
            _recipeStates[recipeIndex] = RecipeState.Idle;
        }

        SaveManager.Instance.SaveGameData();
        return true;
    }

    // 전역 자동 전환 스위치 설정
    public void SetGlobalAuto(bool enabled)
    {
        _isGlobalAutoEnabled = enabled;
        SaveManager.Instance.SaveGameData();
    }

    // [하위 호환] 레시피 토글 상태 조회
    public bool IsRecipeToggleOn(int recipeIndex) => IsRecipeInQueue(recipeIndex);

    // [하위 호환] 레시피 토글 상태 제어
    public bool SetRecipeToggle(int recipeIndex, bool isEnabled) => isEnabled ? AddRecipeToQueue(recipeIndex) : RemoveRecipeFromQueue(recipeIndex);

    // 레시피 1회 수동 즉시 제작 연산
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

        SaveManager.Instance?.SaveGameData();
        return true;
    }

    #endregion

    #region 조회 및 진행도 API

    // 특정 레시피의 정규화 진행률 반환
    public float GetRecipeNormalizedProgress(int recipeIndex)
    {
        if (recipeIndex < 0 || recipeIndex >= recipeDatabase.Count || recipeDatabase[recipeIndex] == null) return 0f;
        if (_recipeProgresses == null || recipeIndex >= _recipeProgresses.Length) return 0f;

        float baseTime = recipeDatabase[recipeIndex].baseCraftingTime;
        if (baseTime <= 0f) return 0f;
        return Mathf.Clamp01(_recipeProgresses[recipeIndex] / baseTime);
    }

    // 특정 레시피의 남은 소요 시간 반환
    public float GetRecipeRemainingTime(int recipeIndex)
    {
        if (recipeIndex < 0 || recipeIndex >= recipeDatabase.Count || recipeDatabase[recipeIndex] == null) return 0f;
        if (_recipeProgresses == null || recipeIndex >= _recipeProgresses.Length) return 0f;

        float baseTime = recipeDatabase[recipeIndex].baseCraftingTime;
        float speed = SpeedMultiplier;
        float remainingProgress = Mathf.Max(0f, baseTime - _recipeProgresses[recipeIndex]);
        return remainingProgress / speed;
    }

    // 특정 레시피의 실시간 제작 상태 반환
    public RecipeState GetRecipeState(int recipeIndex)
    {
        if (_recipeStates == null || recipeIndex < 0 || recipeIndex >= _recipeStates.Length) return RecipeState.Idle;
        return _recipeStates[recipeIndex];
    }

    #endregion

    #region 공장 업그레이드 및 테스트 충전

    // 공장 레벨업 연산
    public bool UpgradeFactory()
    {
        bool success = FactoryUpgradeProcessor.TryUpgradeFactory(ref _factoryLevel);
        if (success)
        {
            SaveManager.Instance.SaveGameData();
        }
        return success;
    }

    // 테스트용 재료 충전 연산
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

    #region 세이브 / 로드 연동

    // 세이브 데이터 저장 처리
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

        if (evt.saveData.crafting.recipeProgresses == null)
        {
            evt.saveData.crafting.recipeProgresses = new List<float>();
        }
        evt.saveData.crafting.recipeProgresses.Clear();
        if (_recipeProgresses != null)
        {
            evt.saveData.crafting.recipeProgresses.AddRange(_recipeProgresses);
        }
    }

    // 세이브 데이터 로드 처리
    private void OnLoad(DataLoadEvent evt)
    {
        if (evt.saveData == null || evt.saveData.crafting == null) return;

        EnsureArrayCapacity(recipeDatabase.Count);

        _factoryLevel = Mathf.Clamp(evt.saveData.crafting.factoryLevel, 1, FactoryUpgradeProcessor.MaxFactoryLevel);
        _isGlobalAutoEnabled = evt.saveData.crafting.isGlobalAutoEnabled;

        _activeRecipeQueue.Clear();
        if (evt.saveData.crafting.queuedRecipeIndices != null)
        {
            foreach (int idx in evt.saveData.crafting.queuedRecipeIndices)
            {
                if (idx >= 0 && idx < recipeDatabase.Count && !_activeRecipeQueue.Contains(idx))
                {
                    _activeRecipeQueue.Add(idx);
                }
            }
        }

        if (evt.saveData.crafting.recipeProgresses != null && _recipeProgresses != null)
        {
            int count = Mathf.Min(_recipeProgresses.Length, evt.saveData.crafting.recipeProgresses.Count);
            for (int i = 0; i < count; i++)
            {
                _recipeProgresses[i] = evt.saveData.crafting.recipeProgresses[i];
            }
        }
    }

    // 공방 상태 초기화 처리
    private void OnReset(DataResetEvent evt)
    {
        _factoryLevel = 1;
        _isGlobalAutoEnabled = false;
        _activeRecipeQueue.Clear();
        EnsureArrayCapacity(recipeDatabase.Count);
        if (_recipeProgresses != null) Array.Clear(_recipeProgresses, 0, _recipeProgresses.Length);
        if (_recipeStates != null) Array.Clear(_recipeStates, 0, _recipeStates.Length);
    }

    #endregion
}
