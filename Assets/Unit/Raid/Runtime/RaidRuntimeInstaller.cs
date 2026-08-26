using EndlessGuard.Unit.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EndlessGuard.Unit.Raid.Runtime
{
    internal static class RaidRuntimeInstaller
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSceneLoadHook()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneLoadHook()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallInitialScene()
        {
            InstallScene(SceneManager.GetActiveScene());
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode _)
        {
            InstallScene(scene);
        }

        private static void InstallScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            GameObject[] roots = scene.GetRootGameObjects();

            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                RaidBattleController[] battles = roots[rootIndex].GetComponentsInChildren<RaidBattleController>(true);

                for (int battleIndex = 0; battleIndex < battles.Length; battleIndex++)
                {
                    InstallFor(battles[battleIndex]);
                }
            }
        }

        internal static void InstallFor(RaidBattleController battle)
        {
            if (battle == null)
            {
                return;
            }

            GameObject host = battle.gameObject;
            Transform raidRoot = battle.transform.parent;
            GameObject numberScaleHost = raidRoot != null ? raidRoot.gameObject : host;
            CombatNumberScale numberScale = Ensure<CombatNumberScale>(numberScaleHost);
            numberScale.SetScales(battle.Config != null ? battle.Config.RaidCombatNumberScale : 0.72f, battle.Config != null ? battle.Config.RaidCriticalNumberScale : 0.95f);
            Ensure<RaidRosterRuntime>(host);
            Ensure<RaidDeploymentRuntime>(host);
            Ensure<RaidFieldBuffRuntime>(host);
            Ensure<RaidItemRuntime>(host);
            Ensure<RaidDeploymentPlanner>(host);
            Ensure<RaidHudView>(host);
            Ensure<RaidDeploymentInput>(host);
            Ensure<RaidContributionView>(host);
            Ensure<RaidDeploymentNoticeView>(host);
            Ensure<RaidAttackRangeTileProvider>(host);
            Ensure<RaidRiftEntryView>(host);
            Ensure<RaidAudioRuntime>(host);
            Ensure<RaidAutoStartRuntime>(host);
        }

        private static T Ensure<T>(GameObject host) where T : Component
        {
            T component = host.GetComponent<T>();
            return component != null ? component : host.AddComponent<T>();
        }
    }
}
