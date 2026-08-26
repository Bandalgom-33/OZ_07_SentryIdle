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
        private const uint MarkerSalt = 0x63D83595u;

        public static GameObject PickSurface(RaidTileVisualSetSO visualSet, RaidTile tile, Vector2Int coordinate, int visualKey)
        {
            if (visualSet == null)
            {
                throw new ArgumentNullException(nameof(visualSet));
            }

            if (tile.SurfaceVisual != RaidTileSurfaceVisual.Auto)
            {
                GameObject fixedPrefab = visualSet.GetSurfaceVisualPrefab(tile.SurfaceVisual);

                if (fixedPrefab == null)
                {
                    throw new InvalidOperationException($"Raid 고정 Surface Visual Prefab이 없습니다. Visual: {tile.SurfaceVisual}");
                }

                return fixedPrefab;
            }

            return Pick(visualSet.GetSurfacePrefabs(tile.Surface), coordinate, visualKey, SurfaceSalt);
        }

        public static GameObject PickBlock(RaidTileVisualSetSO visualSet, RaidTileBlock block, Vector2Int coordinate, int visualKey)
        {
            if (visualSet == null)
            {
                throw new ArgumentNullException(nameof(visualSet));
            }

            return Pick(visualSet.GetBlockPrefabs(block), coordinate, visualKey, BlockSalt);
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
