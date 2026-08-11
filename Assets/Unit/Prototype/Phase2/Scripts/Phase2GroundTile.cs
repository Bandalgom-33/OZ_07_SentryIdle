using UnityEngine;

namespace EndlessGuard.Unit.Prototype.Phase2
{
    public enum Phase2TileSurface
    {
        Ground = 0,
        HighGround = 1
    }

    [DisallowMultipleComponent]
    public sealed class Phase2GroundTile : MonoBehaviour
    {
        [SerializeField] private Vector2Int coordinate;
        [SerializeField] private Phase2TileSurface surface = Phase2TileSurface.Ground;

        public Vector2Int Coordinate => coordinate;
        public Phase2TileSurface Surface => surface;
        public Vector3 WorldPosition => transform.position + Vector3.up * (transform.lossyScale.y * 0.5f);

        private void OnDrawGizmos()
        {
            Gizmos.color = surface == Phase2TileSurface.HighGround
                ? new Color(0.85f, 0.65f, 0.2f, 0.8f)
                : new Color(0.25f, 0.75f, 0.35f, 0.8f);
            Gizmos.DrawWireCube(transform.position, new Vector3(0.9f, 0.08f, 0.9f));
        }
    }
}
