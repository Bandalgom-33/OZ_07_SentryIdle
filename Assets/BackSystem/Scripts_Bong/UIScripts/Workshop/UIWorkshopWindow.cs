using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#region 공방 UI 직렬화 보조 구조체

// 실시간 제작 슬롯 UI 매핑 구조체
[Serializable]
public struct WorkshopCraftingSlotUI
{
    [Tooltip("슬롯 루트 오브젝트")]
    public GameObject rootObject;

    [Tooltip("슬롯 활성화 컨테이너 (아이템 정보 및 진행도 바 포함)")]
    public GameObject activeContainer;

    [Tooltip("슬롯 비활성화/잠금 컨테이너 (미해금 또는 미등록 오버레이)")]
    public GameObject disabledContainer;

    [Tooltip("제작 중인 아이템 대표 아이콘 이미지")]
    public Image itemIconImage;

    [Tooltip("제작 중인 아이템 이름 텍스트")]
    public TMP_Text itemNameText;

    [Tooltip("실시간 제작 진행도 슬라이더")]
    public Slider progressSlider;
}

// 선택된 레시피의 요구 재료 슬롯 UI 매핑 구조체
[Serializable]
public struct WorkshopCostSlotUI
{
    [Tooltip("재료 슬롯 루트 오브젝트 (필요 없으면 비활성화)")]
    public GameObject slotObject;

    [Tooltip("소모 재화/마석 대표 아이콘 이미지")]
    public Image costIconImage;

    [Tooltip("소모 요구량 텍스트 (예: 500 / 100)")]
    public TMP_Text costAmountText;
}

#endregion

// ScriptableObject 기반 레시피 목록, 고정 슬롯 3개 및 재화/스탯을 렌더링하는 공방 메인 팝업 UI 창 컨트롤러
public class UIWorkshopWindow : MonoBehaviour
{
    #region 단위 포맷팅 상수

    private static readonly string[] NumFormats = { "", "K", "M", "B", "T", "Qa", "Qi" };

    #endregion

    #region 인스펙터 바인딩 필드

    [Header("1. 패널 기본 및 헤더")]
    [Tooltip("공방 메인 팝업 패널 오브젝트")]
    [SerializeField] private GameObject workshopPanel;

    [Tooltip("공방 타이틀 텍스트")]
    [SerializeField] private TMP_Text titleText;

    [Tooltip("현재 공방 레벨 표기 텍스트 (예: Lv.1)")]
    [SerializeField] private TMP_Text levelText;

    [Tooltip("공방 패널 닫기 [X] 버튼")]
    [SerializeField] private Button closePanelButton;

    [Header("2. 재화 표시 텍스트 (5종 - 1000단위 축약 포맷 적용)")]
    [Tooltip("골드 수량 표기 텍스트")]
    [SerializeField] private TMP_Text goldText;

    [Tooltip("다이아 수량 표기 텍스트")]
    [SerializeField] private TMP_Text diamondText;

    [Tooltip("웨이브 마석 수량 표기 텍스트")]
    [SerializeField] private TMP_Text waveStoneCountText;

    [Tooltip("던전 마석 수량 표기 텍스트")]
    [SerializeField] private TMP_Text dungeonStoneCountText;

    [Tooltip("레이드 마석 수량 표기 텍스트")]
    [SerializeField] private TMP_Text raidStoneCountText;

    [Header("3. 공방 스탯 표시 및 강화 (설명 + 수치 표기)")]
    [Tooltip("제작 속도 스탯 텍스트 (예: 제작 속도: x1.0)")]
    [SerializeField] private TMP_Text statSpeedText;

    [Tooltip("제작 슬롯 수 스탯 텍스트 (예: 제작 슬롯: 1/1개)")]
    [SerializeField] private TMP_Text statSlotsText;

    [Tooltip("1회 제작 수량 스탯 텍스트 (예: 1회 제작량: 1개)")]
    [SerializeField] private TMP_Text statOutputAmountText;

