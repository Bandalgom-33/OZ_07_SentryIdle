using System;
using EndlessGuard.Unit.Raid.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    public sealed class RaidGrid
    {
        private readonly RaidTile[] tiles;

        public int Width { get; }
        public int Height { get; }
        public int Count => tiles.Length;

        public RaidGrid(int width, int height)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), width, "레이드 그리드 너비는 1 이상이어야 합니다.");
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height), height, "레이드 그리드 높이는 1 이상이어야 합니다.");
            }

            Width = width;
            Height = height;
            tiles = new RaidTile[checked(width * height)];
        }

        public bool IsInside(Vector2Int coordinate)
        {
            return coordinate.x >= 0 && coordinate.x < Width && coordinate.y >= 0 && coordinate.y < Height;
        }

        public bool TryGetTile(Vector2Int coordinate, out RaidTile tile)
        {
            if (!IsInside(coordinate))
            {
                tile = default;
                return false;
            }

            tile = tiles[GetIndex(coordinate)];
            return true;
        }

        public RaidTile GetTile(Vector2Int coordinate)
        {
            ValidateCoordinate(coordinate);
            return tiles[GetIndex(coordinate)];
        }

        public void SetTile(Vector2Int coordinate, RaidTile tile)
        {
            ValidateCoordinate(coordinate);
            tiles[GetIndex(coordinate)] = tile;
        }

        private int GetIndex(Vector2Int coordinate)
        {
            return coordinate.y * Width + coordinate.x;
        }

        private void ValidateCoordinate(Vector2Int coordinate)
        {
            if (!IsInside(coordinate))
            {
                throw new ArgumentOutOfRangeException(nameof(coordinate), coordinate, $"레이드 그리드 범위를 벗어난 좌표입니다. Width: {Width}, Height: {Height}");
            }
        }
    }
}