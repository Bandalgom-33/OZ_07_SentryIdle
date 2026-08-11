using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    [DisallowMultipleComponent]
    public sealed class EnemyDataLink : MonoBehaviour
    {
        [Header("몬스터 데이터 연결")]
        [Tooltip("이 몬스터 프리팹의 원본 정적 데이터를 보관하는 EnemyDataSO입니다.")]
        [SerializeField] private EnemyDataSO enemyData;

        public EnemyDataSO EnemyData => enemyData;
        public string EnemyId => enemyData == null ? string.Empty : enemyData.EnemyId;
        public bool HasData => enemyData != null;
    }
}