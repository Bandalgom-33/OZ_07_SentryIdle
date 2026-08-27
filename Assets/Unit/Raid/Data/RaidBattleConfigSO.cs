using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Data
{
    [CreateAssetMenu(fileName = "RaidBattleConfig", menuName = "EndlessGuard/Raid/Battle Config")]
    public sealed class RaidBattleConfigSO : ScriptableObject
    {
        [Header("보스")]
        [SerializeField] private string bossDisplayName = "RAID BOSS";
        [Min(1f)] [SerializeField] private float bossMaxHp = 10000000f;

        [Header("제한시간")]
        [Min(1f)] [SerializeField] private float timeLimitSeconds = 300f;

        [Header("보스 Phase HP 비율")]
        [Range(0.01f, 0.99f)] [SerializeField] private float phase2HpRatio = 0.67f;
        [Range(0.01f, 0.99f)] [SerializeField] private float phase3HpRatio = 0.34f;

        [Header("보스 스킬 게이지")]
        [Min(1f)] [SerializeField] private float bossSkillGaugeMax = 100f;
        [Min(0f)] [SerializeField] private float goalSkillGaugeGain = 10f;

        [Header("보스 대표 스킬")]
        [Tooltip("보스 스킬 게이지가 가득 찬 뒤 실제 피해가 적용되기 전 예고 시간입니다.")]
        [Min(0f)] [SerializeField] private float bossSkillCastDelay = 1.25f;
        [Tooltip("보스 스킬이 캐릭터를 순차 공격할 때 각 타격 사이의 간격입니다.")]
        [Min(0f)] [SerializeField] private float bossSkillStrikeInterval = 0.08f;
        [Tooltip("캐릭터 타격 번개가 하늘에서 내려오기 시작한 뒤 실제 피해가 적용되기까지의 시간입니다.")]
        [Min(0f)] [SerializeField] private float bossSkillStrikeTelegraphDuration = 0.16f;
        [Tooltip("현재 필드 캐릭터의 최대 HP에 곱해지는 보스 스킬 피해 비율입니다. 0.35 = 최대 HP의 35% 피해.")]
        [Range(0.01f, 1f)] [SerializeField] private float bossSkillMaxHpDamageRatio = 0.35f;
        [Tooltip("최대 HP 비율 계산 결과가 너무 작을 때 보장할 최소 피해입니다.")]
        [Min(0f)] [SerializeField] private float bossSkillMinimumDamage = 1f;

        [Header("레이드 공격 게이지")]
        [Min(1f)] [SerializeField] private float raidAttackGaugeMax = 1000f;
        [Min(0f)] [SerializeField] private float enemyKillRaidGaugeGain = 25f;

        [Header("레이드 보스 전투 방어")]
        [Tooltip("Raid Attack에서 물리 공격 캐릭터가 사용하는 보스 물리 방어력입니다.")]
        [Min(0f)] [SerializeField] private float bossPhysicalDefense = 300f;
        [Tooltip("Raid Attack에서 마법 공격 캐릭터가 사용하는 보스 마법 방어력입니다.")]
        [Min(0f)] [SerializeField] private float bossMagicalDefense = 300f;

        [Header("레이드 공격 발동")]
        [Tooltip("게이지가 100%일 때 Raid Attack이 0%까지 모두 소모되는 시간입니다. Manual은 누르고 있는 동안만 이 속도로 소비합니다.")]
        [Min(1f)] [SerializeField] private float raidAttackFullGaugeDuration = 10f;
        [Tooltip("캐릭터의 실제 기본 공격 피해에 곱하는 레이드 보스 전용 배율입니다. 캐릭터 간 공격력/공속/치명타 차이는 그대로 유지됩니다.")]
        [Min(0.01f)] [SerializeField] private float raidAttackDamageMultiplier = 25f;

        [Header("레이드 전투 숫자")]
        [Tooltip("레이드에서 캐릭터/몬스터/회복 전투 숫자에 적용할 크기 배율입니다. 일반전투에는 적용되지 않습니다.")]
        [Range(0.25f, 1f)] [SerializeField] private float raidCombatNumberScale = 0.72f;
        [Tooltip("레이드에서 치명타가 발생했을 때 표시할 숫자 크기 배율입니다. 일반 레이드 전투 숫자보다 크고 보스 숫자보다 작게 사용합니다.")]
        [Range(0.25f, 1.25f)] [SerializeField] private float raidCriticalNumberScale = 0.95f;
        [Tooltip("레이드 보스가 피해를 받을 때 표시되는 숫자 크기 배율입니다.")]
        [Range(1f, 3f)] [SerializeField] private float raidBossDamageNumberScale = 1.4f;

        [Header("배치 코스트")]
        [Min(0)] [SerializeField] private int startingCost = 0;
        [Min(1)] [SerializeField] private int costMax = 100;
        [Min(0f)] [SerializeField] private float costRegenPerSecond = 1f;

        [Header("개발용 출전 캐릭터")]
        [Tooltip("외부 팀 선택 시스템이 연결되기 전 레이드 개발 검증에만 사용하는 16명입니다. Team1 0~7, Team2 8~15 순서입니다. 외부 로스터가 전달되면 이 목록은 사용하지 않습니다.")]
        [SerializeField] private UnitDataSO[] developmentRoster = new UnitDataSO[16];

        [Header("레이드 캐릭터 배치")]
        [Tooltip("Team1/Team2 합산 필드 최대 배치 인원입니다.")]
        [Range(1, 16)] [SerializeField] private int maxDeployedUnits = 16;
        [Tooltip("Auto 모드가 전황을 다시 평가하는 기본 간격입니다.")]
        [Min(0.1f)] [SerializeField] private float autoDeployDecisionInterval = 0.65f;
        [Tooltip("자동배치 후보가 이 점수보다 낮으면 코스트가 있어도 기다립니다.")]
        [Min(0f)] [SerializeField] private float autoDeployMinimumScore = 2.5f;
        [Tooltip("초반 코스트 획득형 캐릭터가 사용할 수 있는 조금 더 전진한 방어선입니다.")]
        [Range(0.2f, 0.8f)] [SerializeField] private float autoDeployVanguardLineProgress = 0.52f;
        [Tooltip("이 진행도보다 Entry 쪽인 타일은 자동배치에서 강하게 감점합니다.")]
        [Range(0.05f, 0.6f)] [SerializeField] private float autoDeployFrontPenaltyProgress = 0.34f;
        [Tooltip("지상 타일에 배치되는 캐릭터 루트의 바닥 높이 보정입니다.")]
        [SerializeField] private float groundDeployHeight = 0.08f;
        [Tooltip("HighGround 타일에 배치되는 캐릭터 루트의 높이 보정입니다.")]
        [SerializeField] private float highGroundDeployHeight = 0.82f;
        [Tooltip("수동 Facing 드래그가 방향 입력으로 인정되는 최소 월드 거리입니다.")]
        [Min(0.05f)] [SerializeField] private float manualFacingDragDistance = 0.45f;

        [Header("레이드 아이템")]
        [Tooltip("Phase1/2 랜덤 드롭 아이템의 확률, 효과와 Visual Prefab을 보관하는 데이터입니다.")]
        [SerializeField] private RaidItemConfigSO itemConfig;

        [Header("레이드 몬스터 소환")]
        [Tooltip("정식 EnemyDataSO를 ID로 찾기 위한 공용 몬스터 카탈로그입니다.")]
        [SerializeField] private EnemyCatalog enemyCatalog;
        [SerializeField] private bool enableAutomaticSpawn = true;
        [Tooltip("지정하면 자동 Spawn은 고정 초 간격 대신 Beat 패턴을 사용합니다. AudioClip과 직접 연결되지 않으며 순수 웨이브 리듬 데이터입니다.")]
        [SerializeField] private RaidSpawnRhythmSO spawnRhythm;
        [Tooltip("Beat 패턴이 지정되지 않았을 때 사용하는 기존 시간 간격 방식의 시작 지연입니다.")]
        [Min(0f)] [SerializeField] private float spawnStartDelay = 8f;
        [Tooltip("Phase1에서 동시에 필드에 존재할 수 있는 몬스터 상한입니다. 몬스터 소환물도 CombatRegistry 기준에 포함될 수 있습니다.")]
        [Min(1)] [SerializeField] private int phase1MaxActiveEnemies = 16;
        [Tooltip("Phase2에서 동시에 필드에 존재할 수 있는 몬스터 상한입니다.")]
        [Min(1)] [SerializeField] private int phase2MaxActiveEnemies = 24;
        [Tooltip("Phase3에서 동시에 필드에 존재할 수 있는 몬스터 상한입니다.")]
        [Min(1)] [SerializeField] private int phase3MaxActiveEnemies = 30;

        [Header("Phase 1 Spawn")]
        [Tooltip("레이드 시작 후 이 시간 동안은 P1 초반 Ramp를 사용합니다.")]
        [Min(0f)] [SerializeField] private float phase1OpeningDuration = 30f;
        [Tooltip("P1 초반 Ramp에서 한 마리씩 등장하는 간격입니다.")]
        [Min(0.5f)] [SerializeField] private float phase1OpeningSpawnInterval = 6f;
        [Tooltip("P1 초반 Ramp 한 번에 소환할 수입니다. 기본 1마리입니다.")]
        [Range(1, 3)] [SerializeField] private int phase1OpeningSpawnPerPulse = 1;
        [Tooltip("초반 Ramp 종료 뒤 한 Burst가 끝난 후 다음 Burst까지 기다리는 시간입니다.")]
        [Min(0.2f)] [SerializeField] private float phase1SpawnInterval = 5.5f;
        [Tooltip("한 Burst에서 소환할 몬스터 수입니다.")]
        [Range(1, 8)] [SerializeField] private int phase1SpawnPerPulse = 2;
        [Tooltip("같은 Burst 안에서 몬스터 사이의 짧은 소환 간격입니다.")]
        [Min(0f)] [SerializeField] private float phase1SpawnSpacing = 0.45f;
        [SerializeField] private string[] phase1EnemyIds = { "ENEMY_0002", "ENEMY_0003", "ENEMY_0005" };

        [Header("Phase 2 Spawn")]
        [Min(0.2f)] [SerializeField] private float phase2SpawnInterval = 4.2f;
        [Range(1, 8)] [SerializeField] private int phase2SpawnPerPulse = 3;
        [Min(0f)] [SerializeField] private float phase2SpawnSpacing = 0.35f;
        [SerializeField] private string[] phase2EnemyIds = { "ENEMY_0002", "ENEMY_0003", "ENEMY_0004", "ENEMY_0005", "ENEMY_0006" };

        [Header("Phase 3 Spawn")]
        [Min(0.2f)] [SerializeField] private float phase3SpawnInterval = 3.4f;
        [Range(1, 8)] [SerializeField] private int phase3SpawnPerPulse = 3;
        [Min(0f)] [SerializeField] private float phase3SpawnSpacing = 0.28f;
        [SerializeField] private string[] phase3EnemyIds = { "ENEMY_0001", "ENEMY_0004", "ENEMY_0005", "ENEMY_0006" };

        [Header("레이드 이동속도 보정")]
        [Tooltip("이 속도보다 느린 몬스터일수록 레이드에서 더 큰 보정을 받습니다. EnemyDataSO 원본은 변경하지 않습니다.")]
        [Min(0.01f)] [SerializeField] private float slowMoveSpeedThreshold = 1.6f;
        [Tooltip("느린 몬스터가 가까워지도록 보정할 목표 속도입니다. 원래 빠른 몬스터를 이 값으로 강제하지 않습니다.")]
        [Min(0.01f)] [SerializeField] private float slowMoveSpeedTarget = 1.8f;
        [Tooltip("느린 몬스터 보정 강도입니다. 0이면 보정 없음, 1이면 설정한 곡선을 그대로 적용합니다.")]
        [Range(0f, 1f)] [SerializeField] private float slowMoveSpeedCorrection = 0.9f;
        [Min(0.01f)] [SerializeField] private float phase1MoveSpeedMultiplier = 1.05f;
        [Min(0.01f)] [SerializeField] private float phase2MoveSpeedMultiplier = 1.1f;
        [Min(0.01f)] [SerializeField] private float phase3MoveSpeedMultiplier = 1.15f;

        [Header("공중 몬스터 Raid 이동")]
        [Tooltip("공중 몬스터가 바닥 타일 중앙선에서 벗어나 떠서 이동할 높이입니다.")]
        [Min(0f)] [SerializeField] private float airFlightHeight = 1.5f;
        [Tooltip("공중 몬스터의 레이드 이동속도 추가 배율입니다.")]
        [Min(0.01f)] [SerializeField] private float airMoveSpeedMultiplier = 1.05f;

        [Tooltip("공중 몬스터가 지상 Lane을 그대로 따라가지 않도록 사용할 Air Corridor 변형 수입니다. 2~3개를 번갈아 사용합니다.")]
        [Range(1, 3)] [SerializeField] private int airCorridorVariantCount = 3;
        [Tooltip("Air Corridor가 지상 Lane 중심선에서 좌우로 벗어나는 최대 타일 거리입니다. 일부 경로는 다리/협곡을 대각선으로 가로지릅니다.")]
        [Min(0f)] [SerializeField] private float airCorridorLateralOffsetTiles = 2.4f;
        [Tooltip("Air Corridor를 따라 CombatGridPosition을 갱신할 노드 간격입니다. 너무 크면 사거리 판정이 거칠어집니다.")]
        [Min(0.5f)] [SerializeField] private float airCorridorNodeSpacingTiles = 1.25f;

        public string BossDisplayName => string.IsNullOrWhiteSpace(bossDisplayName) ? "RAID BOSS" : bossDisplayName;
        public float BossMaxHp => Mathf.Max(1f, bossMaxHp);
        public float TimeLimitSeconds => Mathf.Max(1f, timeLimitSeconds);
        public float Phase2HpRatio => Mathf.Clamp01(phase2HpRatio);
        public float Phase3HpRatio => Mathf.Clamp01(phase3HpRatio);
        public float BossSkillGaugeMax => Mathf.Max(1f, bossSkillGaugeMax);
        public float GoalSkillGaugeGain => Mathf.Max(0f, goalSkillGaugeGain);
        public float BossSkillCastDelay => Mathf.Max(0f, bossSkillCastDelay);
        public float BossSkillStrikeInterval => Mathf.Max(0f, bossSkillStrikeInterval);
        public float BossSkillStrikeTelegraphDuration => Mathf.Max(0f, bossSkillStrikeTelegraphDuration);
        public float BossSkillMaxHpDamageRatio => Mathf.Clamp01(bossSkillMaxHpDamageRatio);
        public float BossSkillMinimumDamage => Mathf.Max(0f, bossSkillMinimumDamage);
        public float RaidAttackGaugeMax => Mathf.Max(1f, raidAttackGaugeMax);
        public float EnemyKillRaidGaugeGain => Mathf.Max(0f, enemyKillRaidGaugeGain);
        public float BossPhysicalDefense => Mathf.Max(0f, bossPhysicalDefense);
        public float BossMagicalDefense => Mathf.Max(0f, bossMagicalDefense);
        public float RaidAttackFullGaugeDuration => Mathf.Max(1f, raidAttackFullGaugeDuration);
        public float RaidAttackDamageMultiplier => Mathf.Max(0.01f, raidAttackDamageMultiplier);
        public float RaidCombatNumberScale => raidCombatNumberScale > 0f ? Mathf.Clamp(raidCombatNumberScale, 0.25f, 1f) : 0.72f;
        public float RaidCriticalNumberScale => raidCriticalNumberScale > 0f ? Mathf.Clamp(raidCriticalNumberScale, 0.25f, 1.25f) : 0.95f;
        public float RaidBossDamageNumberScale => raidBossDamageNumberScale > 0f ? Mathf.Clamp(raidBossDamageNumberScale, 1f, 3f) : 1.4f;
        public int StartingCost => Mathf.Clamp(startingCost, 0, CostMax);
        public int CostMax => Mathf.Max(1, costMax);
        public float CostRegenPerSecond => Mathf.Max(0f, costRegenPerSecond);
        public IReadOnlyList<UnitDataSO> DevelopmentRoster => developmentRoster;
        public int MaxDeployedUnits => Mathf.Clamp(maxDeployedUnits, 1, 16);
        public float AutoDeployDecisionInterval => Mathf.Max(0.1f, autoDeployDecisionInterval);
        public float AutoDeployMinimumScore => Mathf.Max(0f, autoDeployMinimumScore);
        public float AutoDeployVanguardLineProgress => Mathf.Clamp(autoDeployVanguardLineProgress, 0.2f, 0.8f);
        public float AutoDeployFrontPenaltyProgress => Mathf.Clamp(autoDeployFrontPenaltyProgress, 0.05f, 0.6f);
        public float GroundDeployHeight => groundDeployHeight;
        public float HighGroundDeployHeight => highGroundDeployHeight;
        public float ManualFacingDragDistance => Mathf.Max(0.05f, manualFacingDragDistance);
        public RaidItemConfigSO ItemConfig => itemConfig;
        public EnemyCatalog EnemyCatalog => enemyCatalog;
        public bool EnableAutomaticSpawn => enableAutomaticSpawn;
        public RaidSpawnRhythmSO SpawnRhythm => spawnRhythm;
        public float SpawnStartDelay => Mathf.Max(0f, spawnStartDelay);
        public float Phase1OpeningDuration => Mathf.Max(0f, phase1OpeningDuration);
        public float Phase1OpeningSpawnInterval => Mathf.Max(0.5f, phase1OpeningSpawnInterval);
        public int Phase1OpeningSpawnPerPulse => Mathf.Clamp(phase1OpeningSpawnPerPulse, 1, 3);
        public int GetMaxActiveEnemies(RaidPhase phase)
        {
            switch (phase)
            {
                case RaidPhase.Phase1:
                    return Mathf.Max(1, phase1MaxActiveEnemies);
                case RaidPhase.Phase2:
                    return Mathf.Max(1, phase2MaxActiveEnemies);
                case RaidPhase.Phase3:
                    return Mathf.Max(1, phase3MaxActiveEnemies);
                default:
                    return Mathf.Max(1, phase1MaxActiveEnemies);
            }
        }
        public float AirFlightHeight => Mathf.Max(0f, airFlightHeight);
        public int AirCorridorVariantCount => Mathf.Clamp(airCorridorVariantCount, 1, 3);
        public float AirCorridorLateralOffsetTiles => Mathf.Max(0f, airCorridorLateralOffsetTiles);
        public float AirCorridorNodeSpacingTiles => Mathf.Max(0.5f, airCorridorNodeSpacingTiles);

        public float GetSpawnInterval(RaidPhase phase)
        {
            switch (phase)
            {
                case RaidPhase.Phase1:
                    return Mathf.Max(0.2f, phase1SpawnInterval);
                case RaidPhase.Phase2:
                    return Mathf.Max(0.2f, phase2SpawnInterval);
                case RaidPhase.Phase3:
                    return Mathf.Max(0.2f, phase3SpawnInterval);
                default:
                    return Mathf.Max(0.2f, phase1SpawnInterval);
            }
        }

        public int GetSpawnPerPulse(RaidPhase phase)
        {
            switch (phase)
            {
                case RaidPhase.Phase1:
                    return Mathf.Clamp(phase1SpawnPerPulse, 1, 8);
                case RaidPhase.Phase2:
                    return Mathf.Clamp(phase2SpawnPerPulse, 1, 8);
                case RaidPhase.Phase3:
                    return Mathf.Clamp(phase3SpawnPerPulse, 1, 8);
                default:
                    return 1;
            }
        }

        public float GetSpawnSpacing(RaidPhase phase)
        {
            switch (phase)
            {
                case RaidPhase.Phase1:
                    return Mathf.Max(0f, phase1SpawnSpacing);
                case RaidPhase.Phase2:
                    return Mathf.Max(0f, phase2SpawnSpacing);
                case RaidPhase.Phase3:
                    return Mathf.Max(0f, phase3SpawnSpacing);
                default:
                    return 0f;
            }
        }

        public string[] GetSpawnEnemyIds(RaidPhase phase)
        {
            switch (phase)
            {
                case RaidPhase.Phase1:
                    return phase1EnemyIds;
                case RaidPhase.Phase2:
                    return phase2EnemyIds;
                case RaidPhase.Phase3:
                    return phase3EnemyIds;
                default:
                    return phase1EnemyIds;
            }
        }

        public float GetRaidMoveSpeed(float baseMoveSpeed, RaidPhase phase, EnemyMovementType movementType)
        {
            float speed = Mathf.Max(0f, baseMoveSpeed);
            float threshold = Mathf.Max(0.01f, slowMoveSpeedThreshold);
            float target = Mathf.Max(0.01f, slowMoveSpeedTarget);

            if (speed > 0f && speed < threshold && target > speed)
            {
                float slowRatio = 1f - Mathf.Clamp01(speed / threshold);
                float correction = slowRatio * Mathf.Clamp01(slowMoveSpeedCorrection);
                speed = Mathf.Lerp(speed, target, correction);
            }

            speed *= GetPhaseMoveSpeedMultiplier(phase);

            if (movementType == EnemyMovementType.Air)
            {
                speed *= Mathf.Max(0.01f, airMoveSpeedMultiplier);
            }

            return Mathf.Max(0f, speed);
        }

        private float GetPhaseMoveSpeedMultiplier(RaidPhase phase)
        {
            switch (phase)
            {
                case RaidPhase.Phase1:
                    return Mathf.Max(0.01f, phase1MoveSpeedMultiplier);
                case RaidPhase.Phase2:
                    return Mathf.Max(0.01f, phase2MoveSpeedMultiplier);
                case RaidPhase.Phase3:
                    return Mathf.Max(0.01f, phase3MoveSpeedMultiplier);
                default:
                    return 1f;
            }
        }

        private void OnValidate()
        {
            bossMaxHp = Mathf.Max(1f, bossMaxHp);
            timeLimitSeconds = Mathf.Max(1f, timeLimitSeconds);
            bossSkillGaugeMax = Mathf.Max(1f, bossSkillGaugeMax);
            goalSkillGaugeGain = Mathf.Max(0f, goalSkillGaugeGain);
            bossSkillCastDelay = Mathf.Max(0f, bossSkillCastDelay);
            bossSkillStrikeInterval = Mathf.Max(0f, bossSkillStrikeInterval);
            bossSkillStrikeTelegraphDuration = Mathf.Max(0f, bossSkillStrikeTelegraphDuration);
            bossSkillMaxHpDamageRatio = Mathf.Clamp(bossSkillMaxHpDamageRatio, 0.01f, 1f);
            bossSkillMinimumDamage = Mathf.Max(0f, bossSkillMinimumDamage);
            raidAttackGaugeMax = Mathf.Max(1f, raidAttackGaugeMax);
            enemyKillRaidGaugeGain = Mathf.Max(0f, enemyKillRaidGaugeGain);
            bossPhysicalDefense = Mathf.Max(0f, bossPhysicalDefense);
            bossMagicalDefense = Mathf.Max(0f, bossMagicalDefense);
            raidAttackFullGaugeDuration = Mathf.Max(1f, raidAttackFullGaugeDuration);
            raidAttackDamageMultiplier = Mathf.Max(0.01f, raidAttackDamageMultiplier);
            raidCombatNumberScale = raidCombatNumberScale > 0f ? Mathf.Clamp(raidCombatNumberScale, 0.25f, 1f) : 0.72f;
            raidCriticalNumberScale = raidCriticalNumberScale > 0f ? Mathf.Clamp(raidCriticalNumberScale, 0.25f, 1.25f) : 0.95f;
            raidBossDamageNumberScale = raidBossDamageNumberScale > 0f ? Mathf.Clamp(raidBossDamageNumberScale, 1f, 3f) : 1.4f;
            costMax = Mathf.Max(1, costMax);
            startingCost = Mathf.Clamp(startingCost, 0, costMax);
            costRegenPerSecond = Mathf.Max(0f, costRegenPerSecond);
            maxDeployedUnits = Mathf.Clamp(maxDeployedUnits, 1, 16);
            autoDeployDecisionInterval = Mathf.Max(0.1f, autoDeployDecisionInterval);
            autoDeployMinimumScore = Mathf.Max(0f, autoDeployMinimumScore);
            autoDeployVanguardLineProgress = Mathf.Clamp(autoDeployVanguardLineProgress, 0.2f, 0.8f);
            autoDeployFrontPenaltyProgress = Mathf.Clamp(autoDeployFrontPenaltyProgress, 0.05f, 0.6f);
            manualFacingDragDistance = Mathf.Max(0.05f, manualFacingDragDistance);
            spawnStartDelay = Mathf.Max(0f, spawnStartDelay);
            phase1OpeningDuration = Mathf.Max(0f, phase1OpeningDuration);
            phase1OpeningSpawnInterval = Mathf.Max(0.5f, phase1OpeningSpawnInterval);
            phase1OpeningSpawnPerPulse = Mathf.Clamp(phase1OpeningSpawnPerPulse, 1, 3);
            phase1MaxActiveEnemies = Mathf.Max(1, phase1MaxActiveEnemies);
            phase2MaxActiveEnemies = Mathf.Max(1, phase2MaxActiveEnemies);
            phase3MaxActiveEnemies = Mathf.Max(1, phase3MaxActiveEnemies);
            phase1SpawnInterval = Mathf.Max(0.2f, phase1SpawnInterval);
            phase2SpawnInterval = Mathf.Max(0.2f, phase2SpawnInterval);
            phase3SpawnInterval = Mathf.Max(0.2f, phase3SpawnInterval);
            phase1SpawnPerPulse = Mathf.Clamp(phase1SpawnPerPulse, 1, 8);
            phase2SpawnPerPulse = Mathf.Clamp(phase2SpawnPerPulse, 1, 8);
            phase3SpawnPerPulse = Mathf.Clamp(phase3SpawnPerPulse, 1, 8);
            phase1SpawnSpacing = Mathf.Max(0f, phase1SpawnSpacing);
            phase2SpawnSpacing = Mathf.Max(0f, phase2SpawnSpacing);
            phase3SpawnSpacing = Mathf.Max(0f, phase3SpawnSpacing);
            slowMoveSpeedThreshold = Mathf.Max(0.01f, slowMoveSpeedThreshold);
            slowMoveSpeedTarget = Mathf.Max(0.01f, slowMoveSpeedTarget);
            slowMoveSpeedCorrection = Mathf.Clamp01(slowMoveSpeedCorrection);
            phase1MoveSpeedMultiplier = Mathf.Max(0.01f, phase1MoveSpeedMultiplier);
            phase2MoveSpeedMultiplier = Mathf.Max(0.01f, phase2MoveSpeedMultiplier);
            phase3MoveSpeedMultiplier = Mathf.Max(0.01f, phase3MoveSpeedMultiplier);
            airFlightHeight = Mathf.Max(0f, airFlightHeight);
            airMoveSpeedMultiplier = Mathf.Max(0.01f, airMoveSpeedMultiplier);
            airCorridorVariantCount = Mathf.Clamp(airCorridorVariantCount, 1, 3);
            airCorridorLateralOffsetTiles = Mathf.Max(0f, airCorridorLateralOffsetTiles);
            airCorridorNodeSpacingTiles = Mathf.Max(0.5f, airCorridorNodeSpacingTiles);
            phase2HpRatio = Mathf.Clamp(phase2HpRatio, 0.01f, 0.99f);
            phase3HpRatio = Mathf.Clamp(phase3HpRatio, 0.01f, 0.99f);

            if (phase3HpRatio >= phase2HpRatio)
            {
                phase3HpRatio = Mathf.Max(0.01f, phase2HpRatio - 0.01f);
            }
        }
    }
}
