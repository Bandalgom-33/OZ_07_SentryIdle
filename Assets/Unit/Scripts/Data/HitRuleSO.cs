using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    [CreateAssetMenu(fileName = "HitRule", menuName = "Endless Guard/Combat/Hit Rule")]
    public sealed class HitRuleSO : ScriptableObject
    {
        [Header("명중·회피 계산 규칙")]
        [Tooltip("회피력을 명중 확률 계산식에 반영할 때 곱하는 가중치입니다. 기본값 0.25에서는 명중력과 회피력이 같을 때 명중률이 80%입니다.")]
        [Min(0f)]
        [SerializeField] private float evasionWeight = 0.25f;

        [Tooltip("명중력과 회피력이 모두 0일 때 사용할 명중률입니다.")]
        [Range(0f, 100f)]
        [SerializeField] private float zeroStatHitChancePercent = 100f;

        [Tooltip("일반 공격 명중 판정에서 허용할 최소 명중률입니다.")]
        [Range(0f, 100f)]
        [SerializeField] private float minimumHitChancePercent = 5f;

        [Tooltip("일반 공격 명중 판정에서 허용할 최대 명중률입니다.")]
        [Range(0f, 100f)]
        [SerializeField] private float maximumHitChancePercent = 100f;

        public float EvasionWeight => Mathf.Max(0f, evasionWeight);
        public float MinimumHitChancePercent => Mathf.Clamp(minimumHitChancePercent, 0f, 100f);
        public float MaximumHitChancePercent => Mathf.Clamp(maximumHitChancePercent, MinimumHitChancePercent, 100f);
        public float ZeroStatHitChancePercent => Mathf.Clamp(zeroStatHitChancePercent, MinimumHitChancePercent, MaximumHitChancePercent);

        private void OnValidate()
        {
            evasionWeight = Mathf.Max(0f, evasionWeight);
            minimumHitChancePercent = Mathf.Clamp(minimumHitChancePercent, 0f, 100f);
            maximumHitChancePercent = Mathf.Clamp(maximumHitChancePercent, minimumHitChancePercent, 100f);
            zeroStatHitChancePercent = Mathf.Clamp(zeroStatHitChancePercent, minimumHitChancePercent, maximumHitChancePercent);
        }
    }
}