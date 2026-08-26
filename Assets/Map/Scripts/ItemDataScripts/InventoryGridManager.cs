using System;
using UnityEngine;
using System.Collections.Generic;

public class InventoryGridManager : MonoBehaviour
{
    
    
    [Header("인벤토리 설정")] 
    //50개로 일단 고정
    [SerializeField] private int maxSlotCount = 50;
    
    private List<InventorySlotData> slots = new List<InventorySlotData>();
    
    public IReadOnlyList<InventorySlotData> Slots => slots;
    public event Action OnInventoryChanged;
    
    private void Awake()
    {
        InitializeSlots();
    }

    private void InitializeSlots()
    {
        slots.Clear();

        for (int i = 0; i < maxSlotCount; i++)
        {
            slots.Add(null);
        }
    }

    public bool AddItem(ItemDataSO itemData, int quantity)
    {
        if (itemData == null || quantity <= 0) return false;
        
        //같은 아이템이 이미 있는 슬롯 찾기
        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlotData slot = slots[i];
            
            if(slot == null) continue;
            if (slot.itemData == itemData)
            {
                //현재 슬롯에 몇개가 더 들어가는지 계산
                int availableSpace = itemData.MaxStack - slot.quantity;
                if (availableSpace <= 0) continue;
                //실제로 넣을 수 있는 만큼만 넣게만듬
                int amountToAdd = Mathf.Min(availableSpace, quantity);
                slot.quantity += amountToAdd;
                quantity -= amountToAdd;
                if (quantity <= 0)
                {
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }
        }
        
        // 같은 아이템 없으면 빈 슬롯 찾기
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null) continue;
            int amountToAdd = Mathf.Min(itemData.MaxStack, quantity);

            slots[i] = new InventorySlotData(itemData, amountToAdd);
            quantity -= amountToAdd;

            if (quantity <= 0)
            {
                OnInventoryChanged?.Invoke();
                return true;
            }
            
        }
        //빈 슬롯도 없으면 패스
        return false;
    }

    public bool IsInventoryFull()
    {
        //빈칸 찾기
        for (int i = 0; i < slots.Count; i++)
        {
            //빈칸이 존재하면 false반환
            if(slots[i] ==  null) return false;
        }
        //없으면 true
        return true;
    }
    
}
