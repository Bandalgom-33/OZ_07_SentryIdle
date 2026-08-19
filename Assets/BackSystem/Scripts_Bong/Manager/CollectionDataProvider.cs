using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using UnityEngine;

public class CollectionItemViewModel
{
    public UnitDataSO UnitData { get; set; }
    public Sprite PortraitIcon { get; set; }
    public bool IsOwned { get; set; }
    public int Level { get; set; }
    public long CurrentExp { get; set; }
    public int BreakThroughStep { get; set; }
    public int FragmentCount { get; set; }
    public bool IsInDeck { get; set; }
    public int DeckSlotIndex { get; set; }
    public DeckType CurrentDeckType { get; set; } = DeckType.Normal;

    public string UnitId => UnitData != null ? UnitData.UnitId : string.Empty;
    public string DisplayName => UnitData != null ? UnitData.DisplayName : string.Empty;
    public UnitGrade Grade => UnitData != null ? UnitData.Grade : UnitGrade.None;
}

public class CollectionDataProvider : SingletonBase<CollectionDataProvider>
{
    #region 직렬화 변수 (인스펙터 바인딩)

    [Header("--- 팀원 캐릭터 카탈로그 ---")]
    [Tooltip("팀원 캐릭터 카탈로그 SO 참조")]
    [SerializeField] private UnitCatalog unitCatalog;

    [Header("--- 초상화 매핑 카탈로그 ---")]
    [Tooltip("유닛 초상화 매핑 카탈로그 SO 참조")]
    [SerializeField] private UnitPortraitCatalogSO portraitCatalog;

    #endregion

    #region 내부 세이브 캐시 필드

    private List<UnitSaveData> _cachedOwnedUnits = new List<UnitSaveData>();

    #endregion

    #region 라이프 사이클

    // 카탈로그 리소스 초기 설정
    protected override void Awake()
    {
        if (unitCatalog == null)
        {
            unitCatalog = Resources.Load<UnitCatalog>("Catalogs/UnitCatalog");
        }

        if (portraitCatalog == null)
        {
            portraitCatalog = Resources.Load<UnitPortraitCatalogSO>("UnitPortraitCatalog");
        }
    }

    // 이벤트 버스 구독 연산
    private void OnEnable()
    {
        EventBus.Subscribe<DataLoadEvent>(OnLoad);
        EventBus.Subscribe<DataSaveEvent>(OnSave);
        EventBus.Subscribe<DataResetEvent>(OnReset);
        EventBus.Subscribe<GachaDrawCompletedEvent>(OnGachaDrawCompleted);
    }

    // 이벤트 버스 구독 해제 연산
    private void OnDisable()
    {
        EventBus.Unsubscribe<DataLoadEvent>(OnLoad);
        EventBus.Unsubscribe<DataSaveEvent>(OnSave);
        EventBus.Unsubscribe<DataResetEvent>(OnReset);
        EventBus.Unsubscribe<GachaDrawCompletedEvent>(OnGachaDrawCompleted);
    }

    #endregion

    #region 세이브/로드 및 가챠 이벤트 처리

    // 가챠 완료 시 유닛 보유 정보 갱신
    private void OnGachaDrawCompleted(GachaDrawCompletedEvent evt)
    {
        if (evt.resultItems == null) return;

        bool hasNewUnit = false;
        foreach (var item in evt.resultItems)
        {
            if (item == null || string.IsNullOrEmpty(item.RewardId)) continue;

            int parsedId = ParseUnitId(item.RewardId);

            if (!_cachedOwnedUnits.Exists(u => u.unitId == parsedId))
            {
                _cachedOwnedUnits.Add(new UnitSaveData
                {
                    unitId = parsedId,
                    level = 1,
                    breakThroughStep = 0,
                    fragmentCount = 0
                });
                hasNewUnit = true;
            }
        }
    }

    // 유닛 ID 파싱 처리
    private int ParseUnitId(string unitIdStr)
    {
        if (int.TryParse(unitIdStr.Replace("UNIT_", ""), out int parsedId))
        {
            return parsedId;
        }
        return unitIdStr.GetHashCode();
    }

    // 세이브 데이터 로드 연산
    private void OnLoad(DataLoadEvent evt)
    {
        if (evt.saveData != null && evt.saveData.unitDeck != null)
        {
            _cachedOwnedUnits = evt.saveData.unitDeck.ownedUnits ?? new List<UnitSaveData>();
        }
    }

