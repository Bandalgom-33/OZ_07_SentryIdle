using System.Collections.Generic;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    // 필드에 생성된 적 유닛 목록을 추적하고, 사망 시 객체별 디스폰 정책을 실행하는 매니저
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

        public static bool TryGetExisting(out SpawnedEnemyManager manager)
        {
            if (_instance != null)
            {
                manager = _instance;
                return true;
            }

            manager = FindFirstObjectByType<SpawnedEnemyManager>();
            if (manager == null)
            {
                return false;
            }

            _instance = manager;
            return true;
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

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
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

                if (enemyState.SummonRuntime != null)
                {
                    return;
                }
            }

            EnemyDespawnHandler despawnHandler = eventMessage.enemyGameObject.GetComponent<EnemyDespawnHandler>();
            if (despawnHandler != null)
            {
                despawnHandler.Despawn(despawnDelay);
                return;
            }

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
