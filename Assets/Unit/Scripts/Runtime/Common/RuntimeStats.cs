using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    [Serializable]
    public sealed class RuntimeStats
    {
        [Header("런타임 생존 능력치")]
        [Tooltip("성장, 패시브, 버프와 디버프가 반영된 현재 최대 HP입니다.")]
        [Min(0f)]
        [SerializeField] private float maxHp;

        [Tooltip("성장, 패시브, 버프와 디버프가 반영된 현재 초당 HP 재생량입니다.")]
        [Min(0f)]
        [SerializeField] private float hpRegenPerSecond;

        [Header("런타임 공격 능력치")]
        [Tooltip("성장, 패시브, 버프와 디버프가 반영된 현재 물리 공격력입니다.")]
        [Min(0f)]
        [SerializeField] private float physicalAttack;

        [Tooltip("성장, 패시브, 버프와 디버프가 반영된 현재 마법 공격력입니다.")]
        [Min(0f)]
        [SerializeField] private float magicalAttack;

        [Tooltip("성장, 패시브, 버프와 디버프가 반영된 현재 초당 공격 횟수입니다.")]
        [Min(0f)]
        [SerializeField] private float attacksPerSecond;

        [Header("런타임 방어 능력치")]
        [Tooltip("성장, 패시브, 버프와 디버프가 반영된 현재 물리 방어력입니다.")]
        [Min(0f)]
        [SerializeField] private float physicalDefense;

        [Tooltip("성장, 패시브, 버프와 디버프가 반영된 현재 마법 방어력입니다.")]
        [Min(0f)]
        [SerializeField] private float magicalDefense;

        [Header("런타임 명중·회피 능력치")]
        [Tooltip("성장, 패시브, 버프와 디버프가 반영된 현재 명중 능력치입니다.")]
        [Min(0f)]
        [SerializeField] private float accuracy;

        [Tooltip("성장, 패시브, 버프와 디버프가 반영된 현재 회피 능력치입니다.")]
        [Min(0f)]
        [SerializeField] private float evasion;

        [Header("런타임 치명타 능력치")]
        [Tooltip("성장, 패시브, 버프와 디버프가 반영된 현재 치명타 확률입니다. 단위는 퍼센트입니다.")]
        [Range(0f, 100f)]
        [SerializeField] private float criticalChancePercent;

        [Tooltip("성장, 패시브, 버프와 디버프가 반영된 현재 치명타 추가 피해 비율입니다. 단위는 퍼센트입니다.")]
        [Min(0f)]
        [SerializeField] private float criticalDamageBonusPercent;

        [Header("런타임 이동 능력치")]
        [Tooltip("성장, 패시브, 버프와 디버프가 반영된 현재 초당 이동 거리입니다.")]
        [Min(0f)]
        [SerializeField] private float moveSpeed;

        [Header("런타임 상태")]
        [Tooltip("기준 전투 능력치로 RuntimeStats가 초기화되었는지 표시합니다.")]
        [SerializeField] private bool isInitialized;

        private float baseMaxHp;
        private float baseHpRegenPerSecond;
        private float basePhysicalAttack;
        private float baseMagicalAttack;
        private float baseAttacksPerSecond;
        private float basePhysicalDefense;
        private float baseMagicalDefense;
        private float baseAccuracy;
        private float baseEvasion;
        private float baseCriticalChancePercent;
        private float baseCriticalDamageBonusPercent;
        private float baseMoveSpeed;

        private List<StatModifier> modifiers;
        private int nextModifierId = 1;

        public float MaxHp => maxHp;
        public float HpRegenPerSecond => hpRegenPerSecond;
        public float PhysicalAttack => physicalAttack;
        public float MagicalAttack => magicalAttack;
        public float AttacksPerSecond => attacksPerSecond;
        public float AttackInterval => attacksPerSecond > 0f ? 1f / attacksPerSecond : float.PositiveInfinity;
        public float PhysicalDefense => physicalDefense;
        public float MagicalDefense => magicalDefense;
        public float Accuracy => accuracy;
        public float Evasion => evasion;
        public float CriticalChancePercent => criticalChancePercent;
        public float CriticalDamageBonusPercent => criticalDamageBonusPercent;
        public float MoveSpeed => moveSpeed;
        public bool IsInitialized => isInitialized;
        public int ActiveModifierCount => modifiers == null ? 0 : modifiers.Count;

        public bool Initialize(CombatStats baseStats)
        {
            if (baseStats == null)
            {
                Clear();
                return false;
            }

            ClearModifierEntries();

            baseMaxHp = SanitizeMaxHp(baseStats.MaxHp);
            baseHpRegenPerSecond = 0f;
            basePhysicalAttack = Sanitize(baseStats.PhysicalAttack);
            baseMagicalAttack = Sanitize(baseStats.MagicalAttack);
            baseAttacksPerSecond = Sanitize(baseStats.BaseAttacksPerSecond);
            basePhysicalDefense = Sanitize(baseStats.PhysicalDefense);
            baseMagicalDefense = Sanitize(baseStats.MagicalDefense);
            baseAccuracy = Sanitize(baseStats.Accuracy);
            baseEvasion = Sanitize(baseStats.Evasion);
            baseCriticalChancePercent = 0f;
            baseCriticalDamageBonusPercent = 0f;
            baseMoveSpeed = Sanitize(baseStats.MoveSpeed);

            isInitialized = true;

            RecalculateAll();

            return true;
        }

        public int AddModifier(PassiveStatType statType, float flatBonus, float percentBonus)
        {
            if (!isInitialized || statType == PassiveStatType.None)
            {
                return 0;
            }

            if (modifiers == null)
            {
                modifiers = new List<StatModifier>(4);
            }

            int modifierId = nextModifierId++;
            modifiers.Add(new StatModifier(modifierId, statType, SanitizeSigned(flatBonus), SanitizeSigned(percentBonus)));

            Recalculate(statType);

            return modifierId;
        }

        public bool UpdateModifier(int modifierId, float flatBonus, float percentBonus)
        {
            if (modifierId <= 0 || modifiers == null)
            {
                return false;
            }

            for (int i = 0; i < modifiers.Count; i++)
            {
                StatModifier modifier = modifiers[i];

                if (modifier.Id != modifierId)
                {
                    continue;
                }

                modifier.FlatBonus = SanitizeSigned(flatBonus);
                modifier.PercentBonus = SanitizeSigned(percentBonus);
                modifiers[i] = modifier;

                Recalculate(modifier.StatType);

                return true;
            }

            return false;
        }

        public bool RemoveModifier(int modifierId)
        {
            if (modifierId <= 0 || modifiers == null)
            {
                return false;
            }

            for (int i = 0; i < modifiers.Count; i++)
            {
                StatModifier modifier = modifiers[i];

                if (modifier.Id != modifierId)
                {
                    continue;
                }

                int lastIndex = modifiers.Count - 1;

                if (i != lastIndex)
                {
                    modifiers[i] = modifiers[lastIndex];
                }

                modifiers.RemoveAt(lastIndex);

                Recalculate(modifier.StatType);

                return true;
            }

            return false;
        }

        public void ClearModifiers()
        {
            if (modifiers == null || modifiers.Count == 0)
            {
                return;
            }

            modifiers.Clear();
            RecalculateAll();
        }

        public void SetMaxHp(float value)
        {
            baseMaxHp = SanitizeMaxHp(value);
            Recalculate(PassiveStatType.MaxHp);
        }

        public void SetHpRegenPerSecond(float value)
        {
            baseHpRegenPerSecond = Sanitize(value);
            Recalculate(PassiveStatType.HpRegenPerSecond);
        }

        public void SetPhysicalAttack(float value)
        {
            basePhysicalAttack = Sanitize(value);
            Recalculate(PassiveStatType.PhysicalAttack);
        }

        public void SetMagicalAttack(float value)
        {
            baseMagicalAttack = Sanitize(value);
            Recalculate(PassiveStatType.MagicalAttack);
        }

        public void SetAttacksPerSecond(float value)
        {
            baseAttacksPerSecond = Sanitize(value);
            Recalculate(PassiveStatType.AttacksPerSecond);
        }

        public void SetPhysicalDefense(float value)
        {
            basePhysicalDefense = Sanitize(value);
            Recalculate(PassiveStatType.PhysicalDefense);
        }

        public void SetMagicalDefense(float value)
        {
            baseMagicalDefense = Sanitize(value);
            Recalculate(PassiveStatType.MagicalDefense);
        }

        public void SetAccuracy(float value)
        {
            baseAccuracy = Sanitize(value);
            Recalculate(PassiveStatType.Accuracy);
        }

        public void SetEvasion(float value)
        {
            baseEvasion = Sanitize(value);
            Recalculate(PassiveStatType.Evasion);
        }

        public void SetCriticalChancePercent(float value)
        {
            baseCriticalChancePercent = SanitizePercent(value);
            Recalculate(PassiveStatType.CriticalChancePercent);
        }

        public void SetCriticalDamageBonusPercent(float value)
        {
            baseCriticalDamageBonusPercent = Sanitize(value);
            Recalculate(PassiveStatType.CriticalDamageBonusPercent);
        }

        public void SetMoveSpeed(float value)
        {
            baseMoveSpeed = Sanitize(value);
            Recalculate(PassiveStatType.MoveSpeed);
        }

        public void Clear()
        {
            ClearModifierEntries();

            baseMaxHp = 0f;
            baseHpRegenPerSecond = 0f;
            basePhysicalAttack = 0f;
            baseMagicalAttack = 0f;
            baseAttacksPerSecond = 0f;
            basePhysicalDefense = 0f;
            baseMagicalDefense = 0f;
            baseAccuracy = 0f;
            baseEvasion = 0f;
            baseCriticalChancePercent = 0f;
            baseCriticalDamageBonusPercent = 0f;
            baseMoveSpeed = 0f;

            maxHp = 0f;
            hpRegenPerSecond = 0f;
            physicalAttack = 0f;
            magicalAttack = 0f;
            attacksPerSecond = 0f;
            physicalDefense = 0f;
            magicalDefense = 0f;
            accuracy = 0f;
            evasion = 0f;
            criticalChancePercent = 0f;
            criticalDamageBonusPercent = 0f;
            moveSpeed = 0f;
            isInitialized = false;
        }

        private void RecalculateAll()
        {
            Recalculate(PassiveStatType.MaxHp);
            Recalculate(PassiveStatType.HpRegenPerSecond);
            Recalculate(PassiveStatType.PhysicalAttack);
            Recalculate(PassiveStatType.MagicalAttack);
            Recalculate(PassiveStatType.PhysicalDefense);
            Recalculate(PassiveStatType.MagicalDefense);
            Recalculate(PassiveStatType.AttacksPerSecond);
            Recalculate(PassiveStatType.Accuracy);
            Recalculate(PassiveStatType.Evasion);
            Recalculate(PassiveStatType.CriticalChancePercent);
            Recalculate(PassiveStatType.CriticalDamageBonusPercent);
            Recalculate(PassiveStatType.MoveSpeed);
        }

        private void Recalculate(PassiveStatType statType)
        {
            float baseValue = GetBaseValue(statType);
            float flatBonus = 0f;
            float percentBonus = 0f;

            if (modifiers != null)
            {
                for (int i = 0; i < modifiers.Count; i++)
                {
                    StatModifier modifier = modifiers[i];

                    if (modifier.StatType != statType)
                    {
                        continue;
                    }

                    flatBonus += modifier.FlatBonus;
                    percentBonus += modifier.PercentBonus;
                }
            }

            float multiplier = Mathf.Max(0f, 1f + percentBonus * 0.01f);
            float finalValue = baseValue * multiplier + flatBonus;

            SetFinalValue(statType, finalValue);
        }

        private float GetBaseValue(PassiveStatType statType)
        {
            switch (statType)
            {
                case PassiveStatType.MaxHp:
                    return baseMaxHp;

                case PassiveStatType.HpRegenPerSecond:
                    return baseHpRegenPerSecond;

                case PassiveStatType.PhysicalAttack:
                    return basePhysicalAttack;

                case PassiveStatType.MagicalAttack:
                    return baseMagicalAttack;

                case PassiveStatType.PhysicalDefense:
                    return basePhysicalDefense;

                case PassiveStatType.MagicalDefense:
                    return baseMagicalDefense;

                case PassiveStatType.AttacksPerSecond:
                    return baseAttacksPerSecond;

                case PassiveStatType.Accuracy:
                    return baseAccuracy;

                case PassiveStatType.Evasion:
                    return baseEvasion;

                case PassiveStatType.CriticalChancePercent:
                    return baseCriticalChancePercent;

                case PassiveStatType.CriticalDamageBonusPercent:
                    return baseCriticalDamageBonusPercent;

                case PassiveStatType.MoveSpeed:
                    return baseMoveSpeed;

                default:
                    return 0f;
            }
        }

        private void SetFinalValue(PassiveStatType statType, float value)
        {
            switch (statType)
            {
                case PassiveStatType.MaxHp:
                    maxHp = SanitizeMaxHp(value);
                    break;

                case PassiveStatType.HpRegenPerSecond:
                    hpRegenPerSecond = Sanitize(value);
                    break;

                case PassiveStatType.PhysicalAttack:
                    physicalAttack = Sanitize(value);
                    break;

                case PassiveStatType.MagicalAttack:
                    magicalAttack = Sanitize(value);
                    break;

                case PassiveStatType.PhysicalDefense:
                    physicalDefense = Sanitize(value);
                    break;

                case PassiveStatType.MagicalDefense:
                    magicalDefense = Sanitize(value);
                    break;

                case PassiveStatType.AttacksPerSecond:
                    attacksPerSecond = Sanitize(value);
                    break;

                case PassiveStatType.Accuracy:
                    accuracy = Sanitize(value);
                    break;

                case PassiveStatType.Evasion:
                    evasion = Sanitize(value);
                    break;

                case PassiveStatType.CriticalChancePercent:
                    criticalChancePercent = SanitizePercent(value);
                    break;

                case PassiveStatType.CriticalDamageBonusPercent:
                    criticalDamageBonusPercent = Sanitize(value);
                    break;

                case PassiveStatType.MoveSpeed:
                    moveSpeed = Sanitize(value);
                    break;
            }
        }

        private void ClearModifierEntries()
        {
            if (modifiers != null)
            {
                modifiers.Clear();
            }
        }

        private static float Sanitize(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return 0f;
            }

            return Mathf.Max(0f, value);
        }

        private static float SanitizeMaxHp(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return 1f;
            }

            return Mathf.Max(1f, value);
        }

        private static float SanitizePercent(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return 0f;
            }

            return Mathf.Clamp(value, 0f, 100f);
        }

        private static float SanitizeSigned(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return 0f;
            }

            return value;
        }

        private struct StatModifier
        {
            public int Id;
            public PassiveStatType StatType;
            public float FlatBonus;
            public float PercentBonus;

            public StatModifier(int id, PassiveStatType statType, float flatBonus, float percentBonus)
            {
                Id = id;
                StatType = statType;
                FlatBonus = flatBonus;
                PercentBonus = percentBonus;
            }
        }
    }
}