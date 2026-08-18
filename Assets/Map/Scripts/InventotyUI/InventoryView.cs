using UnityEngine;

public class InventoryView : MonoBehaviour
{
    //실제 인벤토리 데이터를 가지고 있는 객체
    [SerializeField] private InventoryGridManager inventoryGridManager;
    //만든 ItemSlot Prefab
    [SerializeField] private InventorySlotUI itemSlotPrefab;
    //슬롯들이 생성될 부모
    [SerializeField] private Transform itemSlotRoot;
    
    private void Start()
    {
        CreateSlots();
    }

    private void CreateSlots()
    { 
        if (inventoryGridManager == null || itemSlotPrefab == null || itemSlotRoot == null)
        {
            Debug.LogError("InventoryView 참조가 연결되지 않았습니다.");
            return;
        }
        
        InventorySlotUI slotUI = Instantiate(itemSlotPrefab, itemSlotRoot);

        slotUI.SetSlot(inventoryGridManager.Slots[0]);
    }
}