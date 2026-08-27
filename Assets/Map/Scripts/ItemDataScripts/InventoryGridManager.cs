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

    // Resources 경로 내 전체 ItemDataSO 에셋 캐싱
    private void InitializeItemDatabase()
    {
        _itemDatabase.Clear();
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
