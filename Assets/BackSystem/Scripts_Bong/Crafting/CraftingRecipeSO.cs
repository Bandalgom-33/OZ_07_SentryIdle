using System;
using UnityEngine;

// 공방 조합 아이템 레시피 ScriptableObject 데이터 에셋
[CreateAssetMenu(fileName = "CraftingRecipe_", menuName = "SentryIdle/Crafting/Recipe")]
public class CraftingRecipeSO : ScriptableObject
{
    #region 레시피 기본 식별 및 결과 아이템

    [Header("1. 레시피 기본 식별")]
    [Tooltip("레시피 고유 식별자 문자열 (예: RECIPE_POTION_LOW)")]
    public string recipeId = "RECIPE_001";

    [Tooltip("조합 결과로 생성되는 소모품 아이템 종류")]
    public ConsumableType resultType = ConsumableType.HealthPotion_Low;

    [Tooltip("공방 UI에 표시될 아이템 이름")]
    public string displayName = "하급 체력포션";

    [Tooltip("공방 UI에 표시될 아이템 상세 설명 및 효과")]
    [TextArea(2, 4)]
    public string description = "필드 위 전체 아군 유닛의 HP를 25% 회복합니다.";

    [Tooltip("레시피 선택 버튼 및 제작 목록 슬롯에 표시될 대표 아이콘 스프라이트")]
    public Sprite recipeIcon;

    #endregion

    #region 생산 시간 및 비용

    [Header("2. 생산 소요 시간 및 소모 재화")]
    [Tooltip("1회 생산에 필요한 기본 소요 시간 (초)")]
    public float baseCraftingTime = 4.0f;

    [Tooltip("1회 생산 시 소모되는 골드 수량")]
    public long goldCost = 100;

    [Tooltip("1회 생산 시 소모되는 마석 종류 (WaveStone 또는 StageStone)")]
    public CurrencyType requiredStoneType = CurrencyType.WaveStone;

    [Tooltip("1회 생산 시 소모되는 마석 수량")]
    public long stoneCost = 1;

    [Tooltip("1회 생산 시 획득하는 기본 소모품 수량")]
    public int outputAmount = 1;

    #endregion

    #region 공방 레벨 해금 조건

    [Header("3. 공방 해금 조건")]
    [Tooltip("이 레시피를 해금하고 생산하기 위해 필요한 최소 공방 레벨 (1 ~ 5)")]
    [Range(1, 5)]
    public int unlockFactoryLevel = 1;

    #endregion
}
