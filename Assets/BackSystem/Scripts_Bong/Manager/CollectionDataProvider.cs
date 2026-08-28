using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using UnityEngine;

// UI 보관함 화면 표시에 필요한 유닛 뷰모델 데이터 클래스
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

// 보유 유닛 목록 및 유닛 성장 데이터(레벨/경험치/돌파)를 전담 관리하는 단일 진실 공급원(SSOT) 매니저
public class CollectionDataProvider : SingletonBase<CollectionDataProvider>
{
    #region 직렬화 변수 (인스펙터 바인딩)

    [Header("--- 유닛 및 초상화 카탈로그 ---")]
    [Tooltip("게임 내 전체 유닛 메타데이터 조회를 위한 카탈로그 SO")]
    [SerializeField] private UnitCatalog unitCatalog;

    [Tooltip("유닛 초상화 스프라이트 매핑 카탈로그 SO")]
    [SerializeField] private UnitPortraitCatalogSO portraitCatalog;

    #endregion

    #region 내부 세이브 캐시 필드

    // 보유 중인 모든 유닛의 성장/돌파 세이브 데이터 목록 (단일 중앙 저장소)
    private readonly List<UnitSaveData> _cachedOwnedUnits = new List<UnitSaveData>();

    public UnitCatalog UnitCatalog => GetUnitCatalogSafe();
    public UnitPortraitCatalogSO PortraitCatalog => GetPortraitCatalogSafe();

    // 싱글톤 초기화 전후 시점과 무관하게 안전하게 카탈로그 반환
    public UnitCatalog GetUnitCatalogSafe()
    {
        if (unitCatalog != null) return unitCatalog;
        unitCatalog = Resources.Load<UnitCatalog>("Catalogs/UnitCatalog");
        return unitCatalog;
    }

    public UnitPortraitCatalogSO GetPortraitCatalogSafe()
    {
        if (portraitCatalog != null) return portraitCatalog;
        portraitCatalog = Resources.Load<UnitPortraitCatalogSO>("UnitPortraitCatalog");
        return portraitCatalog;
    }

    #endregion

    #region 라이프 사이클

    // 카탈로그 리소스 초기화 및 기본 보유 유닛 설정
    protected override void Awake()
    {
        base.Awake();

        if (unitCatalog == null)
        {
            unitCatalog = Resources.Load<UnitCatalog>("Catalogs/UnitCatalog");
        }

        if (portraitCatalog == null)
        {
            portraitCatalog = Resources.Load<UnitPortraitCatalogSO>("UnitPortraitCatalog");
        }

        // 세이브 데이터가 로드되기 전 기본 유닛(루카, 김하진)을 캐시에 초기화
        if (_cachedOwnedUnits.Count == 0)
        {
            InitDefaultOwnedUnits();
        }
    }

    // 이벤트 버스 구독 등록
    private void OnEnable()
    {
        EventBus.Subscribe<DataLoadEvent>(OnLoad);
        EventBus.Subscribe<DataSaveEvent>(OnSave);
        EventBus.Subscribe<DataResetEvent>(OnReset);
        EventBus.Subscribe<GachaDrawCompletedEvent>(OnGachaDrawCompleted);
    }

    // 이벤트 버스 구독 해제
    private void OnDisable()
    {
        EventBus.Unsubscribe<DataLoadEvent>(OnLoad);
        EventBus.Unsubscribe<DataSaveEvent>(OnSave);
        EventBus.Unsubscribe<DataResetEvent>(OnReset);
        EventBus.Unsubscribe<GachaDrawCompletedEvent>(OnGachaDrawCompleted);
    }

    #endregion

    #region 세이브/로드 및 가챠 이벤트 처리

