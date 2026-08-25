using System;

[Serializable]
public class CharacterEquipmentData
{
    public string UnitId;

    public ItemDataSO Head;
    public ItemDataSO Armor;
    public ItemDataSO Weapon;
    public ItemDataSO Accessory;

    public CharacterEquipmentData(string unitId)
    {
        UnitId = unitId;

        Head = null;
        Armor = null;
        Weapon = null;
        Accessory = null;
    }
}