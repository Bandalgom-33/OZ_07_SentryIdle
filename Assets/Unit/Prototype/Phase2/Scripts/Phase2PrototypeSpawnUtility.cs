using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Runtime;
using UnityEngine;

namespace EndlessGuard.Unit.Prototype.Phase2
{
    internal static class Phase2PrototypeSpawnUtility
    {
        public static UnitRuntimeState SpawnUnit(GameObject prefab, UnitDataSO data, Transform parent, Vector3 position)
        {
            if (prefab == null || data == null)
            {
                return null;
            }

            GameObject gate = new GameObject("Phase2_UnitSpawnGate");
            gate.SetActive(false);

            if (parent != null)
            {
                gate.transform.SetParent(parent, false);
            }

            GameObject instance = UnityEngine.Object.Instantiate(prefab, position, prefab.transform.rotation, gate.transform);
            UnitDataLink link = instance.GetComponent<UnitDataLink>();

            if (link == null || !Phase2PrototypeDataFactory.AssignUnitData(link, data))
            {
                UnityEngine.Object.Destroy(instance);
                UnityEngine.Object.Destroy(gate);
                return null;
            }

            gate.SetActive(true);
            instance.transform.SetParent(parent, true);
            UnityEngine.Object.Destroy(gate);

            UnitRuntimeState state = instance.GetComponent<UnitRuntimeState>();

            if (state != null && !state.IsInitialized)
            {
                state.InitializeRuntime();
            }

            return state;
        }

        public static EnemyRuntimeState SpawnEnemy(GameObject prefab, EnemyDataSO data, Transform parent, Vector3 position)
        {
            if (prefab == null || data == null)
            {
                return null;
            }

            GameObject gate = new GameObject("Phase2_EnemySpawnGate");
            gate.SetActive(false);

            if (parent != null)
            {
                gate.transform.SetParent(parent, false);
            }

            GameObject instance = UnityEngine.Object.Instantiate(prefab, position, prefab.transform.rotation, gate.transform);
            EnemyDataLink link = instance.GetComponent<EnemyDataLink>();

            if (link == null || !Phase2PrototypeDataFactory.AssignEnemyData(link, data))
            {
                UnityEngine.Object.Destroy(instance);
                UnityEngine.Object.Destroy(gate);
                return null;
            }

            gate.SetActive(true);
            instance.transform.SetParent(parent, true);
            UnityEngine.Object.Destroy(gate);

            EnemyRuntimeState state = instance.GetComponent<EnemyRuntimeState>();

            if (state != null && !state.IsInitialized)
            {
                state.InitializeRuntime();
            }

            return state;
        }
    }
}
