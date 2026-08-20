using UnityEngine;

public class InventoryTest : MonoBehaviour
{
    [SerializeField] private InventoryGridManager inventoryGridManager;
    [SerializeField] private ItemDataSO testItem;

    private void Start()
    {
        if (inventoryGridManager == null) return;
        if (testItem == null) return;

        inventoryGridManager.AddItem(testItem, 1);
    }
}