using System;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Data
{
    [CreateAssetMenu(fileName = "RaidMapConfig", menuName = "Endless Guard/Raid/Map Config")]
    public sealed class RaidMapConfigSO : ScriptableObject
    {
        [Header("공통")]
        [Min(0.01f)]
        [SerializeField] private float tileSize = 2f;

        [Header("맵")]
        [SerializeField] private RaidMapCatalogSO catalog;
        [SerializeField] private RaidMapFamilySO defaultFamily;

        public float TileSize => tileSize;
        public RaidMapCatalogSO Catalog => catalog;
        public RaidMapFamilySO DefaultFamily => defaultFamily;

        public RaidMapFamilySO GetFamily(string familyId)
        {
            if (string.IsNullOrWhiteSpace(familyId))
            {
                throw new ArgumentException("Raid Map Family ID가 비어 있습니다.", nameof(familyId));
            }

            if (catalog == null)
            {
                throw new InvalidOperationException("Raid Map Catalog가 연결되지 않았습니다.");
            }

            RaidMapFamilySO family = catalog.FindFamily(familyId);

            if (family == null)
            {
                throw new InvalidOperationException($"Raid Map Family를 찾을 수 없습니다. ID: {familyId}");
            }

            return family;
        }

        public Vector3 GetCenteredOrigin(int width, int height)
        {
            if (width < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(width), width, "맵 Width는 1 이상이어야 합니다.");
            }

            if (height < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(height), height, "맵 Height는 1 이상이어야 합니다.");
            }

            float x = -(width - 1) * tileSize * 0.5f;
            float z = -(height - 1) * tileSize * 0.5f;
            return new Vector3(x, 0f, z);
        }
    }
}
