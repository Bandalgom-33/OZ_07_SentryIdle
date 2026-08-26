using System;
using System.Collections.Generic;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    internal sealed class RaidVisualSpawner
    {
        private readonly Transform parent;
        private readonly List<GameObject> instances = new List<GameObject>();
        private readonly List<Mesh> meshes = new List<Mesh>();

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

            for (int i = meshes.Count - 1; i >= 0; i--)
            {
                if (meshes[i] != null)
                {
                    UnityEngine.Object.Destroy(meshes[i]);
                }
            }

            meshes.Clear();
        }

        public GameObject SpawnTile(GameObject prefab, Vector3 worldPosition, float tileSize)
        {
            if (prefab == null)
            {
                return null;
            }

            GameObject instance = UnityEngine.Object.Instantiate(prefab, parent);
            instance.transform.position = worldPosition;
            instance.transform.localScale = new Vector3(tileSize, 1f, tileSize);
            instances.Add(instance);
            return instance;
        }

        public GameObject SpawnArt(GameObject prefab, Vector3 worldPosition, Quaternion worldRotation, float uniformScale)
        {
            if (prefab == null)
            {
                return null;
            }

            GameObject instance = UnityEngine.Object.Instantiate(prefab, worldPosition, worldRotation * prefab.transform.rotation, parent);
            instance.transform.localScale = prefab.transform.localScale * uniformScale;
            instances.Add(instance);
            return instance;
        }

        public GameObject SpawnMesh(string name, Mesh mesh, Material material)
        {
            if (mesh == null || material == null)
            {
                return null;
            }

            GameObject instance = new GameObject(name);
            instance.transform.SetParent(parent, false);
            MeshFilter filter = instance.AddComponent<MeshFilter>();
            MeshRenderer renderer = instance.AddComponent<MeshRenderer>();
            filter.sharedMesh = mesh;
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            instances.Add(instance);
            meshes.Add(mesh);
            return instance;
        }

        public bool ReleaseInstance(GameObject instance)
        {
            return instance != null && instances.Remove(instance);
        }
    }
}
