using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using UnityEngine;

// 가챠 획득 유닛의 신규 해금, 한계돌파 승급 및 풀돌 상태를 판정하고 세이브 데이터를 갱신하는 처리기
public class BreakthroughProcessor
{
    // 최대 한계 돌파 허용 상한 단계 (0단계: 기본 해금 ~ 6단계: 최대 풀돌)
    public const int MaxBreakthroughLimit = 6;

    // 가챠로 뽑힌 개별 유닛 보상 아이템에 대해 신규/돌파/풀돌 상태를 처리하고 어댑터를 갱신하는 핵심 메서드
    public static void ProcessGachaUnit(
        ref IGachaRewardItem rewardItem,
        List<UnitSaveData> ownedUnitsList,
        HashSet<string> ownedUnitIdSet)
    {
        if (rewardItem == null || rewardItem.TargetUnitData == null || ownedUnitsList == null)
        {
            return;
        }

        string unitIdStr = rewardItem.RewardId;
        int numericId = ParseUnitId(unitIdStr);

        // 세이브 데이터 목록에서 해당 유닛의 기존 저장 데이터 검색
        UnitSaveData existingSave = FindSaveData(ownedUnitsList, numericId, unitIdStr);

        // 1. 신규 미보유 캐릭터 획득 처리 (NewUnlock)
        if (existingSave == null)
        {
            // 신규 유닛 세이브 데이터 인스턴스 생성 (레벨 1, 돌파 0단계)
            UnitSaveData newUnitSave = new UnitSaveData
            {
                unitId = numericId,
                level = 1,
                currentExp = 0L,
                breakThroughStep = 0,
                fragmentCount = 0
            };

            ownedUnitsList.Add(newUnitSave);
            if (ownedUnitIdSet != null)
            {
                ownedUnitIdSet.Add(unitIdStr);
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

        // 2-1. 아직 최대 돌파(6단계)에 도달하지 않은 경우: 한계돌파 1단계 상승
        if (previousStep < MaxBreakthroughLimit)
        {
            existingSave.breakThroughStep++;
            rewardItem.CurrentBreakthroughStep = existingSave.breakThroughStep;
            rewardItem.ResultType = GachaResultType.Breakthrough;
        }
        // 2-2. 이미 6단계 풀돌에 도달한 경우: 변환 아이템 없이 최대 돌파 완료 상태 유지
        else
        {
            rewardItem.CurrentBreakthroughStep = previousStep;
            rewardItem.ResultType = GachaResultType.MaxBreakthroughReached;
        }
    }

    // 세이브 데이터 리스트에서 숫자 ID 또는 문자열 해시값으로 유닛을 검색하는 헬퍼 메서드
    private static UnitSaveData FindSaveData(List<UnitSaveData> list, int numericId, string unitIdStr)
    {
        for (int i = 0; i < list.Count; i++)
        {
            UnitSaveData item = list[i];
            if (item == null) continue;

            if (item.unitId == numericId)
            {
                return item;
            }
        }

        return null;
    }

    // 유닛 ID 문자열(예: "UNIT_0001")에서 정수 번호(1)를 추출하는 유틸리티
    public static int ParseUnitId(string unitIdStr)
    {
        if (string.IsNullOrEmpty(unitIdStr)) return 0;

        if (int.TryParse(unitIdStr.Replace("UNIT_", ""), out int parsedId))
        {
            return parsedId;
        }

        return unitIdStr.GetHashCode();
    }
}
