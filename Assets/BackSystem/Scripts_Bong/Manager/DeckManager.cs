using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using UnityEngine;

// 인스펙터에서 덱 슬롯 편성 상태를 실시간으로 확인하기 위한 디버그 데이터 구조체
[Serializable]
public struct DeckSlotInspectorInfo
{
    [Tooltip("덱 슬롯 번호 (1 ~ 10)")]
    public int slotIndex;
    [Tooltip("유닛 식별 ID (예: UNIT_0001)")]
    public string unitId;
    [Tooltip("유닛 표시 이름")]
    public string unitName;
    [Tooltip("슬롯 장착 여부")]
    public bool isEquipped;
}

// 게임 내 덱 슬롯(총 10개) 편성, 장착, 해제, 교체 및 세이브/로드를 총괄 관리하는 싱글톤 매니저
public class DeckManager : SingletonBase<DeckManager>
{
    #region 직렬화 변수 (인스펙터 바인딩 및 편집)

    [Header("--- 유닛 카탈로그 참조 ---")]
    [Tooltip("유닛 메타데이터 조회를 위한 카탈로그 SO")]
    [SerializeField] private UnitCatalog unitCatalog;

    [Header("--- [Debug] 현재 덱 슬롯 편집 및 모니터링 ---")]
    [Tooltip("덱 슬롯별 유닛 ID 설정 (총 10개 슬롯, 1: 검사, 2: 궁수 등, -1: 미편성 빈 슬롯)")]
    [SerializeField] private int[] deckSlots = new int[10] { 1, 2, -1, -1, -1, -1, -1, -1, -1, -1 };

    [Tooltip("현재 덱에 편성된 슬롯별 실시간 정보")]
    [SerializeField] private List<DeckSlotInspectorInfo> currentDeckUnits = new List<DeckSlotInspectorInfo>();

    #endregion

    #region 내부 필드

    // 런타임 덱 슬롯 유닛 ID 배열 (총 10개, 미배치 시 -1)
    private int[] _deckSlots = new int[10] { 1, 2, -1, -1, -1, -1, -1, -1, -1, -1 };

    #endregion

    #region 프로퍼티

    // 현재 덱 슬롯 읽기 전용 목록
    public IReadOnlyList<int> DeckSlots => _deckSlots;

    #endregion

    #region 라이프사이클

    // 에디터 인스펙터에서 deckSlots 값을 변경했을 때 실시간 동기화 및 이벤트 발행
    private void OnValidate()
    {
        if (deckSlots != null && deckSlots.Length == 10)
        {
            _deckSlots = (int[])deckSlots.Clone();
            RefreshInspectorView();

            // 에디터 플레이 모드 중일 때 이벤트 전파
            if (Application.isPlaying)
            {
                EventBus.Publish(new DeckChangedEvent(_deckSlots));
            }
        }
    }

    // 초기 리소스 로드 및 기본 슬롯 동기화
    protected override void Awake()
    {
        base.Awake();

        // 1. 카탈로그 리소스 미할당 시 자동 로드
        if (unitCatalog == null)
        {
            unitCatalog = Resources.Load<UnitCatalog>("Catalogs/UnitCatalog");
        }

        // 2. 인스펙터에 설정된 슬롯 값을 내부 배열에 반영
        if (deckSlots != null && deckSlots.Length == 10)
        {
            _deckSlots = (int[])deckSlots.Clone();
        }

        RefreshInspectorView();
    }

    // 전역 세이브 / 로드 이벤트 버스 구독 등록
    private void OnEnable()
    {
        EventBus.Subscribe<DataLoadEvent>(OnLoad);
        EventBus.Subscribe<DataSaveEvent>(OnSave);
        EventBus.Subscribe<DataResetEvent>(OnReset);
    }

    // 전역 이벤트 버스 구독 해제
    private void OnDisable()
    {
        EventBus.Unsubscribe<DataLoadEvent>(OnLoad);
        EventBus.Unsubscribe<DataSaveEvent>(OnSave);
        EventBus.Unsubscribe<DataResetEvent>(OnReset);
    }

    #endregion

    #region 세이브 / 로드 연동

    // 세이브 데이터 로드 시 덱 슬롯 데이터 복원
    private void OnLoad(DataLoadEvent evt)
    {
        if (evt.saveData != null && evt.saveData.unitDeck != null && evt.saveData.unitDeck.deckSlots != null && evt.saveData.unitDeck.deckSlots.Length == 10)
        {
            _deckSlots = (int[])evt.saveData.unitDeck.deckSlots.Clone();
            deckSlots = (int[])_deckSlots.Clone();
            RefreshInspectorView();
            EventBus.Publish(new DeckChangedEvent(_deckSlots));
        }
    }

    // 세이브 데이터 저장 시 최신 덱 슬롯 데이터 기록
    private void OnSave(DataSaveEvent evt)
    {
        if (evt.saveData != null && evt.saveData.unitDeck != null)
        {
            evt.saveData.unitDeck.deckSlots = (int[])_deckSlots.Clone();
        }
    }

    // 데이터 리셋 시 덱 슬롯 초기화
    private void OnReset(DataResetEvent evt)
    {
        _deckSlots = new int[10] { 1, 2, -1, -1, -1, -1, -1, -1, -1, -1 };
        deckSlots = (int[])_deckSlots.Clone();
        RefreshInspectorView();
        EventBus.Publish(new DeckChangedEvent(_deckSlots));
    }

    #endregion

