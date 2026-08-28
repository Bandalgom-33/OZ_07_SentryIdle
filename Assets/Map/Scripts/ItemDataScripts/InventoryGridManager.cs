using System;
using System.Collections.Generic;
using UnityEngine;

// 50칸 그리드 기반 가방 인벤토리 데이터 관리 및 세이브/로드 연동 싱글톤 매니저
public class InventoryGridManager : SingletonBase<InventoryGridManager>
{
    #region 인스펙터 바인딩 및 프로퍼티

    [Header("인벤토리 설정")]
    [Tooltip("인벤토리 최대 슬롯 수 (기본값: 50)")]
    [SerializeField] private int maxSlotCount = 50;

    [Tooltip("사전 등록된 전체 ItemDataSO 목록 (인스펙터 할당 시 Resources.LoadAll 디스크 스캔 생략)")]
    [SerializeField] private List<ItemDataSO> predefinedItemDatabase = new List<ItemDataSO>();

    private readonly List<InventorySlotData> slots = new List<InventorySlotData>();
    private readonly Dictionary<string, ItemDataSO> _itemDatabase = new Dictionary<string, ItemDataSO>();

    public IReadOnlyList<InventorySlotData> Slots => slots;
    public int MaxSlotCount => maxSlotCount;
    public event Action OnInventoryChanged;

    #endregion

    #region 라이프사이클 및 초기화

    protected override void Awake()
    {
        base.Awake();
        InitializeItemDatabase();
        InitializeSlots();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<DataSaveEvent>(OnSave);
        EventBus.Subscribe<DataLoadEvent>(OnLoad);
        EventBus.Subscribe<DataResetEvent>(OnReset);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<DataSaveEvent>(OnSave);
        EventBus.Unsubscribe<DataLoadEvent>(OnLoad);
        EventBus.Unsubscribe<DataResetEvent>(OnReset);
    }

    // ItemDataSO 데이터베이스 캐싱 (인스펙터 사전 등록 우선, 없을 시 1회 안전 로드)
    private void InitializeItemDatabase()
    {
        _itemDatabase.Clear();

        // 1. 인스펙터에 사전 바인딩된 목록이 있는 경우: 디스크 탐색 비용 없이 O(N) 즉시 매핑
        if (predefinedItemDatabase != null && predefinedItemDatabase.Count > 0)
        {
            for (int i = 0; i < predefinedItemDatabase.Count; i++)
            {
                ItemDataSO item = predefinedItemDatabase[i];
                if (item != null && !string.IsNullOrEmpty(item.ItemID))
                {
                    _itemDatabase[item.ItemID] = item;
                }
            }
            return;
        }

        // 2. 인스펙터가 비어있을 때의 안전 Fallback: Resources 1회 탐색
        ItemDataSO[] loadedItems = Resources.LoadAll<ItemDataSO>("");
        if (loadedItems != null)
        {
            for (int i = 0; i < loadedItems.Length; i++)
            {
                if (loadedItems[i] != null && !string.IsNullOrEmpty(loadedItems[i].ItemID))
                {
                    _itemDatabase[loadedItems[i].ItemID] = loadedItems[i];
                }
            }
        }
    }

    // 아이템 ID 문자열을 통해 ItemDataSO 에셋 반환 헬퍼
    public ItemDataSO GetItemById(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return null;
        if (_itemDatabase.TryGetValue(itemId, out ItemDataSO item))
        {
            return item;
        }
        return null;
    }

    // 50개 빈 슬롯 초기화
    private void InitializeSlots()
    {
        slots.Clear();
        for (int i = 0; i < maxSlotCount; i++)
        {
            slots.Add(null);
        }
    }

    #endregion

    #region 인벤토리 아이템 조작 API

    // 아이템 추가 연산 (스택 병합 ➔ 빈 슬롯 신규 등록)
    public bool AddItem(ItemDataSO itemData, int quantity)
    {
        if (itemData == null || quantity <= 0) return false;

        // 1. 같은 아이템이 이미 존재하는 슬롯에 수량 가산 시도
        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlotData slot = slots[i];
            if (slot == null) continue;
            if (slot.itemData == itemData)
            {
                int availableSpace = itemData.MaxStack - slot.quantity;
                if (availableSpace <= 0) continue;

                int amountToAdd = Mathf.Min(availableSpace, quantity);
                slot.quantity += amountToAdd;
                quantity -= amountToAdd;

                if (quantity <= 0)
                {
                    OnInventoryChanged?.Invoke();
                    SaveManager.Instance?.SaveGameData();
                    return true;
                }
            }
        }

