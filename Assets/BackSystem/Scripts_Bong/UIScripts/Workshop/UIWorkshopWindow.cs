using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ScriptableObject 기반 레시피 버튼 및 제작 목록 슬롯을 동적으로 생성하는 공방 메인 팝업 UI 창 컨트롤러
public class UIWorkshopWindow : MonoBehaviour
{
    #region 인스펙터 바인딩 필드

    [Header("1. 기본 패널 및 제목")]
    [Tooltip("공방 메인 팝업 패널 오브젝트")]
    [SerializeField] private GameObject workshopPanel;

    [Tooltip("어떤 공방인지 알 수 있는 제목 설명 텍스트 (예: 소모품 공장)")]
    [SerializeField] private TMP_Text titleText;

    [Tooltip("공방 패널 닫기 [X] 버튼")]
    [SerializeField] private Button closePanelButton;

    [Tooltip("테스트용 마석/골드 지급 치트 버튼")]
    [SerializeField] private Button addTestMaterialsButton;

    [Header("2. 공장 상태 및 업그레이드")]
    [Tooltip("현재 적용 중인 공장 능력치 텍스트 (제작 속도, 제작 슬롯, 동시 제작 개수)")]
    [SerializeField] private TMP_Text factoryStatusText;

    [Tooltip("다음 업그레이드 시 변경되는 업그레이드 내용 및 비용 안내 텍스트")]
    [SerializeField] private TMP_Text factoryUpgradeBenefitText;

    [Tooltip("공방 레벨업 실행 버튼")]
    [SerializeField] private Button upgradeFactoryButton;

    [Tooltip("전역 자동 전환 토글 버튼 (켜면 등록된 레시피를 자동 생산)")]
    [SerializeField] private Toggle autoCraftToggle;

    [Header("3. 재화 갯수 텍스트 (2종 분리)")]
    [Tooltip("웨이브 마석 수량 표기용 텍스트")]
    [SerializeField] private TMP_Text waveStoneCountText;

    [Tooltip("던전 마석 수량 표기용 텍스트")]
    [SerializeField] private TMP_Text stageStoneCountText;

    [Header("4. 현재 선택된 레시피 공용 정보창 & 등록/해제 버튼")]
    [Tooltip("현재 선택된 레시피의 이름을 공용으로 표시하는 텍스트")]
    [SerializeField] private TMP_Text selectedRecipeNameText;

    [Tooltip("현재 선택된 레시피의 설명 및 자원/재화 소모량을 공용으로 표시하는 텍스트")]
    [SerializeField] private TMP_Text selectedRecipeDescriptionText;

    [Tooltip("선택한 레시피를 제작 목록에 등록/해제하는 공용 버튼")]
    [SerializeField] private Button recipeQueueActionButton;

    [Tooltip("등록/해제 버튼 내부의 텍스트 ([제작 목록에 등록] ↔ [제작 목록에서 해제])")]
    [SerializeField] private TMP_Text recipeQueueActionText;

    [Header("5. 제작 목록 동적 컨테이너 & 슬롯 프리팹")]
    [Tooltip("제작 목록 프리팹들이 자식으로 추가될 부모 구역 오브젝트 (Layout Group 등)")]
    [SerializeField] private Transform queueContainerTransform;

    [Tooltip("제작 목록에 동적으로 추가될 슬롯 프리팹 (UIWorkshopQueueItemSlot 포함)")]
    [SerializeField] private GameObject queueItemSlotPrefab;

    [Header("6. 제작 레시피 선택 목록 동적 컨테이너 & 버튼 프리팹")]
    [Tooltip("레이어/레이아웃 정리 컴포넌트(GridLayoutGroup 등)가 부착된 레시피 버튼 컨테이너 오브젝트")]
    [SerializeField] private Transform recipeButtonContainerTransform;

    [Tooltip("등록된 SO 개수만큼 생성될 레시피 선택 버튼 프리팹 (UIWorkshopRecipeSelectButton 포함)")]
    [SerializeField] private GameObject recipeSelectButtonPrefab;

    #endregion

    #region 내부 변수

    // 현재 선택된 레시피 인덱스
    private int _currentSelectedRecipeIndex = 0;

    // 동적 생성된 제작 목록 슬롯 인스턴스 목록
    private readonly List<UIWorkshopQueueItemSlot> _spawnedQueueSlots = new List<UIWorkshopQueueItemSlot>();

    // 동적 생성된 레시피 선택 버튼 인스턴스 목록
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

