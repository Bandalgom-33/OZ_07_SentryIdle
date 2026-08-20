using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class InventorySlotUI : MonoBehaviour
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text quantityText;
    [SerializeField] private ItemDetailUI itemDetailUI;

    //현재 이 UI 슬롯이 가지고 있는 아이템 데이터
    private InventorySlotData currentSlotData;

    private Button slotButton;

    private void Awake()
    {
        slotButton = GetComponent<Button>();
        slotButton.onClick.AddListener(OnClickSlot);

        itemDetailUI = FindFirstObjectByType<ItemDetailUI>();
    }

    public void SetSlot(InventorySlotData slotData)
    {
        //현재 슬롯 데이터 저장
        currentSlotData = slotData;

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
    

    private void OnClickSlot()
    {
        if (currentSlotData == null) return;
        if (currentSlotData.itemData == null) return;
        if (itemDetailUI == null) return;

        itemDetailUI.ShowItem(currentSlotData.itemData);
    }
}