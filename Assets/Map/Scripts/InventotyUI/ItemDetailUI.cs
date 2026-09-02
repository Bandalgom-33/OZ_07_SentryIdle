using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 아이템 상세 정보 표시 및 장착/사용 관리 UI 컴포넌트
public class ItemDetailUI : MonoBehaviour
{
    #region 싱글톤 인스턴스

    public static ItemDetailUI Instance { get; private set; }

    #endregion

    #region 직렬화 변수

    [Header("--- 패널 참조 ---")]
    [Tooltip("아이템 상세 정보 팝업 패널 오브젝트")]
    [SerializeField] private GameObject detailPanel;

    [Header("--- 아이템 정보 UI ---")]
    [Tooltip("아이템 아이콘 이미지")]
    [SerializeField] private Image itemIcon;

    [Tooltip("아이템 명칭 텍스트")]
    [SerializeField] private TMP_Text itemNameText;

    [Tooltip("아이템 설명 텍스트")]
    [SerializeField] private TMP_Text itemDescriptionText;

    [Tooltip("장비 장착 부위 텍스트")]
    [SerializeField] private TMP_Text equipmentTypeText;

    [Tooltip("장비 장착 부위 표시용 오브젝트 그룹 (장비 아이템일 때만 활성화)")]
    [SerializeField] private GameObject equipmentTypeGroup;

    [Header("--- 조작 버튼 ---")]
    [Tooltip("사용 또는 장착 버튼 오브젝트")]
    [SerializeField] private GameObject equipButton;

    [Tooltip("장비 장착 해제 버튼 오브젝트")]
    [SerializeField] private GameObject unequipButton;

    [Header("--- 매니저 참조 ---")]
    [Tooltip("장비 관리 매니저 참조")]
    [SerializeField] private EquipmentManager equipmentManager;

    #endregion

    private ItemDataSO currentItem;

    #region 라이프사이클

    // 싱글톤 인스턴스 초기화 및 버튼 클릭 리스너 자동 바인딩
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        BindButtonListeners();
    }

    // 인스펙터 버튼 컴포넌트 이벤트 자동 등록
    private void BindButtonListeners()
    {
        if (equipButton != null)
        {
            Button btn = equipButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveListener(EquipCurrentItem);
                btn.onClick.AddListener(EquipCurrentItem);
            }
        }

        if (unequipButton != null)
        {
            Button btn = unequipButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveListener(UnequipCurrentItem);
                btn.onClick.AddListener(UnequipCurrentItem);
            }
        }
    }

    // 싱글톤 참조 해제
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    #endregion

    // 아이템 상세 정보 팝업 표시 및 데이터 바인딩
    public void ShowItem(ItemDataSO itemData)
    {
        if (itemData == null) return;

        currentItem = itemData;

        if (detailPanel != null)
        {
            detailPanel.SetActive(true);
        }

        if (itemIcon != null)
        {
            itemIcon.sprite = itemData.ItemIcon;
        }

        if (itemNameText != null)
        {
            itemNameText.text = itemData.ItemName;
        }

        if (itemDescriptionText != null)
        {
            itemDescriptionText.text = itemData.Description;
        }

        UpdateEquipmentTypeDisplay(itemData);
        UpdateButtonState();
    }

    // 장비 장착 부위 UI 표시 갱신
    private void UpdateEquipmentTypeDisplay(ItemDataSO itemData)
    {
        bool isEquipment = itemData.ItemCategory == ItemCategory.Equipment;

        if (equipmentTypeGroup != null)
        {
            equipmentTypeGroup.SetActive(isEquipment);
        }

        if (equipmentTypeText != null)
        {
            if (isEquipment)
            {
                equipmentTypeText.gameObject.SetActive(true);
                equipmentTypeText.text = GetEquipmentTypeName(itemData.EquipmentType);
            }
            else
            {
                equipmentTypeText.text = string.Empty;
                if (equipmentTypeGroup == null)
                {
                    equipmentTypeText.gameObject.SetActive(false);
                }
            }
        }
    }

    // 장비 부위 타입의 한글 명칭 변환
    private string GetEquipmentTypeName(EquipmentType type)
    {
        switch (type)
        {
            case EquipmentType.Head:
                return "머리";
            case EquipmentType.Armor:
                return "갑옷";
            case EquipmentType.Weapon:
                return "무기";
            case EquipmentType.Accessory:
                return "장신구";
            default:
                return type.ToString();
        }
    }

    // 장비 매니저 인스턴스 반환
    private EquipmentManager GetEquipmentManager()
    {
        if (equipmentManager != null) return equipmentManager;
        return EquipmentManager.Instance;
    }

    // 현재 아이템 장착 또는 소모품 사용 처리
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

    // 현재 아이템 장착 상태 확인
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

    // 버튼 라벨 텍스트 변경
    private void SetButtonLabel(GameObject buttonObj, string label)
    {
        if (buttonObj == null) return;
        TMP_Text tmp = buttonObj.GetComponentInChildren<TMP_Text>();
        if (tmp != null)
        {
            tmp.text = label;
        }
    }

    // 아이템 종류 및 상태에 따른 버튼 활성화 갱신
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

    // 장착 중인 장비 아이템 해제 처리
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

    // 아이템 상세 패널 닫기
    public void Close()
    {
        if (detailPanel != null)
        {
            detailPanel.SetActive(false);
        }
    }
}