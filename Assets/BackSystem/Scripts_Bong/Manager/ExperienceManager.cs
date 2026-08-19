using System;
using System.Collections.Generic;
using System.Text;
using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Runtime;
using UnityEngine;

// 인스펙터에서 덱 편성 유닛 상태를 확인하기 위한 디버그 데이터 구조체
[Serializable]
public struct DeckUnitInspectorInfo
{
    [Tooltip("덱 슬롯 번호 (1 ~ 10)")]
    public int slotIndex;
    [Tooltip("유닛 식별 ID (예: UNIT_0001)")]
    public string unitId;
    [Tooltip("유닛 표시 이름")]
    public string unitName;
    [Tooltip("현재 수련 레벨")]
    public int level;
    [Tooltip("현재 누적 경험치량")]
    public long currentExp;
    [Tooltip("현재 필드 스폰/전투 배치 여부")]
    public bool isFieldSpawned;
}

// 게임 내 경험치 획득, 덱 유닛 분배, 소모품 지급 및 세이브 데이터 연동을 총괄하는 싱글톤 매니저
public class ExperienceManager : SingletonBase<ExperienceManager>
{
    #region 직렬화 변수 (인스펙터 바인딩)

    [Header("--- 유닛 카탈로그 참조 ---")]
    [Tooltip("게임 내 전체 유닛 데이터 조회를 위한 카탈로그 SO")]
    [SerializeField] private UnitCatalog unitCatalog;

    [Header("--- [Debug] 현재 덱 유닛 경험치 실시간 모니터링 ---")]
    [Tooltip("현재 덱에 편성된 유닛들의 실시간 성장 및 필드 배치 현황")]
    [SerializeField] private List<DeckUnitInspectorInfo> currentDeckUnits = new List<DeckUnitInspectorInfo>();

    #endregion

    #region 내부 캐시 필드

    // 세이브 데이터 내 보유 유닛 참조 캐시
    private List<UnitSaveData> _cachedOwnedUnits = new List<UnitSaveData>();

    #endregion

    #region 라이프사이클

    // 초기 카탈로그 리소스 로드
    protected override void Awake()
    {
        base.Awake();

        // 런타임에 인스펙터 참조가 비어있을 경우 Resources 폴더에서 카탈로그 로드
        if (unitCatalog == null)
        {
            unitCatalog = Resources.Load<UnitCatalog>("Catalogs/UnitCatalog");
        }
    }

    // 전역 이벤트 버스 구독 등록
    private void OnEnable()
    {
        // 적 유닛 사망 시 발행되는 보상 이벤트 수신
        EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
        // 일반 덱 슬롯 변경 이벤트 수신
        EventBus.Subscribe<NormalDeckChangedEvent>(OnNormalDeckChanged);
        // 레거시 덱 슬롯 변경 이벤트 수신
        EventBus.Subscribe<DeckChangedEvent>(OnDeckChanged);
        // 세이브 데이터 로드 및 저장 이벤트 수신
        EventBus.Subscribe<DataLoadEvent>(OnLoad);
        EventBus.Subscribe<DataSaveEvent>(OnSave);
        EventBus.Subscribe<DataResetEvent>(OnReset);

        RefreshDeckUnitsInspectorView();
    }

    // 전역 이벤트 버스 구독 해제
    private void OnDisable()
    {
        EventBus.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
        EventBus.Unsubscribe<NormalDeckChangedEvent>(OnNormalDeckChanged);
        EventBus.Unsubscribe<DeckChangedEvent>(OnDeckChanged);
        EventBus.Unsubscribe<DataLoadEvent>(OnLoad);
        EventBus.Unsubscribe<DataSaveEvent>(OnSave);
        EventBus.Unsubscribe<DataResetEvent>(OnReset);
    }

    #endregion

    #region 세이브 / 로드 및 덱 이벤트 처리

    // 일반 덱 슬롯 변경 이벤트 수신 시 인스펙터 모니터링 갱신
    private void OnNormalDeckChanged(NormalDeckChangedEvent evt)
    {
        RefreshDeckUnitsInspectorView();
    }

