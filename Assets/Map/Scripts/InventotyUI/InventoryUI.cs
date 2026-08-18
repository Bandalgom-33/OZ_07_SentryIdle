using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private GameObject equipmentInventoryPanel;
    [SerializeField] private GameObject simpleInventoryPanel;

    // 가방만 열기
    public void OpenBagInventory()
    {
        //동시에 열리는걸 방지
        equipmentInventoryPanel.SetActive(false);
        simpleInventoryPanel.SetActive(true);
    }

    public void CloseBagInventory()
    {
        simpleInventoryPanel.SetActive(false);
    }

    // 장비창 + 가방 열기
    public void OpenEquipmentInventory()
    {
        //동시에 열리는걸 방지
        simpleInventoryPanel.SetActive(false);
        equipmentInventoryPanel.SetActive(true);
    }

    public void CloseEquipmentInventory()
    {
        equipmentInventoryPanel.SetActive(false);
    }
}