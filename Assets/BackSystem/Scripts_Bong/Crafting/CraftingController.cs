using System;
using System.Collections.Generic;
using UnityEngine;

// 6종 소모품 레시피 SO 목록 관리, 제작 큐 루프, 공장 레벨업 및 세이브 연동 총괄 컨트롤러
public class CraftingController : SingletonBase<CraftingController>
{
    #region 레시피 상태 열거형

    public enum RecipeState
    {
        // 비활성화 대기 상태 (제작 목록 미등록 또는 전역 AUTO OFF)
        Idle,
        // 정상 제작 진행 중 상태
        Crafting,
        // 재료 부족으로 인한 일시 정지 대기 상태
        Hold
    }

    #endregion

    #region 인스펙터 바인딩 필드 (SO 데이터베이스)

    [Header("레시피 ScriptableObject 데이터베이스")]
    [Tooltip("공방에서 제작 가능한 CraftingRecipeSO 에셋 목록")]
    [SerializeField] private List<CraftingRecipeSO> recipeDatabase = new List<CraftingRecipeSO>();

    #endregion

    #region 내부 변수 및 프로퍼티

    // 현재 공장 레벨 (Lv.1 ~ Lv.5)
    private int _factoryLevel = 1;
    // 전역 자동 전환 토글 활성화 여부
    private bool _isGlobalAutoEnabled = false;
    // 현재 제작 목록에 등록된 레시피 인덱스 리스트 (Queue)
    private readonly List<int> _activeRecipeQueue = new List<int>();
    // 레시피별 현재 진행 시간 (초)
    private readonly float[] _recipeProgresses = new float[16];
    // 레시피별 실시간 제작 상태
    private readonly RecipeState[] _recipeStates = new RecipeState[16];

    public int FactoryLevel => _factoryLevel;
    public int MaxActiveSlots => FactoryUpgradeProcessor.GetMaxActiveSlots(_factoryLevel);
    public int OutputAmount => FactoryUpgradeProcessor.GetCraftingOutputAmount(_factoryLevel);
    public float SpeedMultiplier => FactoryUpgradeProcessor.GetCraftingSpeedMultiplier(_factoryLevel);
    public bool IsGlobalAutoEnabled => _isGlobalAutoEnabled;
    public IReadOnlyList<CraftingRecipeSO> Recipes => recipeDatabase;
    public IReadOnlyList<int> ActiveRecipeQueue => _activeRecipeQueue;

    // 현재 등록된 제작 목록 개수
    public int CurrentQueueCount => _activeRecipeQueue.Count;

    // 특정 레시피의 공방 레벨 해금 여부 판정
    public bool IsRecipeUnlocked(int recipeIndex)
    {
        if (recipeIndex < 0 || recipeIndex >= recipeDatabase.Count || recipeDatabase[recipeIndex] == null)
        {
            return false;
        }
        return recipeDatabase[recipeIndex].unlockFactoryLevel <= _factoryLevel;
    }

    // 특정 레시피의 해금 필요 최소 공장 레벨 반환
    public int GetRequiredLevel(int recipeIndex)
    {
        if (recipeIndex >= 0 && recipeIndex < recipeDatabase.Count && recipeDatabase[recipeIndex] != null)
        {
            return recipeDatabase[recipeIndex].unlockFactoryLevel;
        }
        return 1;
    }

    #endregion

    #region 라이프사이클 및 초기화

    // 컨트롤러 싱글톤 초기화 및 SO 데이터베이스 로드
    protected override void Awake()
    {
        base.Awake();
        InitializeRecipeDatabase();
    }

