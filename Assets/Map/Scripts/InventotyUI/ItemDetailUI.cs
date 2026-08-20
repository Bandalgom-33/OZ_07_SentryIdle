using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemDetailUI : MonoBehaviour
{
    [SerializeField] private GameObject detailPanel;

    [Header("아이템 정보 UI")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text categoryText;
    [SerializeField] private TMP_Text equipmentTypeText;
    
    //참조
    [SerializeField] private EquipmentManager equipmentManager;
    
    private ItemDataSO currentItem;

    public void ShowItem(ItemDataSO itemData)
    {
        if (itemData == null) return;

        currentItem = itemData;

        detailPanel.SetActive(true);

        itemIcon.sprite = itemData.ItemIcon;
        itemNameText.text = itemData.ItemName;
        categoryText.text = itemData.ItemCategory.ToString();
        equipmentTypeText.text = itemData.EquipmentType.ToString();
    }
    
    public void EquipCurrentItem()
    {
        if (currentItem == null) return;
        if (equipmentManager == null) return;

        equipmentManager.EquipItem(currentItem);

        Debug.Log($"{currentItem.ItemName} 장착 완료");
    }
    

    public void Close()
    {
        detailPanel.SetActive(false);
    }
}