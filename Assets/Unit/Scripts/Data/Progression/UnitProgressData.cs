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

        public string UnitId => unitId;
        public int CurrentLevel => Mathf.Max(1, currentLevel);
        public long CurrentExp => Math.Max(0L, currentExp);

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
                currentExp = 0L
            };
        }

        internal void SetProgress(int level, long exp)
        {
            currentLevel = Mathf.Max(1, level);
            currentExp = Math.Max(0L, exp);
        }
    }
}