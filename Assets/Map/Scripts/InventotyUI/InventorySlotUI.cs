using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 인벤토리 그리드 개별 슬롯 UI 컴포넌트
[RequireComponent(typeof(Button))]
public class InventorySlotUI : MonoBehaviour
{
    #region 직렬화 변수

    [Tooltip("아이템 아이콘 렌더링 이미지")]
    [SerializeField] private Image itemIcon;

    [Tooltip("아이템 보유 수량 표시 텍스트")]
    [SerializeField] private TMP_Text quantityText;

    [Tooltip("아이템 상세 정보 팝업 컴포넌트 참조")]
    [SerializeField] private ItemDetailUI itemDetailUI;

    #endregion

    #region 내부 변수 및 프로퍼티

    private InventorySlotData currentSlotData;
    private Button slotButton;

    public InventorySlotData CurrentSlotData => currentSlotData;

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

    #region 슬롯 데이터 설정 및 클릭 처리

    // 슬롯 데이터 바인딩 및 UI 갱신
    public void SetSlot(InventorySlotData slotData)
    {
        currentSlotData = slotData;

        if (slotData == null || slotData.itemData == null)
        {
            if (itemIcon != null)
            {
                itemIcon.enabled = false;
                itemIcon.sprite = null;
            }

            if (quantityText != null)
            {
                quantityText.text = string.Empty;
            }
            return;
        }

        if (itemIcon != null)
        {
            itemIcon.enabled = true;
            itemIcon.sprite = slotData.itemData.ItemIcon;
        }

        if (quantityText != null)
        {
            quantityText.text = slotData.quantity > 1 ? slotData.quantity.ToString() : string.Empty;
        }
    }

    // 슬롯 클릭 시 아이템 상세 팝업 표시
    private void OnClickSlot()
    {
        if (currentSlotData == null || currentSlotData.itemData == null) return;

        ItemDetailUI detailUI = itemDetailUI != null ? itemDetailUI : (ItemDetailUI.Instance != null ? ItemDetailUI.Instance : FindFirstObjectByType<ItemDetailUI>());
        if (detailUI == null) return;

        detailUI.ShowItem(currentSlotData.itemData);
    }

    #endregion
}