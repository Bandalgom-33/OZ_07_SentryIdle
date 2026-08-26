using EndlessGuard.Unit.Runtime;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    internal static class RaidRuntimeInstaller
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            RaidBattleController battle = Object.FindFirstObjectByType<RaidBattleController>();
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
        }

        private static T Ensure<T>(GameObject host) where T : Component
        {
            T component = host.GetComponent<T>();
            return component != null ? component : host.AddComponent<T>();
        }
    }
}
