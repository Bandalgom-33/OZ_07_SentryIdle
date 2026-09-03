using EndlessGuard.Unit.Raid.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EndlessGuard.Unit.Raid.Editor
{
    [InitializeOnLoad]
    internal static class RaidEditorPlayBootstrap
    {
        private const string DirectPlaySceneName = "Unit";
        private const double ReadyTimeoutSeconds = 10.0;

        private static bool waitingForRaid;
        private static double readyDeadline;

        static RaidEditorPlayBootstrap()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;

            if (EditorApplication.isPlaying)
            {
                ScheduleAutoStart();
            }
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                ScheduleAutoStart();
                return;
            }

            if (change == PlayModeStateChange.ExitingPlayMode || change == PlayModeStateChange.EnteredEditMode)
            {
                CancelAutoStart();
            }
        }

        private static void ScheduleAutoStart()
        {
            if (!EditorApplication.isPlaying)
            {
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();

            if (!activeScene.IsValid() || activeScene.name != DirectPlaySceneName)
            {
                return;
            }

            waitingForRaid = true;
            readyDeadline = EditorApplication.timeSinceStartup + ReadyTimeoutSeconds;
            EditorApplication.update -= TryAutoStart;
            EditorApplication.update += TryAutoStart;
        }

        private static void TryAutoStart()
        {
            if (!waitingForRaid || !EditorApplication.isPlaying)
            {
                CancelAutoStart();
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();

            if (!activeScene.IsValid() || activeScene.name != DirectPlaySceneName)
            {
                CancelAutoStart();
                return;
            }

            RaidBattleController battle = UnityEngine.Object.FindFirstObjectByType<RaidBattleController>();
            RaidBoardRuntime board = UnityEngine.Object.FindFirstObjectByType<RaidBoardRuntime>();

            if (battle == null || board == null || board.Board == null)
            {
                if (EditorApplication.timeSinceStartup >= readyDeadline)
                {
                    Debug.LogWarning("[RaidDev] Unit 씬 Play Mode 자동 시작을 10초 안에 준비하지 못했습니다. RaidBattleController와 RaidBoardRuntime 상태를 확인하세요.");
                    CancelAutoStart();
                }

                return;
            }

            if (battle.IsRunning || battle.IsPreparing || battle.IsTransitioning || battle.State == RaidBattleState.Victory || battle.State == RaidBattleState.Defeat)
            {
                CancelAutoStart();
                return;
            }

            bool started = battle.BeginRaid();

            if (started)
            {
            }
            else
            {
                Debug.LogWarning("[RaidDev] RaidBattleController.BeginRaid()가 자동 시작 요청을 거부했습니다. Config 또는 Board 상태를 확인하세요.", battle);
            }

            CancelAutoStart();
        }

        private static void CancelAutoStart()
        {
            waitingForRaid = false;
            EditorApplication.update -= TryAutoStart;
        }
    }
}
