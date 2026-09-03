using UnityEngine;
using UnityEngine.UI;

// 캐릭터 장비 4부위 슬롯 UI 컴포넌트
[RequireComponent(typeof(Button))]
public class EquipmentSlotUI : MonoBehaviour
{
    #region 직렬화 변수

    [Header("--- 슬롯 설정 ---")]
    [Tooltip("이 슬롯에 대응하는 장비 부위 타입 (Head, Armor, Weapon, Accessory)")]
    [SerializeField] private EquipmentType equipmentType;

    [Header("--- UI 바인딩 ---")]
    [Tooltip("장착된 장비 아이콘 이미지")]
    [SerializeField] private Image itemIcon;

    [Tooltip("아이템 상세 정보 팝업 컴포넌트 참조")]
    [SerializeField] private ItemDetailUI itemDetailUI;

    #endregion

    #region 내부 변수 및 프로퍼티

    private ItemDataSO equippedItem;
    private Button slotButton;

    public EquipmentType EquipmentType => equipmentType;
    public ItemDataSO EquippedItem => equippedItem;

    #endregion

    #region 라이프사이클

    // 컴포넌트 초기화 및 버튼 클릭 리스너 등록
    private void Awake()
    {
        slotButton = GetComponent<Button>();
        if (slotButton != null)
        {
            slotButton.onClick.AddListener(OnClickSlot);
        }

        if (itemDetailUI == null)
        {
            itemDetailUI = ItemDetailUI.Instance != null ? ItemDetailUI.Instance : FindFirstObjectByType<ItemDetailUI>();
        }
    }

    #endregion

    #region 장비 아이템 설정 및 클릭 처리

    // 장착 아이템 데이터 바인딩 및 UI 갱신
    public void SetItem(ItemDataSO itemData)
    {
        equippedItem = itemData;

        if (itemData == null)
        {
            if (itemIcon != null)
            {
                itemIcon.enabled = false;
                itemIcon.sprite = null;
            }
            return;
        }

        if (itemIcon != null)
        {
            itemIcon.enabled = true;
            itemIcon.sprite = itemData.ItemIcon;
        }
    }

    // 장비 슬롯 클릭 시 아이템 상세 팝업 표시
    private void OnClickSlot()
    {
        if (equippedItem == null) return;

        ItemDetailUI detailUI = itemDetailUI != null ? itemDetailUI : (ItemDetailUI.Instance != null ? ItemDetailUI.Instance : FindFirstObjectByType<ItemDetailUI>());
        if (detailUI == null) return;

        detailUI.ShowItem(equippedItem);
    }

    #endregion
}