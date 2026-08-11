using System;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    [DisallowMultipleComponent]
    public sealed class CombatHealth : MonoBehaviour
    {
        [Header("Runtime Health State")]
        [SerializeField] private float maxHp;
        [SerializeField] private float currentHp;
        [SerializeField] private float currentShield;
        [SerializeField] private bool isInitialized;
        [SerializeField] private bool isDead;

        public event Action<CombatHealth> OnHealthChanged;
        public event Action<CombatHealth, float> OnDamaged;
        public event Action<CombatHealth, DamageInfo, float> OnDamageResolved;
        public event Action<CombatHealth, float> OnHealed;
        public event Action<CombatHealth> OnDied;

        public float MaxHp => maxHp;
        public float CurrentHp => currentHp;
        public float CurrentShield => currentShield;
        public float NormalizedHp => maxHp > 0f ? Mathf.Clamp01(currentHp / maxHp) : 0f;
        public bool IsInitialized => isInitialized;
        public bool IsDead => isDead;

        public void Initialize(float initialMaxHp)
        {
            maxHp = Mathf.Max(1f, initialMaxHp);
            currentHp = maxHp;
            currentShield = 0f;
            isInitialized = true;
            isDead = false;
        }

        public bool SetMaxHp(float newMaxHp)
        {
            if (!isInitialized || float.IsNaN(newMaxHp) || float.IsInfinity(newMaxHp))
            {
                return false;
            }

            float sanitizedMaxHp = Mathf.Max(1f, newMaxHp);

            if (Mathf.Approximately(maxHp, sanitizedMaxHp))
            {
                return false;
            }

            maxHp = sanitizedMaxHp;

            if (currentHp > maxHp)
            {
                currentHp = maxHp;
            }

            OnHealthChanged?.Invoke(this);
            return true;
        }

        public float ApplyDamage(float finalDamage)
        {
            return ApplyDamage(new DamageInfo(finalDamage, DamageType.None, false));
        }

        public float ApplyDamage(DamageInfo damageInfo)
        {
            if (!isInitialized || isDead || damageInfo.FinalDamage <= 0f)
            {
                return 0f;
            }

            float remainingDamage = damageInfo.FinalDamage;
            float absorbedDamage = 0f;

            if (currentShield > 0f)
            {
                absorbedDamage = Mathf.Min(currentShield, remainingDamage);
                currentShield -= absorbedDamage;
                remainingDamage -= absorbedDamage;
            }

            float previousHp = currentHp;

            if (remainingDamage > 0f)
            {
                currentHp = Mathf.Max(0f, currentHp - remainingDamage);
            }

            float hpDamage = previousHp - currentHp;
            float appliedDamage = absorbedDamage + hpDamage;

            if (appliedDamage <= 0f)
            {
                return 0f;
            }

            OnDamaged?.Invoke(this, appliedDamage);
            OnDamageResolved?.Invoke(this, damageInfo, appliedDamage);
            OnHealthChanged?.Invoke(this);

            if (currentHp <= 0f && !isDead)
            {
                isDead = true;
                OnDied?.Invoke(this);
            }

            return appliedDamage;
        }

        public float Heal(float amount)
        {
            if (!isInitialized || isDead || amount <= 0f || currentHp >= maxHp)
            {
                return 0f;
            }

            float previousHp = currentHp;
            currentHp = Mathf.Min(maxHp, currentHp + amount);
            float healedAmount = currentHp - previousHp;

            if (healedAmount <= 0f)
            {
                return 0f;
            }

            OnHealed?.Invoke(this, healedAmount);
            OnHealthChanged?.Invoke(this);
            return healedAmount;
        }

        public float AddShield(float amount)
        {
            if (!isInitialized || isDead || amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount))
            {
                return 0f;
            }

            float previousShield = currentShield;
            currentShield = Mathf.Max(currentShield, amount);
            float addedShield = currentShield - previousShield;

            if (addedShield > 0f)
            {
                OnHealthChanged?.Invoke(this);
            }

            return addedShield;
        }

        public float ClearShield()
        {
            if (!isInitialized || currentShield <= 0f)
            {
                return 0f;
            }

            float removedShield = currentShield;
            currentShield = 0f;
            OnHealthChanged?.Invoke(this);
            return removedShield;
        }
    }
}