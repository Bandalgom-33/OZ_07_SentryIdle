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

// 게임 내 몬스터 처치 경험치 분배 및 소모품 경험치 지급을 전담하는 싱글톤 매니저
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

    #region 라이프사이클

    // 초기 카탈로그 리소스 로드
    protected override void Awake()
    {
        base.Awake();

        if (unitCatalog == null)
        {
            unitCatalog = CollectionDataProvider.Instance != null 
                ? CollectionDataProvider.Instance.UnitCatalog 
                : Resources.Load<UnitCatalog>("Catalogs/UnitCatalog");
        }
    }

    // 전역 이벤트 버스 구독 등록
    private void OnEnable()
    {
        EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
        EventBus.Subscribe<NormalDeckChangedEvent>(OnNormalDeckChanged);

        RefreshDeckUnitsInspectorView();
    }

    // 전역 이벤트 버스 구독 해제
    private void OnDisable()
    {
        EventBus.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
        EventBus.Unsubscribe<NormalDeckChangedEvent>(OnNormalDeckChanged);
    }

    #endregion

    #region 덱 이벤트 처리

    // 일반 덱 슬롯 변경 이벤트 수신 시 인스펙터 모니터링 갱신
    private void OnNormalDeckChanged(NormalDeckChangedEvent evt)
    {
        RefreshDeckUnitsInspectorView();
    }

    #endregion

    #region 경험치 지급 핵심 로직

    // 적 사망 이벤트 수신 처리
    private void OnEnemyDied(EnemyDiedEvent eventMessage)
    {
        if (eventMessage.rewardExp <= 0)
        {
            return;
        }

        AddExperienceToDeckUnits(eventMessage.rewardExp);
    }

    // 현재 덱에 등록된 모든 유닛들에게 경험치 일괄 지급 연산
    public void AddExperienceToDeckUnits(long exp)
    {
        if (exp <= 0L || DeckManager.Instance == null)
        {
            return;
        }

        IReadOnlyList<int> currentSlots = DeckManager.Instance.DeckSlots;
        if (currentSlots == null)
        {
            return;
        }

        StringBuilder logBuilder = new StringBuilder();
        logBuilder.AppendLine($"[ExperienceManager] 몬스터 처치 경험치 획득: +{exp} EXP");

        int processedCount = 0;

        for (int i = 0; i < currentSlots.Count; i++)
        {
            int rawId = currentSlots[i];
            if (rawId <= 0) continue;

            string unitIdStr = UnitIdHelper.ToUnitKey(rawId);

            // 1. 해당 덱 유닛이 현재 필드에 스폰되어 있는지 확인
            UnitRuntimeState runtimeUnit = FindRuntimeUnitById(unitIdStr);

            if (runtimeUnit != null)
            {
                if (runtimeUnit.AddExperience(exp, out UnitLevelResult result))
                {
                    processedCount++;

                    if (CollectionDataProvider.Instance != null)
                    {
                        CollectionDataProvider.Instance.UpdateUnitExpAndLevel(rawId, result.CurrentLevel, result.CurrentExp);
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
                // 2. 필드에 아직 스폰되지 않은 덱 유닛은 CollectionDataProvider의 데이터에서 계산
                UnitSaveData savedUnit = CollectionDataProvider.Instance != null ? CollectionDataProvider.Instance.GetOwnedUnitSaveData(rawId) : null;
                int currentLvl = savedUnit != null ? savedUnit.level : 1;
                long currentE = savedUnit != null ? savedUnit.currentExp : 0L;
                int bStep = savedUnit != null ? savedUnit.breakThroughStep : 0;

                if (unitCatalog != null && unitCatalog.TryGetById(unitIdStr, out UnitDataSO unitData) && unitData != null)
                {
                    UnitProgressData progress = UnitProgressData.Create(unitData, currentLvl, currentE, bStep);
                    if (UnitProgressionService.TryAddExperience(unitData, progress, exp, out UnitLevelResult saveResult))
                    {
                        processedCount++;

                        if (CollectionDataProvider.Instance != null)
                        {
                            CollectionDataProvider.Instance.UpdateUnitExpAndLevel(rawId, progress.CurrentLevel, progress.CurrentExp);
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

    // 소모품 아이템 등을 통해 특정 단일 유닛에게 경험치 지급 연산 (문자열 ID 기반)
    public bool AddExperienceToUnit(string unitId, long exp)
    {
        if (string.IsNullOrWhiteSpace(unitId) || exp <= 0L)
        {
            return false;
        }

        int rawId = UnitIdHelper.ParseUnitId(unitId);

        // 1. 현재 필드에 스폰되어 활성화된 런타임 유닛 탐색
        UnitRuntimeState runtimeUnit = FindRuntimeUnitById(unitId);

        if (runtimeUnit != null)
        {
            int beforeLevel = runtimeUnit.CurrentLevel;
            long beforeExp = runtimeUnit.Progress != null ? runtimeUnit.Progress.CurrentExp : 0L;

            if (runtimeUnit.AddExperience(exp, out UnitLevelResult result))
            {
                if (CollectionDataProvider.Instance != null)
                {
                    CollectionDataProvider.Instance.UpdateUnitExpAndLevel(rawId, result.CurrentLevel, result.CurrentExp);
                }

                RefreshDeckUnitsInspectorView();

                string unitName = (runtimeUnit.DataLink != null && runtimeUnit.DataLink.HasData) ? runtimeUnit.DataLink.UnitData.DisplayName : unitId;
                string levelUpTag = result.DidLevelUp ? $" [★ LEVEL UP! Lv.{result.PreviousLevel} -> Lv.{result.CurrentLevel}]" : string.Empty;
                Debug.Log($"[ExperienceManager] 소모품 경험치 지급 (필드 유닛): [{unitId}] {unitName} +{exp:#,##0} EXP | [전] Lv.{result.PreviousLevel} (EXP: {result.PreviousExp:#,##0}) ➔ [후] Lv.{result.CurrentLevel} (EXP: {result.CurrentExp:#,##0}){levelUpTag}");
                return true;
            }

            return false;
        }

        // 2. 필드에 없는 유닛인 경우 CollectionDataProvider 데이터에서 계산
        UnitSaveData savedUnit = CollectionDataProvider.Instance != null ? CollectionDataProvider.Instance.GetOwnedUnitSaveData(rawId) : null;
        if (savedUnit == null)
        {
            Debug.LogWarning($"[ExperienceManager] 소모품 경험치 지급 실패: 보유하지 않은 유닛 ID ({unitId})");
            return false;
        }

        if (unitCatalog == null || !unitCatalog.TryGetById(unitId, out UnitDataSO unitData) || unitData == null)
        {
            Debug.LogWarning($"[ExperienceManager] 소모품 경험치 지급 실패: 카탈로그에서 유닛 정보를 찾을 수 없습니다 ({unitId})");
            return false;
        }

        int prevLevel = savedUnit.level;
        long prevExp = savedUnit.currentExp;

        UnitProgressData progress = UnitProgressData.Create(unitData, savedUnit.level, savedUnit.currentExp, savedUnit.breakThroughStep);
        if (UnitProgressionService.TryAddExperience(unitData, progress, exp, out UnitLevelResult saveResult))
        {
            if (CollectionDataProvider.Instance != null)
            {
                CollectionDataProvider.Instance.UpdateUnitExpAndLevel(rawId, progress.CurrentLevel, progress.CurrentExp);
            }

            RefreshDeckUnitsInspectorView();

            string levelUpTag = saveResult.DidLevelUp ? $" [★ LEVEL UP! Lv.{prevLevel} -> Lv.{saveResult.CurrentLevel}]" : string.Empty;
            Debug.Log($"[ExperienceManager] 소모품 경험치 지급 (보관함 유닛): [{unitId}] {unitData.DisplayName} +{exp:#,##0} EXP | [전] Lv.{prevLevel} (EXP: {prevExp:#,##0}) ➔ [후] Lv.{saveResult.CurrentLevel} (EXP: {saveResult.CurrentExp:#,##0}){levelUpTag}");
            return true;
        }

        return false;
    }

    // 소모품 아이템 등을 통해 특정 단일 유닛에게 경험치 지급 연산 (정수 ID 오버로딩)
    public bool AddExperienceToUnit(int unitId, long exp)
    {
        string unitIdStr = UnitIdHelper.ToUnitKey(unitId);
        return AddExperienceToUnit(unitIdStr, exp);
    }

    #endregion

    #region 유닛 초기화 보조 메서드

    // 유닛이 필드에 스폰될 때 CollectionDataProvider의 성장 데이터를 주입하는 동기화 헬퍼
    public void SyncProgressionToUnit(UnitRuntimeState unit)
    {
        if (unit == null || unit.IsSummon || unit.DataLink == null || !unit.DataLink.HasData)
        {
            return;
        }

        UnitDataSO unitData = unit.DataLink.UnitData;
        int rawId = UnitIdHelper.ParseUnitId(unitData.UnitId);
        UnitSaveData savedUnit = CollectionDataProvider.Instance != null ? CollectionDataProvider.Instance.GetOwnedUnitSaveData(rawId) : null;

        if (savedUnit != null)
        {
            UnitProgressData progress = UnitProgressData.Create(unitData, savedUnit.level, savedUnit.currentExp, savedUnit.breakThroughStep);
            unit.ApplyProgression(progress);
        }

        RefreshDeckUnitsInspectorView();
    }

    #endregion

    #region 인스펙터 디버그 뷰 갱신

    // 현재 덱에 편성된 유닛들의 정보를 인스펙터 디버그 리스트에 실시간 반영
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

            string unitIdStr = UnitIdHelper.ToUnitKey(rawId);
            string unitName = "Unknown";
            int level = 1;
            long exp = 0L;
            bool isFieldSpawned = false;

            if (unitCatalog != null && unitCatalog.TryGetById(unitIdStr, out UnitDataSO unitData) && unitData != null)
            {
                unitName = unitData.DisplayName;
            }

            UnitSaveData saved = CollectionDataProvider.Instance != null ? CollectionDataProvider.Instance.GetOwnedUnitSaveData(rawId) : null;
            if (saved != null)
            {
                level = saved.level;
                exp = saved.currentExp;
            }

            UnitRuntimeState runtimeUnit = FindRuntimeUnitById(unitIdStr);
            if (runtimeUnit != null)
            {
                isFieldSpawned = true;
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

    #endregion
}
