using System;
using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [Serializable]
    public sealed class LevelExpOverride
    {
        [Tooltip("이 레벨에서 다음 레벨로 올라갈 때 예외 경험치를 적용합니다. 29를 입력하면 Lv.29에서 Lv.30으로 올라갈 때 사용됩니다.")]
        [Min(1)]
        [SerializeField] private int currentLevel = 1;

        [Tooltip("해당 레벨에서 다음 레벨로 올라가기 위해 필요한 경험치입니다.")]
        [SerializeField] private long requiredExp = 1L;

        public int CurrentLevel => Mathf.Max(1, currentLevel);
        public long RequiredExp => Math.Max(1L, requiredExp);
    }
}