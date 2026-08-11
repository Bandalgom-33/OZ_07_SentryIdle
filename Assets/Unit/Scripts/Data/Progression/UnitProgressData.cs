using System;
using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [Serializable]
    public sealed class UnitProgressData
    {
        [SerializeField] private string unitId;
        [SerializeField] private int currentLevel = 1;
        [SerializeField] private long currentExp;
        [SerializeField] private int promotionStage;

        public string UnitId => unitId;
        public int CurrentLevel => Mathf.Max(1, currentLevel);
        public long CurrentExp => Math.Max(0L, currentExp);
        public int PromotionStage => Mathf.Max(0, promotionStage);

        public static UnitProgressData Create(UnitDataSO unitData)
        {
            if (unitData == null)
            {
                throw new ArgumentNullException(nameof(unitData));
            }

            return new UnitProgressData
            {
                unitId = unitData.UnitId,
                currentLevel = Mathf.Max(1, unitData.InitialLevel),
                currentExp = 0L,
                promotionStage = 0
            };
        }

        public static UnitProgressData Create(UnitDataSO unitData, int level, long exp, int promotionStage)
        {
            if (unitData == null)
            {
                throw new ArgumentNullException(nameof(unitData));
            }

            return new UnitProgressData
            {
                unitId = unitData.UnitId,
                currentLevel = Mathf.Max(1, level),
                currentExp = Math.Max(0L, exp),
                promotionStage = Mathf.Max(0, promotionStage)
            };
        }

        public bool Matches(UnitDataSO unitData)
        {
            return unitData != null && string.Equals(UnitId, unitData.UnitId, StringComparison.Ordinal);
        }

        internal void SetProgress(int level, long exp)
        {
            currentLevel = Mathf.Max(1, level);
            currentExp = Math.Max(0L, exp);
        }

        internal void SetPromotionStage(int stage)
        {
            promotionStage = Mathf.Max(0, stage);
        }
    }
}
