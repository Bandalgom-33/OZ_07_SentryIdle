using System;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    public sealed class RaidGridSpace
    {
        private readonly Transform root;

        public Vector3 LocalOrigin { get; }
        public float TileSize { get; }

        public RaidGridSpace(Transform root, Vector3 localOrigin, float tileSize)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            if (float.IsNaN(tileSize) || float.IsInfinity(tileSize) || tileSize <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(tileSize), tileSize, "레이드 타일 크기는 0보다 큰 유한한 값이어야 합니다.");
            }

            this.root = root;
            LocalOrigin = localOrigin;
            TileSize = tileSize;
        }

        public Vector3 TileToWorld(Vector2Int coordinate)
        {
            return TileToWorld(coordinate, 0f);
        }

        public Vector3 TileToWorld(Vector2Int coordinate, float heightOffset)
        {
            Vector3 localPosition = LocalOrigin + new Vector3(coordinate.x * TileSize, heightOffset, coordinate.y * TileSize);
            return root.TransformPoint(localPosition);
        }

        public Vector2Int WorldToTile(Vector3 worldPosition)
        {
            Vector3 localPosition = root.InverseTransformPoint(worldPosition) - LocalOrigin;
            int x = Mathf.RoundToInt(localPosition.x / TileSize);
            int y = Mathf.RoundToInt(localPosition.z / TileSize);
            return new Vector2Int(x, y);
        }
    }
}