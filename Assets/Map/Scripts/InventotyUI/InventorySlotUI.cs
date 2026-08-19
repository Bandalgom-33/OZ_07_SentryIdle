using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlotUI : MonoBehaviour
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text quantityText;

    public void SetSlot(InventorySlotData slotData)
    {
        if (slotData == null)
        {
            itemIcon.enabled = false;
            quantityText.text = "";
            return;
        }

        itemIcon.enabled = true;
        itemIcon.sprite = slotData.itemData.ItemIcon;
        quantityText.text = slotData.quantity.ToString();

    }


}
