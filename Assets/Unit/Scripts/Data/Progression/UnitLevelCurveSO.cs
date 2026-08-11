using System;
using System.Collections.Generic;
using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [CreateAssetMenu(fileName = "UnitLevelCurve", menuName = "Endless Guard/Progression/Unit Level Curve")]
    public sealed class UnitLevelCurveSO : ScriptableObject
    {
        [Header("필요 경험치 계산식")]
        [Tooltip("Lv.1에서 Lv.2로 올라갈 때 사용하는 기준 필요 경험치입니다.")]
        [SerializeField] private long baseRequiredExp = 100L;

        [Tooltip("현재 레벨이 1 증가할 때마다 추가되는 선형 경험치입니다.")]
        [SerializeField] private long linearIncreasePerLevel = 25L;

        [Tooltip("레벨이 높아질수록 완만한 곡선 형태로 추가되는 경험치의 계수입니다.")]
        [Min(0f)]
        [SerializeField] private float powerCoefficient = 5f;

        [Tooltip("거듭제곱 성장 곡선의 지수입니다. 값이 높을수록 후반 필요 경험치가 빠르게 증가합니다.")]
        [Range(1f, 3f)]
        [SerializeField] private float powerExponent = 1.5f;

        [Header("특정 레벨 예외")]
        [Tooltip("자동 수식 대신 별도의 필요 경험치를 사용할 레벨만 등록합니다.")]
        [SerializeField] private List<LevelExpOverride> levelOverrides = new List<LevelExpOverride>();

        public long BaseRequiredExp => Math.Max(1L, baseRequiredExp);
        public long LinearIncreasePerLevel => Math.Max(0L, linearIncreasePerLevel);
        public float PowerCoefficient => Mathf.Max(0f, powerCoefficient);
        public float PowerExponent => Mathf.Clamp(powerExponent, 1f, 3f);
        public IReadOnlyList<LevelExpOverride> LevelOverrides => levelOverrides;

        public long GetRequiredExp(int currentLevel)
        {
            currentLevel = Mathf.Max(1, currentLevel);

            if (TryGetOverride(currentLevel, out long overriddenExp))
            {
                return overriddenExp;
            }

            double levelOffset = currentLevel - 1d;
            double linearValue = LinearIncreasePerLevel * levelOffset;
            double powerValue = PowerCoefficient * Math.Pow(levelOffset, PowerExponent);
            double calculatedValue = BaseRequiredExp + linearValue + powerValue;

            if (double.IsNaN(calculatedValue) || calculatedValue <= 1d)
            {
                return 1L;
            }

            if (double.IsInfinity(calculatedValue) || calculatedValue >= long.MaxValue)
            {
                return long.MaxValue;
            }

            return (long)Math.Ceiling(calculatedValue);
        }

        public long GetTotalRequiredExp(int startLevel, int targetLevel)
        {
            startLevel = Mathf.Max(1, startLevel);
            targetLevel = Mathf.Max(startLevel, targetLevel);
            long totalExp = 0L;

            for (int currentLevel = startLevel; currentLevel < targetLevel; currentLevel++)
            {
                long requiredExp = GetRequiredExp(currentLevel);

                if (totalExp > long.MaxValue - requiredExp)
                {
                    return long.MaxValue;
                }

                totalExp += requiredExp;
            }

            return totalExp;
        }

        private bool TryGetOverride(int currentLevel, out long requiredExp)
        {
            for (int i = 0; i < levelOverrides.Count; i++)
            {
                LevelExpOverride levelOverride = levelOverrides[i];

                if (levelOverride != null && levelOverride.CurrentLevel == currentLevel)
                {
                    requiredExp = levelOverride.RequiredExp;
                    return true;
                }
            }

            requiredExp = 0L;
            return false;
        }
    }
}