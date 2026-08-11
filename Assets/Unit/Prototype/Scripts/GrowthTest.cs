using EndlessGuard.Unit.Runtime;
using UnityEngine;

namespace EndlessGuard.Unit.Prototype
{
    [DisallowMultipleComponent]
    public sealed class GrowthTest : MonoBehaviour
    {
        [Header("골드 공통 성장 검증 수치")]
        [Tooltip("최대 HP 성장 버튼을 한 번 누를 때 모든 필드 캐릭터에게 더할 값입니다.")]
        [Min(0f)]
        [SerializeField] private float maxHpAmount = 1000f;

        [Tooltip("초당 HP 재생 성장 버튼을 한 번 누를 때 모든 필드 캐릭터에게 더할 값입니다.")]
        [Min(0f)]
        [SerializeField] private float hpRegenAmount = 25f;

        [Tooltip("물리 공격력 성장 버튼을 한 번 누를 때 모든 필드 캐릭터에게 더할 값입니다.")]
        [Min(0f)]
        [SerializeField] private float physicalAttackAmount = 100f;

        [Tooltip("마법 공격력 성장 버튼을 한 번 누를 때 모든 필드 캐릭터에게 더할 값입니다.")]
        [Min(0f)]
        [SerializeField] private float magicalAttackAmount = 100f;

        [Tooltip("물리 방어력 성장 버튼을 한 번 누를 때 모든 필드 캐릭터에게 더할 값입니다.")]
        [Min(0f)]
        [SerializeField] private float physicalDefenseAmount = 100f;

        [Tooltip("마법 방어력 성장 버튼을 한 번 누를 때 모든 필드 캐릭터에게 더할 값입니다.")]
        [Min(0f)]
        [SerializeField] private float magicalDefenseAmount = 100f;

        [Tooltip("공격속도 성장 버튼을 한 번 누를 때 모든 필드 캐릭터의 초당 기본 공격 횟수에 더할 값입니다.")]
        [Min(0f)]
        [SerializeField] private float attackSpeedAmount = 0.5f;

        [Tooltip("명중 성장 버튼을 한 번 누를 때 모든 필드 캐릭터에게 더할 값입니다.")]
        [Min(0f)]
        [SerializeField] private float accuracyAmount = 10f;

        [Tooltip("회피 성장 버튼을 한 번 누를 때 모든 필드 캐릭터에게 더할 값입니다.")]
        [Min(0f)]
        [SerializeField] private float evasionAmount = 10f;

        [Tooltip("치명타 확률 성장 버튼을 한 번 누를 때 모든 필드 캐릭터에게 더할 퍼센트포인트 값입니다.")]
        [Min(0f)]
        [SerializeField] private float criticalChanceAmount = 10f;

        [Tooltip("치명타 피해량 성장 버튼을 한 번 누를 때 모든 필드 캐릭터에게 더할 퍼센트포인트 값입니다.")]
        [Min(0f)]
        [SerializeField] private float criticalDamageAmount = 25f;

        [Header("검증 상태")]
        [Tooltip("마지막 성장 버튼이 적용된 캐릭터 수입니다.")]
        [SerializeField] private int lastAppliedUnitCount;

        [Tooltip("마지막 성장 검증 결과입니다.")]
        [TextArea(2, 4)]
        [SerializeField] private string lastMessage;

        public float MaxHpAmount => maxHpAmount;
        public float HpRegenAmount => hpRegenAmount;
        public float PhysicalAttackAmount => physicalAttackAmount;
        public float MagicalAttackAmount => magicalAttackAmount;
        public float PhysicalDefenseAmount => physicalDefenseAmount;
        public float MagicalDefenseAmount => magicalDefenseAmount;
        public float AttackSpeedAmount => attackSpeedAmount;
        public float AccuracyAmount => accuracyAmount;
        public float EvasionAmount => evasionAmount;
        public float CriticalChanceAmount => criticalChanceAmount;
        public float CriticalDamageAmount => criticalDamageAmount;
        public int LastAppliedUnitCount => lastAppliedUnitCount;
        public string LastMessage => lastMessage;

        public void AddMaxHp()
        {
            int appliedCount = 0;

            foreach (UnitRuntimeState unit in CombatRegistry.Units)
            {
                if (!CanApply(unit))
                {
                    continue;
                }

                unit.SetMaxHp(unit.Stats.MaxHp + maxHpAmount);
                appliedCount++;
            }

            Complete("최대 HP", maxHpAmount, appliedCount);
        }

        public void AddHpRegen()
        {
            int appliedCount = 0;

            foreach (UnitRuntimeState unit in CombatRegistry.Units)
            {
                if (!CanApply(unit))
                {
                    continue;
                }

                unit.Stats.SetHpRegenPerSecond(unit.Stats.HpRegenPerSecond + hpRegenAmount);
                appliedCount++;
            }

            Complete("초당 HP 재생", hpRegenAmount, appliedCount);
        }

