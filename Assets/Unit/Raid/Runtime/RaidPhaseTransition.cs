using System;
using System.Collections;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Raid.Data;
using EndlessGuard.Unit.Runtime;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    public readonly struct RaidPhaseTransitionInfo
    {
        public RaidPhase FromPhase { get; }
        public RaidPhase ToPhase { get; }
        public int CollapsingTileCount { get; }
        public float Duration { get; }

        internal RaidPhaseTransitionInfo(RaidPhase fromPhase, RaidPhase toPhase, int collapsingTileCount, float duration)
        {
            FromPhase = fromPhase;
            ToPhase = toPhase;
            CollapsingTileCount = collapsingTileCount;
            Duration = duration;
        }
    }

    public readonly struct RaidForcedRetreatInfo
    {
        public UnitRuntimeState Unit { get; }
        public string UnitId { get; }
        public Vector2Int Tile { get; }
        public int RefundCost { get; }
        public bool IsSummon { get; }

        internal RaidForcedRetreatInfo(UnitRuntimeState unit, Vector2Int tile)
        {
            Unit = unit != null ? unit : throw new ArgumentNullException(nameof(unit));
            UnitId = unit.UnitId;
            Tile = tile;
            IsSummon = unit.IsSummon;
            RefundCost = !unit.IsSummon && unit.DataLink != null && unit.DataLink.HasData ? unit.DataLink.UnitData.SummonCost : 0;
        }
    }

    public enum RaidEnemyPhaseRemovalReason
    {
        CollapsingTile = 0,
        RepathFailed = 1
    }

    public readonly struct RaidEnemyPhaseRemovalInfo
    {
        public EnemyRuntimeState Enemy { get; }
        public string EnemyId { get; }
        public Vector2Int Tile { get; }
        public RaidEnemyPhaseRemovalReason Reason { get; }

        internal RaidEnemyPhaseRemovalInfo(EnemyRuntimeState enemy, Vector2Int tile, RaidEnemyPhaseRemovalReason reason)
        {
            Enemy = enemy != null ? enemy : throw new ArgumentNullException(nameof(enemy));
            EnemyId = enemy.EnemyId;
            Tile = tile;
            Reason = reason;
        }
    }

    internal sealed class RaidPhaseTransitionPlan
    {
        private readonly bool[] collapsingTiles;
        private readonly RaidCollapseCluster[] clusters;

        public RaidPhase FromPhase { get; }
        public RaidPhase ToPhase { get; }
        public RaidMapSO SourceMap { get; }
        public RaidMapSO TargetMap { get; }
        public int CollapsingTileCount { get; }
        public IReadOnlyList<RaidCollapseCluster> Clusters => clusters;

        private RaidPhaseTransitionPlan(RaidPhase fromPhase, RaidPhase toPhase, RaidMapSO sourceMap, RaidMapSO targetMap, bool[] collapsingTiles, int collapsingTileCount)
        {
            FromPhase = fromPhase;
            ToPhase = toPhase;
            SourceMap = sourceMap;
            TargetMap = targetMap;
            this.collapsingTiles = collapsingTiles;
            clusters = RaidCollapseClusterBuilder.Build(collapsingTiles, sourceMap.Width, sourceMap.Height);
            CollapsingTileCount = collapsingTileCount;
        }

        public bool IsCollapsing(Vector2Int coordinate)
        {
            if (coordinate.x < 0 || coordinate.y < 0 || coordinate.x >= SourceMap.Width || coordinate.y >= SourceMap.Height)
            {
                return false;
            }

            return collapsingTiles[coordinate.y * SourceMap.Width + coordinate.x];
        }

        public static bool TryCreate(RaidBoardRuntime boardRuntime, RaidPhase targetPhase, out RaidPhaseTransitionPlan plan, out string error)
        {
            plan = null;
            error = string.Empty;

            if (boardRuntime == null || boardRuntime.Board == null || boardRuntime.CurrentMapData == null)
            {
                error = "Raid Board가 준비되지 않아 Phase 전환 계획을 만들 수 없습니다.";
                return false;
            }

            RaidPhase expectedPhase;

            switch (boardRuntime.Phase)
            {
                case RaidPhase.Phase1:
                    expectedPhase = RaidPhase.Phase2;
                    break;
                case RaidPhase.Phase2:
                    expectedPhase = RaidPhase.Phase3;
                    break;
                default:
                    error = $"현재 Phase에서는 다음 Phase 전환이 없습니다. Phase: {boardRuntime.Phase}";
                    return false;
            }

            if (targetPhase != expectedPhase)
            {
                error = $"Raid Phase는 순서대로만 전환할 수 있습니다. Current: {boardRuntime.Phase}, Requested: {targetPhase}, Expected: {expectedPhase}";
                return false;
            }

            if (!boardRuntime.TryGetMapData(targetPhase, out RaidMapSO targetMap) || targetMap == null)
            {
                error = $"선택된 Map Family에 전환 대상 Map이 없습니다. Phase: {targetPhase}";
                return false;
            }

            RaidMapSO sourceMap = boardRuntime.CurrentMapData;

            if (sourceMap.Width != targetMap.Width || sourceMap.Height != targetMap.Height || sourceMap.TileCount != targetMap.TileCount)
            {
                error = $"같은 Map Family의 Phase Board 크기가 다릅니다. Source: {sourceMap.Width}x{sourceMap.Height}, Target: {targetMap.Width}x{targetMap.Height}";
                return false;
            }

            bool[] collapsing = new bool[sourceMap.TileCount];
            int collapsingCount = 0;

            for (int i = 0; i < sourceMap.TileCount; i++)
            {
                RaidTile sourceTile = sourceMap.GetTile(i);
                RaidTile targetTile = targetMap.GetTile(i);
                bool sourceExists = sourceTile.Surface != RaidTileSurface.Void;
                bool targetExists = targetTile.Surface != RaidTileSurface.Void;

                if (!sourceExists && targetExists)
                {
                    int x = i % sourceMap.Width;
                    int y = i / sourceMap.Width;
                    error = $"파괴 Phase 전환 중 새 Surface가 생성됩니다. Tile: ({x}, {y}), Source: {sourceTile.Surface}, Target: {targetTile.Surface}";
                    return false;
                }

                if (sourceExists && !targetExists)
                {
                    collapsing[i] = true;
                    collapsingCount++;
                }
            }

            if (collapsingCount == 0)
            {
                error = $"Phase 전환에 실제로 붕괴되는 Surface가 없습니다. {sourceMap.Phase} -> {targetMap.Phase}";
                return false;
            }

            plan = new RaidPhaseTransitionPlan(sourceMap.Phase, targetMap.Phase, sourceMap, targetMap, collapsing, collapsingCount);
            return true;
        }
    }

    internal sealed class RaidPhaseTransitionRuntime
    {
        private readonly RaidPhaseActorState actors = new RaidPhaseActorState();
        private readonly RaidCollapseView collapseView;
        private readonly RaidCrackScarView scarView;
        private readonly Camera camera;
        private readonly Transform cameraTransform;
        private readonly Transform battlefieldTransform;
        private Vector3 cameraBasePosition;
        private Quaternion cameraBaseRotation;
        private float cameraBaseFieldOfView;
        private Vector3 battlefieldBasePosition;
        private Quaternion battlefieldBaseRotation;
        private bool cameraCaptured;
        private bool battlefieldCaptured;

        public bool LastCommitSucceeded { get; private set; }

        public RaidPhaseTransitionRuntime(RaidBoardRuntime boardRuntime, Camera camera)
        {
            if (boardRuntime == null)
            {
                throw new ArgumentNullException(nameof(boardRuntime));
            }

            collapseView = new RaidCollapseView(boardRuntime);
            scarView = new RaidCrackScarView(boardRuntime);
            this.camera = camera;
            cameraTransform = camera != null ? camera.transform : null;
            battlefieldTransform = boardRuntime.BoardView != null ? boardRuntime.BoardView.transform : null;
        }

        public void CaptureActors(RaidPhaseTransitionPlan plan)
        {
            actors.Capture(plan);
        }

        public void CommitActors(RaidPhaseTransitionPlan plan, RaidBoard targetBoard, Action<RaidForcedRetreatInfo> onForcedRetreat, Action<RaidEnemyPhaseRemovalInfo> onEnemyRemoved)
        {
            actors.Commit(plan, targetBoard, onForcedRetreat, onEnemyRemoved);
        }

        public IEnumerator Play(RaidPhaseTransitionPlan plan, RaidBoard sourceBoard, float duration, float commitTime, float impactShake, float rumbleShake, float effectScale, Func<bool> commit)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            if (sourceBoard == null)
            {
                throw new ArgumentNullException(nameof(sourceBoard));
            }

            if (commit == null)
            {
                throw new ArgumentNullException(nameof(commit));
            }

            duration = Mathf.Max(0.5f, duration);
            commitTime = Mathf.Clamp(commitTime, 0.1f, duration - 0.05f);
            impactShake = Mathf.Max(0f, impactShake);
            rumbleShake = Mathf.Max(0f, rumbleShake);
            effectScale = Mathf.Clamp(effectScale, 0.5f, 2f);
            LastCommitSucceeded = false;

            CaptureCamera();
            CaptureBattlefield();
            collapseView.Begin(plan, sourceBoard, effectScale);
            scarView.Begin(plan, sourceBoard, effectScale);

            float elapsed = 0f;
            bool actorsResolved = false;
            bool committed = false;

            while (elapsed < duration)
            {
                float deltaTime = Mathf.Max(0f, Time.unscaledDeltaTime);
                elapsed = Mathf.Min(duration, elapsed + deltaTime);

                collapseView.Update(elapsed, duration);
                scarView.Update(elapsed);
                ApplyCameraShake(elapsed, duration, impactShake, rumbleShake);
                ApplyBattlefieldShake(elapsed, duration, impactShake, rumbleShake);

                if (!actorsResolved && elapsed >= 0.94f)
                {
                    actorsResolved = true;
                    actors.PrepareCollapsing(plan);
                }

                if (!committed && elapsed >= commitTime)
                {
                    committed = true;
                    LastCommitSucceeded = commit();

                    if (!LastCommitSucceeded)
                    {
                        CancelVisuals(false);
                        yield break;
                    }

                    collapseView.MarkCommitted();
                    scarView.Commit();
                }

                yield return null;
            }

            if (!actorsResolved)
            {
                actors.PrepareCollapsing(plan);
            }

            if (!committed)
            {
                LastCommitSucceeded = commit();

                if (LastCommitSucceeded)
                {
                    collapseView.MarkCommitted();
                    scarView.Commit();
                }
            }

            CancelVisuals(false);
        }

        public void CancelVisuals(bool restoreBoard)
        {
            if (!LastCommitSucceeded)
            {
                actors.RestorePrepared();
            }

            collapseView.Dispose(restoreBoard);
            scarView.Dispose();

            if (restoreBoard)
            {
                scarView.ClearPersistent();
            }

            RestoreCamera();
        }

        private void CaptureCamera()
        {
            if (cameraTransform == null)
            {
                cameraCaptured = false;
                return;
            }

            cameraBasePosition = cameraTransform.localPosition;
            cameraBaseRotation = cameraTransform.localRotation;
            cameraBaseFieldOfView = camera != null ? camera.fieldOfView : 0f;
            cameraCaptured = true;
        }

        private void CaptureBattlefield()
        {
            if (battlefieldTransform == null)
            {
                battlefieldCaptured = false;
                return;
            }

            battlefieldBasePosition = battlefieldTransform.localPosition;
            battlefieldBaseRotation = battlefieldTransform.localRotation;
            battlefieldCaptured = true;
        }

        private void ApplyCameraShake(float elapsed, float duration, float impactShake, float rumbleShake)
        {
            if (!cameraCaptured || cameraTransform == null)
            {
                return;
            }

            float slam = Pulse(elapsed, 0.065f, 0.075f);
            float recoil = Pulse(elapsed, 0.21f, 0.14f);
            float settle = Pulse(elapsed, 0.43f, 0.24f);
            float rumbleIn = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.16f, 0.48f, elapsed));
            float rumbleOut = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(duration - 0.72f, duration, elapsed));
            float collapsePulse = Pulse(elapsed, 1.2f, 0.12f) * 0.56f + Pulse(elapsed, 1.52f, 0.14f) * 0.46f + Pulse(elapsed, 1.88f, 0.16f) * 0.36f + Pulse(elapsed, 2.18f, 0.18f) * 0.26f;
            float lowRumble = rumbleShake * rumbleIn * rumbleOut;
            float pulseAmplitude = impactShake * collapsePulse;
            float noiseAmplitude = lowRumble * 0.82f + pulseAmplitude * 0.28f;
            float jitterX = Mathf.Sin(elapsed * 27.4f + 0.35f) * noiseAmplitude + Mathf.Sin(elapsed * 13.8f + 1.7f) * noiseAmplitude * 0.42f;
            float jitterY = Mathf.Sin(elapsed * 23.1f + 2.2f) * noiseAmplitude * 0.62f;
            float kickX = impactShake * (slam * 0.24f - recoil * 0.12f + settle * 0.038f);
            float kickY = impactShake * (-slam * 0.96f + recoil * 0.42f - settle * 0.09f);
            cameraTransform.localPosition = cameraBasePosition + new Vector3(kickX + jitterX, kickY + jitterY, 0f);

            float roll = impactShake * (slam * 0.62f - recoil * 0.24f + settle * 0.07f);
            roll += Mathf.Sin(elapsed * 21.6f + 0.55f) * (lowRumble * 0.75f + pulseAmplitude * 0.22f);
            float pitch = impactShake * (-slam * 2.05f + recoil * 0.78f - settle * 0.22f);
            pitch += Mathf.Sin(elapsed * 18.8f + 1.75f) * (lowRumble * 1.45f + pulseAmplitude * 0.48f);
            cameraTransform.localRotation = cameraBaseRotation * Quaternion.Euler(pitch, 0f, roll);

            if (camera != null && !camera.orthographic)
            {
                float fovPunch = impactShake * (-slam * 2.8f + recoil * 0.9f - settle * 0.18f + collapsePulse * 0.18f);
                camera.fieldOfView = cameraBaseFieldOfView + fovPunch;
            }
        }

        private void ApplyBattlefieldShake(float elapsed, float duration, float impactShake, float rumbleShake)
        {
            if (!battlefieldCaptured || battlefieldTransform == null)
            {
                return;
            }

            float slam = Pulse(elapsed, 0.125f, 0.105f);
            float recoil = Pulse(elapsed, 0.31f, 0.18f);
            float settle = Pulse(elapsed, 0.54f, 0.28f);
            float collapsePulse = Pulse(elapsed, 1.18f, 0.11f) * 0.54f + Pulse(elapsed, 1.52f, 0.13f) * 0.48f + Pulse(elapsed, 1.88f, 0.15f) * 0.4f + Pulse(elapsed, 2.18f, 0.18f) * 0.28f;
            float worldRumbleIn = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.14f, 0.52f, elapsed));
            float worldRumbleOut = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(duration - 0.78f, duration, elapsed));
            float worldRumble = rumbleShake * worldRumbleIn * worldRumbleOut * 0.34f;
            float pulseWeight = impactShake * collapsePulse;

            float kickX = impactShake * (-slam * 0.15f + recoil * 0.075f - settle * 0.024f);
            float kickY = impactShake * (-slam * 0.065f + recoil * 0.028f - settle * 0.01f);
            float kickZ = impactShake * (slam * 0.3f - recoil * 0.135f + settle * 0.042f);
            float rumbleX = Mathf.Sin(elapsed * 18.7f + 0.32f) * worldRumble + Mathf.Sin(elapsed * 9.4f + 1.4f) * worldRumble * 0.46f;
            float rumbleZ = Mathf.Sin(elapsed * 16.1f + 2.3f) * worldRumble * 0.9f + Mathf.Sin(elapsed * 7.3f + 0.7f) * worldRumble * 0.28f;
            float rumbleY = Mathf.Sin(elapsed * 14.8f + 1.1f) * worldRumble * 0.18f;
            battlefieldTransform.localPosition = battlefieldBasePosition + new Vector3(kickX + rumbleX, kickY + rumbleY, kickZ + rumbleZ);

            float pitch = impactShake * (-slam * 1.25f + recoil * 0.52f - settle * 0.14f) + Mathf.Sin(elapsed * 10.4f + 0.6f) * (worldRumble * 2.1f + pulseWeight * 0.16f);
            float yaw = impactShake * (slam * 0.82f - recoil * 0.38f + settle * 0.11f) + Mathf.Sin(elapsed * 8.3f + 1.8f) * (worldRumble * 1.2f + pulseWeight * 0.1f);
            float roll = impactShake * (slam * 0.92f - recoil * 0.42f + settle * 0.15f) + Mathf.Sin(elapsed * 12.9f + 2.05f) * (worldRumble * 1.4f + pulseWeight * 0.12f);
            battlefieldTransform.localRotation = battlefieldBaseRotation * Quaternion.Euler(pitch, yaw, roll);
        }

        private static float Pulse(float time, float center, float halfWidth)
        {
            float distance = Mathf.Abs(time - center);

            if (distance >= halfWidth)
            {
                return 0f;
            }

            float normalized = 1f - distance / halfWidth;
            return normalized * normalized;
        }

        private void RestoreCamera()
        {
            if (cameraCaptured && cameraTransform != null)
            {
                cameraTransform.localPosition = cameraBasePosition;
                cameraTransform.localRotation = cameraBaseRotation;
            }

            if (cameraCaptured && camera != null && !camera.orthographic)
            {
                camera.fieldOfView = cameraBaseFieldOfView;
            }

            if (battlefieldCaptured && battlefieldTransform != null)
            {
                battlefieldTransform.localPosition = battlefieldBasePosition;
                battlefieldTransform.localRotation = battlefieldBaseRotation;
            }

            cameraCaptured = false;
            battlefieldCaptured = false;
        }
    }

    internal sealed class RaidPhaseActorState
    {
        private readonly List<UnitSnapshot> units = new List<UnitSnapshot>(8);
        private readonly List<EnemySnapshot> enemies = new List<EnemySnapshot>(64);
        private readonly RaidPhasePathRouter router = new RaidPhasePathRouter();

        public void Capture(RaidPhaseTransitionPlan plan)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            units.Clear();
            enemies.Clear();

            foreach (UnitRuntimeState unit in CombatRegistry.Units)
            {
                if (unit == null || !unit.IsInitialized || unit.GridPosition == null || !unit.GridPosition.IsInitialized)
                {
                    continue;
                }

                Vector2Int tile = unit.GridPosition.TileCoordinate;

                if (plan.IsCollapsing(tile))
                {
                    units.Add(new UnitSnapshot(unit, tile));
                }
            }

            foreach (EnemyRuntimeState enemy in CombatRegistry.Enemies)
            {
                if (enemy == null || !enemy.IsInitialized || enemy.GridPosition == null || !enemy.GridPosition.IsInitialized || enemy.Health == null || enemy.Health.IsDead)
                {
                    continue;
                }

                Vector2Int tile = enemy.GridPosition.TileCoordinate;
                bool collapsing = plan.IsCollapsing(tile);
                bool hasGoal = TryGetGoalTile(enemy, out Vector2Int goalTile);
                enemies.Add(new EnemySnapshot(enemy, tile, enemy.transform.position, enemy.GridPosition.FacingDirection, collapsing, hasGoal, goalTile));
            }
        }

        public void PrepareCollapsing(RaidPhaseTransitionPlan plan)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            for (int i = 0; i < units.Count; i++)
            {
                UnitSnapshot snapshot = units[i];
                UnitRuntimeState unit = snapshot.Unit;

                if (unit == null || !unit.gameObject.activeInHierarchy)
                {
                    continue;
                }

                unit.gameObject.SetActive(false);
                snapshot.Prepared = true;
            }

            for (int i = 0; i < enemies.Count; i++)
            {
                EnemySnapshot snapshot = enemies[i];

                if (!snapshot.Collapsing || snapshot.Enemy == null || !snapshot.Enemy.gameObject.activeInHierarchy)
                {
                    continue;
                }

                snapshot.Enemy.gameObject.SetActive(false);
                snapshot.Prepared = true;
            }
        }

        public void Commit(RaidPhaseTransitionPlan plan, RaidBoard targetBoard, Action<RaidForcedRetreatInfo> onForcedRetreat, Action<RaidEnemyPhaseRemovalInfo> onEnemyRemoved)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            if (targetBoard == null)
            {
                throw new ArgumentNullException(nameof(targetBoard));
            }

            for (int i = 0; i < units.Count; i++)
            {
                UnitSnapshot snapshot = units[i];
                UnitRuntimeState unit = snapshot.Unit;

                if (unit == null)
                {
                    continue;
                }

                unit.Block?.ReleaseAll();
                onForcedRetreat?.Invoke(new RaidForcedRetreatInfo(unit, snapshot.Tile));
                unit.gameObject.SetActive(false);
                snapshot.Prepared = false;
            }

            units.Clear();
            router.Prepare(targetBoard);

            for (int i = 0; i < enemies.Count; i++)
            {
                EnemySnapshot snapshot = enemies[i];
                EnemyRuntimeState enemy = snapshot.Enemy;

                if (enemy == null)
                {
                    continue;
                }

                if (snapshot.Collapsing)
                {
                    BlockLink.Release(enemy.Block);
                    onEnemyRemoved?.Invoke(new RaidEnemyPhaseRemovalInfo(enemy, snapshot.Tile, RaidEnemyPhaseRemovalReason.CollapsingTile));
                    GameObject collapsingInstance = enemy.gameObject;
                    if (!RaidEnemyPool.Release(collapsingInstance))
                    {
                        collapsingInstance.SetActive(false);
                        UnityEngine.Object.Destroy(collapsingInstance);
                    }
                    snapshot.Enemy = null;
                    snapshot.Prepared = false;
                    continue;
                }

                if (!enemy.gameObject.activeInHierarchy)
                {
                    enemy.gameObject.SetActive(true);
                }

                if (!snapshot.HasGoal)
                {
                    continue;
                }

                BlockLink.Release(enemy.Block);

                if (!router.TryBuild(snapshot.Tile, snapshot.WorldPosition, snapshot.Facing, snapshot.GoalTile, out PathNode[] path) || !enemy.Move.SetPath(path))
                {
                    RemoveEnemy(enemy, snapshot.Tile, RaidEnemyPhaseRemovalReason.RepathFailed, onEnemyRemoved);
                    snapshot.Enemy = null;
                }
            }

            enemies.Clear();
        }

        public void RestorePrepared()
        {
            for (int i = 0; i < units.Count; i++)
            {
                UnitSnapshot snapshot = units[i];

                if (snapshot.Prepared && snapshot.Unit != null)
                {
                    snapshot.Unit.gameObject.SetActive(true);
                    snapshot.Prepared = false;
                }
            }

            for (int i = 0; i < enemies.Count; i++)
            {
                EnemySnapshot snapshot = enemies[i];

                if (snapshot.Prepared && snapshot.Enemy != null)
                {
                    snapshot.Enemy.gameObject.SetActive(true);
                    snapshot.Prepared = false;
                }
            }
        }

        private static bool TryGetGoalTile(EnemyRuntimeState enemy, out Vector2Int goalTile)
        {
            goalTile = default;
            return enemy != null && enemy.Move != null && enemy.Move.TryGetGoalTile(out goalTile);
        }

        private static void RemoveEnemy(EnemyRuntimeState enemy, Vector2Int tile, RaidEnemyPhaseRemovalReason reason, Action<RaidEnemyPhaseRemovalInfo> onEnemyRemoved)
        {
            if (enemy == null)
            {
                return;
            }

            BlockLink.Release(enemy.Block);
            onEnemyRemoved?.Invoke(new RaidEnemyPhaseRemovalInfo(enemy, tile, reason));
            GameObject instance = enemy.gameObject;
            if (!RaidEnemyPool.Release(instance))
            {
                instance.SetActive(false);
                UnityEngine.Object.Destroy(instance);
            }
        }

        private sealed class UnitSnapshot
        {
            public UnitRuntimeState Unit { get; }
            public Vector2Int Tile { get; }
            public bool Prepared { get; set; }

            public UnitSnapshot(UnitRuntimeState unit, Vector2Int tile)
            {
                Unit = unit;
                Tile = tile;
            }
        }

        private sealed class EnemySnapshot
        {
            public EnemyRuntimeState Enemy { get; set; }
            public Vector2Int Tile { get; }
            public Vector3 WorldPosition { get; }
            public GridFacingDirection Facing { get; }
            public bool Collapsing { get; }
            public bool HasGoal { get; }
            public Vector2Int GoalTile { get; }
            public bool Prepared { get; set; }

            public EnemySnapshot(EnemyRuntimeState enemy, Vector2Int tile, Vector3 worldPosition, GridFacingDirection facing, bool collapsing, bool hasGoal, Vector2Int goalTile)
            {
                Enemy = enemy;
                Tile = tile;
                WorldPosition = worldPosition;
                Facing = facing;
                Collapsing = collapsing;
                HasGoal = hasGoal;
                GoalTile = goalTile;
            }
        }
    }

    internal sealed class RaidPhasePathRouter
    {
        private static readonly Vector2Int[] Directions = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

        private RaidBoard board;
        private int[] parents = Array.Empty<int>();
        private int[] visitMarks = Array.Empty<int>();
        private int[] queue = Array.Empty<int>();
        private int[] reversePath = Array.Empty<int>();
        private int visitMark;

        public void Prepare(RaidBoard targetBoard)
        {
            board = targetBoard ?? throw new ArgumentNullException(nameof(targetBoard));
            int count = board.Count;

            if (parents.Length != count)
            {
                parents = new int[count];
                visitMarks = new int[count];
                queue = new int[count];
                reversePath = new int[count];
                visitMark = 0;
            }
        }

        public bool TryBuild(Vector2Int startTile, Vector3 startWorldPosition, GridFacingDirection startFacing, Vector2Int goalTile, out PathNode[] path)
        {
            path = null;

            if (board == null || !board.IsInside(startTile) || !board.IsInside(goalTile))
            {
                return false;
            }

            RaidTile start = board.GetTile(startTile);
            RaidTile goal = board.GetTile(goalTile);

            if (start.Surface == RaidTileSurface.Void || !goal.IsGoal || !goal.IsPath)
            {
                return false;
            }

            BeginVisit();
            int startIndex = ToIndex(startTile);
            int goalIndex = ToIndex(goalTile);
            int head = 0;
            int tail = 0;
            queue[tail++] = startIndex;
            visitMarks[startIndex] = visitMark;
            parents[startIndex] = -1;

            while (head < tail)
            {
                int currentIndex = queue[head++];

                if (currentIndex == goalIndex)
                {
                    return BuildPath(startTile, startWorldPosition, startFacing, goalIndex, out path);
                }

                Vector2Int current = ToCoordinate(currentIndex);

                for (int i = 0; i < Directions.Length; i++)
                {
                    Vector2Int next = current + Directions[i];

                    if (!board.IsInside(next))
                    {
                        continue;
                    }

                    int nextIndex = ToIndex(next);

                    if (visitMarks[nextIndex] == visitMark)
                    {
                        continue;
                    }

                    RaidTile nextTile = board.GetTile(next);

                    if (!nextTile.IsPath)
                    {
                        continue;
                    }

                    visitMarks[nextIndex] = visitMark;
                    parents[nextIndex] = currentIndex;
                    queue[tail++] = nextIndex;
                }
            }

            return false;
        }

        private bool BuildPath(Vector2Int startTile, Vector3 startWorldPosition, GridFacingDirection startFacing, int goalIndex, out PathNode[] path)
        {
            int count = 0;
            int current = goalIndex;

            while (current >= 0 && count < reversePath.Length)
            {
                reversePath[count++] = current;
                current = parents[current];
            }

            if (count == 0 || reversePath[count - 1] != ToIndex(startTile))
            {
                path = null;
                return false;
            }

            path = new PathNode[count];

            for (int i = 0; i < count; i++)
            {
                int sourceIndex = reversePath[count - 1 - i];
                Vector2Int tile = ToCoordinate(sourceIndex);
                Vector3 position = i == 0 ? startWorldPosition : board.TileToWorld(tile);
                GridFacingDirection facing;

                if (count == 1)
                {
                    facing = startFacing;
                }
                else if (i == 0)
                {
                    Vector2Int nextTile = ToCoordinate(reversePath[count - 2]);
                    facing = ToFacing(tile, nextTile, startFacing);
                }
                else
                {
                    Vector2Int previousTile = ToCoordinate(reversePath[count - i]);
                    facing = ToFacing(previousTile, tile, startFacing);
                }

                path[i] = new PathNode(position, tile, facing);
            }

            return path.Length >= 2;
        }

        private void BeginVisit()
        {
            visitMark++;

            if (visitMark != int.MaxValue)
            {
                return;
            }

            Array.Clear(visitMarks, 0, visitMarks.Length);
            visitMark = 1;
        }

        private int ToIndex(Vector2Int coordinate)
        {
            return coordinate.y * board.Width + coordinate.x;
        }

        private Vector2Int ToCoordinate(int index)
        {
            return new Vector2Int(index % board.Width, index / board.Width);
        }

        private static GridFacingDirection ToFacing(Vector2Int from, Vector2Int to, GridFacingDirection fallback)
        {
            Vector2Int delta = to - from;

            if (delta.x > 0 && delta.y == 0)
            {
                return GridFacingDirection.East;
            }

            if (delta.x < 0 && delta.y == 0)
            {
                return GridFacingDirection.West;
            }

            if (delta.y > 0 && delta.x == 0)
            {
                return GridFacingDirection.North;
            }

            if (delta.y < 0 && delta.x == 0)
            {
                return GridFacingDirection.South;
            }

            return fallback;
        }
    }
}