    [Tooltip("공방 레벨업 실행 버튼")]
    [SerializeField] private Button upgradeFactoryButton;

    [Tooltip("공방 업그레이드 비용/혜택 안내 텍스트 (선택 사항)")]
    [SerializeField] private TMP_Text factoryUpgradeBenefitText;

    [Header("4. 카테고리 탭 버튼")]
    [Tooltip("소모품 레시피 카테고리 선택 탭 버튼")]
    [SerializeField] private Button consumableTabButton;

    [Tooltip("장비 레시피 카테고리 선택 탭 버튼")]
    [SerializeField] private Button equipmentTabButton;

    [Header("5. 레시피 스크롤 목록 및 동적 생성")]
    [Tooltip("레시피 버튼들이 생성될 부모 Content Transform (GridLayoutGroup 부착)")]
    [SerializeField] private Transform recipeButtonContainerTransform;

    [Tooltip("동적 생성될 레시피 선택 버튼 프리팹 (UIWorkshopRecipeSelectButton 부착)")]
    [SerializeField] private GameObject recipeSelectButtonPrefab;

    [Header("6. 선택된 레시피 상세 정보창")]
    [Tooltip("현재 선택된 레시피의 대표 아이콘 이미지")]
    [SerializeField] private Image selectedRecipeIconImage;

    [Tooltip("현재 선택된 레시피 이름 텍스트")]
    [SerializeField] private TMP_Text selectedRecipeNameText;

    [Tooltip("현재 선택된 레시피 설명 텍스트")]
    [SerializeField] private TMP_Text selectedRecipeDescriptionText;

    [Tooltip("요구 재료/재화 슬롯 3종 배열")]
    [SerializeField] private WorkshopCostSlotUI[] costSlots = new WorkshopCostSlotUI[3];

    [Tooltip("선택한 레시피를 제작 목록에 등록/해제하는 공용 버튼")]
    [SerializeField] private Button recipeQueueActionButton;

    [Tooltip("등록/해제 버튼 내부 텍스트")]
    [SerializeField] private TMP_Text recipeQueueActionText;

    [Header("7. 실시간 자동 제작 슬롯 (3종 고정)")]
    [Tooltip("자동 제작 슬롯 3개 UI 매핑 (자동 제작1, 자동 제작2, 자동 제작3)")]
    [SerializeField] private WorkshopCraftingSlotUI[] craftingSlots = new WorkshopCraftingSlotUI[3];

    [Header("8. 재화 아이콘 스프라이트 (요구 재료 슬롯 표시용)")]
    [Tooltip("골드 기본 아이콘 스프라이트")]
    [SerializeField] private Sprite goldIconSprite;

    [Tooltip("다이아 기본 아이콘 스프라이트")]
    [SerializeField] private Sprite diamondIconSprite;

    [Tooltip("웨이브 마석 기본 아이콘 스프라이트")]
    [SerializeField] private Sprite waveStoneIconSprite;

    [Tooltip("던전 마석 기본 아이콘 스프라이트")]
    [SerializeField] private Sprite dungeonStoneIconSprite;

    [Tooltip("레이드 마석 기본 아이콘 스프라이트")]
    [SerializeField] private Sprite raidStoneIconSprite;

    #endregion

    #region 내부 변수

    private ItemCategory _currentCategory = ItemCategory.Consumable;
    private int _currentSelectedRecipeIndex = 0;
    private readonly List<UIWorkshopRecipeSelectButton> _spawnedRecipeButtons = new List<UIWorkshopRecipeSelectButton>();

    #endregion

    #region 라이프사이클

    // 버튼 이벤트 리스너 등록
    private void Awake()
    {
        if (closePanelButton != null)
        {
            closePanelButton.onClick.AddListener(() => SetPanelActive(false));
        }

        if (upgradeFactoryButton != null)
        {
            upgradeFactoryButton.onClick.AddListener(OnClickUpgradeFactory);
        }

        if (recipeQueueActionButton != null)
        {
            recipeQueueActionButton.onClick.AddListener(OnClickQueueAction);
        }

        if (consumableTabButton != null)
        {
            consumableTabButton.onClick.AddListener(() => SetCategory(ItemCategory.Consumable));
        }

        if (equipmentTabButton != null)
        {
            equipmentTabButton.onClick.AddListener(() => SetCategory(ItemCategory.Equipment));
        }
    }

