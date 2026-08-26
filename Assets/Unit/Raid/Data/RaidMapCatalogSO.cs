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

        public RaidMapFamilySO GetRandomCompleteFamily()
        {
            int completeCount = 0;

            for (int i = 0; i < FamilyCount; i++)
            {
                RaidMapFamilySO family = families[i];

                if (family != null && family.IsComplete)
                {
                    completeCount++;
                }
            }

            if (completeCount == 0)
            {
                throw new InvalidOperationException("완성된 Raid Map Family가 Catalog에 없습니다.");
            }

            int selectedIndex = UnityEngine.Random.Range(0, completeCount);

            for (int i = 0; i < FamilyCount; i++)
            {
                RaidMapFamilySO family = families[i];

                if (family == null || !family.IsComplete)
                {
                    continue;
                }

                if (selectedIndex == 0)
                {
                    return family;
                }

                selectedIndex--;
            }

            throw new InvalidOperationException("Raid Map Family 랜덤 선택에 실패했습니다.");
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
