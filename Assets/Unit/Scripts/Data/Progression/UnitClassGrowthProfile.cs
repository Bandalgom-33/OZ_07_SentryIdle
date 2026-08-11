using System;
using System.Collections.Generic;
using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [Serializable]
    public sealed class UnitClassGrowthProfile
    {
        [Tooltip("이 성장 프로필을 공유하는 캐릭터의 상위 분류입니다.")]
        [SerializeField] private UnitClass unitClass = UnitClass.None;

        [Header("레벨업 성장")]
        [Tooltip("레벨이 1 증가할 때 적용할 공통 성장 규칙입니다.")]
        [SerializeField] private UnitGrowthRule levelGrowth = new UnitGrowthRule();

        [Header("승급 성장")]
        [Tooltip("승급 단계가 1 증가할 때 적용할 공통 성장 규칙입니다. 아직 밸런스를 정하지 않았다면 능력치 선택 없음 / 0%로 두면 됩니다.")]
        [SerializeField] private UnitGrowthRule promotionGrowth = new UnitGrowthRule();

        [Header("최대 레벨")]
        [Tooltip("승급하지 않은 상태(PromotionStage 0)의 최대 레벨입니다.")]
        [Min(1)]
        [SerializeField] private int baseMaxLevel = 30;

        [Tooltip("승급 단계별 최대 레벨을 데이터로 설정합니다. 승급 조건/중복 캐릭터 소비는 다른 담당 시스템에서 처리합니다.")]
        [SerializeField] private List<PromotionLevelCap> promotionLevelCaps = new List<PromotionLevelCap>();

        public UnitClass UnitClass => unitClass;
        public UnitGrowthRule LevelGrowth => levelGrowth;
        public UnitGrowthRule PromotionGrowth => promotionGrowth;
        public int BaseMaxLevel => Mathf.Max(1, baseMaxLevel);
        public IReadOnlyList<PromotionLevelCap> PromotionLevelCaps => promotionLevelCaps;

        public int GetMaxLevel(int promotionStage)
        {
            promotionStage = Mathf.Max(0, promotionStage);
            int resolvedMaxLevel = BaseMaxLevel;

            if (promotionLevelCaps == null)
            {
                return resolvedMaxLevel;
            }

            for (int i = 0; i < promotionLevelCaps.Count; i++)
            {
                PromotionLevelCap cap = promotionLevelCaps[i];

                if (cap == null || cap.PromotionStage > promotionStage)
                {
                    continue;
                }

                resolvedMaxLevel = Mathf.Max(resolvedMaxLevel, cap.MaxLevel);
            }

            return resolvedMaxLevel;
        }

        public bool HasPromotionStage(int promotionStage)
        {
            if (promotionStage <= 0 || promotionLevelCaps == null)
            {
                return false;
            }

            for (int i = 0; i < promotionLevelCaps.Count; i++)
            {
                PromotionLevelCap cap = promotionLevelCaps[i];

                if (cap != null && cap.PromotionStage == promotionStage)
                {
                    return true;
                }
            }

            return false;
        }

        internal void SetUnitClass(UnitClass value)
        {
            unitClass = value;
        }
    }
}
