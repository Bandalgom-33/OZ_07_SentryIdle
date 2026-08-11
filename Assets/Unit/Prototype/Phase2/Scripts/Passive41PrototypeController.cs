using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Runtime;
using UnityEngine;

namespace EndlessGuard.Unit.Prototype.Phase2
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CombatLoop))]
    public sealed class Passive41PrototypeController : MonoBehaviour
    {
        private const int RequiredPassiveCount = 41;
        private const int OpponentCount = 3;

        [Header("기준 Prefab")]
        [Tooltip("정식 캐릭터 Prefab을 연결합니다. 조성원처럼 UnitRuntimeState가 완성된 Prefab을 사용합니다.")]
        [SerializeField] private GameObject unitPrefab;

        [Tooltip("정식 몬스터 Prefab을 연결합니다. 요르문간드처럼 EnemyRuntimeState가 완성된 Prefab을 사용합니다.")]
        [SerializeField] private GameObject enemyPrefab;

        [Header("소환 패시브 검증용 Prefab")]
        [Tooltip("치명타 소환 등 캐릭터 측 소환 패시브에서 사용할 임시/정식 소환물 Prefab입니다.")]
        [SerializeField] private GameObject unitSummonPrefab;

        [Tooltip("주기적 소환 등 몬스터 측 소환 패시브에서 사용할 임시/정식 소환물 Prefab입니다.")]
        [SerializeField] private GameObject enemySummonPrefab;

        [Tooltip("주기적 소환 패시브를 빠르게 확인할 Prototype 전용 주기입니다. 원본 PassiveDataSO는 수정하지 않습니다.")]
        [Min(0.1f)]
        [SerializeField] private float summonIntervalOverrideSeconds = 1f;

        [Header("간단 전투 배치")]
        [SerializeField] private Vector3 worldOrigin = Vector3.zero;
        [Min(0.01f)]
        [SerializeField] private float tileWorldSize = 1f;
        [SerializeField] private float unitHeight;
        [SerializeField] private float enemyHeight;
        [SerializeField] private Vector2Int ownerUnitTile = Vector2Int.zero;
        [SerializeField] private Vector2Int enemyStartTile = new Vector2Int(0, 3);
        [SerializeField] private Vector2Int goalTile = new Vector2Int(0, -3);

        [Header("검증 편의")]
        [Tooltip("치명타 관련 패시브를 확인하기 쉽도록 Runtime 복제 데이터의 치명타 확률만 100%로 만듭니다. 원본 데이터는 변경하지 않습니다.")]
        [SerializeField] private bool forceCriticalChance100 = true;

        [Range(1f, 99f)]
        [SerializeField] private float damageToRemainingHpPercent = 30f;

        [HideInInspector]
        [SerializeField] private List<PassiveDataSO> officialPassives = new List<PassiveDataSO>();

        [HideInInspector]
        [SerializeField] private int selectedPassiveIndex;

        [HideInInspector]
        [SerializeField] private int coverageRegisteredCount;
        [HideInInspector]
        [SerializeField] private int coverageUnsupportedCount;
        [HideInInspector]
        [SerializeField] private int coverageInvalidCompatibilityCount;
        [HideInInspector]
        [SerializeField] private int activeAssignedPassiveCount;
        [HideInInspector]
        [SerializeField] private int activeAppliedPassiveCount;
        [HideInInspector]
        [SerializeField] private int activeRejectedPassiveCount;
        [HideInInspector]
        [SerializeField] private int activeUnsupportedPassiveCount;
        [HideInInspector]
        [TextArea(3, 8)]
        [SerializeField] private string lastMessage;

        private readonly List<GameObject> spawnedObjects = new List<GameObject>(8);
        private readonly List<ScriptableObject> runtimeDataObjects = new List<ScriptableObject>(8);
        private readonly List<UnitRuntimeState> spawnedUnits = new List<UnitRuntimeState>(OpponentCount);
        private readonly List<EnemyRuntimeState> spawnedEnemies = new List<EnemyRuntimeState>(OpponentCount);

        private CombatLoop combatLoop;
        private UnitRuntimeState activeUnit;
        private EnemyRuntimeState activeEnemy;
        private bool selectedPassiveUsesUnit;

        public IReadOnlyList<PassiveDataSO> OfficialPassives => officialPassives;
        public int SelectedPassiveIndex => selectedPassiveIndex;
        public PassiveDataSO SelectedPassive => selectedPassiveIndex >= 0 && selectedPassiveIndex < officialPassives.Count ? officialPassives[selectedPassiveIndex] : null;
        public int CoverageRegisteredCount => coverageRegisteredCount;
        public int CoverageUnsupportedCount => coverageUnsupportedCount;
        public int CoverageInvalidCompatibilityCount => coverageInvalidCompatibilityCount;
        public int ActiveAssignedPassiveCount => activeAssignedPassiveCount;
        public int ActiveAppliedPassiveCount => activeAppliedPassiveCount;
        public int ActiveRejectedPassiveCount => activeRejectedPassiveCount;
        public int ActiveUnsupportedPassiveCount => activeUnsupportedPassiveCount;
        public UnitRuntimeState ActiveUnit => activeUnit;
        public EnemyRuntimeState ActiveEnemy => activeEnemy;
        public bool SelectedPassiveUsesUnit => selectedPassiveUsesUnit;
        public string LastMessage => lastMessage;

        private void Awake()
        {
            combatLoop = GetComponent<CombatLoop>();
        }

        private void OnDisable()
        {
            ResetScenario();
        }

        public void SetSelectedPassiveIndex(int index)
        {
            selectedPassiveIndex = Mathf.Clamp(index, 0, Mathf.Max(0, officialPassives.Count - 1));
        }

        public bool ValidateCoverage()
        {
            coverageRegisteredCount = 0;
            coverageUnsupportedCount = 0;
            coverageInvalidCompatibilityCount = 0;

            HashSet<int> uniqueIds = new HashSet<int>();

            for (int i = 0; i < officialPassives.Count; i++)
            {
                PassiveDataSO passive = officialPassives[i];

                if (passive == null || !uniqueIds.Add(passive.GetInstanceID()))
                {
                    coverageUnsupportedCount++;
                    continue;
                }

                if (!PassiveRegistry.TryGet(passive, out _))
                {
                    coverageUnsupportedCount++;
                    continue;
                }

                coverageRegisteredCount++;

                bool unitValid = passive.CanBeUsedByUnit(UnitClass.Specialist);
                bool enemyValid = false;

                if (passive.Compatibility != null && passive.Compatibility.AllowedEnemySizes != null)
                {
                    IReadOnlyList<EnemySize> sizes = passive.Compatibility.AllowedEnemySizes;

                    for (int sizeIndex = 0; sizeIndex < sizes.Count; sizeIndex++)
                    {
                        if (passive.CanBeUsedByEnemy(sizes[sizeIndex]))
                        {
                            enemyValid = true;
                            break;
                        }
                    }
                }

                bool declaredUnit = passive.UsableBy == PassiveUserType.Unit || passive.UsableBy == PassiveUserType.Both;
                bool declaredEnemy = passive.UsableBy == PassiveUserType.Enemy || passive.UsableBy == PassiveUserType.Both;

                if ((declaredUnit && !unitValid) || (declaredEnemy && !enemyValid))
                {
                    coverageInvalidCompatibilityCount++;
                }
            }

            bool passed = officialPassives.Count == RequiredPassiveCount
                && uniqueIds.Count == RequiredPassiveCount
                && coverageRegisteredCount == RequiredPassiveCount
                && coverageUnsupportedCount == 0
                && coverageInvalidCompatibilityCount == 0;

            lastMessage = passed
                ? $"패시브 정적 커버리지 PASS: {coverageRegisteredCount}/{RequiredPassiveCount}, 중복/미지원 0, 호환 오류 0"
                : $"패시브 정적 커버리지 FAIL: 목록 {officialPassives.Count}, 고유 {uniqueIds.Count}, Registry {coverageRegisteredCount}, 미지원 {coverageUnsupportedCount}, 호환 오류 {coverageInvalidCompatibilityCount}";

            LogResult(passed, lastMessage);
            return passed;
        }

        public void SpawnSelectedScenario()
        {
            ResetScenario();

            PassiveDataSO passive = SelectedPassive;

            if (passive == null)
            {
                Fail("선택된 패시브가 없습니다. 먼저 정식 패시브 41개를 자동 연결하세요.");
                return;
            }

            if (!TryGetSourceData(out UnitDataSO unitSource, out EnemyDataSO enemySource))
            {
                return;
            }

            bool canUseUnit = passive.CanBeUsedByUnit(UnitClass.Specialist);
            EnemySize compatibleSize = Phase2PrototypeDataFactory.ResolveCompatibleEnemySize(passive, enemySource.Size);
            bool canUseEnemy = passive.CanBeUsedByEnemy(compatibleSize);

            if (!canUseUnit && !canUseEnemy)
            {
                Fail($"{passive.DisplayName} 패시브를 현재 Prototype 기준 캐릭터/몬스터에 적용할 수 없습니다.");
                return;
            }

            selectedPassiveUsesUnit = canUseUnit;

            if (selectedPassiveUsesUnit)
            {
                SpawnUnitPassiveScenario(unitSource, enemySource, passive);
            }
            else
            {
                SpawnEnemyPassiveScenario(unitSource, enemySource, passive, compatibleSize);
            }

            if (activeUnit == null || activeEnemy == null)
            {
                Fail("패시브 검증 대상 생성에 실패했습니다. Prefab 구성과 DataLink를 확인하세요.");
                return;
            }

            if (combatLoop == null)
            {
                combatLoop = GetComponent<CombatLoop>();
            }

            combatLoop?.StartLoop();
            RefreshPassiveCounters();

            lastMessage = $"패시브 시나리오 준비 완료: {passive.DisplayName} / 적용 주체 {(selectedPassiveUsesUnit ? "캐릭터" : "몬스터")} / Applied {activeAppliedPassiveCount}";
            Debug.Log(lastMessage, this);
        }

        public void StartCombat()
        {
            combatLoop?.StartLoop();
            lastMessage = "패시브 Prototype 전투 루프 시작";
            Debug.Log(lastMessage, this);
        }

        public void StopCombat()
        {
            combatLoop?.StopLoop();
            lastMessage = "패시브 Prototype 전투 루프 정지";
            Debug.Log(lastMessage, this);
        }

        public void ForceBlockPossibleEnemies()
        {
            int boundCount = 0;

            if (selectedPassiveUsesUnit && activeUnit != null && activeUnit.Block != null)
            {
                for (int i = 0; i < spawnedEnemies.Count; i++)
                {
                    EnemyRuntimeState enemy = spawnedEnemies[i];

                    if (enemy != null && enemy.Block != null && BlockLink.TryBind(activeUnit.Block, enemy.Block))
                    {
                        boundCount++;
                    }
                }
            }
            else if (!selectedPassiveUsesUnit && activeEnemy != null && activeEnemy.Block != null)
            {
                for (int i = 0; i < spawnedUnits.Count; i++)
                {
                    UnitRuntimeState unit = spawnedUnits[i];

                    if (unit != null && unit.Block != null && BlockLink.TryBind(unit.Block, activeEnemy.Block))
                    {
                        boundCount++;
                        break;
                    }
                }
            }

            lastMessage = $"강제 저지 연결 결과: {boundCount}개";
            Debug.Log(lastMessage, this);
        }

        public void ReleaseAllBlocks()
        {
            int released = 0;

            for (int i = 0; i < spawnedEnemies.Count; i++)
            {
                EnemyRuntimeState enemy = spawnedEnemies[i];

                if (enemy != null && enemy.Block != null && BlockLink.Release(enemy.Block))
                {
                    released++;
                }
            }

            lastMessage = $"저지 해제 결과: {released}개";
            Debug.Log(lastMessage, this);
        }

        public void DamageOwnerToConfiguredPercent()
        {
            if (selectedPassiveUsesUnit)
            {
                DamageToRemainingPercent(activeUnit, damageToRemainingHpPercent);
            }
            else
            {
                DamageToRemainingPercent(activeEnemy, damageToRemainingHpPercent);
            }
        }

        public void DamagePrimaryOpponentToConfiguredPercent()
        {
            if (selectedPassiveUsesUnit)
            {
                DamageToRemainingPercent(activeEnemy, damageToRemainingHpPercent);
            }
            else
            {
                DamageToRemainingPercent(activeUnit, damageToRemainingHpPercent);
            }
        }

        public void KillOwner()
        {
            if (selectedPassiveUsesUnit)
            {
                Kill(activeUnit);
            }
            else
            {
                Kill(activeEnemy);
            }
        }

        public void KillPrimaryOpponent()
        {
            if (selectedPassiveUsesUnit)
            {
                Kill(activeEnemy);
            }
            else
            {
                Kill(activeUnit);
            }
        }

        public void NotifyUnitSkillSucceeded()
        {
            UnitRuntimeState target = selectedPassiveUsesUnit ? activeUnit : (spawnedUnits.Count > 0 ? spawnedUnits[0] : activeUnit);

            if (target == null)
            {
                Fail("스킬 성공 이벤트를 보낼 캐릭터가 없습니다.");
                return;
            }

            PassiveRuntimeEvents.NotifyUnitSkillSucceeded(target);
            lastMessage = $"스킬 성공 신호 발생: {target.UnitId}";
            Debug.Log(lastMessage, target);
        }

        public void SimulateOwnerEvadeSuccess()
        {
            if (!selectedPassiveUsesUnit || activeUnit == null || activeEnemy == null || activeUnit.Passives == null)
            {
                Fail("회피 성공 Probe는 캐릭터 패시브 시나리오를 먼저 생성해야 합니다.");
                return;
            }

            float attackPower = activeEnemy.Stats != null ? Mathf.Max(1f, activeEnemy.Stats.PhysicalAttack) : 1f;
            float defense = activeUnit.Stats != null ? Mathf.Max(0f, activeUnit.Stats.PhysicalDefense) : 0f;
            BasicAttackResult missed = BasicAttackResult.Missed(DamageType.Physical, attackPower, defense, 0f);
            activeUnit.Passives.NotifyBasicAttackReceived(activeUnit, activeEnemy, missed);
            lastMessage = "캐릭터 회피 성공 Probe 전달 완료. DefenseBuff/CostGain 회피 조건을 확인하세요.";
            Debug.Log(lastMessage, activeUnit);
        }

        public void InjectNegativeStatusToOwner()
        {
            PassiveDataSO passive = SelectedPassive;

            if (passive == null)
            {
                Fail("선택된 패시브가 없습니다.");
                return;
            }

            bool applied = selectedPassiveUsesUnit
                ? activeUnit != null && activeUnit.Statuses != null && activeUnit.Statuses.ApplyTimedModifier(this, passive, PassiveStatType.MoveSpeed, 0f, -50f, 10f, true)
                : activeEnemy != null && activeEnemy.Statuses != null && activeEnemy.Statuses.ApplyTimedModifier(this, passive, PassiveStatType.MoveSpeed, 0f, -50f, 10f, true);

            lastMessage = applied
                ? "검증용 이동속도 -50% 디버프를 패시브 주체에 적용했습니다. 정화 계열은 주기 후 제거되는지 확인하세요."
                : "검증용 디버프 적용에 실패했습니다.";
            LogResult(applied, lastMessage);
        }

        public void SpawnUnitSummonSignal()
        {
            if (!selectedPassiveUsesUnit || activeUnit == null || unitSummonPrefab == null)
            {
                Fail("캐릭터 패시브 시나리오와 UnitSummonRuntime Prefab을 먼저 준비하세요.");
                return;
            }

            bool success = SummonService.TrySpawn(new SummonRequest(activeUnit, unitSummonPrefab, 1, this), out int spawnedCount);
            lastMessage = $"아군 소환물 생성 Probe {(success ? "PASS" : "FAIL")}: {spawnedCount}개";
            LogResult(success, lastMessage);
        }

        public void ReleaseUnitSummonSignals()
        {
            int released = ReleaseOwnedUnitSummons();
            lastMessage = $"아군 소환물 해제 Probe: {released}개";
            Debug.Log(lastMessage, this);
        }

        public void ResetScenario()
        {
            combatLoop?.StopLoop();
            ReleaseOwnedUnitSummons();

            for (int i = spawnedObjects.Count - 1; i >= 0; i--)
            {
                if (spawnedObjects[i] != null)
                {
                    Destroy(spawnedObjects[i]);
                }
            }

            for (int i = runtimeDataObjects.Count - 1; i >= 0; i--)
            {
                if (runtimeDataObjects[i] != null)
                {
                    Destroy(runtimeDataObjects[i]);
                }
            }

            spawnedObjects.Clear();
            runtimeDataObjects.Clear();
            spawnedUnits.Clear();
            spawnedEnemies.Clear();
            activeUnit = null;
            activeEnemy = null;
            selectedPassiveUsesUnit = false;
            activeAssignedPassiveCount = 0;
            activeAppliedPassiveCount = 0;
            activeRejectedPassiveCount = 0;
            activeUnsupportedPassiveCount = 0;
        }

        private void SpawnUnitPassiveScenario(UnitDataSO unitSource, EnemyDataSO enemySource, PassiveDataSO passive)
        {
            UnitDataSO unitData = Phase2PrototypeDataFactory.CloneUnitData(
                unitSource,
                passive,
                unitSummonPrefab,
                null,
                null,
                forceCriticalChance100);
            TrackRuntimeData(unitData);

            activeUnit = SpawnUnit(unitData, ownerUnitTile);

            EnemySize opponentSize = ResolveRequiredOpponentSize(passive, enemySource.Size);

            EnemyMovementType? opponentMovement = passive is AirAttackSO || passive is MasterSO
                ? EnemyMovementType.Air
                : (EnemyMovementType?)null;

            for (int i = 0; i < OpponentCount; i++)
            {
                EnemyDataSO enemyData = Phase2PrototypeDataFactory.CloneEnemyData(
                    enemySource,
                    null,
                    null,
                    0f,
                    opponentSize,
                    opponentMovement);
                TrackRuntimeData(enemyData);

                int xOffset = i - 1;
                Vector2Int startTile = new Vector2Int(enemyStartTile.x + xOffset, enemyStartTile.y);
                Vector2Int endTile = new Vector2Int(goalTile.x + xOffset, goalTile.y);
                EnemyRuntimeState enemy = SpawnEnemy(enemyData, startTile, endTile);

                if (enemy != null)
                {
                    spawnedEnemies.Add(enemy);
                }
            }

            activeEnemy = spawnedEnemies.Count > 1 ? spawnedEnemies[1] : (spawnedEnemies.Count > 0 ? spawnedEnemies[0] : null);
        }

        private void SpawnEnemyPassiveScenario(UnitDataSO unitSource, EnemyDataSO enemySource, PassiveDataSO passive, EnemySize compatibleSize)
        {
            EnemyDataSO enemyData = Phase2PrototypeDataFactory.CloneEnemyData(
                enemySource,
                passive,
                enemySummonPrefab,
                summonIntervalOverrideSeconds,
                compatibleSize,
                null);
            TrackRuntimeData(enemyData);

            activeEnemy = SpawnEnemy(enemyData, enemyStartTile, goalTile);

            if (passive is CommandSO || passive is DefenseAuraSO || passive is CleanseSO)
            {
                EnemyDataSO allyEnemyData = Phase2PrototypeDataFactory.CloneEnemyData(enemySource, null, null, 0f, compatibleSize, null);
                TrackRuntimeData(allyEnemyData);
                EnemyRuntimeState allyEnemy = SpawnEnemy(allyEnemyData, new Vector2Int(enemyStartTile.x + 1, enemyStartTile.y), new Vector2Int(goalTile.x + 1, goalTile.y));

                if (allyEnemy != null)
                {
                    spawnedEnemies.Add(allyEnemy);
                }
            }

            for (int i = 0; i < OpponentCount; i++)
            {
                UnitDataSO unitData = Phase2PrototypeDataFactory.CloneUnitData(unitSource);
                TrackRuntimeData(unitData);

                Vector2Int tile = new Vector2Int(ownerUnitTile.x + i - 1, ownerUnitTile.y);
                UnitRuntimeState unit = SpawnUnit(unitData, tile);

                if (unit != null)
                {
                    spawnedUnits.Add(unit);
                }
            }

            activeUnit = spawnedUnits.Count > 1 ? spawnedUnits[1] : (spawnedUnits.Count > 0 ? spawnedUnits[0] : null);
        }

        private int ReleaseOwnedUnitSummons()
        {
            if (activeUnit == null)
            {
                return 0;
            }

            List<GameObject> targets = new List<GameObject>();

            foreach (UnitRuntimeState unit in CombatRegistry.Units)
            {
                if (unit != null && unit.IsSummon && unit.SummonRuntime != null && unit.SummonRuntime.Owner == activeUnit)
                {
                    targets.Add(unit.gameObject);
                }
            }

            for (int i = 0; i < targets.Count; i++)
            {
                SummonService.Release(targets[i]);
            }

            return targets.Count;
        }

        private static EnemySize ResolveRequiredOpponentSize(PassiveDataSO passive, EnemySize fallback)
        {
            if (passive is SizeDamagePassiveSO sizeDamage && sizeDamage.TargetSize != EnemySize.None)
            {
                return sizeDamage.TargetSize;
            }

            if (passive is SizeAttackSO sizeAttack && sizeAttack.TargetSize != EnemySize.None)
            {
                return sizeAttack.TargetSize;
            }

            if (passive is AttackSpeedSO attackSpeed && attackSpeed.TargetSize != EnemySize.None)
            {
                return attackSpeed.TargetSize;
            }

            if (passive is SnipeSO snipe && snipe.TargetSize != EnemySize.None)
            {
                return snipe.TargetSize;
            }

            if (passive is DefenseBuffSO defenseBuff)
            {
                switch (defenseBuff.Trigger)
                {
                    case DefenseBuffTrigger.BlockingSmall:
                        return EnemySize.Small;
                    case DefenseBuffTrigger.BlockingMedium:
                        return EnemySize.Medium;
                    case DefenseBuffTrigger.BlockingLarge:
                        return EnemySize.Large;
                }
            }

            return fallback;
        }

        private UnitRuntimeState SpawnUnit(UnitDataSO data, Vector2Int tile)
        {
            Vector3 position = ToWorld(tile, unitHeight);
            UnitRuntimeState state = Phase2PrototypeSpawnUtility.SpawnUnit(unitPrefab, data, transform, position);

            if (state == null)
            {
                return null;
            }

            state.GridPosition.Initialize(tile, GridFacingDirection.North, CombatTargetLayer.Ground);
            spawnedObjects.Add(state.gameObject);
            return state;
        }

        private EnemyRuntimeState SpawnEnemy(EnemyDataSO data, Vector2Int startTile, Vector2Int endTile)
        {
            Vector3 position = ToWorld(startTile, enemyHeight);
            EnemyRuntimeState state = Phase2PrototypeSpawnUtility.SpawnEnemy(enemyPrefab, data, transform, position);

            if (state == null || state.Move == null)
            {
                return null;
            }

            PathNode[] path = BuildStraightPath(startTile, endTile, enemyHeight);

            if (!state.Move.SetPath(path))
            {
                Destroy(state.gameObject);
                return null;
            }

            spawnedObjects.Add(state.gameObject);
            return state;
        }

        private PathNode[] BuildStraightPath(Vector2Int start, Vector2Int goal, float height)
        {
            int xCount = Mathf.Abs(goal.x - start.x);
            int yCount = Mathf.Abs(goal.y - start.y);
            PathNode[] path = new PathNode[xCount + yCount + 1];
            Vector2Int current = start;
            int index = 0;

            path[index++] = new PathNode(ToWorld(current, height), current, GetFacing(current, goal));

            while (current.x != goal.x)
            {
                int step = goal.x > current.x ? 1 : -1;
                current = new Vector2Int(current.x + step, current.y);
                path[index++] = new PathNode(ToWorld(current, height), current, step > 0 ? GridFacingDirection.East : GridFacingDirection.West);
            }

            while (current.y != goal.y)
            {
                int step = goal.y > current.y ? 1 : -1;
                current = new Vector2Int(current.x, current.y + step);
                path[index++] = new PathNode(ToWorld(current, height), current, step > 0 ? GridFacingDirection.North : GridFacingDirection.South);
            }

            return path;
        }

        private GridFacingDirection GetFacing(Vector2Int from, Vector2Int to)
        {
            if (to.x > from.x) return GridFacingDirection.East;
            if (to.x < from.x) return GridFacingDirection.West;
            return to.y >= from.y ? GridFacingDirection.North : GridFacingDirection.South;
        }

        private Vector3 ToWorld(Vector2Int tile, float height)
        {
            return new Vector3(
                worldOrigin.x + tile.x * tileWorldSize,
                worldOrigin.y + height,
                worldOrigin.z + tile.y * tileWorldSize);
        }

        private bool TryGetSourceData(out UnitDataSO unitSource, out EnemyDataSO enemySource)
        {
            unitSource = null;
            enemySource = null;

            if (unitPrefab == null || enemyPrefab == null)
            {
                Fail("Unit Prefab과 Enemy Prefab을 모두 연결해야 합니다.");
                return false;
            }

            UnitDataLink unitLink = unitPrefab.GetComponent<UnitDataLink>();
            EnemyDataLink enemyLink = enemyPrefab.GetComponent<EnemyDataLink>();

            if (unitLink == null || !unitLink.HasData || enemyLink == null || !enemyLink.HasData)
            {
                Fail("Prefab의 UnitDataLink/EnemyDataLink에 정식 데이터가 연결되어 있지 않습니다.");
                return false;
            }

            unitSource = unitLink.UnitData;
            enemySource = enemyLink.EnemyData;
            return true;
        }

        private void RefreshPassiveCounters()
        {
            if (selectedPassiveUsesUnit && activeUnit != null && activeUnit.Passives != null)
            {
                activeAssignedPassiveCount = activeUnit.Passives.AssignedPassiveCount;
                activeAppliedPassiveCount = activeUnit.Passives.AppliedPassiveCount;
                activeRejectedPassiveCount = activeUnit.Passives.RejectedPassiveCount;
                activeUnsupportedPassiveCount = activeUnit.Passives.UnsupportedPassiveCount;
            }
            else if (!selectedPassiveUsesUnit && activeEnemy != null && activeEnemy.Passives != null)
            {
                activeAssignedPassiveCount = activeEnemy.Passives.AssignedPassiveCount;
                activeAppliedPassiveCount = activeEnemy.Passives.AppliedPassiveCount;
                activeRejectedPassiveCount = activeEnemy.Passives.RejectedPassiveCount;
                activeUnsupportedPassiveCount = activeEnemy.Passives.UnsupportedPassiveCount;
            }
        }

        private void TrackRuntimeData(ScriptableObject data)
        {
            if (data != null)
            {
                runtimeDataObjects.Add(data);
            }
        }

        private void DamageToRemainingPercent(UnitRuntimeState unit, float remainingPercent)
        {
            if (unit == null || unit.Health == null || unit.Health.IsDead)
            {
                return;
            }

            float targetHp = unit.Health.MaxHp * Mathf.Clamp01(remainingPercent * 0.01f);
            unit.ApplyDamage(Mathf.Max(0f, unit.Health.CurrentHp - targetHp));
        }

        private void DamageToRemainingPercent(EnemyRuntimeState enemy, float remainingPercent)
        {
            if (enemy == null || enemy.Health == null || enemy.Health.IsDead)
            {
                return;
            }

            float targetHp = enemy.Health.MaxHp * Mathf.Clamp01(remainingPercent * 0.01f);
            enemy.ApplyDamage(Mathf.Max(0f, enemy.Health.CurrentHp - targetHp));
        }

        private static void Kill(UnitRuntimeState unit)
        {
            if (unit != null && unit.Health != null && !unit.Health.IsDead)
            {
                unit.ApplyDamage(unit.Health.CurrentHp);
            }
        }

        private static void Kill(EnemyRuntimeState enemy)
        {
            if (enemy != null && enemy.Health != null && !enemy.Health.IsDead)
            {
                enemy.ApplyDamage(enemy.Health.CurrentHp);
            }
        }

        private void Fail(string message)
        {
            lastMessage = message;
            Debug.LogError(message, this);
        }

        private static void LogResult(bool passed, string message)
        {
            if (passed)
            {
                Debug.Log(message);
            }
            else
            {
                Debug.LogWarning(message);
            }
        }
    }
}
