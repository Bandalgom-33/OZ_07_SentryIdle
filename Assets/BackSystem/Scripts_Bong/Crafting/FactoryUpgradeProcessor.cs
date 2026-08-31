using System;
using UnityEngine;

public enum FactoryUpgradeResult
{
    Success,
    MaxLevelReached,
    InvalidLevel,
    NotEnoughCurrency,
    AlreadyUpgrading
}

public static class FactoryUpgradeProcessor
{
    public const int MaxFactoryLevel = 5;

    // 레벨별 최대 동시 활성화 슬롯 수 반환
    public static int GetMaxActiveSlots(int level)
    {
        return level switch
        {
            1 => 1,
            2 => 2,
            3 => 2,
            4 => 2,
            5 => 3,
            _ => 1
        };
    }

    // 레벨별 1회 제작 수량 반환
    public static int GetCraftingOutputAmount(int level)
    {
        return level switch
        {
            1 => 1,
            2 => 1,
            3 => 2,
            4 => 2,
            5 => 3,
            _ => 1
        };
    }

    // 레벨별 생산 속도 배율 반환
    public static float GetCraftingSpeedMultiplier(int level)
    {
        return level switch
        {
            1 => 1.0f,
            2 => 1.0f,
            3 => 1.0f,
            4 => 1.5f,
            5 => 2.0f,
            _ => 1.0f
        };
    }

    // 레벨별 해금되는 총 레시피 개수 반환
    public static int GetUnlockedRecipeCount(int level)
    {
        return level switch
        {
            1 => 1,
            2 => 2,
            3 => 3,
            4 => 4,
            5 => 6,
            _ => 1
        };
    }

    // 특정 레시피의 공방 레벨별 해금 여부 확인
    public static bool IsRecipeUnlocked(int level, int recipeIndex)
    {
        int unlockedCount = GetUnlockedRecipeCount(level);
        return recipeIndex >= 0 && recipeIndex < unlockedCount;
    }

    // 특정 레시피의 해금 필요 최소 공방 레벨 반환
    public static int GetRequiredFactoryLevelForRecipe(int recipeIndex)
    {
        return recipeIndex switch
        {
            0 => 1,
            1 => 2,
            2 => 3,
            3 => 4,
            4 => 5,
            5 => 5,
            _ => 1
        };
    }

    // 다음 레벨 업그레이드 필요 재화 비용 조회
    public static bool GetUpgradeCost(int currentLevel, out long goldCost, out long waveStoneCost, out long dungeonStoneCost)
    {
        goldCost = 0;
        waveStoneCost = 0;
        dungeonStoneCost = 0;

        if (currentLevel < 1 || currentLevel >= MaxFactoryLevel)
        {
            return false;
        }

        switch (currentLevel)
        {
            case 1:
                goldCost = 1000;
                waveStoneCost = 5;
                dungeonStoneCost = 0;
                return true;
            case 2:
                goldCost = 3000;
                waveStoneCost = 0;
                dungeonStoneCost = 5;
                return true;
            case 3:
                goldCost = 10000;
                waveStoneCost = 15;
                dungeonStoneCost = 10;
                return true;
            case 4:
                goldCost = 30000;
                waveStoneCost = 30;
                dungeonStoneCost = 30;
                return true;
            default:
                return false;
        }
    }

    // 다음 레벨 특화 혜택 안내 문구 생성
    public static string GetNextLevelBenefitDescription(int nextLevel)
    {
        return nextLevel switch
        {
            2 => "• 특화: <color=#00FFFF>제작 슬롯 +1개 확장 (총 2개)</color> & [초급 경험치책] 레시피 해금",
            3 => "• 특화: <color=#FFD700>1회 제작 수량 2배 증가 (1회 2개)</color> & [중급 체력포션] 레시피 해금",
            4 => "• 특화: <color=#00FF00>제작 속도 +50% 증가 (x1.5배)</color> & [중급 경험치책] 레시피 해금",
            5 => "• 특화: <color=#FF00FF>전부 대폭 강화 (슬롯 3개, 수량 3개, 속도 2배, 전체 레시피 해금)</color>",
            _ => string.Empty
        };
    }

    // 공장 레벨업 실행 및 재화 후불 차감 처리
    public static FactoryUpgradeResult TryUpgradeFactory(ref int currentLevel)
    {
        if (currentLevel < 1)
        {
            Debug.LogWarning($"[FactoryUpgradeProcessor] 비정상적인 공장 레벨({currentLevel})입니다.");
            return FactoryUpgradeResult.InvalidLevel;
        }

        if (currentLevel >= MaxFactoryLevel)
        {
            Debug.LogWarning("[FactoryUpgradeProcessor] 공장이 이미 최고 레벨(Lv.5)입니다.");
            return FactoryUpgradeResult.MaxLevelReached;
        }

        if (!GetUpgradeCost(currentLevel, out long goldCost, out long waveCost, out long stageCost))
        {
            return FactoryUpgradeResult.InvalidLevel;
        }

        CurrencyManager cm = CurrencyManager.Instance;
        if (cm == null) return FactoryUpgradeResult.NotEnoughCurrency;

        if (!cm.HasGold(goldCost) || !cm.HasWaveStone(waveCost) || !cm.HasStageStone(stageCost))
        {
            Debug.LogWarning("[FactoryUpgradeProcessor] 공장 업그레이드에 필요한 재화가 부족합니다.");
            return FactoryUpgradeResult.NotEnoughCurrency;
        }

        if (goldCost > 0 && !cm.TrySpendGold(goldCost)) return FactoryUpgradeResult.NotEnoughCurrency;
        if (waveCost > 0 && !cm.TrySpendWaveStone(waveCost)) return FactoryUpgradeResult.NotEnoughCurrency;
        if (stageCost > 0 && !cm.TrySpendStageStone(stageCost)) return FactoryUpgradeResult.NotEnoughCurrency;

        currentLevel++;
        Debug.Log($"[FactoryUpgradeProcessor] 공장 레벨업 성공! 현재 레벨: Lv.{currentLevel}");

        return FactoryUpgradeResult.Success;
    }
}
