using UnityEngine;
using UnityEngine.UI;

public class EquipmentSlotUI : MonoBehaviour
{
    [Header("이 슬롯의 장비 타입")]
    [SerializeField] private EquipmentType equipmentType;

    [Header("장착 아이템 표시")]
    [SerializeField] private Image itemIcon;

    private ItemDataSO equippedItem;

    public EquipmentType EquipmentType => equipmentType;
    public ItemDataSO EquippedItem => equippedItem;

    public void SetItem(ItemDataSO itemData)
    {
        equippedItem = itemData;

        if (itemData == null)
        {
            itemIcon.enabled = false;
            itemIcon.sprite = null;
            return;
        }

        itemIcon.enabled = true;
        itemIcon.sprite = itemData.ItemIcon;
    }
}