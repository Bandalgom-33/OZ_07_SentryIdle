using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using UnityEngine;

// 가챠 획득 유닛의 신규 해금, 한계돌파 승급 및 풀돌 상태를 판정하는 처리기
public static class BreakthroughProcessor
{
    public const int MaxBreakthroughLimit = 6;

    // 가챠 보상 유닛의 신규/돌파/풀돌 상태 판정 및 결과 갱신 연산
    public static void ProcessGachaUnit(ref IGachaRewardItem rewardItem)
    {
        if (rewardItem == null || rewardItem.TargetUnitData == null)
        {
            return;
        }

        int numericId = UnitIdHelper.ParseUnitId(rewardItem.RewardId);
        if (numericId <= 0) return;

        UnitSaveData existingSave = CollectionDataProvider.Instance != null ? CollectionDataProvider.Instance.GetOwnedUnitSaveData(numericId) : null;

        // 1. 신규 미보유 캐릭터 획득 처리
        if (existingSave == null)
        {
            if (CollectionDataProvider.Instance != null)
            {
                CollectionDataProvider.Instance.AddOrUpdateOwnedUnit(numericId, 1, 0L, 0, 0);
            }

            rewardItem.IsOwned = false;
            rewardItem.ResultType = GachaResultType.NewUnlock;
            rewardItem.PreviousBreakthroughStep = 0;
            rewardItem.CurrentBreakthroughStep = 0;
            return;
        }

        // 2. 이미 보유 중인 캐릭터 중복 획득 처리
        rewardItem.IsOwned = true;
        int previousStep = existingSave.breakThroughStep;
        rewardItem.PreviousBreakthroughStep = previousStep;

        if (previousStep < MaxBreakthroughLimit)
        {
            existingSave.breakThroughStep++;
            rewardItem.CurrentBreakthroughStep = existingSave.breakThroughStep;
            rewardItem.ResultType = GachaResultType.Breakthrough;
        }
        else
        {
            rewardItem.CurrentBreakthroughStep = previousStep;
            rewardItem.ResultType = GachaResultType.MaxBreakthroughReached;
        }
    }

    // [하위 호환] 리스트 직접 전달 방식 가챠 유닛 처리 연산
    public static void ProcessGachaUnit(
        ref IGachaRewardItem rewardItem,
        List<UnitSaveData> ownedUnitsList,
        HashSet<string> ownedUnitIdSet)
    {
        ProcessGachaUnit(ref rewardItem);
    }
}
