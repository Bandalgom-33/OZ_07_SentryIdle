using System;
using System.Collections.Generic;
using UnityEngine;

// 스테이지 방치 보상, 공방 소모품 제작, 던전 파견 생산 등 전체 오프라인 보상 계산 총괄 매니저
public class OfflineRewardManager : SingletonBase<OfflineRewardManager>
{
    #region 상수 설정

    private const double MaxOfflineSeconds = 86400.0;
    private const double MinOfflineSeconds = 10.0;
    private const float OfflineEfficiency = 0.50f;

    #endregion

    #region 내부 필드 및 프로퍼티

    public OfflineRewardReportData LastReportData { get; private set; }

    #endregion

    #region 라이프 사이클

    // 이벤트 버스 구독 등록
    private void OnEnable()
    {
        EventBus.Subscribe<DataLoadEvent>(OnDataLoaded);
    }

    // 이벤트 버스 구독 해제
    private void OnDisable()
    {
        EventBus.Unsubscribe<DataLoadEvent>(OnDataLoaded);
    }

    #endregion

    #region 통합 오프라인 보상 정산 연산

    // 세이브 데이터 로드 이벤트 수신 및 오프라인 보상 정산 처리
    private void OnDataLoaded(DataLoadEvent evt)
    {
        if (evt.saveData == null || string.IsNullOrEmpty(evt.saveData.lastSaveTimestamp))
        {
            LastReportData = null;
            return;
        }

        if (!DateTime.TryParse(evt.saveData.lastSaveTimestamp, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime lastSaveTime))
        {
            LastReportData = null;
            return;
        }

        TimeSpan elapsed = DateTime.UtcNow - lastSaveTime;
        double rawSeconds = Math.Max(0.0, elapsed.TotalSeconds);
        double validSeconds = Math.Min(rawSeconds, MaxOfflineSeconds);

        LastReportData = new OfflineRewardReportData
        {
            ValidOfflineSeconds = validSeconds,
            FormattedDuration = FormatDurationString(validSeconds)
        };

        if (rawSeconds >= MinOfflineSeconds)
        {
            ProcessStageOfflineRewards(evt.saveData.stage, validSeconds, LastReportData);
            ProcessCraftingOfflineRewards(evt.saveData.crafting, validSeconds, LastReportData);
            ProcessDungeonOfflineRewards(evt.saveData.dungeon, (float)validSeconds, LastReportData);
        }

        EventBus.Publish(new OfflineRewardReportEvent(LastReportData));

        if (LastReportData.HasAnyReward)
        {
            Debug.Log($"[OfflineRewardManager] 오프라인 보상 정산 완료: 시간={LastReportData.FormattedDuration}, Gold=+{LastReportData.GainedGold:N0}, WaveStone=+{LastReportData.GainedWaveStone:N0}");
            EventBus.Publish(new RequestSaveGameEvent(force: false));
        }
    }

    // 일반 스테이지 방치 보상 계산
    private void ProcessStageOfflineRewards(StageData stage, double validSeconds, OfflineRewardReportData report)
    {
        if (stage == null || CurrencyManager.Instance == null) return;

        float avgWaveDuration = stage.averageWaveDuration > 0f ? stage.averageWaveDuration : 15.0f;

        int simulatedWaveCount = Mathf.FloorToInt((float)(validSeconds / avgWaveDuration));
        if (simulatedWaveCount <= 0) return;

        const int monstersPerWave = 6;
        const int baseMonsterGold = 10;

        float stageScale = 1.0f + (Mathf.Max(1, stage.currentStage) - 1) * 0.20f;
        long totalMonsterBaseGold = (long)Mathf.FloorToInt(baseMonsterGold * stageScale * monstersPerWave);

        long goldPerWave = (long)Mathf.FloorToInt(totalMonsterBaseGold * OfflineEfficiency);
        if (goldPerWave < 1) goldPerWave = 1;

        long totalBaseGold = goldPerWave * simulatedWaveCount;
        long totalWaveStone = 1L * simulatedWaveCount;

        CurrencyManager.Instance.GetGold(totalBaseGold, applyModifiers: true);
        CurrencyManager.Instance.AddCurrency(CurrencyType.WaveStone, totalWaveStone, applyModifiers: false);

        report.GainedGold = totalBaseGold;
        report.GainedWaveStone = totalWaveStone;
    }

