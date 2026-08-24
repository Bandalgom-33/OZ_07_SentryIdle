using System;
using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    [Header("장비 슬롯")]
    [SerializeField] private EquipmentSlotUI headSlot;
    [SerializeField] private EquipmentSlotUI armorSlot;
    [SerializeField] private EquipmentSlotUI weaponSlot;
    [SerializeField] private EquipmentSlotUI accessorySlot;

    // 현재 장착 중인 아이템
    private ItemDataSO equippedHead;
    private ItemDataSO equippedArmor;
    private ItemDataSO equippedWeapon;
    private ItemDataSO equippedAccessory;
    public EquipmentBonusStats CurrentBonusStats { get; private set; }

    public event Action<EquipmentBonusStats> OnEquipmentStatsChanged;

    public ItemDataSO EquippedHead => equippedHead;
    public ItemDataSO EquippedArmor => equippedArmor;
    public ItemDataSO EquippedWeapon => equippedWeapon;
    public ItemDataSO EquippedAccessory => equippedAccessory;

    
    public void EquipItem(ItemDataSO itemData)
    {
        if (itemData == null) return;

        ItemDataSO previousItem = null;

        switch (itemData.EquipmentType)
        {
            case EquipmentType.Head:
                previousItem = equippedHead;
                equippedHead = itemData;
                headSlot.SetItem(itemData);
                break;

            case EquipmentType.Armor:
                previousItem = equippedArmor;
                equippedArmor = itemData;
                armorSlot.SetItem(itemData);
                break;

            case EquipmentType.Weapon:
                previousItem = equippedWeapon;
                equippedWeapon = itemData;
                weaponSlot.SetItem(itemData);
                break;

            case EquipmentType.Accessory:
                previousItem = equippedAccessory;
                equippedAccessory = itemData;
                accessorySlot.SetItem(itemData);
                break;
        }

        if (previousItem != null)
        {
            Debug.Log(
                $"{previousItem.ItemName} → {itemData.ItemName} 으로 교체"
            );
        }
        else
        {
            Debug.Log($"{itemData.ItemName} 장착");
        }
        RecalculateEquipmentStats();
    }
    
    public void UnequipItem(EquipmentType equipmentType)
    {
        switch (equipmentType)
        {
            case EquipmentType.Head:
                equippedHead = null;
                headSlot.SetItem(null);
                break;

            case EquipmentType.Armor:
                equippedArmor = null;
                armorSlot.SetItem(null);
                break;

            case EquipmentType.Weapon:
                equippedWeapon = null;
                weaponSlot.SetItem(null);
                break;

            case EquipmentType.Accessory:
                equippedAccessory = null;
                accessorySlot.SetItem(null);
                break;
        }
        Debug.Log($"{equipmentType} 장비 해제");
        RecalculateEquipmentStats();
    }
    
    //장비스텟 계산하는 역할
    private void RecalculateEquipmentStats()
    {
        int physicalAttack = 0;
        int magicAttack = 0;

        int physicalDefense = 0;
        int magicDefense = 0;

        float criticalDamageBonus = 0f;
        float accuracy = 0f;

        AddItemStats(equippedHead);
        AddItemStats(equippedArmor);
        AddItemStats(equippedWeapon);
        AddItemStats(equippedAccessory);

        CurrentBonusStats = new EquipmentBonusStats(
            physicalAttack,
            magicAttack,
            physicalDefense,
            magicDefense,
            criticalDamageBonus,
            accuracy
        );
            
        Debug.Log(
            $"장비 스탯 합계 | " +
            $"물공: {physicalAttack} / " +
            $"마공: {magicAttack} / " +
            $"물방: {physicalDefense} / " +
            $"마방: {magicDefense} / " +
            $"치명타피해: {criticalDamageBonus} / " +
            $"명중: {accuracy}"
        );

        OnEquipmentStatsChanged?.Invoke(CurrentBonusStats);

        void AddItemStats(ItemDataSO itemData)
        {
            if (itemData == null) return;

            physicalAttack += itemData.PhysicalAttack;
            magicAttack += itemData.MagicAttack;

            physicalDefense += itemData.PhysicalDefense;
            magicDefense += itemData.MagicDefense;

            criticalDamageBonus += itemData.CriticalDamageBonus;
            accuracy += itemData.Accuracy;
        }
    }
}