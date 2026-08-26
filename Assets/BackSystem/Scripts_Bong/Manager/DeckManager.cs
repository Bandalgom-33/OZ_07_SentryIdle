using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using UnityEngine;
using UnityEngine.Serialization;

// 에디터 인스펙터에서 덱 슬롯 편성 상태를 직관적으로 모니터링하기 위한 디버그 데이터 구조체
[Serializable]
public struct DeckSlotInspectorInfo
{
    [Tooltip("덱 슬롯 번호 (1 ~ Capacity)")]
    public int slotIndex;
    [Tooltip("유닛 식별 문자열 ID (예: UNIT_0001)")]
    public string unitId;
    [Tooltip("유닛 표시 이름")]
    public string unitName;
    [Tooltip("슬롯 장착 여부")]
    public bool isEquipped;
}

// 일반 필드 덱 1개와 레이드 전용 덱 2개(총 3개)의 슬롯 편성, 용량 관리 및 이벤트 발행을 총괄하는 덱 전담 매니저
public class DeckManager : SingletonBase<DeckManager>
{
    #region 직렬화 변수 (인스펙터 설정 및 모니터링)

    [Header("--- 유닛 카탈로그 참조 ---")]
    [Tooltip("유닛 메타데이터(스탯, 이름 등) 조회를 위한 ScriptableObject 카탈로그")]
    [SerializeField] private UnitCatalog unitCatalog;

    [Header("--- 덱별 최대 슬롯 용량 설정 (Capacity) ---")]
    [Tooltip("일반 필드/스테이지 디펜스 덱의 최대 슬롯 수")]
    [Range(1, 20)]
    [SerializeField] private int normalDeckCapacity = 10;

    [Tooltip("레이드 1팀 덱의 최대 슬롯 수")]
    [Range(1, 20)]
    [SerializeField] private int raid1DeckCapacity = 10;

    [Tooltip("레이드 2팀 덱의 최대 슬롯 수")]
    [Range(1, 20)]
    [SerializeField] private int raid2DeckCapacity = 10;

    [Header("--- [Debug] 덱별 슬롯 편집 (유닛 정수 ID, -1: 빈 슬롯) ---")]
    [Tooltip("일반 필드 덱 슬롯 배열 (1번: 루카 ID 2, 2번: 김하진 ID 4, -1: 미편성)")]
    [FormerlySerializedAs("deckSlots")]
    [SerializeField] private int[] normalDeckSlots = new int[10] { 2, 4, -1, -1, -1, -1, -1, -1, -1, -1 };

