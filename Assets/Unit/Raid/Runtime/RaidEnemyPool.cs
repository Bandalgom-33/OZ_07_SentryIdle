using System.Collections.Generic;
using EndlessGuard.Unit.Runtime;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    internal static class RaidEnemyPool
    {
        private const string PoolRootName = "[RaidEnemyPool]";
        private const int DefaultStackCapacity = 4;

        private sealed class Bucket
        {
            public readonly GameObject Prefab;
            public readonly Stack<GameObject> Inactive = new Stack<GameObject>(DefaultStackCapacity);
            public readonly HashSet<GameObject> Active = new HashSet<GameObject>();
            public Transform Root;
            public int CreatedCount;

            public Bucket(GameObject prefab)
            {
                Prefab = prefab;
            }
        }

        private static readonly Dictionary<int, Bucket> bucketsByPrefabId = new Dictionary<int, Bucket>(16);
        private static readonly Dictionary<int, Bucket> bucketsByInstanceId = new Dictionary<int, Bucket>(64);
        private static readonly HashSet<int> inactiveInstanceIds = new HashSet<int>();
        private static readonly List<GameObject> releaseBuffer = new List<GameObject>(32);
        private static Transform poolRoot;
        private static Transform configuredParent;

        public static int CreatedCount { get; private set; }
        public static int ReusedCount { get; private set; }
        public static int ReleasedCount { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            bucketsByPrefabId.Clear();
            bucketsByInstanceId.Clear();
            inactiveInstanceIds.Clear();
            releaseBuffer.Clear();
            poolRoot = null;
            configuredParent = null;
            CreatedCount = 0;
            ReusedCount = 0;
            ReleasedCount = 0;
        }

        public static GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
        {
            if (prefab == null)
            {
                return null;
            }

            Bucket bucket = GetOrCreateBucket(prefab, parent);
            GameObject instance = null;

            while (bucket.Inactive.Count > 0 && instance == null)
            {
                instance = bucket.Inactive.Pop();
                if (instance != null)
                {
                    inactiveInstanceIds.Remove(instance.GetInstanceID());
                }
            }

            if (instance == null)
            {
                instance = Object.Instantiate(prefab, position, rotation, bucket.Root);
                EnsurePoolMember(instance);
                int instanceId = instance.GetInstanceID();
                bucketsByInstanceId[instanceId] = bucket;
                bucket.CreatedCount++;
                CreatedCount++;
            }
            else
            {
                instance.transform.SetPositionAndRotation(position, rotation);
                ReusedCount++;

                if (!instance.activeSelf)
                {
                    instance.SetActive(true);
                }
            }

            bucket.Active.Add(instance);
            return instance;
        }

        public static void Prewarm(GameObject prefab, int count, Transform parent)
        {
            if (prefab == null || count <= 0)
            {
                return;
            }

            Bucket bucket = GetOrCreateBucket(prefab, parent);
            int target = Mathf.Max(0, count);

            while (bucket.CreatedCount < target)
            {
                GameObject instance = Object.Instantiate(prefab, Vector3.zero, prefab.transform.rotation, bucket.Root);
                EnsurePoolMember(instance);
                int instanceId = instance.GetInstanceID();
                bucketsByInstanceId[instanceId] = bucket;
                bucket.CreatedCount++;
                CreatedCount++;

                if (instance.activeSelf)
                {
                    instance.SetActive(false);
                }

                inactiveInstanceIds.Add(instanceId);
                bucket.Inactive.Push(instance);
            }
        }

        public static bool Release(GameObject instance)
        {
            if (instance == null)
            {
                return false;
            }

            int instanceId = instance.GetInstanceID();
            if (!bucketsByInstanceId.TryGetValue(instanceId, out Bucket bucket))
            {
                return false;
            }

            if (!inactiveInstanceIds.Add(instanceId))
            {
                return true;
            }

            EnemyRuntimeState state = instance.GetComponent<EnemyRuntimeState>();
            if (state != null)
            {
                SummonService.ReleaseEnemySummonsOwnedBy(state);

                if (SpawnedEnemyManager.TryGetExisting(out SpawnedEnemyManager manager))
                {
                    manager.UnregisterEnemy(state);
                }
            }

            bucket.Active.Remove(instance);

            if (instance.activeSelf)
            {
                instance.SetActive(false);
            }

            Transform bucketRoot = EnsureBucketRoot(bucket, configuredParent);
            instance.transform.SetParent(bucketRoot, false);
            bucket.Inactive.Push(instance);
            ReleasedCount++;
            return true;
        }

        public static void ReleaseAll()
        {
            releaseBuffer.Clear();

            foreach (Bucket bucket in bucketsByPrefabId.Values)
            {
                foreach (GameObject instance in bucket.Active)
                {
                    if (instance != null)
                    {
                        releaseBuffer.Add(instance);
                    }
                }
            }

            for (int i = 0; i < releaseBuffer.Count; i++)
            {
                Release(releaseBuffer[i]);
            }

            releaseBuffer.Clear();
        }

        private static Bucket GetOrCreateBucket(GameObject prefab, Transform parent)
        {
            EnsurePoolRoot(parent);
            int prefabId = prefab.GetInstanceID();

            if (!bucketsByPrefabId.TryGetValue(prefabId, out Bucket bucket))
            {
                bucket = new Bucket(prefab);
                bucketsByPrefabId.Add(prefabId, bucket);
            }

            EnsureBucketRoot(bucket, parent);
            return bucket;
        }

        private static void EnsurePoolRoot(Transform parent)
        {
            if (poolRoot != null)
            {
                if (parent != null && configuredParent != parent)
                {
                    configuredParent = parent;
                    poolRoot.SetParent(parent, false);
                }

                return;
            }

            configuredParent = parent;
            Transform existing = parent != null ? parent.Find(PoolRootName) : null;
            if (existing != null)
            {
                poolRoot = existing;
                return;
            }

            GameObject root = new GameObject(PoolRootName);
            poolRoot = root.transform;
            if (parent != null)
            {
                poolRoot.SetParent(parent, false);
            }
        }

        private static Transform EnsureBucketRoot(Bucket bucket, Transform parent)
        {
            EnsurePoolRoot(parent);

            if (bucket.Root != null)
            {
                return bucket.Root;
            }

            GameObject root = new GameObject(bucket.Prefab.name);
            bucket.Root = root.transform;
            bucket.Root.SetParent(poolRoot, false);
            return bucket.Root;
        }

        private static void EnsurePoolMember(GameObject instance)
        {
            if (instance != null && instance.GetComponent<RaidEnemyPoolMember>() == null)
            {
                instance.AddComponent<RaidEnemyPoolMember>();
            }
        }
    }
}
