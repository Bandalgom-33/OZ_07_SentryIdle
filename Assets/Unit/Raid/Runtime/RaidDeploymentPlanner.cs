using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Raid.Data;
using EndlessGuard.Unit.Runtime;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    [DisallowMultipleComponent]
    public sealed class RaidDeploymentPlanner : MonoBehaviour
    {
        private const float FormationForecastSeconds = 7f;
        private const float SecondaryLineOffset = 0.10f;
        private const float PrimaryLineTolerance = 0.07f;
        private const float SecondaryLineTolerance = 0.07f;
        private const int FormationReserveCost = 8;
        private const float InfluenceProgressRadius = 0.24f;
        private const float KillZoneMinProgress = 0.38f;
        private const float KillZoneMaxProgress = 0.84f;
        private const float KillZoneProgressStep = 0.04f;
        private const float KillZoneThreatLead = 0.14f;
        private const float ReservationMaxWaitSeconds = 18f;
        private const float ReservationReactionBuffer = 1.25f;
        private const float ReservationEmergencySeconds = 2.5f;
        private const float PriorityItemScoreMultiplier = 3.25f;

        private RaidBattleController battle;
        private RaidBoardRuntime boardRuntime;
        private RaidRosterRuntime roster;
        private RaidDeploymentRuntime deployment;
        private RaidItemRuntime itemRuntime;
        private float decisionElapsed;
        private bool preparedForRaidStart;
        private RaidRosterSlotState reservedSlot;
        private int reservedPathIndex = -1;
        private FormationIntent reservedIntent = FormationIntent.Wait;

        private void Awake()
        {
            battle = GetComponent<RaidBattleController>();
            boardRuntime = GetComponent<RaidBoardRuntime>();
            roster = GetComponent<RaidRosterRuntime>();
            deployment = GetComponent<RaidDeploymentRuntime>();
            itemRuntime = GetComponent<RaidItemRuntime>();
        }

        private void OnEnable()
        {
            ResolveDependencies();

            if (battle != null)
            {
                battle.OnRaidPreparing += HandleRaidPreparing;
                battle.OnRaidStarted += HandleRaidStarted;
            }
        }

        private void OnDisable()
        {
            if (battle != null)
            {
                battle.OnRaidPreparing -= HandleRaidPreparing;
                battle.OnRaidStarted -= HandleRaidStarted;
            }

            preparedForRaidStart = false;
            ClearReservation();
        }

        private void HandleRaidPreparing()
        {
            float interval = battle != null && battle.Config != null ? battle.Config.AutoDeployDecisionInterval : 0.65f;
            decisionElapsed = Mathf.Max(0f, interval);
            preparedForRaidStart = true;
            ClearReservation();
        }

        private void HandleRaidStarted()
        {
            preparedForRaidStart = false;
        }

        private void Update()
        {
            if (battle == null || boardRuntime == null || roster == null || deployment == null || battle.Config == null || battle.State != RaidBattleState.Running || battle.IsTransitioning || battle.Mode != RaidBattleMode.Auto || !deployment.HasCapacity)
            {
                if (!preparedForRaidStart)
                {
                    decisionElapsed = 0f;
                }

                return;
            }

            decisionElapsed += Time.deltaTime;
            float interval = battle.Config != null ? battle.Config.AutoDeployDecisionInterval : 0.65f;
            if (decisionElapsed < interval)
            {
                return;
            }

            decisionElapsed = 0f;
            TryAutoDeploy();
        }

        public GridFacingDirection GetBestFacing(UnitDataSO unitData, Vector2Int tile)
        {
            float bestScore = float.NegativeInfinity;
            GridFacingDirection bestFacing = GridFacingDirection.North;

            for (int i = 0; i < 4; i++)
            {
                GridFacingDirection facing = (GridFacingDirection)i;
                float score = ScorePlacement(unitData, tile, facing);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestFacing = facing;
                }
            }

            return bestFacing;
        }

        private bool TryAutoDeploy()
        {
            if (boardRuntime.Board == null || roster == null || battle.Config == null)
            {
                ClearReservation();
                return false;
            }

            if (deployment.DeployedCount >= battle.Config.MaxDeployedUnits)
            {
                ClearReservation();
                return false;
            }

            bool hasFormationPlan = TryBuildFormationPlan(out FormationPlan plan);
            bool emergencyPlan = hasFormationPlan && plan.Intent != FormationIntent.Wait && IsEmergencyPlan(plan);

            if (!emergencyPlan && TryFindPriorityItemCandidates(out DeploymentCandidate itemAffordable, out bool hasItemAffordable, out DeploymentCandidate itemOverall, out bool hasItemOverall))
            {
                ClearReservation();

                if (hasItemAffordable)
                {
                    return deployment.TryDeploy(
                        itemAffordable.Slot,
                        itemAffordable.Tile,
                        itemAffordable.Facing,
                        true,
                        out _);
                }

                if (hasItemOverall && CanWaitForPriorityItem(itemOverall))
                {
                    return false;
                }
            }

            if (!hasFormationPlan || plan.Intent == FormationIntent.Wait)
            {
                ClearReservation();
                return false;
            }

            DeploymentCandidate bestAffordable = default;
            DeploymentCandidate bestOverall = default;
            bool hasAffordable = false;
            bool hasOverall = false;

            for (int team = 0; team < RaidRosterRuntime.TeamCount; team++)
            {
                for (int slotIndex = 0; slotIndex < RaidRosterRuntime.SlotsPerTeam; slotIndex++)
                {
                    RaidRosterSlotState slot = roster.GetSlot(team, slotIndex);

                    if (slot == null ||
                        !slot.CanDeploy ||
                        slot.UnitData == null ||
                        slot.UnitData.UnitPrefab == null ||
                        !IsUnitSuitableForIntent(slot.UnitData, plan.Intent))
                    {
                        continue;
                    }

                    if (!TryFindBestPlacement(slot, plan, out DeploymentCandidate candidate))
                    {
                        continue;
                    }

                    if (!hasOverall || candidate.Score > bestOverall.Score)
                    {
                        bestOverall = candidate;
                        hasOverall = true;
                    }

                    if (slot.UnitData.SummonCost <= battle.CurrentCost &&
                        (!hasAffordable || candidate.Score > bestAffordable.Score))
                    {
                        bestAffordable = candidate;
                        hasAffordable = true;
                    }
                }
            }

            if (!hasOverall || bestOverall.Score < battle.Config.AutoDeployMinimumScore)
            {
                ClearReservation();
                return false;
            }

            if (reservedSlot != null)
            {
                if (!ReservationMatches(plan) ||
                    !reservedSlot.CanDeploy ||
                    reservedSlot.UnitData == null ||
                    !IsUnitSuitableForIntent(reservedSlot.UnitData, plan.Intent) ||
                    !TryFindBestPlacement(reservedSlot, plan, out DeploymentCandidate reservedCandidate) ||
                    reservedCandidate.Score < battle.Config.AutoDeployMinimumScore)
                {
                    ClearReservation();
                }
                else
                {
                    int reservedCost = Mathf.Max(0, reservedSlot.UnitData.SummonCost);

                    if (reservedCost <= battle.CurrentCost)
                    {
                        ClearReservation();
                        return deployment.TryDeploy(
                            reservedCandidate.Slot,
                            reservedCandidate.Tile,
                            reservedCandidate.Facing,
                            true,
                            out _);
                    }

                    if (CanSafelyWaitForCandidate(plan, reservedCandidate))
                    {
                        return false;
                    }

                    ClearReservation();
                }
            }

            if (!hasAffordable)
            {
                SetReservation(plan, bestOverall);
                return false;
            }

            if (ShouldReserveForBetterUnit(plan, bestAffordable, bestOverall))
            {
                SetReservation(plan, bestOverall);
                return false;
            }

            ClearReservation();
            return deployment.TryDeploy(
                bestAffordable.Slot,
                bestAffordable.Tile,
                bestAffordable.Facing,
                true,
                out _);
        }

        private bool TryFindPriorityItemCandidates(out DeploymentCandidate bestAffordable, out bool hasAffordable, out DeploymentCandidate bestOverall, out bool hasOverall)
        {
            bestAffordable = default;
            bestOverall = default;
            hasAffordable = false;
            hasOverall = false;

            if (itemRuntime == null || itemRuntime.ActiveItemCount <= 0 || boardRuntime.Board == null)
            {
                return false;
            }

            float costRegen = battle.Config != null ? Mathf.Max(0f, battle.Config.CostRegenPerSecond) : 0f;
            for (int itemIndex = 0; itemIndex < itemRuntime.ActiveItemCount; itemIndex++)
            {
                if (!itemRuntime.TryGetActiveItem(itemIndex, out _, out Vector2Int tile, out float remainingSeconds) || remainingSeconds <= 0f)
                {
                    continue;
                }

                for (int team = 0; team < RaidRosterRuntime.TeamCount; team++)
                {
                    for (int slotIndex = 0; slotIndex < RaidRosterRuntime.SlotsPerTeam; slotIndex++)
                    {
                        RaidRosterSlotState slot = roster.GetSlot(team, slotIndex);
                        if (slot == null || !slot.CanDeploy || slot.UnitData == null || slot.UnitData.UnitPrefab == null || !deployment.IsTileDeployable(slot.UnitData, tile))
                        {
                            continue;
                        }

                        UnitDataSO unitData = slot.UnitData;
                        int summonCost = Mathf.Max(0, unitData.SummonCost);
                        if (summonCost > battle.Config.CostMax)
                        {
                            continue;
                        }

                        bool affordableNow = summonCost <= battle.CurrentCost;
                        if (!affordableNow && costRegen <= 0f)
                        {
                            continue;
                        }

                        float itemValue = Mathf.Max(0.25f, itemRuntime.GetDeploymentBonus(unitData, tile));

                        for (int facingIndex = 0; facingIndex < 4; facingIndex++)
                        {
                            GridFacingDirection facing = (GridFacingDirection)facingIndex;
                            float placementScore = ScorePlacement(unitData, tile, facing);
                            if (float.IsNegativeInfinity(placementScore))
                            {
                                continue;
                            }

                            float expiryUrgency = 1f + Mathf.Clamp01(1f - remainingSeconds / 10f) * 1.5f;
                            float score = itemValue * PriorityItemScoreMultiplier * expiryUrgency + placementScore * 0.20f - summonCost * 0.08f;
                            DeploymentCandidate candidate = new DeploymentCandidate(slot, tile, facing, score);

                            if (!hasOverall || candidate.Score > bestOverall.Score)
                            {
                                bestOverall = candidate;
                                hasOverall = true;
                            }

                            if (affordableNow && (!hasAffordable || candidate.Score > bestAffordable.Score))
                            {
                                bestAffordable = candidate;
                                hasAffordable = true;
                            }
                        }
                    }
                }
            }

            return hasOverall;
        }

        private bool CanWaitForPriorityItem(DeploymentCandidate candidate)
        {
            if (candidate.Slot == null || candidate.Slot.UnitData == null || battle.Config == null || itemRuntime == null)
            {
                return false;
            }

            int summonCost = Mathf.Max(0, candidate.Slot.UnitData.SummonCost);
            if (summonCost <= battle.CurrentCost)
            {
                return true;
            }

            float costRegen = battle.Config.CostRegenPerSecond;
            if (costRegen <= 0f || summonCost > battle.Config.CostMax)
            {
                return false;
            }

            float secondsToAfford = (summonCost - battle.CurrentCost) / costRegen;
            return itemRuntime.EnsureReservationWindow(candidate.Tile, secondsToAfford);
        }

        private bool TryFindBestPlacement(RaidRosterSlotState slot, FormationPlan plan, out DeploymentCandidate best)
        {
            best = default;
            RaidBoard board = boardRuntime.Board;
            UnitDataSO unitData = slot.UnitData;
            float bestScore = float.NegativeInfinity;
            bool found = false;

            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    Vector2Int tile = new Vector2Int(x, y);

                    if (!deployment.IsTileDeployable(unitData, tile) ||
                        !IsTileSuitableForIntent(unitData, tile, plan))
                    {
                        continue;
                    }

                    for (int facingIndex = 0; facingIndex < 4; facingIndex++)
                    {
                        GridFacingDirection facing = (GridFacingDirection)facingIndex;
                        float formationScore = ScoreFormationCandidate(unitData, tile, facing, plan);

                        if (float.IsNegativeInfinity(formationScore))
                        {
                            continue;
                        }

                        float score = ScorePlacement(unitData, tile, facing) + formationScore;

                        if (!found || score > bestScore)
                        {
                            found = true;
                            bestScore = score;
                            best = new DeploymentCandidate(slot, tile, facing, score);
                        }
                    }
                }
            }

            return found;
        }

        private float ScorePlacement(UnitDataSO unitData, Vector2Int tile, GridFacingDirection facing)
        {
            if (unitData == null || boardRuntime.Board == null)
            {
                return float.NegativeInfinity;
            }

            RaidBoard board = boardRuntime.Board;
            RaidTile raidTile = board.GetTile(tile);
            AttackSettings attack = unitData.AttackSettings;
            bool groundLaneTile = raidTile.IsPath && raidTile.IsGroundCombatDeployable;
            bool highGroundTile = raidTile.IsHighGroundDeployable;
            bool canAttackGround = CanAttackLayer(attack, CombatTargetLayer.Ground);
            bool canAttackAir = CanAttackLayer(attack, CombatTargetLayer.Air);
            float score = 0f;
            float nearbyGroundThreat = 0f;
            float emergencyInterceptionScore = 0f;
            float emergencyBehindPenalty = 0f;
            float urgentUncoveredPenalty = 0f;
            float damageMatchScore = 0f;
            int liveEnemyCount = 0;
            int groundEnemyCount = 0;
            int urgentGroundEnemyCount = 0;
            int airEnemyCount = 0;
            int coveredEnemyCount = 0;
            int coveredGroundCount = 0;
            int coveredAirCount = 0;
            int coveredSmallCount = 0;
            int coveredMediumCount = 0;
            int coveredLargeCount = 0;
            int nearbyAllies = 0;

            bool hasRouteProgress = TryGetNearestTravelPathProgress(tile, out float candidateRouteProgress, out float candidateRouteDistance);
            if (hasRouteProgress)
            {
                score += ScoreDefenseLinePosition(unitData, groundLaneTile, highGroundTile, candidateRouteProgress, candidateRouteDistance);
                score += ScoreInfluenceBalance(unitData, tile, facing, candidateRouteProgress);
            }

            foreach (EnemyRuntimeState enemy in CombatRegistry.Enemies)
            {
                if (enemy == null || !enemy.IsInitialized || enemy.Health == null || enemy.Health.IsDead || enemy.GridPosition == null || !enemy.GridPosition.IsInitialized)
                {
                    continue;
                }

                liveEnemyCount++;
                bool air = enemy.GridPosition.TargetLayer == CombatTargetLayer.Air;
                if (air)
                {
                    airEnemyCount++;
                }
                else
                {
                    groundEnemyCount++;
                }

                float progress = enemy.Move != null ? enemy.Move.PathProgress : 0f;
                float urgency = enemy.Move != null ? 0.65f + progress * 1.65f : 0.75f;
                int distance = Manhattan(tile, enemy.GridPosition.TileCoordinate);
                bool canCoverEnemy = CanAttackEnemyFrom(unitData, tile, facing, enemy);

                if (!air && progress >= 0.55f)
                {
                    urgentGroundEnemyCount++;

                    if (!canCoverEnemy && attack != null && attack.AttackMode != AttackMode.None)
                    {
                        urgentUncoveredPenalty += 1.5f + progress * 2.5f;
                    }
                }

                if (!air && hasRouteProgress && enemy.Move != null && progress >= 0.45f)
                {
                    float routeDelta = candidateRouteProgress - progress;

                    if (routeDelta >= 0.025f)
                    {
                        float aheadQuality = Mathf.Clamp01(routeDelta / 0.28f);
                        float emergencyWeight = 2.5f + progress * progress * 9f;

                        if (groundLaneTile && unitData.BlockCount > 0)
                        {
                            emergencyInterceptionScore += emergencyWeight * (1.2f + aheadQuality * 2.2f);
                        }
                        else if (canCoverEnemy)
                        {
                            emergencyInterceptionScore += emergencyWeight * (0.45f + aheadQuality * 0.8f);
                        }
                    }
                    else if (routeDelta <= -0.02f)
                    {
                        float behindAmount = Mathf.Clamp01(-routeDelta / 0.3f);
                        float behindWeight = 4f + progress * progress * 14f;

                        if (groundLaneTile && unitData.BlockCount > 0)
                        {
                            emergencyBehindPenalty += behindWeight * (1f + behindAmount * 2.5f);
                        }
                        else if (progress >= 0.6f)
                        {
                            emergencyBehindPenalty += behindWeight * (0.35f + behindAmount);
                        }
                    }
                }

                if (groundLaneTile && unitData.BlockCount > 0 && !air && enemy.Move != null && enemy.Move.TryGetGoalTile(out Vector2Int goalTile))
                {
                    int enemyGoalDistance = Manhattan(enemy.GridPosition.TileCoordinate, goalTile);
                    int candidateGoalDistance = Manhattan(tile, goalTile);
                    int interceptDistance = Manhattan(tile, enemy.GridPosition.TileCoordinate);

                    if (candidateGoalDistance < enemyGoalDistance)
                    {
                        float progressWeight = 0.35f + progress * progress * 2.4f;
                        float interceptCloseness = Mathf.Max(0f, 9f - interceptDistance);
                        float goalGuard = Mathf.Max(0f, 8f - candidateGoalDistance);
                        emergencyInterceptionScore += progressWeight * (interceptCloseness * 1.25f + goalGuard * 0.9f);
                    }
                    else if (progress >= 0.5f && candidateGoalDistance > enemyGoalDistance)
                    {
                        emergencyBehindPenalty += (0.5f + progress * 2f) * Mathf.Min(8f, 1f + candidateGoalDistance - enemyGoalDistance);
                    }
                }

                if (canCoverEnemy)
                {
                    coveredEnemyCount++;
                    if (air)
                    {
                        coveredAirCount++;
                    }
                    else
                    {
                        coveredGroundCount++;
                    }

                    float layerWeight = air ? 1.35f : 1f;
                    score += 5.1f * urgency * layerWeight;
                    damageMatchScore += GetDamageMatchScore(unitData, enemy);

                    if (enemy.DataLink != null && enemy.DataLink.HasData && enemy.DataLink.EnemyData != null)
                    {
                        switch (enemy.DataLink.EnemyData.Size)
                        {
                            case EnemySize.Small:
                                coveredSmallCount++;
                                break;
                            case EnemySize.Medium:
                                coveredMediumCount++;
                                break;
                            case EnemySize.Large:
                                coveredLargeCount++;
                                break;
                        }
                    }
                }

                if (groundLaneTile && unitData.BlockCount > 0 && !air && distance <= 6)
                {
                    nearbyGroundThreat += (7 - distance) * 0.9f * urgency;
                }
            }

            foreach (UnitRuntimeState ally in CombatRegistry.Units)
            {
                if (ally == null || ally.IsSummon || ally.Health == null || ally.Health.IsDead || ally.GridPosition == null || !ally.GridPosition.IsInitialized)
                {
                    continue;
                }

                int distance = Manhattan(tile, ally.GridPosition.TileCoordinate);
                if (distance <= 3)
                {
                    nearbyAllies++;
                }
            }

            int pathCoverage = 0;
            if (attack != null && attack.AttackMode != AttackMode.None && attack.BasicAttackRange != null)
            {
                pathCoverage = CountPathCoverage(unitData, tile, facing);
                score += pathCoverage * (highGroundTile ? 1.0f : 0.58f);

                if (highGroundTile && pathCoverage == 0)
                {
                    score -= 6f;
                }
            }

            score += ScoreCombatProfile(unitData, attack, coveredEnemyCount, damageMatchScore, groundLaneTile);
            score += ScorePassiveProfile(unitData, tile, groundLaneTile, highGroundTile, liveEnemyCount, groundEnemyCount, airEnemyCount, coveredEnemyCount, coveredAirCount, coveredSmallCount, coveredMediumCount, coveredLargeCount, nearbyAllies);
            score += Mathf.Min(32f, emergencyInterceptionScore);
            score -= Mathf.Min(24f, emergencyBehindPenalty);

            if (urgentGroundEnemyCount > 0 && attack != null && attack.AttackMode != AttackMode.None && coveredEnemyCount == 0)
            {
                score -= Mathf.Min(10f, urgentUncoveredPenalty);
            }

            if (unitData.BlockCount > 0)
            {
                if (groundLaneTile)
                {
                    score += 4.5f + unitData.BlockCount * 2.6f + Mathf.Min(9f, nearbyGroundThreat);
                    if (attack != null && attack.AttackMode == AttackMode.Melee)
                    {
                        score += 2.2f;
                    }
                }
                else if (CanUseGroundPlacement(unitData))
                {
                    float offLanePenalty = attack != null && attack.AttackMode == AttackMode.Melee ? 5f : 1.2f;
                    score -= offLanePenalty + unitData.BlockCount * 1.15f;
                }
            }

            if (highGroundTile && attack != null && attack.AttackMode == AttackMode.Ranged)
            {
                score += 1.7f;
                if (attack.AttackTarget == AttackTarget.GroundAndAir)
                {
                    score += 1.25f;
                }
            }

            if (airEnemyCount > 0)
            {
                if (canAttackAir)
                {
                    score += Mathf.Min(4.5f, airEnemyCount * 0.65f + coveredAirCount * 0.75f);
                }
                else if (highGroundTile)
                {
                    score -= Mathf.Min(10f, 3f + airEnemyCount * 1.25f);
                }
            }

            if (groundEnemyCount > 0 && !canAttackGround && attack != null && attack.AttackMode != AttackMode.None)
            {
                score -= Mathf.Min(8f, 3f + groundEnemyCount * 0.9f);
            }

            if (attack == null || attack.AttackMode == AttackMode.None || attack.TargetCount <= 0)
            {
                if (deployment.DeployedCount >= 3)
                {
                    score += 2.3f + Mathf.Min(2f, nearbyAllies * 0.5f);
                }
                else
                {
                    score -= 2f;
                }
            }
            else if (liveEnemyCount > 0 && coveredEnemyCount == 0)
            {
                score -= highGroundTile ? 5f : 3f;
            }

            if (coveredGroundCount > 0 && groundLaneTile && unitData.BlockCount > 0)
            {
                score += Mathf.Min(2.5f, coveredGroundCount * 0.55f);
            }

            if (itemRuntime != null)
            {
                score += itemRuntime.GetDeploymentBonus(unitData, tile);
            }

            score -= nearbyAllies * 1.10f;
            if (nearbyAllies >= 3)
            {
                score -= (nearbyAllies - 2) * 1.75f;
            }
            score -= Mathf.Max(0, unitData.SummonCost) * 0.065f;
            score += StableTieBreaker(unitData.UnitId, tile, facing);
            return score;
        }

        private static float ScoreCombatProfile(UnitDataSO unitData, AttackSettings attack, int coveredEnemyCount, float damageMatchScore, bool groundLaneTile)
        {
            if (unitData == null || unitData.BaseStats == null)
            {
                return 0f;
            }

            CombatStats stats = unitData.BaseStats;
            float score = 0f;

            if (attack != null && attack.AttackMode != AttackMode.None && attack.TargetCount > 0 && coveredEnemyCount > 0)
            {
                float attackPower;
                switch (attack.DamageType)
                {
                    case DamageType.Physical:
                        attackPower = stats.PhysicalAttack;
                        break;
                    case DamageType.Magical:
                        attackPower = stats.MagicalAttack;
                        break;
                    default:
                        attackPower = Mathf.Max(stats.PhysicalAttack, stats.MagicalAttack);
                        break;
                }

                float attacksPerSecond = Mathf.Max(0f, stats.BaseAttacksPerSecond);
                int effectiveTargets = Mathf.Min(Mathf.Max(1, attack.TargetCount), coveredEnemyCount);
                float criticalFactor = 1f + Mathf.Clamp01(unitData.CriticalChancePercent / 100f) * Mathf.Max(0f, unitData.CriticalDamageBonusPercent) / 100f;
                float normalizedAttack = Mathf.Log10(1f + Mathf.Max(0f, attackPower)) * 0.8f;
                float normalizedSpeed = Mathf.Clamp(attacksPerSecond, 0f, 4f) * 0.55f;

                score += (normalizedAttack + normalizedSpeed) * criticalFactor;
                score += Mathf.Max(0, effectiveTargets - 1) * 0.9f;
                score += damageMatchScore * 0.45f;
            }

            if (groundLaneTile && unitData.BlockCount > 0)
            {
                float defenseTotal = Mathf.Max(0f, stats.PhysicalDefense) + Mathf.Max(0f, stats.MagicalDefense);
                float survivability = Mathf.Log10(1f + Mathf.Max(0f, stats.MaxHp)) * 0.55f + Mathf.Log10(1f + defenseTotal) * 0.35f;
                score += survivability + unitData.BlockCount * 0.35f;
            }

            return score;
        }

        private static float GetDamageMatchScore(UnitDataSO unitData, EnemyRuntimeState enemy)
        {
            if (unitData == null || unitData.BaseStats == null || unitData.AttackSettings == null || enemy == null || enemy.Stats == null || !enemy.Stats.IsInitialized)
            {
                return 0f;
            }

            AttackSettings attack = unitData.AttackSettings;
            float attackPower;
            float defense;

            switch (attack.DamageType)
            {
                case DamageType.Physical:
                    attackPower = unitData.BaseStats.PhysicalAttack;
                    defense = enemy.Stats.PhysicalDefense;
                    break;
                case DamageType.Magical:
                    attackPower = unitData.BaseStats.MagicalAttack;
                    defense = enemy.Stats.MagicalDefense;
                    break;
                default:
                    return 0f;
            }

            attackPower = Mathf.Max(0f, attackPower);
            defense = Mathf.Max(0f, defense);
            return attackPower / Mathf.Max(1f, attackPower + defense);
        }

        private float ScorePassiveProfile(UnitDataSO unitData, Vector2Int tile, bool groundLaneTile, bool highGroundTile, int liveEnemyCount, int groundEnemyCount, int airEnemyCount, int coveredEnemyCount, int coveredAirCount, int coveredSmallCount, int coveredMediumCount, int coveredLargeCount, int nearbyAllies)
        {
            if (unitData == null || unitData.Passives == null || unitData.Passives.Count == 0)
            {
                return 0f;
            }

            float score = 0f;

            for (int i = 0; i < unitData.Passives.Count; i++)
            {
                PassiveDataSO passive = unitData.Passives[i];
                if (passive == null)
                {
                    continue;
                }

                if (passive is CostGainPassiveSO)
                {
                    score += deployment.DeployedCount < 3 ? 2.2f : 0.7f;
                    if (groundLaneTile)
                    {
                        score += 0.5f;
                    }
                }
                else if (passive is BlockAttackSO || passive is BlockGaugeSO || passive is DefenseBuffSO || passive is HeavyArmorSO || passive is ReflectSO || passive is LifeStealSO || passive is LostHpAttackSO || passive is BerserkSO)
                {
                    if (groundLaneTile)
                    {
                        score += 1.4f + unitData.BlockCount * 0.25f;
                    }
                }
                else if (passive is SlowSO || passive is AttackSlowSO || passive is WeakSO || passive is ExplosionSO || passive is RandomAttackSO || passive is SnipeBurstSO)
                {
                    score += Mathf.Min(2.8f, coveredEnemyCount * 0.55f + liveEnemyCount * 0.08f);
                }
                else if (passive is AirAttackSO)
                {
                    score += airEnemyCount > 0 ? 2.8f + Mathf.Min(1.5f, coveredAirCount * 0.4f) : 0.2f;
                }
                else if (passive is AllyAidSO || passive is DefenseAuraSO || passive is CommandSO || passive is HealSO || passive is CleanseSO || passive is CritBuffSO)
                {
                    score += Mathf.Min(2.8f, nearbyAllies * 0.75f);
                    if (highGroundTile)
                    {
                        score += 0.4f;
                    }
                }
                else if (passive is FrontlineCommandSO || passive is SummonDefenseSO)
                {
                    score += HasSummonSpace(passive, tile) ? 1.6f : 0.25f;
                    score += Mathf.Min(1.5f, nearbyAllies * 0.35f);
                }
                else if (passive is CritSummonSO || passive is SummonSO)
                {
                    score += HasSummonSpace(passive, tile) ? 2.4f : -2.5f;
                }
                else if (passive is AttackSpeedSO)
                {
                    score += coveredEnemyCount > 0 ? 1.2f + Mathf.Min(1.2f, coveredSmallCount * 0.35f) : 0f;
                }
                else if (passive is SizeDamagePassiveSO || passive is SizeAttackSO)
                {
                    score += coveredSmallCount * 0.25f + coveredMediumCount * 0.35f + coveredLargeCount * 0.65f;
                }
                else if (passive is SnipeSO)
                {
                    score += coveredEnemyCount > 0 ? 1.1f + Mathf.Min(1.5f, coveredLargeCount * 0.5f) : 0f;
                }
                else
                {
                    score += 0.2f;
                }
            }

            if (groundEnemyCount == 0 && groundLaneTile && unitData.BlockCount > 0)
            {
                score -= 5.5f;
            }

            return score;
        }

        private bool TryBuildFormationPlan(out FormationPlan plan)
        {
            plan = default;

            if (boardRuntime == null ||
                boardRuntime.Board == null ||
                boardRuntime.TravelPaths == null ||
                boardRuntime.TravelPaths.Count == 0)
            {
                return false;
            }

            FormationPlan bestPlan = default;
            bool found = false;

            for (int pathIndex = 0; pathIndex < boardRuntime.TravelPaths.Count; pathIndex++)
            {
                if (!TryBuildLanePlan(pathIndex, out FormationPlan lanePlan))
                {
                    continue;
                }

                if (!found || lanePlan.Priority > bestPlan.Priority)
                {
                    bestPlan = lanePlan;
                    found = true;
                }
            }

            if (!found)
            {
                return false;
            }

            plan = bestPlan;
            return true;
        }

        private bool TrySelectKillZone(int pathIndex, int liveGround, float maxGroundProgress, float maxProjectedGroundProgress, out float progress, out Vector2Int anchor)
        {
            progress = 0f;
            anchor = default;

            if (TryFindEstablishedKillZone(pathIndex, maxGroundProgress, out progress, out anchor))
            {
                return true;
            }

            float idealProgress = liveGround > 0
                ? Mathf.Clamp(Mathf.Max(maxGroundProgress + KillZoneThreatLead, Mathf.Lerp(maxGroundProgress, maxProjectedGroundProgress, 0.45f) + 0.06f), KillZoneMinProgress, KillZoneMaxProgress)
                : 0.52f;
            float bestScore = float.NegativeInfinity;
            bool found = false;
            Vector2Int previousAnchor = new Vector2Int(int.MinValue, int.MinValue);

            for (float candidateProgress = KillZoneMinProgress; candidateProgress <= KillZoneMaxProgress + 0.001f; candidateProgress += KillZoneProgressStep)
            {
                if (!TryGetFormationAnchor(pathIndex, candidateProgress, out Vector2Int candidateAnchor) || candidateAnchor == previousAnchor)
                {
                    continue;
                }

                previousAnchor = candidateAnchor;

                if (!boardRuntime.Board.TryGetTile(candidateAnchor, out RaidTile anchorTile) ||
                    !anchorTile.IsPath ||
                    !anchorTile.IsGroundCombatDeployable)
                {
                    continue;
                }

                if (liveGround > 0 && candidateProgress <= maxGroundProgress + 0.025f)
                {
                    continue;
                }

                int highGroundPositions;
                int supportPositions = CountPotentialSupportPositions(candidateAnchor, out highGroundPositions);
                float lineFit = 1f - Mathf.Clamp01(Mathf.Abs(candidateProgress - idealProgress) / 0.30f);
                float score = lineFit * 12f;
                score += Mathf.Min(6, supportPositions) * 4.25f;
                score += Mathf.Min(4, highGroundPositions) * 1.35f;
                score += (1f - candidateProgress) * 3.5f;

                if (supportPositions <= 0)
                {
                    score -= 12f;
                }

                if (candidateProgress > 0.78f)
                {
                    score -= (candidateProgress - 0.78f) * 35f;
                }

                if (!found || score > bestScore)
                {
                    bestScore = score;
                    progress = candidateProgress;
                    anchor = candidateAnchor;
                    found = true;
                }
            }

            if (found)
            {
                return true;
            }

            float emergencyProgress = Mathf.Clamp(Mathf.Max(KillZoneMinProgress, maxGroundProgress + 0.045f), KillZoneMinProgress, 0.94f);

            if (!TryGetFormationAnchor(pathIndex, emergencyProgress, out anchor))
            {
                return false;
            }

            progress = emergencyProgress;
            return true;
        }

        private bool TryFindEstablishedKillZone(int pathIndex, float maxGroundProgress, out float progress, out Vector2Int anchor)
        {
            progress = 0f;
            anchor = default;
            float bestScore = float.NegativeInfinity;
            bool found = false;

            foreach (UnitRuntimeState ally in CombatRegistry.Units)
            {
                if (!IsEligibleFormationUnit(ally) ||
                    ally.DataLink == null ||
                    !ally.DataLink.HasData ||
                    ally.DataLink.UnitData.BlockCount <= 0 ||
                    !boardRuntime.Board.TryGetTile(ally.GridPosition.TileCoordinate, out RaidTile tile) ||
                    !tile.IsPath ||
                    !TryGetNearestTravelPathProgress(ally.GridPosition.TileCoordinate, out int allyPathIndex, out float allyProgress, out _) ||
                    allyPathIndex != pathIndex ||
                    allyProgress > 0.92f)
                {
                    continue;
                }

                if (maxGroundProgress > 0f && allyProgress <= maxGroundProgress - 0.03f)
                {
                    continue;
                }

                if (allyProgress < KillZoneMinProgress && maxGroundProgress >= 0.25f)
                {
                    continue;
                }

                int supportCount = CountCurrentSupportAtAnchor(ally.GridPosition.TileCoordinate, ally);
                float threatLead = allyProgress - maxGroundProgress;
                float score = supportCount * 7f + ally.DataLink.UnitData.BlockCount * 2.5f;
                score += Mathf.Clamp01((threatLead + 0.04f) / 0.24f) * 3f;
                score += (1f - allyProgress) * 2f;

                if (!found || score > bestScore)
                {
                    bestScore = score;
                    progress = allyProgress;
                    anchor = ally.GridPosition.TileCoordinate;
                    found = true;
                }
            }

            return found;
        }

        private int CountCurrentSupportAtAnchor(Vector2Int anchor, UnitRuntimeState blocker)
        {
            int count = 0;

            foreach (UnitRuntimeState ally in CombatRegistry.Units)
            {
                if (!IsEligibleFormationUnit(ally) || ally == blocker || !CanUnitSupportFormationTile(ally, anchor))
                {
                    continue;
                }

                count++;
            }

            return count;
        }

        private int CountPotentialSupportPositions(Vector2Int anchor, out int highGroundPositions)
        {
            highGroundPositions = 0;
            int count = 0;
            RaidBoard board = boardRuntime.Board;

            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    Vector2Int tile = new Vector2Int(x, y);

                    if (tile == anchor || Manhattan(tile, anchor) > 8 || !board.TryGetTile(tile, out RaidTile raidTile))
                    {
                        continue;
                    }

                    bool supported = false;

                    for (int team = 0; team < RaidRosterRuntime.TeamCount && !supported; team++)
                    {
                        for (int slotIndex = 0; slotIndex < RaidRosterRuntime.SlotsPerTeam; slotIndex++)
                        {
                            RaidRosterSlotState slot = roster.GetSlot(team, slotIndex);

                            if (slot == null || !slot.CanDeploy || slot.UnitData == null || !CanPotentiallyDeployOnTile(slot.UnitData, raidTile))
                            {
                                continue;
                            }

                            if (CanAttackFormationAnchor(slot.UnitData, tile, anchor))
                            {
                                supported = true;
                                break;
                            }
                        }
                    }

                    if (!supported)
                    {
                        continue;
                    }

                    count++;

                    if (raidTile.IsHighGroundDeployable)
                    {
                        highGroundPositions++;
                    }

                    if (count >= 8)
                    {
                        return count;
                    }
                }
            }

            return count;
        }

        private static bool CanPotentiallyDeployOnTile(UnitDataSO unitData, RaidTile tile)
        {
            if (unitData == null)
            {
                return false;
            }

            switch (unitData.Placement)
            {
                case UnitPlacement.Ground:
                    return RaidDeploymentRuntime.IsGroundCombatDeployable(tile);
                case UnitPlacement.HighGround:
                    return tile.IsHighGroundDeployable;
                case UnitPlacement.GroundAndHighGround:
                    return RaidDeploymentRuntime.IsGroundCombatDeployable(tile) || tile.IsHighGroundDeployable;
                default:
                    return false;
            }
        }

        private bool TryBuildLanePlan(int pathIndex, out FormationPlan plan)
        {
            plan = default;

            int liveGround = 0;
            int liveAir = 0;
            float maxGroundProgress = 0f;
            float maxAirProgress = 0f;
            float maxProjectedGroundProgress = 0f;

            foreach (EnemyRuntimeState enemy in CombatRegistry.Enemies)
            {
                if (enemy == null ||
                    !enemy.IsInitialized ||
                    enemy.Health == null ||
                    enemy.Health.IsDead ||
                    enemy.GridPosition == null ||
                    !enemy.GridPosition.IsInitialized ||
                    enemy.Move == null ||
                    !TryGetNearestTravelPathProgress(enemy.GridPosition.TileCoordinate, out int enemyPathIndex, out _, out _) ||
                    enemyPathIndex != pathIndex)
                {
                    continue;
                }

                float progress = Mathf.Clamp01(enemy.Move.PathProgress);
                bool air = enemy.GridPosition.TargetLayer == CombatTargetLayer.Air;

                if (air)
                {
                    liveAir++;
                    maxAirProgress = Mathf.Max(maxAirProgress, progress);
                }
                else
                {
                    liveGround++;
                    maxGroundProgress = Mathf.Max(maxGroundProgress, progress);
                    maxProjectedGroundProgress = Mathf.Max(maxProjectedGroundProgress, ProjectEnemyProgress(enemy, FormationForecastSeconds));
                }
            }

            if (!TrySelectKillZone(pathIndex, liveGround, maxGroundProgress, maxProjectedGroundProgress, out float primaryProgress, out Vector2Int primaryAnchor))
            {
                return false;
            }

            float secondaryProgress = Mathf.Min(0.94f, primaryProgress + SecondaryLineOffset);
            if (!TryGetFormationAnchor(pathIndex, secondaryProgress, out Vector2Int secondaryAnchor))
            {
                secondaryAnchor = primaryAnchor;
                secondaryProgress = primaryProgress;
            }

            int projectedGround = 0;
            int urgentAir = 0;
            float projectedGroundPressure = 0f;

            foreach (EnemyRuntimeState enemy in CombatRegistry.Enemies)
            {
                if (enemy == null ||
                    !enemy.IsInitialized ||
                    enemy.Health == null ||
                    enemy.Health.IsDead ||
                    enemy.GridPosition == null ||
                    !enemy.GridPosition.IsInitialized ||
                    enemy.Move == null ||
                    !TryGetNearestTravelPathProgress(enemy.GridPosition.TileCoordinate, out int enemyPathIndex, out _, out _) ||
                    enemyPathIndex != pathIndex)
                {
                    continue;
                }

                float projectedProgress = ProjectEnemyProgress(enemy, FormationForecastSeconds);
                bool air = enemy.GridPosition.TargetLayer == CombatTargetLayer.Air;

                if (air)
                {
                    if (projectedProgress >= primaryProgress - 0.12f)
                    {
                        urgentAir++;
                    }

                    continue;
                }

                if (projectedProgress >= primaryProgress - 0.12f)
                {
                    projectedGround++;
                    projectedGroundPressure += 1f + Mathf.Clamp01(projectedProgress - primaryProgress + 0.16f) * 0.8f;
                }
            }

            int primaryBlockCapacity = 0;
            int secondaryBlockCapacity = 0;
            int supportCoverage = 0;
            int airCoverage = 0;
            int primaryBlockerCount = 0;
            int secondaryBlockerCount = 0;

            foreach (UnitRuntimeState ally in CombatRegistry.Units)
            {
                if (!IsEligibleFormationUnit(ally) ||
                    !TryGetNearestTravelPathProgress(ally.GridPosition.TileCoordinate, out int allyPathIndex, out float allyProgress, out _) ||
                    allyPathIndex != pathIndex)
                {
                    continue;
                }

                UnitDataSO data = ally.DataLink != null && ally.DataLink.HasData ? ally.DataLink.UnitData : null;

                if (data == null)
                {
                    continue;
                }

                bool pathBlocker = data.BlockCount > 0 &&
                                   boardRuntime.Board.TryGetTile(ally.GridPosition.TileCoordinate, out RaidTile unitTile) &&
                                   unitTile.IsPath;

                if (pathBlocker)
                {
                    if (Mathf.Abs(allyProgress - primaryProgress) <= PrimaryLineTolerance)
                    {
                        primaryBlockCapacity += Mathf.Max(0, data.BlockCount);
                        primaryBlockerCount++;
                    }
                    else if (allyProgress > primaryProgress && Mathf.Abs(allyProgress - secondaryProgress) <= SecondaryLineTolerance)
                    {
                        secondaryBlockCapacity += Mathf.Max(0, data.BlockCount);
                        secondaryBlockerCount++;
                    }
                }

                if (CanUnitSupportFormationTile(ally, primaryAnchor) || CanUnitSupportFormationTile(ally, secondaryAnchor))
                {
                    AttackSettings attack = data.AttackSettings;

                    if (attack != null && attack.AttackMode != AttackMode.None && attack.TargetCount > 0)
                    {
                        if (!pathBlocker || attack.AttackMode == AttackMode.Ranged)
                        {
                            supportCoverage++;
                        }
                    }
                }

                if (CanDeployedUnitCoverAirThreat(ally, pathIndex))
                {
                    airCoverage++;
                }
            }

            int overflow = Mathf.Max(0, projectedGround - primaryBlockCapacity);
            int desiredSupport = projectedGround > 0 && primaryBlockCapacity > 0 ? 1 : 0;

            if (projectedGround >= 4)
            {
                desiredSupport = 2;
            }

            if (projectedGround >= 7)
            {
                desiredSupport = 3;
            }

            int desiredAirCoverage = urgentAir <= 0 ? 0 : Mathf.Clamp(Mathf.CeilToInt(urgentAir * 0.5f), 1, 3);
            bool overflowUrgent = overflow > 0 && (maxGroundProgress >= primaryProgress - 0.13f || projectedGroundPressure >= primaryBlockCapacity + 1.5f);
            FormationIntent intent = FormationIntent.Wait;
            float priority = 0f;
            float targetProgress = primaryProgress;
            Vector2Int anchor = primaryAnchor;

            if (urgentAir > 0 && airCoverage < desiredAirCoverage &&
                (maxAirProgress >= maxGroundProgress + 0.08f || projectedGround == 0))
            {
                intent = FormationIntent.AirGuard;
                priority = 24f + maxAirProgress * 18f + (desiredAirCoverage - airCoverage) * 5f;
            }
            else if (projectedGround > 0 && primaryBlockCapacity <= 0)
            {
                intent = FormationIntent.PrimaryBlocker;
                priority = 32f + maxGroundProgress * 25f + projectedGroundPressure * 3f;
            }
            else if (projectedGround > 0 && supportCoverage < desiredSupport && !overflowUrgent)
            {
                intent = FormationIntent.Support;
                priority = 26f + maxGroundProgress * 17f + (desiredSupport - supportCoverage) * 5f;
            }
            else if (projectedGround > 0 && overflow > 0 && secondaryBlockCapacity < overflow)
            {
                intent = FormationIntent.SecondaryBlocker;
                anchor = secondaryAnchor;
                targetProgress = secondaryProgress;
                priority = 29f + maxGroundProgress * 24f + overflow * 4f;
            }
            else if (projectedGround > 0 && supportCoverage < desiredSupport)
            {
                intent = FormationIntent.Support;
                priority = 22f + maxGroundProgress * 16f + (desiredSupport - supportCoverage) * 4f;
            }
            else if (urgentAir > 0 && airCoverage < desiredAirCoverage)
            {
                intent = FormationIntent.AirGuard;
                priority = 21f + maxAirProgress * 16f + (desiredAirCoverage - airCoverage) * 4f;
            }
            else
            {
                intent = FormationIntent.Wait;
                priority = (liveGround + liveAir) * 0.1f + Mathf.Max(maxGroundProgress, maxAirProgress);
            }

            if (intent == FormationIntent.PrimaryBlocker &&
                primaryBlockerCount == 0 &&
                deployment.DeployedCount == 0 &&
                maxGroundProgress < 0.30f)
            {
                float vanguardProgress = battle.Config.AutoDeployVanguardLineProgress;

                if (vanguardProgress < primaryProgress && TryGetFormationAnchor(pathIndex, vanguardProgress, out Vector2Int vanguardAnchor))
                {
                    targetProgress = vanguardProgress;
                    anchor = vanguardAnchor;
                }
            }

            int recommendedFieldCount =
                primaryBlockerCount +
                secondaryBlockerCount +
                supportCoverage +
                airCoverage +
                (intent == FormationIntent.Wait ? 0 : 1);

            plan = new FormationPlan(
                intent,
                pathIndex,
                anchor,
                primaryAnchor,
                secondaryAnchor,
                primaryProgress,
                secondaryProgress,
                targetProgress,
                priority,
                maxGroundProgress,
                maxAirProgress,
                projectedGround,
                primaryBlockCapacity,
                secondaryBlockCapacity,
                supportCoverage,
                airCoverage,
                Mathf.Clamp(recommendedFieldCount, 0, battle.Config.MaxDeployedUnits));

            return true;
        }

        private bool IsUnitSuitableForIntent(UnitDataSO unitData, FormationIntent intent)
        {
            if (unitData == null)
            {
                return false;
            }

            AttackSettings attack = unitData.AttackSettings;

            switch (intent)
            {
                case FormationIntent.PrimaryBlocker:
                case FormationIntent.SecondaryBlocker:
                    return CanUseGroundPlacement(unitData) && unitData.BlockCount > 0;

                case FormationIntent.Support:
                    return attack != null &&
                           attack.AttackMode != AttackMode.None &&
                           attack.TargetCount > 0 &&
                           CanAttackLayer(attack, CombatTargetLayer.Ground);

                case FormationIntent.AirGuard:
                    return attack != null &&
                           attack.AttackMode != AttackMode.None &&
                           attack.TargetCount > 0 &&
                           CanAttackLayer(attack, CombatTargetLayer.Air);

                default:
                    return false;
            }
        }

        private bool IsTileSuitableForIntent(UnitDataSO unitData, Vector2Int tile, FormationPlan plan)
        {
            if (!TryGetNearestTravelPathProgress(tile, out int pathIndex, out float routeProgress, out float routeDistance))
            {
                return false;
            }

            RaidTile raidTile = boardRuntime.Board.GetTile(tile);

            switch (plan.Intent)
            {
                case FormationIntent.PrimaryBlocker:
                    return pathIndex == plan.PathIndex &&
                           raidTile.IsPath &&
                           raidTile.IsGroundCombatDeployable &&
                           Mathf.Abs(routeProgress - plan.TargetProgress) <= PrimaryLineTolerance;

                case FormationIntent.SecondaryBlocker:
                    return pathIndex == plan.PathIndex &&
                           raidTile.IsPath &&
                           raidTile.IsGroundCombatDeployable &&
                           routeProgress > plan.PrimaryProgress + 0.04f &&
                           Mathf.Abs(routeProgress - plan.TargetProgress) <= SecondaryLineTolerance;

                case FormationIntent.Support:
                    if (raidTile.IsPath && unitData.BlockCount <= 0)
                    {
                        return false;
                    }

                    return Manhattan(tile, plan.PrimaryAnchor) <= 7 &&
                           CanAttackFormationAnchor(unitData, tile, plan.PrimaryAnchor);

                case FormationIntent.AirGuard:
                    return Manhattan(tile, plan.PrimaryAnchor) <= 8 &&
                           CanCoverAnyAirEnemyFrom(unitData, tile, plan.PathIndex);

                default:
                    return false;
            }
        }

        private float ScoreFormationCandidate(UnitDataSO unitData, Vector2Int tile, GridFacingDirection facing, FormationPlan plan)
        {
            if (!TryGetNearestTravelPathProgress(tile, out int pathIndex, out float routeProgress, out _))
            {
                return float.NegativeInfinity;
            }

            AttackSettings attack = unitData.AttackSettings;
            float score = 0f;

            switch (plan.Intent)
            {
                case FormationIntent.PrimaryBlocker:
                {
                    if (pathIndex != plan.PathIndex || unitData.BlockCount <= 0)
                    {
                        return float.NegativeInfinity;
                    }

                    float lineFit = 1f - Mathf.Clamp01(Mathf.Abs(routeProgress - plan.TargetProgress) / PrimaryLineTolerance);
                    score += 42f * lineFit;
                    score += unitData.BlockCount * 5.5f;
                    score += GetBlockerSurvivalScore(unitData);

                    if (HasPassive<CostGainPassiveSO>(unitData) && plan.TargetProgress <= battle.Config.AutoDeployVanguardLineProgress + 0.05f)
                    {
                        score += 7f;
                    }

                    break;
                }

                case FormationIntent.SecondaryBlocker:
                {
                    if (pathIndex != plan.PathIndex || unitData.BlockCount <= 0)
                    {
                        return float.NegativeInfinity;
                    }

                    float lineFit = 1f - Mathf.Clamp01(Mathf.Abs(routeProgress - plan.TargetProgress) / SecondaryLineTolerance);
                    score += 40f * lineFit;
                    score += unitData.BlockCount * 5f;
                    score += GetBlockerSurvivalScore(unitData);

                    if (CanAttackFormationAnchor(unitData, tile, plan.PrimaryAnchor, facing))
                    {
                        score += 5f;
                    }

                    break;
                }

                case FormationIntent.Support:
                {
                    if (attack == null ||
                        attack.AttackMode == AttackMode.None ||
                        attack.TargetCount <= 0 ||
                        !CanAttackFormationAnchor(unitData, tile, plan.PrimaryAnchor, facing))
                    {
                        return float.NegativeInfinity;
                    }

                    int distance = Manhattan(tile, plan.PrimaryAnchor);
                    score += 36f;
                    score += Mathf.Max(0f, 8f - distance) * 1.4f;
                    score += attack.AttackMode == AttackMode.Ranged ? 6f : 1f;
                    score += Mathf.Max(0, attack.TargetCount - 1) * 2.2f;

                    if (boardRuntime.Board.GetTile(tile).IsHighGroundDeployable)
                    {
                        score += 5f;
                    }

                    if (CanAttackFormationAnchor(unitData, tile, plan.SecondaryAnchor, facing))
                    {
                        score += 6f;
                    }

                    score += GetSupportSynergyScore(unitData);
                    break;
                }

                case FormationIntent.AirGuard:
                {
                    if (attack == null ||
                        attack.AttackMode == AttackMode.None ||
                        !CanAttackLayer(attack, CombatTargetLayer.Air))
                    {
                        return float.NegativeInfinity;
                    }

                    int coveredAir = CountCoveredAirEnemies(unitData, tile, facing, plan.PathIndex);

                    if (coveredAir <= 0)
                    {
                        return float.NegativeInfinity;
                    }

                    score += 35f + coveredAir * 9f;
                    score += attack.AttackMode == AttackMode.Ranged ? 5f : 0f;

                    if (boardRuntime.Board.GetTile(tile).IsHighGroundDeployable)
                    {
                        score += 5f;
                    }

                    break;
                }
            }

            return score;
        }

        private float ScoreInfluenceBalance(
            UnitDataSO unitData,
            Vector2Int tile,
            GridFacingDirection facing,
            float candidateProgress)
        {
            if (!TryGetNearestTravelPathProgress(tile, out int candidatePathIndex, out _, out _))
            {
                return 0f;
            }

            float enemyInfluence = 0f;
            float allyInfluence = 0f;
            AttackSettings attack = unitData != null ? unitData.AttackSettings : null;
            bool canAttackGround = CanAttackLayer(attack, CombatTargetLayer.Ground);
            bool canAttackAir = CanAttackLayer(attack, CombatTargetLayer.Air);

            foreach (EnemyRuntimeState enemy in CombatRegistry.Enemies)
            {
                if (enemy == null ||
                    !enemy.IsInitialized ||
                    enemy.Health == null ||
                    enemy.Health.IsDead ||
                    enemy.GridPosition == null ||
                    !enemy.GridPosition.IsInitialized ||
                    enemy.Move == null ||
                    !TryGetNearestTravelPathProgress(
                        enemy.GridPosition.TileCoordinate,
                        out int enemyPathIndex,
                        out _,
                        out _) ||
                    enemyPathIndex != candidatePathIndex)
                {
                    continue;
                }

                float projectedProgress = ProjectEnemyProgress(enemy, 4.5f);
                float progressGap = Mathf.Abs(projectedProgress - candidateProgress);

                if (progressGap > InfluenceProgressRadius)
                {
                    continue;
                }

                bool air = enemy.GridPosition.TargetLayer == CombatTargetLayer.Air;
                float proximity = 1f - progressGap / InfluenceProgressRadius;
                float urgency = 0.7f + projectedProgress * 1.4f;
                float layerWeight =
                    air
                        ? (canAttackAir ? 1.35f : 0.35f)
                        : (canAttackGround ? 1f : 0.35f);

                float influence = proximity * urgency * layerWeight;

                if (CanAttackEnemyFrom(unitData, tile, facing, enemy))
                {
                    influence *= 1.2f;
                }

                enemyInfluence += influence;
            }

            foreach (UnitRuntimeState ally in CombatRegistry.Units)
            {
                if (!IsEligibleFormationUnit(ally) ||
                    !TryGetNearestTravelPathProgress(
                        ally.GridPosition.TileCoordinate,
                        out int allyPathIndex,
                        out float allyProgress,
                        out _) ||
                    allyPathIndex != candidatePathIndex)
                {
                    continue;
                }

                float progressGap = Mathf.Abs(allyProgress - candidateProgress);

                if (progressGap > InfluenceProgressRadius)
                {
                    continue;
                }

                UnitDataSO allyData =
                    ally.DataLink != null && ally.DataLink.HasData
                        ? ally.DataLink.UnitData
                        : null;

                if (allyData == null)
                {
                    continue;
                }

                float proximity = 1f - progressGap / InfluenceProgressRadius;
                float influence = proximity * 0.8f;

                if (allyData.BlockCount > 0 &&
                    boardRuntime.Board.TryGetTile(
                        ally.GridPosition.TileCoordinate,
                        out RaidTile allyTile) &&
                    allyTile.IsPath)
                {
                    influence += proximity * (0.9f + allyData.BlockCount * 0.35f);
                }

                if (CanUnitSupportFormationTile(ally, tile))
                {
                    influence += proximity * 1.1f;
                }

                allyInfluence += influence;
            }

            float deficit = enemyInfluence - allyInfluence * 0.85f;

            if (deficit > 0f)
            {
                return Mathf.Min(9f, deficit * 1.55f);
            }

            float saturation = allyInfluence - enemyInfluence;

            if (saturation > 0f)
            {
                return -Mathf.Min(14f, saturation * 1.8f);
            }

            return 0f;
        }

        private float EstimateSecondsUntilFormationThreat(FormationPlan plan)
        {
            float bestSeconds = float.PositiveInfinity;

            foreach (EnemyRuntimeState enemy in CombatRegistry.Enemies)
            {
                if (enemy == null ||
                    !enemy.IsInitialized ||
                    enemy.Health == null ||
                    enemy.Health.IsDead ||
                    enemy.Move == null ||
                    enemy.Stats == null ||
                    !enemy.Stats.IsInitialized ||
                    enemy.Stats.MoveSpeed <= 0f ||
                    enemy.Move.TotalPathDistance <= 0.001f ||
                    !TryGetNearestTravelPathProgress(
                        enemy.GridPosition.TileCoordinate,
                        out int enemyPathIndex,
                        out _,
                        out _) ||
                    enemyPathIndex != plan.PathIndex)
                {
                    continue;
                }

                float progress = Mathf.Clamp01(enemy.Move.PathProgress);

                if (progress >= plan.TargetProgress)
                {
                    return 0f;
                }

                float remainingProgress = plan.TargetProgress - progress;
                float remainingDistance = remainingProgress * enemy.Move.TotalPathDistance;
                float seconds = remainingDistance / enemy.Stats.MoveSpeed;

                if (seconds < bestSeconds)
                {
                    bestSeconds = seconds;
                }
            }

            return float.IsPositiveInfinity(bestSeconds) ? 999f : bestSeconds;
        }

        private bool ShouldReserveForBetterUnit(FormationPlan plan, DeploymentCandidate affordable, DeploymentCandidate overall)
        {
            if (affordable.Slot == null ||
                affordable.Slot.UnitData == null ||
                overall.Slot == null ||
                overall.Slot.UnitData == null ||
                overall.Slot.UnitData.SummonCost <= battle.CurrentCost ||
                battle.Config.CostRegenPerSecond <= 0f ||
                IsEmergencyPlan(plan))
            {
                return false;
            }

            int missingCost = overall.Slot.UnitData.SummonCost - battle.CurrentCost;
            float secondsToAfford = missingCost / battle.Config.CostRegenPerSecond;
            float secondsToThreat = EstimateSecondsUntilFormationThreat(plan);

            if (secondsToAfford > ReservationMaxWaitSeconds || secondsToAfford + ReservationReactionBuffer >= secondsToThreat)
            {
                return false;
            }

            int affordableCost = Mathf.Max(0, affordable.Slot.UnitData.SummonCost);
            int postAffordableCost = battle.CurrentCost - affordableCost;
            float requiredScoreGain = 2.0f + secondsToAfford * 0.12f;

            if (postAffordableCost < FormationReserveCost)
            {
                requiredScoreGain = Mathf.Max(1.25f, requiredScoreGain - 0.75f);
            }

            return overall.Score >= affordable.Score + requiredScoreGain;
        }

        private bool CanSafelyWaitForCandidate(FormationPlan plan, DeploymentCandidate candidate)
        {
            if (candidate.Slot == null || candidate.Slot.UnitData == null || battle.Config.CostRegenPerSecond <= 0f || IsEmergencyPlan(plan))
            {
                return false;
            }

            int missingCost = Mathf.Max(0, candidate.Slot.UnitData.SummonCost - battle.CurrentCost);
            float secondsToAfford = missingCost / battle.Config.CostRegenPerSecond;

            if (secondsToAfford > ReservationMaxWaitSeconds)
            {
                return false;
            }

            float secondsToThreat = EstimateSecondsUntilFormationThreat(plan);
            return secondsToAfford + ReservationReactionBuffer < secondsToThreat;
        }

        private bool IsEmergencyPlan(FormationPlan plan)
        {
            if (plan.MaxGroundProgress >= 0.88f || plan.MaxAirProgress >= 0.90f)
            {
                return true;
            }

            return EstimateSecondsUntilFormationThreat(plan) <= ReservationEmergencySeconds;
        }

        private bool ReservationMatches(FormationPlan plan)
        {
            return reservedSlot != null && reservedPathIndex == plan.PathIndex && reservedIntent == plan.Intent;
        }

        private void SetReservation(FormationPlan plan, DeploymentCandidate candidate)
        {
            reservedSlot = candidate.Slot;
            reservedPathIndex = plan.PathIndex;
            reservedIntent = plan.Intent;
        }

        private void ClearReservation()
        {
            reservedSlot = null;
            reservedPathIndex = -1;
            reservedIntent = FormationIntent.Wait;
        }

        private float ProjectEnemyProgress(EnemyRuntimeState enemy, float forecastSeconds)
        {
            if (enemy == null || enemy.Move == null)
            {
                return 0f;
            }

            float progress = Mathf.Clamp01(enemy.Move.PathProgress);

            if (enemy.Move.TotalPathDistance <= 0.001f ||
                enemy.Stats == null ||
                !enemy.Stats.IsInitialized ||
                enemy.Stats.MoveSpeed <= 0f)
            {
                return progress;
            }

            float projectedDistance = enemy.Stats.MoveSpeed * Mathf.Max(0f, forecastSeconds);
            return Mathf.Clamp01(progress + projectedDistance / enemy.Move.TotalPathDistance);
        }

        private bool TryGetFormationAnchor(int pathIndex, float progress, out Vector2Int tile)
        {
            tile = default;

            if (boardRuntime == null ||
                boardRuntime.Board == null ||
                boardRuntime.TravelPaths == null ||
                pathIndex < 0 ||
                pathIndex >= boardRuntime.TravelPaths.Count)
            {
                return false;
            }

            RaidTravelPath path = boardRuntime.TravelPaths[pathIndex];

            if (path == null || path.PointCount <= 0)
            {
                return false;
            }

            int pointIndex = Mathf.Clamp(
                Mathf.RoundToInt(Mathf.Clamp01(progress) * (path.PointCount - 1)),
                0,
                path.PointCount - 1);

            return boardRuntime.Board.TryWorldToTile(path.GetPoint(pointIndex), out tile);
        }

        private bool TryGetNearestTravelPathProgress(Vector2Int tile, out int pathIndex, out float progress, out float distance)
        {
            pathIndex = -1;
            progress = 0f;
            distance = float.PositiveInfinity;

            if (boardRuntime == null ||
                boardRuntime.Board == null ||
                boardRuntime.TravelPaths == null ||
                boardRuntime.TravelPaths.Count == 0)
            {
                return false;
            }

            Vector3 world = boardRuntime.Board.TileToWorld(tile);
            float bestDistanceSqr = float.PositiveInfinity;

            for (int candidatePathIndex = 0; candidatePathIndex < boardRuntime.TravelPaths.Count; candidatePathIndex++)
            {
                RaidTravelPath path = boardRuntime.TravelPaths[candidatePathIndex];

                if (path == null || path.PointCount < 2)
                {
                    continue;
                }

                for (int pointIndex = 0; pointIndex < path.PointCount; pointIndex++)
                {
                    Vector3 point = path.GetPoint(pointIndex);
                    float dx = world.x - point.x;
                    float dz = world.z - point.z;
                    float distanceSqr = dx * dx + dz * dz;

                    if (distanceSqr >= bestDistanceSqr)
                    {
                        continue;
                    }

                    bestDistanceSqr = distanceSqr;
                    pathIndex = candidatePathIndex;
                    progress = pointIndex / (float)(path.PointCount - 1);
                }
            }

            if (pathIndex < 0)
            {
                return false;
            }

            distance = Mathf.Sqrt(bestDistanceSqr);
            return true;
        }

        private bool CanUnitSupportFormationTile(UnitRuntimeState unit, Vector2Int target)
        {
            if (!IsEligibleFormationUnit(unit) || unit.DataLink == null || !unit.DataLink.HasData)
            {
                return false;
            }

            UnitDataSO unitData = unit.DataLink.UnitData;
            AttackSettings attack = unitData.AttackSettings;

            if (attack != null && attack.RangeRotationMode == AttackRangeRotationMode.FollowFacing)
            {
                return CanAttackFormationAnchor(unitData, unit.GridPosition.TileCoordinate, target);
            }

            return CanAttackFormationAnchor(
                unitData,
                unit.GridPosition.TileCoordinate,
                target,
                unit.GridPosition.FacingDirection);
        }

        private bool CanAttackFormationAnchor(UnitDataSO unitData, Vector2Int origin, Vector2Int target)
        {
            for (int facingIndex = 0; facingIndex < 4; facingIndex++)
            {
                if (CanAttackFormationAnchor(unitData, origin, target, (GridFacingDirection)facingIndex))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool CanAttackFormationAnchor(UnitDataSO unitData, Vector2Int origin, Vector2Int target, GridFacingDirection facing)
        {
            if (unitData == null)
            {
                return false;
            }

            AttackSettings attack = unitData.AttackSettings;

            if (attack == null ||
                attack.AttackMode == AttackMode.None ||
                attack.TargetCount <= 0 ||
                attack.BasicAttackRange == null ||
                !BasicAttackRangeEvaluator.CanAttackTargetLayer(attack.AttackTarget, CombatTargetLayer.Ground))
            {
                return false;
            }

            Vector2Int relative = target - origin;
            Vector2Int pattern = BasicAttackRangeEvaluator.ConvertWorldTileToPatternTile(
                relative,
                attack.RangeRotationMode,
                facing);

            return attack.BasicAttackRange.Contains(pattern);
        }

        private bool CanDeployedUnitCoverAirThreat(UnitRuntimeState unit, int pathIndex)
        {
            if (!IsEligibleFormationUnit(unit) ||
                unit.DataLink == null ||
                !unit.DataLink.HasData ||
                !CanAttackLayer(unit.DataLink.UnitData.AttackSettings, CombatTargetLayer.Air))
            {
                return false;
            }

            foreach (EnemyRuntimeState enemy in CombatRegistry.Enemies)
            {
                if (enemy == null ||
                    !enemy.IsInitialized ||
                    enemy.Health == null ||
                    enemy.Health.IsDead ||
                    enemy.GridPosition == null ||
                    enemy.GridPosition.TargetLayer != CombatTargetLayer.Air ||
                    !TryGetNearestTravelPathProgress(enemy.GridPosition.TileCoordinate, out int enemyPathIndex, out _, out _) ||
                    enemyPathIndex != pathIndex)
                {
                    continue;
                }

                if (CanAttackEnemyFrom(
                    unit.DataLink.UnitData,
                    unit.GridPosition.TileCoordinate,
                    unit.GridPosition.FacingDirection,
                    enemy))
                {
                    return true;
                }
            }

            return false;
        }

        private bool CanCoverAnyAirEnemyFrom(UnitDataSO unitData, Vector2Int origin, int pathIndex)
        {
            if (unitData == null ||
                !CanAttackLayer(unitData.AttackSettings, CombatTargetLayer.Air))
            {
                return false;
            }

            for (int facingIndex = 0; facingIndex < 4; facingIndex++)
            {
                if (CountCoveredAirEnemies(unitData, origin, (GridFacingDirection)facingIndex, pathIndex) > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private int CountCoveredAirEnemies(UnitDataSO unitData, Vector2Int origin, GridFacingDirection facing, int pathIndex)
        {
            int count = 0;

            foreach (EnemyRuntimeState enemy in CombatRegistry.Enemies)
            {
                if (enemy == null ||
                    !enemy.IsInitialized ||
                    enemy.Health == null ||
                    enemy.Health.IsDead ||
                    enemy.GridPosition == null ||
                    enemy.GridPosition.TargetLayer != CombatTargetLayer.Air ||
                    !TryGetNearestTravelPathProgress(enemy.GridPosition.TileCoordinate, out int enemyPathIndex, out _, out _) ||
                    enemyPathIndex != pathIndex)
                {
                    continue;
                }

                if (CanAttackEnemyFrom(unitData, origin, facing, enemy))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool IsEligibleFormationUnit(UnitRuntimeState unit)
        {
            return unit != null &&
                   !unit.IsSummon &&
                   unit.IsInitialized &&
                   unit.Health != null &&
                   !unit.Health.IsDead &&
                   unit.GridPosition != null &&
                   unit.GridPosition.IsInitialized;
        }

        private static float GetBlockerSurvivalScore(UnitDataSO unitData)
        {
            if (unitData == null || unitData.BaseStats == null)
            {
                return 0f;
            }

            CombatStats stats = unitData.BaseStats;
            float hp = Mathf.Log10(1f + Mathf.Max(0f, stats.MaxHp)) * 2.1f;
            float defense = Mathf.Log10(1f + Mathf.Max(0f, stats.PhysicalDefense) + Mathf.Max(0f, stats.MagicalDefense)) * 1.3f;
            return hp + defense;
        }

        private static float GetSupportSynergyScore(UnitDataSO unitData)
        {
            if (unitData == null || unitData.Passives == null)
            {
                return 0f;
            }

            float score = 0f;

            for (int i = 0; i < unitData.Passives.Count; i++)
            {
                PassiveDataSO passive = unitData.Passives[i];

                if (passive is AllyAidSO ||
                    passive is DefenseAuraSO ||
                    passive is CommandSO ||
                    passive is CritBuffSO ||
                    passive is HealSO ||
                    passive is FrontlineCommandSO)
                {
                    score += 2.2f;
                }
            }

            return Mathf.Min(6f, score);
        }

        private readonly struct FormationPlan
        {
            public FormationIntent Intent { get; }
            public int PathIndex { get; }
            public Vector2Int Anchor { get; }
            public Vector2Int PrimaryAnchor { get; }
            public Vector2Int SecondaryAnchor { get; }
            public float PrimaryProgress { get; }
            public float SecondaryProgress { get; }
            public float TargetProgress { get; }
            public float Priority { get; }
            public float MaxGroundProgress { get; }
            public float MaxAirProgress { get; }
            public int ProjectedGround { get; }
            public int PrimaryBlockCapacity { get; }
            public int SecondaryBlockCapacity { get; }
            public int SupportCoverage { get; }
            public int AirCoverage { get; }
            public int RecommendedFieldCount { get; }

            public FormationPlan(
                FormationIntent intent,
                int pathIndex,
                Vector2Int anchor,
                Vector2Int primaryAnchor,
                Vector2Int secondaryAnchor,
                float primaryProgress,
                float secondaryProgress,
                float targetProgress,
                float priority,
                float maxGroundProgress,
                float maxAirProgress,
                int projectedGround,
                int primaryBlockCapacity,
                int secondaryBlockCapacity,
                int supportCoverage,
                int airCoverage,
                int recommendedFieldCount)
            {
                Intent = intent;
                PathIndex = pathIndex;
                Anchor = anchor;
                PrimaryAnchor = primaryAnchor;
                SecondaryAnchor = secondaryAnchor;
                PrimaryProgress = primaryProgress;
                SecondaryProgress = secondaryProgress;
                TargetProgress = targetProgress;
                Priority = priority;
                MaxGroundProgress = maxGroundProgress;
                MaxAirProgress = maxAirProgress;
                ProjectedGround = projectedGround;
                PrimaryBlockCapacity = primaryBlockCapacity;
                SecondaryBlockCapacity = secondaryBlockCapacity;
                SupportCoverage = supportCoverage;
                AirCoverage = airCoverage;
                RecommendedFieldCount = recommendedFieldCount;
            }
        }

        private enum FormationIntent
        {
            Wait = 0,
            PrimaryBlocker = 1,
            SecondaryBlocker = 2,
            Support = 3,
            AirGuard = 4
        }

        private float ScoreDefenseLinePosition(UnitDataSO unitData, bool groundLaneTile, bool highGroundTile, float routeProgress, float routeDistance)
        {
            if (battle == null || battle.Config == null || unitData == null)
            {
                return 0f;
            }

            float maxRelevantDistance = boardRuntime.Board.TileSize * 4f;
            float distanceFit = 1f - Mathf.Clamp01(routeDistance / Mathf.Max(0.01f, maxRelevantDistance));
            float score = distanceFit * (groundLaneTile && unitData.BlockCount > 0 ? 2.2f : highGroundTile ? 1.8f : 1.1f);
            float frontLimit = battle.Config.AutoDeployFrontPenaltyProgress;

            if (routeProgress < frontLimit)
            {
                float frontSeverity = 1f - Mathf.Clamp01(routeProgress / Mathf.Max(0.01f, frontLimit));
                float penalty = groundLaneTile && unitData.BlockCount > 0 ? 9f : 5f;
                score -= penalty * (0.35f + frontSeverity * 0.65f);
            }

            if (routeProgress > 0.90f)
            {
                float goalSeverity = Mathf.Clamp01((routeProgress - 0.90f) / 0.10f);
                score -= (groundLaneTile && unitData.BlockCount > 0 ? 6f : 3f) * goalSeverity;
            }

            return score;
        }

        private bool TryGetNearestTravelPathProgress(Vector2Int tile, out float progress, out float distance)
        {
            return TryGetNearestTravelPathProgress(tile, out _, out progress, out distance);
        }

        private static bool HasPassive<T>(UnitDataSO unitData) where T : PassiveDataSO
        {
            if (unitData == null || unitData.Passives == null)
            {
                return false;
            }

            for (int i = 0; i < unitData.Passives.Count; i++)
            {
                if (unitData.Passives[i] is T)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasSummonSpace(PassiveDataSO passive, Vector2Int ownerTile)
        {
            if (passive == null || boardRuntime.Board == null)
            {
                return false;
            }

            UnitPlacement summonPlacement = UnitPlacement.GroundAndHighGround;
            if (passive.TryGetDefaultReference(PassiveRefKey.SummonPrefab, out UnityEngine.Object reference) && reference is GameObject prefab)
            {
                UnitDataLink dataLink = prefab.GetComponent<UnitDataLink>();
                if (dataLink != null && dataLink.HasData && dataLink.UnitData != null)
                {
                    summonPlacement = dataLink.UnitData.Placement;
                }
            }

            RaidBoard board = boardRuntime.Board;
            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    if (x == 0 && y == 0)
                    {
                        continue;
                    }

                    Vector2Int candidate = ownerTile + new Vector2Int(x, y);
                    if (!board.TryGetTile(candidate, out RaidTile raidTile) || deployment.IsTileOccupied(candidate))
                    {
                        continue;
                    }

                    bool groundAllowed = summonPlacement == UnitPlacement.Ground || summonPlacement == UnitPlacement.GroundAndHighGround;
                    bool highAllowed = summonPlacement == UnitPlacement.HighGround || summonPlacement == UnitPlacement.GroundAndHighGround;
                    if ((groundAllowed && raidTile.IsGroundCombatDeployable) || (highAllowed && raidTile.IsHighGroundDeployable))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private int CountPathCoverage(UnitDataSO unitData, Vector2Int origin, GridFacingDirection facing)
        {
            AttackSettings attack = unitData.AttackSettings;
            if (attack == null || attack.BasicAttackRange == null)
            {
                return 0;
            }

            int count = 0;
            var attackTiles = attack.BasicAttackRange.AttackTiles;
            RaidBoard board = boardRuntime.Board;

            for (int i = 0; i < attackTiles.Count; i++)
            {
                Vector2Int offset = attack.RangeRotationMode == AttackRangeRotationMode.Fixed
                    ? attackTiles[i]
                    : PatternToWorld(attackTiles[i], facing);
                Vector2Int worldTile = origin + offset;

                if (board.TryGetTile(worldTile, out RaidTile tile) && tile.IsPath)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool CanAttackLayer(AttackSettings attack, CombatTargetLayer layer)
        {
            return attack != null &&
                   attack.AttackMode != AttackMode.None &&
                   attack.TargetCount > 0 &&
                   BasicAttackRangeEvaluator.CanAttackTargetLayer(attack.AttackTarget, layer);
        }

        private static bool CanUseGroundPlacement(UnitDataSO unitData)
        {
            return unitData != null &&
                   (unitData.Placement == UnitPlacement.Ground || unitData.Placement == UnitPlacement.GroundAndHighGround);
        }

        private static bool CanAttackEnemyFrom(UnitDataSO unitData, Vector2Int origin, GridFacingDirection facing, EnemyRuntimeState enemy)
        {
            AttackSettings attack = unitData.AttackSettings;
            if (attack == null || attack.AttackMode == AttackMode.None || attack.TargetCount <= 0 || attack.BasicAttackRange == null || enemy == null || enemy.GridPosition == null)
            {
                return false;
            }

            if (!BasicAttackRangeEvaluator.CanAttackTargetLayer(attack.AttackTarget, enemy.GridPosition.TargetLayer))
            {
                return false;
            }

            Vector2Int relative = enemy.GridPosition.TileCoordinate - origin;
            Vector2Int pattern = BasicAttackRangeEvaluator.ConvertWorldTileToPatternTile(relative, attack.RangeRotationMode, facing);
            return attack.BasicAttackRange.Contains(pattern);
        }

        private bool ResolveDependencies()
        {
            if (battle == null)
            {
                battle = GetComponent<RaidBattleController>();
            }

            if (boardRuntime == null)
            {
                boardRuntime = GetComponent<RaidBoardRuntime>();
            }

            if (roster == null)
            {
                roster = GetComponent<RaidRosterRuntime>();
            }

            if (deployment == null)
            {
                deployment = GetComponent<RaidDeploymentRuntime>();
            }

            if (itemRuntime == null)
            {
                itemRuntime = GetComponent<RaidItemRuntime>();
            }

            return battle != null && boardRuntime != null && roster != null && deployment != null && battle.Config != null;
        }

        private static Vector2Int PatternToWorld(Vector2Int pattern, GridFacingDirection facing)
        {
            switch (facing)
            {
                case GridFacingDirection.East:
                    return new Vector2Int(pattern.y, -pattern.x);
                case GridFacingDirection.South:
                    return new Vector2Int(-pattern.x, -pattern.y);
                case GridFacingDirection.West:
                    return new Vector2Int(-pattern.y, pattern.x);
                default:
                    return pattern;
            }
        }

        private static int Manhattan(Vector2Int a, Vector2Int b)
        {
            Vector2Int delta = a - b;
            return Mathf.Abs(delta.x) + Mathf.Abs(delta.y);
        }

        private static float StableTieBreaker(string unitId, Vector2Int tile, GridFacingDirection facing)
        {
            unchecked
            {
                int hash = 17;
                if (!string.IsNullOrEmpty(unitId))
                {
                    for (int i = 0; i < unitId.Length; i++)
                    {
                        hash = hash * 31 + unitId[i];
                    }
                }

                hash = hash * 31 + tile.x;
                hash = hash * 31 + tile.y;
                hash = hash * 31 + (int)facing;
                return (hash & 1023) * 0.00001f;
            }
        }

        private readonly struct DeploymentCandidate
        {
            public RaidRosterSlotState Slot { get; }
            public Vector2Int Tile { get; }
            public GridFacingDirection Facing { get; }
            public float Score { get; }

            public DeploymentCandidate(RaidRosterSlotState slot, Vector2Int tile, GridFacingDirection facing, float score)
            {
                Slot = slot;
                Tile = tile;
                Facing = facing;
                Score = score;
            }
        }
    }
}
