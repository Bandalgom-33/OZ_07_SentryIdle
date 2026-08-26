using System;
using UnityEngine;

// 공방에서 제작 가능한 6종 소모품 아이템 분류 열거형
public enum ConsumableType
{
    // 하급 체력포션 (전체 아군 HP 25% 회복)
    HealthPotion_Low = 0,
    // 중급 체력포션 (전체 아군 HP 50% 회복)
    HealthPotion_Mid = 1,
    // 상급 체력포션 (전체 아군 HP 100% 완전 회복)
    HealthPotion_High = 2,

    // 초급 경험치책 (지정 유닛 +100 EXP)
    ExpBook_Low = 3,
    // 중급 경험치책 (지정 유닛 +1,000 EXP)
    ExpBook_Mid = 4,
    // 고급 경험치책 (지정 유닛 +10,000 EXP)
    ExpBook_High = 5
}

// 개별 조합 레시피의 정적 메타데이터 정의 구조체
[Serializable]
public struct CraftingRecipeData
{
    // 레시피 슬롯 고유 번호
    public int recipeIndex;
    // 결과 생성 소모품 종류
    public ConsumableType resultType;
    // 아이템 표시 이름
    public string displayName;
    // 아이템 상세 효과 설명
    public string description;
    // 기본 생산 소요 시간 (초)
    public float baseCraftingTime;
    // 1회 생산 시 소모되는 골드 수량
    public long goldCost;
    // 1회 생산 시 소모되는 마석 재화 타입
    public CurrencyType requiredStoneType;
    // 1회 생산 시 소모되는 마석 수량
    public long stoneCost;
    // 1회 생산 시 획득하는 소모품 수량
    public int outputAmount;

    // 레시피 데이터 생성자
    public CraftingRecipeData(
        int index,
        ConsumableType result,
        string name,
        string desc,
        float time,
        long gold,
        CurrencyType stoneType,
        long stoneAmount,
        int output = 1)
    {
        recipeIndex = index;
        resultType = result;
        displayName = name;
        description = desc;
        baseCraftingTime = time;
        goldCost = gold;
        requiredStoneType = stoneType;
        stoneCost = stoneAmount;
        outputAmount = output;
    }
}
