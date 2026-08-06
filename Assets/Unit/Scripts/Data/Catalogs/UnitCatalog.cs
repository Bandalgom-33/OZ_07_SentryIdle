using System;
using System.Collections.Generic;
using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [CreateAssetMenu(fileName = "UnitCatalog", menuName = "Endless Guard/Catalog/Unit Catalog")]
    public sealed class UnitCatalog : ScriptableObject
    {
        [SerializeField, HideInInspector] private int lastIssuedNumber;

        [Header("캐릭터 데이터 목록")]
        [Tooltip("게임에 존재하는 모든 캐릭터 데이터 목록입니다. 제작 도구에서 신규 캐릭터 데이터를 생성할 때 자동으로 등록합니다.")]
        [SerializeField] private List<UnitDataSO> units = new List<UnitDataSO>();

        public int LastIssuedNumber => lastIssuedNumber;
        public IReadOnlyList<UnitDataSO> Units => units;

        public bool TryGetById(string unitId, out UnitDataSO unitData)
        {
            if (string.IsNullOrWhiteSpace(unitId))
            {
                unitData = null;
                return false;
            }

            for (int i = 0; i < units.Count; i++)
            {
                UnitDataSO current = units[i];

                if (current != null && string.Equals(current.UnitId, unitId, StringComparison.Ordinal))
                {
                    unitData = current;
                    return true;
                }
            }

            unitData = null;
            return false;
        }
    }
}