using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    public readonly struct PassiveSummonRequest
    {
        public UnitRuntimeState UnitOwner { get; }
        public EnemyRuntimeState EnemyOwner { get; }
        public GameObject Prefab { get; }
        public int Count { get; }
        public PassiveDataSO Passive { get; }
        public Vector3 Origin { get; }

        public bool IsUnitRequest => UnitOwner != null;
        public bool IsEnemyRequest => EnemyOwner != null;

        public PassiveSummonRequest(UnitRuntimeState unitOwner, GameObject prefab, int count, PassiveDataSO passive)
        {
            UnitOwner = unitOwner;
            EnemyOwner = null;
            Prefab = prefab;
            Count = Mathf.Max(1, count);
            Passive = passive;
            Origin = unitOwner != null ? unitOwner.transform.position : Vector3.zero;
        }

        public PassiveSummonRequest(EnemyRuntimeState enemyOwner, GameObject prefab, int count, PassiveDataSO passive)
        {
            UnitOwner = null;
            EnemyOwner = enemyOwner;
            Prefab = prefab;
            Count = Mathf.Max(1, count);
            Passive = passive;
            Origin = enemyOwner != null ? enemyOwner.transform.position : Vector3.zero;
        }
    }

    public static class PassiveRuntimeEvents
    {
        public static event Action<UnitRuntimeState, int, PassiveDataSO> OnSummonCostGainRequested;
        public static event Action<PassiveSummonRequest> OnSummonRequested;

        internal static event Action<UnitRuntimeState> OnUnitSkillSucceeded;
        internal static event Action<UnitRuntimeState, GameObject> OnUnitSummonCreated;
        internal static event Action<UnitRuntimeState, GameObject> OnUnitSummonDestroyed;

        private static readonly HashSet<int> activeUnitSummonIds = new HashSet<int>();

        public static int ActiveUnitSummonCount => activeUnitSummonIds.Count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            OnSummonCostGainRequested = null;
            OnSummonRequested = null;
            OnUnitSkillSucceeded = null;
            OnUnitSummonCreated = null;
            OnUnitSummonDestroyed = null;
            activeUnitSummonIds.Clear();
        }

        public static void NotifyUnitSkillSucceeded(UnitRuntimeState unit)
        {
            if (unit == null || !unit.IsInitialized || unit.Health == null || unit.Health.IsDead)
            {
                return;
            }

            OnUnitSkillSucceeded?.Invoke(unit);
        }

        public static void NotifyUnitSummonCreated(UnitRuntimeState source, GameObject summon)
        {
            if (summon == null)
            {
                return;
            }

            int instanceId = summon.GetInstanceID();

            if (!activeUnitSummonIds.Add(instanceId))
            {
                return;
            }

            OnUnitSummonCreated?.Invoke(source, summon);
        }

        public static void NotifyUnitSummonDestroyed(UnitRuntimeState source, GameObject summon)
        {
            if (summon == null)
            {
                return;
            }

            int instanceId = summon.GetInstanceID();

            if (!activeUnitSummonIds.Remove(instanceId))
            {
                return;
            }

            OnUnitSummonDestroyed?.Invoke(source, summon);
        }

        internal static void RequestSummonCostGain(UnitRuntimeState source, int amount, PassiveDataSO passive)
        {
            if (source == null || amount <= 0)
            {
                return;
            }

            OnSummonCostGainRequested?.Invoke(source, amount, passive);
        }

        internal static void RequestSummon(UnitRuntimeState source, GameObject prefab, int count, PassiveDataSO passive)
        {
            if (source == null || count <= 0)
            {
                return;
            }

            PassiveSummonRequest request = new PassiveSummonRequest(source, prefab, count, passive);
            OnSummonRequested?.Invoke(request);
            SummonService.TrySpawn(new SummonRequest(source, prefab, count, passive), out _);
        }

        internal static void RequestSummon(EnemyRuntimeState source, GameObject prefab, int count, PassiveDataSO passive)
        {
            if (source == null || count <= 0)
            {
                return;
            }

            PassiveSummonRequest request = new PassiveSummonRequest(source, prefab, count, passive);
            OnSummonRequested?.Invoke(request);
            SummonService.TrySpawn(new SummonRequest(source, prefab, count, passive), out _);
        }
    }
}
