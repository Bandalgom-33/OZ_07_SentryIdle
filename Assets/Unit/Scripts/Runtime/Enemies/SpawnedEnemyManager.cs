using System.Collections.Generic;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    // 필드에 생성(소환)된 적 유닛 목록 추적 및 사망 시 디스폰(오브젝트 파괴) 관리를 전담하는 매니저
    public sealed class SpawnedEnemyManager : MonoBehaviour
    {
        private static SpawnedEnemyManager _instance;

        public static SpawnedEnemyManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<SpawnedEnemyManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("[SpawnedEnemyManager]");
                        _instance = go.AddComponent<SpawnedEnemyManager>();
                    }
                }
                return _instance;
            }
        }

        [Header("디스폰 지연 시간 (초)")]
        [SerializeField] private float despawnDelay = 0.5f;

        private readonly HashSet<EnemyRuntimeState> _activeEnemies = new HashSet<EnemyRuntimeState>();

        public IReadOnlyCollection<EnemyRuntimeState> ActiveEnemies => _activeEnemies;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
        }

        // 스폰된 적 유닛 등록
        public void RegisterEnemy(EnemyRuntimeState enemy)
        {
            if (enemy != null && !_activeEnemies.Contains(enemy))
            {
                _activeEnemies.Add(enemy);
            }
        }

        // 유닛 파괴/제거 시 등록 해제
        public void UnregisterEnemy(EnemyRuntimeState enemy)
        {
            if (enemy != null)
            {
                _activeEnemies.Remove(enemy);
            }
        }

        // 적 사망 이벤트 수신 시 디스폰/파괴 연동
        private void OnEnemyDied(EnemyDiedEvent eventMessage)
        {
            if (eventMessage.enemyGameObject == null)
            {
                return;
            }

            EnemyRuntimeState enemyState = eventMessage.enemyGameObject.GetComponent<EnemyRuntimeState>();
            if (enemyState != null)
            {
                UnregisterEnemy(enemyState);
            }

            // 사망 애니메이션/연출 고려 지연 파괴 (필요 시 바로 Destroy(eventMessage.enemyGameObject) 변경 가능)
            if (despawnDelay > 0f)
            {
                Destroy(eventMessage.enemyGameObject, despawnDelay);
            }
            else
            {
                Destroy(eventMessage.enemyGameObject);
            }
        }
    }
}