    // 덱 슬롯 변경 이벤트 수신 시 인스펙터 모니터링 갱신
    private void OnDeckChanged(DeckChangedEvent evt)
    {
        if (evt.deckType == DeckType.Normal)
        {
            RefreshDeckUnitsInspectorView();
        }
    }

    // 세이브 데이터 로드 시 유닛 성장 정보 캐시 동기화
    private void OnLoad(DataLoadEvent evt)
    {
        if (evt.saveData != null && evt.saveData.unitDeck != null)
        {
            _cachedOwnedUnits = evt.saveData.unitDeck.ownedUnits ?? new List<UnitSaveData>();
        }
        RefreshDeckUnitsInspectorView();
    }

    // 세이브 데이터 저장 시 최신 유닛 성장 정보 반영
    private void OnSave(DataSaveEvent evt)
    {
        if (evt.saveData != null && evt.saveData.unitDeck != null)
        {
            evt.saveData.unitDeck.ownedUnits = _cachedOwnedUnits;
        }
        RefreshDeckUnitsInspectorView();
    }

    // 데이터 리셋 시 캐시 초기화
    private void OnReset(DataResetEvent evt)
    {
        _cachedOwnedUnits.Clear();
        RefreshDeckUnitsInspectorView();
    }

    #endregion

    #region 경험치 지급 핵심 로직

    // 적 사망 이벤트 수신 처리
    private void OnEnemyDied(EnemyDiedEvent eventMessage)
    {
        // 처치 보상 경험치가 0 이하인 경우 처리 생략
        if (eventMessage.rewardExp <= 0)
        {
            return;
        }

        // 덱에 등록된 모든 유닛들에게 경험치 균등 전액 지급 (방안 A)
        AddExperienceToDeckUnits(eventMessage.rewardExp);
    }