    // 가챠 완료 시 유닛 획득 및 보유 목록 동기화 처리
    private void OnGachaDrawCompleted(GachaDrawCompletedEvent evt)
    {
        if (evt.resultItems == null) return;

        foreach (var item in evt.resultItems)
        {
            if (item == null || string.IsNullOrEmpty(item.RewardId)) continue;

            int parsedId = UnitIdHelper.ParseUnitId(item.RewardId);
            if (parsedId <= 0) continue;

            UnitSaveData existing = FindSaveDataByUnitId(parsedId);
            if (existing == null)
            {
                _cachedOwnedUnits.Add(new UnitSaveData
                {
                    unitId = parsedId,
                    level = 1,
                    currentExp = 0L,
                    breakThroughStep = item.CurrentBreakthroughStep,
                    fragmentCount = 0
                });
            }
            else
            {
                existing.breakThroughStep = item.CurrentBreakthroughStep;
            }
        }
    }

    // 세이브 데이터 로드 처리
    private void OnLoad(DataLoadEvent evt)
    {
        _cachedOwnedUnits.Clear();
        if (evt.saveData != null && evt.saveData.unitDeck != null && evt.saveData.unitDeck.ownedUnits != null && evt.saveData.unitDeck.ownedUnits.Count > 0)
        {
            _cachedOwnedUnits.AddRange(evt.saveData.unitDeck.ownedUnits);
        }
        else
        {
            // 세이브 데이터에 보유 유닛이 비어있을 경우 기본 유닛(루카, 김하진) 자동 복원
            InitDefaultOwnedUnits();
        }
    }

    // 세이브 데이터 저장 처리 (단일 원천 데이터 저장)
    private void OnSave(DataSaveEvent evt)
    {
        if (evt.saveData == null) return;
        if (evt.saveData.unitDeck == null)
        {
            evt.saveData.unitDeck = new UnitDeckData();
        }

        evt.saveData.unitDeck.ownedUnits = new List<UnitSaveData>(_cachedOwnedUnits);
    }

    // 데이터 초기화 처리 (초기화 시 기본 유닛인 루카와 김하진 재할당)
    private void OnReset(DataResetEvent evt)
    {
        InitDefaultOwnedUnits();
    }

    // 기본 지급 유닛 초기화 헬퍼 (루카: ID 2 1성, 김하진: ID 4 2성, 0돌파)
    private void InitDefaultOwnedUnits()
    {
        _cachedOwnedUnits.Clear();

        // 1성 뱅가드 루카 (UNIT_0002, 0돌파 기본 보유)
        _cachedOwnedUnits.Add(new UnitSaveData
        {
            unitId = 2,
            level = 1,
            currentExp = 0L,
            breakThroughStep = 0,
            fragmentCount = 0
        });

        // 2성 가드 김하진 (UNIT_0004, 0돌파 기본 보유)
        _cachedOwnedUnits.Add(new UnitSaveData
        {
            unitId = 4,
            level = 1,
            currentExp = 0L,
            breakThroughStep = 0,
            fragmentCount = 0
        });
    }

    #endregion

    #region 보유 유닛 조회 및 갱신 API

    // 유닛 보유 여부 판정 연산 (정수 ID)
    public bool IsUnitOwned(int unitId)
    {
        return FindSaveDataByUnitId(unitId) != null;
    }

    // 유닛 보유 여부 판정 연산 (문자열 키)
    public bool IsUnitOwned(string unitKey)
    {
        int unitId = UnitIdHelper.ParseUnitId(unitKey);
        return IsUnitOwned(unitId);
    }

    // 보유 유닛 세이브 데이터 조회 (정수 ID)
    public UnitSaveData GetOwnedUnitSaveData(int unitId)
    {
        return FindSaveDataByUnitId(unitId);
    }

    // 보유 유닛 세이브 데이터 조회 (문자열 키)
    public UnitSaveData GetOwnedUnitSaveData(string unitKey)
    {
        int unitId = UnitIdHelper.ParseUnitId(unitKey);
        return FindSaveDataByUnitId(unitId);
    }

