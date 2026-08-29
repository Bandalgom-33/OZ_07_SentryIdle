using System.Collections.Generic;

// 가챠 획득 유닛의 신규 해금, 한계돌파 승급 및 풀돌 상태를 판정하는 순수 연산 처리기
// CollectionDataProvider 등의 외부 저장소를 직접 변경하지 않고(Side-Effect 제거),
// 10연차 배치 컨텍스트(batchContext)를 기반으로 실시간 누적 돌파 단계를 정확히 판정함
public static class BreakthroughProcessor
{
    // 캐릭터 최대 한계돌파 단계 상한선 (0돌파 ~ 6돌파)
    public const int MaxBreakthroughLimit = 6;

    // 가챠 보상 유닛의 신규/돌파/풀돌 상태 판정 및 DTO 결과 갱신 연산
    public static void ProcessGachaUnit(GachaRewardItem rewardItem, Dictionary<int, UnitSaveData> batchContext)
    {
        if (rewardItem == null || rewardItem.TargetUnitData == null)
        {
            return;
        }

        int unitId = rewardItem.UnitId;
        if (unitId <= 0) return;

        // 1. 이번 연차 배치 컨텍스트에서 이전 당첨 이력 확인, 없으면 CollectionDataProvider 저장소에서 기존 보유 정보 조회
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
                // 원본 세이브 객체에 부수 효과가 즉시 가해지지 않도록 배치 컨텍스트 복제본 생성
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

        // 2. 신규 미보유 캐릭터 최초 획득 처리 (해금)
        if (currentStatus == null)
        {
            rewardItem.IsOwned = false;
            rewardItem.ResultType = GachaResultType.NewUnlock;
            rewardItem.PreviousBreakthroughStep = 0;
            rewardItem.CurrentBreakthroughStep = 0;

            // 이번 10연차 배치 컨텍스트에 0돌파 상태로 신규 등록 (다음 루프에서 중복 등장 시 1돌파로 정상 계산되도록 보장)
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

        // 3. 이미 보유 중인 캐릭터 중복 획득 처리 (돌파 단계 상승 또는 풀돌 유지)
        rewardItem.IsOwned = true;
        int previousStep = currentStatus.breakThroughStep;
        rewardItem.PreviousBreakthroughStep = previousStep;

        if (previousStep < MaxBreakthroughLimit)
        {
            int nextStep = previousStep + 1;
            rewardItem.CurrentBreakthroughStep = nextStep;
            rewardItem.ResultType = GachaResultType.Breakthrough;

            // 배치 컨텍스트의 돌파 단계를 1 증가시켜 다음 중복 시 실시간 반영
            currentStatus.breakThroughStep = nextStep;
        }
        else
        {
            rewardItem.CurrentBreakthroughStep = previousStep;
            rewardItem.ResultType = GachaResultType.MaxBreakthroughReached;
        }

        // 배치 컨텍스트에 최신 돌파 상태 기록 유지
        if (batchContext != null)
        {
            batchContext[unitId] = currentStatus;
        }
    }
}
