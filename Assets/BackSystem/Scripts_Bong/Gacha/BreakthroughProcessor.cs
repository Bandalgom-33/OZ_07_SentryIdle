using System.Collections.Generic;
using EndlessGuard.Unit.Data;

public static class BreakthroughProcessor
{
    public const int MaxBreakthroughLimit = 6;

    // 가챠 보상 유닛의 돌파 및 상태 판정
    public static void ProcessGachaUnit(GachaRewardItem rewardItem, Dictionary<int, UnitSaveData> batchContext)
    {
        if (rewardItem == null || rewardItem.TargetUnitData == null)
        {
            return;
        }

        int unitId = rewardItem.UnitId;
        if (unitId <= 0) return;

        UnitSaveData currentStatus = null;
        if (batchContext != null && batchContext.TryGetValue(unitId, out UnitSaveData batchUnit))
        {
            currentStatus = batchUnit;
        }
        else if (CollectionDataProvider.Instance != null)
        {
            UnitSaveData saved = CollectionDataProvider.Instance.GetOwnedUnitSaveData(unitId);
            if (saved != null)
            {
                currentStatus = new UnitSaveData
                {
                    unitId = saved.unitId,
                    level = saved.level,
                    currentExp = saved.currentExp,
                    breakThroughStep = saved.breakThroughStep,
                    fragmentCount = saved.fragmentCount
                };
            }
        }

        if (currentStatus == null)
        {
            rewardItem.IsOwned = false;
            rewardItem.ResultType = GachaResultType.NewUnlock;
            rewardItem.PreviousBreakthroughStep = 0;
            rewardItem.CurrentBreakthroughStep = 0;

            if (batchContext != null)
            {
                batchContext[unitId] = new UnitSaveData
                {
                    unitId = unitId,
                    level = 1,
                    currentExp = 0L,
                    breakThroughStep = 0,
                    fragmentCount = 0
                };
            }
            return;
        }

        rewardItem.IsOwned = true;
        int previousStep = currentStatus.breakThroughStep;
        rewardItem.PreviousBreakthroughStep = previousStep;

        if (previousStep < MaxBreakthroughLimit)
        {
            int nextStep = previousStep + 1;
            rewardItem.CurrentBreakthroughStep = nextStep;
            rewardItem.ResultType = GachaResultType.Breakthrough;

            currentStatus.breakThroughStep = nextStep;
        }
        else
        {
            rewardItem.CurrentBreakthroughStep = previousStep;
            rewardItem.ResultType = GachaResultType.MaxBreakthroughReached;
        }

        if (batchContext != null)
        {
            batchContext[unitId] = currentStatus;
        }
    }
}
