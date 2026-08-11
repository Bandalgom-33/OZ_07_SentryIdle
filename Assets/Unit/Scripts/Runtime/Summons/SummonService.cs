using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    public static class SummonService
    {
        private static readonly HashSet<int> invalidPrefabWarnings = new HashSet<int>();
        private static readonly HashSet<int> missingPlacementWarnings = new HashSet<int>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            invalidPrefabWarnings.Clear();
            missingPlacementWarnings.Clear();
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
                        WarnMissingPlacementProviderOnce(request);
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

        private static bool TryResolveUnitTile(SummonRequest request, out SummonTile tile)
        {
            tile = default;

            UnitDataLink dataLink = request.Prefab != null ? request.Prefab.GetComponent<UnitDataLink>() : null;
            UnitDataSO summonData = dataLink != null ? dataLink.UnitData : null;

            if (summonData == null)
            {
                return false;
            }

            int radius = request.Source is CritSummonSO ? 1 : 1;
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