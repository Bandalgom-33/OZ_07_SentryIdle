using System;
using System.Collections.Generic;
using UnityEngine;

// 게임 재접속 시 경과 시간 기반 공방 오프라인 생산 보상 50% 감쇄 정산기
public static class OfflineCraftingCalculator
{
    // 오프라인 방치 시간 최대 캡 (24시간 = 86,400초)
    private const double MaxOfflineSeconds = 86400.0;
    // 오프라인 생산 보상 감쇄율 (50%)
    private const float OfflineEfficiency = 0.50f;

    // 오프라인 방치 생산 보상 정산 및 재화 차감/소모품 지급 실행
    public static void ProcessOfflineCrafting(
        string lastSaveTimestamp,
        int factoryLevel,
        bool isGlobalAutoEnabled,
        List<int> queuedRecipeIndices,
        CraftingRecipeData[] recipes)
    {
        if (!isGlobalAutoEnabled || queuedRecipeIndices == null || queuedRecipeIndices.Count == 0 || string.IsNullOrEmpty(lastSaveTimestamp) || recipes == null)
        {
            return;
        }

        if (!DateTime.TryParse(lastSaveTimestamp, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime lastTime))
        {
            return;
        }

        TimeSpan elapsed = DateTime.Now - lastTime;
        double totalSeconds = Math.Min(elapsed.TotalSeconds, MaxOfflineSeconds);

        if (totalSeconds < 10.0)
        {
            return;
        }

        float speedMultiplier = FactoryUpgradeProcessor.GetCraftingSpeedMultiplier(factoryLevel);
        CurrencyManager cm = CurrencyManager.Instance;
        ConsumableItemManager cim = ConsumableItemManager.Instance;

        if (cm == null || cim == null) return;

        int totalCraftedItems = 0;

        for (int q = 0; q < queuedRecipeIndices.Count; q++)
        {
            int recipeIndex = queuedRecipeIndices[q];
            if (recipeIndex < 0 || recipeIndex >= recipes.Length) continue;

            CraftingRecipeData recipe = recipes[recipeIndex];
            float effectiveTime = recipe.baseCraftingTime / speedMultiplier;
            if (effectiveTime <= 0f) effectiveTime = 1f;

            int theoreticalCount = Mathf.FloorToInt((float)(totalSeconds / effectiveTime) * OfflineEfficiency);
            if (theoreticalCount <= 0) continue;

            int affordableByGold = recipe.goldCost > 0 ? (int)(cm.Gold / recipe.goldCost) : theoreticalCount;
            int affordableByStone = theoreticalCount;

            if (recipe.requiredStoneType == CurrencyType.WaveStone && recipe.stoneCost > 0)
            {
                affordableByStone = (int)(cm.WaveStone / recipe.stoneCost);
            }
            else if (recipe.requiredStoneType == CurrencyType.StageStone && recipe.stoneCost > 0)
            {
                affordableByStone = (int)(cm.StageStone / recipe.stoneCost);
            }

            int finalCraftCount = Mathf.Min(theoreticalCount, Mathf.Min(affordableByGold, affordableByStone));

            if (finalCraftCount > 0)
            {
                long totalGold = recipe.goldCost * finalCraftCount;
                long totalStone = recipe.stoneCost * finalCraftCount;

                cm.TrySpendGold(totalGold);
                if (recipe.requiredStoneType == CurrencyType.WaveStone) cm.TrySpendWaveStone(totalStone);
                if (recipe.requiredStoneType == CurrencyType.StageStone) cm.TrySpendStageStone(totalStone);

                int outputPerCraft = FactoryUpgradeProcessor.GetCraftingOutputAmount(factoryLevel);
                int totalOutput = recipe.outputAmount * outputPerCraft * finalCraftCount;
                cim.AddConsumable(recipe.resultType, totalOutput);
                totalCraftedItems += totalOutput;

                Debug.Log($"[OfflineCraftingCalculator] 오프라인 50% 생산 정산: [{recipe.displayName}] x{totalOutput}개 획득 (소모: 골드 {totalGold}, 마석 {totalStone})");
            }
        }

        if (totalCraftedItems > 0)
        {
            Debug.Log($"[OfflineCraftingCalculator] 총 {totalCraftedItems}개의 소모품이 오프라인 방치 보상으로 지급되었습니다.");
            SaveManager.Instance.SaveGameData();
        }
    }
}
