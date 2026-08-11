using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Runtime;
using UnityEngine;

namespace EndlessGuard.Unit.Prototype.Phase2
{
    [DisallowMultipleComponent]
    public sealed class ProgressionPrototypeController : MonoBehaviour
    {
        [Header("기준 캐릭터")]
        [SerializeField] private GameObject unitPrefab;

        [Header("Prototype 전용 레벨 성장")]
        [Tooltip("원본 성장 데이터는 건드리지 않고 Runtime 복제 데이터에만 적용됩니다.")]
        [SerializeField] private GrowthStatMask levelGrowthStats = GrowthStatMask.MaxHp | GrowthStatMask.PhysicalAttack | GrowthStatMask.MagicalAttack | GrowthStatMask.PhysicalDefense | GrowthStatMask.MagicalDefense;
        [Min(0f)]
        [SerializeField] private float levelGrowthPercentPerLevel = 1f;

        [Header("Prototype 전용 승급 성장")]
        [SerializeField] private GrowthStatMask promotionGrowthStats = GrowthStatMask.MaxHp | GrowthStatMask.PhysicalAttack | GrowthStatMask.MagicalAttack | GrowthStatMask.PhysicalDefense | GrowthStatMask.MagicalDefense;
        [Min(0f)]
        [SerializeField] private float promotionGrowthPercentPerStage = 5f;
        [Min(1)]
        [SerializeField] private int baseMaxLevel = 2;
        [Min(2)]
        [SerializeField] private int firstPromotionMaxLevel = 5;

        [Header("경험치 버튼")]
        [Min(0)]
        [SerializeField] private long customExperience = 1000L;

        [Header("배치")]
        [SerializeField] private Vector3 spawnPosition = Vector3.zero;
        [SerializeField] private Vector2Int tileCoordinate = Vector2Int.zero;

        [HideInInspector]
        [SerializeField] private int progressEventCount;
        [HideInInspector]
        [SerializeField] private string beforeSnapshot;
        [HideInInspector]
        [SerializeField] private string afterSnapshot;
        [HideInInspector]
        [TextArea(3, 8)]
        [SerializeField] private string lastMessage;

        private UnitRuntimeState spawnedUnit;
        private UnitDataSO runtimeUnitData;
        private UnitClassGrowthTableSO runtimeGrowthTable;

        public UnitRuntimeState SpawnedUnit => spawnedUnit;
        public int ProgressEventCount => progressEventCount;
        public string BeforeSnapshot => beforeSnapshot;
        public string AfterSnapshot => afterSnapshot;
        public string LastMessage => lastMessage;

        private void OnEnable()
        {
            UnitProgressEvents.OnUnitProgressChanged += HandleProgressChanged;
        }

        private void OnDisable()
        {
            UnitProgressEvents.OnUnitProgressChanged -= HandleProgressChanged;
            ResetPrototype();
        }

        public void SpawnTestUnit()
        {
            ResetPrototype();

            if (unitPrefab == null)
            {
                Fail("레벨/승급 검증에 사용할 Unit Prefab을 연결하세요.");
                return;
            }

            UnitDataLink sourceLink = unitPrefab.GetComponent<UnitDataLink>();

            if (sourceLink == null || !sourceLink.HasData)
            {
                Fail("Unit Prefab의 UnitDataLink에 정식 UnitDataSO가 연결되어 있지 않습니다.");
                return;
            }

            UnitDataSO source = sourceLink.UnitData;
            runtimeGrowthTable = Phase2PrototypeDataFactory.CreateGrowthTable(
                source.GrowthTable,
                source.Class,
                levelGrowthStats,
                levelGrowthPercentPerLevel,
                promotionGrowthStats,
                promotionGrowthPercentPerStage,
                baseMaxLevel,
                firstPromotionMaxLevel);

            runtimeUnitData = Phase2PrototypeDataFactory.CloneUnitData(
                source,
                null,
                null,
                null,
                runtimeGrowthTable,
                false);

            spawnedUnit = Phase2PrototypeSpawnUtility.SpawnUnit(unitPrefab, runtimeUnitData, transform, spawnPosition);

            if (spawnedUnit == null)
            {
                Fail("레벨/승급 검증 캐릭터 생성에 실패했습니다.");
                return;
            }

            spawnedUnit.GridPosition.Initialize(tileCoordinate, GridFacingDirection.North, CombatTargetLayer.Ground);
            progressEventCount = 0;
            beforeSnapshot = BuildSnapshot(spawnedUnit);
            afterSnapshot = beforeSnapshot;
            lastMessage = "레벨/승급 Prototype 캐릭터 준비 완료. 원본 성장 데이터는 변경되지 않았습니다.";
            Debug.Log(lastMessage, spawnedUnit);
        }

