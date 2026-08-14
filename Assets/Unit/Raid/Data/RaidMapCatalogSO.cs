using System;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Data
{
    [CreateAssetMenu(fileName = "RaidMapCatalog", menuName = "Endless Guard/Raid/Map Catalog")]
    public sealed class RaidMapCatalogSO : ScriptableObject
    {
        [SerializeField] private RaidMapFamilySO[] families = Array.Empty<RaidMapFamilySO>();

        public int FamilyCount => families != null ? families.Length : 0;

        public RaidMapFamilySO GetFamily(int index)
        {
            if (index < 0 || index >= FamilyCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Raid Map Family 범위를 벗어났습니다.");
            }

            return families[index];
        }

        public RaidMapFamilySO FindFamily(string familyId)
        {
            if (string.IsNullOrWhiteSpace(familyId))
            {
                return null;
            }

            for (int i = 0; i < FamilyCount; i++)
            {
                RaidMapFamilySO family = families[i];

                if (family != null && string.Equals(family.FamilyId, familyId, StringComparison.Ordinal))
                {
                    return family;
                }
            }

            return null;
        }
    }
}
