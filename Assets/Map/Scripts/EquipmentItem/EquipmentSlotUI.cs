using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class EquipmentSlotUI : MonoBehaviour
{
    [Header("이 슬롯의 장비 타입")]
    [SerializeField] private EquipmentType equipmentType;

    [Header("장착 아이템 표시")]
    [SerializeField] private Image itemIcon;

    private ItemDataSO equippedItem;
    private Button slotButton;
    private ItemDetailUI itemDetailUI;

    public EquipmentType EquipmentType => equipmentType;
    public ItemDataSO EquippedItem => equippedItem;

    private void Awake()
    {
        slotButton = GetComponent<Button>();

        slotButton.onClick.AddListener(OnClickSlot);

        itemDetailUI = FindFirstObjectByType<ItemDetailUI>();
    }

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

    private void OnClickSlot()
    {
        // 장착된 아이템이 없으면 아무것도 하지 않음
        if (equippedItem == null) return;

        if (itemDetailUI == null) return;

        itemDetailUI.ShowItem(equippedItem);
    }
}