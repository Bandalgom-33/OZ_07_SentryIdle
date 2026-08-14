using System;
using System.Collections.Generic;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    internal sealed class RaidVisualSpawner
    {
        private readonly Transform parent;
        private readonly List<GameObject> instances = new List<GameObject>();

        public RaidVisualSpawner(Transform parent)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            this.parent = parent;
        }

        public void Clear()
        {
            for (int i = instances.Count - 1; i >= 0; i--)
            {
                if (instances[i] != null)
                {
                    UnityEngine.Object.Destroy(instances[i]);
                }
            }

            instances.Clear();
        }

        public void SpawnTile(GameObject prefab, Vector3 worldPosition, float tileSize)
        {
            if (prefab == null)
            {
                return;
            }

            GameObject instance = UnityEngine.Object.Instantiate(prefab, parent);
            instance.transform.position = worldPosition;
            instance.transform.localScale = new Vector3(tileSize, 1f, tileSize);
            instances.Add(instance);
        }

        public void SpawnArt(GameObject prefab, Vector3 worldPosition, Quaternion worldRotation, float uniformScale)
        {
            if (prefab == null)
            {
                return;
            }

            GameObject instance = UnityEngine.Object.Instantiate(prefab, worldPosition, worldRotation * prefab.transform.rotation, parent);
            instance.transform.localScale = prefab.transform.localScale * uniformScale;
            instances.Add(instance);
        }
    }
}
