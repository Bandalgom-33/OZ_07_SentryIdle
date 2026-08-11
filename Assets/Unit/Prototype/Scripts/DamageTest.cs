using EndlessGuard.Unit.Runtime;
using UnityEngine;

namespace EndlessGuard.Unit.Prototype
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CombatStatePrototypeController))]
    [RequireComponent(typeof(CombatLoop))]
    public sealed class DamageTest : MonoBehaviour
    {
        [Header("검증 대상 연결")]
        [Tooltip("검증용 캐릭터를 생성하는 기존 전투 상태 컴포넌트입니다.")]
        [SerializeField] private CombatStatePrototypeController state;

        [Tooltip("연속 피해 검증 중 다른 자동 전투가 실행되지 않도록 정지할 전투 루프입니다.")]
        [SerializeField] private CombatLoop combatLoop;

        [Header("연속 피해 설정")]
        [Tooltip("한 번에 적용할 피해량입니다.")]
        [Min(1f)]
        [SerializeField] private float damageAmount = 600f;

        [Tooltip("연속으로 적용할 피해 횟수입니다.")]
        [Min(1)]
        [SerializeField] private int hitCount = 6;

        [Tooltip("각 피해 사이의 시간 간격입니다.")]
        [Min(0.01f)]
        [SerializeField] private float hitInterval = 0.1f;

        [Tooltip("연타 완료 후 모든 피해 숫자가 풀로 돌아올 때까지 기다리는 최대 시간입니다.")]
        [Min(0.5f)]
        [SerializeField] private float returnTimeoutSeconds = 2f;

        [HideInInspector][SerializeField] private UnitRuntimeState target;
        [HideInInspector][SerializeField] private bool isReady;
        [HideInInspector][SerializeField] private bool isRunning;
        [HideInInspector][SerializeField] private bool burstComplete;
        [HideInInspector][SerializeField] private bool numberLimitPassed;
        [HideInInspector][SerializeField] private bool damagePassed;
        [HideInInspector][SerializeField] private bool poolReturnStarted;
        [HideInInspector][SerializeField] private bool poolReturnPassed;
        [HideInInspector][SerializeField] private bool finalPassed;
        [HideInInspector][SerializeField] private int appliedHitCount;
        [HideInInspector][SerializeField] private int peakActiveCount;
        [HideInInspector][SerializeField] private int activeCountAfterBurst;
        [HideInInspector][SerializeField] private float startHp;
        [HideInInspector][SerializeField] private float currentHp;
        [HideInInspector][SerializeField] private float totalAppliedDamage;
        [HideInInspector][SerializeField] private float elapsedSeconds;
        [HideInInspector][SerializeField] private float returnElapsedSeconds;

        [HideInInspector]
        [TextArea(2, 4)]
        [SerializeField] private string message;

        private DamageNumberPool pool;
        private float hitElapsedSeconds;

        public UnitRuntimeState Target => target;
        public bool IsReady => isReady;
        public bool IsRunning => isRunning;
        public bool BurstComplete => burstComplete;
        public bool NumberLimitPassed => numberLimitPassed;
        public bool DamagePassed => damagePassed;
        public bool PoolReturnStarted => poolReturnStarted;
        public bool PoolReturnPassed => poolReturnPassed;
        public bool FinalPassed => finalPassed;
        public int AppliedHitCount => appliedHitCount;
        public int PeakActiveCount => peakActiveCount;
        public int ActiveCountAfterBurst => activeCountAfterBurst;
        public int CurrentActiveCount => pool == null ? 0 : pool.ActiveCount;
        public int AvailableCount => pool == null ? 0 : pool.AvailableCount;
        public int MaxNumbersPerTarget => pool == null ? 0 : pool.MaxNumbersPerTarget;
        public float StartHp => startHp;
        public float CurrentHp => currentHp;
        public float TotalAppliedDamage => totalAppliedDamage;
        public float ElapsedSeconds => elapsedSeconds;
        public float ReturnElapsedSeconds => returnElapsedSeconds;
        public string Message => message;

        private void Reset()
        {
            state = GetComponent<CombatStatePrototypeController>();
            combatLoop = GetComponent<CombatLoop>();
        }

        private void OnValidate()
        {
            if (state == null)
            {
                state = GetComponent<CombatStatePrototypeController>();
            }

            if (combatLoop == null)
            {
                combatLoop = GetComponent<CombatLoop>();
            }

            damageAmount = Mathf.Max(1f, damageAmount);
            hitCount = Mathf.Max(1, hitCount);
            hitInterval = Mathf.Max(0.01f, hitInterval);
            returnTimeoutSeconds = Mathf.Max(0.5f, returnTimeoutSeconds);
        }

        private void Update()
        {
            if (!isRunning || target == null || pool == null)
            {
                return;
            }

            elapsedSeconds += Time.deltaTime;

            if (!burstComplete)
            {
                UpdateBurst(Time.deltaTime);
                return;
            }

            UpdatePoolReturn(Time.deltaTime);
        }

        private void OnDisable()
        {
            isRunning = false;
        }

        public void SetupTest()
        {
            ResetResult();

            if (state == null || combatLoop == null)
            {
                FailTest("CombatStatePrototypeController 또는 CombatLoop가 연결되지 않았습니다.");
                return;
            }

            pool = DamageNumberPool.Instance;

            if (pool == null)
            {
                FailTest("씬에서 DamageNumberPool을 찾지 못했습니다.");
                return;
            }

            if (pool.ActiveCount > 0)
            {
                FailTest("아직 활성 피해 숫자가 남아 있습니다. 모두 사라진 뒤 다시 준비하세요.");
                return;
            }

            if (hitCount != pool.MaxNumbersPerTarget)
            {
                FailTest($"이번 검증은 Hit Count와 Max Numbers Per Target을 같게 사용합니다. 현재 {hitCount} / {pool.MaxNumbersPerTarget}");
                return;
            }

            combatLoop.StopLoop();
            state.SpawnActors();
            target = state.SpawnedUnit;

            if (target == null || target.Health == null)
            {
                FailTest("검증용 캐릭터를 생성하지 못했습니다.");
                return;
            }

            if (target.GetComponent<DamageNumberEmitter>() == null)
            {
                FailTest("검증용 캐릭터에 DamageNumberEmitter가 없습니다.");
                return;
            }

            float requiredHp = damageAmount * hitCount;

            if (target.Health.CurrentHp <= requiredHp)
            {
                FailTest($"캐릭터 HP가 피해 {damageAmount:0} × {hitCount}회를 버티기에 부족합니다.");
                return;
            }

            startHp = target.Health.CurrentHp;
            currentHp = startHp;
            isReady = true;

            message = $"Push 피해 숫자 검증 준비 완료: 피해 {damageAmount:0} × {hitCount}회, 간격 {hitInterval:0.###}초, 최대 표시 {pool.MaxNumbersPerTarget}개";
            Debug.Log(message, this);
        }

        public void StartTest()
        {
            if (!isReady || target == null || pool == null)
            {
                message = "먼저 피해 숫자 검증 준비를 실행하세요.";
                Debug.LogWarning(message, this);
                return;
            }

            isRunning = true;
            hitElapsedSeconds = hitInterval;
            elapsedSeconds = 0f;
            returnElapsedSeconds = 0f;

            message = "피해 숫자 Pop·Push 검증을 시작했습니다.";
            Debug.Log(message, this);
        }

        public void StopTest()
        {
            isRunning = false;
            currentHp = target == null || target.Health == null ? 0f : target.Health.CurrentHp;

            message = "피해 숫자 검증을 수동으로 정지했습니다.";
            Debug.Log(message, this);
        }

        public void ResetResult()
        {
            isRunning = false;
            isReady = false;
            burstComplete = false;
            numberLimitPassed = false;
            damagePassed = false;
            poolReturnStarted = false;
            poolReturnPassed = false;
            finalPassed = false;
            appliedHitCount = 0;
            peakActiveCount = 0;
            activeCountAfterBurst = 0;
            startHp = 0f;
            currentHp = 0f;
            totalAppliedDamage = 0f;
            elapsedSeconds = 0f;
            returnElapsedSeconds = 0f;
            hitElapsedSeconds = 0f;
            target = null;
            pool = DamageNumberPool.Instance;
            message = string.Empty;
        }

        private void UpdateBurst(float deltaTime)
        {
            hitElapsedSeconds += deltaTime;

            if (hitElapsedSeconds < hitInterval)
            {
                return;
            }

            hitElapsedSeconds -= hitInterval;
            ApplyHit();

            if (!isRunning || appliedHitCount < hitCount)
            {
                return;
            }

            burstComplete = true;
            poolReturnStarted = true;
            returnElapsedSeconds = 0f;
            activeCountAfterBurst = pool.ActiveCount;
            currentHp = target.Health.CurrentHp;

            numberLimitPassed = peakActiveCount == pool.MaxNumbersPerTarget && activeCountAfterBurst == pool.MaxNumbersPerTarget;
            damagePassed = Mathf.Approximately(startHp - currentHp, totalAppliedDamage);

            Debug.Log($"연속 피해 완료: {appliedHitCount}회, 최대 동시 숫자 {peakActiveCount}, 현재 활성 숫자 {activeCountAfterBurst}", this);
        }

        private void UpdatePoolReturn(float deltaTime)
        {
            returnElapsedSeconds += deltaTime;

            if (pool.ActiveCount == 0)
            {
                poolReturnPassed = true;
                CompleteTest();
                return;
            }

            if (returnElapsedSeconds >= returnTimeoutSeconds)
            {
                FailTest($"제한 시간 {returnTimeoutSeconds:0.##}초 안에 피해 숫자가 모두 풀로 반환되지 않았습니다. 남은 활성 숫자 {pool.ActiveCount}");
            }
        }

        private void ApplyHit()
        {
            float appliedDamage = target.ApplyDamage(damageAmount);

            if (appliedDamage <= 0f)
            {
                FailTest("피해 적용에 실패했거나 캐릭터가 먼저 사망했습니다.");
                return;
            }

            appliedHitCount++;
            totalAppliedDamage += appliedDamage;
            currentHp = target.Health.CurrentHp;
            peakActiveCount = Mathf.Max(peakActiveCount, pool.ActiveCount);

            Debug.Log($"연속 피해 {appliedHitCount}/{hitCount}: 피해 {appliedDamage:0}, 활성 숫자 {pool.ActiveCount}", this);
        }

        private void CompleteTest()
        {
            isRunning = false;
            currentHp = target == null || target.Health == null ? 0f : target.Health.CurrentHp;
            poolReturnPassed = pool != null && pool.ActiveCount == 0;

            finalPassed = burstComplete && numberLimitPassed && damagePassed && poolReturnStarted && poolReturnPassed;

            if (finalPassed)
            {
                message = $"Push 피해 숫자 검증 성공: {appliedHitCount}회 연속 피해, 최대 {peakActiveCount}개 표시, 전부 자연 소멸 후 풀 반환";
                Debug.Log(message, this);
                return;
            }

            FailTest($"피해 숫자 검증 실패: 표시 제한 {numberLimitPassed}, 피해 적용 {damagePassed}, 풀 반환 {poolReturnPassed}");
        }

        private void FailTest(string failureMessage)
        {
            isRunning = false;
            finalPassed = false;
            message = failureMessage;
            Debug.LogWarning(message, this);
        }
    }
}