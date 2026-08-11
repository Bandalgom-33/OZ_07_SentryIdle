using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Runtime;
using UnityEngine;

namespace EndlessGuard.Unit.Prototype.Phase2
{
    [DisallowMultipleComponent]
    public sealed class Phase2EnemyRoute : MonoBehaviour
    {
        [Tooltip("지상 몬스터가 실제로 따라갈 Ground 타일 경로입니다. 언덕 타일을 넣으면 검증 실패합니다.")]
        [SerializeField] private List<Phase2GroundTile> routeTiles = new List<Phase2GroundTile>();

        public IReadOnlyList<Phase2GroundTile> RouteTiles => routeTiles;

        public bool ValidateGroundRoute(out string message)
        {
            if (routeTiles == null || routeTiles.Count < 2)
            {
                message = "지상 경로에는 최소 2개의 타일이 필요합니다.";
                return false;
            }

            for (int i = 0; i < routeTiles.Count; i++)
            {
                Phase2GroundTile tile = routeTiles[i];

                if (tile == null)
                {
                    message = $"지상 경로 {i}번 타일이 비어 있습니다.";
                    return false;
                }

                if (tile.Surface != Phase2TileSurface.Ground)
                {
                    message = $"지상 경로에 HighGround 타일 {tile.Coordinate}가 포함되어 있습니다.";
                    return false;
                }

                if (i > 0)
                {
                    Vector2Int delta = tile.Coordinate - routeTiles[i - 1].Coordinate;
                    int manhattan = Mathf.Abs(delta.x) + Mathf.Abs(delta.y);

                    if (manhattan != 1)
                    {
                        message = $"경로 타일 {routeTiles[i - 1].Coordinate} -> {tile.Coordinate}가 인접 1칸이 아닙니다.";
                        return false;
                    }
                }
            }

            message = $"지상 경로 PASS: {routeTiles.Count}개 Ground 타일";
            return true;
        }

        public bool BuildGroundPath(out PathNode[] path)
        {
            path = null;

            if (!ValidateGroundRoute(out _))
            {
                return false;
            }

            path = new PathNode[routeTiles.Count];

            for (int i = 0; i < routeTiles.Count; i++)
            {
                Phase2GroundTile current = routeTiles[i];
                GridFacingDirection facing = i + 1 < routeTiles.Count
                    ? ResolveFacing(current.Coordinate, routeTiles[i + 1].Coordinate)
                    : (i > 0 ? ResolveFacing(routeTiles[i - 1].Coordinate, current.Coordinate) : GridFacingDirection.North);

                path[i] = new PathNode(current.WorldPosition, current.Coordinate, facing);
            }

            return true;
        }

        public bool BuildAirPath(float airHeight, out PathNode[] path)
        {
            path = null;

            if (routeTiles == null || routeTiles.Count < 2 || routeTiles[0] == null || routeTiles[routeTiles.Count - 1] == null)
            {
                return false;
            }

            Phase2GroundTile start = routeTiles[0];
            Phase2GroundTile goal = routeTiles[routeTiles.Count - 1];
            GridFacingDirection facing = ResolveFacing(start.Coordinate, goal.Coordinate);

            path = new[]
            {
                new PathNode(start.WorldPosition + Vector3.up * airHeight, start.Coordinate, facing),
                new PathNode(goal.WorldPosition + Vector3.up * airHeight, goal.Coordinate, facing)
            };

            return true;
        }

        private static GridFacingDirection ResolveFacing(Vector2Int from, Vector2Int to)
        {
            Vector2Int delta = to - from;

            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y) && delta.x != 0)
            {
                return delta.x > 0 ? GridFacingDirection.East : GridFacingDirection.West;
            }

            return delta.y >= 0 ? GridFacingDirection.North : GridFacingDirection.South;
        }

        private void OnDrawGizmos()
        {
            if (routeTiles == null || routeTiles.Count < 2)
            {
                return;
            }

            Gizmos.color = Color.cyan;

            for (int i = 1; i < routeTiles.Count; i++)
            {
                if (routeTiles[i - 1] != null && routeTiles[i] != null)
                {
                    Gizmos.DrawLine(routeTiles[i - 1].WorldPosition + Vector3.up * 0.15f, routeTiles[i].WorldPosition + Vector3.up * 0.15f);
                }
            }
        }
    }
}
