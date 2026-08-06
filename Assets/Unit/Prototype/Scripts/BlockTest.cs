using EndlessGuard.Unit.Runtime;
using UnityEngine;

namespace EndlessGuard.Unit.Prototype
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CombatStatePrototypeController))]
    public sealed class BlockTest : MonoBehaviour
    {
        [Header("검증 대상 연결")]
        [Tooltip("캐릭터와 첫 번째 몬스터를 생성하는 기존 검증 컴포넌트입니다.")]
        [SerializeField] private CombatStatePrototypeController state;

        [Header("추가 몬스터 배치")]
        [Tooltip("두 번째 몬스터가 첫 번째 몬스터로부터 떨어질 월드 위치입니다.")]
        [SerializeField] private Vector3 secondEnemyOffset = new Vector3(1.5f, 0f, 0f);

        [Tooltip("세 번째 몬스터가 첫 번째 몬스터로부터 떨어질 월드 위치입니다.")]
        [SerializeField] private Vector3 thirdEnemyOffset = new Vector3(3f, 0f, 0f);

        [HideInInspector]
        [SerializeField] private UnitBlock unitBlock;

        [HideInInspector]
        [SerializeField] private EnemyBlock firstBlock;

        [HideInInspector]
        [SerializeField] private EnemyBlock secondBlock;

        [HideInInspector]
        [SerializeField] private EnemyBlock thirdBlock;

        [HideInInspector]
        [SerializeField] private GameObject secondEnemyObject;

        [HideInInspector]
        [SerializeField] private GameObject thirdEnemyObject;

        [HideInInspector]
        [SerializeField] private bool lastResult;

        [HideInInspector]
        [TextArea(2, 4)]
        [SerializeField] private string lastMessage;

        public UnitBlock UnitBlock => unitBlock;
        public EnemyBlock FirstBlock => firstBlock;
        public EnemyBlock SecondBlock => secondBlock;
        public EnemyBlock ThirdBlock => thirdBlock;
        public bool LastResult => lastResult;
        public string LastMessage => lastMessage;

        private void Reset()
        {
            state = GetComponent<CombatStatePrototypeController>();
        }

        private void OnValidate()
        {
            if (state == null)
            {
                state = GetComponent<CombatStatePrototypeController>();
            }
        }

        private void OnDisable()
        {
            if (Application.isPlaying)
            {
                CleanupExtras();
            }
        }

        public void Setup()
        {
            CleanupExtras();

            if (state == null)
            {
                lastMessage = "CombatStatePrototypeController가 연결되지 않았습니다.";
                Debug.LogError(lastMessage, this);
                return;
            }

            state.SpawnActors();

            if (state.SpawnedUnit == null || state.SpawnedEnemy == null)
            {
                lastMessage = "검증용 캐릭터 또는 첫 번째 몬스터가 생성되지 않았습니다.";
                Debug.LogError(lastMessage, this);
                return;
            }

            unitBlock = state.SpawnedUnit.GetComponent<UnitBlock>();
            firstBlock = state.SpawnedEnemy.GetComponent<EnemyBlock>();

            if (unitBlock == null || firstBlock == null)
            {
                lastMessage = "생성된 프리팹에서 UnitBlock 또는 EnemyBlock을 찾지 못했습니다.";
                Debug.LogError(lastMessage, this);
                return;
            }

            GameObject enemyPrefab = state.SpawnedEnemy.DataLink.EnemyData.EnemyPrefab;

            if (enemyPrefab == null)
            {
                lastMessage = "첫 번째 몬스터 데이터에 연결된 프리팹이 없습니다.";
                Debug.LogError(lastMessage, this);
                return;
            }

            Vector3 firstPosition = state.SpawnedEnemy.transform.position;
            secondEnemyObject = Instantiate(enemyPrefab, firstPosition + secondEnemyOffset, Quaternion.identity, transform);
            thirdEnemyObject = Instantiate(enemyPrefab, firstPosition + thirdEnemyOffset, Quaternion.identity, transform);
            secondBlock = secondEnemyObject.GetComponent<EnemyBlock>();
            thirdBlock = thirdEnemyObject.GetComponent<EnemyBlock>();

            if (secondBlock == null || thirdBlock == null)
            {
                lastMessage = "추가 몬스터에서 EnemyBlock을 찾지 못했습니다.";
                Debug.LogError(lastMessage, this);
                CleanupExtras();
                return;
            }

            lastResult = true;
            lastMessage = $"저지 검증 준비 완료: 최대 {unitBlock.MaxCount}마리, 현재 {unitBlock.Count}마리";
            Debug.Log(lastMessage, this);
        }

        public void BindFirst()
        {
            Bind(firstBlock, "첫 번째 몬스터");
        }

        public void BindSecond()
        {
            Bind(secondBlock, "두 번째 몬스터");
        }

        public void BindThird()
        {
            Bind(thirdBlock, "세 번째 몬스터");
        }

        public void ReleaseFirst()
        {
            Release(firstBlock, "첫 번째 몬스터");
        }

        public void ReleaseSecond()
        {
            Release(secondBlock, "두 번째 몬스터");
        }

        public void ReleaseThird()
        {
            Release(thirdBlock, "세 번째 몬스터");
        }

        public void ReleaseAll()
        {
            if (unitBlock == null)
            {
                lastMessage = "저지 검증이 준비되지 않았습니다.";
                return;
            }

            unitBlock.ReleaseAll();
            lastResult = true;
            lastMessage = $"전체 저지 해제 완료: 현재 {unitBlock.Count}마리";
            Debug.Log(lastMessage, this);
        }

        public void KillFirst()
        {
            KillEnemy(firstBlock, "첫 번째 몬스터");
        }

        public void KillSecond()
        {
            KillEnemy(secondBlock, "두 번째 몬스터");
        }

        public void KillUnit()
        {
            if (state == null || state.SpawnedUnit == null || state.SpawnedUnit.Health == null)
            {
                lastMessage = "사망시킬 캐릭터가 없습니다.";
                return;
            }

            state.SpawnedUnit.ApplyDamage(state.SpawnedUnit.Health.CurrentHp);
            lastResult = state.SpawnedUnit.Health.IsDead;
            lastMessage = $"캐릭터 사망 처리: 현재 저지 {GetBlockCount()}마리";
            Debug.Log(lastMessage, state.SpawnedUnit);
        }

        public void CleanupExtras()
        {
            if (unitBlock != null)
            {
                unitBlock.ReleaseAll();
            }

            if (secondEnemyObject != null)
            {
                Destroy(secondEnemyObject);
            }

            if (thirdEnemyObject != null)
            {
                Destroy(thirdEnemyObject);
            }

            unitBlock = null;
            firstBlock = null;
            secondBlock = null;
            thirdBlock = null;
            secondEnemyObject = null;
            thirdEnemyObject = null;
        }

        private void Bind(EnemyBlock enemy, string targetName)
        {
            if (unitBlock == null || enemy == null)
            {
                lastResult = false;
                lastMessage = $"{targetName} 저지 실패: 검증 대상이 준비되지 않았습니다.";
                return;
            }

            lastResult = BlockLink.TryBind(unitBlock, enemy);
            lastMessage = $"{targetName} 저지 {(lastResult ? "성공" : "실패")}: 현재 {unitBlock.Count} / {unitBlock.MaxCount}";
            Debug.Log(lastMessage, this);
        }

        private void Release(EnemyBlock enemy, string targetName)
        {
            if (enemy == null)
            {
                lastResult = false;
                lastMessage = $"{targetName} 해제 실패: 검증 대상이 없습니다.";
                return;
            }

            lastResult = BlockLink.Release(enemy);
            lastMessage = $"{targetName} 저지 해제 {(lastResult ? "성공" : "실패")}: 현재 {GetBlockCount()}마리";
            Debug.Log(lastMessage, this);
        }

        private void KillEnemy(EnemyBlock enemy, string targetName)
        {
            if (enemy == null)
            {
                lastResult = false;
                lastMessage = $"{targetName} 사망 실패: 검증 대상이 없습니다.";
                return;
            }

            EnemyRuntimeState enemyState = enemy.GetComponent<EnemyRuntimeState>();

            if (enemyState == null || enemyState.Health == null)
            {
                lastResult = false;
                lastMessage = $"{targetName}에서 EnemyRuntimeState를 찾지 못했습니다.";
                return;
            }

            enemyState.ApplyDamage(enemyState.Health.CurrentHp);
            lastResult = enemyState.Health.IsDead;
            lastMessage = $"{targetName} 사망 처리: 현재 저지 {GetBlockCount()}마리";
            Debug.Log(lastMessage, enemyState);
        }

        private int GetBlockCount()
        {
            return unitBlock == null ? 0 : unitBlock.Count;
        }
    }
}