using System;
using System.Collections.Generic;
using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [CreateAssetMenu(fileName = "EnemyCatalog", menuName = "Endless Guard/Catalog/Enemy Catalog")]
    public sealed class EnemyCatalog : ScriptableObject
    {
        [SerializeField, HideInInspector] private int lastIssuedNumber;

        [Header("몬스터 데이터 목록")]
        [Tooltip("게임에 존재하는 모든 몬스터 데이터 목록입니다. 제작 도구에서 신규 몬스터 데이터를 생성할 때 자동으로 등록합니다.")]
        [SerializeField] private List<EnemyDataSO> enemies = new List<EnemyDataSO>();

        public int LastIssuedNumber => lastIssuedNumber;
        public IReadOnlyList<EnemyDataSO> Enemies => enemies;

        public bool TryGetById(string enemyId, out EnemyDataSO enemyData)
        {
            if (string.IsNullOrWhiteSpace(enemyId))
            {
                enemyData = null;
                return false;
            }

            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyDataSO current = enemies[i];

                if (current != null && string.Equals(current.EnemyId, enemyId, StringComparison.Ordinal))
                {
                    enemyData = current;
                    return true;
                }
            }

            enemyData = null;
            return false;
        }
    }
}