        public void AddPhysicalAttack()
        {
            int appliedCount = 0;

            foreach (UnitRuntimeState unit in CombatRegistry.Units)
            {
                if (!CanApply(unit))
                {
                    continue;
                }

                unit.Stats.SetPhysicalAttack(unit.Stats.PhysicalAttack + physicalAttackAmount);
                appliedCount++;
            }

            Complete("물리 공격력", physicalAttackAmount, appliedCount);
        }

        public void AddMagicalAttack()
        {
            int appliedCount = 0;

            foreach (UnitRuntimeState unit in CombatRegistry.Units)
            {
                if (!CanApply(unit))
                {
                    continue;
                }

                unit.Stats.SetMagicalAttack(unit.Stats.MagicalAttack + magicalAttackAmount);
                appliedCount++;
            }

            Complete("마법 공격력", magicalAttackAmount, appliedCount);
        }

        public void AddPhysicalDefense()
        {
            int appliedCount = 0;

            foreach (UnitRuntimeState unit in CombatRegistry.Units)
            {
                if (!CanApply(unit))
                {
                    continue;
                }

                unit.Stats.SetPhysicalDefense(unit.Stats.PhysicalDefense + physicalDefenseAmount);
                appliedCount++;
            }

            Complete("물리 방어력", physicalDefenseAmount, appliedCount);
        }

        public void AddMagicalDefense()
        {
            int appliedCount = 0;

            foreach (UnitRuntimeState unit in CombatRegistry.Units)
            {
                if (!CanApply(unit))
                {
                    continue;
                }

                unit.Stats.SetMagicalDefense(unit.Stats.MagicalDefense + magicalDefenseAmount);
                appliedCount++;
            }

            Complete("마법 방어력", magicalDefenseAmount, appliedCount);
        }

        public void AddAttackSpeed()
        {
            int appliedCount = 0;

            foreach (UnitRuntimeState unit in CombatRegistry.Units)
            {
                if (!CanApply(unit))
                {
                    continue;
                }

                unit.Stats.SetAttacksPerSecond(unit.Stats.AttacksPerSecond + attackSpeedAmount);
                appliedCount++;
            }

            Complete("공격속도", attackSpeedAmount, appliedCount);
        }

        public void AddAccuracy()
        {
            int appliedCount = 0;

            foreach (UnitRuntimeState unit in CombatRegistry.Units)
            {
                if (!CanApply(unit))
                {
                    continue;
                }

                unit.Stats.SetAccuracy(unit.Stats.Accuracy + accuracyAmount);
                appliedCount++;
            }

            Complete("명중", accuracyAmount, appliedCount);
        }

        public void AddEvasion()
        {
            int appliedCount = 0;

            foreach (UnitRuntimeState unit in CombatRegistry.Units)
            {
                if (!CanApply(unit))
                {
                    continue;
                }

                unit.Stats.SetEvasion(unit.Stats.Evasion + evasionAmount);
                appliedCount++;
            }

            Complete("회피", evasionAmount, appliedCount);
        }

        public void AddCriticalChance()
        {
            int appliedCount = 0;

            foreach (UnitRuntimeState unit in CombatRegistry.Units)
            {
                if (!CanApply(unit))
                {
                    continue;
                }

                unit.Stats.SetCriticalChancePercent(unit.Stats.CriticalChancePercent + criticalChanceAmount);
                appliedCount++;
            }

            Complete("치명타 확률", criticalChanceAmount, appliedCount);
        }

        public void AddCriticalDamage()
        {
            int appliedCount = 0;

            foreach (UnitRuntimeState unit in CombatRegistry.Units)
            {
                if (!CanApply(unit))
                {
                    continue;
                }

                unit.Stats.SetCriticalDamageBonusPercent(unit.Stats.CriticalDamageBonusPercent + criticalDamageAmount);
                appliedCount++;
            }

            Complete("치명타 피해량", criticalDamageAmount, appliedCount);
        }

        private static bool CanApply(UnitRuntimeState unit)
        {
            return unit != null && unit.IsInitialized && unit.Stats != null && unit.Stats.IsInitialized;
        }

        private void Complete(string statName, float amount, int appliedCount)
        {
            lastAppliedUnitCount = appliedCount;

            if (appliedCount <= 0)
            {
                lastMessage = $"{statName} 성장을 적용할 필드 캐릭터가 없습니다.";
                Debug.LogWarning(lastMessage, this);
                return;
            }

            lastMessage = $"{statName} +{amount:0.###} 적용 완료: 필드 캐릭터 {appliedCount}명";
            Debug.Log(lastMessage, this);
        }
    }
}