    // 현재 덱에 등록된 모든 유닛(1~10번 슬롯)에게 경험치를 지급하는 메서드
    public void AddExperienceToDeckUnits(long exp)
    {
        if (exp <= 0L)
        {
            return;
        }

        IReadOnlyList<int> currentSlots = (DeckManager.Instance != null) ? DeckManager.Instance.DeckSlots : null;
        if (currentSlots == null)
        {
            return;
        }

        StringBuilder logBuilder = new StringBuilder();
        logBuilder.AppendLine($"[ExperienceManager] 몬스터 처치 경험치 획득: +{exp} EXP");

        int processedCount = 0;

        // 덱 10개 슬롯을 순회하여 등록된 유닛들에게 경험치 지급
        for (int i = 0; i < currentSlots.Count; i++)
        {
            int rawId = currentSlots[i];
            if (rawId <= 0)
            {
                continue;
            }

            string unitIdStr = $"UNIT_{rawId:D4}";

            // 1. 해당 덱 유닛이 현재 필드에 스폰되어 있는지 확인
            UnitRuntimeState runtimeUnit = FindRuntimeUnitById(unitIdStr);

            if (runtimeUnit != null)
            {
                // 필드 유닛에 직접 경험치 부여 (사망 유닛 포함)
                if (runtimeUnit.AddExperience(exp, out UnitLevelResult result))
                {
                    processedCount++;
                    UpdateUnitSaveData(unitIdStr, result.CurrentLevel, result.CurrentExp);

                    if (CollectionDataProvider.Instance != null)
                    {
                        CollectionDataProvider.Instance.UpdateUnitExpAndLevel(unitIdStr, result.CurrentLevel, result.CurrentExp);
                    }

                    string unitName = (runtimeUnit.DataLink != null && runtimeUnit.DataLink.HasData) ? runtimeUnit.DataLink.UnitData.DisplayName : unitIdStr;
                    string levelUpTag = result.DidLevelUp ? $" [★ LEVEL UP! Lv.{result.PreviousLevel} -> Lv.{result.CurrentLevel}]" : string.Empty;
                    string maxLevelTag = result.ReachedMaxLevel ? " [MAX LEVEL]" : string.Empty;
                    string discardedTag = result.DiscardedExp > 0L ? $" (초과 소멸: -{result.DiscardedExp} EXP)" : string.Empty;

                    logBuilder.AppendLine($"  - [Slot {i + 1}] [{unitIdStr}] {unitName} : Lv.{result.CurrentLevel} | EXP: {result.PreviousExp} -> {result.CurrentExp} (소모: {result.ConsumedExp}){levelUpTag}{maxLevelTag}{discardedTag}");
                }
            }
            else
            {
                // 2. 필드에 아직 스폰되지 않은 덱 유닛은 세이브 데이터에서 직접 계산
                UnitSaveData savedUnit = FindSaveDataByUnitId(unitIdStr);
                if (savedUnit == null)
                {
                    savedUnit = new UnitSaveData { unitId = rawId, level = 1, currentExp = 0L };
                    _cachedOwnedUnits.Add(savedUnit);
                }

                if (unitCatalog != null && unitCatalog.TryGetById(unitIdStr, out UnitDataSO unitData) && unitData != null)
                {
                    UnitProgressData progress = UnitProgressData.Create(unitData, savedUnit.level, savedUnit.currentExp, savedUnit.breakThroughStep);
                    if (UnitProgressionService.TryAddExperience(unitData, progress, exp, out UnitLevelResult saveResult))
                    {
                        processedCount++;
                        savedUnit.level = progress.CurrentLevel;
                        savedUnit.currentExp = progress.CurrentExp;

                        if (CollectionDataProvider.Instance != null)
                        {
                            CollectionDataProvider.Instance.UpdateUnitExpAndLevel(unitIdStr, progress.CurrentLevel, progress.CurrentExp);
                        }

                        string levelUpTag = saveResult.DidLevelUp ? $" [★ LEVEL UP! Lv.{saveResult.PreviousLevel} -> Lv.{saveResult.CurrentLevel}]" : string.Empty;
                        string maxLevelTag = saveResult.ReachedMaxLevel ? " [MAX LEVEL]" : string.Empty;
                        string discardedTag = saveResult.DiscardedExp > 0L ? $" (초과 소멸: -{saveResult.DiscardedExp} EXP)" : string.Empty;

                        logBuilder.AppendLine($"  - [Slot {i + 1}] [{unitIdStr}] {unitData.DisplayName} (보관함) : Lv.{saveResult.CurrentLevel} | EXP: {saveResult.PreviousExp} -> {saveResult.CurrentExp} (소모: {saveResult.ConsumedExp}){levelUpTag}{maxLevelTag}{discardedTag}");
                    }
                }
            }
        }

        if (processedCount > 0)
        {
            Debug.Log(logBuilder.ToString());
            RefreshDeckUnitsInspectorView();
        }
    }

