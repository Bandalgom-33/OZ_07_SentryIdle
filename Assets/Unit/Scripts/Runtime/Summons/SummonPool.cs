using System.Collections.Generic;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    internal static class SummonPool
    {
        private sealed class Bucket
        {
            public readonly GameObject Prefab;
            public readonly Stack<GameObject> Inactive = new Stack<GameObject>(4);

            public Bucket(GameObject prefab)
            {
                Prefab = prefab;
            }
        }

        private static readonly Dictionary<int, Bucket> bucketsByPrefabId = new Dictionary<int, Bucket>();
        private static readonly Dictionary<int, Bucket> bucketsByInstanceId = new Dictionary<int, Bucket>();
        private static readonly HashSet<int> inactiveInstanceIds = new HashSet<int>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            bucketsByPrefabId.Clear();
            bucketsByInstanceId.Clear();
            inactiveInstanceIds.Clear();
        }

        public static GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null)
            {
                return null;
            }

            int prefabId = prefab.GetInstanceID();

            if (!bucketsByPrefabId.TryGetValue(prefabId, out Bucket bucket))
            {
                bucket = new Bucket(prefab);
                bucketsByPrefabId.Add(prefabId, bucket);
            }

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
                instance = Object.Instantiate(bucket.Prefab, position, rotation);
                bucketsByInstanceId[instance.GetInstanceID()] = bucket;
            }
            else
            {
                instance.transform.SetPositionAndRotation(position, rotation);

                if (!instance.activeSelf)
                {
                    instance.SetActive(true);
                }
            }

            if (!instance.activeSelf)
            {
                instance.SetActive(true);
            }

            return instance;
        }

        public static void Release(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            int instanceId = instance.GetInstanceID();

            if (!bucketsByInstanceId.TryGetValue(instanceId, out Bucket bucket))
            {
                Object.Destroy(instance);
                return;
            }

            if (!inactiveInstanceIds.Add(instanceId))
            {
                return;
            }

            if (instance.activeSelf)
            {
                instance.SetActive(false);
            }

            bucket.Inactive.Push(instance);
        }
    }
}
