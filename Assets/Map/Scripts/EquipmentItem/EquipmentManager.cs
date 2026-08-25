using System;
using UnityEngine;
using System.Collections.Generic;

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
    
    private readonly Dictionary<string, CharacterEquipmentData> characterEquipments  = new Dictionary<string, CharacterEquipmentData>();

    private string currentUnitId;
    

    public void TestSelectLuka()
    {
        SetCurrentUnit("UNIT_0002");
    }

    public void TestSelectKimHajin()
    {
        SetCurrentUnit("UNIT_0004");
    }
    
    
    
    public void EquipItem(ItemDataSO itemData)
    {
        if (itemData == null) return;
        if (string.IsNullOrEmpty(currentUnitId)) return;
        //현재 캐릭터 데이터 가져오기
        if (!characterEquipments.TryGetValue(currentUnitId, out CharacterEquipmentData characterData)) return;
        
        ItemDataSO previousItem = null;

        switch (itemData.EquipmentType)
        {
            case EquipmentType.Head:
                previousItem = equippedHead;
                equippedHead = itemData;
                characterData.Head = itemData;
                headSlot.SetItem(itemData);
                break;

            case EquipmentType.Armor:
                previousItem = equippedArmor;
                equippedArmor = itemData;
                characterData.Armor = itemData;
                armorSlot.SetItem(itemData);
                break;

            case EquipmentType.Weapon:
                previousItem = equippedWeapon;
                equippedWeapon = itemData;
                characterData.Weapon = itemData;
                weaponSlot.SetItem(itemData);
                break;

            case EquipmentType.Accessory:
                previousItem = equippedAccessory;
                equippedAccessory = itemData;
                characterData.Accessory = itemData;
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
        
        if (string.IsNullOrEmpty(currentUnitId)) return;

        if (!characterEquipments.TryGetValue( currentUnitId, out CharacterEquipmentData characterData)) return;
        
        
        switch (equipmentType)
        {
            
            case EquipmentType.Head:
                equippedHead = null;
                characterData.Head = null;
                headSlot.SetItem(null);
                break;

            case EquipmentType.Armor:
                equippedArmor = null;
                characterData.Armor = null;
                armorSlot.SetItem(null);
                break;

            case EquipmentType.Weapon:
                equippedWeapon = null;
                characterData.Weapon = null;
                weaponSlot.SetItem(null);
                break;

            case EquipmentType.Accessory:
                equippedAccessory = null;
                characterData.Accessory = null;
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
    
    //현재 어떤 캐릭터의 장비창을 보고 있는지?
    public void SetCurrentUnit(string unitId)
    {
        if (string.IsNullOrEmpty(unitId)) return;

        currentUnitId = unitId;

        if (!characterEquipments.ContainsKey(unitId))
        {
            characterEquipments.Add(
                unitId,
                new CharacterEquipmentData(unitId)
            );
        }

        LoadCurrentUnitEquipment();
    }
    
    //장비 데이터를 불러오는 역할
    private void LoadCurrentUnitEquipment()
    {
        if (string.IsNullOrEmpty(currentUnitId)) return;

        if (!characterEquipments.TryGetValue(
                currentUnitId,
                out CharacterEquipmentData data))
        {
            return;
        }

        equippedHead = data.Head;
        equippedArmor = data.Armor;
        equippedWeapon = data.Weapon;
        equippedAccessory = data.Accessory;

        headSlot.SetItem(equippedHead);
        armorSlot.SetItem(equippedArmor);
        weaponSlot.SetItem(equippedWeapon);
        accessorySlot.SetItem(equippedAccessory);

        RecalculateEquipmentStats();
    }
    
}