    #region 덱 조작 및 편집 API

    // 특정 슬롯(0~9)에 유닛을 장착 (동일 유닛 중복 배치 시 기존 슬롯 자동 해제)
    public bool SetSlot(int slotIndex, int unitId)
    {
        if (slotIndex < 0 || slotIndex >= 10 || unitId <= 0)
        {
            return false;
        }

        // 1. 이미 다른 슬롯에 같은 유닛이 배치되어 있다면 기존 슬롯 해제 (중복 방지)
        for (int i = 0; i < 10; i++)
        {
            if (_deckSlots[i] == unitId)
            {
                _deckSlots[i] = -1;
            }
        }

        // 2. 지정된 슬롯에 유닛 장착
        _deckSlots[slotIndex] = unitId;

        // 3. 인스펙터 동기화 및 전역 이벤트 발행
        PublishDeckChanged();
        return true;
    }

    // 특정 슬롯(0~9)의 유닛을 해제 (빈 슬롯으로 변경)
    public bool RemoveSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 10)
        {
            return false;
        }

        if (_deckSlots[slotIndex] == -1)
        {
            return true;
        }

        _deckSlots[slotIndex] = -1;
        PublishDeckChanged();
        return true;
    }

    // 특정 유닛 ID를 덱에서 찾아 해제
    public bool RemoveUnit(int unitId)
    {
        if (unitId <= 0)
        {
            return false;
        }

        bool removed = false;
        for (int i = 0; i < 10; i++)
        {
            if (_deckSlots[i] == unitId)
            {
                _deckSlots[i] = -1;
                removed = true;
            }
        }

        if (removed)
        {
            PublishDeckChanged();
        }

        return removed;
    }

    // 문자열 ID 기반 유닛 덱 해제 오버로딩
    public bool RemoveUnit(string unitIdStr)
    {
        int unitId = ParseUnitId(unitIdStr);
        return RemoveUnit(unitId);
    }

    // 두 슬롯 간 유닛 배치 위치 교체 (Swap)
    public bool SwapSlots(int slotIndexA, int slotIndexB)
    {
        if (slotIndexA < 0 || slotIndexA >= 10 || slotIndexB < 0 || slotIndexB >= 10 || slotIndexA == slotIndexB)
        {
            return false;
        }

        int temp = _deckSlots[slotIndexA];
        _deckSlots[slotIndexA] = _deckSlots[slotIndexB];
        _deckSlots[slotIndexB] = temp;

        PublishDeckChanged();
        return true;
    }

    // 덱 전체 슬롯 초기화 (모든 슬롯 해제)
    public bool ClearDeck()
    {
        for (int i = 0; i < 10; i++)
        {
            _deckSlots[i] = -1;
        }

        PublishDeckChanged();
        return true;
    }

    #endregion

    #region 덱 조회 헬퍼 API

    // 현재 덱 슬롯 배열의 전체 복사본 반환
    public int[] GetDeckSlotsCopy()
    {
        return (int[])_deckSlots.Clone();
    }

    // 정수형 유닛 ID가 덱에 포함되어 있는지 검사하고 장착된 슬롯 번호(0~9) 반환
    public bool IsInDeck(int unitId, out int slotIndex)
    {
        slotIndex = -1;
        if (unitId <= 0)
        {
            return false;
        }

        for (int i = 0; i < 10; i++)
        {
            if (_deckSlots[i] == unitId)
            {
                slotIndex = i;
                return true;
            }
        }

        return false;
    }

    // 문자열 유닛 ID가 덱에 포함되어 있는지 검사하는 오버로딩 메서드
    public bool IsInDeck(string unitIdStr, out int slotIndex)
    {
        int parsedId = ParseUnitId(unitIdStr);
        return IsInDeck(parsedId, out slotIndex);
    }

    #endregion

    #region 내부 동기화 및 헬퍼 메서드

    // 덱 변경 시 인스펙터 갱신 및 EventBus 전파
    private void PublishDeckChanged()
    {
        deckSlots = (int[])_deckSlots.Clone();
        RefreshInspectorView();
        EventBus.Publish(new DeckChangedEvent(_deckSlots));
    }

    // 인스펙터 모니터링 리스트 갱신
    public void RefreshInspectorView()
    {
        currentDeckUnits.Clear();

        if (_deckSlots == null)
        {
            return;
        }

        for (int i = 0; i < 10; i++)
        {
            int rawId = (i < _deckSlots.Length) ? _deckSlots[i] : -1;
            bool isEquipped = rawId > 0;
            string unitIdStr = isEquipped ? $"UNIT_{rawId:D4}" : "-";
            string unitName = "None";

            if (isEquipped && unitCatalog != null && unitCatalog.TryGetById(unitIdStr, out UnitDataSO unitData) && unitData != null)
            {
                unitName = unitData.DisplayName;
            }

            currentDeckUnits.Add(new DeckSlotInspectorInfo
            {
                slotIndex = i + 1,
                unitId = unitIdStr,
                unitName = unitName,
                isEquipped = isEquipped
            });
        }
    }

    // 유닛 ID 파싱 유틸리티 (UNIT_0001 -> 1)
    private int ParseUnitId(string unitIdStr)
    {
        if (string.IsNullOrEmpty(unitIdStr)) return -1;

        if (int.TryParse(unitIdStr.Replace("UNIT_", ""), out int parsedId))
        {
            return parsedId;
        }
        return unitIdStr.GetHashCode();
    }

    #endregion
}