    // SO 데이터베이스 자동 적재 및 미생성 시 런타임 인스턴스 생성
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
    }

    // 기본 6종 레시피 런타임 인스턴스 생성
    private void CreateDefaultRuntimeRecipes()
    {
        recipeDatabase = new List<CraftingRecipeSO>()
        {
            CreateRecipeInstance("RECIPE_HP_01", ConsumableType.HealthPotion_Low, "하급 체력포션", "전체 아군 HP 25% 회복", 4.0f, 100, CurrencyType.WaveStone, 1, 1),
            CreateRecipeInstance("RECIPE_EXP_01", ConsumableType.ExpBook_Low, "초급 경험치책", "지정 유닛 +100 EXP (10마리분)", 5.0f, 200, CurrencyType.StageStone, 1, 2),
            CreateRecipeInstance("RECIPE_HP_02", ConsumableType.HealthPotion_Mid, "중급 체력포션", "전체 아군 HP 50% 회복", 8.0f, 300, CurrencyType.WaveStone, 3, 3),
            CreateRecipeInstance("RECIPE_EXP_02", ConsumableType.ExpBook_Mid, "중급 경험치책", "지정 유닛 +1,000 EXP (100마리분)", 10.0f, 600, CurrencyType.StageStone, 2, 4),
            CreateRecipeInstance("RECIPE_HP_03", ConsumableType.HealthPotion_High, "상급 체력포션", "전체 아군 HP 100% 완전 회복", 15.0f, 1000, CurrencyType.WaveStone, 10, 5),
            CreateRecipeInstance("RECIPE_EXP_03", ConsumableType.ExpBook_High, "고급 경험치책", "지정 유닛 +10,000 EXP (1000마리분)", 20.0f, 2000, CurrencyType.StageStone, 5, 5)
        };
    }

    // 단일 레시피 SO 인스턴스 생성 헬퍼
    private CraftingRecipeSO CreateRecipeInstance(
        string id, ConsumableType result, string name, string desc,
        float time, long gold, CurrencyType stoneType, long stoneAmount, int reqLevel)
    {
        CraftingRecipeSO so = ScriptableObject.CreateInstance<CraftingRecipeSO>();
        so.recipeId = id;
        so.resultType = result;
        so.displayName = name;
        so.description = desc;
        so.baseCraftingTime = time;
        so.goldCost = gold;
        so.requiredStoneType = stoneType;
        so.stoneCost = stoneAmount;
        so.outputAmount = 1;
        so.unlockFactoryLevel = reqLevel;
        return so;
    }

    // 이벤트 구독 등록
    private void OnEnable()
    {
        EventBus.Subscribe<DataSaveEvent>(OnSave);
        EventBus.Subscribe<DataLoadEvent>(OnLoad);
        EventBus.Subscribe<DataResetEvent>(OnReset);
        EventBus.Subscribe<CurrencyChangedEvent>(OnCurrencyChanged);
    }

    // 이벤트 구독 해제
    private void OnDisable()
    {
        EventBus.Unsubscribe<DataSaveEvent>(OnSave);
        EventBus.Unsubscribe<DataLoadEvent>(OnLoad);
        EventBus.Unsubscribe<DataResetEvent>(OnReset);
        EventBus.Unsubscribe<CurrencyChangedEvent>(OnCurrencyChanged);
    }

    // 런타임 제작 틱 루프 갱신
    private void Update()
    {
        UpdateCraftingProgress(Time.deltaTime);
    }

    #endregion

    #region 자동 제작 틱 루프 (Hold & Resume 알고리즘)

    // 매 프레임 등록된 레시피들의 생산 진행도 갱신 및 완료 처리
    private void UpdateCraftingProgress(float deltaTime)
    {
        CurrencyManager cm = CurrencyManager.Instance;
        ConsumableItemManager cim = ConsumableItemManager.Instance;
        if (cm == null || cim == null) return;

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

            bool hasGold = cm.HasGold(recipe.goldCost);
            bool hasStone = recipe.requiredStoneType switch
            {
                CurrencyType.WaveStone => cm.HasWaveStone(recipe.stoneCost),
                CurrencyType.StageStone => cm.HasStageStone(recipe.stoneCost),
                _ => false
            };

            if (!hasGold || !hasStone)
            {
                _recipeStates[recipeIndex] = RecipeState.Hold;
                continue;
            }

            _recipeStates[recipeIndex] = RecipeState.Crafting;
            _recipeProgresses[recipeIndex] += deltaTime * speed;

            if (_recipeProgresses[recipeIndex] >= recipe.baseCraftingTime)
            {
                cm.TrySpendGold(recipe.goldCost);
                if (recipe.requiredStoneType == CurrencyType.WaveStone) cm.TrySpendWaveStone(recipe.stoneCost);
                if (recipe.requiredStoneType == CurrencyType.StageStone) cm.TrySpendStageStone(recipe.stoneCost);

                int finalAmount = recipe.outputAmount * currentOutput;
                cim.AddConsumable(recipe.resultType, finalAmount);

                _recipeProgresses[recipeIndex] = 0f;
                SaveManager.Instance.SaveGameData();

                Debug.Log($"[CraftingController] 자동 생산 완료: [{recipe.displayName}] x{finalAmount}개 제작 (공장 배율: {currentOutput}개)");
            }
        }
    }

    // 재화 변동 시 Hold 상태 해제 유도
    private void OnCurrencyChanged(CurrencyChangedEvent evt)
    {
    }

    #endregion

    #region 제작 목록(큐) 등록 및 해제 API

    // 특정 레시피의 제작 목록 등록 여부 반환
    public bool IsRecipeInQueue(int recipeIndex)
    {
        return _activeRecipeQueue.Contains(recipeIndex);
    }

    // 선택한 레시피의 제작 목록 등록 처리
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
            Debug.LogWarning($"[CraftingController] 제작 슬롯 한도({MaxActiveSlots}개)가 가득 찼습니다. 공장을 업그레이드하세요.");
            return false;
        }

        _activeRecipeQueue.Add(recipeIndex);
        SaveManager.Instance.SaveGameData();
        Debug.Log($"[CraftingController] 제작 목록 등록 완료: [{recipeDatabase[recipeIndex].displayName}] (현재 등록 수: {_activeRecipeQueue.Count}/{MaxActiveSlots})");
        return true;
    }

    // 선택한 레시피의 제작 목록 해제 처리
    public bool RemoveRecipeFromQueue(int recipeIndex)
    {
        if (!_activeRecipeQueue.Contains(recipeIndex))
        {
            return false;
        }

        _activeRecipeQueue.Remove(recipeIndex);
        if (recipeIndex < _recipeProgresses.Length) _recipeProgresses[recipeIndex] = 0f;
        if (recipeIndex < _recipeStates.Length) _recipeStates[recipeIndex] = RecipeState.Idle;

        SaveManager.Instance.SaveGameData();
        Debug.Log($"[CraftingController] 제작 목록 해제 완료: [{recipeDatabase[recipeIndex].displayName}] (현재 등록 수: {_activeRecipeQueue.Count}/{MaxActiveSlots})");
        return true;
    }

    // 전역 자동 전환 스위치 설정
    public void SetGlobalAuto(bool enabled)
    {
        _isGlobalAutoEnabled = enabled;
        SaveManager.Instance.SaveGameData();
        Debug.Log($"[CraftingController] 전역 자동 전환 스위치: {(enabled ? "ON" : "OFF")}");
    }

    // [하위 호환] 레시피 토글 상태 조회
    public bool IsRecipeToggleOn(int recipeIndex)
    {
        return IsRecipeInQueue(recipeIndex);
    }

    // [하위 호환] 레시피 토글 상태 제어
    public bool SetRecipeToggle(int recipeIndex, bool isEnabled)
    {
        return isEnabled ? AddRecipeToQueue(recipeIndex) : RemoveRecipeFromQueue(recipeIndex);
    }

    // 레시피 1회 수동 즉시 제작 실행
    public bool CraftOnce(int recipeIndex)
    {
        if (recipeIndex < 0 || recipeIndex >= recipeDatabase.Count) return false;

        if (!IsRecipeUnlocked(recipeIndex))
        {
            int reqLevel = GetRequiredLevel(recipeIndex);
            Debug.LogWarning($"[CraftingController] 해당 레시피는 공장 Lv.{reqLevel} 이상에서 해금됩니다.");
            return false;
        }

        CraftingRecipeSO recipe = recipeDatabase[recipeIndex];
        CurrencyManager cm = CurrencyManager.Instance;
        ConsumableItemManager cim = ConsumableItemManager.Instance;

        if (cm == null || cim == null) return false;

        if (!cm.HasGold(recipe.goldCost)) return false;
        if (recipe.requiredStoneType == CurrencyType.WaveStone && !cm.HasWaveStone(recipe.stoneCost)) return false;
        if (recipe.requiredStoneType == CurrencyType.StageStone && !cm.HasStageStone(recipe.stoneCost)) return false;

        cm.TrySpendGold(recipe.goldCost);
        if (recipe.requiredStoneType == CurrencyType.WaveStone) cm.TrySpendWaveStone(recipe.stoneCost);
        if (recipe.requiredStoneType == CurrencyType.StageStone) cm.TrySpendStageStone(recipe.stoneCost);

        int finalAmount = recipe.outputAmount * OutputAmount;
        cim.AddConsumable(recipe.resultType, finalAmount);
        SaveManager.Instance.SaveGameData();

        Debug.Log($"[CraftingController] 1회 즉시 제작 완료: [{recipe.displayName}] x{finalAmount}개");
        return true;
    }

    #endregion

    #region 조회 및 진행도 API

    // 특정 레시피의 진행률(0.0 ~ 1.0) 반환
    public float GetRecipeNormalizedProgress(int recipeIndex)
    {
        if (recipeIndex < 0 || recipeIndex >= recipeDatabase.Count || recipeDatabase[recipeIndex] == null) return 0f;
        float baseTime = recipeDatabase[recipeIndex].baseCraftingTime;
        if (baseTime <= 0f) return 0f;
        return Mathf.Clamp01(_recipeProgresses[recipeIndex] / baseTime);
    }

    // 특정 레시피의 남은 소요 시간(초) 반환
    public float GetRecipeRemainingTime(int recipeIndex)
    {
        if (recipeIndex < 0 || recipeIndex >= recipeDatabase.Count || recipeDatabase[recipeIndex] == null) return 0f;
        float baseTime = recipeDatabase[recipeIndex].baseCraftingTime;
        float speed = SpeedMultiplier;
        float remainingProgress = Mathf.Max(0f, baseTime - _recipeProgresses[recipeIndex]);
        return remainingProgress / speed;
    }

    // 특정 레시피의 실시간 제작 상태 반환
    public RecipeState GetRecipeState(int recipeIndex)
    {
        if (recipeIndex < 0 || recipeIndex >= _recipeStates.Length) return RecipeState.Idle;
        return _recipeStates[recipeIndex];
    }

    #endregion

    #region 공장 업그레이드 및 테스트 충전

    // 공장 레벨업 실행
    public bool UpgradeFactory()
    {
        bool success = FactoryUpgradeProcessor.TryUpgradeFactory(ref _factoryLevel);
        if (success)
        {
            SaveManager.Instance.SaveGameData();
        }
        return success;
    }

    // 테스트용 재료 즉시 충전 치트 실행
    public void AddTestMaterials()
    {
        CurrencyManager cm = CurrencyManager.Instance;
        if (cm != null)
        {
            cm.GetGold(10000, applyModifiers: false);
            cm.GetWaveStone(50);
            cm.GetStageStone(50);
            Debug.Log("[CraftingController] [CHEAT] 테스트 재료 충전 완료! (+10,000 Gold, +50 WaveStone, +50 StageStone)");
        }
    }

    #endregion

    #region 세이브 / 로드 및 오프라인 보상 연동

    // 세이브 데이터에 공방 상태 저장
    private void OnSave(DataSaveEvent evt)
    {
        if (evt.saveData == null) return;

        evt.saveData.crafting.factoryLevel = _factoryLevel;
        evt.saveData.crafting.isGlobalAutoEnabled = _isGlobalAutoEnabled;
        evt.saveData.crafting.queuedRecipeIndices = new List<int>(_activeRecipeQueue);
        Array.Copy(_recipeProgresses, evt.saveData.crafting.recipeProgresses, Mathf.Min(_recipeProgresses.Length, evt.saveData.crafting.recipeProgresses.Length));
    }

    // 세이브 데이터로부터 공방 상태 복원
    private void OnLoad(DataLoadEvent evt)
    {
        if (evt.saveData == null || evt.saveData.crafting == null) return;

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

        if (evt.saveData.crafting.recipeProgresses != null)
        {
            int len = Mathf.Min(_recipeProgresses.Length, evt.saveData.crafting.recipeProgresses.Length);
            Array.Copy(evt.saveData.crafting.recipeProgresses, _recipeProgresses, len);
        }
    }

    // 공방 상태 데이터 초기화
    private void OnReset(DataResetEvent evt)
    {
        _factoryLevel = 1;
        _isGlobalAutoEnabled = false;
        _activeRecipeQueue.Clear();
        Array.Clear(_recipeProgresses, 0, _recipeProgresses.Length);
        Array.Clear(_recipeStates, 0, _recipeStates.Length);
    }

    #endregion
}
