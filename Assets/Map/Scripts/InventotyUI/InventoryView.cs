using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryView : MonoBehaviour
{
    //실제 인벤토리 데이터를 가지고 있는 객체
    [SerializeField] private InventoryGridManager inventoryGridManager;
    //만든 ItemSlot Prefab
    [SerializeField] private InventorySlotUI itemSlotPrefab;
    //슬롯들이 생성될 부모
    [SerializeField] private Transform itemSlotRoot;
    
    // 생성한 50개의 슬롯 UI 인스턴스 목록
    private readonly List<InventorySlotUI> slotUIs = new List<InventorySlotUI>();
    
    private void Awake()
    {
        if (inventoryGridManager == null)
        {
            inventoryGridManager = InventoryGridManager.Instance;
        }
    }

    private void Start()
    {
        if (inventoryGridManager == null)
        {
            inventoryGridManager = InventoryGridManager.Instance;
        }

        CreateSlots();
        Refresh();
    }

    private void OnEnable()
    {
        if (inventoryGridManager == null)
        {
            inventoryGridManager = InventoryGridManager.Instance;
        }

        if (inventoryGridManager != null) 
            inventoryGridManager.OnInventoryChanged += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        if (inventoryGridManager != null)
            inventoryGridManager.OnInventoryChanged -= Refresh;
    }

    private void CreateSlots()
    { 
        if (inventoryGridManager == null || itemSlotPrefab == null || itemSlotRoot == null) return;
        if (slotUIs.Count > 0) return; // 이미 생성된 경우 중복 생성 방지
        
        for (int i = 0; i < inventoryGridManager.Slots.Count; i++)
        {
            InventorySlotUI slotUI = Instantiate(itemSlotPrefab, itemSlotRoot);

            slotUIs.Add(slotUI);
        }
    }
    
    public void Refresh()
    {
        for (int i = 0; i < slotUIs.Count; i++)
        {
            slotUIs[i].SetSlot(
                inventoryGridManager.Slots[i]
            );
        }
    }
}