using System;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    [Serializable]
    public sealed class AttackProgressState
    {
        [Tooltip("다음 기본 공격을 위해 누적된 공격 진행도입니다. 1 이상이면 공격 실행 가능 횟수가 존재합니다.")]
        [Min(0f)]
        [SerializeField] private float progress;

        public float Progress => progress;
        public int ReadyAttackCount => progress >= 1f ? Mathf.FloorToInt(progress) : 0;

        public void Reset()
        {
            progress = 0f;
        }

        public void Advance(float attacksPerSecond, float deltaTime)
        {
            if (attacksPerSecond <= 0f || deltaTime <= 0f)
            {
                return;
            }

            float addedProgress = attacksPerSecond * deltaTime;
            progress = Mathf.Min(progress + addedProgress, int.MaxValue);
        }

        public int ConsumeReadyAttacks(int maxAttackCount)
        {
            if (maxAttackCount <= 0)
            {
                return 0;
            }

            int consumedCount = Mathf.Min(ReadyAttackCount, maxAttackCount);
            progress -= consumedCount;
            return consumedCount;
        }
    }
}