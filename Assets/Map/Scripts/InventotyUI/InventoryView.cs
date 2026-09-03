using System.Collections.Generic;
using UnityEngine;

// 가방 내 50칸 그리드 슬롯 UI 생성 및 실시간 동기화 뷰 컴포넌트
public class InventoryView : MonoBehaviour
{
    #region 직렬화 변수

    [Header("--- 데이터 및 슬롯 프리팹 ---")]
    [Tooltip("인벤토리 데이터 매니저 참조")]
    [SerializeField] private InventoryGridManager inventoryGridManager;

    [Tooltip("인벤토리 개별 슬롯 프리팹")]
    [SerializeField] private InventorySlotUI itemSlotPrefab;

    [Tooltip("슬롯 인스턴스들이 배치될 부모 트랜스폼 (GridLayoutGroup 부착 오브젝트)")]
    [SerializeField] private Transform itemSlotRoot;

    #endregion

    #region 내부 변수

    private readonly List<InventorySlotUI> slotUIs = new List<InventorySlotUI>();

    #endregion

    #region 라이프사이클

    // 매니저 인스턴스 초기화
    private void Awake()
    {
        if (inventoryGridManager == null)
        {
            inventoryGridManager = InventoryGridManager.Instance;
        }
    }

    // 초기 슬롯 생성 및 최초 갱신
    private void Start()
    {
        EnsureManagerReference();
        CreateSlots();
        Refresh();
    }

    // 패널 활성화 시 이벤트 구독 및 새로고침
    private void OnEnable()
    {
        EnsureManagerReference();

        if (inventoryGridManager != null)
        {
            inventoryGridManager.OnInventoryChanged -= Refresh;
            inventoryGridManager.OnInventoryChanged += Refresh;
        }

        CreateSlots();
        Refresh();
    }

    // 패널 비활성화 시 이벤트 구독 해제
    private void OnDisable()
    {
        if (inventoryGridManager != null)
        {
            inventoryGridManager.OnInventoryChanged -= Refresh;
        }
    }

    #endregion

    #region 슬롯 생성 및 갱신 로직

    // 인벤토리 매니저 참조 확보
    private void EnsureManagerReference()
    {
        if (inventoryGridManager == null)
        {
            inventoryGridManager = InventoryGridManager.Instance != null ? InventoryGridManager.Instance : FindFirstObjectByType<InventoryGridManager>();
        }
    }

    // 그리드 슬롯 인스턴스 생성
    private void CreateSlots()
    {
        EnsureManagerReference();
        if (inventoryGridManager == null || itemSlotPrefab == null || itemSlotRoot == null) return;
        if (slotUIs.Count > 0) return;

        for (int i = 0; i < inventoryGridManager.Slots.Count; i++)
        {
            InventorySlotUI slotUI = Instantiate(itemSlotPrefab, itemSlotRoot);
            slotUIs.Add(slotUI);
        }
    }

    // 전체 슬롯 UI 데이터 동기화 갱신
    public void Refresh()
    {
        EnsureManagerReference();
        if (inventoryGridManager == null) return;

        if (slotUIs.Count == 0)
        {
            CreateSlots();
        }

        int count = Mathf.Min(slotUIs.Count, inventoryGridManager.Slots.Count);
        for (int i = 0; i < count; i++)
        {
            slotUIs[i].SetSlot(inventoryGridManager.Slots[i], i);
        }
    }

    #endregion
}