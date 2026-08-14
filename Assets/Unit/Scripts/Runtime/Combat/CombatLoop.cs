using System.Collections.Generic;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    [DisallowMultipleComponent]
    public sealed class CombatLoop : MonoBehaviour
    {
        private static CombatLoop activeLoop;

        [Header("전투 갱신")]
        [Tooltip("등록된 캐릭터와 몬스터의 전투 기능을 매 프레임 갱신할지 설정합니다.")]
        [SerializeField] private bool isRunning = true;

        private readonly List<UnitRuntimeState> unitBuffer = new List<UnitRuntimeState>();
        private readonly List<EnemyRuntimeState> enemyBuffer = new List<EnemyRuntimeState>();

        public bool IsRunning => isRunning;
        public int UnitCount => CombatRegistry.UnitCount;
        public int EnemyCount => CombatRegistry.EnemyCount;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetActiveLoop()
        {
            activeLoop = null;
        }

        private void OnEnable()
        {
            if (activeLoop != null && activeLoop != this)
            {
                Debug.LogError("활성 CombatLoop가 이미 존재합니다. 전투 갱신 중복을 막기 위해 이 CombatLoop를 비활성화합니다.", this);
                enabled = false;
                return;
            }

            activeLoop = this;
        }

        private void OnDisable()
        {
            if (activeLoop == this)
            {
                activeLoop = null;
            }
        }

        private void Update()
        {
            if (!isRunning)
            {
                return;
            }

            Step(Time.deltaTime);
        }

        public void StartLoop()
        {
            isRunning = true;
        }

        public void StopLoop()
        {
            isRunning = false;
        }

        public void Step(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            SummonLifetimeRegistry.Step(deltaTime);
            CopyActors();
            StepEnemyMovement(deltaTime);
            StepUnits(deltaTime);
            StepEnemyAttacks(deltaTime);
        }

        private void CopyActors()
        {
            unitBuffer.Clear();
            enemyBuffer.Clear();

            foreach (UnitRuntimeState unit in CombatRegistry.Units)
            {
                if (unit != null)
                {
                    unitBuffer.Add(unit);
                }
            }

            foreach (EnemyRuntimeState enemy in CombatRegistry.Enemies)
            {
                if (enemy != null)
                {
                    enemyBuffer.Add(enemy);
                }
            }
        }

        private void StepEnemyMovement(float deltaTime)
        {
            for (int i = 0; i < enemyBuffer.Count; i++)
            {
                EnemyRuntimeState enemy = enemyBuffer[i];

                if (!CanStep(enemy))
                {
                    continue;
                }

                enemy.StepPassiveRuntime(deltaTime);

                if (enemy.IsSummon && enemy.SummonRuntime != null && enemy.SummonRuntime.IsInitialized && enemy.SummonRuntime.Chase != null)
                {
                    enemy.SummonRuntime.Chase.Step(deltaTime);
                    continue;
                }

                enemy.Move.Step(deltaTime);
            }
        }

        private void StepUnits(float deltaTime)
        {
            for (int i = 0; i < unitBuffer.Count; i++)
            {
                UnitRuntimeState unit = unitBuffer[i];

                if (!CanStep(unit))
                {
                    continue;
                }

                unit.StepPassiveRuntime(deltaTime);
                unit.StepHealthRegeneration(deltaTime);
                unit.StepSkillGaugeRegeneration(deltaTime);

                if (unit.Attack != null)
                {
                    unit.Attack.Step(deltaTime);
                }
            }
        }

        private void StepEnemyAttacks(float deltaTime)
        {
            for (int i = 0; i < enemyBuffer.Count; i++)
            {
                EnemyRuntimeState enemy = enemyBuffer[i];

                if (!CanStep(enemy))
                {
                    continue;
                }

                if (enemy.Attack != null)
                {
                    enemy.Attack.Step(deltaTime);
                }
            }
        }

        private static bool CanStep(UnitRuntimeState unit)
        {
            return unit != null && unit.IsInitialized && unit.Health != null && !unit.Health.IsDead;
        }

        private static bool CanStep(EnemyRuntimeState enemy)
        {
            return enemy != null && enemy.IsInitialized && enemy.Health != null && !enemy.Health.IsDead && enemy.Move != null && !enemy.Move.HasReachedGoal;
        }
    }
}