    // 소모품 아이템 등을 통해 특정 단일 유닛에게 경험치를 지급하는 메서드 (문자열 ID 기반)
    public bool AddExperienceToUnit(string unitId, long exp)
    {
        if (string.IsNullOrWhiteSpace(unitId) || exp <= 0L)
        {
            return false;
        }

        // 1. 현재 필드에 스폰되어 활성화된 런타임 유닛이 있는지 먼저 탐색
        UnitRuntimeState runtimeUnit = FindRuntimeUnitById(unitId);

        if (runtimeUnit != null)
        {
            // 필드 런타임 유닛에 경험치 가산 및 스탯 갱신
            if (runtimeUnit.AddExperience(exp, out UnitLevelResult result))
            {
                UpdateUnitSaveData(unitId, result.CurrentLevel, result.CurrentExp);

                if (CollectionDataProvider.Instance != null)
                {
                    CollectionDataProvider.Instance.UpdateUnitExpAndLevel(unitId, result.CurrentLevel, result.CurrentExp);
                }

                RefreshDeckUnitsInspectorView();

                string unitName = (runtimeUnit.DataLink != null && runtimeUnit.DataLink.HasData) ? runtimeUnit.DataLink.UnitData.DisplayName : unitId;
                string levelUpTag = result.DidLevelUp ? $" [★ LEVEL UP! Lv.{result.PreviousLevel} -> Lv.{result.CurrentLevel}]" : string.Empty;
                Debug.Log($"[ExperienceManager] 소모품 경험치 지급 (필드 유닛): [{unitId}] {unitName} +{exp} EXP (Lv.{result.CurrentLevel}, EXP: {result.CurrentExp}){levelUpTag}");
                return true;
            }

            return false;
        }

        // 2. 필드에 없는 유닛(보관함에만 존재하는 유닛)인 경우 세이브 데이터에서 직접 계산
        UnitSaveData savedUnit = FindSaveDataByUnitId(unitId);
        if (savedUnit == null)
        {
            Debug.LogWarning($"[ExperienceManager] 소모품 경험치 지급 실패: 보유하지 않은 유닛 ID ({unitId})");
            return false;
        }

        // 카탈로그에서 유닛 메타데이터 조회
        if (unitCatalog == null || !unitCatalog.TryGetById(unitId, out UnitDataSO unitData) || unitData == null)
        {
            Debug.LogWarning($"[ExperienceManager] 소모품 경험치 지급 실패: 카탈로그에서 유닛 정보를 찾을 수 없습니다 ({unitId})");
            return false;
        }

        // UnitProgressData 인스턴스를 임시 생성하여 도메인 레벨 계산기 실행
        UnitProgressData progress = UnitProgressData.Create(unitData, savedUnit.level, savedUnit.currentExp, savedUnit.breakThroughStep);
        if (UnitProgressionService.TryAddExperience(unitData, progress, exp, out UnitLevelResult saveResult))
        {
            // 계산된 결과를 세이브 캐시에 반영
            savedUnit.level = progress.CurrentLevel;
            savedUnit.currentExp = progress.CurrentExp;

            if (CollectionDataProvider.Instance != null)
            {
                CollectionDataProvider.Instance.UpdateUnitExpAndLevel(unitId, progress.CurrentLevel, progress.CurrentExp);
            }

            RefreshDeckUnitsInspectorView();

            string levelUpTag = saveResult.DidLevelUp ? $" [★ LEVEL UP! Lv.{saveResult.PreviousLevel} -> Lv.{saveResult.CurrentLevel}]" : string.Empty;
            Debug.Log($"[ExperienceManager] 소모품 경험치 지급 (보관함 유닛): [{unitId}] {unitData.DisplayName} +{exp} EXP (Lv.{saveResult.CurrentLevel}, EXP: {saveResult.CurrentExp}){levelUpTag}");
            return true;
        }

        return false;
    }

    // 소모품 아이템 등을 통해 특정 단일 유닛에게 경험치를 지급하는 메서드 (정수 ID 오버로딩)
    public bool AddExperienceToUnit(int unitId, long exp)
    {
        string unitIdStr = $"UNIT_{unitId:D4}";
        return AddExperienceToUnit(unitIdStr, exp);
    }

    #endregion

    #region 유닛 초기화 보조 메서드

    // 유닛이 필드에 스폰될 때 세이브 데이터의 레벨과 누적 경험치를 주입하는 동기화 헬퍼 (UnitRuntimeState 무수정 지원용)
    public void SyncProgressionToUnit(UnitRuntimeState unit)
    {
        if (unit == null || unit.IsSummon || unit.DataLink == null || !unit.DataLink.HasData)
        {
            return;
        }

        UnitDataSO unitData = unit.DataLink.UnitData;
        UnitSaveData savedUnit = FindSaveDataByUnitId(unitData.UnitId);

        if (savedUnit != null)
        {
            // 세이브된 레벨 및 경험치로 UnitProgressData를 생성하여 주입
            UnitProgressData progress = UnitProgressData.Create(unitData, savedUnit.level, savedUnit.currentExp, savedUnit.breakThroughStep);
            unit.ApplyProgression(progress);
        }

        RefreshDeckUnitsInspectorView();
    }

