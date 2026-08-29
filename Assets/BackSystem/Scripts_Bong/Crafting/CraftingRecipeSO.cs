using System;
using UnityEngine;

// 공방 조합 아이템 레시피 ScriptableObject 데이터 에셋
[CreateAssetMenu(fileName = "CraftingRecipe_", menuName = "SentryIdle/Crafting/Recipe")]
public class CraftingRecipeSO : ScriptableObject
{
    #region 레시피 기본 식별 및 결과 아이템

    [Header("1. 레시피 기본 식별 및 결과 아이템")]
    [Tooltip("레시피 고유 식별자 문자열 (예: RECIPE_POTION_LOW)")]
    public string recipeId = "RECIPE_001";

    [Tooltip("레시피 카테고리 (소모품 vs 장비)")]
    public ItemCategory itemCategory = ItemCategory.Consumable;

    [Tooltip("조합 결과로 생성되는 ItemDataSO 에셋 (소모품/장비 공통)")]
    public ItemDataSO resultItem;

    #endregion

    #region 생산 시간 및 비용

    [Header("2. 생산 소요 시간 및 소모 재화")]
    [Tooltip("1회 생산에 필요한 기본 소요 시간 (초)")]
    public float baseCraftingTime = 4.0f;

    [Tooltip("1회 생산 시 소모되는 골드 수량")]
    public long goldCost = 0;

    [Tooltip("1회 생산 시 소모되는 다이아 수량")]
    public long diamondCost = 0;

    [Tooltip("1회 생산 시 소모되는 마석 종류 (WaveStone, DungeonStone, RaidStone 3종 전용)")]
    public StoneType requiredStoneType = StoneType.WaveStone;

    [Tooltip("1회 생산 시 소모되는 마석 수량")]
    public long stoneCost = 1;

    [Tooltip("1회 생산 시 획득하는 기본 수량")]
    public int outputAmount = 1;

    #endregion

    #region 공방 레벨 해금 조건

    [Header("3. 공방 해금 조건")]
    [Tooltip("이 레시피를 해금하고 생산하기 위해 필요한 최소 공방 레벨 (1 ~ 5)")]
    [Range(1, 5)]
    public int unlockFactoryLevel = 1;

    #endregion

    #region 널 안전 프로퍼티

    // 레시피 표시 이름 반환 프로퍼티
    public string DisplayName => resultItem != null ? resultItem.ItemName : recipeId;

    // 레시피 상세 설명 반환 프로퍼티
    public string Description => resultItem != null ? resultItem.Description : string.Empty;

    // 레시피 대표 아이콘 반환 프로퍼티
    public Sprite RecipeIcon => resultItem != null ? resultItem.ItemIcon : null;

    #endregion
}
