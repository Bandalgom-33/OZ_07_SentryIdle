using System;
using System.Collections.Generic;
using UnityEngine;

// 스테이지 방치 보상, 공방 소모품 제작, 던전 파견 생산 등 전체 오프라인 보상 계산을 단일 총괄하는 싱글톤 매니저
public class OfflineRewardManager : SingletonBase<OfflineRewardManager>
{
    #region 상수 설정

    // 오프라인 방치 보상 최대 누적 인정 시간 (24시간 = 86,400초 캡)
    private const double MaxOfflineSeconds = 86400.0;

    // 최소 유효 오프라인 시간 (10초 미만의 재접속은 정산 생략)
    private const double MinOfflineSeconds = 10.0;

    // 스테이지 및 공방 오프라인 생산 감쇄 효율 (50%)
    private const float OfflineEfficiency = 0.50f;

    #endregion

    #region 내부 필드 및 프로퍼티

    // 최근 정산된 오프라인 보상 종합 리포트 데이터 캐시 (UI 팝업 조회용)
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

    // 세이브 데이터 로드 시 단일 진입점으로 오프라인 보상 일괄 정산 실행
    private void OnDataLoaded(DataLoadEvent evt)
    {
        if (evt.saveData == null || string.IsNullOrEmpty(evt.saveData.lastSaveTimestamp))
        {
            LastReportData = null;
            return;
        }

        // 1. 마지막 저장 시점과 현재 시간 차이 산출
        if (!DateTime.TryParse(evt.saveData.lastSaveTimestamp, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime lastSaveTime))
        {
            LastReportData = null;
            return;
        }

        TimeSpan elapsed = DateTime.UtcNow - lastSaveTime;
        double rawSeconds = Math.Max(0.0, elapsed.TotalSeconds);

        // 최대 24시간까지만 인정되는 누적 상한(Cap) 적용
        double validSeconds = Math.Min(rawSeconds, MaxOfflineSeconds);

        // UI 표시 및 이벤트 전달용 종합 리포트 DTO 상시 생성 및 캐싱
        LastReportData = new OfflineRewardReportData
        {
            ValidOfflineSeconds = validSeconds,
            FormattedDuration = FormatDurationString(validSeconds)
        };

        // 10초 미만의 초단기 재접속은 보상 정산 생략 (리포트에는 보상 없음으로 기록됨)
        if (rawSeconds >= MinOfflineSeconds)
        {
            // 2. 일반 스테이지 방치 보상 정산 (골드 50%, 웨이브 마석 1개, 경험치 0)
            ProcessStageOfflineRewards(evt.saveData.stage, validSeconds, LastReportData);

            // 3. 공방 소모품 생산 정산 (CraftingRecipeSO 기반 50% 생산)
            ProcessCraftingOfflineRewards(evt.saveData.crafting, validSeconds, LastReportData);

            // 4. 던전 파견 생산 정산 (전투력 충족 슬롯 사이클 보상)
            ProcessDungeonOfflineRewards(evt.saveData.dungeon, (float)validSeconds, LastReportData);
        }

        // 이벤트 발행
        EventBus.Publish(new OfflineRewardReportEvent(LastReportData));

        // 5. 보상이 발생한 경우 자동 저장
        if (LastReportData.HasAnyReward)
        {
            Debug.Log($"[OfflineRewardManager] 오프라인 보상 정산 완료: 시간={LastReportData.FormattedDuration}, Gold=+{LastReportData.GainedGold:N0}, WaveStone=+{LastReportData.GainedWaveStone:N0}");
            EventBus.Publish(new RequestSaveGameEvent(force: false));
        }
    }

    // 일반 스테이지 방치 보상 연산 (현재 스테이지/웨이브 고정 기준)
    private void ProcessStageOfflineRewards(StageData stage, double validSeconds, OfflineRewardReportData report)
    {
        if (stage == null || CurrencyManager.Instance == null) return;

        // 최근 5개 웨이브 평균 클리어 소요 시간 (기록이 없을 시 기본값 15.0초)
        float avgWaveDuration = stage.averageWaveDuration > 0f ? stage.averageWaveDuration : 15.0f;

        // 인정 시간 동안 진행된 환산 웨이브 횟수 산출
        int simulatedWaveCount = Mathf.FloorToInt((float)(validSeconds / avgWaveDuration));
        if (simulatedWaveCount <= 0) return;

        // 현재 스테이지/웨이브 기준 스폰 몬스터 수 (기본 3마리 x 2경로 = 6마리)
        const int monstersPerWave = 6;
        const int baseMonsterGold = 10;

        // 스테이지 스케일링 계수
        float stageScale = 1.0f + (Mathf.Max(1, stage.currentStage) - 1) * 0.20f;
        long totalMonsterBaseGold = (long)Mathf.FloorToInt(baseMonsterGold * stageScale * monstersPerWave);

        // 50% 효율 적용한 1웨이브당 지급 기본 골드 (현재 웨이브 몬스터 처치 총 기본 골드의 50%)
        long goldPerWave = (long)Mathf.FloorToInt(totalMonsterBaseGold * OfflineEfficiency);
        if (goldPerWave < 1) goldPerWave = 1;

        // 총 지급 기본 골드 및 웨이브 마석 산출
        long totalBaseGold = goldPerWave * simulatedWaveCount;
        long totalWaveStone = 1L * simulatedWaveCount; // 웨이브당 1개 지급

        // CurrencyManager를 통해 지급 (applyModifiers: true로 유저 골드 보너스 및 배율 업그레이드 수치 자동 적용, 경험치는 0)
        CurrencyManager.Instance.GetGold(totalBaseGold, applyModifiers: true);
        CurrencyManager.Instance.AddCurrency(CurrencyType.WaveStone, totalWaveStone, applyModifiers: false);

        report.GainedGold = totalBaseGold;
        report.GainedWaveStone = totalWaveStone;
    }

    // 공방 소모품 오프라인 생산 연산 (CraftingRecipeSO 단일 기준, 50% 감쇄 효율)
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

            // 50% 효율이 적용된 이론 생산 가능 횟수
            int theoreticalCount = Mathf.FloorToInt((float)(validSeconds / effectiveTime) * OfflineEfficiency);
            if (theoreticalCount <= 0) continue;

            // 유저 보유 재화(골드/다이아/3종 마석) 잔액 내에서 지불 가능한 최대 제작 횟수 검증
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

            // 인벤토리 수용 가능 수량에 따른 최대 제작 횟수 캡핑
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

    // 던전 파견 오프라인 생산 연산 (DungeonManager 연동)
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

    // 경과 시간(초)을 "00시간 00분 00초" 문자열로 포맷팅하는 헬퍼
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