    #endregion

    #region 인스펙터 디버그 뷰 갱신

    // 현재 덱에 편성된 10개 슬롯 유닛들의 정보를 인스펙터 디버그 리스트에 실시간 반영
    public void RefreshDeckUnitsInspectorView()
    {
        currentDeckUnits.Clear();

        IReadOnlyList<int> currentSlots = (DeckManager.Instance != null) ? DeckManager.Instance.DeckSlots : null;
        if (currentSlots == null)
        {
            return;
        }

        for (int i = 0; i < currentSlots.Count; i++)
        {
            int rawId = currentSlots[i];
            if (rawId <= 0)
            {
                continue;
            }

            string unitIdStr = $"UNIT_{rawId:D4}";
            string unitName = "Unknown";
            int level = 1;
            long exp = 0L;
            bool isFieldSpawned = false;

            // 카탈로그에서 유닛 표시 이름 조회
            if (unitCatalog != null && unitCatalog.TryGetById(unitIdStr, out UnitDataSO unitData) && unitData != null)
            {
                unitName = unitData.DisplayName;
            }

            // 세이브 캐시에서 레벨 및 누적 경험치 조회
            UnitSaveData saved = FindSaveDataByUnitId(unitIdStr);
            if (saved != null)
            {
                level = saved.level;
                exp = saved.currentExp;
            }

            // 전투 레지스트리를 통해 필드 스폰 여부 확인
            UnitRuntimeState runtimeUnit = FindRuntimeUnitById(unitIdStr);
            if (runtimeUnit != null)
            {
                isFieldSpawned = true;
                // 필드 유닛의 실시간 레벨/경험치로 최신화
                level = runtimeUnit.CurrentLevel;
                exp = runtimeUnit.Progress != null ? runtimeUnit.Progress.CurrentExp : exp;
            }

            currentDeckUnits.Add(new DeckUnitInspectorInfo
            {
                slotIndex = i + 1,
                unitId = unitIdStr,
                unitName = unitName,
                level = level,
                currentExp = exp,
                isFieldSpawned = isFieldSpawned
            });
        }
    }

    #endregion

    #region 내부 헬퍼 메서드

    // 필드 전투 레지스트리에서 유닛 ID로 런타임 유닛 조회
    private UnitRuntimeState FindRuntimeUnitById(string unitId)
    {
        if (string.IsNullOrEmpty(unitId)) return null;

        foreach (UnitRuntimeState unit in CombatRegistry.Units)
        {
            if (unit != null && !unit.IsSummon && string.Equals(unit.UnitId, unitId, StringComparison.Ordinal))
            {
                return unit;
            }
        }

        return null;
    }

    // 유닛 ID(문자열) 기반 세이브 캐시 조회
    private UnitSaveData FindSaveDataByUnitId(string unitIdStr)
    {
        if (_cachedOwnedUnits == null || string.IsNullOrEmpty(unitIdStr)) return null;

        int targetId = ParseUnitId(unitIdStr);

        for (int i = 0; i < _cachedOwnedUnits.Count; i++)
        {
            UnitSaveData u = _cachedOwnedUnits[i];
            if (u != null && u.unitId == targetId)
            {
                return u;
            }
        }

        return null;
    }

    // 세이브 캐시의 유닛 레벨 및 누적 경험치 갱신
    private void UpdateUnitSaveData(string unitIdStr, int newLevel, long newExp)
    {
        UnitSaveData saved = FindSaveDataByUnitId(unitIdStr);
        if (saved != null)
        {
            saved.level = Mathf.Max(1, newLevel);
            saved.currentExp = Math.Max(0L, newExp);
        }
    }

    // 유닛 ID 파싱 유틸리티 (UNIT_0001 -> 1)
    private int ParseUnitId(string unitIdStr)
    {
        if (int.TryParse(unitIdStr.Replace("UNIT_", ""), out int parsedId))
        {
            return parsedId;
        }
        return unitIdStr.GetHashCode();
    }

    #endregion
}
