using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 아이템 상세 정보 표시 및 소모품 사용 컨트롤러
public class ItemDetailUI : MonoBehaviour
{
    [Header("--- UI 패널 ---")]
    [Tooltip("아이템 상세 정보 팝업 패널")]
    [SerializeField] private GameObject detailPanel;

    [Header("--- 아이템 정보 UI ---")]
    [Tooltip("아이템 아이콘 이미지")]
    [SerializeField] private Image itemIcon;

    [Tooltip("아이템 명칭 텍스트")]
    [SerializeField] private TMP_Text itemNameText;

    [Tooltip("아이템 설명 텍스트")]
    [SerializeField] private TMP_Text categoryText;

    [Tooltip("보조 정보 텍스트")]
    [SerializeField] private TMP_Text equipmentTypeText;
    
    [Header("--- 매니저 참조 ---")]
    [Tooltip("장비 관리 매니저 참조")]
    [SerializeField] private EquipmentManager equipmentManager;
    
    [Header("--- 조작 버튼 ---")]
    [Tooltip("장착 또는 사용 버튼")]
    [SerializeField] private GameObject equipButton;

    [Tooltip("장착 해제 버튼")]
    [SerializeField] private GameObject unequipButton;
    
    private ItemDataSO currentItem;

    // 아이템 상세 창 표시 및 정보 바인딩
    public void ShowItem(ItemDataSO itemData)
    {
        if (itemData == null) return;

        currentItem = itemData;

        detailPanel.SetActive(true);

        itemIcon.sprite = itemData.ItemIcon;
        itemNameText.text = itemData.ItemName;

        string desc = !string.IsNullOrEmpty(itemData.Description) 
            ? itemData.Description 
            : $"[{itemData.ItemCategory}] {itemData.EquipmentType}";

        if (categoryText != null)
        {
            categoryText.text = desc;
        }

        if (equipmentTypeText != null)
        {
            equipmentTypeText.text = string.Empty;
        }

        UpdateButtonState();
    }
    
    // 장비 매니저 참조 반환
    private EquipmentManager GetEquipmentManager()
    {
        if (equipmentManager != null) return equipmentManager;
        return EquipmentManager.Instance;
    }

    // 아이템 장착 또는 경험치책 사용 처리
    public void EquipCurrentItem()
    {
        if (currentItem == null) return;

        if (currentItem.ItemCategory == ItemCategory.Consumable && IsExpBook(currentItem.ConsumableType))
        {
            EquipmentManager em = GetEquipmentManager();
            string targetUnitId = em != null ? em.CurrentUnitId : "UNIT_0002";

            if (ConsumableItemManager.Instance != null)
            {
                bool success = ConsumableItemManager.Instance.UseExpBook(currentItem.ConsumableType, targetUnitId);
                if (success)
                {
                    Debug.Log($"[ItemDetailUI] {currentItem.ItemName} 사용 완료 -> 유닛[{targetUnitId}] 경험치 부여");
                }
            }

            UpdateButtonState();
            return;
        }

        EquipmentManager manager = GetEquipmentManager();
        if (manager == null) return;

        manager.EquipItem(currentItem);
        UpdateButtonState();
    }
    
    // 아이템 장착 여부 확인
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
    
    // 버튼 텍스트 라벨 갱신
    private void SetButtonLabel(GameObject buttonObj, string label)
    {
        if (buttonObj == null) return;
        TMP_Text tmp = buttonObj.GetComponentInChildren<TMP_Text>();
        if (tmp != null)
        {
            tmp.text = label;
        }
    }

    // 버튼 표시 상태 및 상호작용 갱신
    private void UpdateButtonState()
    {
        if (currentItem == null) return;

        if (currentItem.ItemCategory == ItemCategory.Consumable && IsHealthPotion(currentItem.ConsumableType))
        {
            if (equipButton != null) equipButton.SetActive(false);
            if (unequipButton != null) unequipButton.SetActive(false);
            return;
        }

        if (currentItem.ItemCategory == ItemCategory.Consumable && IsExpBook(currentItem.ConsumableType))
        {
            int count = ConsumableItemManager.Instance != null 
                ? ConsumableItemManager.Instance.GetItemCount(currentItem.ConsumableType) 
                : 0;

            if (equipButton != null)
            {
                equipButton.SetActive(true);
                SetButtonLabel(equipButton, "사용");

                Button btn = equipButton.GetComponent<Button>();
                if (btn != null) btn.interactable = count > 0;
            }

            if (unequipButton != null) unequipButton.SetActive(false);
            return;
        }

        bool isEquipped = IsEquipped(currentItem);

        if (equipButton != null)
        {
            equipButton.SetActive(!isEquipped);
            SetButtonLabel(equipButton, "장착");
            Button btn = equipButton.GetComponent<Button>();
            if (btn != null) btn.interactable = true;
        }

        if (unequipButton != null)
        {
            unequipButton.SetActive(isEquipped);
            SetButtonLabel(unequipButton, "해제");
        }
    }
    
    // 장비 아이템 장착 해제 처리
    public void UnequipCurrentItem()
    {
        if (currentItem == null) return;
        EquipmentManager em = GetEquipmentManager();
        if (em == null) return;

        em.UnequipItem(currentItem.EquipmentType);
        UpdateButtonState();

        Debug.Log($"{currentItem.ItemName} 장착 해제");
    }

    // 체력 포션 타입 판별
    private bool IsHealthPotion(ConsumableType type)
    {
        return type == ConsumableType.HealthPotion_Low ||
               type == ConsumableType.HealthPotion_Mid ||
               type == ConsumableType.HealthPotion_High;
    }

    // 경험치책 타입 판별
    private bool IsExpBook(ConsumableType type)
    {
        return type == ConsumableType.ExpBook_Low ||
               type == ConsumableType.ExpBook_Mid ||
               type == ConsumableType.ExpBook_High;
    }

    // 상세 정보 창 닫기
    public void Close()
    {
        detailPanel.SetActive(false);
    }
}