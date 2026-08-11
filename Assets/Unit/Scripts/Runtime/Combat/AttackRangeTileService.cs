using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    public readonly struct AttackRangeTile
    {
        public Vector3 WorldPosition { get; }
        public Vector3 Scale { get; }

        public AttackRangeTile(Vector3 worldPosition, Vector3 scale)
        {
            WorldPosition = worldPosition;
            Scale = scale;
        }
    }

    public interface IAttackRangeTileProvider
    {
        bool TryGetAttackRangeTile(Vector2Int tileCoordinate, out AttackRangeTile tile);
    }

    public static class AttackRangeTileService
    {
        private static IAttackRangeTileProvider provider;

        public static bool HasProvider => provider != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            provider = null;
        }

        public static void Register(IAttackRangeTileProvider tileProvider)
        {
            if (tileProvider == null)
            {
                return;
            }

            provider = tileProvider;

            if (AttackRangeDisplay.SelectedUnit != null)
            {
                AttackRangeDisplay.RefreshSelected();
            }
        }

        public static void Unregister(IAttackRangeTileProvider tileProvider)
        {
            if (tileProvider == null || !object.ReferenceEquals(provider, tileProvider))
            {
                return;
            }

            provider = null;
            AttackRangeDisplay.Hide();
        }

        public static bool TryGetTile(Vector2Int tileCoordinate, out AttackRangeTile tile)
        {
            tile = default;
            return provider != null && provider.TryGetAttackRangeTile(tileCoordinate, out tile);
        }
    }
}
