using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Raid.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    public static class RaidTileVisualPicker
    {
        private const uint SurfaceSalt = 0xA511E9B3u;
        private const uint BlockSalt = 0x71E38A59u;
        private const uint RouteSalt = 0xC2B2AE35u;
        private const uint MarkerSalt = 0x63D83595u;

        public static GameObject PickSurface(RaidTileVisualSetSO visualSet, RaidTileSurface surface, Vector2Int coordinate, int visualKey)
        {
            if (visualSet == null)
            {
                throw new ArgumentNullException(nameof(visualSet));
            }

            return Pick(visualSet.GetSurfacePrefabs(surface), coordinate, visualKey, SurfaceSalt);
        }

        public static GameObject PickBlock(RaidTileVisualSetSO visualSet, RaidTileBlock block, Vector2Int coordinate, int visualKey)
        {
            if (visualSet == null)
            {
                throw new ArgumentNullException(nameof(visualSet));
            }

            return Pick(visualSet.GetBlockPrefabs(block), coordinate, visualKey, BlockSalt);
        }

        public static GameObject PickRoute(RaidTileVisualSetSO visualSet, RaidTileRoute route, Vector2Int coordinate, int visualKey)
        {
            if (visualSet == null)
            {
                throw new ArgumentNullException(nameof(visualSet));
            }

            return Pick(visualSet.GetRoutePrefabs(route), coordinate, visualKey, RouteSalt);
        }

        public static GameObject PickMarker(RaidTileVisualSetSO visualSet, RaidTileMarker marker, Vector2Int coordinate, int visualKey)
        {
            if (visualSet == null)
            {
                throw new ArgumentNullException(nameof(visualSet));
            }

            return Pick(visualSet.GetMarkerPrefabs(marker), coordinate, visualKey, MarkerSalt);
        }

        private static GameObject Pick(IReadOnlyList<GameObject> prefabs, Vector2Int coordinate, int visualKey, uint salt)
        {
            if (prefabs == null || prefabs.Count == 0)
            {
                return null;
            }

            uint hash = unchecked((uint)visualKey);
            hash ^= unchecked((uint)coordinate.x) * 0x9E3779B9u;
            hash ^= unchecked((uint)coordinate.y) * 0x85EBCA6Bu;
            hash ^= salt;
            hash ^= hash >> 16;
            hash *= 0x7FEB352Du;
            hash ^= hash >> 15;
            hash *= 0x846CA68Bu;
            hash ^= hash >> 16;

            int index = (int)(hash % (uint)prefabs.Count);
            return prefabs[index];
        }
    }
}
