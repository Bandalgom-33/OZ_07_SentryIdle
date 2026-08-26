using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Raid.Data;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Editor
{
    [CustomEditor(typeof(Runtime.RaidEnemySpawner))]
    public sealed class RaidEnemySpawnerEditor : UnityEditor.Editor
    {
        private readonly List<Runtime.RaidSpawnInfo> spawns = new List<Runtime.RaidSpawnInfo>(32);

        private EnemyDataSO enemyData;
        private Runtime.RaidRouteGraph cachedGraph;
        private int[] entryIds = Array.Empty<int>();
        private string[] entryLabels = Array.Empty<string>();
        private int[] pathCounts = Array.Empty<int>();
        private int selectedEntry;
        private int batchCount = 6;
        private int width1Count;
        private int width2Count;
        private int width3Count;
        private int otherWidthCount;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("플레이 검수", EditorStyles.boldLabel);

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("위 '맵 선택'에서 Map 01/02를 고른 뒤 Play하면 해당 정식 Map Family의 Phase1부터 시작합니다. 검증 완료 후 '랜덤'으로 바꾸면 됩니다.", MessageType.Info);
                return;
            }

            Runtime.RaidEnemySpawner spawner = (Runtime.RaidEnemySpawner)target;
            Runtime.RaidBattleController battle = spawner.GetComponent<Runtime.RaidBattleController>();
            Runtime.RaidBoardRuntime board = spawner.GetComponent<Runtime.RaidBoardRuntime>();

            if (battle == null || board == null || board.Board == null || board.RouteGraph == null || board.EnemyPaths == null)
            {
                EditorGUILayout.HelpBox("레이드 맵 또는 Enemy Path가 아직 준비되지 않았습니다.", MessageType.Warning);
                return;
            }

            RefreshGraph(board);

            if (entryIds.Length == 0)
            {
                EditorGUILayout.HelpBox("현재 맵에 Entry가 없습니다.", MessageType.Error);
                return;
            }

            DrawMapInfo(board);

            enemyData = (EnemyDataSO)EditorGUILayout.ObjectField("Enemy Data", enemyData, typeof(EnemyDataSO), false);

            if (selectedEntry >= entryIds.Length)
            {
                selectedEntry = 0;
            }

            selectedEntry = EditorGUILayout.Popup("Entry", selectedEntry, entryLabels);
            EditorGUILayout.LabelField("선택 Entry Path 수", pathCounts[selectedEntry].ToString());

            batchCount = EditorGUILayout.IntSlider("동시 생성 수", batchCount, 2, 12);

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(battle.IsRunning || battle.IsTransitioning))
            {
                if (GUILayout.Button("레이드 시작"))
                {
                    battle.BeginRaid();
                }
            }

            EditorGUILayout.LabelField("Battle State", battle.State.ToString());
            DrawRaidCoreDebug(battle);
            DrawAutomaticSpawnDebug(spawner);

            if (board.Phase == RaidPhase.Phase1)
            {
                using (new EditorGUI.DisabledScope(!battle.IsRunning))
                {
                    if (GUILayout.Button("Phase1 → Phase2 붕괴 전환"))
                    {
                        battle.TryTransitionTo(RaidPhase.Phase2);
                    }
                }
            }
            else if (board.Phase == RaidPhase.Phase2)
            {
                using (new EditorGUI.DisabledScope(!battle.IsRunning))
                {
                    if (GUILayout.Button("Phase2 → Phase3 붕괴 전환"))
                    {
                        battle.TryTransitionTo(RaidPhase.Phase3);
                    }
                }
            }
            else if (board.Phase == RaidPhase.Phase3)
            {
                EditorGUILayout.HelpBox("현재 선택된 Map Family의 최종 Phase3입니다.", MessageType.Info);
            }

            using (new EditorGUI.DisabledScope(!battle.IsRunning || enemyData == null))
            {
                if (GUILayout.Button("몬스터 1회 생성"))
                {
                    SpawnOne(spawner, entryIds[selectedEntry]);
                }

                if (GUILayout.Button($"선택 Entry {batchCount}회 동시 생성"))
                {
                    SpawnBatch(spawner, entryIds[selectedEntry], batchCount);
                }

                if (GUILayout.Button("모든 Entry 각 1회 생성"))
                {
                    SpawnAllEntries(spawner);
                }
            }

            if (GUILayout.Button("Path 선택 순서 초기화"))
            {
                spawner.ResetPathSelection();
            }

            if (spawns.Count > 0 && GUILayout.Button("검수 기록 지우기"))
            {
                spawns.Clear();
            }

            DrawSpawns(board, spawner);
            RepaintIfMoving();
        }

        private static void DrawRaidCoreDebug(Runtime.RaidBattleController battle)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("레이드 코어 상태", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Boss HP", $"{battle.CurrentBossHp:N0} / {battle.BossMaxHp:N0}  ({battle.BossHpRatio:P1})");
            EditorGUILayout.LabelField("Time", battle.RemainingTime.ToString("F1"));
            EditorGUILayout.LabelField("Boss Skill", $"{battle.BossSkillGauge:F0} / {battle.BossSkillGaugeMax:F0}");
            EditorGUILayout.LabelField("Boss Skill Casting", battle.IsBossSkillCasting.ToString());
            EditorGUILayout.LabelField("Raid Attack", $"{battle.RaidAttackGauge:F0} / {battle.RaidAttackGaugeMax:F0}");
            EditorGUILayout.LabelField("Mode", battle.Mode.ToString());

            using (new EditorGUI.DisabledScope(!battle.IsRunning))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Boss HP -10%"))
                    {
                        battle.ApplyBossDamage(battle.BossMaxHp * 0.1f);
                    }

                    if (GUILayout.Button("Goal 1회 Gauge"))
                    {
                        float amount = battle.Config != null ? battle.Config.GoalSkillGaugeGain : 0f;
                        battle.AddBossSkillGauge(amount);
                    }

                    if (GUILayout.Button("Raid Gauge +25%"))
                    {
                        battle.AddRaidAttackGauge(battle.RaidAttackGaugeMax * 0.25f);
                    }
                }

                if (GUILayout.Button("Raid Gauge 소비"))
                {
                    battle.ConsumeRaidAttackGauge();
                }
            }
        }

        private static void DrawAutomaticSpawnDebug(Runtime.RaidEnemySpawner spawner)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("자동 Spawn 상태", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Running", spawner.IsAutomaticSpawnRunning.ToString());
            EditorGUILayout.LabelField("Active Enemy", spawner.ActiveEnemyCount.ToString());
            EditorGUILayout.LabelField("Burst", spawner.AutomaticBurstCount.ToString());
            EditorGUILayout.LabelField("Automatic Spawn", spawner.AutomaticSpawnCount.ToString());
            EditorGUILayout.LabelField("Air Spawn", spawner.AirSpawnCount.ToString());
            EditorGUILayout.LabelField("Speed Adjusted", spawner.SpeedAdjustedSpawnCount.ToString());
            EditorGUILayout.LabelField("Spawn Fail", spawner.FailedAutomaticSpawnCount.ToString());
            EditorGUILayout.LabelField("Entry Cache", spawner.AutomaticEntryCount.ToString());
            EditorGUILayout.LabelField("Enemy Data Pool", spawner.AutomaticEnemyPoolCount.ToString());
            EditorGUILayout.LabelField("Object Pool Created", spawner.PooledEnemyCreatedCount.ToString());
            EditorGUILayout.LabelField("Object Pool Reused", spawner.PooledEnemyReusedCount.ToString());
            EditorGUILayout.LabelField("Object Pool Released", spawner.PooledEnemyReleasedCount.ToString());

            if (!string.IsNullOrEmpty(spawner.LastSpawnedEnemyId))
            {
                string movement = spawner.LastSpawnWasAir ? "Air" : "Ground";
                EditorGUILayout.LabelField("Last Enemy", $"{spawner.LastSpawnedEnemyId}  ({movement})");
                EditorGUILayout.LabelField("Last Move Speed", $"{spawner.LastBaseMoveSpeed:F2} → {spawner.LastRaidMoveSpeed:F2}");

                if (spawner.LastSpawnWasAir)
                {
                    EditorGUILayout.LabelField("Last Air Corridor", $"Variant {spawner.LastAirCorridorVariant + 1}");
                }
            }
        }

        private void RefreshGraph(Runtime.RaidBoardRuntime board)
        {
            Runtime.RaidRouteGraph graph = board.RouteGraph;

            if (ReferenceEquals(cachedGraph, graph))
            {
                return;
            }

            cachedGraph = graph;
            spawns.Clear();

            int entryCount = 0;

            for (int nodeId = 0; nodeId < graph.NodeCount; nodeId++)
            {
                if (graph.GetNode(nodeId).Type == Runtime.RaidRouteNodeType.Entry)
                {
                    entryCount++;
                }
            }

            entryIds = new int[entryCount];
            entryLabels = new string[entryCount];
            pathCounts = new int[entryCount];

            int entryIndex = 0;

            for (int nodeId = 0; nodeId < graph.NodeCount; nodeId++)
            {
                Runtime.RaidRouteNode node = graph.GetNode(nodeId);

                if (node.Type != Runtime.RaidRouteNodeType.Entry)
                {
                    continue;
                }

                entryIds[entryIndex] = nodeId;
                entryLabels[entryIndex] = $"Entry {entryIndex + 1}  Node {node.Id}  Tile {node.Coordinate}";
                entryIndex++;
            }

            for (int pathIndex = 0; pathIndex < board.TravelPaths.Count; pathIndex++)
            {
                Runtime.RaidTravelPath path = board.TravelPaths[pathIndex];

                for (int i = 0; i < entryIds.Length; i++)
                {
                    if (entryIds[i] == path.EntryNodeId)
                    {
                        pathCounts[i]++;
                        break;
                    }
                }
            }

            width1Count = 0;
            width2Count = 0;
            width3Count = 0;
            otherWidthCount = 0;

            for (int edgeIndex = 0; edgeIndex < graph.EdgeCount; edgeIndex++)
            {
                int width = graph.Edges[edgeIndex].Width;

                switch (width)
                {
                    case 1:
                        width1Count++;
                        break;
                    case 2:
                        width2Count++;
                        break;
                    case 3:
                        width3Count++;
                        break;
                    default:
                        otherWidthCount++;
                        break;
                }
            }

            selectedEntry = 0;
        }

        private void DrawMapInfo(Runtime.RaidBoardRuntime board)
        {
            EditorGUILayout.LabelField("맵 현황", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Map Family", $"{board.FamilyId}  {board.FamilyName}");
            EditorGUILayout.LabelField("Map", board.MapId);
            EditorGUILayout.LabelField("Phase", board.Phase.ToString());
            EditorGUILayout.LabelField("Entry", entryIds.Length.ToString());
            EditorGUILayout.LabelField("Travel Paths", board.TravelPaths.Count.ToString());
            EditorGUILayout.LabelField("Width 1 Edges", width1Count.ToString());
            EditorGUILayout.LabelField("Width 2 Edges", width2Count.ToString());
            EditorGUILayout.LabelField("Width 3 Edges", width3Count.ToString());

            if (otherWidthCount > 0)
            {
                EditorGUILayout.LabelField("Other Width Edges", otherWidthCount.ToString());
            }

            EditorGUILayout.Space();
        }

        private void SpawnOne(Runtime.RaidEnemySpawner spawner, int entryNodeId)
        {
            if (spawner.TrySpawn(enemyData, entryNodeId, out Runtime.RaidSpawnInfo spawn))
            {
                spawns.Add(spawn);
            }
            else
            {
                Debug.LogWarning($"레이드 몬스터 생성에 실패했습니다. Entry: {entryNodeId}", spawner);
            }
        }

        private void SpawnBatch(Runtime.RaidEnemySpawner spawner, int entryNodeId, int count)
        {
            for (int i = 0; i < count; i++)
            {
                SpawnOne(spawner, entryNodeId);
            }
        }

        private void SpawnAllEntries(Runtime.RaidEnemySpawner spawner)
        {
            for (int i = 0; i < entryIds.Length; i++)
            {
                SpawnOne(spawner, entryIds[i]);
            }
        }

        private void DrawSpawns(Runtime.RaidBoardRuntime board, Runtime.RaidEnemySpawner spawner)
        {
            if (spawns.Count == 0)
            {
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Spawn 검수", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("총 Spawn", spawner.SpawnCount.ToString());

            int start = Mathf.Max(0, spawns.Count - 12);

            for (int i = start; i < spawns.Count; i++)
            {
                Runtime.RaidSpawnInfo spawn = spawns[i];

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    if (spawn.PathIndex < 0 || spawn.PathIndex >= board.TravelPaths.Count)
                    {
                        EditorGUILayout.LabelField($"#{i + 1}", "잘못된 Path Index");
                        continue;
                    }

                    Runtime.RaidTravelPath path = board.TravelPaths[spawn.PathIndex];

                    EditorGUILayout.LabelField($"#{i + 1}", $"Entry {spawn.EntryNodeId} → Goal {path.GoalNodeId}");
                    EditorGUILayout.LabelField("Path", spawn.PathIndex.ToString());
                    EditorGUILayout.LabelField("Route Plan", path.RoutePlanIndex.ToString());
                    EditorGUILayout.LabelField("Lane Variant", (path.LaneVariantIndex + 1).ToString());

                    if (spawn.Enemy == null)
                    {
                        EditorGUILayout.LabelField("Enemy", "제거됨");
                        continue;
                    }

                    EditorGUILayout.ObjectField("Enemy", spawn.Enemy, typeof(EndlessGuard.Unit.Runtime.EnemyRuntimeState), true);

                    if (spawn.Enemy.Move != null)
                    {
                        EditorGUILayout.LabelField("Progress", spawn.Enemy.Move.PathProgress.ToString("P1"));
                        EditorGUILayout.LabelField("Moving", spawn.Enemy.Move.IsMoving.ToString());
                        EditorGUILayout.LabelField("Reached Goal", spawn.Enemy.Move.HasReachedGoal.ToString());
                    }
                }
            }
        }

        private static void RepaintIfMoving()
        {
            if (EditorApplication.isPlaying)
            {
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }
        }
    }
}
