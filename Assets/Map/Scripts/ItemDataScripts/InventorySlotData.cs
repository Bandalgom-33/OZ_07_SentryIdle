using System;
using UnityEngine;

[Serializable]
public class InventorySlotData
{
   public ItemDataSO itemData;
   public int quantity;

   //어떤 아이템을 보유중인지, 몇개를 가지고 있는지를 저장
   public InventorySlotData(ItemDataSO itemData, int quantity)
   {
      this.itemData = itemData;
      this.quantity = quantity;
   }
    
}
