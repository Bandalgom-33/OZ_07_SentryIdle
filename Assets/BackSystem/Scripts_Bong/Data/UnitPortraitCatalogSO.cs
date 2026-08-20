using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [Serializable]
    public struct UnitPortraitMapping
    {
        public UnitDataSO unitData;
        public Sprite portraitIcon;
        public Sprite fullIllustration;
    }

    [CreateAssetMenu(fileName = "UnitPortraitCatalog", menuName = "Endless Guard/Catalog/Unit Portrait Catalog")]
    public sealed class UnitPortraitCatalogSO : ScriptableObject
    {
        #region 직렬화 변수

        [Header("--- 팀원 캐릭터 카탈로그 ---")]
        [Tooltip("팀원이 관리하는 원본 UnitCatalog SO 참조")]
        [SerializeField] private UnitCatalog targetCatalog;

        [Header("--- 유닛 초상화 매핑 리스트 (자동 동기화) ---")]
        [Tooltip("UnitCatalog의 모든 유닛에 대응하는 초상화 매핑 리스트")]
        [SerializeField] private List<UnitPortraitMapping> portraitMappings = new List<UnitPortraitMapping>();

        #endregion

        #region 프로퍼티

        public IReadOnlyList<UnitPortraitMapping> PortraitMappings => portraitMappings;

        #endregion

        #region 에디터 동기화

        // 인스펙터 변경 시 자동 동기화 처리
        private void OnValidate()
        {
            SyncWithCatalog();
        }

        // 카탈로그 데이터 기반 초상화 매핑 동기화
        public void SyncWithCatalog()
        {
            if (targetCatalog == null || targetCatalog.Units == null) return;
            
            Dictionary<string, UnitPortraitMapping> existingMap = new Dictionary<string, UnitPortraitMapping>();
            for (int i = 0; i < portraitMappings.Count; i++)
            {
                UnitPortraitMapping mapping = portraitMappings[i];
                if (mapping.unitData != null && !string.IsNullOrEmpty(mapping.unitData.UnitId))
                {
                    existingMap[mapping.unitData.UnitId] = mapping;
                }
            }

            List<UnitPortraitMapping> syncedList = new List<UnitPortraitMapping>();
            for (int i = 0; i < targetCatalog.Units.Count; i++)
            {
                UnitDataSO unitSO = targetCatalog.Units[i];
                if (unitSO == null) continue;

                if (existingMap.TryGetValue(unitSO.UnitId, out UnitPortraitMapping existingMapping))
                {
                    existingMapping.unitData = unitSO;
                    syncedList.Add(existingMapping);
                }
                else
                {
                    syncedList.Add(new UnitPortraitMapping
                    {
                        unitData = unitSO,
                        portraitIcon = null,
                        fullIllustration = null
                    });
                }
            }

            portraitMappings = syncedList;
        }

        #endregion

        #region 조회 메서드

        // 유닛 데이터 기반 초상화 이미지 조회
        public Sprite GetPortraitByUnitData(UnitDataSO unitData)
        {
            if (unitData == null) return null;

            for (int i = 0; i < portraitMappings.Count; i++)
            {
                if (portraitMappings[i].unitData == unitData)
                {
                    return portraitMappings[i].portraitIcon;
                }
            }

            return null;
        }

        // 유닛 ID 기반 초상화 이미지 조회
        public Sprite GetPortraitByUnitId(string unitId)
        {
            if (string.IsNullOrEmpty(unitId)) return null;

            for (int i = 0; i < portraitMappings.Count; i++)
            {
                if (portraitMappings[i].unitData != null && string.Equals(portraitMappings[i].unitData.UnitId, unitId, StringComparison.Ordinal))
                {
                    return portraitMappings[i].portraitIcon;
                }
            }

            return null;
        }

        #endregion
    }
}
