using System;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    [DisallowMultipleComponent]
    public sealed class CombatHealth : MonoBehaviour
    {
        [Header("런타임 체력 상태")]
        [Tooltip("현재 런타임에서 사용하는 최대 HP입니다.")]
        [Min(0f)]
        [SerializeField] private float maxHp;

        [Tooltip("현재 남아 있는 HP입니다.")]
        [Min(0f)]
        [SerializeField] private float currentHp;

        [Tooltip("체력 상태가 정적 데이터로 초기화됐는지 표시합니다.")]
        [SerializeField] private bool isInitialized;

        [Tooltip("현재 사망 상태인지 표시합니다.")]
        [SerializeField] private bool isDead;

        public event Action<CombatHealth> OnHealthChanged;
        public event Action<CombatHealth, float> OnDamaged;
        public event Action<CombatHealth, float> OnHealed;
        public event Action<CombatHealth> OnDied;

        public float MaxHp => maxHp;
        public float CurrentHp => currentHp;
        public float NormalizedHp => maxHp > 0f ? currentHp / maxHp : 0f;
        public bool IsInitialized => isInitialized;
        public bool IsDead => isDead;

        public void Initialize(float initialMaxHp)
        {
            maxHp = Mathf.Max(1f, initialMaxHp);
            currentHp = maxHp;
            isInitialized = true;
            isDead = false;
            OnHealthChanged?.Invoke(this);
        }

        public float ApplyDamage(float damage)
        {
            if (!isInitialized || isDead || damage <= 0f)
            {
                return 0f;
            }

            float appliedDamage = Mathf.Min(currentHp, damage);
            currentHp -= appliedDamage;
            OnDamaged?.Invoke(this, appliedDamage);
            OnHealthChanged?.Invoke(this);

            if (currentHp <= 0f)
            {
                currentHp = 0f;
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
            OnHealed?.Invoke(this, healedAmount);
            OnHealthChanged?.Invoke(this);
            return healedAmount;
        }
    }
}