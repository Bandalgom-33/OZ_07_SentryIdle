using System.Collections;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RaidBattleController))]
    [RequireComponent(typeof(RaidBoardRuntime))]
    public sealed class RaidAutoStartRuntime : MonoBehaviour
    {
        private const float ReadyTimeoutSeconds = 10f;

        private RaidBattleController battle;
        private RaidBoardRuntime board;
        private Coroutine startRoutine;

        private void Awake()
        {
            battle = GetComponent<RaidBattleController>();
            board = GetComponent<RaidBoardRuntime>();
        }

        private void OnEnable()
        {
            if (startRoutine == null)
            {
                startRoutine = StartCoroutine(WaitForBoardAndStart());
            }
        }

        private void OnDisable()
        {
            if (startRoutine != null)
            {
                StopCoroutine(startRoutine);
                startRoutine = null;
            }
        }

        private IEnumerator WaitForBoardAndStart()
        {
            float deadline = Time.realtimeSinceStartup + ReadyTimeoutSeconds;

            while (isActiveAndEnabled)
            {
                if (battle == null || board == null)
                {
                    Debug.LogError("RaidAutoStartRuntime이 RaidBattleController 또는 RaidBoardRuntime을 찾지 못했습니다.", this);
                    startRoutine = null;
                    yield break;
                }

                if (battle.State != RaidBattleState.Idle || battle.IsPreparing)
                {
                    startRoutine = null;
                    yield break;
                }

                if (board.Board != null)
                {
                    bool started = battle.BeginRaid();

                    if (!started && battle.State == RaidBattleState.Idle && !battle.IsPreparing)
                    {
                        Debug.LogError("Raid 씬은 준비되었지만 RaidBattleController.BeginRaid()가 시작 요청을 거부했습니다.", battle);
                    }

                    startRoutine = null;
                    yield break;
                }

                if (Time.realtimeSinceStartup >= deadline)
                {
                    Debug.LogError("Raid Board가 10초 안에 준비되지 않아 자동 시작하지 못했습니다.", this);
                    startRoutine = null;
                    yield break;
                }

                yield return null;
            }

            startRoutine = null;
        }
    }
}