    // 공방 소모품 오프라인 생산 계산
    private void ProcessCraftingOfflineRewards(CraftingSaveData crafting, double validSeconds, OfflineRewardReportData report)
    {
        if (crafting == null || !crafting.isGlobalAutoEnabled || crafting.queuedRecipeIndices == null || crafting.queuedRecipeIndices.Count == 0)
        {
            return;
        }

        CraftingController controller = CraftingController.Instance;
        CurrencyManager cm = CurrencyManager.Instance;
        ConsumableItemManager cim = ConsumableItemManager.Instance;

        if (controller == null || cm == null) return;

        List<CraftingRecipeSO> recipes = controller.RecipeDatabase;
        if (recipes == null || recipes.Count == 0) return;

        float speedMultiplier = FactoryUpgradeProcessor.GetCraftingSpeedMultiplier(crafting.factoryLevel);

        for (int q = 0; q < crafting.queuedRecipeIndices.Count; q++)
        {
            int recipeIndex = crafting.queuedRecipeIndices[q];
            if (recipeIndex < 0 || recipeIndex >= recipes.Count) continue;

            CraftingRecipeSO recipe = recipes[recipeIndex];
            if (recipe == null || recipe.resultItem == null) continue;

            float effectiveTime = recipe.baseCraftingTime / speedMultiplier;
            if (effectiveTime <= 0f) effectiveTime = 1f;

            int theoreticalCount = Mathf.FloorToInt((float)(validSeconds / effectiveTime) * OfflineEfficiency);
            if (theoreticalCount <= 0) continue;

            int affordableByGold = recipe.goldCost > 0 ? (int)(cm.Gold / recipe.goldCost) : theoreticalCount;
            int affordableByDiamond = recipe.diamondCost > 0 ? (int)(cm.Diamond / recipe.diamondCost) : theoreticalCount;
            int affordableByStone = theoreticalCount;

            if (recipe.requiredStoneType == StoneType.WaveStone && recipe.stoneCost > 0)
            {
                affordableByStone = (int)(cm.WaveStone / recipe.stoneCost);
            }
            else if (recipe.requiredStoneType == StoneType.DungeonStone && recipe.stoneCost > 0)
            {
                affordableByStone = (int)(cm.DungeonStone / recipe.stoneCost);
            }
            else if (recipe.requiredStoneType == StoneType.RaidStone && recipe.stoneCost > 0)
            {
                affordableByStone = (int)(cm.RaidStone / recipe.stoneCost);
            }

            int outputPerCraft = FactoryUpgradeProcessor.GetCraftingOutputAmount(crafting.factoryLevel);
            int unitOutput = recipe.outputAmount * outputPerCraft;
            int maxAvailableCapacity = InventoryGridManager.Instance != null
                ? InventoryGridManager.Instance.GetAvailableCapacityForItem(recipe.resultItem)
                : int.MaxValue;

            int affordableByCapacity = unitOutput > 0 ? maxAvailableCapacity / unitOutput : 0;
            int finalCraftCount = Mathf.Min(theoreticalCount, Mathf.Min(affordableByGold, Mathf.Min(affordableByDiamond, Mathf.Min(affordableByStone, affordableByCapacity))));

            if (finalCraftCount > 0)
            {
                if (recipe.goldCost > 0) cm.TrySpendGold(recipe.goldCost * finalCraftCount);
                if (recipe.diamondCost > 0) cm.TrySpendDiamond(recipe.diamondCost * finalCraftCount);

                long totalStoneCost = recipe.stoneCost * finalCraftCount;
                if (recipe.requiredStoneType == StoneType.WaveStone) cm.TrySpendWaveStone(totalStoneCost);
                else if (recipe.requiredStoneType == StoneType.DungeonStone) cm.TrySpendDungeonStone(totalStoneCost);
                else if (recipe.requiredStoneType == StoneType.RaidStone) cm.TrySpendRaidStone(totalStoneCost);

                int totalOutput = unitOutput * finalCraftCount;
                InventoryGridManager.Instance?.AddItem(recipe.resultItem, totalOutput);

                if (recipe.itemCategory == ItemCategory.Consumable)
                {
                    ConsumableType cType = recipe.resultItem.ConsumableType;
                    if (!report.GainedConsumables.ContainsKey(cType))
                    {
                        report.GainedConsumables[cType] = 0;
                    }
                    report.GainedConsumables[cType] += totalOutput;
                }
            }
        }
    }

    // 던전 파견 오프라인 생산 계산
    private void ProcessDungeonOfflineRewards(DungeonSaveData dungeon, float validSeconds, OfflineRewardReportData report)
    {
        DungeonManager dm = DungeonManager.Instance;
        if (dm == null || dungeon == null || dungeon.dungeonSlots == null) return;

        List<DungeonDataSO> dList = dm.DungeonList;
        if (dList == null) return;

        const int maxOfflineDungeonCycles = 100;

        for (int i = 0; i < dList.Count; i++)
        {
            DungeonDataSO dataSO = dList[i];
            if (dataSO == null) continue;

            string dId = dataSO.DungeonId;
            int totalPower = dm.GetDungeonTotalPower(dId);
            bool isRunning = totalPower >= dataSO.RequiredMinCombatPower;

            if (isRunning)
            {
                float prevTimer = dm.GetDungeonCycleTimer(dId);
                float totalAccumulatedTime = prevTimer + validSeconds;
                float cycleDuration = dataSO.BaseCycleSeconds;

                int theoreticalCycles = (int)(totalAccumulatedTime / cycleDuration);
                int offlineCycles = Mathf.Min(maxOfflineDungeonCycles, theoreticalCycles);
                float remainingTimer = totalAccumulatedTime % cycleDuration;

                if (offlineCycles > 0)
                {
                    dm.GrantDungeonReward(dataSO, totalPower, offlineCycles);
                    report.DungeonCompletedCycles[dId] = offlineCycles;
                }

                dm.SetDungeonCycleTimer(dId, remainingTimer);
            }
        }
    }

    // 경과 시간 포맷팅 문자열 반환
    private string FormatDurationString(double totalSeconds)
    {
        TimeSpan ts = TimeSpan.FromSeconds(totalSeconds);
        if (ts.TotalHours >= 1.0)
        {
            return $"{(int)ts.TotalHours}시간 {ts.Minutes}분 {ts.Seconds}초";
        }
        else if (ts.TotalMinutes >= 1.0)
        {
            return $"{ts.Minutes}분 {ts.Seconds}초";
        }
        else
        {
            return $"{ts.Seconds}초";
        }
    }

    #endregion
}