        if (addTestMaterialsButton != null)
        {
            addTestMaterialsButton.onClick.AddListener(OnClickAddTestMaterials);
        }

        if (upgradeFactoryButton != null)
        {
            upgradeFactoryButton.onClick.AddListener(OnClickUpgradeFactory);
        }

        if (autoCraftToggle != null)
        {
            autoCraftToggle.onValueChanged.AddListener(OnToggleAutoCraft);
        }

        if (recipeQueueActionButton != null)
        {
            recipeQueueActionButton.onClick.AddListener(OnClickQueueAction);
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

    // 슬롯별 실시간 진행도 슬라이더 갱신
    private void Update()
    {
        RefreshRealtimeProgress();
    }

    #endregion

    #region 레시피 선택 버튼 동적 생성 (SO 기반)

    // 레시피 SO 목록 순회 및 버튼 프리팹 동적 생성
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

            GameObject btnObj = Instantiate(recipeSelectButtonPrefab, recipeButtonContainerTransform);
            UIWorkshopRecipeSelectButton selectBtn = btnObj.GetComponent<UIWorkshopRecipeSelectButton>();

            if (selectBtn != null)
            {
                selectBtn.BindRecipeSO(recipeSO, i, SelectRecipe);
                _spawnedRecipeButtons.Add(selectBtn);
            }
        }
    }

    #endregion

    #region 전체 UI 갱신 메서드

    // 전체 공방 UI 새로고침
    public void RefreshAllUI()
    {
        RefreshHeaderAndCurrencies();
        RefreshFactoryUpgradePanel();
        RefreshSelectedRecipeDetail();
        RefreshQueueSlots();
        RefreshRecipeSelectButtons();
    }

    // 소모품 보유 수량 변경 이벤트 핸들러
    private void HandleConsumableCountChanged(ConsumableType type, int newCount)
    {
        CraftingController cc = CraftingController.Instance;
        if (cc == null || _currentSelectedRecipeIndex < 0 || _currentSelectedRecipeIndex >= cc.Recipes.Count) return;

        if (cc.Recipes[_currentSelectedRecipeIndex] != null && cc.Recipes[_currentSelectedRecipeIndex].resultType == type)
        {
            RefreshSelectedRecipeDetail();
        }
    }

    // 상단 헤더, 공장 상태 및 재화 수량 텍스트 갱신
    private void RefreshHeaderAndCurrencies()
    {
        CraftingController cc = CraftingController.Instance;
        CurrencyManager cm = CurrencyManager.Instance;
        if (cc == null || cm == null) return;

        if (titleText != null) titleText.text = "소모품 공장";

        if (waveStoneCountText != null)
        {
            waveStoneCountText.text = $"웨이브 마석: <color=#00FFFF>{cm.WaveStone:#,##0}</color>개";
        }

        if (stageStoneCountText != null)
        {
            stageStoneCountText.text = $"던전 마석: <color=#CC33FF>{cm.StageStone:#,##0}</color>개";
        }

        if (autoCraftToggle != null)
        {
            autoCraftToggle.SetIsOnWithoutNotify(cc.IsGlobalAutoEnabled);
        }

        if (factoryStatusText != null)
        {
            float speedBonus = (cc.SpeedMultiplier - 1.0f) * 100.0f;
            factoryStatusText.text = $"[ 공장 상태 (Lv.{cc.FactoryLevel}) ]\n" +
                                     $"• 제작 속도: <color=#00FF00>x{cc.SpeedMultiplier:F1} (+{speedBonus:F0}%)</color> | " +
                                     $"• 제작 슬롯: <color=#00FFFF>{cc.CurrentQueueCount} / {cc.MaxActiveSlots}개</color> | " +
                                     $"• 1회 제작 수량: <color=#FFD700>{cc.OutputAmount}개</color>";
        }
    }

    // 공장 업그레이드 패널 텍스트 및 버튼 활성화 갱신
    private void RefreshFactoryUpgradePanel()
    {
        CraftingController cc = CraftingController.Instance;
        CurrencyManager cm = CurrencyManager.Instance;
        if (cc == null || cm == null) return;

        int currentLevel = cc.FactoryLevel;

        if (currentLevel >= FactoryUpgradeProcessor.MaxFactoryLevel)
        {
            if (factoryUpgradeBenefitText != null)
            {
                factoryUpgradeBenefitText.text = "<color=#00FF00>공장이 최고 레벨(MAX Lv.5)에 도달하여 최대 강화가 완료되었습니다.</color>";
            }
            if (upgradeFactoryButton != null)
            {
                upgradeFactoryButton.interactable = false;
            }
            return;
        }

        int nextLevel = currentLevel + 1;
        string benefitDesc = FactoryUpgradeProcessor.GetNextLevelBenefitDescription(nextLevel);

        if (FactoryUpgradeProcessor.GetUpgradeCost(currentLevel, out long goldCost, out long waveCost, out long stageCost))
        {
            string costStr = $"{goldCost:#,##0} Gold";
            if (waveCost > 0) costStr += $" + 웨이브 마석 {waveCost}개";
            if (stageCost > 0) costStr += $" + 던전 마석 {stageCost}개";

            if (factoryUpgradeBenefitText != null)
            {
                factoryUpgradeBenefitText.text = $"[ 다음 업그레이드 (Lv.{nextLevel}) ]\n" +
                                                 $"{benefitDesc}\n" +
                                                 $"• 필요 비용: <color=#FFD700>{costStr}</color>";
            }

            bool canAfford = cm.HasGold(goldCost) && cm.HasWaveStone(waveCost) && cm.HasStageStone(stageCost);
            if (upgradeFactoryButton != null)
            {
                upgradeFactoryButton.interactable = canAfford;
            }
        }
    }

    // 선택된 레시피 공용 상세 정보창 갱신
    private void RefreshSelectedRecipeDetail()
    {
        CraftingController cc = CraftingController.Instance;
        ConsumableItemManager cim = ConsumableItemManager.Instance;
        if (cc == null || _currentSelectedRecipeIndex < 0 || _currentSelectedRecipeIndex >= cc.Recipes.Count) return;

        CraftingRecipeSO recipe = cc.Recipes[_currentSelectedRecipeIndex];
        if (recipe == null) return;

        bool isUnlocked = cc.IsRecipeUnlocked(_currentSelectedRecipeIndex);
        int reqLevel = recipe.unlockFactoryLevel;

        if (selectedRecipeNameText != null)
        {
            if (isUnlocked)
            {
                int ownedCount = cim != null ? cim.GetItemCount(recipe.resultType) : 0;
                selectedRecipeNameText.text = $"{recipe.displayName} <color=#00FFFF>(보유: {ownedCount:#,##0}개)</color>";
            }
            else
            {
                selectedRecipeNameText.text = $"{recipe.displayName} <color=#FF4444>[🔒 미해금 (공방 Lv.{reqLevel} 필요)]</color>";
            }
        }

        if (selectedRecipeDescriptionText != null)
        {
            if (isUnlocked)
            {
                string stoneName = recipe.requiredStoneType == CurrencyType.WaveStone ? "웨이브 마석" : "던전 마석";
                selectedRecipeDescriptionText.text = $"• 효과: {recipe.description}\n" +
                                                     $"• 기본 소요 시간: {recipe.baseCraftingTime:F1}초 (1회 제작: {cc.OutputAmount}개)\n" +
                                                     $"• 소모 재료: <color=#FFD700>{recipe.goldCost:#,##0} Gold</color> + <color=#00FFFF>{stoneName} {recipe.stoneCost}개</color>";
            }
            else
            {
                selectedRecipeDescriptionText.text = $"<color=#AAAAAA>• 해당 레시피는 공방을 <color=#FFFF00>Lv.{reqLevel}</color> 이상으로 업그레이드하면 해금됩니다.</color>\n" +
                                                     $"• 효과: {recipe.description}";
            }
        }

        bool isInQueue = cc.IsRecipeInQueue(_currentSelectedRecipeIndex);

        if (recipeQueueActionText != null)
        {
            if (!isUnlocked)
            {
                recipeQueueActionText.text = "<color=#888888>미해금 레시피</color>";
            }
            else
            {
                recipeQueueActionText.text = isInQueue ? "<color=#FF6666>제작 목록에서 해제</color>" : "<color=#00FF00>제작 목록에 등록</color>";
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

    // 제작 목록 큐 슬롯 프리팹 동적 생성 및 갱신
    private void RefreshQueueSlots()
    {
        CraftingController cc = CraftingController.Instance;
        if (cc == null || queueContainerTransform == null) return;

        for (int i = 0; i < _spawnedQueueSlots.Count; i++)
        {
            if (_spawnedQueueSlots[i] != null)
            {
                Destroy(_spawnedQueueSlots[i].gameObject);
            }
        }
        _spawnedQueueSlots.Clear();

        if (queueItemSlotPrefab == null) return;

        IReadOnlyList<int> queue = cc.ActiveRecipeQueue;

        for (int i = 0; i < queue.Count; i++)
        {
            int recipeIdx = queue[i];
            if (recipeIdx < 0 || recipeIdx >= cc.Recipes.Count) continue;

            CraftingRecipeSO recipeSO = cc.Recipes[recipeIdx];
            if (recipeSO == null) continue;

            GameObject slotObj = Instantiate(queueItemSlotPrefab, queueContainerTransform);
            UIWorkshopQueueItemSlot slot = slotObj.GetComponent<UIWorkshopQueueItemSlot>();

            if (slot != null)
            {
                slot.BindRecipe(recipeSO, recipeIdx, HandleSlotRemove);
                _spawnedQueueSlots.Add(slot);
            }
        }
    }

    // 슬롯 [X] 버튼 클릭 시 제작 목록 해제 처리
    private void HandleSlotRemove(int recipeIndex)
    {
        CraftingController.Instance?.RemoveRecipeFromQueue(recipeIndex);
        RefreshAllUI();
    }

    // 레시피 선택 버튼 하이라이트 및 잠금 상태 갱신
    private void RefreshRecipeSelectButtons()
    {
        CraftingController cc = CraftingController.Instance;
        if (cc == null) return;

        for (int i = 0; i < _spawnedRecipeButtons.Count; i++)
        {
            if (_spawnedRecipeButtons[i] != null)
            {
                _spawnedRecipeButtons[i].SetSelected(i == _currentSelectedRecipeIndex);

                bool isUnlocked = cc.IsRecipeUnlocked(i);
                int reqLevel = cc.GetRequiredLevel(i);
                _spawnedRecipeButtons[i].SetLocked(!isUnlocked, reqLevel);
            }
        }
    }

    // 제작 목록 슬롯별 진행도 슬라이더 실시간 갱신
    private void RefreshRealtimeProgress()
    {
        CraftingController cc = CraftingController.Instance;
        if (cc == null || _spawnedQueueSlots.Count == 0) return;

        for (int i = 0; i < _spawnedQueueSlots.Count; i++)
        {
            UIWorkshopQueueItemSlot slot = _spawnedQueueSlots[i];
            if (slot == null) continue;

            int recipeIdx = slot.BoundRecipeIndex;
            if (recipeIdx >= 0 && recipeIdx < cc.Recipes.Count)
            {
                float normalized = cc.GetRecipeNormalizedProgress(recipeIdx);
                float remaining = cc.GetRecipeRemainingTime(recipeIdx);
                CraftingController.RecipeState state = cc.GetRecipeState(recipeIdx);

                slot.UpdateProgress(normalized, remaining, state);
            }
        }
    }

    #endregion

    #region 사용자 조작 이벤트 핸들러

    // 레시피 선택 이벤트 처리
    public void SelectRecipe(int recipeIndex)
    {
        _currentSelectedRecipeIndex = recipeIndex;
        RefreshSelectedRecipeDetail();
        RefreshRecipeSelectButtons();
    }

    // 등록/해제 공용 버튼 클릭 이벤트 처리
    private void OnClickQueueAction()
    {
        CraftingController cc = CraftingController.Instance;
        if (cc == null) return;

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

    // 전역 자동 전환 토글 변경 이벤트 처리
    private void OnToggleAutoCraft(bool isOn)
    {
        CraftingController cc = CraftingController.Instance;
        if (cc != null)
        {
            cc.SetGlobalAuto(isOn);
            RefreshHeaderAndCurrencies();
        }
    }

    // 테스트용 재료 충전 버튼 클릭 이벤트 처리
    private void OnClickAddTestMaterials()
    {
        CraftingController cc = CraftingController.Instance;
        if (cc != null)
        {
            cc.AddTestMaterials();
            RefreshAllUI();
        }
    }

    // 공방 업그레이드 버튼 클릭 이벤트 처리
    private void OnClickUpgradeFactory()
    {
        CraftingController cc = CraftingController.Instance;
        if (cc != null)
        {
            bool success = cc.UpgradeFactory();
            if (success)
            {
                RefreshAllUI();
            }
        }
    }

    // 공방 팝업 패널 활성화/비활성화 토글
    public void SetPanelActive(bool active)
    {
        if (workshopPanel != null)
        {
            workshopPanel.SetActive(active);
            if (active) RefreshAllUI();
        }
    }

    // 재화 변경 수신 시 UI 동기화
    private void OnCurrencyChanged(CurrencyChangedEvent evt)
    {
        RefreshHeaderAndCurrencies();
        RefreshFactoryUpgradePanel();
    }

    #endregion
}