    // 세이브 데이터 저장 연산
    private void OnSave(DataSaveEvent evt)
    {
        if (evt.saveData != null && evt.saveData.unitDeck != null)
        {
            _cachedOwnedUnits = evt.saveData.unitDeck.ownedUnits ?? new List<UnitSaveData>();
        }
    }

    // 데이터 초기화 연산
    private void OnReset(DataResetEvent evt)
    {
        _cachedOwnedUnits.Clear();
    }

    #endregion

    #region 뷰모델 제공 메서드

    // 보관함 유닛 뷰모델 리스트 생성 및 반환 (기본값: 일반 필드 덱 기준)
    public List<CollectionItemViewModel> GetCollectionViewModels(DeckType deckType = DeckType.Normal)
    {
        List<CollectionItemViewModel> list = new List<CollectionItemViewModel>();

        if (unitCatalog == null || unitCatalog.Units == null) return list;

        IReadOnlyList<UnitDataSO> units = unitCatalog.Units;

        for (int i = 0; i < units.Count; i++)
        {
            UnitDataSO unitSO = units[i];
            if (unitSO == null) continue;

            UnitSaveData savedUnit = FindSaveDataByUnitId(_cachedOwnedUnits, unitSO.UnitId);
            bool isOwned = savedUnit != null;
            int level = savedUnit != null ? savedUnit.level : unitSO.InitialLevel;
            long currentExp = savedUnit != null ? savedUnit.currentExp : 0L;
            int breakThroughStep = savedUnit != null ? savedUnit.breakThroughStep : 0;
            int fragmentCount = savedUnit != null ? savedUnit.fragmentCount : 0;

            // DeckManager를 통해 지정된 덱(Normal / Raid1 / Raid2) 내 포함 여부 및 슬롯 번호 조회
            bool isInDeck = false;
            int deckIndex = -1;
            if (DeckManager.Instance != null)
            {
                isInDeck = DeckManager.Instance.IsUnitInDeck(deckType, unitSO.UnitId, out deckIndex);
            }

            Sprite portrait = portraitCatalog != null ? portraitCatalog.GetPortraitByUnitData(unitSO) : null;

            list.Add(new CollectionItemViewModel
            {
                UnitData = unitSO,
                PortraitIcon = portrait,
                IsOwned = isOwned,
                Level = level,
                CurrentExp = currentExp,
                BreakThroughStep = breakThroughStep,
                FragmentCount = fragmentCount,
                IsInDeck = isInDeck,
                DeckSlotIndex = deckIndex,
                CurrentDeckType = deckType
            });
        }

        return list;
    }

    // 유닛 ID 기반 보유 유닛 세이브 데이터 외부 조회용 메서드
    public UnitSaveData GetOwnedUnitSaveData(string unitIdStr)
    {
        return FindSaveDataByUnitId(_cachedOwnedUnits, unitIdStr);
    }

    // 정수형 유닛 ID 기반 보유 유닛 세이브 데이터 외부 조회용 메서드
    public UnitSaveData GetOwnedUnitSaveData(int unitId)
    {
        if (_cachedOwnedUnits == null) return null;

        for (int i = 0; i < _cachedOwnedUnits.Count; i++)
        {
            UnitSaveData u = _cachedOwnedUnits[i];
            if (u != null && u.unitId == unitId)
            {
                return u;
            }
        }

        return null;
    }

    // 유닛 레벨 및 누적 경험치 캐시 갱신 메서드
    public void UpdateUnitExpAndLevel(string unitIdStr, int newLevel, long newExp)
    {
        UnitSaveData saved = FindSaveDataByUnitId(_cachedOwnedUnits, unitIdStr);
        if (saved != null)
        {
            saved.level = Mathf.Max(1, newLevel);
            saved.currentExp = Math.Max(0L, newExp);
        }
    }

    // 유닛 ID 기반 세이브 데이터 조회
    private UnitSaveData FindSaveDataByUnitId(List<UnitSaveData> ownedUnits, string unitIdStr)
    {
        if (ownedUnits == null || string.IsNullOrEmpty(unitIdStr)) return null;

        int targetId = ParseUnitId(unitIdStr);

        for (int i = 0; i < ownedUnits.Count; i++)
        {
            UnitSaveData u = ownedUnits[i];
            if (u == null) continue;

            if (u.unitId == targetId)
            {
                return u;
            }
        }

        return null;
    }

    #endregion
}
