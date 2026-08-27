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
    
    //장착버튼
    [SerializeField] private GameObject equipButton;
    //장착 해제 버튼
    [SerializeField] private GameObject unequipButton;
    
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

        UpdateButtonState();
    }
    
    private EquipmentManager GetEquipmentManager()
    {
        if (equipmentManager != null) return equipmentManager;
        return EquipmentManager.Instance;
    }

    public void EquipCurrentItem()
    {
        if (currentItem == null) return;
        EquipmentManager em = GetEquipmentManager();
        if (em == null) return;

        em.EquipItem(currentItem);

        UpdateButtonState();
    }
    
    //장착 중인지 확인 하는 메서드
    private bool IsEquipped(ItemDataSO itemData)
    {
        if (itemData == null) return false;
        EquipmentManager em = GetEquipmentManager();
        if (em == null) return false;

        switch (itemData.EquipmentType)
        {
            case EquipmentType.Head:
                return em.EquippedHead == itemData;

            case EquipmentType.Armor:
                return em.EquippedArmor == itemData;

            case EquipmentType.Weapon:
                return em.EquippedWeapon == itemData;

            case EquipmentType.Accessory:
                return em.EquippedAccessory == itemData;
        }

        return false;
    }
    
    private void UpdateButtonState()
    {
        bool isEquipped = IsEquipped(currentItem);

        if (equipButton != null) equipButton.SetActive(!isEquipped);
        if (unequipButton != null) unequipButton.SetActive(isEquipped);
    }
    
    public void UnequipCurrentItem()
    {
        if (currentItem == null) return;
        EquipmentManager em = GetEquipmentManager();
        if (em == null) return;

        em.UnequipItem(currentItem.EquipmentType);

        UpdateButtonState();

        Debug.Log($"{currentItem.ItemName} 장착 해제");
    }

    public void Close()
    {
        detailPanel.SetActive(false);
    }
}