        public void AddOneLevelExperience()
        {
            if (!CanUseUnit())
            {
                return;
            }

            UnitLevelCurveSO curve = runtimeGrowthTable != null ? runtimeGrowthTable.LevelCurve : null;

            if (curve == null)
            {
                Fail("경험치 곡선이 없습니다.");
                return;
            }

            long required = curve.GetRequiredExp(spawnedUnit.CurrentLevel);
            ApplyExperience(required, $"현재 레벨 필요 경험치 {required}");
        }

        public void AddCustomExperience()
        {
            if (!CanUseUnit())
            {
                return;
            }

            ApplyExperience(customExperience, $"사용자 지정 경험치 {customExperience}");
        }

        public void ApplyApprovedPromotion()
        {
            if (!CanUseUnit())
            {
                return;
            }

            beforeSnapshot = BuildSnapshot(spawnedUnit);
            bool success = spawnedUnit.ApplyApprovedPromotion();
            afterSnapshot = BuildSnapshot(spawnedUnit);
            lastMessage = success
                ? $"승급 적용 PASS: {beforeSnapshot} -> {afterSnapshot}"
                : $"승급 적용 FAIL: 현재 Stage {spawnedUnit.PromotionStage}, MaxLv {spawnedUnit.MaxLevel}. Prototype 승급 최대 레벨 설정을 확인하세요.";

            if (success)
            {
                Debug.Log(lastMessage, spawnedUnit);
            }
            else
            {
                Debug.LogWarning(lastMessage, spawnedUnit);
            }
        }

        public void RefreshSnapshot()
        {
            if (!CanUseUnit())
            {
                return;
            }

            afterSnapshot = BuildSnapshot(spawnedUnit);
            lastMessage = afterSnapshot;
            Debug.Log(lastMessage, spawnedUnit);
        }

        public void ResetPrototype()
        {
            if (spawnedUnit != null)
            {
                Destroy(spawnedUnit.gameObject);
            }

            if (runtimeUnitData != null)
            {
                Destroy(runtimeUnitData);
            }

            if (runtimeGrowthTable != null)
            {
                Destroy(runtimeGrowthTable);
            }

            spawnedUnit = null;
            runtimeUnitData = null;
            runtimeGrowthTable = null;
            progressEventCount = 0;
            beforeSnapshot = string.Empty;
            afterSnapshot = string.Empty;
        }

        private void ApplyExperience(long amount, string label)
        {
            beforeSnapshot = BuildSnapshot(spawnedUnit);
            bool success = spawnedUnit.AddExperience(amount, out UnitLevelResult result);
            afterSnapshot = BuildSnapshot(spawnedUnit);

            lastMessage = success
                ? $"EXP 적용 PASS ({label}): Lv {result.PreviousLevel}->{result.CurrentLevel}, EXP {result.PreviousExp}->{result.CurrentExp}\n{beforeSnapshot}\n-> {afterSnapshot}"
                : $"EXP 적용 FAIL ({label})";

            if (success)
            {
                Debug.Log(lastMessage, spawnedUnit);
            }
            else
            {
                Debug.LogWarning(lastMessage, spawnedUnit);
            }
        }

        private void HandleProgressChanged(UnitProgressChangedInfo info)
        {
            if (spawnedUnit == null || info.Progress == null || !info.Progress.Matches(spawnedUnit.DataLink.UnitData))
            {
                return;
            }

            progressEventCount++;
            afterSnapshot = BuildSnapshot(spawnedUnit);
        }

        private bool CanUseUnit()
        {
            if (spawnedUnit != null && spawnedUnit.IsInitialized && spawnedUnit.Health != null && !spawnedUnit.Health.IsDead)
            {
                return true;
            }

            Fail("먼저 레벨/승급 검증 캐릭터를 생성하세요.");
            return false;
        }

        private static string BuildSnapshot(UnitRuntimeState unit)
        {
            if (unit == null || unit.Stats == null)
            {
                return "Unit 없음";
            }

            RuntimeStats stats = unit.Stats;
            return $"Lv {unit.CurrentLevel}/{unit.MaxLevel}, Promotion {unit.PromotionStage}, HP {stats.MaxHp:0.##}, PAtk {stats.PhysicalAttack:0.##}, MAtk {stats.MagicalAttack:0.##}, PDef {stats.PhysicalDefense:0.##}, MDef {stats.MagicalDefense:0.##}, APS {stats.AttacksPerSecond:0.###}";
        }

        private void Fail(string message)
        {
            lastMessage = message;
            Debug.LogError(message, this);
        }
    }
}
