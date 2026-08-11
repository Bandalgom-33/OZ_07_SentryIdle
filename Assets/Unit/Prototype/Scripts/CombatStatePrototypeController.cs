using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Runtime;
using UnityEngine;

namespace EndlessGuard.Unit.Prototype
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CombatLoop))]
    public sealed class CombatStatePrototypeController : MonoBehaviour
    {
        [Header("검증 대상 프리팹")]
        [Tooltip("런타임 체력과 SP를 검증할 공식 캐릭터 프리팹입니다.")]
        [SerializeField] private GameObject unitPrefab;

        [Tooltip("런타임 체력과 사망 이벤트를 검증할 공식 몬스터 프리팹입니다.")]
        [SerializeField] private GameObject enemyPrefab;

        [Header("격자 생성 설정")]
        [Tooltip("격자 좌표 (0, 0)이 배치될 기준 월드 위치입니다.")]
        [SerializeField] private Vector3 gridWorldOrigin;

        [Tooltip("격자 한 칸의 월드 크기입니다. 현재 프로토타입은 1을 사용합니다.")]
        [Min(0.01f)]
        [SerializeField] private float tileWorldSize = 1f;

        [Tooltip("캐릭터가 생성될 격자 타일 좌표입니다.")]
        [SerializeField] private Vector2Int unitTileCoordinate = Vector2Int.zero;

        [Tooltip("몬스터가 생성될 격자 타일 좌표입니다.")]
        [SerializeField] private Vector2Int enemyTileCoordinate = new Vector2Int(1, 0);

        [Tooltip("캐릭터의 월드 Y 위치입니다. 언덕 배치 검증이 필요하면 이 값만 변경합니다.")]
        [SerializeField] private float unitSpawnHeight;

        [Tooltip("몬스터의 월드 Y 위치입니다.")]
        [SerializeField] private float enemySpawnHeight;

        [Tooltip("캐릭터가 생성될 때 바라보는 격자 방향입니다.")]
        [SerializeField] private GridFacingDirection unitFacingDirection = GridFacingDirection.East;

        [Tooltip("몬스터가 생성될 때 바라보는 격자 방향입니다.")]
        [SerializeField] private GridFacingDirection enemyFacingDirection = GridFacingDirection.West;

        [Tooltip("Play 시작 시 캐릭터와 몬스터를 자동으로 생성합니다.")]
        [SerializeField] private bool autoSpawnOnStart = true;

        [Header("검증 수치")]
        [Tooltip("캐릭터 피해 버튼을 한 번 누를 때 적용할 피해입니다.")]
        [Min(0f)]
        [SerializeField] private float unitDamageAmount = 1000f;

        [Tooltip("캐릭터 회복 버튼을 한 번 누를 때 적용할 회복량입니다.")]
        [Min(0f)]
        [SerializeField] private float unitHealAmount = 500f;

        [Tooltip("몬스터 피해 버튼을 한 번 누를 때 적용할 피해입니다.")]
        [Min(0f)]
        [SerializeField] private float enemyDamageAmount = 1000f;

        [Tooltip("몬스터 회복 버튼을 한 번 누를 때 적용할 회복량입니다.")]
        [Min(0f)]
        [SerializeField] private float enemyHealAmount = 500f;

        [Tooltip("캐릭터 SP 증가·소모 버튼에서 사용할 수치입니다.")]
        [Min(0f)]
        [SerializeField] private float skillGaugeAmount = 10f;

        [Header("공격 진행도 검증")]
        [Tooltip("공격 진행도 증가 버튼을 한 번 누를 때 흐른 것으로 계산할 시간입니다.")]
        [Min(0f)]
        [SerializeField] private float attackProgressStepSeconds = 0.3f;

        [Tooltip("준비된 공격을 한 번에 소비할 수 있는 최대 횟수입니다.")]
        [Min(1)]
        [SerializeField] private int maxAttackConsumeCount = 10;

        [HideInInspector]
        [SerializeField] private GameObject spawnedUnitObject;

        [HideInInspector]
        [SerializeField] private GameObject spawnedEnemyObject;

        [HideInInspector]
        [SerializeField] private UnitRuntimeState spawnedUnit;

        [HideInInspector]
        [SerializeField] private EnemyRuntimeState spawnedEnemy;

        [HideInInspector]
        [SerializeField] private int unitHealthChangedCount;

        [HideInInspector]
        [SerializeField] private int enemyHealthChangedCount;

        [HideInInspector]
        [SerializeField] private int unitSkillGaugeChangedCount;

        [HideInInspector]
        [SerializeField] private int unitDeathEventCount;

        [HideInInspector]
        [SerializeField] private int enemyDeathEventCount;

        [HideInInspector]
        [SerializeField] private int unitConsumedAttackCount;

        [HideInInspector]
        [SerializeField] private int enemyConsumedAttackCount;

        [HideInInspector]
        [TextArea(2, 4)]
        [SerializeField] private string lastEventMessage;

        private CombatLoop combatLoop;

        public UnitRuntimeState SpawnedUnit => spawnedUnit;
        public EnemyRuntimeState SpawnedEnemy => spawnedEnemy;
        public int UnitHealthChangedCount => unitHealthChangedCount;
        public int EnemyHealthChangedCount => enemyHealthChangedCount;
        public int UnitSkillGaugeChangedCount => unitSkillGaugeChangedCount;
        public int UnitDeathEventCount => unitDeathEventCount;
        public int EnemyDeathEventCount => enemyDeathEventCount;
        public int UnitConsumedAttackCount => unitConsumedAttackCount;
        public int EnemyConsumedAttackCount => enemyConsumedAttackCount;
        public string LastEventMessage => lastEventMessage;

        private void Awake()
        {
            combatLoop = GetComponent<CombatLoop>();
        }

        private void OnEnable()
        {
            CombatEvents.OnUnitDied += HandleUnitDied;
            CombatEvents.OnEnemyDied += HandleEnemyDied;
        }

        private void Start()
        {
            combatLoop.StartLoop();

            if (autoSpawnOnStart)
            {
                SpawnActors();
            }
        }

        private void OnDisable()
        {
            CombatEvents.OnUnitDied -= HandleUnitDied;
            CombatEvents.OnEnemyDied -= HandleEnemyDied;
            UnsubscribeInstanceEvents();
        }

        public void SpawnActors()
        {
            if (unitPrefab == null || enemyPrefab == null)
            {
                lastEventMessage = "캐릭터 또는 몬스터 프리팹이 연결되지 않았습니다.";
                Debug.LogError(lastEventMessage, this);
                return;
            }

            DespawnActors();
            ResetEventCounts();

            Vector3 unitSpawnPosition = GetWorldPosition(unitTileCoordinate, unitSpawnHeight);
            Vector3 enemySpawnPosition = GetWorldPosition(enemyTileCoordinate, enemySpawnHeight);

            spawnedUnitObject = Instantiate(unitPrefab, unitSpawnPosition, Quaternion.identity, transform);
            spawnedEnemyObject = Instantiate(enemyPrefab, enemySpawnPosition, Quaternion.identity, transform);
            spawnedUnit = spawnedUnitObject.GetComponent<UnitRuntimeState>();
            spawnedEnemy = spawnedEnemyObject.GetComponent<EnemyRuntimeState>();

            if (spawnedUnit == null || spawnedEnemy == null)
            {
                lastEventMessage = "생성된 프리팹에서 UnitRuntimeState 또는 EnemyRuntimeState를 찾지 못했습니다.";
                Debug.LogError(lastEventMessage, this);
                DespawnActors();
                return;
            }

            if (!InitializeGridPositions())
            {
                DespawnActors();
                return;
            }

            SubscribeInstanceEvents();

            float worldDistance = Vector3.Distance(unitSpawnPosition, enemySpawnPosition);
            lastEventMessage = $"검증 대상 생성 완료: {spawnedUnit.UnitId} 타일 {unitTileCoordinate}, {spawnedEnemy.EnemyId} 타일 {enemyTileCoordinate}, 월드 거리 {worldDistance:0.###}";
            Debug.Log(lastEventMessage, this);
        }

        public void DespawnActors()
        {
            UnsubscribeInstanceEvents();

            if (spawnedUnitObject != null)
            {
                Destroy(spawnedUnitObject);
            }

            if (spawnedEnemyObject != null)
            {
                Destroy(spawnedEnemyObject);
            }

            spawnedUnitObject = null;
            spawnedEnemyObject = null;
            spawnedUnit = null;
            spawnedEnemy = null;
        }

        public void DamageUnit()
        {
            if (!CanUseUnit())
            {
                return;
            }

            spawnedUnit.ApplyDamage(unitDamageAmount);
        }

        public void HealUnit()
        {
            if (!CanUseUnit())
            {
                return;
            }

            spawnedUnit.Heal(unitHealAmount);
        }

        public void KillUnit()
        {
            if (!CanUseUnit())
            {
                return;
            }

            spawnedUnit.ApplyDamage(spawnedUnit.Health.CurrentHp);
        }

        public void AddUnitSkillGauge()
        {
            if (!CanUseUnit())
            {
                return;
            }

            spawnedUnit.AddSkillGauge(skillGaugeAmount);
        }

        public void ConsumeUnitSkillGauge()
        {
            if (!CanUseUnit())
            {
                return;
            }

            bool consumed = spawnedUnit.TryConsumeSkillGauge(skillGaugeAmount);

            if (!consumed)
            {
                lastEventMessage = $"캐릭터 SP가 부족해 {skillGaugeAmount:0.##}을 소모하지 못했습니다.";
                Debug.Log(lastEventMessage, this);
            }
        }

        public void AdvanceUnitAttackProgress()
        {
            if (!CanUseUnit())
            {
                return;
            }

            float attacksPerSecond = spawnedUnit.Stats.AttacksPerSecond;
            spawnedUnit.AdvanceAttackProgress(attacksPerSecond, attackProgressStepSeconds);

            lastEventMessage = $"캐릭터 공격 진행: {attackProgressStepSeconds:0.###}초, 빈도 {attacksPerSecond:0.###}회/초, 진행도 {spawnedUnit.AttackProgress:0.###}, 준비 공격 {spawnedUnit.ReadyAttackCount}회";
            Debug.Log(lastEventMessage, spawnedUnit);
        }

        public void ConsumeUnitReadyAttacks()
        {
            if (!CanUseUnit())
            {
                return;
            }

            int consumedCount = spawnedUnit.ConsumeReadyAttacks(maxAttackConsumeCount);
            unitConsumedAttackCount += consumedCount;

            lastEventMessage = $"캐릭터 준비 공격 {consumedCount}회 소비, 남은 진행도 {spawnedUnit.AttackProgress:0.###}, 누적 소비 {unitConsumedAttackCount}회";
            Debug.Log(lastEventMessage, spawnedUnit);
        }

        public void DamageEnemy()
        {
            if (!CanUseEnemy())
            {
                return;
            }

            spawnedEnemy.ApplyDamage(enemyDamageAmount);
        }

        public void HealEnemy()
        {
            if (!CanUseEnemy())
            {
                return;
            }

            spawnedEnemy.Heal(enemyHealAmount);
        }

        public void KillEnemy()
        {
            if (!CanUseEnemy())
            {
                return;
            }

            spawnedEnemy.ApplyDamage(spawnedEnemy.Health.CurrentHp);
        }

        public void AdvanceEnemyAttackProgress()
        {
            if (!CanUseEnemy())
            {
                return;
            }

            float attacksPerSecond = spawnedEnemy.Stats.AttacksPerSecond;
            spawnedEnemy.AdvanceAttackProgress(attacksPerSecond, attackProgressStepSeconds);

            lastEventMessage = $"몬스터 공격 진행: {attackProgressStepSeconds:0.###}초, 빈도 {attacksPerSecond:0.###}회/초, 진행도 {spawnedEnemy.AttackProgress:0.###}, 준비 공격 {spawnedEnemy.ReadyAttackCount}회";
            Debug.Log(lastEventMessage, spawnedEnemy);
        }

        public void ConsumeEnemyReadyAttacks()
        {
            if (!CanUseEnemy())
            {
                return;
            }

            int consumedCount = spawnedEnemy.ConsumeReadyAttacks(maxAttackConsumeCount);
            enemyConsumedAttackCount += consumedCount;

            lastEventMessage = $"몬스터 준비 공격 {consumedCount}회 소비, 남은 진행도 {spawnedEnemy.AttackProgress:0.###}, 누적 소비 {enemyConsumedAttackCount}회";
            Debug.Log(lastEventMessage, spawnedEnemy);
        }

        private bool InitializeGridPositions()
        {
            if (spawnedUnit.GridPosition == null || spawnedEnemy.GridPosition == null)
            {
                lastEventMessage = "생성된 캐릭터 또는 몬스터에서 CombatGridPosition을 찾지 못했습니다.";
                Debug.LogError(lastEventMessage, this);
                return false;
            }

            CombatTargetLayer enemyTargetLayer = spawnedEnemy.DataLink.EnemyData.MovementType == EnemyMovementType.Air ? CombatTargetLayer.Air : CombatTargetLayer.Ground;

            spawnedUnit.GridPosition.Initialize(unitTileCoordinate, unitFacingDirection, CombatTargetLayer.Ground);
            spawnedEnemy.GridPosition.Initialize(enemyTileCoordinate, enemyFacingDirection, enemyTargetLayer);

            return true;
        }

        private Vector3 GetWorldPosition(Vector2Int tileCoordinate, float worldHeight)
        {
            float worldX = gridWorldOrigin.x + tileCoordinate.x * tileWorldSize;
            float worldY = gridWorldOrigin.y + worldHeight;
            float worldZ = gridWorldOrigin.z + tileCoordinate.y * tileWorldSize;

            return new Vector3(worldX, worldY, worldZ);
        }

        private bool CanUseUnit()
        {
            return spawnedUnit != null && spawnedUnit.IsInitialized && spawnedUnit.Health != null && !spawnedUnit.Health.IsDead;
        }

        private bool CanUseEnemy()
        {
            return spawnedEnemy != null && spawnedEnemy.IsInitialized && spawnedEnemy.Health != null && !spawnedEnemy.Health.IsDead;
        }

        private void SubscribeInstanceEvents()
        {
            spawnedUnit.Health.OnHealthChanged += HandleUnitHealthChanged;
            spawnedUnit.OnSkillGaugeChanged += HandleUnitSkillGaugeChanged;
            spawnedEnemy.Health.OnHealthChanged += HandleEnemyHealthChanged;
        }

        private void UnsubscribeInstanceEvents()
        {
            if (spawnedUnit != null && spawnedUnit.Health != null)
            {
                spawnedUnit.Health.OnHealthChanged -= HandleUnitHealthChanged;
                spawnedUnit.OnSkillGaugeChanged -= HandleUnitSkillGaugeChanged;
            }

            if (spawnedEnemy != null && spawnedEnemy.Health != null)
            {
                spawnedEnemy.Health.OnHealthChanged -= HandleEnemyHealthChanged;
            }
        }

        private void ResetEventCounts()
        {
            unitHealthChangedCount = 0;
            enemyHealthChangedCount = 0;
            unitSkillGaugeChangedCount = 0;
            unitDeathEventCount = 0;
            enemyDeathEventCount = 0;
            unitConsumedAttackCount = 0;
            enemyConsumedAttackCount = 0;
            lastEventMessage = string.Empty;
        }

        private void HandleUnitHealthChanged(CombatHealth health)
        {
            unitHealthChangedCount++;
            lastEventMessage = $"캐릭터 HP 변경: {health.CurrentHp:0.##} / {health.MaxHp:0.##}";
            Debug.Log(lastEventMessage, health);
        }

        private void HandleEnemyHealthChanged(CombatHealth health)
        {
            enemyHealthChangedCount++;
            lastEventMessage = $"몬스터 HP 변경: {health.CurrentHp:0.##} / {health.MaxHp:0.##}";
            Debug.Log(lastEventMessage, health);
        }

        private void HandleUnitSkillGaugeChanged(UnitRuntimeState unit)
        {
            unitSkillGaugeChangedCount++;
            lastEventMessage = $"캐릭터 SP 변경: {unit.CurrentSkillGauge:0.##} / {unit.MaxSkillGauge:0.##}";
            Debug.Log(lastEventMessage, unit);
        }

        private void HandleUnitDied(UnitDiedInfo info)
        {
            if (spawnedUnit == null || info.RuntimeId != spawnedUnit.RuntimeId)
            {
                return;
            }

            unitDeathEventCount++;
            lastEventMessage = $"{info.UnitId} OnUnitDied 발생 / Runtime {info.RuntimeId}";
            Debug.Log(lastEventMessage, spawnedUnit);
        }

        private void HandleEnemyDied(EnemyDiedInfo info)
        {
            if (spawnedEnemy == null || info.RuntimeId != spawnedEnemy.RuntimeId)
            {
                return;
            }

            enemyDeathEventCount++;
            lastEventMessage = $"{info.EnemyId} OnEnemyDied 발생 / Runtime {info.RuntimeId}";
            Debug.Log(lastEventMessage, spawnedEnemy);
        }
    }
}