using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    public readonly struct SummonTileRequest
    {
        public UnitRuntimeState Owner { get; }
        public GameObject Prefab { get; }
        public Object Source { get; }
        public UnitDataSO SummonData { get; }
        public int Radius { get; }

        public SummonTileRequest(UnitRuntimeState owner, GameObject prefab, Object source, UnitDataSO summonData, int radius)
        {
            Owner = owner;
            Prefab = prefab;
            Source = source;
            SummonData = summonData;
            Radius = Mathf.Max(1, radius);
        }
    }

    public readonly struct SummonTile
    {
        public Vector3 WorldPosition { get; }
        public Vector2Int TileCoordinate { get; }

        public SummonTile(Vector3 worldPosition, Vector2Int tileCoordinate)
        {
            WorldPosition = worldPosition;
            TileCoordinate = tileCoordinate;
        }
    }

    public interface ISummonTileProvider
    {
        bool TryGetTile(SummonTileRequest request, out SummonTile tile);
    }

    public static class SummonTileService
    {
        private static ISummonTileProvider provider;

        public static bool HasProvider => provider != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            provider = null;
        }

        public static void Register(ISummonTileProvider tileProvider)
        {
            if (tileProvider == null)
            {
                return;
            }

            provider = tileProvider;
        }

        public static void Unregister(ISummonTileProvider tileProvider)
        {
            if (tileProvider == null || !object.ReferenceEquals(provider, tileProvider))
            {
                return;
            }

            provider = null;
        }

        public static bool TryGetTile(SummonTileRequest request, out SummonTile tile)
        {
            tile = default;
            return provider != null && provider.TryGetTile(request, out tile);
        }
    }
}