    // 이벤트 구독 및 초기 UI 렌더링
    private void OnEnable()
    {
        EventBus.Subscribe<CurrencyChangedEvent>(OnCurrencyChanged);
        ConsumableItemManager.OnConsumableCountChanged += HandleConsumableCountChanged;

        InitializeRecipeButtons();
        RefreshAllUI();
    }

    // 이벤트 구독 해제
    private void OnDisable()
    {
        EventBus.Unsubscribe<CurrencyChangedEvent>(OnCurrencyChanged);
        ConsumableItemManager.OnConsumableCountChanged -= HandleConsumableCountChanged;
    }

    // 슬롯별 실시간 진행도 슬라이더 갱신 루프
    private void Update()
    {
        RefreshRealtimeProgress();
    }

    #endregion

    #region 유틸리티 메서드

    // 1000 단위 대용량 재화 포맷팅 처리
    private string FormatCurrencyNumber(double value)
    {
        if (value < 1000)
        {
            return value.ToString("N0");
        }

        int formatIndex = 0;
        while (value >= 1000 && formatIndex < NumFormats.Length - 1)
        {
            value /= 1000;
            formatIndex++;
        }

        return value.ToString("N1") + NumFormats[formatIndex];
    }

    #endregion

    #region 카테고리 탭 전환 및 레시피 버튼 동적 생성

    // 카테고리 탭 변경 처리
    public void SetCategory(ItemCategory category)
    {
        _currentCategory = category;
        InitializeRecipeButtons();

        CraftingController cc = CraftingController.Instance;
        if (cc != null && cc.Recipes != null)
        {
            int firstIdx = -1;
            for (int i = 0; i < cc.Recipes.Count; i++)
            {
                if (cc.Recipes[i] != null && cc.Recipes[i].itemCategory == _currentCategory)
                {
                    firstIdx = i;
                    break;
                }
            }

            if (firstIdx >= 0)
            {
                SelectRecipe(firstIdx);
            }
            else
            {
                _currentSelectedRecipeIndex = -1;
                RefreshSelectedRecipeDetail();
            }
        }
    }

    // 레시피 SO 목록 순회 및 현재 카테고리 버튼 동적 생성
    private void InitializeRecipeButtons()
    {
        CraftingController cc = CraftingController.Instance;
        if (cc == null || recipeButtonContainerTransform == null || recipeSelectButtonPrefab == null) return;

        for (int i = 0; i < _spawnedRecipeButtons.Count; i++)
        {
            if (_spawnedRecipeButtons[i] != null)
            {
                Destroy(_spawnedRecipeButtons[i].gameObject);
            }
        }
        _spawnedRecipeButtons.Clear();

        IReadOnlyList<CraftingRecipeSO> recipes = cc.Recipes;
        if (recipes == null) return;

        for (int i = 0; i < recipes.Count; i++)
        {
            CraftingRecipeSO recipeSO = recipes[i];
            if (recipeSO == null) continue;

            if (recipeSO.itemCategory != _currentCategory) continue;

            GameObject btnObj = Instantiate(recipeSelectButtonPrefab, recipeButtonContainerTransform);
            UIWorkshopRecipeSelectButton selectBtn = btnObj.GetComponent<UIWorkshopRecipeSelectButton>();

            if (selectBtn != null)
            {
                int index = i;
                selectBtn.BindRecipeSO(recipeSO, index, SelectRecipe);
                _spawnedRecipeButtons.Add(selectBtn);
            }
        }

        RefreshRecipeSelectButtons();
    }

    #endregion

    #region 전체 UI 갱신 메서드

