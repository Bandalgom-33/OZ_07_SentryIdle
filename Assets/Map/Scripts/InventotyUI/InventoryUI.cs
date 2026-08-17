using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private GameObject inventoryPanel;

    //인벤 열기
    public void OpenInventory()
    {
        inventoryPanel.SetActive(true);
    }

    //닫기 
    public void CloseInventory()
    {
        inventoryPanel.SetActive(false);
    }
}
