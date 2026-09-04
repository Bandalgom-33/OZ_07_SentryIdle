using System;
using System.Collections.Generic;
using System.Text;
using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        EventBus.Subscribe<RaidDeckChangedEvent>(OnRaidDeckChanged);

        RefreshDeckUnitsInspectorView();
    }

    // 전역 이벤트 버스 구독 해제
    private void OnDisable()
    {
        EventBus.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
        EventBus.Unsubscribe<NormalDeckChangedEvent>(OnNormalDeckChanged);
        EventBus.Unsubscribe<RaidDeckChangedEvent>(OnRaidDeckChanged);
    }

    #endregion

    #region 덱 이벤트 처리

    // 일반 덱 슬롯 변경 이벤트 수신 시 인스펙터 모니터링 갱신
    private void OnNormalDeckChanged(NormalDeckChangedEvent evt)
    {
        RefreshDeckUnitsInspectorView();
    }

    // 레이드 덱 슬롯 변경 이벤트 수신 시 인스펙터 모니터링 갱신
    private void OnRaidDeckChanged(RaidDeckChangedEvent evt)
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

        DistributeBattleExperience(eventMessage.rewardExp);
    }

    // 전투 상황(일반 스테이지 vs 레이드)에 따른 출전 유닛(100%) 및 비출전 보유 유닛(10%) 경험치 분배 연산
    public void DistributeBattleExperience(long baseExp)
    {
        if (baseExp <= 0L || DeckManager.Instance == null)
        {
            return;
        }

        List<int> activeDeckUnitIds = GetActiveBattleDeckUnitIds();
        HashSet<int> activeDeckUnitIdSet = new HashSet<int>(activeDeckUnitIds);

        int processedMainCount = 0;
        int processedSubCount = 0;

        for (int i = 0; i < activeDeckUnitIds.Count; i++)
        {
            int unitId = activeDeckUnitIds[i];
            if (unitId > 0 && ApplyExperienceToUnitInternal(unitId, baseExp, out _))
            {
                processedMainCount++;
            }
        }

        long subExp = Math.Max(1L, (long)Math.Floor(baseExp * 0.1f));
        if (CollectionDataProvider.Instance != null)
        {
            var allOwned = CollectionDataProvider.Instance.GetAllOwnedUnits();
            if (allOwned != null)
            {
                for (int i = 0; i < allOwned.Count; i++)
                {
                    int unitId = allOwned[i].unitId;
                    if (unitId > 0 && !activeDeckUnitIdSet.Contains(unitId))
                    {
                        if (ApplyExperienceToUnitInternal(unitId, subExp, out _))
                        {
                            processedSubCount++;
                        }
                    }
                }
            }
        }

        if (processedMainCount > 0 || processedSubCount > 0)
        {
            RefreshDeckUnitsInspectorView();
        }
    }

    // 기존 호환용 덱 경험치 지급 연산
    public void AddExperienceToDeckUnits(long exp)
    {
        DistributeBattleExperience(exp);
    }

    // 소모품 아이템 등을 통해 특정 단일 유닛에게 경험치 지급 연산 (문자열 ID 기반)
    public bool AddExperienceToUnit(string unitId, long exp)
    {
        if (string.IsNullOrWhiteSpace(unitId) || exp <= 0L)
        {
            return false;
        }

        int rawId = UnitIdHelper.ParseUnitId(unitId);
        bool success = ApplyExperienceToUnitInternal(rawId, exp, out bool didLevelUp);

        if (success)
        {
            RefreshDeckUnitsInspectorView();
        }

        return success;
    }

    // 소모품 아이템 등을 통해 특정 단일 유닛에게 경험치 지급 연산 (정수 ID 오버로딩)
    public bool AddExperienceToUnit(int unitId, long exp)
    {
        string unitIdStr = UnitIdHelper.ToUnitKey(unitId);
        return AddExperienceToUnit(unitIdStr, exp);
    }

    // 단일 유닛 경험치 주입 및 레벨업 내부 공용 처리 연산
    private bool ApplyExperienceToUnitInternal(int rawId, long exp, out bool didLevelUp)
    {
        didLevelUp = false;
        if (rawId <= 0 || exp <= 0L) return false;

        string unitIdStr = UnitIdHelper.ToUnitKey(rawId);
        UnitRuntimeState runtimeUnit = FindRuntimeUnitById(unitIdStr);

        if (runtimeUnit != null)
        {
            if (runtimeUnit.AddExperience(exp, out UnitLevelResult result))
            {
                didLevelUp = result.DidLevelUp;
                if (CollectionDataProvider.Instance != null)
                {
                    CollectionDataProvider.Instance.UpdateUnitExpAndLevel(rawId, result.CurrentLevel, result.CurrentExp);
                }
                EventBus.Publish(new UnitExpChangedEvent(rawId, unitIdStr, result.CurrentLevel, result.CurrentExp));
                return true;
            }
            return false;
        }

        UnitSaveData savedUnit = CollectionDataProvider.Instance != null ? CollectionDataProvider.Instance.GetOwnedUnitSaveData(rawId) : null;
        if (savedUnit == null) return false;

        if (unitCatalog != null && unitCatalog.TryGetById(unitIdStr, out UnitDataSO unitData) && unitData != null)
        {
            UnitProgressData progress = UnitProgressData.Create(unitData, savedUnit.level, savedUnit.currentExp, savedUnit.breakThroughStep);
            if (UnitProgressionService.TryAddExperience(unitData, progress, exp, out UnitLevelResult saveResult))
            {
                didLevelUp = saveResult.DidLevelUp;
                if (CollectionDataProvider.Instance != null)
                {
                    CollectionDataProvider.Instance.UpdateUnitExpAndLevel(rawId, progress.CurrentLevel, progress.CurrentExp);
                }
                EventBus.Publish(new UnitExpChangedEvent(rawId, unitIdStr, progress.CurrentLevel, progress.CurrentExp));
                return true;
            }
        }

        return false;
    }

    // 현재 전투 씬에 따른 출전 덱 유닛 ID 리스트 조회 연산
    private List<int> GetActiveBattleDeckUnitIds()
    {
        List<int> result = new List<int>();
        if (DeckManager.Instance == null) return result;

        bool isRaidScene = false;
        if (SceneLoader.Instance != null)
        {
            isRaidScene = SceneLoader.Instance.CurrentSceneType == SceneType.Raid;
        }
        else
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name != null && activeScene.name.IndexOf("Raid", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                isRaidScene = true;
            }
        }

        if (isRaidScene)
        {
            int[] r1 = DeckManager.Instance.GetDeckSlotsCopy(DeckType.Raid1);
            int[] r2 = DeckManager.Instance.GetDeckSlotsCopy(DeckType.Raid2);

            if (r1 != null)
            {
                for (int i = 0; i < r1.Length; i++) if (r1[i] > 0) result.Add(r1[i]);
            }
            if (r2 != null)
            {
                for (int i = 0; i < r2.Length; i++) if (r2[i] > 0) result.Add(r2[i]);
            }
        }
        else
        {
            int[] normal = DeckManager.Instance.GetDeckSlotsCopy(DeckType.Normal);
            if (normal != null)
            {
                for (int i = 0; i < normal.Length; i++) if (normal[i] > 0) result.Add(normal[i]);
            }
        }

        return result;
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
#if UNITY_EDITOR
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
#endif
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