    // 전체 공방 UI 새로고침
    public void RefreshAllUI()
    {
        RefreshHeaderAndCurrencies();
        RefreshFactoryStatsAndUpgrade();
        RefreshSelectedRecipeDetail();
        RefreshFixedCraftingSlots();
        RefreshRecipeSelectButtons();
    }

    // 소모품 수량 변경 이벤트 수신 처리
    private void HandleConsumableCountChanged(ConsumableType type, int newCount)
    {
        CraftingController cc = CraftingController.Instance;
        if (cc == null || _currentSelectedRecipeIndex < 0 || _currentSelectedRecipeIndex >= cc.Recipes.Count) return;

        CraftingRecipeSO currentRecipe = cc.Recipes[_currentSelectedRecipeIndex];
        if (currentRecipe != null && currentRecipe.resultItem != null && currentRecipe.resultItem.ConsumableType == type)
        {
            RefreshSelectedRecipeDetail();
        }
    }

    // 상단 헤더, 레벨 및 5종 재화 텍스트 갱신
    private void RefreshHeaderAndCurrencies()
    {
        CraftingController cc = CraftingController.Instance;
        CurrencyManager cm = CurrencyManager.Instance;
        if (cc == null || cm == null) return;

        if (titleText != null) titleText.text = "공방";
        if (levelText != null) levelText.text = $"Lv.{cc.FactoryLevel}";

        if (goldText != null) goldText.text = FormatCurrencyNumber(cm.Gold);
        if (diamondText != null) diamondText.text = FormatCurrencyNumber(cm.Diamond);
        if (waveStoneCountText != null) waveStoneCountText.text = FormatCurrencyNumber(cm.WaveStone);
        if (dungeonStoneCountText != null) dungeonStoneCountText.text = FormatCurrencyNumber(cm.DungeonStone);
        if (raidStoneCountText != null) raidStoneCountText.text = FormatCurrencyNumber(cm.RaidStone);
    }

    // 공방 3종 스탯 및 레벨업 버튼 갱신
    private void RefreshFactoryStatsAndUpgrade()
    {
        CraftingController cc = CraftingController.Instance;
        CurrencyManager cm = CurrencyManager.Instance;
        if (cc == null || cm == null) return;

        int currentLevel = cc.FactoryLevel;

        if (statSpeedText != null)
        {
            statSpeedText.text = $"제작 속도: x{cc.SpeedMultiplier:F1}";
        }

        if (statSlotsText != null)
        {
            statSlotsText.text = $"제작 슬롯: {cc.CurrentQueueCount} / {cc.MaxActiveSlots}개";
        }

        if (statOutputAmountText != null)
        {
            statOutputAmountText.text = $"1회 제작량: {cc.OutputAmount}개";
        }

        if (currentLevel >= FactoryUpgradeProcessor.MaxFactoryLevel)
        {
            if (factoryUpgradeBenefitText != null)
            {
                factoryUpgradeBenefitText.text = "<color=#00FF00>공장이 최고 레벨(MAX Lv.5)에 도달하였습니다.</color>";
            }
            if (upgradeFactoryButton != null)
            {
                upgradeFactoryButton.interactable = false;
            }
            return;
        }

        int nextLevel = currentLevel + 1;
        string benefitDesc = FactoryUpgradeProcessor.GetNextLevelBenefitDescription(nextLevel);

        if (FactoryUpgradeProcessor.GetUpgradeCost(currentLevel, out long goldCost, out long waveCost, out long dungeonCost))
        {
            if (factoryUpgradeBenefitText != null)
            {
                string costStr = $"{FormatCurrencyNumber(goldCost)} Gold";
                if (waveCost > 0) costStr += $" + 웨이브 마석 {FormatCurrencyNumber(waveCost)}개";
                if (dungeonCost > 0) costStr += $" + 던전 마석 {FormatCurrencyNumber(dungeonCost)}개";

                factoryUpgradeBenefitText.text = $"[ Lv.{nextLevel} ] {benefitDesc}\n비용: {costStr}";
            }

            bool canAfford = cm.HasGold(goldCost) && cm.HasWaveStone(waveCost) && cm.HasDungeonStone(dungeonCost);
            if (upgradeFactoryButton != null)
            {
                upgradeFactoryButton.interactable = canAfford;
            }
        }
    }