    // 보유 유닛 목록 읽기 전용 반환
    public IReadOnlyList<UnitSaveData> GetAllOwnedUnits()
    {
        return _cachedOwnedUnits;
    }

    // 유닛 레벨 및 경험치 갱신 처리 (문자열 키)
    public void UpdateUnitExpAndLevel(string unitKey, int newLevel, long newExp)
    {
        int unitId = UnitIdHelper.ParseUnitId(unitKey);
        UpdateUnitExpAndLevel(unitId, newLevel, newExp);
    }

    // 유닛 레벨 및 경험치 갱신 처리 (정수 ID)
    public void UpdateUnitExpAndLevel(int unitId, int newLevel, long newExp)
    {
        UnitSaveData saved = FindSaveDataByUnitId(unitId);
        if (saved != null)
        {
            saved.level = Mathf.Max(1, newLevel);
            saved.currentExp = Math.Max(0L, newExp);
        }
    }

    // 유닛 추가 또는 기존 정보 갱신 처리
    public void AddOrUpdateOwnedUnit(int unitId, int level, long exp, int breakThroughStep, int fragmentCount)
    {
        if (unitId <= 0) return;

        UnitSaveData existing = FindSaveDataByUnitId(unitId);
        if (existing == null)
        {
            _cachedOwnedUnits.Add(new UnitSaveData
            {
                unitId = unitId,
                level = Mathf.Max(1, level),
                currentExp = Math.Max(0L, exp),
                breakThroughStep = Mathf.Max(0, breakThroughStep),
                fragmentCount = Mathf.Max(0, fragmentCount)
            });
        }
        else
        {
            existing.level = Mathf.Max(1, level);
            existing.currentExp = Math.Max(0L, exp);
            existing.breakThroughStep = Mathf.Max(0, breakThroughStep);
            existing.fragmentCount = Mathf.Max(0, fragmentCount);
        }
    }

    #endregion

    #region 뷰모델 제공 메서드

    // 보관함 화면용 전체 유닛 뷰모델 목록 생성 및 반환
    public List<CollectionItemViewModel> GetCollectionViewModels(DeckType deckType = DeckType.Normal)
    {
        List<CollectionItemViewModel> list = new List<CollectionItemViewModel>();

        if (unitCatalog == null || unitCatalog.Units == null) return list;

        IReadOnlyList<UnitDataSO> units = unitCatalog.Units;

        for (int i = 0; i < units.Count; i++)
        {
            UnitDataSO unitSO = units[i];
            if (unitSO == null) continue;

            int unitId = UnitIdHelper.ParseUnitId(unitSO.UnitId);
            UnitSaveData savedUnit = FindSaveDataByUnitId(unitId);

            bool isOwned = savedUnit != null;
            int level = isOwned ? savedUnit.level : unitSO.InitialLevel;
            long currentExp = isOwned ? savedUnit.currentExp : 0L;
            int breakThroughStep = isOwned ? savedUnit.breakThroughStep : 0;
            int fragmentCount = isOwned ? savedUnit.fragmentCount : 0;

            // DeckManager를 통해 지정된 덱 내 장착 여부 및 슬롯 인덱스 조회 (Read-Only)
            bool isInDeck = false;
            int deckIndex = -1;
            if (DeckManager.Instance != null)
            {
                isInDeck = DeckManager.Instance.IsUnitInDeck(deckType, unitId, out deckIndex);
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

    #endregion

    #region 내부 헬퍼 메서드

    // 내부 세이브 캐시에서 유닛 ID로 검색
    private UnitSaveData FindSaveDataByUnitId(int unitId)
    {
        if (unitId <= 0 || _cachedOwnedUnits == null) return null;

        for (int i = 0; i < _cachedOwnedUnits.Count; i++)
        {
            if (_cachedOwnedUnits[i] != null && _cachedOwnedUnits[i].unitId == unitId)
            {
                return _cachedOwnedUnits[i];
            }
        }

        return null;
    }

    #endregion
}
