using UnityEngine;

public class InventoryTest : MonoBehaviour
{
    [SerializeField] private InventoryGridManager inventoryGridManager;

    [Header("장비 테스트")]
    [SerializeField] private ItemDataSO headItem;
    [SerializeField] private ItemDataSO armorItem;
    [SerializeField] private ItemDataSO weaponItem;
    [SerializeField] private ItemDataSO accessoryItem;

    private void Start()
    {
        if (inventoryGridManager == null) return;

        inventoryGridManager.AddItem(headItem, 1);
        inventoryGridManager.AddItem(armorItem, 1);
        inventoryGridManager.AddItem(weaponItem, 1);
        inventoryGridManager.AddItem(accessoryItem, 1);
    }
}