    // 선택된 레시피의 상세 정보창 갱신
    private void RefreshSelectedRecipeDetail()
    {
        CraftingController cc = CraftingController.Instance;
        ConsumableItemManager cim = ConsumableItemManager.Instance;
        if (cc == null) return;

        if (_currentSelectedRecipeIndex < 0 || _currentSelectedRecipeIndex >= cc.Recipes.Count)
        {
            ClearRecipeDetail();
            return;
        }

        CraftingRecipeSO recipe = cc.Recipes[_currentSelectedRecipeIndex];
        if (recipe == null)
        {
            ClearRecipeDetail();
            return;
        }

        bool isUnlocked = cc.IsRecipeUnlocked(_currentSelectedRecipeIndex);
        int reqLevel = recipe.unlockFactoryLevel;

        if (selectedRecipeIconImage != null)
        {
            Sprite icon = recipe.RecipeIcon;
            selectedRecipeIconImage.sprite = icon;
            selectedRecipeIconImage.enabled = (icon != null);
        }

        if (selectedRecipeNameText != null)
        {
            if (isUnlocked)
            {
                int ownedCount = 0;
                if (recipe.resultItem != null && InventoryGridManager.Instance != null)
                {
                    ownedCount = InventoryGridManager.Instance.GetItemCount(recipe.resultItem);
                }
                else if (recipe.itemCategory == ItemCategory.Consumable && recipe.resultItem != null && cim != null)
                {
                    ownedCount = cim.GetItemCount(recipe.resultItem.ConsumableType);
                }

                selectedRecipeNameText.text = $"{recipe.DisplayName} <size=70%><color=#00FFFF>({FormatCurrencyNumber(ownedCount)}개 보유)</color></size>";
            }
            else
            {
                selectedRecipeNameText.text = $"{recipe.DisplayName} <size=70%><color=#FF4444>[Lv.{reqLevel} 해금]</color></size>";
            }
        }

        if (selectedRecipeDescriptionText != null)
        {
            if (isUnlocked)
            {
                selectedRecipeDescriptionText.text = $"{recipe.Description}\n<color=#AAAAAA>기본 소요 시간: {recipe.baseCraftingTime:F1}초 (1회: {cc.OutputAmount}개)</color>";
            }
            else
            {
                selectedRecipeDescriptionText.text = $"<color=#AAAAAA>해당 레시피는 공방을 <color=#FFFF00>Lv.{reqLevel}</color> 이상으로 업그레이드하면 해금됩니다.\n{recipe.Description}</color>";
            }
        }

        BindCostSlots(recipe);

        bool isInQueue = cc.IsRecipeInQueue(_currentSelectedRecipeIndex);

        if (recipeQueueActionText != null)
        {
            if (!isUnlocked)
            {
                recipeQueueActionText.text = "미해금";
            }
            else
            {
                recipeQueueActionText.text = isInQueue ? "제작 해제" : "제작 등록";
            }
        }

        if (recipeQueueActionButton != null)
        {
            if (!isUnlocked)
            {
                recipeQueueActionButton.interactable = false;
            }
            else
            {
                bool canAction = isInQueue || (cc.CurrentQueueCount < cc.MaxActiveSlots);
                recipeQueueActionButton.interactable = canAction;
            }
        }
    }

    // 상세 정보창 초기화
    private void ClearRecipeDetail()
    {
        if (selectedRecipeIconImage != null) selectedRecipeIconImage.enabled = false;
        if (selectedRecipeNameText != null) selectedRecipeNameText.text = string.Empty;
        if (selectedRecipeDescriptionText != null) selectedRecipeDescriptionText.text = string.Empty;

        for (int i = 0; i < costSlots.Length; i++)
        {
            if (costSlots[i].slotObject != null)
            {
                costSlots[i].slotObject.SetActive(false);
            }
        }

        if (recipeQueueActionButton != null) recipeQueueActionButton.interactable = false;
        if (recipeQueueActionText != null) recipeQueueActionText.text = "-";
    }

