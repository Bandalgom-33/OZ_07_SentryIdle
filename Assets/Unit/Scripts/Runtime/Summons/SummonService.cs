using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    public static class SummonService
    {
        private readonly struct EnemySummonKey : System.IEquatable<EnemySummonKey>
        {
            public int OwnerId { get; }
            public int SourceId { get; }

            public EnemySummonKey(int ownerId, int sourceId)
            {
                OwnerId = ownerId;
                SourceId = sourceId;
            }

            public bool Equals(EnemySummonKey other)
            {
                return OwnerId == other.OwnerId && SourceId == other.SourceId;
            }

            public override bool Equals(object obj)
            {
                return obj is EnemySummonKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (OwnerId * 397) ^ SourceId;
                }
            }
        }

        private readonly struct EnemySummonRegistration
        {
            public EnemySummonKey Key { get; }

            public EnemySummonRegistration(EnemySummonKey key)
            {
                Key = key;
            }
        }

        private static readonly HashSet<int> invalidPrefabWarnings = new HashSet<int>();
        private static readonly HashSet<int> missingPlacementWarnings = new HashSet<int>();
        private static readonly HashSet<int> unavailablePlacementWarnings = new HashSet<int>();
        private static readonly Dictionary<EnemySummonKey, int> activeEnemySummonCounts = new Dictionary<EnemySummonKey, int>();
        private static readonly Dictionary<int, EnemySummonRegistration> enemySummonRegistrations = new Dictionary<int, EnemySummonRegistration>();
        private static readonly Dictionary<int, HashSet<EnemySummonRuntime>> enemySummonsByOwnerId = new Dictionary<int, HashSet<EnemySummonRuntime>>();
        private static readonly List<EnemySummonRuntime> enemySummonReleaseBuffer = new List<EnemySummonRuntime>(8);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            invalidPrefabWarnings.Clear();
            missingPlacementWarnings.Clear();
            unavailablePlacementWarnings.Clear();
            activeEnemySummonCounts.Clear();
            enemySummonRegistrations.Clear();
            enemySummonsByOwnerId.Clear();
            enemySummonReleaseBuffer.Clear();
        }

        public static bool TrySpawn(SummonRequest request, out int spawnedCount)
        {
            spawnedCount = 0;

            if (request.Prefab == null || request.Count <= 0)
            {
                return false;
            }

            if (request.IsUnitRequest && (request.UnitOwner == null || !request.UnitOwner.IsInitialized || request.UnitOwner.Health == null || request.UnitOwner.Health.IsDead))
            {
                return false;
            }

            if (request.IsEnemyRequest && (request.EnemyOwner == null || !request.EnemyOwner.IsInitialized || request.EnemyOwner.Health == null || request.EnemyOwner.Health.IsDead))
            {
                return false;
            }

            for (int i = 0; i < request.Count; i++)
            {
                bool hasResolvedPlacement = false;
                SummonTile resolvedTile = default;

                if (request.IsUnitRequest)
                {
                    hasResolvedPlacement = TryResolveUnitTile(request, out resolvedTile);

                    if (request.Source is CritSummonSO && !hasResolvedPlacement)
                    {
                        if (SummonTileService.HasProvider)
                        {
                            WarnUnavailablePlacementOnce(request);
                        }
                        else
                        {
                            WarnMissingPlacementProviderOnce(request);
                        }

                        continue;
                    }
                }

                Vector3 spawnPosition = hasResolvedPlacement ? resolvedTile.WorldPosition : request.Origin;
                GameObject instance = SummonPool.Get(request.Prefab, spawnPosition, request.Prefab.transform.rotation);

                if (instance == null)
                {
                    continue;
                }

                bool initialized = request.IsUnitRequest ? InitializeUnitSummon(request, instance, hasResolvedPlacement, resolvedTile) : InitializeEnemySummon(request, instance);

                if (!initialized)
                {
                    SummonPool.Release(instance);
                    WarnInvalidPrefabOnce(request.Prefab, request.IsUnitRequest);
                    continue;
                }

                spawnedCount++;
            }

            return spawnedCount > 0;
        }

        public static void Release(GameObject summon)
        {
            if (summon == null)
            {
                return;
            }

            SummonLifetimeRegistry.Unregister(summon);
            SummonPool.Release(summon);
        }

        internal static int GetActiveEnemySummonCount(EnemyRuntimeState owner, UnityEngine.Object source)
        {
            if (owner == null)
            {
                return 0;
            }

            EnemySummonKey key = CreateEnemySummonKey(owner, source);
            return activeEnemySummonCounts.TryGetValue(key, out int count) ? count : 0;
        }

        internal static void RegisterEnemySummon(EnemySummonRuntime runtime, EnemyRuntimeState owner, UnityEngine.Object source)
        {
            if (runtime == null || owner == null)
            {
                return;
            }

            int runtimeId = runtime.GetInstanceID();
            UnregisterEnemySummon(runtime);

            EnemySummonKey key = CreateEnemySummonKey(owner, source);
            enemySummonRegistrations[runtimeId] = new EnemySummonRegistration(key);
            activeEnemySummonCounts[key] = GetCount(key) + 1;

            int ownerId = owner.GetInstanceID();
            if (!enemySummonsByOwnerId.TryGetValue(ownerId, out HashSet<EnemySummonRuntime> summons))
            {
                summons = new HashSet<EnemySummonRuntime>();
                enemySummonsByOwnerId.Add(ownerId, summons);
            }

            summons.Add(runtime);
        }

        internal static void UnregisterEnemySummon(EnemySummonRuntime runtime)
        {
            if (runtime == null)
            {
                return;
            }

            int runtimeId = runtime.GetInstanceID();
            if (!enemySummonRegistrations.TryGetValue(runtimeId, out EnemySummonRegistration registration))
            {
                return;
            }

            enemySummonRegistrations.Remove(runtimeId);

            int count = GetCount(registration.Key) - 1;
            if (count > 0)
            {
                activeEnemySummonCounts[registration.Key] = count;
            }
            else
            {
                activeEnemySummonCounts.Remove(registration.Key);
            }

            if (enemySummonsByOwnerId.TryGetValue(registration.Key.OwnerId, out HashSet<EnemySummonRuntime> summons))
            {
                summons.Remove(runtime);
                if (summons.Count == 0)
                {
                    enemySummonsByOwnerId.Remove(registration.Key.OwnerId);
                }
            }
        }

        public static void ReleaseEnemySummonsOwnedBy(EnemyRuntimeState owner)
        {
            if (owner == null || !enemySummonsByOwnerId.TryGetValue(owner.GetInstanceID(), out HashSet<EnemySummonRuntime> summons) || summons.Count == 0)
            {
                return;
            }

            enemySummonReleaseBuffer.Clear();
            foreach (EnemySummonRuntime summon in summons)
            {
                if (summon != null)
                {
                    enemySummonReleaseBuffer.Add(summon);
                }
            }

            for (int i = 0; i < enemySummonReleaseBuffer.Count; i++)
            {
                EnemySummonRuntime summon = enemySummonReleaseBuffer[i];
                if (summon != null)
                {
                    summon.Release();
                }
            }

            enemySummonReleaseBuffer.Clear();
        }

        private static EnemySummonKey CreateEnemySummonKey(EnemyRuntimeState owner, UnityEngine.Object source)
        {
            int ownerId = owner != null ? owner.GetInstanceID() : 0;
            int sourceId = source != null ? source.GetInstanceID() : 0;
            return new EnemySummonKey(ownerId, sourceId);
        }

        private static int GetCount(EnemySummonKey key)
        {
            return activeEnemySummonCounts.TryGetValue(key, out int count) ? count : 0;
        }

        private static bool TryResolveUnitTile(SummonRequest request, out SummonTile tile)
        {
            tile = default;

            UnitDataLink dataLink = request.Prefab != null ? request.Prefab.GetComponent<UnitDataLink>() : null;
            UnitDataSO summonData = dataLink != null ? dataLink.UnitData : null;

            if (summonData == null)
            {
                return false;
            }

            const int radius = 1;
            SummonTileRequest tileRequest = new SummonTileRequest(request.UnitOwner, request.Prefab, request.Source, summonData, radius);
            return SummonTileService.TryGetTile(tileRequest, out tile);
        }

        private static bool InitializeUnitSummon(SummonRequest request, GameObject instance, bool hasResolvedPlacement, SummonTile tile)
        {
            UnitSummonRuntime summonRuntime = instance.GetComponent<UnitSummonRuntime>();

            if (summonRuntime == null)
            {
                return false;
            }

            return hasResolvedPlacement ? summonRuntime.InitializeSummon(request.UnitOwner, request.Source, tile) : summonRuntime.InitializeSummon(request.UnitOwner, request.Source);
        }

        private static bool InitializeEnemySummon(SummonRequest request, GameObject instance)
        {
            EnemySummonRuntime summonRuntime = instance.GetComponent<EnemySummonRuntime>();
            return summonRuntime != null && summonRuntime.InitializeSummon(request.EnemyOwner, request.Source);
        }

        private static void WarnInvalidPrefabOnce(GameObject prefab, bool unitRequest)
        {
            if (prefab == null || !invalidPrefabWarnings.Add(prefab.GetInstanceID()))
            {
                return;
            }

            string requiredComponent = unitRequest ? nameof(UnitSummonRuntime) : nameof(EnemySummonRuntime);
            Debug.LogError($"소환 프리팹 '{prefab.name}'에 {requiredComponent} 컴포넌트가 없거나 소환 초기화에 실패했습니다. 소환물 프리팹 구성을 확인하세요.", prefab);
        }

        private static void WarnUnavailablePlacementOnce(SummonRequest request)
        {
            int warningId = request.Source != null ? request.Source.GetInstanceID() : request.Prefab.GetInstanceID();

            if (!unavailablePlacementWarnings.Add(warningId))
            {
                return;
            }

            Debug.LogWarning($"치명타 소환 '{request.Prefab.name}'의 Provider는 연결되어 있지만 현재 주변에 유효한 소환 타일이 없습니다.", request.Prefab);
        }

        private static void WarnMissingPlacementProviderOnce(SummonRequest request)
        {
            int warningId = request.Source != null ? request.Source.GetInstanceID() : request.Prefab.GetInstanceID();

            if (!missingPlacementWarnings.Add(warningId))
            {
                return;
            }

            Debug.LogError($"치명타 소환 '{request.Prefab.name}'에 사용할 맵 소환 위치 Provider가 연결되지 않았습니다. ISummonTileProvider를 맵 시스템에서 등록해야 합니다.", request.Prefab);
        }
    }
}