    [Tooltip("레이드 1팀 덱 슬롯 배열")]
    [SerializeField] private int[] raid1DeckSlots = new int[10] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };

    [Tooltip("레이드 2팀 덱 슬롯 배열")]
    [SerializeField] private int[] raid2DeckSlots = new int[10] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };

    [Header("--- [Debug] 실시간 덱 상태 모니터링 ---")]
    [Tooltip("일반 필드 덱 실시간 슬롯 정보")]
    [SerializeField] private List<DeckSlotInspectorInfo> normalDeckInspectorView = new List<DeckSlotInspectorInfo>();

    [Tooltip("레이드 1팀 덱 실시간 슬롯 정보")]
    [SerializeField] private List<DeckSlotInspectorInfo> raid1DeckInspectorView = new List<DeckSlotInspectorInfo>();

    [Tooltip("레이드 2팀 덱 실시간 슬롯 정보")]
    [SerializeField] private List<DeckSlotInspectorInfo> raid2DeckInspectorView = new List<DeckSlotInspectorInfo>();

    #endregion

    #region 내부 런타임 필드

    // 런타임 덱 데이터 딕셔너리 (덱 타입별 정수 ID 배열 관리)
    private readonly Dictionary<DeckType, int[]> _deckSlotMap = new Dictionary<DeckType, int[]>();

    #endregion

    #region 레거시 호환 프로퍼티

    // 기존 단일 덱 코드와의 하위 호환성을 위한 일반 덱 슬롯 읽기 전용 프로퍼티
    public IReadOnlyList<int> DeckSlots => GetDeckSlotsCopy(DeckType.Normal);

    #endregion

    #region 라이프사이클

    // 에디터 인스펙터 값 변경 시 배열 크기 동기화 및 뷰 갱신
    private void OnValidate()
    {
        // 1번 슬롯 루카(2), 2번 슬롯 김하진(4)을 기본값으로 크기 조정
        AdjustDeckArraySize(ref normalDeckSlots, normalDeckCapacity, new int[] { 2, 4 });
        AdjustDeckArraySize(ref raid1DeckSlots, raid1DeckCapacity, null);
        AdjustDeckArraySize(ref raid2DeckSlots, raid2DeckCapacity, null);

        SyncInternalMapFromSerializedFields();
        RefreshAllInspectorViews();

        if (Application.isPlaying)
        {
            PublishDeckChanged(DeckType.Normal);
            PublishDeckChanged(DeckType.Raid1);
            PublishDeckChanged(DeckType.Raid2);
        }
    }

    // 초기화 및 기본 덱 데이터 구성
    protected override void Awake()
    {
        base.Awake();

        if (unitCatalog == null)
        {
            unitCatalog = Resources.Load<UnitCatalog>("Catalogs/UnitCatalog");
        }

        // 1번 슬롯 루카(2), 2번 슬롯 김하진(4)을 기본값으로 배열 초기화
        AdjustDeckArraySize(ref normalDeckSlots, normalDeckCapacity, new int[] { 2, 4 });
        AdjustDeckArraySize(ref raid1DeckSlots, raid1DeckCapacity, null);
        AdjustDeckArraySize(ref raid2DeckSlots, raid2DeckCapacity, null);

        SyncInternalMapFromSerializedFields();
        RefreshAllInspectorViews();
    }

    // 전역 이벤트 버스 구독 등록
    private void OnEnable()
    {
        EventBus.Subscribe<DataLoadEvent>(OnLoad);
        EventBus.Subscribe<DataSaveEvent>(OnSave);
        EventBus.Subscribe<DataResetEvent>(OnReset);

        EventBus.Subscribe<RequestSetDeckSlotEvent>(OnRequestSetDeckSlot);
        EventBus.Subscribe<RequestAutoAddDeckEvent>(OnRequestAutoAddDeck);
    }

    // 전역 이벤트 버스 구독 해제
    private void OnDisable()
    {
        EventBus.Unsubscribe<DataLoadEvent>(OnLoad);
        EventBus.Unsubscribe<DataSaveEvent>(OnSave);
        EventBus.Unsubscribe<DataResetEvent>(OnReset);

        EventBus.Unsubscribe<RequestSetDeckSlotEvent>(OnRequestSetDeckSlot);
        EventBus.Unsubscribe<RequestAutoAddDeckEvent>(OnRequestAutoAddDeck);
    }

    #endregion

    #region 세이브 / 로드 연동

    // 세이브 데이터 로드 시 3개 덱 데이터 복원
    private void OnLoad(DataLoadEvent evt)
    {
        if (evt.saveData == null || evt.saveData.unitDeck == null) return;

        UnitDeckData deckData = evt.saveData.unitDeck;

        if (deckData.normalDeckSlots != null && deckData.normalDeckSlots.Length > 0)
        {
            normalDeckSlots = (int[])deckData.normalDeckSlots.Clone();
            AdjustDeckArraySize(ref normalDeckSlots, normalDeckCapacity, new int[] { 2, 4 });
            _deckSlotMap[DeckType.Normal] = (int[])normalDeckSlots.Clone();
        }

        if (deckData.raid1DeckSlots != null && deckData.raid1DeckSlots.Length > 0)
        {
            raid1DeckSlots = (int[])deckData.raid1DeckSlots.Clone();
            AdjustDeckArraySize(ref raid1DeckSlots, raid1DeckCapacity, null);
            _deckSlotMap[DeckType.Raid1] = (int[])raid1DeckSlots.Clone();
        }

        if (deckData.raid2DeckSlots != null && deckData.raid2DeckSlots.Length > 0)
        {
            raid2DeckSlots = (int[])deckData.raid2DeckSlots.Clone();
            AdjustDeckArraySize(ref raid2DeckSlots, raid2DeckCapacity, null);
            _deckSlotMap[DeckType.Raid2] = (int[])raid2DeckSlots.Clone();
        }

        RefreshAllInspectorViews();
        PublishDeckChanged(DeckType.Normal);
        PublishDeckChanged(DeckType.Raid1);
        PublishDeckChanged(DeckType.Raid2);
    }

    // 세이브 데이터 저장 시 3개 덱 슬롯 데이터 기록 (덱 전담)
    private void OnSave(DataSaveEvent evt)
    {
        if (evt.saveData == null) return;
        if (evt.saveData.unitDeck == null)
        {
            evt.saveData.unitDeck = new UnitDeckData();
        }

        evt.saveData.unitDeck.normalDeckSlots = GetDeckSlotsCopy(DeckType.Normal);
        evt.saveData.unitDeck.raid1DeckSlots = GetDeckSlotsCopy(DeckType.Raid1);
        evt.saveData.unitDeck.raid2DeckSlots = GetDeckSlotsCopy(DeckType.Raid2);
    }

    // 데이터 리셋 시 기본 덱 구성으로 초기화
    private void OnReset(DataResetEvent evt)
    {
        // 1번 슬롯 루카(2), 2번 슬롯 김하진(4)으로 기본 덱 복구
        normalDeckSlots = CreateDefaultSlotArray(normalDeckCapacity, new int[] { 2, 4 });
        raid1DeckSlots = CreateDefaultSlotArray(raid1DeckCapacity, null);
        raid2DeckSlots = CreateDefaultSlotArray(raid2DeckCapacity, null);

        SyncInternalMapFromSerializedFields();
        RefreshAllInspectorViews();

        PublishDeckChanged(DeckType.Normal);
        PublishDeckChanged(DeckType.Raid1);
        PublishDeckChanged(DeckType.Raid2);
    }

    #endregion

    #region 커맨드 이벤트 수신 핸들러 (EventBus Command Listener)

    // 외부/UI에서 발행한 슬롯 장착 커맨드 처리
    private void OnRequestSetDeckSlot(RequestSetDeckSlotEvent evt)
    {
        int unitId = UnitIdHelper.ParseUnitId(evt.unitKey);
        if (unitId <= 0)
        {
            RemoveSlot(evt.deckType, evt.slotIndex);
        }
        else
        {
            SetSlot(evt.deckType, evt.slotIndex, unitId);
        }
    }

    // 외부/UI에서 발행한 빈 슬롯 자동 장착 커맨드 처리
    private void OnRequestAutoAddDeck(RequestAutoAddDeckEvent evt)
    {
        int unitId = UnitIdHelper.ParseUnitId(evt.unitKey);
        if (unitId > 0)
        {
            TryAddUnitToDeck(evt.deckType, unitId, out _);
        }
    }

    #endregion

    #region 팀원 호출용 덱 조회 API (Query APIs - Read-Only)

    // 특정 덱의 최대 슬롯 용량(Capacity) 반환
    public int GetDeckCapacity(DeckType deckType)
    {
        return deckType switch
        {
            DeckType.Normal => normalDeckCapacity,
            DeckType.Raid1 => raid1DeckCapacity,
            DeckType.Raid2 => raid2DeckCapacity,
            _ => 10
        };
    }

    // 특정 덱에 실제 등록된 유닛 문자열 키 목록 반환
    public List<string> GetRegisteredUnitKeys(DeckType deckType)
    {
        List<string> keys = new List<string>();
        int[] slots = GetInternalSlots(deckType);

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] > 0)
            {
                keys.Add(UnitIdHelper.ToUnitKey(slots[i]));
            }
        }
        return keys;
    }

    // 특정 덱에 실제 등록된 유닛 정수 ID 목록 반환
    public List<int> GetRegisteredUnitIds(DeckType deckType)
    {
        List<int> ids = new List<int>();
        int[] slots = GetInternalSlots(deckType);

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] > 0)
            {
                ids.Add(slots[i]);
            }
        }
        return ids;
    }

    // 특정 덱에 실제 등록된 유닛들의 UnitDataSO 리스트 반환
    public List<UnitDataSO> GetRegisteredUnitData(DeckType deckType)
    {
        List<UnitDataSO> dataList = new List<UnitDataSO>();
        int[] slots = GetInternalSlots(deckType);

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] > 0)
            {
                string unitKey = UnitIdHelper.ToUnitKey(slots[i]);
                if (TryGetUnitDataSO(unitKey, out UnitDataSO so) && so != null)
                {
                    dataList.Add(so);
                }
            }
        }
        return dataList;
    }

    // 특정 덱의 전체 슬롯 상세 엔트리 목록 반환 (빈 슬롯 포함)
    public List<DeckSlotUnitEntry> GetAllDeckSlotEntries(DeckType deckType)
    {
        List<DeckSlotUnitEntry> list = new List<DeckSlotUnitEntry>();
        int[] slots = GetInternalSlots(deckType);

        for (int i = 0; i < slots.Length; i++)
        {
            int rawId = slots[i];
            bool isEquipped = rawId > 0;
            string key = isEquipped ? UnitIdHelper.ToUnitKey(rawId) : string.Empty;
            TryGetUnitDataSO(key, out UnitDataSO so);

            list.Add(new DeckSlotUnitEntry(i, rawId, key, so));
        }
        return list;
    }

    // 특정 덱에서 실제 장착된 유효 슬롯 엔트리만 필터링하여 반환
    public List<DeckSlotUnitEntry> GetActiveDeckSlotEntries(DeckType deckType)
    {
        List<DeckSlotUnitEntry> list = new List<DeckSlotUnitEntry>();
        int[] slots = GetInternalSlots(deckType);

        for (int i = 0; i < slots.Length; i++)
        {
            int rawId = slots[i];
            if (rawId > 0)
            {
                string key = UnitIdHelper.ToUnitKey(rawId);
                TryGetUnitDataSO(key, out UnitDataSO so);
                list.Add(new DeckSlotUnitEntry(i, rawId, key, so));
            }
        }
        return list;
    }

    // 특정 덱 슬롯 배열 전체 복사본 반환
    public int[] GetDeckSlotsCopy(DeckType deckType)
    {
        int[] slots = GetInternalSlots(deckType);
        return (int[])slots.Clone();
    }

    // 특정 유닛(정수 ID)이 지정 덱에 편성되어 있는지 확인하고 슬롯 인덱스를 반환
    public bool IsUnitInDeck(DeckType deckType, int unitId, out int slotIndex)
    {
        slotIndex = -1;
        if (unitId <= 0) return false;

        int[] slots = GetInternalSlots(deckType);
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == unitId)
            {
                slotIndex = i;
                return true;
            }
        }
        return false;
    }

    // 특정 유닛(문자열 키)이 지정 덱에 편성되어 있는지 확인하는 오버로딩 메서드
    public bool IsUnitInDeck(DeckType deckType, string unitKey, out int slotIndex)
    {
        int unitId = UnitIdHelper.ParseUnitId(unitKey);
        return IsUnitInDeck(deckType, unitId, out slotIndex);
    }

    #endregion

    #region 덱 조작 및 편집 API (Command APIs)

    // 특정 덱의 지정 슬롯에 유닛 장착 (동일 덱 내 중복 배치 방지)
    public bool SetSlot(DeckType deckType, int slotIndex, int unitId)
    {
        int[] slots = GetInternalSlots(deckType);
        if (slotIndex < 0 || slotIndex >= slots.Length || unitId <= 0)
        {
            return false;
        }

        // 해당 덱 내에 이미 동일한 유닛이 배치되어 있다면 기존 위치 해제
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == unitId)
            {
                slots[i] = -1;
            }
        }

        slots[slotIndex] = unitId;
        PublishDeckChanged(deckType);
        return true;
    }

    // 문자열 키 기반 슬롯 장착 오버로딩
    public bool SetSlot(DeckType deckType, int slotIndex, string unitKey)
    {
        int unitId = UnitIdHelper.ParseUnitId(unitKey);
        return SetSlot(deckType, slotIndex, unitId);
    }

    // 덱의 첫 번째 빈 슬롯에 유닛 자동 장착
    public bool TryAddUnitToDeck(DeckType deckType, int unitId, out int assignedSlotIndex)
    {
        assignedSlotIndex = -1;
        if (unitId <= 0) return false;

        int[] slots = GetInternalSlots(deckType);

        if (IsUnitInDeck(deckType, unitId, out int existingIndex))
        {
            assignedSlotIndex = existingIndex;
            return true;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] <= 0)
            {
                slots[i] = unitId;
                assignedSlotIndex = i;
                PublishDeckChanged(deckType);
                return true;
            }
        }

        return false;
    }

    // 문자열 키 기반 빈 슬롯 자동 장착 오버로딩
    public bool TryAddUnitToDeck(DeckType deckType, string unitKey, out int assignedSlotIndex)
    {
        int unitId = UnitIdHelper.ParseUnitId(unitKey);
        return TryAddUnitToDeck(deckType, unitId, out assignedSlotIndex);
    }

    // 특정 덱의 슬롯 유닛 해제
    public bool RemoveSlot(DeckType deckType, int slotIndex)
    {
        int[] slots = GetInternalSlots(deckType);
        if (slotIndex < 0 || slotIndex >= slots.Length)
        {
            return false;
        }

        if (slots[slotIndex] == -1)
        {
            return true;
        }

        slots[slotIndex] = -1;
        PublishDeckChanged(deckType);
        return true;
    }

    // 특정 덱에서 지정 유닛 ID를 찾아 해제
    public bool RemoveUnit(DeckType deckType, int unitId)
    {
        if (unitId <= 0) return false;

        int[] slots = GetInternalSlots(deckType);
        bool removed = false;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == unitId)
            {
                slots[i] = -1;
                removed = true;
            }
        }

        if (removed)
        {
            PublishDeckChanged(deckType);
        }
        return removed;
    }

    // 문자열 키 기반 유닛 해제 오버로딩
    public bool RemoveUnit(DeckType deckType, string unitKey)
    {
        int unitId = UnitIdHelper.ParseUnitId(unitKey);
        return RemoveUnit(deckType, unitId);
    }

    // 특정 덱 내 두 슬롯 위치 교체 (Swap)
    public bool SwapSlots(DeckType deckType, int slotIndexA, int slotIndexB)
    {
        int[] slots = GetInternalSlots(deckType);
        if (slotIndexA < 0 || slotIndexA >= slots.Length || slotIndexB < 0 || slotIndexB >= slots.Length || slotIndexA == slotIndexB)
        {
            return false;
        }

        int temp = slots[slotIndexA];
        slots[slotIndexA] = slots[slotIndexB];
        slots[slotIndexB] = temp;

        PublishDeckChanged(deckType);
        return true;
    }

    // 특정 덱의 모든 슬롯 비우기
    public bool ClearDeck(DeckType deckType)
    {
        int[] slots = GetInternalSlots(deckType);
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i] = -1;
        }

        PublishDeckChanged(deckType);
        return true;
    }

    // 원본 덱의 유닛 구성을 대상 덱으로 복사
    public void CopyDeck(DeckType sourceDeck, DeckType targetDeck)
    {
        if (sourceDeck == targetDeck) return;

        int[] srcSlots = GetInternalSlots(sourceDeck);
        int[] tgtSlots = GetInternalSlots(targetDeck);

        int copyCount = Math.Min(srcSlots.Length, tgtSlots.Length);
        for (int i = 0; i < tgtSlots.Length; i++)
        {
            tgtSlots[i] = (i < copyCount) ? srcSlots[i] : -1;
        }

        PublishDeckChanged(targetDeck);
    }

    #endregion

    #region 레거시 호환 조작 메서드 (단일 덱 지원)

    public bool SetSlot(int slotIndex, int unitId) => SetSlot(DeckType.Normal, slotIndex, unitId);
    public bool RemoveSlot(int slotIndex) => RemoveSlot(DeckType.Normal, slotIndex);
    public bool RemoveUnit(int unitId) => RemoveUnit(DeckType.Normal, unitId);
    public bool RemoveUnit(string unitIdStr) => RemoveUnit(DeckType.Normal, unitIdStr);
    public bool SwapSlots(int slotIndexA, int slotIndexB) => SwapSlots(DeckType.Normal, slotIndexA, slotIndexB);
    public bool ClearDeck() => ClearDeck(DeckType.Normal);
    public int[] GetDeckSlotsCopy() => GetDeckSlotsCopy(DeckType.Normal);
    public bool IsInDeck(int unitId, out int slotIndex) => IsUnitInDeck(DeckType.Normal, unitId, out slotIndex);
    public bool IsInDeck(string unitIdStr, out int slotIndex) => IsUnitInDeck(DeckType.Normal, unitIdStr, out slotIndex);

    #endregion

    #region 내부 헬퍼 및 이벤트 발행 로직

    // 덱 변경 시 인스펙터 동기화 및 일반/레이드 독립 이벤트 발행
    private void PublishDeckChanged(DeckType deckType)
    {
        SyncSerializedFieldsFromInternalMap(deckType);
        RefreshInspectorView(deckType);

        List<DeckSlotUnitEntry> allSlots = GetAllDeckSlotEntries(deckType);
        List<DeckSlotUnitEntry> activeUnits = GetActiveDeckSlotEntries(deckType);
        List<string> registeredKeys = GetRegisteredUnitKeys(deckType);
        List<int> registeredIds = GetRegisteredUnitIds(deckType);
        List<UnitDataSO> registeredDatas = GetRegisteredUnitData(deckType);

        if (deckType == DeckType.Normal)
        {
            EventBus.Publish(new NormalDeckChangedEvent(
                allSlots,
                activeUnits,
                registeredKeys,
                registeredIds,
                registeredDatas
            ));
        }
        else
        {
            EventBus.Publish(new RaidDeckChangedEvent(
                deckType,
                allSlots,
                activeUnits,
                registeredKeys,
                registeredIds,
                registeredDatas
            ));
        }
    }

    // 덱 타입별 내부 슬롯 배열 획득
    private int[] GetInternalSlots(DeckType deckType)
    {
        if (!_deckSlotMap.TryGetValue(deckType, out int[] slots) || slots == null)
        {
            int cap = GetDeckCapacity(deckType);
            slots = CreateDefaultSlotArray(cap, deckType == DeckType.Normal ? new int[] { 2, 4 } : null);
            _deckSlotMap[deckType] = slots;
        }
        return slots;
    }

    // 직렬화 필드 값을 런타임 딕셔너리로 동기화
    private void SyncInternalMapFromSerializedFields()
    {
        _deckSlotMap[DeckType.Normal] = normalDeckSlots != null ? (int[])normalDeckSlots.Clone() : CreateDefaultSlotArray(normalDeckCapacity, new int[] { 2, 4 });
        _deckSlotMap[DeckType.Raid1] = raid1DeckSlots != null ? (int[])raid1DeckSlots.Clone() : CreateDefaultSlotArray(raid1DeckCapacity, null);
        _deckSlotMap[DeckType.Raid2] = raid2DeckSlots != null ? (int[])raid2DeckSlots.Clone() : CreateDefaultSlotArray(raid2DeckCapacity, null);
    }

    // 런타임 슬롯 배열을 직렬화 필드에 동기화
    private void SyncSerializedFieldsFromInternalMap(DeckType deckType)
    {
        int[] current = GetInternalSlots(deckType);
        switch (deckType)
        {
            case DeckType.Normal:
                normalDeckSlots = (int[])current.Clone();
                break;
            case DeckType.Raid1:
                raid1DeckSlots = (int[])current.Clone();
                break;
            case DeckType.Raid2:
                raid2DeckSlots = (int[])current.Clone();
                break;
        }
    }

    // 모든 덱의 인스펙터 모니터링 뷰 갱신
    public void RefreshAllInspectorViews()
    {
        RefreshInspectorView(DeckType.Normal);
        RefreshInspectorView(DeckType.Raid1);
        RefreshInspectorView(DeckType.Raid2);
    }

    // 특정 덱의 인스펙터 모니터링 뷰 갱신
    private void RefreshInspectorView(DeckType deckType)
    {
        List<DeckSlotInspectorInfo> targetList = deckType switch
        {
            DeckType.Normal => normalDeckInspectorView,
            DeckType.Raid1 => raid1DeckInspectorView,
            DeckType.Raid2 => raid2DeckInspectorView,
            _ => null
        };

        if (targetList == null) return;
        targetList.Clear();

        int[] slots = GetInternalSlots(deckType);
        for (int i = 0; i < slots.Length; i++)
        {
            int rawId = slots[i];
            bool isEquipped = rawId > 0;
            string unitKey = isEquipped ? UnitIdHelper.ToUnitKey(rawId) : "-";
            string unitName = "None";

            if (isEquipped && TryGetUnitDataSO(unitKey, out UnitDataSO dataSO) && dataSO != null)
            {
                unitName = dataSO.DisplayName;
            }

            targetList.Add(new DeckSlotInspectorInfo
            {
                slotIndex = i + 1,
                unitId = unitKey,
                unitName = unitName,
                isEquipped = isEquipped
            });
        }
    }

    // 카탈로그에서 UnitDataSO 안전 조회
    private bool TryGetUnitDataSO(string unitKey, out UnitDataSO unitData)
    {
        unitData = null;
        if (string.IsNullOrEmpty(unitKey) || unitCatalog == null) return false;

        return unitCatalog.TryGetById(unitKey, out unitData);
    }

    // 슬롯 용량 변경 시 배열 크기 재할당 및 데이터 보존 헬퍼
    private void AdjustDeckArraySize(ref int[] array, int capacity, int[] defaultInitValues)
    {
        capacity = Mathf.Max(1, capacity);
        if (array == null || array.Length != capacity)
        {
            int[] newArr = new int[capacity];
            for (int i = 0; i < capacity; i++)
            {
                if (array != null && i < array.Length)
                {
                    newArr[i] = array[i];
                }
                else if (defaultInitValues != null && i < defaultInitValues.Length)
                {
                    newArr[i] = defaultInitValues[i];
                }
                else
                {
                    newArr[i] = -1;
                }
            }
            array = newArr;
        }
    }

    // 기본 슬롯 배열 생성 헬퍼
    private int[] CreateDefaultSlotArray(int capacity, int[] defaultValues)
    {
        capacity = Mathf.Max(1, capacity);
        int[] arr = new int[capacity];
        for (int i = 0; i < capacity; i++)
        {
            if (defaultValues != null && i < defaultValues.Length)
            {
                arr[i] = defaultValues[i];
            }
            else
            {
                arr[i] = -1;
            }
        }
        return arr;
    }

    #endregion
}
