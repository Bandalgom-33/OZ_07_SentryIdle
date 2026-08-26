using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Raid.Data;
using EndlessGuard.Unit.Runtime;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RaidBattleController))]
    [RequireComponent(typeof(RaidBoardRuntime))]
    public sealed class RaidSummonTileProvider : MonoBehaviour, ISummonTileProvider
    {
        private const int CritSummonFallbackRadius = 3;
        private readonly List<SummonCandidate> candidates = new List<SummonCandidate>(16);
        private RaidBattleController battle;
        private RaidBoardRuntime boardRuntime;
        private RaidItemRuntime itemRuntime;

        public static RaidSummonTileProvider EnsureInstalled(GameObject host)
        {
            if (host == null)
            {
                return null;
            }

            RaidSummonTileProvider provider = host.GetComponent<RaidSummonTileProvider>();

            if (provider == null)
            {
                provider = host.AddComponent<RaidSummonTileProvider>();
            }

            provider.ResolveDependencies();
            provider.EnsureRegistered();
            return provider;
        }

        private void Awake()
        {
            battle = GetComponent<RaidBattleController>();
            boardRuntime = GetComponent<RaidBoardRuntime>();
            itemRuntime = GetComponent<RaidItemRuntime>();
        }

        private void OnEnable()
        {
            ResolveDependencies();

            if (battle != null)
            {
                battle.OnRaidStarted += HandleRaidStarted;
            }

            EnsureRegistered();
        }

        private void OnDisable()
        {
            if (battle != null)
            {
                battle.OnRaidStarted -= HandleRaidStarted;
            }

            SummonTileService.Unregister(this);
            candidates.Clear();
        }

        public void EnsureRegistered()
        {
            SummonTileService.Register(this);
        }

        private void HandleRaidStarted()
        {
            ResolveDependencies();
            EnsureRegistered();
        }

        public bool TryGetTile(SummonTileRequest request, out SummonTile tile)
        {
            tile = default;

            if (!ResolveDependencies() || battle.State != RaidBattleState.Running || battle.IsTransitioning || request.Owner == null || request.SummonData == null || request.Owner.GridPosition == null || !request.Owner.GridPosition.IsInitialized)
            {
                return false;
            }

            RaidBoard board = boardRuntime.Board;
            if (board == null)
            {
                return false;
            }

            Vector2Int ownerTile = request.Owner.GridPosition.TileCoordinate;
            if (!board.TryGetTile(ownerTile, out RaidTile ownerRaidTile))
            {
                return false;
            }

            bool ownerOnHighGround = ownerRaidTile.IsHighGroundDeployable;
            bool ownerOnGround = ownerRaidTile.IsGroundCombatDeployable;
            int requestedRadius = Mathf.Max(1, request.Radius);
            int maxRadius = request.Source is CritSummonSO ? Mathf.Max(requestedRadius, CritSummonFallbackRadius) : requestedRadius;
            candidates.Clear();

            for (int searchRadius = requestedRadius; searchRadius <= maxRadius; searchRadius++)
            {
                CollectCandidates(request, board, ownerTile, ownerOnGround, ownerOnHighGround, searchRadius);

                if (candidates.Count > 0)
                {
                    break;
                }
            }

            if (candidates.Count == 0)
            {
                return false;
            }

            int preferredCount = 0;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].MatchesOwnerSurface)
                {
                    preferredCount++;
                }
            }

            int selectedIndex;
            if (preferredCount > 0)
            {
                int preferredPick = Random.Range(0, preferredCount);
                selectedIndex = 0;

                for (int i = 0; i < candidates.Count; i++)
                {
                    if (!candidates[i].MatchesOwnerSurface)
                    {
                        continue;
                    }

                    if (preferredPick-- == 0)
                    {
                        selectedIndex = i;
                        break;
                    }
                }
            }
            else
            {
                selectedIndex = Random.Range(0, candidates.Count);
            }

            SummonCandidate selected = candidates[selectedIndex];
            tile = new SummonTile(selected.WorldPosition, selected.Coordinate);
            return true;
        }

        private void CollectCandidates(SummonTileRequest request, RaidBoard board, Vector2Int ownerTile, bool ownerOnGround, bool ownerOnHighGround, int radius)
        {
            candidates.Clear();

            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (x == 0 && y == 0)
                    {
                        continue;
                    }

                    if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) > radius)
                    {
                        continue;
                    }

                    Vector2Int coordinate = ownerTile + new Vector2Int(x, y);
                    if (!board.TryGetTile(coordinate, out RaidTile raidTile))
                    {
                        continue;
                    }

                    if (!IsPlacementAllowed(request.SummonData.Placement, raidTile))
                    {
                        continue;
                    }

                    if (HasLiveUnitAt(coordinate) || HasLiveEnemyAt(coordinate) || itemRuntime != null && itemRuntime.HasItemAt(coordinate))
                    {
                        continue;
                    }

                    bool candidateHigh = raidTile.IsHighGroundDeployable;
                    bool candidateGround = raidTile.IsGroundCombatDeployable;
                    bool sameSurface = ownerOnHighGround ? candidateHigh : ownerOnGround && candidateGround;
                    float height = candidateHigh ? battle.Config.HighGroundDeployHeight : battle.Config.GroundDeployHeight;
                    candidates.Add(new SummonCandidate(coordinate, board.TileToWorld(coordinate, height), sameSurface));
                }
            }
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

            if (itemRuntime == null)
            {
                itemRuntime = GetComponent<RaidItemRuntime>();
            }

            return battle != null && battle.Config != null && boardRuntime != null;
        }

        private static bool IsPlacementAllowed(UnitPlacement placement, RaidTile tile)
        {
            bool groundAllowed = placement == UnitPlacement.Ground || placement == UnitPlacement.GroundAndHighGround;
            bool highAllowed = placement == UnitPlacement.HighGround || placement == UnitPlacement.GroundAndHighGround;
            return groundAllowed && tile.IsGroundCombatDeployable || highAllowed && tile.IsHighGroundDeployable;
        }

        private static bool HasLiveUnitAt(Vector2Int coordinate)
        {
            if (!CombatRegistry.TryGetUnitsAt(coordinate, out HashSet<UnitRuntimeState> units))
            {
                return false;
            }

            foreach (UnitRuntimeState unit in units)
            {
                if (unit != null &&
                    unit.gameObject.activeInHierarchy &&
                    unit.Health != null &&
                    !unit.Health.IsDead)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasLiveEnemyAt(Vector2Int coordinate)
        {
            if (!CombatRegistry.TryGetEnemiesAt(coordinate, out HashSet<EnemyRuntimeState> enemies))
            {
                return false;
            }

            foreach (EnemyRuntimeState enemy in enemies)
            {
                if (enemy != null && enemy.gameObject.activeInHierarchy && enemy.Health != null && !enemy.Health.IsDead)
                {
                    return true;
                }
            }

            return false;
        }

        private readonly struct SummonCandidate
        {
            public Vector2Int Coordinate { get; }
            public Vector3 WorldPosition { get; }
            public bool MatchesOwnerSurface { get; }

            public SummonCandidate(Vector2Int coordinate, Vector3 worldPosition, bool matchesOwnerSurface)
            {
                Coordinate = coordinate;
                WorldPosition = worldPosition;
                MatchesOwnerSurface = matchesOwnerSurface;
            }
        }
    }
}
