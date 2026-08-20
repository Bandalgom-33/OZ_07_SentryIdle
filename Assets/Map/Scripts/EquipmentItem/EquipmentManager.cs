using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    [Header("장비 슬롯")]
    [SerializeField] private EquipmentSlotUI headSlot;
    [SerializeField] private EquipmentSlotUI armorSlot;
    [SerializeField] private EquipmentSlotUI weaponSlot;
    [SerializeField] private EquipmentSlotUI accessorySlot;

    public void EquipItem(ItemDataSO itemData)
    {
        if (itemData == null) return;

        switch (itemData.EquipmentType)
        {
            case EquipmentType.Head:
                headSlot.SetItem(itemData);
                break;

            case EquipmentType.Armor:
                armorSlot.SetItem(itemData);
                break;

            case EquipmentType.Weapon:
                weaponSlot.SetItem(itemData);
                break;

            case EquipmentType.Accessory:
                accessorySlot.SetItem(itemData);
                break;
        }
    }
}