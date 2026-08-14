using System;
using EndlessGuard.Unit.Runtime;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    public enum RaidBattleState
    {
        Idle = 0,
        Running = 1,
        Victory = 2,
        Defeat = 3
    }

    public enum RaidBattleResult
    {
        Victory = 0,
        Defeat = 1
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(CombatLoop))]
    public sealed class RaidBattleController : MonoBehaviour
    {
        [Header("레이드 진행 상태")]
        [Tooltip("현재 레이드 전투의 진행 상태입니다.")]
        [SerializeField] private RaidBattleState state = RaidBattleState.Idle;

        private CombatLoop combatLoop;

        public RaidBattleState State => state;
        public bool IsRunning => state == RaidBattleState.Running;

        public event Action OnRaidStarted;
        public event Action<RaidBattleResult> OnRaidEnded;
        public event Action<RaidBattleState> OnStateChanged;

        private void Awake()
        {
            combatLoop = GetComponent<CombatLoop>();
            combatLoop.StopLoop();
            state = RaidBattleState.Idle;
        }

        private void OnDisable()
        {
            if (combatLoop != null)
            {
                combatLoop.StopLoop();
            }
        }

        public bool BeginRaid()
        {
            if (state == RaidBattleState.Running)
            {
                return false;
            }

            combatLoop.StartLoop();
            SetState(RaidBattleState.Running);
            OnRaidStarted?.Invoke();

            Debug.Log("레이드 전투 시작", this);
            return true;
        }

        public bool EndRaid(RaidBattleResult result)
        {
            if (state != RaidBattleState.Running)
            {
                return false;
            }

            combatLoop.StopLoop();

            RaidBattleState endState = result == RaidBattleResult.Victory ? RaidBattleState.Victory : RaidBattleState.Defeat;

            SetState(endState);
            OnRaidEnded?.Invoke(result);

            Debug.Log($"레이드 전투 종료: {result}", this);
            return true;
        }

        public void ResetRaid()
        {
            combatLoop.StopLoop();
            SetState(RaidBattleState.Idle);
        }

        private void SetState(RaidBattleState nextState)
        {
            if (state == nextState)
            {
                return;
            }

            state = nextState;
            OnStateChanged?.Invoke(state);
        }
    }
}