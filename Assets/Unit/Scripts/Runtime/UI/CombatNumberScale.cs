using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    [DisallowMultipleComponent]
    public sealed class CombatNumberScale : MonoBehaviour
    {
        [Tooltip("이 오브젝트 아래에서 표시되는 일반 피해/피격/회복/MISS 전투 숫자의 크기 배율입니다. 일반전투 기본값은 1입니다.")]
        [Range(0.1f, 3f)]
        [SerializeField] private float scale = 1f;

        [Tooltip("이 오브젝트 아래에서 표시되는 치명타 숫자의 크기 배율입니다. 일반전투 기본값은 1입니다.")]
        [Range(0.1f, 3f)]
        [SerializeField] private float criticalScale = 1f;

        public float Scale => Mathf.Clamp(scale, 0.1f, 3f);
        public float CriticalScale => Mathf.Clamp(criticalScale, 0.1f, 3f);

        public void SetScales(float value, float criticalValue)
        {
            scale = Mathf.Clamp(value, 0.1f, 3f);
            criticalScale = Mathf.Clamp(criticalValue, 0.1f, 3f);
        }

        private void OnValidate()
        {
            scale = Mathf.Clamp(scale, 0.1f, 3f);
            criticalScale = Mathf.Clamp(criticalScale, 0.1f, 3f);
        }
    }
}
