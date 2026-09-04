using UnityEngine;

// 통합 인벤토리 패널 내 가방 모드 및 캐릭터 장비창 모드 관리 UI 컨트롤러
public class InventoryUI : MonoBehaviour
{
    #region 싱글톤 인스턴스

    public static InventoryUI Instance { get; private set; }

    #endregion

    #region 직렬화 변수

    [Header("--- 패널 및 윈도우 참조 ---")]
    [Tooltip("통합 인벤토리 전체 패널 루트 오브젝트")]
    [SerializeField] private GameObject inventoryPanelRoot;

    [Tooltip("장비창 서브 윈도우 오브젝트 (캐릭터 선택 시 활성화)")]
    [SerializeField] private GameObject equipmentWindow;

    [Tooltip("인벤토리 가방 서브 윈도우 오브젝트")]
    [SerializeField] private GameObject inventoryWindow;

    [Header("--- 컴포넌트 참조 ---")]
    [Tooltip("인벤토리 슬롯 뷰 컴포넌트")]
    [SerializeField] private InventoryView inventoryView;

    [Tooltip("장비 관리 매니저 참조")]
    [SerializeField] private EquipmentManager equipmentManager;

    #endregion

    #region 라이프사이클

    // 싱글톤 인스턴스 초기화
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (inventoryPanelRoot == null)
        {
            inventoryPanelRoot = gameObject;
        }
    }

    // 패널 활성화 시 인벤토리 뷰 갱신
    private void OnEnable()
    {
        RefreshInventoryView();
    }

    // 인스턴스 파괴 시 싱글톤 참조 해제
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    #endregion

    #region 패널 오픈 및 상태 제어 API

    // 가방 인벤토리 전용 탭 모드 오픈 (장비창 숨김)
    public void OpenBagOnly()
    {
        if (inventoryPanelRoot != null) inventoryPanelRoot.SetActive(true);
        if (equipmentWindow != null) equipmentWindow.SetActive(false);
        if (inventoryWindow != null) inventoryWindow.SetActive(true);

        SortInventory();
    }

    // 캐릭터 선택 기반 장비창 + 가방 통합 모드 오픈 (장비창 노출)
    public void OpenEquipment(string unitId)
    {
        if (inventoryPanelRoot != null) inventoryPanelRoot.SetActive(true);
        if (equipmentWindow != null) equipmentWindow.SetActive(true);
        if (inventoryWindow != null) inventoryWindow.SetActive(true);

        EquipmentManager em = GetEquipmentManager();
        if (em != null && !string.IsNullOrEmpty(unitId))
        {
            em.SetCurrentUnit(unitId);
        }

        SortInventory();
    }

    // 장비창 가시성 토글 설정
    public void SetEquipmentWindowVisible(bool visible)
    {
        if (equipmentWindow != null)
        {
            equipmentWindow.SetActive(visible);
        }
    }

    // 인벤토리 아이템 자동 정렬 요청
    public void SortInventory()
    {
        if (InventoryGridManager.Instance != null)
        {
            InventoryGridManager.Instance.SortInventory();
        }
        else
        {
            RefreshInventoryView();
        }
    }

    // 인벤토리 가방 뷰 갱신
    public void RefreshInventoryView()
    {
        if (inventoryView != null)
        {
            inventoryView.Refresh();
        }
    }

    // 통합 인벤토리 패널 닫기
    public void Close()
    {
        if (inventoryPanelRoot != null)
        {
            inventoryPanelRoot.SetActive(false);
        }
    }

    // 장비 매니저 인스턴스 획득 헬퍼
    private EquipmentManager GetEquipmentManager()
    {
        if (equipmentManager != null) return equipmentManager;
        return EquipmentManager.Instance;
    }

    #endregion
}