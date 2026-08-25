using System.Collections.Generic;
using UnityEngine;
using EndlessGuard.Unit.Runtime;
using EndlessGuard.Unit.Data;

public class EquipmentUnitStatBridge : MonoBehaviour
{
    [SerializeField] private EquipmentManager equipmentManager;

    [Header("장비 스탯을 적용할 캐릭터")]
    [SerializeField] private string targetUnitId;

    private readonly List<int> modifierIds = new List<int>();

    private void OnEnable()
    {
        if (equipmentManager != null)
        {
            equipmentManager.OnEquipmentStatsChanged += ApplyEquipmentStats;
        }
    }

    private void OnDisable()
    {
        if (equipmentManager != null)
        {
            equipmentManager.OnEquipmentStatsChanged -= ApplyEquipmentStats;
        }
    }

    private void ApplyEquipmentStats(EquipmentBonusStats stats)
    {
        UnitRuntimeState targetUnit = FindTargetUnit();

        if (targetUnit == null) return;
        if (targetUnit.Stats == null) return;
        if (!targetUnit.Stats.IsInitialized) return;
        
        Debug.Log(
            $"[장비 적용 전] " +
            $"물공: {targetUnit.Stats.PhysicalAttack} / " +
            $"마공: {targetUnit.Stats.MagicalAttack} / " +
            $"물방: {targetUnit.Stats.PhysicalDefense} / " +
            $"마방: {targetUnit.Stats.MagicalDefense} / " +
            $"치피: {targetUnit.Stats.CriticalDamageBonusPercent} / " +
            $"명중: {targetUnit.Stats.Accuracy}"
        );

        RemoveCurrentModifiers(targetUnit);

        AddModifier(targetUnit, PassiveStatType.PhysicalAttack, stats.PhysicalAttack );
        AddModifier(targetUnit, PassiveStatType.MagicalAttack, stats.MagicAttack);
        AddModifier(targetUnit, PassiveStatType.PhysicalDefense, stats.PhysicalDefense);
        AddModifier(targetUnit, PassiveStatType.MagicalDefense, stats.MagicDefense);
        AddModifier(targetUnit, PassiveStatType.CriticalDamageBonusPercent, stats.CriticalDamageBonus );
        AddModifier(targetUnit, PassiveStatType.Accuracy, stats.Accuracy);
    }

    private UnitRuntimeState FindTargetUnit()
    {
        foreach (UnitRuntimeState unit in CombatRegistry.Units)
        {
            if (unit == null) continue;

            if (unit.UnitId == targetUnitId) return unit;
            
        }

        return null;
    }

    private void AddModifier( UnitRuntimeState unit, PassiveStatType statType, float value)
    {
        if (Mathf.Approximately(value, 0f)) return;
        int modifierId = unit.Stats.AddModifier(statType, value, 0f);
        if (modifierId > 0) modifierIds.Add(modifierId);
        
    }

    private void RemoveCurrentModifiers(UnitRuntimeState unit)
    {
        for (int i = 0; i < modifierIds.Count; i++)
        {
            unit.Stats.RemoveModifier(modifierIds[i]);
        }

        modifierIds.Clear();
    }
}