    // 선택된 레시피의 요구 재화/마석 목록을 요구 슬롯 3종에 순차 매핑
    private void BindCostSlots(CraftingRecipeSO recipe)
    {
        if (costSlots == null || costSlots.Length == 0) return;

        CurrencyManager cm = CurrencyManager.Instance;

        List<(Sprite icon, long cost, long owned)> requirements = new List<(Sprite, long, long)>();

        if (recipe.goldCost > 0)
        {
            requirements.Add((goldIconSprite, recipe.goldCost, cm != null ? cm.Gold : 0));
        }

        if (recipe.diamondCost > 0)
        {
            requirements.Add((diamondIconSprite, recipe.diamondCost, cm != null ? cm.Diamond : 0));
        }

        if (recipe.stoneCost > 0)
        {
            Sprite stoneIcon = recipe.requiredStoneType switch
            {
                StoneType.WaveStone => waveStoneIconSprite,
                StoneType.DungeonStone => dungeonStoneIconSprite,
                StoneType.RaidStone => raidStoneIconSprite,
                _ => dungeonStoneIconSprite
            };

            long ownedStone = cm != null ? (recipe.requiredStoneType switch
            {
                StoneType.WaveStone => cm.WaveStone,
                StoneType.DungeonStone => cm.DungeonStone,
                StoneType.RaidStone => cm.RaidStone,
                _ => 0
            }) : 0;

            requirements.Add((stoneIcon, recipe.stoneCost, ownedStone));
        }

        for (int i = 0; i < costSlots.Length; i++)
        {
            if (i < requirements.Count)
            {
                var req = requirements[i];

                if (costSlots[i].slotObject != null)
                {
                    costSlots[i].slotObject.SetActive(true);
                }

                if (costSlots[i].costIconImage != null)
                {
                    costSlots[i].costIconImage.sprite = req.icon;
                    costSlots[i].costIconImage.enabled = (req.icon != null);
                }

                if (costSlots[i].costAmountText != null)
                {
                    bool hasEnough = req.owned >= req.cost;
                    string colorTag = hasEnough ? "<color=#FFFFFF>" : "<color=#FF4444>";
                    costSlots[i].costAmountText.text = $"{colorTag}{FormatCurrencyNumber(req.cost)}</color>";
                }
            }
            else
            {
                if (costSlots[i].slotObject != null)
                {
                    costSlots[i].slotObject.SetActive(false);
                }
            }
        }
    }

    // 3개 고정 제작 슬롯의 활성화/비활성화 및 아이템 정보 렌더링
    private void RefreshFixedCraftingSlots()
    {
        CraftingController cc = CraftingController.Instance;
        if (cc == null || craftingSlots == null) return;

        int maxSlots = cc.MaxActiveSlots;
        IReadOnlyList<int> queue = cc.ActiveRecipeQueue;

        for (int i = 0; i < craftingSlots.Length; i++)
        {
            WorkshopCraftingSlotUI slot = craftingSlots[i];
            if (slot.rootObject == null) continue;

            bool isSlotUnlocked = (i < maxSlots);
            bool hasAssignedRecipe = (i < queue.Count);

            if (isSlotUnlocked && hasAssignedRecipe)
            {
                if (slot.activeContainer != null) slot.activeContainer.SetActive(true);
                if (slot.disabledContainer != null) slot.disabledContainer.SetActive(false);

                int recipeIdx = queue[i];
                if (recipeIdx >= 0 && recipeIdx < cc.Recipes.Count)
                {
                    CraftingRecipeSO recipe = cc.Recipes[recipeIdx];
                    if (recipe != null)
                    {
                        if (slot.itemIconImage != null)
                        {
                            slot.itemIconImage.sprite = recipe.RecipeIcon;
                            slot.itemIconImage.enabled = (recipe.RecipeIcon != null);
                        }

                        if (slot.itemNameText != null)
                        {
                            slot.itemNameText.text = recipe.DisplayName;
                        }
                    }
                }
            }
            else
            {
                if (slot.activeContainer != null) slot.activeContainer.SetActive(false);
                if (slot.disabledContainer != null) slot.disabledContainer.SetActive(true);
            }
        }
    }

