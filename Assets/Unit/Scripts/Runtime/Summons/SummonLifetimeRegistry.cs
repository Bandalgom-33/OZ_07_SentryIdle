using System.Collections.Generic;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    internal static class SummonLifetimeRegistry
    {
        private struct Entry
        {
            public GameObject Instance;
            public float RemainingSeconds;

            public Entry(GameObject instance, float remainingSeconds)
            {
                Instance = instance;
                RemainingSeconds = remainingSeconds;
            }
        }

        private static readonly List<Entry> entries = new List<Entry>(8);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            entries.Clear();
        }

        public static void Register(GameObject instance, float lifetimeSeconds)
        {
            if (instance == null || lifetimeSeconds <= 0f)
            {
                return;
            }

            Unregister(instance);
            entries.Add(new Entry(instance, lifetimeSeconds));
        }

        public static void Unregister(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            int instanceId = instance.GetInstanceID();

            for (int i = entries.Count - 1; i >= 0; i--)
            {
                GameObject current = entries[i].Instance;

                if (current != null && current.GetInstanceID() != instanceId)
                {
                    continue;
                }

                RemoveAtSwapBack(i);
            }
        }

        public static void Step(float deltaTime)
        {
            if (deltaTime <= 0f || entries.Count == 0)
            {
                return;
            }

            for (int i = entries.Count - 1; i >= 0; i--)
            {
                Entry entry = entries[i];

                if (entry.Instance == null || !entry.Instance.activeInHierarchy)
                {
                    RemoveAtSwapBack(i);
                    continue;
                }

                entry.RemainingSeconds -= deltaTime;

                if (entry.RemainingSeconds > 0f)
                {
                    entries[i] = entry;
                    continue;
                }

                GameObject expired = entry.Instance;
                RemoveAtSwapBack(i);
                SummonService.Release(expired);
            }
        }

        private static void RemoveAtSwapBack(int index)
        {
            int lastIndex = entries.Count - 1;

            if (index != lastIndex)
            {
                entries[index] = entries[lastIndex];
            }

            entries.RemoveAt(lastIndex);
        }
    }
}