        // 2. 남은 수량을 빈 슬롯에 신규 배치
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null) continue;
            int amountToAdd = Mathf.Min(itemData.MaxStack, quantity);

            slots[i] = new InventorySlotData(itemData, amountToAdd);
            quantity -= amountToAdd;

            if (quantity <= 0)
            {
                OnInventoryChanged?.Invoke();
                SaveManager.Instance?.SaveGameData();
                return true;
            }
        }

        // 빈 슬롯이 부족하여 추가하지 못한 경우
        return false;
    }

    // 특정 슬롯의 아이템 데이터 반환
    public InventorySlotData GetSlot(int index)
    {
        if (index >= 0 && index < slots.Count)
        {
            return slots[index];
        }
        return null;
    }

    // 가방 만석 여부 판정
    public bool IsInventoryFull()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null) return false;
        }
        return true;
    }

    // 특정 아이템 ID의 인벤토리 총 보유 수량 조회
    public int GetItemCount(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return 0;
        int total = 0;
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null && slots[i].itemData != null && slots[i].itemData.ItemID == itemId)
            {
                total += slots[i].quantity;
            }
        }
        return total;
    }

    // 특정 ItemDataSO의 인벤토리 총 보유 수량 조회
    public int GetItemCount(ItemDataSO itemData)
    {
        if (itemData == null) return 0;
        int total = 0;
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null && slots[i].itemData == itemData)
            {
                total += slots[i].quantity;
            }
        }
        return total;
    }

    // 소모품 타입별 인벤토리 총 보유 수량 조회
    public int GetConsumableCount(ConsumableType type)
    {
        int total = 0;
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null && slots[i].itemData != null &&
                slots[i].itemData.ItemCategory == ItemCategory.Consumable &&
                slots[i].itemData.ConsumableType == type)
            {
                total += slots[i].quantity;
            }
        }
        return total;
    }

    // 소모품 타입에 해당하는 ItemDataSO 조회
    public ItemDataSO GetConsumableItemData(ConsumableType type)
    {
        // 1. 인벤토리 슬롯 우선 탐색
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null && slots[i].itemData != null &&
                slots[i].itemData.ItemCategory == ItemCategory.Consumable &&
                slots[i].itemData.ConsumableType == type)
            {
                return slots[i].itemData;
            }
        }

        // 2. 캐시된 아이템 데이터베이스 탐색
        foreach (var item in _itemDatabase.Values)
        {
            if (item != null && item.ItemCategory == ItemCategory.Consumable && item.ConsumableType == type)
            {
                return item;
            }
        }
        return null;
    }

    // 특정 아이템을 지정 수량만큼 인벤토리에서 차감 소비
    public bool TrySpendItem(ItemDataSO itemData, int quantity)
    {
        if (itemData == null || quantity <= 0) return false;
        if (GetItemCount(itemData) < quantity) return false;

        int remainToSpend = quantity;
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null && slots[i].itemData == itemData)
            {
                if (slots[i].quantity <= remainToSpend)
                {
                    remainToSpend -= slots[i].quantity;
                    slots[i] = null;
                }
                else
                {
                    slots[i].quantity -= remainToSpend;
                    remainToSpend = 0;
                }

                if (remainToSpend <= 0) break;
            }
        }

        OnInventoryChanged?.Invoke();
        SaveManager.Instance?.SaveGameData();
        return true;
    }

    // 소모품 타입 아이템을 지정 수량만큼 인벤토리에서 차감 소비
    public bool TrySpendConsumable(ConsumableType type, int quantity)
    {
        if (quantity <= 0) return false;
        if (GetConsumableCount(type) < quantity) return false;

        int remainToSpend = quantity;
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null && slots[i].itemData != null &&
                slots[i].itemData.ItemCategory == ItemCategory.Consumable &&
                slots[i].itemData.ConsumableType == type)
            {
                if (slots[i].quantity <= remainToSpend)
                {
                    remainToSpend -= slots[i].quantity;
                    slots[i] = null;
                }
                else
                {
                    slots[i].quantity -= remainToSpend;
                    remainToSpend = 0;
                }

                if (remainToSpend <= 0) break;
            }
        }

        OnInventoryChanged?.Invoke();
        SaveManager.Instance?.SaveGameData();
        return true;
    }

    // 특정 아이템의 추가 수용 가능 최대 수량 계산
    public int GetAvailableCapacityForItem(ItemDataSO itemData)
    {
        if (itemData == null) return 0;

        int capacity = 0;
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null)
            {
                capacity += itemData.MaxStack;
            }
            else if (slots[i].itemData == itemData)
            {
                capacity += Mathf.Max(0, itemData.MaxStack - slots[i].quantity);
            }
        }
        return capacity;
    }

    #endregion

    #region 세이브 / 로드 연동

    // 인벤토리 세이브 데이터 저장 처리
    private void OnSave(DataSaveEvent evt)
    {
        if (evt.saveData == null) return;
        if (evt.saveData.inventory == null)
        {
            evt.saveData.inventory = new InventorySaveData();
        }

        evt.saveData.inventory.slots.Clear();
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null && slots[i].itemData != null && slots[i].quantity > 0)
            {
                evt.saveData.inventory.slots.Add(new InventorySlotSaveEntry
                {
                    slotIndex = i,
                    itemId = slots[i].itemData.ItemID,
                    quantity = slots[i].quantity
                });
            }
        }
    }

    // 인벤토리 세이브 데이터 로드 처리
    private void OnLoad(DataLoadEvent evt)
    {
        if (evt.saveData == null || evt.saveData.inventory == null) return;

        InitializeSlots();

        if (evt.saveData.inventory.slots != null)
        {
            for (int i = 0; i < evt.saveData.inventory.slots.Count; i++)
            {
                InventorySlotSaveEntry entry = evt.saveData.inventory.slots[i];
                if (entry != null && entry.slotIndex >= 0 && entry.slotIndex < maxSlotCount)
                {
                    ItemDataSO itemSO = GetItemById(entry.itemId);
                    if (itemSO != null)
                    {
                        slots[entry.slotIndex] = new InventorySlotData(itemSO, entry.quantity);
                    }
                }
            }
        }

        OnInventoryChanged?.Invoke();
    }

    // 인벤토리 데이터 초기화 처리
    private void OnReset(DataResetEvent evt)
    {
        InitializeSlots();
        OnInventoryChanged?.Invoke();
    }

    #endregion
}