    // 레시피 선택 버튼 하이라이트 및 잠금 상태 갱신
    private void RefreshRecipeSelectButtons()
    {
        CraftingController cc = CraftingController.Instance;
        if (cc == null) return;

        for (int i = 0; i < _spawnedRecipeButtons.Count; i++)
        {
            UIWorkshopRecipeSelectButton btn = _spawnedRecipeButtons[i];
            if (btn != null)
            {
                int rIdx = btn.RecipeIndex;
                btn.SetSelected(rIdx == _currentSelectedRecipeIndex);

                bool isUnlocked = cc.IsRecipeUnlocked(rIdx);
                int reqLevel = cc.GetRequiredLevel(rIdx);
                btn.SetLocked(!isUnlocked, reqLevel);
            }
        }
    }

    // 3개 고정 슬롯의 실시간 진행률 슬라이더 갱신
    private void RefreshRealtimeProgress()
    {
        CraftingController cc = CraftingController.Instance;
        if (cc == null || craftingSlots == null) return;

        IReadOnlyList<int> queue = cc.ActiveRecipeQueue;

        for (int i = 0; i < craftingSlots.Length; i++)
        {
            if (i >= queue.Count) continue;

            WorkshopCraftingSlotUI slot = craftingSlots[i];
            if (slot.progressSlider == null) continue;

            int recipeIdx = queue[i];
            if (recipeIdx >= 0 && recipeIdx < cc.Recipes.Count)
            {
                float progress = cc.GetRecipeNormalizedProgress(recipeIdx);
                slot.progressSlider.value = progress;
            }
        }
    }

    #endregion

    #region 사용자 조작 이벤트 핸들러

    // 레시피 선택 처리
    public void SelectRecipe(int recipeIndex)
    {
        _currentSelectedRecipeIndex = recipeIndex;
        RefreshSelectedRecipeDetail();
        RefreshRecipeSelectButtons();
    }

    // 등록/해제 공용 버튼 클릭 처리
    private void OnClickQueueAction()
    {
        CraftingController cc = CraftingController.Instance;
        if (cc == null || _currentSelectedRecipeIndex < 0) return;

        if (cc.IsRecipeInQueue(_currentSelectedRecipeIndex))
        {
            cc.RemoveRecipeFromQueue(_currentSelectedRecipeIndex);
        }
        else
        {
            cc.AddRecipeToQueue(_currentSelectedRecipeIndex);
        }

        RefreshAllUI();
    }

    // 공방 레벨업 버튼 클릭 처리
    private void OnClickUpgradeFactory()
    {
        CraftingController cc = CraftingController.Instance;
        if (cc != null)
        {
            bool success = cc.UpgradeFactory();
            if (success)
            {
                Debug.Log($"[UIWorkshopWindow] 공방 강화 성공! 현재 Lv.{cc.FactoryLevel}");
            }
            RefreshAllUI();
        }
    }

    // 공방 팝업 패널 활성화 및 비활성화
    public void SetPanelActive(bool active)
    {
        if (workshopPanel != null)
        {
            workshopPanel.SetActive(active);
            if (active)
            {
                RefreshAllUI();
            }
            else
            {
                CraftingController.Instance?.SaveIfDirty();
            }
        }
    }

    // 재화 변경 수신 시 전체 수치 및 버튼 상태 갱신
    private void OnCurrencyChanged(CurrencyChangedEvent evt)
    {
        RefreshHeaderAndCurrencies();
        RefreshFactoryStatsAndUpgrade();
        RefreshSelectedRecipeDetail();
    }

    #endregion
}
