using EndlessGuard.Unit.Runtime;
using UnityEngine;

namespace EndlessGuard.Unit.Prototype
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BlockTest))]
    public sealed class AttackTest : MonoBehaviour
    {
        private const int EnemyCount = 3;

        [Header("검증 대상 연결")]
        [Tooltip("자동 이동과 저지 결과를 제공하는 검증 컴포넌트입니다.")]
        [SerializeField] private BlockTest blockTest;

        [Header("자동 공격 검증")]
        [Tooltip("두 저지 몬스터가 공격하지 못했을 때 검증을 종료할 제한 시간입니다.")]
        [Min(0.1f)]
        [SerializeField] private float timeoutSeconds = 3f;

        [HideInInspector]
        [SerializeField] private EnemyAttack[] attacks = new EnemyAttack[EnemyCount];

        [HideInInspector]
        [SerializeField] private UnitRuntimeState target;

        [HideInInspector]
        [SerializeField] private bool isReady;

        [HideInInspector]
        [SerializeField] private bool isRunning;

        [HideInInspector]
        [SerializeField] private float elapsedSeconds;

        [HideInInspector]
        [SerializeField] private float startHp;

        [HideInInspector]
        [SerializeField] private float currentHp;

        [HideInInspector]
        [SerializeField] private int firstAttackCount;

        [HideInInspector]
        [SerializeField] private int secondAttackCount;

        [HideInInspector]
        [SerializeField] private int thirdAttackCount;

        [HideInInspector]
        [SerializeField] private bool finalPassed;

        [HideInInspector]
        [TextArea(2, 4)]
        [SerializeField] private string message;

        public UnitRuntimeState Target => target;
        public bool IsReady => isReady;
        public bool IsRunning => isRunning;
        public float ElapsedSeconds => elapsedSeconds;
        public float StartHp => startHp;
        public float CurrentHp => currentHp;
        public float AppliedDamage => Mathf.Max(0f, startHp - currentHp);
        public int FirstAttackCount => firstAttackCount;
        public int SecondAttackCount => secondAttackCount;
        public int ThirdAttackCount => thirdAttackCount;
        public bool FinalPassed => finalPassed;
        public string Message => message;

        private void Reset()
        {
            blockTest = GetComponent<BlockTest>();
        }

        private void OnValidate()
        {
            if (blockTest == null)
            {
                blockTest = GetComponent<BlockTest>();
            }

            timeoutSeconds = Mathf.Max(0.1f, timeoutSeconds);
        }

        private void Update()
        {
            if (!isRunning)
            {
                return;
            }

            elapsedSeconds += Time.deltaTime;

            if (attacks[0] != null && attacks[0].Step(Time.deltaTime))
            {
                firstAttackCount++;
            }

            if (attacks[1] != null && attacks[1].Step(Time.deltaTime))
            {
                secondAttackCount++;
            }

            if (attacks[2] != null && attacks[2].Step(Time.deltaTime))
            {
                thirdAttackCount++;
            }

            currentHp = target == null || target.Health == null ? 0f : target.Health.CurrentHp;

            if (firstAttackCount > 0 && secondAttackCount > 0)
            {
                CompleteTest();
                return;
            }

            if (elapsedSeconds >= timeoutSeconds)
            {
                FailTest("자동 공격 제한 시간 안에 두 저지 몬스터가 모두 공격하지 못했습니다.");
            }
        }

        private void OnDisable()
        {
            isRunning = false;
        }

        public void SetupTest()
        {
            ResetResult();

            if (blockTest == null)
            {
                FailTest("BlockTest가 연결되지 않았습니다.");
                return;
            }

            if (!blockTest.AutoMovePassed)
            {
                FailTest("자동 이동 저지 검증을 먼저 완료해야 합니다.");
                return;
            }

            if (blockTest.UnitBlock == null || blockTest.UnitBlock.State == null)
            {
                FailTest("자동 공격 대상 캐릭터를 찾지 못했습니다.");
                return;
            }

            if (blockTest.FirstBlock == null || blockTest.SecondBlock == null || blockTest.ThirdBlock == null)
            {
                FailTest("자동 공격 검증에 필요한 몬스터 3마리를 찾지 못했습니다.");
                return;
            }

            if (!blockTest.FirstBlock.IsBlocked || !blockTest.SecondBlock.IsBlocked || blockTest.ThirdBlock.IsBlocked)
            {
                FailTest("첫 번째와 두 번째 몬스터만 저지된 상태가 아닙니다.");
                return;
            }

            target = blockTest.UnitBlock.State;
            attacks[0] = blockTest.FirstBlock.GetComponent<EnemyAttack>();
            attacks[1] = blockTest.SecondBlock.GetComponent<EnemyAttack>();
            attacks[2] = blockTest.ThirdBlock.GetComponent<EnemyAttack>();

            if (attacks[0] == null || attacks[1] == null || attacks[2] == null)
            {
                FailTest("검증 몬스터에서 EnemyAttack을 찾지 못했습니다.");
                return;
            }

            if (target.Health == null || target.Health.IsDead)
            {
                FailTest("공격 대상 캐릭터가 없거나 이미 사망했습니다.");
                return;
            }

            startHp = target.Health.CurrentHp;
            currentHp = startHp;
            isReady = true;
            message = $"자동 공격 검증 준비 완료: 캐릭터 시작 HP {startHp:0.##}";
            Debug.Log(message, this);
        }

        public void StartTest()
        {
            if (!isReady || target == null)
            {
                message = "먼저 자동 공격 검증 준비를 실행하세요.";
                Debug.LogWarning(message, this);
                return;
            }

            isRunning = true;
            elapsedSeconds = 0f;
            message = "저지된 몬스터 2마리의 자동 공격을 시작했습니다.";
            Debug.Log(message, this);
        }

        public void StopTest()
        {
            isRunning = false;
            currentHp = target == null || target.Health == null ? 0f : target.Health.CurrentHp;
            message = "자동 공격 검증을 수동으로 정지했습니다.";
            Debug.Log(message, this);
        }

        public void ResetResult()
        {
            attacks = new EnemyAttack[EnemyCount];
            target = null;
            isReady = false;
            isRunning = false;
            elapsedSeconds = 0f;
            startHp = 0f;
            currentHp = 0f;
            firstAttackCount = 0;
            secondAttackCount = 0;
            thirdAttackCount = 0;
            finalPassed = false;
            message = string.Empty;
        }

        private void CompleteTest()
        {
            isRunning = false;
            currentHp = target == null || target.Health == null ? 0f : target.Health.CurrentHp;
            finalPassed = firstAttackCount > 0 && secondAttackCount > 0 && thirdAttackCount == 0 && currentHp < startHp;

            if (finalPassed)
            {
                message = $"자동 공격 검증 성공: 첫 번째 {firstAttackCount}회, 두 번째 {secondAttackCount}회, 세 번째 {thirdAttackCount}회, 적용 피해 {AppliedDamage:0.##}";
                Debug.Log(message, this);
                return;
            }

            message = $"자동 공격 검증 실패: 첫 번째 {firstAttackCount}회, 두 번째 {secondAttackCount}회, 세 번째 {thirdAttackCount}회, 적용 피해 {AppliedDamage:0.##}";
            Debug.LogWarning(message, this);
        }

        private void FailTest(string failureMessage)
        {
            isReady = false;
            isRunning = false;
            finalPassed = false;
            message = failureMessage;
            Debug.LogWarning(message, this);
        }
    }
}